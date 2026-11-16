# Swealium

[![Build and smoke test](https://github.com/RustamovHumoyunMirzo/Swealium/actions/workflows/build.yml/badge.svg)](https://github.com/RustamovHumoyunMirzo/Swealium/actions/workflows/build.yml)

A .NET 10 retained-mode GUI framework using SDL3 windowing and SkiaSharp GPU rendering with CPU fallback. Public drawing, layout, input, window, and native-interop types belong to Swealium.

```csharp
using Swealium;
using Swealium.UI;

SwealiumApplication.Run(new WindowOptions { Title = "Hello" }, window =>
{
    var root = new Container { Padding = new(24), Gap = 12 };
    var button = new Button { Text = "Click me" };
    button.Click += () => button.Text = "Hello, Swealium!";
    root.Children.Add(button);
    window.Content = root;
});
```

```sh
dotnet run --project samples/Swealium.Sample
dotnet run --project tests/Swealium.Tests -c Release
dotnet pack src/Swealium -c Release -o artifacts
```

**[Full documentation](docs/README.md)** · [Getting started](docs/getting-started.md) · [API contracts](docs/api/windowing.md) · [Complete API signatures](docs/api/reference.md)

- [Layout and rounded corners](docs/layout.md)
- [Every widget, scrolling, and customizable scrollbars](docs/widgets/README.md)
- [Grid rows, columns, and spans](docs/grid.md)
- [Shaped text, HTML markup, editing, and IME](docs/text.md)
- [Bitmaps, image formats, SVG, and vector paths](docs/images-and-vectors.md)
- [System and custom bitmap cursors](docs/cursors.md)
- [Input and focus](docs/input.md)
- [Multiple pointers](docs/multiple-pointers.md) · [Accessibility bridge boundary](docs/accessibility.md)
- [Gradients in existing Color properties](docs/gradients.md)
- [DrawingCanvas and large point datasets](docs/widgets/drawing-canvas.md)
- [Drawing and custom widgets](docs/custom-widgets.md)
- [Multiple windows and controls](docs/windows.md)
- [Dialogs and native message boxes](docs/dialogs.md)
- [Portable IDs and borrowed native handles](docs/native-interop.md)
- [Platform/deployment status](docs/platforms.md)

Prerelease 0.1.0-alpha.1; no NuGet package is published. Windows x64 is locally verified. Linux/macOS have native assets and CI configuration but require target runtime verification. iOS/Android application hosts are not implemented. See the platform guide for dependencies and current limitations.

[Rendering settings and GPU acceleration](docs/rendering.md) · Application defaults and per-context anti-aliasing.
