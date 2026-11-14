namespace Swealium;

public readonly record struct WindowPosition(int X, int Y);
public readonly record struct WindowSize(int Width, int Height);
public enum WindowState { Normal, Minimized, Maximized, Fullscreen }
public enum WindowAttention { Cancel, Briefly, UntilFocused }

/// <summary>A read-only snapshot of observed native window flags, mapped to Swealium values.</summary>
[Flags]
public enum WindowFlags
{
    None = 0, Visible = 1, Resizable = 2, Borderless = 4, AlwaysOnTop = 8,
    Minimized = 16, Maximized = 32, Fullscreen = 64, Focused = 128,
    MouseFocused = 256, MouseGrabbed = 512, KeyboardGrabbed = 1024,
    Modal = 2048, HighPixelDensity = 4096, Utility = 8192, Focusable = 16384, Occluded = 32768
}
