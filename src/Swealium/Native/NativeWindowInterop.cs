namespace Swealium.Native;

internal static class NativeWindowInterop
{
    internal static bool TryRead(uint properties, out NativeWindowHandle handle)
    {
        foreach (var (kind, property) in new (NativeWindowKind, string)[]
        {
            (NativeWindowKind.Win32, "SDL.window.win32.hwnd"),
            (NativeWindowKind.Cocoa, "SDL.window.cocoa.window"),
            (NativeWindowKind.UIKit, "SDL.window.uikit.window"),
            (NativeWindowKind.Android, "SDL.window.android.window")
        })
        {
            nint pointer = Sdl.SDL_GetPointerProperty(properties, property, 0);
            if (pointer != 0) { handle = new(kind, unchecked((nuint)pointer)); return true; }
        }
        long xid = Sdl.SDL_GetNumberProperty(properties, "SDL.window.x11.window", 0);
        if (xid > 0 && (ulong)xid <= (ulong)nuint.MaxValue)
        {
            handle = new(NativeWindowKind.X11, (nuint)xid,
                Sdl.SDL_GetPointerProperty(properties, "SDL.window.x11.display", 0));
            return true;
        }
        nint surface = Sdl.SDL_GetPointerProperty(properties, "SDL.window.wayland.surface", 0);
        if (surface != 0)
        {
            handle = new(NativeWindowKind.Wayland, unchecked((nuint)surface),
                Sdl.SDL_GetPointerProperty(properties, "SDL.window.wayland.display", 0));
            return true;
        }
        handle = default;
        return false;
    }
}
