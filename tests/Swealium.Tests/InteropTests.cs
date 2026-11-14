using Swealium;
using Swealium.Native;

internal static class InteropTests
{
    internal static void HandleMapping()
    {
        foreach (var (kind, key) in new (NativeWindowKind, string)[]
        {
            (NativeWindowKind.Win32, "SDL.window.win32.hwnd"),
            (NativeWindowKind.Cocoa, "SDL.window.cocoa.window"),
            (NativeWindowKind.UIKit, "SDL.window.uikit.window"),
            (NativeWindowKind.Android, "SDL.window.android.window"),
            (NativeWindowKind.Wayland, "SDL.window.wayland.surface"),
            (NativeWindowKind.X11, "SDL.window.x11.window")
        })
        {
            uint properties = Sdl.SDL_CreateProperties();
            Sdl.Check(properties != 0);
            try
            {
                if (kind == NativeWindowKind.X11) Sdl.Check(Sdl.SDL_SetNumberProperty(properties, key, 12345));
                else Sdl.Check(Sdl.SDL_SetPointerProperty(properties, key, 12345));
                if (kind is NativeWindowKind.X11 or NativeWindowKind.Wayland)
                    Sdl.Check(Sdl.SDL_SetPointerProperty(properties,
                        kind == NativeWindowKind.X11 ? "SDL.window.x11.display" : "SDL.window.wayland.display", 6789));
                if (!NativeWindowInterop.TryRead(properties, out var result) || result.Kind != kind ||
                    result.Value != 12345 || !result.IsValid ||
                    (kind is NativeWindowKind.X11 or NativeWindowKind.Wayland && result.Display != 6789))
                    throw new InvalidOperationException($"Invalid native mapping for {kind}");
            }
            finally { Sdl.SDL_DestroyProperties(properties); }
        }
    }
}
