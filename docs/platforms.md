# Platform support, deployment, and packaging

[Documentation index](README.md)

## Supported versus verified

| Target | Implementation/dependencies | Runtime evidence in this workspace |
| --- | --- | --- |
| Windows x64 | Desktop loop, windows, input, CPU renderer, ownership/dialogs | Local headless suite, visible multi-window smoke, native window-control and handle checks |
| Windows x86/ARM64 | Managed AnyCPU plus dependency-provided native assets | Not executed locally |
| Linux x64/ARM/ARM64 | Desktop code and native assets, SDL backend dependent | CI configured; not observed executing here |
| macOS x64/ARM64 | Desktop code and native assets | CI configured; not observed executing here |
| iOS / Android | Shared primitives/backend code; some dependency native assets | Native lifecycle hosts and runnable app projects are not implemented |

AnyCPU does not imply support for every possible CPU/OS combination. Available native assets, .NET runtimes, and SDL backends bound deployment coverage. [SDL binding package platform matrix](https://github.com/ppy/SDL3-CS/blob/master/README_nuget.md) describes its binaries; it is not proof that this framework has passed runtime tests on each target.

## Dependencies

The library targets net10.0 and pins:

| Package | Version | Purpose |
| --- | --- | --- |
| SkiaSharp | 4.153.1 | Managed raster drawing and platform native references |
| SkiaSharp.NativeAssets.Linux | 4.153.1 | Fontconfig-enabled Linux renderer |
| Topten.RichTextKit | 0.6.173 | Paragraph shaping, bidi, wrapping, fallback, and caret geometry (Apache-2.0) |
| AngleSharp | 1.10.3 | HTML text parsing without resource loaders/scripts (MIT) |
| Svg.Skia | 5.2.3 | Static SVG renderer, document/model dependencies, internal Skia integration (MIT) |
| StbImageSharp | 2.30.16 | Portable additional raster codecs (Unlicense OR MIT) |
| ppy.SDL3-CS | 2026.1002.1 | SDL3 family package/native assets |

Swealium's public API does not require using backend types. Package references remain transitive so runtime assets can resolve. Replacing native binaries independently can break ABI/version assumptions.

RichTextKit brings SkiaSharp.HarfBuzz 4.153.1 and HarfBuzzSharp.NativeAssets.Linux 14.2.1.301 transitively. Publish for a concrete RID and include the corresponding native assets. Third-party terms remain applicable: [RichTextKit](https://github.com/toptensoftware/RichTextKit), [AngleSharp](https://github.com/AngleSharp/AngleSharp), [SkiaSharp](https://github.com/mono/SkiaSharp), and [SDL](https://github.com/libsdl-org/SDL). Fonts are supplied by the deployment environment; no fonts are bundled by Swealium.

Linux requires a graphical display backend and its dependencies. System default text needs fontconfig and installed fonts. Headless tests use SDL_VIDEO_DRIVER=dummy and SDL_RENDER_DRIVER=software; this does not validate compositor behavior, focus activation, real message-box presentation, or every window-manager control.

## Consumer publishing

Publish the application, not the library, for a concrete runtime identifier on a suitable SDK environment:

```sh
dotnet publish samples/Swealium.Sample -c Release -r win-x64 --self-contained false
```

Substitute a supported RID and deployment model. A successful cross-publish is not a runtime smoke test. Verify native files and fonts on the actual target machine. Trimming and NativeAOT have not been established as supported release configurations; test them before relying on them.

Mobile deployment requires platform workloads, signing/build tools, lifecycle integration, native library/framework linking, and appropriate app manifests. The desktop Run method explicitly rejects Android and iOS. Native-handle mappings for UIKit/Android do not change that limitation.

## Package the library

```sh
dotnet pack src/Swealium -c Release -o artifacts
```

The prerelease package ID is Swealium and current version is 0.1.0-alpha.1. No package is published by this repository workflow. Set license, authors, and repository metadata before publishing. The package includes the README and documentation tree so relative documentation links remain available in the archive.

## Validation

CI builds the solution, runs the executable integration harness and a headless multi-window sample, checks generated documentation/signatures/links/SVGs, then packs. Native system prompts are intentionally not opened unattended. Manual/desktop checks remain necessary for native UI presentation and window-manager behavior.

Read [maintenance](maintenance.md) for regeneration commands and [troubleshooting](troubleshooting.md) for deployment failures.

