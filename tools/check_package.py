"""Validate release/tag versions and packed metadata without external Python dependencies."""
from pathlib import Path
from zipfile import ZipFile
import argparse
import os
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("directory", nargs="?", type=Path)
parser.add_argument("--release-tag")
args = parser.parse_args()
project = ET.parse(ROOT / "src/Swealium/Swealium.csproj").getroot()
version = project.findtext("./PropertyGroup/Version")
if not version or not re.fullmatch(r"(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?", version):
    raise SystemExit("Use a three-part SemVer project Version, optionally with a prerelease suffix.")
if "-" in version:
    for part in version.split("-", 1)[1].split("."):
        if part.isdigit() and len(part) > 1 and part[0] == "0":
            raise SystemExit("Numeric prerelease identifiers cannot start with zero.")
if args.release_tag and args.release_tag not in (version, "v" + version):
    raise SystemExit(f"Release tag {args.release_tag!r} does not match project Version {version!r}.")
if os.environ.get("GITHUB_OUTPUT"):
    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
        output.write(f"version={version}\n")

if args.directory:
    package = args.directory / f"Swealium.{version}.nupkg"
    symbols = args.directory / f"Swealium.{version}.snupkg"
    with ZipFile(package) as archive:
        names = set(archive.namelist())
        required = {"README.md", "LICENSE", "lib/net10.0/Swealium.dll", "lib/net10.0/Swealium.xml", "docs/README.md"}
        if required - names:
            raise SystemExit(f"Package is missing: {sorted(required - names)}")
        nuspec_name = next(name for name in names if name.endswith(".nuspec"))
        metadata = ET.fromstring(archive.read(nuspec_name)).find("{*}metadata")
        if metadata is None:
            raise SystemExit("Package metadata is missing.")
        def field(name):
            return metadata.findtext("{*}" + name)
        if field("id") != "Swealium" or field("version") != version:
            raise SystemExit("Packed ID/version differs from the project.")
        license_element = metadata.find("{*}license")
        repository = metadata.find("{*}repository")
        if license_element is None or license_element.get("type") != "expression" or license_element.text != "Apache-2.0":
            raise SystemExit("Expected Apache-2.0 license metadata.")
        if repository is None or repository.get("url") != "https://github.com/RustamovHumoyunMirzo/Swealium" or not repository.get("commit"):
            raise SystemExit("Expected source repository and commit metadata.")
        if not field("authors") or field("readme") != "README.md" or not field("projectUrl"):
            raise SystemExit("Expected authors, README and project URL metadata.")
        dependencies = {dependency.get("id"): dependency for dependency in metadata.findall(".//{*}dependency")}
        expected = {item.get("Include"): item.get("Version") for item in project.findall("./ItemGroup/PackageReference")}
        for name, requested in expected.items():
            dependency = dependencies.get(name)
            if dependency is None or dependency.get("version", "").strip("[](), ") != requested:
                raise SystemExit(f"Missing or unexpected transitive dependency: {name} {requested}")
            if set(re.split(r"[,;\s]+", dependency.get("exclude", "").lower())) & {"all", "runtime", "native"}:
                raise SystemExit(f"Runtime dependency must flow to consumers: {name}")
        if any(name.lower().endswith((".ttf", ".ttc")) for name in names):
            raise SystemExit("Sample/test font assets must not be shipped in the framework package.")
    with ZipFile(symbols) as archive:
        if not any(name.endswith("Swealium.pdb") for name in archive.namelist()):
            raise SystemExit("Symbol package must contain the portable PDB.")
    print(f"Verified package and symbols: Swealium {version}; {len(expected)} transitive package dependencies.")
else:
    print(f"Verified release version: {version}")
