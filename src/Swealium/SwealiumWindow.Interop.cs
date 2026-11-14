using Swealium.Native;
namespace Swealium;

public partial class SwealiumWindow
{
    /// <summary>Query a borrowed OS window handle using one API on every supported window system.</summary>
    /// <remarks>Call on the UI thread. False means this backend exposes no supported native window handle.
    /// Requery before each interop operation; native objects can change during window lifecycle transitions.</remarks>
    public bool TryGetNativeWindowHandle(out NativeWindowHandle handle)
    {
        VerifyAccess();
        uint properties = Sdl.SDL_GetWindowProperties(window);
        Sdl.Check(properties != 0);
        return NativeWindowInterop.TryRead(properties, out handle);
    }

    /// <summary>Get a borrowed native window handle or throw if the active backend does not expose one.</summary>
    public NativeWindowHandle GetNativeWindowHandle() => TryGetNativeWindowHandle(out var handle) ? handle
        : throw new PlatformNotSupportedException("The active window backend does not expose a supported OS window handle.");
}
