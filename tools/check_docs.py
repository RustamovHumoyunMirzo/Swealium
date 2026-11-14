"""Check local Markdown links/anchors, image alternatives, and SVG safety/accessibility."""
from pathlib import Path
from urllib.parse import unquote, urlsplit
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PAGES = [ROOT / "README.md", *sorted((ROOT / "docs").rglob("*.md"))]
errors = []
cache = {}

def plain_markdown(path):
    return re.sub(r"```[^\n]*\n.*?```", "", path.read_text(encoding="utf-8"), flags=re.S)

def anchors(path):
    if path in cache:
        return cache[path]
    content = plain_markdown(path)
    result = set(re.findall(r'<a\s+id="([^"]+)"', content))
    counts = {}
    for heading in re.findall(r"^#{1,6}\s+(.+?)\s*#*\s*$", content, re.M):
        value = re.sub(r"[^\w\-\s]", "", heading.replace("`", "").lower())
        value = value.replace(" ", "-")
        count = counts.get(value, 0)
        result.add(value + (f"-{count}" if count else ""))
        counts[value] = count + 1
    cache[path] = result
    return result

links = 0
for page in PAGES:
    content = plain_markdown(page)
    for match in re.finditer(r"(!?)\[([^\]]*)\]\(([^)]+)\)", content):
        image, label, destination = match.groups()
        destination = destination.strip().strip("<>")
        parsed = urlsplit(destination)
        if parsed.scheme or destination.startswith("//"):
            continue
        links += 1
        if image and not label.strip():
            errors.append(f"{page.relative_to(ROOT)}: image has no alternative text")
        target = (page.parent / unquote(parsed.path)).resolve() if parsed.path else page
        if not target.is_relative_to(ROOT):
            errors.append(f"{page.relative_to(ROOT)}: link escapes repository: {destination}")
        elif not target.is_file():
            errors.append(f"{page.relative_to(ROOT)}: missing target: {destination}")
        elif parsed.fragment and target.suffix == ".md" and unquote(parsed.fragment) not in anchors(target):
            errors.append(f"{page.relative_to(ROOT)}: missing anchor: {destination}")

svgs = sorted((ROOT / "docs" / "images").glob("*.svg"))
ns = {"svg": "http://www.w3.org/2000/svg"}
for path in svgs:
    try:
        root = ET.parse(path).getroot()
        if root.tag != "{http://www.w3.org/2000/svg}svg":
            errors.append(f"{path.name}: not an SVG root")
        if root.find("svg:title", ns) is None or root.find("svg:desc", ns) is None:
            errors.append(f"{path.name}: missing accessible title/description")
        ids = {element.attrib["id"] for element in root.iter() if "id" in element.attrib}
        for name in root.attrib.get("aria-labelledby", "").split():
            if name not in ids:
                errors.append(f"{path.name}: missing aria-labelledby target {name}")
        if not root.attrib.get("viewBox"):
            errors.append(f"{path.name}: missing viewBox")
        for element in root.iter():
            if element.tag.split("}")[-1] in {"script", "foreignObject"}:
                errors.append(f"{path.name}: active embedded content")
            for key, value in element.attrib.items():
                if key.split("}")[-1] in {"href", "src"} and not value.startswith("#"):
                    errors.append(f"{path.name}: external resource {value}")
    except ET.ParseError as error:
        errors.append(f"{path.name}: {error}")
if errors:
    raise SystemExit("\n".join(errors))
print(f"Documentation verified: {len(PAGES)} Markdown pages, {links} local links, {len(svgs)} accessible SVGs.")

