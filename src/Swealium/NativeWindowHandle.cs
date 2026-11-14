namespace Swealium;

/// <summary>The native window system represented by a borrowed platform handle.</summary>
public enum NativeWindowKind { None, Win32, Cocoa, UIKit, Android, X11, Wayland }

/// <summary>A borrowed OS window pointer or identifier. Never destroy or retain it beyond its window's lifetime.</summary>
/// <param name="Kind">Native system that defines Value.</param>
/// <param name="Value">HWND, NSWindow*, UIWindow*, ANativeWindow*, X11 Window ID, or wl_surface*.</param>
/// <param name="Display">X11 Display* or Wayland wl_display*, when available; otherwise zero.</param>
public readonly record struct NativeWindowHandle(NativeWindowKind Kind, nuint Value, nint Display = 0)
{
    public bool IsValid => Kind != NativeWindowKind.None && Value != 0;
}
