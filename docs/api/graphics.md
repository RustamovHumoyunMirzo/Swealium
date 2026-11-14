# Graphics and value-type API contracts

[Documentation index](../README.md) · [Complete signatures](reference.md) · [Drawing guide](../drawing.md)

Namespace: Swealium.Graphics. These types expose no Skia objects.

## Coordinate and color records

| Type / constructor | Members | Contract |
| --- | --- | --- |
| Point(float X, float Y) | init X/Y | Logical 2D position; no constructor validation |
| Size(float Width, float Height) | init Width/Height | Logical extent/constraint; no constructor validation |
| Rect(float X, float Y, float Width, float Height) | init components; Contains(Point) | Axis-aligned logical rectangle |
| Color(byte R, byte G, byte B, byte A = 255) | init components; static Transparent/White/Black | Byte RGBA color |

Rect.Contains returns false for nonpositive extent, includes left/top, and excludes right/bottom. Layout accepts only meaningful validated Size values even though the raw record can represent negatives/NaN/infinity.

default(Point/Size/Rect) is all zeros. default(Color) is transparent black, whereas new Color(r,g,b) defaults alpha to 255. Transparent=(0,0,0,0), White=(255,255,255,255), Black=(0,0,0,255).

These readonly records support generated equality/operators, with-expressions, ToString, and positional Deconstruct. Their generated members and init accessors are in the inventory.

## Thickness

Readonly nonpositional record struct. Constructors:

- Thickness(float uniform): all four sides.
- Thickness(float horizontal, float vertical): left/right, then top/bottom.
- Thickness(float left, float top, float right, float bottom): independent sides.

Left/Top/Right/Bottom are read-only. Horizontal=Left+Right; Vertical=Top+Bottom. default(Thickness) is zero. Every supplied side must be finite and nonnegative, otherwise ArgumentOutOfRangeException. There is no mutable side setter or implicit scalar conversion.

Use for Widget.Padding and Margin. DesiredSize includes outer margin; ContentBounds excludes inner padding.

## CornerRadii

Readonly nonpositional record struct. Constructor overloads take uniform or (topLeft, topRight, bottomRight, bottomLeft). The corresponding properties are read-only; default is all zero. Inputs must be finite and nonnegative.

| Method | Contract |
| --- | --- |
| Normalize(Size size) | Common proportional scale makes adjacent circular radii fit width/height; nonpositive size returns zero radii |
| Inset(float amount) | Subtract amount from each radius with a floor of zero; a negative amount increases radii, subject to constructor validity |
| Contains(Point local, Size size) | Rounded-rectangle analytic membership using normalized radii; excludes points outside the half-open base rectangle |

Use meaningful finite extents with Normalize/Contains. The type does not act as a universal invalid-geometry sanitizer. Inset does not know an associated rectangle; normalize against the rectangle when needed.

Widget.Radii uses this shape for hit testing and clipping. DrawingContext rounded primitives independently normalize to their own supplied rectangle.

## DrawingContext

Abstract frame-scoped interface. Protected default constructor supports backend implementations, but callers normally receive it in Draw/Paint.

| Signature shape | Parameters / behavior |
| --- | --- |
| FillRectangle(Rect bounds, Color color, float cornerRadius = 0) | Uniform radius overload forwarding to CornerRadii |
| FillRectangle(Rect bounds, Color color, CornerRadii radii) | Abstract per-corner filled rectangle |
| StrokeRectangle(Rect bounds, Color color, float thickness = 1, float cornerRadius = 0) | Uniform radius stroke overload |
| StrokeRectangle(Rect bounds, Color color, CornerRadii radii, float thickness = 1) | Abstract per-corner stroked rectangle |
| FillEllipse(Rect bounds, Color color) | Abstract filled ellipse within bounds |
| DrawLine(Point start, Point end, Color color, float thickness = 1) | Abstract stroked line |
| DrawText(string text, Point baseline, Color color, float fontSize = 16) | Abstract single-line text at a baseline |
| MeasureText(string text, float fontSize = 16) | Abstract Size: advance width and font ascent/descent height |

All coordinates/radii/stroke/font sizes are logical units, already scaled by the backend. Do not apply window pixel density again. Use finite positive stroke/font sizes; nonpositive values are rejected by the current backend, but not every nonfinite primitive input is explicitly guarded.

The current backend uses antialiased CPU Skia drawing, a default system typeface, and no shaping/wrapping API. Text can contain Unicode but glyph availability and correct complex-script shaping are not guaranteed.

The supplied implementation throws ObjectDisposedException when used after its callback scope. There is no public Dispose, native canvas, save/restore/transform, image, path, or font-family API. Implementing a different DrawingContext does not automatically replace a window's internal backend; no backend injection API is currently exposed.

