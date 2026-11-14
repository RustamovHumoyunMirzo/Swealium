using System.Runtime.InteropServices;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.Native;
namespace Swealium;

public partial class SwealiumWindow
{
    private Color background;
    private bool observedVisible;
    private WindowState observedState;
    public event Action<bool>? FocusChanged;
    public event Action<bool>? MouseFocusChanged;
    public event Action<bool>? VisibilityChanged;
    public event Action<WindowPosition>? Moved;
    public event Action<Size>? Resized;
    public event Action<WindowSize>? PixelSizeChanged;
    public event Action<WindowState>? StateChanged;
    public event Action<float>? DisplayScaleChanged;
    public event Action<uint>? DisplayChanged;
    public event Action? Exposed;
    /// <summary>Focus observed from the native event stream.</summary>
    public bool HasFocus { get; private set; }
    public bool HasMouseFocus { get; private set; }

    public string Title
    {
        get { VerifyAccess(); return Marshal.PtrToStringUTF8(Sdl.SDL_GetWindowTitle(window)) ?? string.Empty; }
        set
        {
            VerifyAccess();
            ArgumentNullException.ThrowIfNull(value);
            if (value.Contains('\0')) throw new ArgumentException("Title cannot contain a null character.", nameof(value));
            Sdl.Check(Sdl.SDL_SetWindowTitle(window, value));
        }
    }
    public Color Background { get { VerifyAccess(); return background; } set { VerifyAccess(); background = value; } }
    public WindowPosition Position
    {
        get { VerifyAccess(); Sdl.Check(Sdl.SDL_GetWindowPosition(window, out int x, out int y)); return new(x, y); }
        set { VerifyAccess(); Sdl.Check(Sdl.SDL_SetWindowPosition(window, value.X, value.Y)); }
    }
    /// <summary>Logical client size. Setters request a native size; the OS may adjust it.</summary>
    public WindowSize Size
    {
        get { VerifyAccess(); Sdl.Check(Sdl.SDL_GetWindowSize(window, out int w, out int h)); return new(w, h); }
        set { VerifyAccess(); ValidateWindowSize(value); Sdl.Check(Sdl.SDL_SetWindowSize(window, value.Width, value.Height)); UpdateViewport(); }
    }
    public WindowSize PixelSize
    {
        get { VerifyAccess(); Sdl.Check(Sdl.SDL_GetWindowSizeInPixels(window, out int w, out int h)); return new(w, h); }
    }
    public WindowSize MinimumSize
    {
        get { VerifyAccess(); Sdl.Check(Sdl.SDL_GetWindowMinimumSize(window, out int w, out int h)); return new(w, h); }
        set
        {
            VerifyAccess(); ValidateWindowSize(value, true); ValidateLimits(value, MaximumSize);
            Sdl.Check(Sdl.SDL_SetWindowMinimumSize(window, value.Width, value.Height)); UpdateViewport();
        }
    }
    /// <summary>Zero on an axis removes the upper size limit.</summary>
    public WindowSize MaximumSize
    {
        get { VerifyAccess(); Sdl.Check(Sdl.SDL_GetWindowMaximumSize(window, out int w, out int h)); return new(w, h); }
        set
        {
            VerifyAccess(); ValidateWindowSize(value, true); ValidateLimits(MinimumSize, value);
            Sdl.Check(Sdl.SDL_SetWindowMaximumSize(window, value.Width, value.Height)); UpdateViewport();
        }
    }
    public WindowFlags Flags
    {
        get
        {
            VerifyAccess();
            ulong native = Sdl.SDL_GetWindowFlags(window);
            WindowFlags result = WindowFlags.None;
            if ((native & 0x8UL) == 0) result |= WindowFlags.Visible;
            if ((native & 0x20UL) != 0) result |= WindowFlags.Resizable;
            if ((native & 0x10UL) != 0) result |= WindowFlags.Borderless;
            if ((native & 0x10000UL) != 0) result |= WindowFlags.AlwaysOnTop;
            if ((native & 0x40UL) != 0) result |= WindowFlags.Minimized;
            if ((native & 0x80UL) != 0) result |= WindowFlags.Maximized;
            if ((native & 0x1UL) != 0) result |= WindowFlags.Fullscreen;
            if ((native & 0x200UL) != 0) result |= WindowFlags.Focused;
            if ((native & 0x400UL) != 0) result |= WindowFlags.MouseFocused;
            if ((native & 0x100UL) != 0) result |= WindowFlags.MouseGrabbed;
            if ((native & 0x100000UL) != 0) result |= WindowFlags.KeyboardGrabbed;
            if ((native & 0x1000UL) != 0) result |= WindowFlags.Modal;
            if ((native & 0x2000UL) != 0) result |= WindowFlags.HighPixelDensity;
            if ((native & 0x20000UL) != 0) result |= WindowFlags.Utility;
            if ((native & 0x80000000UL) == 0) result |= WindowFlags.Focusable;
            if ((native & 0x4UL) != 0) result |= WindowFlags.Occluded;
            return result;
        }
    }
    public bool IsVisible { get => Flags.HasFlag(WindowFlags.Visible); set { if (value) Show(); else Hide(); } }
    public bool IsMinimized => Flags.HasFlag(WindowFlags.Minimized);
    public bool IsMaximized => Flags.HasFlag(WindowFlags.Maximized);
    public bool IsFullscreen
    {
        get => Flags.HasFlag(WindowFlags.Fullscreen);
        set { VerifyAccess(); Sdl.Check(Sdl.SDL_SetWindowFullscreen(window, value)); }
    }
    public bool Resizable
    {
        get => Flags.HasFlag(WindowFlags.Resizable);
        set { VerifyAccess(); Sdl.Check(Sdl.SDL_SetWindowResizable(window, value)); }
    }
    public bool Borderless
    {
        get => Flags.HasFlag(WindowFlags.Borderless);
        set { VerifyAccess(); Sdl.Check(Sdl.SDL_SetWindowBordered(window, !value)); }
    }
    public bool AlwaysOnTop
    {
        get => Flags.HasFlag(WindowFlags.AlwaysOnTop);
        set { VerifyAccess(); Sdl.Check(Sdl.SDL_SetWindowAlwaysOnTop(window, value)); }
    }
    public bool Focusable
    {
        get => Flags.HasFlag(WindowFlags.Focusable);
        set { VerifyAccess(); Sdl.Check(Sdl.SDL_SetWindowFocusable(window, value)); if (!value) Input.Cancel(); }
    }
    public bool MouseGrabbed
    {
        get => Flags.HasFlag(WindowFlags.MouseGrabbed);
        set { VerifyAccess(); Sdl.Check(Sdl.SDL_SetWindowMouseGrab(window, value)); }
    }
    public bool KeyboardGrabbed
    {
        get => Flags.HasFlag(WindowFlags.KeyboardGrabbed);
        set { VerifyAccess(); Sdl.Check(Sdl.SDL_SetWindowKeyboardGrab(window, value)); }
    }
    public bool ShowInTaskbar => !Flags.HasFlag(WindowFlags.Utility);
    public bool HighPixelDensity => Flags.HasFlag(WindowFlags.HighPixelDensity);
    public bool IsOccluded => Flags.HasFlag(WindowFlags.Occluded);
    public float Opacity
    {
        get { VerifyAccess(); return Sdl.SDL_GetWindowOpacity(window); }
        set
        {
            VerifyAccess();
            if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            Sdl.Check(Sdl.SDL_SetWindowOpacity(window, value));
        }
    }
    public float DisplayScale
    {
        get { VerifyAccess(); float value = Sdl.SDL_GetWindowDisplayScale(window); Sdl.Check(value > 0); return value; }
    }
    public float PixelDensity
    {
        get { VerifyAccess(); float value = Sdl.SDL_GetWindowPixelDensity(window); Sdl.Check(value > 0); return value; }
    }
    public WindowState State
    {
        get
        {
            var flags = Flags;
            return flags.HasFlag(WindowFlags.Minimized) ? WindowState.Minimized
                : flags.HasFlag(WindowFlags.Fullscreen) ? WindowState.Fullscreen
                : flags.HasFlag(WindowFlags.Maximized) ? WindowState.Maximized : WindowState.Normal;
        }
        set
        {
            VerifyAccess();
            switch (value)
            {
                case WindowState.Normal: Restore(); break;
                case WindowState.Minimized: Minimize(); break;
                case WindowState.Maximized: Maximize(); break;
                case WindowState.Fullscreen: IsFullscreen = true; break;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }
    public void Show() { VerifyAccess(); Sdl.Check(Sdl.SDL_ShowWindow(window)); }
    public void Hide() { VerifyAccess(); Sdl.Check(Sdl.SDL_HideWindow(window)); Input.Cancel(); }
    /// <summary>Request OS activation. The window manager may deny focus stealing.</summary>
    public void Activate()
    {
        VerifyAccess();
        if (IsInputBlocked) throw new InvalidOperationException("Activate the active modal dialog instead of its blocked owner.");
        Show();
        Sdl.Check(Sdl.SDL_RaiseWindow(window));
    }
    public void Center() { VerifyAccess(); Sdl.Check(Sdl.SDL_SetWindowPosition(window, 0x2fff0000, 0x2fff0000)); }
    public void Minimize() { VerifyAccess(); Sdl.Check(Sdl.SDL_MinimizeWindow(window)); }
    public void Maximize() { VerifyAccess(); Sdl.Check(Sdl.SDL_MaximizeWindow(window)); }
    public void Restore()
    {
        VerifyAccess();
        if (IsFullscreen) IsFullscreen = false;
        Sdl.Check(Sdl.SDL_RestoreWindow(window));
    }
    public void RequestAttention(WindowAttention attention = WindowAttention.Briefly)
    {
        VerifyAccess();
        if (!Enum.IsDefined(attention)) throw new ArgumentOutOfRangeException(nameof(attention));
        Sdl.Check(Sdl.SDL_FlashWindow(window, (int)attention));
    }
    private static void ValidateWindowSize(WindowSize size, bool allowZero = false)
    {
        if (size.Width < (allowZero ? 0 : 1) || size.Height < (allowZero ? 0 : 1))
            throw new ArgumentOutOfRangeException(nameof(size));
    }
    private static void ValidateLimits(WindowSize minimum, WindowSize maximum)
    {
        if ((maximum.Width != 0 && maximum.Width < minimum.Width) || (maximum.Height != 0 && maximum.Height < minimum.Height))
            throw new ArgumentException("Maximum size must be zero (unbounded) or at least the minimum on each axis.");
    }
    private void InitializeWindowObservations()
    {
        var flags = Flags;
        HasFocus = flags.HasFlag(WindowFlags.Focused);
        HasMouseFocus = flags.HasFlag(WindowFlags.MouseFocused);
        observedVisible = IsVisible;
        observedState = State;
    }
    private void HandleWindowEvent(Sdl.Event e)
    {
        switch (e.Type)
        {
            case 0x202: SetObservedVisibility(true); break;
            case 0x203: Input.Cancel(); SetObservedVisibility(false); break;
            case 0x204: OnExposed(); break;
            case 0x205: OnMoved(new(e.Data1, e.Data2)); break;
            case 0x206: UpdateViewport(); OnResized(new(e.Data1, e.Data2)); break;
            case 0x207: UpdateViewport(); OnPixelSizeChanged(new(e.Data1, e.Data2)); break;
            case 0x209: Input.Cancel(); SetObservedState(WindowState.Minimized); break;
            case 0x20a: SetObservedState(WindowState.Maximized); break;
            case 0x20b: SetObservedState(IsFullscreen ? WindowState.Fullscreen : WindowState.Normal); break;
            case 0x20c:
                if (!HasMouseFocus) { HasMouseFocus = true; OnMouseFocusChanged(); }
                break;
            case 0x20d:
                Input.ProcessPointer(new(PointerEventKind.Leave, default));
                if (HasMouseFocus) { HasMouseFocus = false; OnMouseFocusChanged(); }
                break;
            case 0x20e:
                if (!HasFocus) { HasFocus = true; OnFocusChanged(); }
                break;
            case 0x20f:
                Input.Cancel();
                if (HasFocus) { HasFocus = false; OnFocusChanged(); }
                break;
            case 0x213: OnDisplayChanged(unchecked((uint)e.Data1)); break;
            case 0x214: UpdateViewport(); OnDisplayScaleChanged(DisplayScale); break;
            case 0x217: SetObservedState(WindowState.Fullscreen); break;
            case 0x218: SetObservedState(WindowState.Normal); break;
        }
    }
    private void SetObservedVisibility(bool value)
    {
        if (observedVisible == value) return;
        observedVisible = value;
        OnVisibilityChanged(value);
    }
    private void SetObservedState(WindowState value)
    {
        if (observedState == value) return;
        observedState = value;
        OnStateChanged(value);
    }
    protected virtual void OnFocusChanged() => FocusChanged?.Invoke(HasFocus);
    protected virtual void OnMouseFocusChanged() => MouseFocusChanged?.Invoke(HasMouseFocus);
    protected virtual void OnVisibilityChanged(bool visible) => VisibilityChanged?.Invoke(visible);
    protected virtual void OnMoved(WindowPosition position) => Moved?.Invoke(position);
    protected virtual void OnResized(Size size) => Resized?.Invoke(size);
    protected virtual void OnPixelSizeChanged(WindowSize size) => PixelSizeChanged?.Invoke(size);
    protected virtual void OnStateChanged(WindowState state) => StateChanged?.Invoke(state);
    protected virtual void OnDisplayChanged(uint displayId) => DisplayChanged?.Invoke(displayId);
    protected virtual void OnDisplayScaleChanged(float scale) => DisplayScaleChanged?.Invoke(scale);
    protected virtual void OnExposed() => Exposed?.Invoke();
}

