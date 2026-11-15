# Rendering settings and GPU acceleration

[Documentation index](README.md) · [Drawing](drawing.md) · [Window contracts](api/windowing.md)

```csharp
SwealiumApplication.AntiAliasing = true;
SwealiumApplication.HardwareAcceleration = true;
SwealiumApplication.Run(new WindowOptions { Title = "Accelerated drawing" }, window =>
{
    Console.WriteLine(window.IsHardwareAccelerated);
    Console.WriteLine(window.HardwareAccelerationFallbackReason);
});
```

## Application defaults

Both settings default to true. Set HardwareAcceleration before Run; changing it during an active session throws InvalidOperationException, even when assigning the current value. It selects the policy for newly constructed windows, including externally hosted windows. Existing windows keep their backend. AntiAliasing can change on the UI thread between frames and supplies the initial state of every new DrawingContext, including Bitmap.Draw and VectorImage.Create. Settings persist between application sessions.

A context can override edge smoothing for subsequent drawing:

```csharp
public override void Draw(DrawingContext drawing)
{
    drawing.AntiAliasing = false;
    drawing.DrawLine(new Point(0, 10), new Point(100, 10), Color.White);
    drawing.AntiAliasing = true;
    drawing.FillEllipse(new Rect(10, 20, 40, 40), Color.White);
}
```

Overrides affect shapes, paths, bitmap edge coverage, and shaped text glyphs/decorations. They stay within that context; siblings and overlays start from the application default. Accessing an expired native-backed context property throws ObjectDisposedException. Framework widget clips and per-side strokes use the application default. Anti-aliasing does not alter layout, input geometry, opacity, or bitmap interpolation. Previously recorded vector/SVG paints and decoded pixels retain their existing appearance; changing the context flag does not rewrite them. It is not an MSAA sample-count control.

## Hardware path and fallback

Enabled requests GPU-backed Skia rendering through SDL-managed OpenGL contexts: desktop core GL 3.3 (macOS 3.2), or ES 3.0 for mobile native hosts. Each window owns a separate context/device and wraps its physical window framebuffer. Drawing and compositing execute through Skia's GPU backend; frames are submitted and swapped without a full-frame CPU pixel upload. Multi-window drawing switches/restores native contexts. Resize recreates the framebuffer wrapper; disposal releases Skia resources before destroying the native context.

Unavailable accelerated visuals, context creation, or Skia framebuffer initialization trigger automatic raster fallback before the framework window is registered. IsHardwareAccelerated reports the selected Skia backend; HardwareAccelerationFallbackReason contains an initialization diagnostic, or null when GPU initialization succeeded or acceleration was disabled. Driver implementations ultimately determine physical device execution. Setting HardwareAcceleration=false explicitly selects CPU Skia plus SDL's software presentation renderer.

Fallback handles initialization failure. Later device loss/presentation errors propagate through the normal application exception/cleanup path; runtime window replacement and device recovery are not implemented. No Vulkan, Direct3D, or Metal backend is supplied. OpenGL support is deprecated on Apple platforms and depends on OS/driver availability. Mobile still requires a native lifecycle host; this setting does not supply one.

Bitmap drawing, image decoding, text layout/shaping, and vector recording remain CPU operations. Image/vector playback can use the window GPU backend. Resource ownership and frame-scoped DrawingContext rules are identical on both paths.

## Verification

The regular test suite checks aliased/smoothed pixels and glyphs, context isolation/lifetime, forced software, and dummy-driver fallback. On a graphical desktop run:

```sh
dotnet run --project tests/Swealium.Tests -c Release -- --gpu-rendering
```

This explicit check requires GPU-backed Skia and fails on fallback. It exercises two windows, resizing, disposal, and framebuffer pixel readback/orientation. Windows has been checked locally; Linux, macOS, Android, and iOS GPU behavior still needs platform runtime verification. Headless dummy-driver tests validate fallback, not GPU execution.

Backend integration follows [SDL GL context creation](https://wiki.libsdl.org/SDL3/SDL_GL_CreateContext), [SDL GL attributes](https://wiki.libsdl.org/SDL3/SDL_GLAttr), and [Skia GPU surface wrapping](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.sksurface).
