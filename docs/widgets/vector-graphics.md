# VectorGraphics

[Widget hub](README.md) · [Inherited Widget API](widget.md) · [Vector guide](../images-and-vectors.md#svg-and-recorded-vectors) · [API contracts](../api/media.md)

`public class VectorGraphics : Widget` displays a borrowed VectorImage, loaded from static SVG/SVGZ or recorded through Swealium DrawingContext. It replays the vector recording at actual display scale rather than pre-rasterizing an icon.

| Public property | Default / contract |
| --- | --- |
| Source | null; borrowed VectorImage; assignment invalidates layout |
| Stretch | ImageStretch.Uniform; also None, Fill, UniformToFill; setter invalidates layout |
| Opacity | 1; finite [0,1] |
| HorizontalContentAlignment / VerticalContentAlignment | Center; Start/End position; Stretch positions as Start |

```csharp
using var icon = VectorImage.Load("icon.svg");
var view = new VectorGraphics { Source = icon, Width = 96, Height = 96,
    Padding = new(8), Stretch = ImageStretch.Uniform };
```

Measurement uses Source.Size and available constraints, preserving aspect ratio for Uniform/UniformToFill. Fully unbounded measurement returns natural size. Draw uses actual ContentBounds, scales the recording's ViewBox (including nonzero origin), aligns it, and clips to the padded content area. A null Source measures as zero content and paints nothing.

Invalid enums/opacity throw ArgumentOutOfRangeException. Source is borrowed: assignment/removal does not dispose it. Disposing Source while in use makes subsequent drawing fail with ObjectDisposedException. The widget is not focusable by default and adds no input handling; inherited radii, per-side strokes, cursor, clipping, visibility, events, layout, and ZIndex remain active.

This is a static vector display, with no SVG DOM mutation API, script engine, network asset loader, animation timeline, or automatic path-shaped hit test. Use VectorPath.Contains from custom input/ContainsPoint when a specific path-shaped hit region is needed. Use [Image](image.md) for raster assets; use VectorImage.Rasterize to export vectors or create bitmap cursors.
