# GitHub releases and nuget.org

[Documentation index](README.md) · [Installation](getting-started.md) · [Deployment](platforms.md)

The framework package is `Swealium`. Version is defined in `src/Swealium/Swealium.csproj`; the initial release version is **0.1.0**. Releases publish to the official https://api.nuget.org/v3/index.json feed.

## One-time publisher setup

Publishing uses [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) and [NuGet/login](https://github.com/NuGet/login). GitHub exchanges an OIDC identity for a short-lived publishing key. No permanent NuGet API key or account password is stored in the repository.

Sign into nuget.org as **HumoyunMirzo**, open [Trusted Publishing](https://www.nuget.org/account/trustedpublishing), and create a policy:

| Setting | Value |
| --- | --- |
| Policy owner | HumoyunMirzo (the package-owning user account) |
| GitHub repository owner | RustamovHumoyunMirzo |
| Repository | Swealium |
| Workflow file | publish.yml (filename only) |
| Environment | nuget |
| Package pattern | Swealium |
| Scopes | Publish new packages and publish new versions |

The new-package scope is needed for the first publication; future versions also need permission to update this package. Keep the policy scoped to Swealium. The GitHub repository has an environment named `nuget` and a repository variable `NUGET_USER=HumoyunMirzo`; the workflow also defaults to this profile. If the publishing owner changes, update both the NuGet policy and GitHub variable.

## Publish a version

1. Set the csproj Version, update version-specific install/docs examples, and commit to main.
2. Wait for the ordinary build workflow to pass.
3. Publish a GitHub release with the matching tag, such as **v0.1.0**. Plain **0.1.0** is also accepted. The tagged commit must belong to main's history.
4. The `Publish NuGet release` workflow validates the tag/version and runs the reusable three-platform build, integration tests, documentation checks, sample smokes and NuGet-only consumer test.
5. Only when validation succeeds does it build the release package/symbols from that exact tag, attach them to the GitHub release, authenticate through OIDC, and push to nuget.org.

The checked project version, release tag and NuGet version must agree. A mismatched tag fails before publishing; the workflow never silently packages a different version. The GitHub release name can contain descriptive text; its **tag** supplies the checked version. Do not move an already-published tag or attempt to replace a NuGet version; package versions are immutable.

NuGet may take time to validate/index an accepted upload. The GitHub release assets can appear before the package becomes searchable. Check both the workflow result and the [package page](https://www.nuget.org/packages/Swealium).

## Package contract and consumer verification

```sh
dotnet build -c Release
dotnet run --project tests/Swealium.Tests -c Release --no-build
dotnet pack src/Swealium -c Release --no-build -o artifacts/packages
python tools/check_package.py artifacts/packages --release-tag v0.1.0
```

The checker requires matching package/project versions, license, author/repository/commit metadata, README/LICENSE/docs, XML API comments, portable symbols and all declared transitive dependencies. It rejects accidentally bundled sample fonts. SDK-generated Source Link and the repository commit support debugging from the .snupkg.

`tests/Swealium.PackageSmoke` intentionally has **only one PackageReference** to Swealium. It is outside the source solution so a normal source build does not need an already-published Swealium. After packing, CI restores this consumer from the local artifact feed plus nuget.org, then uses only public APIs to exercise native raster rendering, shaping/fonts, HTML, SVG, image decoding, SDL window creation and presentation.

```sh
dotnet restore tests/Swealium.PackageSmoke --configfile tests/Swealium.PackageSmoke/NuGet.Config -p:SwealiumPackageVersion=0.1.0
dotnet run --project tests/Swealium.PackageSmoke -c Release --no-restore -p:SwealiumPackageVersion=0.1.0
```

For headless execution set SDL_VIDEO_DRIVER=dummy and SDL_RENDER_DRIVER=software in the process environment before launching. This test checks native dependency resolution; normal desktop display behavior is validated separately. NuGet cannot install OS drivers, display servers or fonts; see [platform prerequisites](getting-started.md#requirements).

## Retry a failed publication

Fix missing trusted-policy/account configuration, then re-run the failed GitHub job. The workflow also offers `workflow_dispatch` with an existing release tag. It checks and builds that tag, requires matching version and main ancestry, reruns validation, and uploads/pushes the matching artifacts. Its official NuGet push uses `--skip-duplicate` to permit safe retries after a previous accepted upload. A manual retry still requires the GitHub release to exist for asset attachment.

If tests/package checks fail, fix them on main and create the appropriate new release commit/version. Do not force a different package body into an existing published version. Do not commit credentials or paste API keys into source/YAML.
