# Swealium documentation

Documentation for **0.1.0-alpha.1 / .NET 10**. This describes the implemented API in this repository, including its current limitations. Swealium is a retained-mode GUI foundation with CPU raster drawing, SDL3 platform integration, explicit UI-thread ownership, and a small widget set.

## Start here

1. [Build and run your first application](getting-started.md).
2. [Understand the application and rendering lifecycle](architecture.md).
3. [Arrange a visual tree](layout.md).
4. [Handle pointer, keyboard, and text input](input.md).
5. [Draw and create custom widgets](custom-widgets.md).

## Task guides

| Guide | Topics |
| --- | --- |
| [Windows and lifecycle](windows.md) | Independent windows, ownership, close cancellation, visibility, state, events, subclass hooks |
| [Dialogs and system prompts](dialogs.md) | Nested modality, async results, native warnings, confirmation presets |
| [Layout](layout.md) | Measure/arrange, box model, stack/overlay/absolute, alignment, growth, constraints, clipping |
| [Grid](grid.md) | Fixed/content/fraction tracks, cell placement, spans, implicit tracks, row/column limits |
| [Input](input.md) | Hit regions, routing, capture, focus, keyboard activation, Unicode |
| [Drawing](drawing.md) | Logical units, frame-scoped context, shapes, text, radii, DPI |
| [Text and editing](text.md) | Shaping, bidi, HTML text markup, Text/TextBox, selection, clipboard, IME preedit/candidates |
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
- [Text, editing, composition, and clipboard contracts](api/text.md)
- [Input contracts and enums](api/input.md)
- [Complete public/protected signatures](api/reference.md)

Inherited widget members are documented once on Widget. WidgetCollection also inherits the standard Collection<Widget> operations. Record-generated equality, formatting, and positional deconstruction are included in the signature inventory.

## Current boundaries

Desktop windowing, stack/grid/wrap layout, weighted allocation, shaped text, formatted HTML text, plain-text editing, IME event/preedit/candidate rendering, multi-window ownership, and dialogs are implemented. Windows x64 has local native runtime verification; Linux/macOS CI is configured but requires target runtime verification for changes. Interactive IMEs require manual OS/provider testing. iOS/Android native lifecycle hosts are **not implemented**. There is no standalone scrolling container, multiple-pointer API, accessibility bridge, or GPU Skia backend.

Examples in the guides use Swealium-owned public types. The backend's Skia and SDL classes remain internal. Native interop deliberately exposes only a borrowed OS handle descriptor, never an SDL_Window pointer.

