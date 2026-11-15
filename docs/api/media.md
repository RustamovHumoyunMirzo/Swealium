# Raster and vector API contracts

[Documentation index](../README.md) · [Usage and format coverage](../images-and-vectors.md) · [Complete signatures](reference.md)

All public media types live in Swealium.Graphics. No backend image, path, SVG, or codec types appear in public signatures. Resources are caller-owned; widgets borrow them. Mutation/drawing/disposal of a shared resource must be serialized.

## Bitmap

Sealed IDisposable raster. Constructor `(int width,int height,Color background=default)` allocates positive finite-size RGBA storage, initially transparent unless supplied otherwise. Default pixel cap is 64 million, with a hard byte-array representability limit. Invalid dimensions/caps raise InvalidDataException. Allocation may fail independently of limits.

| Member | Contract |
| --- | --- |
| Width / Height / Size | Pixel dimensions; access after disposal throws |
| IsDisposed | Whether pixel storage has been released |
| Load(string path,ImageLoadOptions? options=null) | Read owned file handle then close it; ordinary file errors propagate |
| Load(Stream,ImageLoadOptions? options=null) | Read remaining bytes up to limit, preserving stream ownership |
| Decode(ReadOnlySpan&lt;byte&gt;,ImageLoadOptions? options=null) | Detect native/portable format, apply JPEG orientation, produce first/default frame |
| FromPixels(int width,int height,ReadOnlySpan&lt;byte&gt; rgba) | Copy exactly width×height×4 unpremultiplied RGBA bytes; mismatch throws ArgumentException |
| GetPixel(int x,int y) / SetPixel(int x,int y,Color) | Bounds checked; invalid pixel throws ArgumentOutOfRangeException; writes invalidate cached image snapshots |
| CopyPixels() | Independent tightly packed straight-alpha RGBA byte array |
| Clone() | Independent owned copy |
| Draw(Action&lt;DrawingContext&gt;) | Temporary offscreen surface over these pixels; expiring context; reentry/disposal in callback throws InvalidOperationException |
| Encode(BitmapFormat format=Png,int quality=90) | Owned encoded byte array; enum and quality 0–100 validated; unavailable encoder throws NotSupportedException |
| Save(string path,BitmapFormat=Png,int quality=90) | Write encoded bytes; extension does not select format |
| Save(Stream,BitmapFormat=Png,int quality=90) | Write without closing caller stream |
| Dispose() | Idempotent except forbidden while drawing; releases pixels and native snapshot |

Null stream/callback arguments throw ArgumentNullException. Malformed/unsupported/truncated input and limits raise InvalidDataException. Native codecs are tried first (Netpbm uses the portable P1–P6 implementation), then the portable fallback. Animated formats decode one image, not an animation object. BitmapFormat values: Png, Jpeg, WebP. BitmapInterpolation values: Nearest, Linear, Cubic (Mitchell). Draw-time access after resource disposal throws ObjectDisposedException.

## ImageLoadOptions

Sealed immutable record. MaximumEncodedBytes=64×1024×1024, MaximumPixels=64×1024×1024; both positive, validated when loading. The encoded-byte limit also bounds decompressed SVGZ and XML character count; MaximumPixels applies to raster decoding. SVG logical dimensions and embedded-resource processing follow renderer semantics, not a promise of bounded arbitrary SVG complexity. Keep input/resource ownership explicit.

## VectorImage

Sealed IDisposable immutable recording. `Parse(string,ImageLoadOptions?=null)` and `Load(string/Stream,ImageLoadOptions?=null)` parse static SVG/SVGZ. DTD resolution is prohibited, and renderer resource policy permits only same-document/data references. Null inputs throw; bad XML/non-SVG roots/missing positive dimensions raise InvalidDataException. Decoder-specific unsupported constructs may be omitted or represented by placeholders.

`Create(Rect viewBox,Action<DrawingContext>)` records Swealium drawing into a vector picture. Bounds must be finite, positive in extent. Callback context expires even on failure. Recordings retain native references needed for playback; later source/path edits do not change them.

