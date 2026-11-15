# Swealium documentation

Documentation for **0.1.0-alpha.1 / .NET 10**. This describes the implemented API in this repository, including its current limitations. Swealium is a retained-mode GUI foundation with GPU drawing and CPU raster fallback, SDL3 platform integration, explicit UI-thread ownership, and a small widget set.

## Start here

1. [Build and run your first application](getting-started.md).
2. [Understand the application and rendering lifecycle](architecture.md).
3. [Arrange a visual tree](layout.md).
4. [Handle pointer, keyboard, and text input](input.md).
5. [Draw and create custom widgets](custom-widgets.md).
6. [Browse every widget](widgets/README.md), including base APIs and customization hooks.

## Task guides

| Guide | Topics |
| --- | --- |
| [Windows and lifecycle](windows.md) | Independent windows, ownership, close cancellation, visibility, state, events, subclass hooks |
| [Dialogs and system prompts](dialogs.md) | Nested modality, async results, native warnings, confirmation presets |
| [Layout](layout.md) | Measure/arrange, box model, stack/overlay/absolute, alignment, growth, constraints, clipping |
| [Grid](grid.md) | Fixed/content/fraction tracks, cell placement, spans, implicit tracks, row/column limits |
| [Input](input.md) | Hit regions, routing, capture, focus, keyboard activation, Unicode |
| [Multiple pointers](multiple-pointers.md) | Mouse/touch/pen identity, per-contact capture/hover, pressure/tilt, native routing |
| [Accessibility boundary](accessibility.md) | Semantic snapshots, actions, notifications, owned adapter lifecycle; native providers remain separate |
| [Rendering settings](rendering.md) | Application/context anti-aliasing, GPU selection, diagnostics, software fallback |
| [Drawing](drawing.md) | Logical units, frame-scoped context, shapes, text, radii, DPI |
| [Images and vectors](images-and-vectors.md) | Raster formats, Bitmap, offscreen drawing, SVG/SVGZ, paths, scaling, ownership |
| [Cursors](cursors.md) | System types, inheritance/capture, window defaults, hidden and custom bitmap cursors |
| [Text and editing](text.md) | Shaping, bidi, HTML text markup, Text/TextBox, selection, clipboard, IME preedit/candidates |
| [Widget hub](widgets/README.md) | Every public widget, inherited contracts, examples, strokes, and scrollbars |
| [Scrolling](widgets/scroll-container.md) | Directions, viewport/extent, nested input, custom or hidden scrollbars |
| [Custom widgets](custom-widgets.md) | Intrinsic measurement, drawing, event overrides, custom containers |
| [Native interop](native-interop.md) | Portable routing IDs versus borrowed OS handles, window-system kinds, ownership |
| [Platform and deployment](platforms.md) | Native dependencies, target coverage, workloads, packaging, reproducibility |
| [Troubleshooting](troubleshooting.md) | Missing fonts/libraries, geometry, input, unsupported window controls |
| [Maintaining documentation](maintenance.md) | Regenerate signatures and SVGs, validation, contribution expectations |

## API reference

The curated contract pages explain defaults, parameters, results, exceptions, threading, inheritance, and important edge cases. The generated reference enumerates the full declared API, including overloads and protected extension hooks.

- [Windowing and application contracts](api/windowing.md)
- [Dialogs and message-box contracts](api/dialogs.md)
- [Widgets and layout contracts](api/widgets.md)
- [Grid contracts](api/grid.md)
- [Graphics and value-type contracts](api/graphics.md)
- [Bitmap/vector/path contracts](api/media.md)
- [Text, editing, composition, and clipboard contracts](api/text.md)
- [Input contracts and enums](api/input.md)
- [Accessibility contracts](api/accessibility.md)
- [Complete public/protected signatures](api/reference.md)

Inherited widget members are documented once on Widget. WidgetCollection also inherits the standard Collection<Widget> operations. Record-generated equality, formatting, and positional deconstruction are included in the signature inventory.

## Current boundaries

Desktop windowing, stack/grid/wrap/scroll layout, shaped text/editing, media/cursors, multi-window dialogs, and independent mouse/touch/pen routing are implemented. Widget opacity applies to complete subtrees. Accessibility provides semantic trees/actions and a bridge boundary; native UIA/AX/AT-SPI/mobile providers are **not implemented**. Windows x64 has local runtime verification; Linux/macOS CI requires target runtime verification for changes. Physical touch/pen and interactive IMEs need device/provider testing. GPU-backed Skia uses SDL OpenGL/OpenGL ES, with raster fallback. iOS/Android native lifecycle hosts are **not implemented**.
