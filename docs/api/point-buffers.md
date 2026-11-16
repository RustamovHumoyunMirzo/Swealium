# Point buffers and batch drawing

[Documentation index](../README.md) · [DrawingCanvas](../widgets/drawing-canvas.md) · [Complete signatures](reference.md)

Namespace Swealium.Graphics. Public APIs expose Swealium geometry and no backend point/canvas classes.

## PointBuffer

Sealed managed resource; no IDisposable requirement. Constructor PointBuffer(ReadOnlySpan&lt;Point&gt;) validates/copies finite coordinates. Arrays implicitly convert to the span input. Count, Bounds, IsXOrdered, Version, and indexer this[int] are read-only. Bounds encloses point centers, not marker/stroke extents; empty buffers have default Rect, and degenerate axes have zero extent. IsXOrdered means nondecreasing X; duplicates are allowed. Invalid indices throw ArgumentOutOfRangeException.

Update(ReadOnlySpan&lt;Point&gt;) validates before changing data, reuses its native-compatible managed array when count matches, replaces it otherwise, increments Version, and clears cached reductions. Invalid finite/range input throws ArgumentOutOfRangeException and leaves the previous buffer intact. Bounds that cannot be represented as finite float dimensions are rejected. CopyPoints() returns a caller-owned Point[] copy. Serialize updates and rendering on the UI thread; do not mutate the buffer concurrently with drawing/recording.

Project(Rect destination, Rect? source=null) returns a new buffer mapping source (default Bounds) into destination. Extents must be finite nonnegative, including finite end coordinates. Zero source extent maps that axis to the destination center. Projection does not clip outside points. Coordinates beyond the drawable float range throw ArgumentOutOfRangeException. Project when data/viewport changes, not on every unchanged frame.

ReduceForWidth(int columns, Rect? viewBox=null) requires columns>0 and IsXOrdered=true (invalid columns throw ArgumentOutOfRangeException; unordered data InvalidOperationException). It uses viewBox's X interval (default Bounds), ignores its Y interval for sample selection, and retains first/last/minimum-Y/maximum-Y per column in source order. Duplicate candidates are removed. One neighbor before/after the interval preserves connecting segments. The result has at most four samples per column plus two neighbors. It creates an independent PointBuffer and does not change source indices/data. Rect validation follows Project. This is a peak-preserving display envelope, not exact geometry or an analytical downsample.

## DrawingContext extensions

| Method | Contract |
| --- | --- |
| DrawPoints(PointBuffer points, Color color, float size=1, PointShape shape=Square, Rect? clip=null) | Batched square/round markers centered on each point |
| DrawLines(PointBuffer points, Color color, float width=1, PathLineCap cap=Flat, Rect? clip=null) | Independent pairs; odd point count throws ArgumentException |
| DrawPolyline(PointBuffer points, Color color, float width=1, bool closed=false, PathLineCap cap=Flat, Rect? clip=null) | Connected segments, optionally connecting last to first for count>2 |
| PolylineDetail | virtual DrawingDetail get/set; Exact by default; native implementation validates and checks context lifetime |

PointShape: Square, Round. DrawingDetail: Exact, Automatic. Width/size must be finite nonnegative; zero requests a hairline. Enums must be defined. Null buffers throw ArgumentNullException; invalid widths/enums/rects throw ArgumentOutOfRangeException. Empty batches and polylines with fewer than two points draw nothing after validation. Line caps reuse PathLineCap; polyline joins are segment joins, not the joined-path semantics of VectorPath. AntiAliasing and solid/gradient Color use the existing context settings; gradient coordinates cover buffer bounds. Optional clip is local logical geometry.

Methods are virtual and default to NotSupportedException for external DrawingContext subclasses that do not implement batches. The supplied backend submits one point/line/polyline batch plus one closing segment when requested. Recording copies draw geometry into the native recording; later PointBuffer.Update does not change an existing VectorImage. All methods require a live context; retained contexts throw ObjectDisposedException.

Automatic mode reduces only dense open X-ordered polylines using visible local X and physical scale, with a 32,768-column maximum. Up to four reductions are cached in each buffer by view/width, invalidated on Update. It does not reduce scatter/paired lines, closed or unordered polylines. DrawingCanvas defaults to Automatic and restores the caller's policy; other contexts remain Exact unless explicitly changed. Exact mode preserves input segments and can be much slower for dense plots.