ViewBox and Size are immutable metadata, readable after disposal. IsDisposed reports ownership state. `Rasterize(int width,int height,ImageStretch stretch=Uniform)` returns a caller-owned transparent Bitmap with centered scaling; invalid dimensions/stretch are rejected. Calling Rasterize or drawing after disposal throws ObjectDisposedException. Dispose releases the recording and any SVG owner, idempotently.

## VectorPath

Sealed IDisposable editable path. Constructor starts empty; FillRule=Winding. Fluent methods return this: MoveTo(Point), LineTo(Point), QuadraticTo(Point control,Point end), CubicTo(Point first,Point second,Point end), ArcTo(Rect oval,float startDegrees,float sweepDegrees,bool startNewFigure=false), Close(), AddRectangle(Rect), AddEllipse(Rect), Clear(). Points/angles must be finite, extents nonnegative. Arc angles are degrees and follow the backend's clockwise screen coordinate convention.

`Parse(string svgPathData)` supports SVG path syntax including relative coordinates/arcs; invalid data raises FormatException. Bounds includes the current path's control points and need not be the tight curve bounds; Contains(Point) tests according to FillRule (Winding/EvenOdd). Setting an invalid FillRule raises ArgumentOutOfRangeException. Drawing takes a disposable internal snapshot. Methods after Dispose throw ObjectDisposedException. PathLineCap: Flat/Round/Square; PathLineJoin: Miter/Round/Bevel.

## DrawingContext additions

| Method | Contract |
| --- | --- |
| DrawBitmap(Bitmap,Rect destination,Rect? source=null,float opacity=1,BitmapInterpolation=Linear,Rect? clip=null) | Scaled source region; crop must lie inside pixels; local clip optional |
| DrawBitmap(Bitmap,Point origin,float opacity=1,BitmapInterpolation=Linear) | Natural pixel size in logical drawing units |
| DrawVector(VectorImage,Rect destination,float opacity=1,Rect? clip=null) | Scale ViewBox into destination, honoring its origin; clip destination and optional local clip |
| FillPath(VectorPath,Color) | Snapshot and fill using its FillRule |
| StrokePath(VectorPath,Color,float width=1,PathLineCap=Flat,PathLineJoin=Miter) | Positive finite width, validated cap/join |

Opacity must be finite in [0,1]. Rects require finite positions/extents/end coordinates and nonnegative extents. Invalid enum/range values raise ArgumentOutOfRangeException. Null resources throw ArgumentNullException; disposed resources/expired contexts throw ObjectDisposedException. Empty geometry/zero opacity paint nothing. New backend extension methods are virtual and default to NotSupportedException for existing third-party DrawingContext subclasses. The supplied implementation supports all of them.

ImageStretch is shared by display widgets and Rasterize: None, Fill, Uniform, UniformToFill. See [Image](../widgets/image.md) and [VectorGraphics](../widgets/vector-graphics.md) for retained sizing, clipping, and alignment defaults.

## Bitmap transformations and Base64

`FromBase64(string, ImageLoadOptions? = null)` decodes raw Base64 or an image data URI with a base64 marker, applying encoded-byte and pixel limits. Malformed payloads raise InvalidDataException. `ToBase64(BitmapFormat = Png, int quality = 90)` and `ToDataUri(...)` encode with the same format/quality rules as Encode.

`Clear(Color = default)` mutates all pixels. The following return independent caller-owned Bitmaps: `Resize(int width, int height, BitmapInterpolation = Linear)`, `Crop(int x, int y, int width, int height)`, `Rotate(BitmapRotation)`, `Flip(BitmapFlip)`, and `WithOpacity(float)`. Dimensions must be positive, crop bounds must fit, and opacity must be finite in [0,1]. BitmapRotation: None, Clockwise90, Clockwise180, Clockwise270. BitmapFlip flags: None, Horizontal, Vertical, Both. Transformations preserve the source; rotations swap dimensions at 90/270 degrees.
