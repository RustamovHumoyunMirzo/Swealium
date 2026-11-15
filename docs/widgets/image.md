# Image

[Widget hub](README.md) · [Inherited Widget API](widget.md) · [Media guide](../images-and-vectors.md) · [Bitmap contracts](../api/media.md)

`public class Image : Widget` displays a borrowed Bitmap through DrawingContext.DrawBitmap. No resource loading, native bitmap type, or implicit disposal is hidden in the widget.

| Public property | Default / contract |
| --- | --- |
| Source | null; Bitmap reference, borrowed; assignment invalidates layout |
| SourceRectangle | null: entire bitmap; otherwise nonnegative finite crop; assignment invalidates layout |
| Stretch | ImageStretch.Uniform; None/Fill/UniformToFill also available |
| Opacity | 1; finite [0,1] |
| Interpolation | BitmapInterpolation.Linear; also Nearest/Cubic |
| HorizontalContentAlignment / VerticalContentAlignment | Center; Start/End position within ContentBounds; Stretch positions as Start |

Source null means no intrinsic content/no image drawing. Measurement uses crop dimensions or source pixel size, then applies constrained aspect-preserving measurement for Uniform/Cover or independent Fill sizing. Unbounded measurement uses natural dimensions. Explicit Width/Height, Min/Max, margin/padding, grid placement, and inherited alignment retain their normal behavior. Draw uses current actual ContentBounds, applies scaling/alignment, and clips to the padded content area. UniformToFill therefore crops without painting padding.

```csharp
using var bitmap = Bitmap.Load("photo.webp");
var image = new Image { Source = bitmap, Width = 240, Height = 160,
    Stretch = ImageStretch.UniformToFill, Radii = new(12), Padding = new(4) };
```

Finite-range/enum validation rejects bad opacity/interpolation/stretch/alignment/rect values. A crop outside Source is rejected when drawn. Accessing a disposed Source during measurement/drawing throws; detach consumers before disposing shared resources. Pixel changes appear on subsequent frames without size invalidation because a Bitmap's dimensions do not change. Source assignment never disposes the old/new bitmap.

Image is not focusable by default and adds no input behavior, but inherited pointer/keyboard/Click hooks remain available. Radii clip/hit testing, Stroke, cursor inheritance, and ZIndex work like any other widget. For static SVG or vector recordings use [VectorGraphics](vector-graphics.md).
