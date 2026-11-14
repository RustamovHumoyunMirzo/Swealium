using Swealium;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.Native;
using Swealium.UI;

internal static class WindowControlTests
{
    internal static bool RequireNativeHandle;
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool IsWindow(nint handle);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }
    internal static void Controls()
    {
        SwealiumWindow? saved = null;
        SwealiumApplication.Run(new()
        {
            Title = "Initial 世界", Width = 160, Height = 100, Position = new(40, 60),
            MinimumSize = new(80, 60), MaximumSize = new(400, 300), Visible = false
        }, root =>
        {
            saved = root;
            Check(root.Id != 0, "Portable routing ID");
            if (root.TryGetNativeWindowHandle(out var native))
            {
                Check(native.IsValid && root.GetNativeWindowHandle() == native, "Native handle queries agree");
                if (OperatingSystem.IsWindows()) Check(native.Kind == NativeWindowKind.Win32, "Windows native kind");
                if (RequireNativeHandle && OperatingSystem.IsWindows())
                    Check(IsWindow(unchecked((nint)native.Value)), "Returned value is an actual OS HWND");
            }
            else
            {
                Check(!RequireNativeHandle, "The real desktop check requires an available native handle");
                Check(native == default, "Unavailable native handle clears output");
                Throws<PlatformNotSupportedException>(() => root.GetNativeWindowHandle());
            }
            Check(root.Title == "Initial 世界" && !root.IsVisible, "Initial options and UTF-8 getter");
            Check(root.MinimumSize == new WindowSize(80, 60) && root.MaximumSize == new WindowSize(400, 300), "Initial constraints");
            Check(root.Position == new WindowPosition(40, 60), "Initial position");
            Check(root.HighPixelDensity && root.ShowInTaskbar && root.Focusable, "Default creation flags");
            int hiddenPaints = 0;
            root.Paint += (_, _) => hiddenPaints++;
            root.Render();
            Check(hiddenPaints == 0, "Hidden window skips drawing");
            root.Title = "Updated 👋";
            Check(root.Title == "Updated 👋", "Mutable title");
            root.Content = new Container();
            root.Size = new(220, 140);
            Check(root.Size == new WindowSize(220, 140) && root.ClientSize == new Size(220, 140), "Native logical size and cached viewport agree");
            Check(root.Content.ActualWidth == 220 && root.Content.ActualHeight == 140, "Size change relayouts content");
            root.Position = new(-10, 25);
            Check(root.Position == new WindowPosition(-10, 25), "Position round trip");
            Check(root.PixelSize.Width > 0 && root.PixelDensity > 0 && root.DisplayScale > 0, "Physical dimensions and scale");
            Capability(() => root.Resizable = false, () => Check(!root.Resizable, "Resizable round trip"));
            Capability(() => root.Borderless = true, () => Check(root.Borderless, "Borderless round trip"));
            Capability(() => root.AlwaysOnTop = true, () => Check(root.AlwaysOnTop, "Topmost round trip"));
            Check(root.Flags.HasFlag(WindowFlags.Visible) == root.IsVisible, "Flags use Swealium values");
            Capability(() => root.Resizable = true, () => Check(root.Resizable, "Resizable restore"));
            Capability(() => root.Borderless = false, () => Check(!root.Borderless, "Borderless restore"));
            Capability(() => root.AlwaysOnTop = false, () => Check(!root.AlwaysOnTop, "Topmost restore"));
            root.MinimumSize = new(90, 70);
            root.MaximumSize = new(350, 250);
            Check(root.MinimumSize == new WindowSize(90, 70) && root.MaximumSize == new WindowSize(350, 250), "Constraint setters");
            root.MaximumSize = default;
            Check(root.MaximumSize == default, "Zero removes size cap");
            root.Background = new Color(80, 20, 30);
            Check(root.Background == new Color(80, 20, 30), "Mutable background");
            Throws<ArgumentOutOfRangeException>(() => root.Size = new(0, 100));
            Throws<ArgumentOutOfRangeException>(() => root.MinimumSize = new(-1, 0));
            Throws<ArgumentException>(() => root.MaximumSize = new(1, 1));
            Throws<ArgumentOutOfRangeException>(() => root.Opacity = float.NaN);
            Throws<ArgumentOutOfRangeException>(() => root.Opacity = 2);
            Throws<ArgumentOutOfRangeException>(() => root.State = (WindowState)999);
            Throws<ArgumentOutOfRangeException>(() => root.RequestAttention((WindowAttention)999));
            Throws<ArgumentException>(() => root.Title = "bad\0title");
            Task.Run(() => Throws<InvalidOperationException>(() => _ = root.Title)).GetAwaiter().GetResult();
            root.Show();
            Check(root.IsVisible, "Show changes native visibility");
            root.Hide();
            Check(!root.IsVisible, "Hide changes native visibility");
            root.Show();
            Capability(root.Minimize);
            Capability(root.Maximize);
            Capability(root.Restore);
            Capability(() => root.IsFullscreen = true);
            Capability(() => root.IsFullscreen = false);
            Capability(root.Restore);
            Capability(root.Center);
            Capability(() => root.Opacity = 0.8f);
            Capability(() => root.Opacity = 1);
            Capability(() => root.RequestAttention(WindowAttention.Cancel));
            root.Paint += (_, _) => root.RequestClose();
        });
        Throws<ObjectDisposedException>(() => _ = saved!.Title);
        Throws<ObjectDisposedException>(() => saved!.TryGetNativeWindowHandle(out _));
        Throws<ObjectDisposedException>(() => saved!.Show());
        Throws<ObjectDisposedException>(() => saved!.Size = new(10, 10));
        Throws<ArgumentException>(() => SwealiumApplication.Run(new() { MinimumSize = new(100, 100), MaximumSize = new(10, 10) }));
        Throws<ArgumentException>(() => SwealiumApplication.Run(new() { Title = "bad\0title" }));
    }
    private static void Capability(Action request, Action? verify = null)
    {
        try { request(); }
        catch (InvalidOperationException error) when (error.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
        {
            // The dummy video backend deliberately has no window manager. Verify failures surface to callers.
            return;
        }
        verify?.Invoke();
    }
    internal static void Events()
    {
        SwealiumApplication.Run(new() { Width = 120, Height = 80 }, root =>
        {
            var target = new ObservedWindow(new() { Width = 160, Height = 120, Visible = false });
            var button = new Button();
            target.Content = button;
            target.Show();
            List<bool> focus = [], mouse = [], visibility = [];
            List<WindowState> states = [];
            WindowPosition moved = default;
            Size resized = default;
            WindowSize pixels = default;
            float scale = 0;
            uint display = 0;
            int exposed = 0;
            target.FocusChanged += hasFocus =>
            {
                Check(target.HasFocus == hasFocus, "Focus state updated before notification");
                if (!hasFocus) Check(target.Input.FocusedWidget is null && target.Input.CapturedWidget is null, "Focus loss cancels input before callbacks");
                focus.Add(hasFocus);
            };
            target.MouseFocusChanged += value => mouse.Add(value);
            target.VisibilityChanged += value => visibility.Add(value);
            target.StateChanged += value => states.Add(value);
            target.Moved += value => moved = value;
            target.Resized += value => resized = value;
            target.PixelSizeChanged += value => pixels = value;
            target.DisplayScaleChanged += value => scale = value;
            target.DisplayChanged += value => display = value;
            target.Exposed += () => exposed++;
            void Send(uint type, int first = 0, int second = 0) =>
                target.HandleSdlEvent(new() { Type = type, WindowId = target.Id, Data1 = first, Data2 = second });
            Send(0x20f);
            focus.Clear(); target.FocusHooks = 0;
            Send(0x20e); Send(0x20e);
            Check(focus.SequenceEqual([true]) && target.FocusHooks == 1, "Focus event and overridable hook run once per change");
            target.Input.ProcessPointer(new(PointerEventKind.Down, new(20, 20), PointerButton.Primary));
            Check(button.IsFocused && button.IsPressed, "Focused interaction begins");
            Send(0x20f);
            Check(focus.SequenceEqual([true, false]) && !button.IsPressed, "Focus loss event and cancellation");
            Send(0x20d); mouse.Clear();
            Send(0x20c); Send(0x20c); Send(0x20d);
            Check(mouse.SequenceEqual([true, false]), "Pointer focus notifications");
            Send(0x203); visibility.Clear();
            Send(0x202); Send(0x202); Send(0x203);
            Check(visibility.SequenceEqual([true, false]), "Visibility transition notifications");
            Send(0x205, -15, 42);
            Check(moved == new WindowPosition(-15, 42), "Signed native position payload");
            target.Size = new(200, 150);
            Send(0x206, 200, 150); Send(0x207, 400, 300);
            Check(resized == new Size(200, 150) && pixels == new WindowSize(400, 300) &&
                button.ActualWidth == 200 && button.ActualHeight == 150, "Logical/pixel events remain distinct and layout updates");
            Send(0x209); Send(0x209); Send(0x20a); Send(0x20b); Send(0x217); Send(0x218);
            Check(states.SequenceEqual([WindowState.Minimized, WindowState.Maximized, WindowState.Normal, WindowState.Fullscreen, WindowState.Normal]), "Window state event mapping");
            Send(0x213, 7); Send(0x214); Send(0x204);
            Check(display == 7 && scale > 0 && exposed == 1, "Display and exposed events");
            target.HandleSdlEvent(new() { Type = 0x205, WindowId = root.Id, Data1 = 999 });
            Check(moved.X == -15, "Foreign events do not reach hooks");
            var dialog = target.ShowDialog();
            Throws<InvalidOperationException>(target.Activate);
            dialog.RequestClose();
            target.RequestClose(); root.RequestClose();
        });
    }
    private sealed class ObservedWindow(WindowOptions options) : SwealiumWindow(options)
    {
        internal int FocusHooks;
        protected override void OnFocusChanged()
        {
            FocusHooks++;
            base.OnFocusChanged();
        }
    }
}

