# DrawingCanvas performance

[Documentation index](README.md) · [DrawingCanvas](widgets/drawing-canvas.md) · [Point-buffer contracts](api/point-buffers.md)

The large-data path uses one retained widget, contiguous prepared PointBuffer storage, and batch drawing. It does not create 100,000 widgets/paints. Equal-size Update reuses storage; unchanged geometry can be painted repeatedly without per-point managed allocations.

## Measured reference run

Measured locally on Windows x64, .NET 10.0.9, with an 800×450 drawing surface, three warmup frames and sixteen measured frames per case. These are illustrative local timings from a shared interactive machine, not guaranteed frame rates or cross-platform certification. The GPU case uses the SDL/OpenGL Skia backend and a synchronous GPU flush. Both cases include surface clearing and exclude buffer swap/compositor, the SDL texture/presentation path, data generation/projection, and initial automatic-reduction preparation. The application frame clock is separate.

| Dataset / drawing | CPU median / p95 | GPU median / p95 |
| --- | --- | --- |
| 100K scatter, all points, square 1 px, AA off | 2.52 / 3.53 ms | 2.69 / 3.33 ms |
| 100K ordered polyline, automatic detail, width 1, AA on | 2.76 / 4.15 ms | 4.61 / 6.51 ms |
| 100K ordered polyline, exact detail, AA on | 109.09 / 118.72 ms | 254.73 / 275.25 ms |
| 1M scatter, all points, square 1 px, AA off | 28.28 / 36.41 ms | 25.34 / 39.89 ms |
| 1M ordered polyline, automatic detail, AA on | 3.89 / 6.02 ms | 8.27 / 11.22 ms |

The 100K and 1M ordered curves reduced to 1,820 and 2,864 display samples at this width. Explicit reduction preparation took 1.86 ms and 10.34 ms in the CPU reference run; preparation is cached across unchanged frames. Rendering allocated approximately 248–260 managed bytes per frame in these cases, independent of input count. The test suite checks bounded managed allocations for prepared 100K scatter and automatically reduced plots; it intentionally does not impose machine-dependent timing assertions.

The exact comparison matters: batching removes managed overhead but cannot eliminate the raster/GPU cost of drawing every anti-aliased segment. DrawingCanvas therefore defaults to automatic peak-envelope reduction for dense X-ordered polylines. Exact mode is available when preserving every input segment is necessary. Scatter samples, closed paths, unordered paths, and line-pair batches retain all geometry.

## Reproduce

```sh
dotnet run --project tests/Swealium.Tests -c Release -- --canvas-benchmark
dotnet run --project tests/Swealium.Tests -c Release -- --canvas-benchmark-gpu
```

The CPU benchmark is offscreen and requires no display. The GPU benchmark requires a working graphical SDL/OpenGL backend and fails if unavailable. It flushes synchronously so measurements include GPU completion rather than only command submission. It excludes swaps because Windows compositor/hidden-window pacing dominated earlier swap-inclusive measurements at roughly 33 ms even for small reduced series. Actual application presentation can therefore be slower than the isolated drawing numbers.

## Practical use

- Prepare PointBuffer outside Paint. Project into widget coordinates only when data/viewport changes.
- Keep Automatic detail for dense, ordered plots; use Exact deliberately for geometry-sensitive work.
- Use one batch per color/size/style series. Large translucent markers and repeated full-area overdraw remain expensive.
- Cache static scenes with existing VectorImage.Create when appropriate; DrawingCanvas itself is immediate and repaints each visible frame.
- Compute data off-thread if useful, then update/swap buffers on the UI thread. Rendering and mutation of the same buffer are serialized.
- Pointer input is per canvas. Use a sorted lookup or spatial index for sample picking rather than a widget/hit target per datum.

Run `dotnet run --project samples/Swealium.Sample -- --canvas-demo` for live 100K/1M plot/scatter and automatic/exact controls. GPU behavior still requires target OS/driver verification; measurements here do not certify Linux/macOS/mobile performance.
