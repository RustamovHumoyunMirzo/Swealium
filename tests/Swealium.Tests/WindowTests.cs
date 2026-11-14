using System.Runtime.InteropServices;
using Swealium;
using Swealium.Native;
using Swealium.UI;

internal static class WindowTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }
    private static void Click(SwealiumWindow target, bool queue = false)
    {
        var down = new Sdl.Event { Type = 0x401, WindowId = target.Id, Button = 1, MouseX = 20, MouseY = 20 };
        var up = down; up.Type = 0x402;
        if (queue) { Sdl.Check(Sdl.SDL_PushEvent(in down)); Sdl.Check(Sdl.SDL_PushEvent(in up)); }
        else { target.HandleSdlEvent(down); target.HandleSdlEvent(up); }
    }

    internal static void MultipleWindows()
    {
        int rootClicks = 0, secondaryClicks = 0, frames = 0;
        SwealiumWindow? secondary = null, child = null, grandchild = null, first = null;
        List<string> closed = [];
        SwealiumApplication.Run(new() { Width = 160, Height = 120 }, root =>
        {
            first = root;
            var rootButton = new Button();
            rootButton.Click += () => rootClicks++;
            root.Content = rootButton;
            secondary = SwealiumApplication.CreateWindow(new() { Width = 180, Height = 140 });
            var secondaryButton = new Button();
            secondaryButton.Click += () => secondaryClicks++;
            secondary.Content = secondaryButton;
            child = root.CreateChild(new() { Width = 100, Height = 80 });
            grandchild = child.CreateChild(new() { Width = 80, Height = 60 });
            Check(SwealiumApplication.Windows.Count == 4, "All windows register with the application");
            Check(Sdl.SDL_GetWindowParent(child.NativeHandle) == root.NativeHandle &&
                Sdl.SDL_GetWindowParent(grandchild.NativeHandle) == child.NativeHandle, "Native ownership matches managed ownership");
            grandchild.Closed += () => closed.Add("grandchild");
            child.Closed += () => closed.Add("child");
            root.Closed += () => closed.Add("root");
            Click(secondary, queue: true);
            root.Paint += (_, _) => root.RequestClose();
            secondary.Paint += (_, _) =>
            {
                frames++;
                if (frames < 2) return;
                Check(root.IsDisposed && child.IsDisposed && grandchild.IsDisposed, "Closing owner destroys owned tree");
                Check(rootClicks == 0 && secondaryClicks == 1, "Queued input reaches only the target window");
                Check(SwealiumApplication.Windows.Count == 1 && root.OwnedWindows.Count == 0, "Independent window keeps application alive");
                secondary.RequestClose();
            };
            var crossThread = Task.Run(() =>
                Throws<InvalidOperationException>(() => SwealiumApplication.CreateWindow()));
            crossThread.GetAwaiter().GetResult();
        });
        Check(frames >= 2 && secondary!.IsDisposed && first!.IsDisposed, "All windows close before SDL shutdown");
        Check(closed.SequenceEqual(["grandchild", "child", "root"]), "Children release resources before their native owner");
        Check(SwealiumApplication.Windows.Count == 0, "Registry is reset");
    }

    internal static void Modality()
    {
        int phase = 0, rootClicks = 0;
        SwealiumApplication.Run(new() { Width = 200, Height = 140 }, root =>
        {
            var button = new Button();
            button.Click += () => rootClicks++;
            root.Content = button;
            root.Input.Focus(button);
            var sibling = root.CreateChild(new() { Width = 80, Height = 60 });
            var independent = SwealiumApplication.CreateWindow(new() { Width = 100, Height = 80 });
            var dialog = root.ShowDialog(new() { Width = 160, Height = 100 }, d => d.Content = new Button());
            Check(root.IsInputBlocked && sibling.IsInputBlocked && !dialog.IsInputBlocked && !independent.IsInputBlocked,
                "Modal window blocks only its ownership group");
            Check(root.Input.FocusedWidget is null, "Blocked owner cancels input");
            Click(root);
            root.Input.ProcessPointer(new(Swealium.Input.PointerEventKind.Down, new(20, 20), Swealium.Input.PointerButton.Primary));
            root.Input.ProcessPointer(new(Swealium.Input.PointerEventKind.Up, new(20, 20), Swealium.Input.PointerButton.Primary));
            Check(rootClicks == 0, "Blocked window ignores pointer input");
            root.HandleSdlEvent(new() { Type = 0x210, WindowId = root.Id });
            Check(!root.IsCloseRequested, "Blocked owner ignores native close requests");
            Throws<InvalidOperationException>(() => root.ShowDialog());
            Throws<InvalidOperationException>(() => MessageBox.Warning("test", "blocked", root));
            Check(Sdl.SDL_GetWindowParent(dialog.NativeHandle) == root.NativeHandle, "Dialog has native parent");
            var nested = dialog.ShowDialog(new() { Width = 100, Height = 80 });
            Check(dialog.IsInputBlocked && !nested.IsInputBlocked, "Nested dialog becomes the active modal target");
            independent.Paint += (_, _) =>
            {
                switch (++phase)
                {
                    case 1: nested.CloseDialog(DialogResult.Ok); break;
                    case 2:
                        Check(nested.IsDisposed && nested.Completion.Result == DialogResult.Ok && !dialog.IsInputBlocked, "Nested result unblocks enclosing dialog");
                        Check(root.IsInputBlocked, "Outer owner stays blocked");
                        dialog.CloseDialog(DialogResult.Yes);
                        break;
                    case 3:
                        Check(dialog.IsDisposed && dialog.Completion.Result == DialogResult.Yes && !root.IsInputBlocked && !sibling.IsInputBlocked,
                            "Dialog completion releases group modality");
                        Check(root.Input.FocusedWidget == button, "Owner focus restored");
                        Click(root);
                        Check(rootClicks == 1, "Owner receives input after dismissal");
                        root.RequestClose(); independent.RequestClose();
                        break;
                }
            };
        });
        Check(phase == 3, "All modal lifecycle phases ran");
    }

    internal static void AsyncDialog()
    {
        int thread = Environment.CurrentManagedThreadId, resumedThread = 0;
        DialogResult result = DialogResult.None;
        SwealiumApplication.Run(new() { Width = 120, Height = 80 }, root =>
        {
            Task<DialogResult> task = root.ShowDialogAsync(new() { Width = 100, Height = 80 }, d =>
                d.Paint += (_, _) => d.CloseDialog(DialogResult.Ok));
            Observe();
            async void Observe()
            {
                result = await task;
                resumedThread = Environment.CurrentManagedThreadId;
                var child = root.CreateChild(new() { Width = 60, Height = 60 });
                child.RequestClose();
                root.RequestClose();
            }
        });
        Check(result == DialogResult.Ok && resumedThread == thread, "Await resumes inside the UI event loop");
    }

    internal static void Closing()
    {
        SwealiumWindow? dialog = null, child = null;
        bool closed = false, veto = true;
        SwealiumApplication.Run(new() { Width = 120, Height = 80 }, root =>
        {
            bool vetoRoot = true;
            root.Closing += e => e.Cancel = vetoRoot;
            root.RequestClose();
            Check(!root.IsCloseRequested, "Root close can be canceled");
            vetoRoot = false;
            Throws<InvalidOperationException>(() => root.ShowDialog(configure: _ => throw new InvalidOperationException("configuration")));
            Check(root.OwnedWindows.Count == 0 && !root.IsInputBlocked, "Failed dialog configuration releases modality and registration");
            child = root.CreateChild(new() { Width = 80, Height = 60 });
            dialog = root.ShowDialog(new() { Width = 100, Height = 80 });
            dialog.Closing += e => e.Cancel = veto;
            dialog.CloseDialog(DialogResult.Ok);
            Check(!dialog.IsCloseRequested && dialog.Result == DialogResult.None && root.IsInputBlocked, "Canceled dialog close does not commit a result");
            veto = false;
            dialog.CloseDialog(DialogResult.Yes);
            Check(dialog.IsCloseRequested && dialog.Result == DialogResult.Yes, "Accepted close commits result");
            root.Closed += () => closed = true;
            root.RequestClose();
        });
        Check(closed && dialog!.IsDisposed && child!.IsDisposed && dialog.Completion.Result == DialogResult.Yes, "Owner closure releases child resources");

        SwealiumApplication.Run(new() { Width = 120, Height = 80 }, root =>
        {
            var pending = root.ShowDialog(new() { Width = 100, Height = 80 });
            pending.Closing += e => e.Cancel = true;
            root.Dispose();
            Check(pending.IsDisposed && pending.Completion.Result == DialogResult.Cancel, "Forced owner destruction overrides child veto");
        });
        Throws<ArgumentException>(() => SwealiumApplication.Run(new() { IsModal = true }));
        Throws<InvalidOperationException>(() => SwealiumApplication.CreateWindow());
        Throws<TestCleanupException>(() => SwealiumApplication.Run(configure: root =>
        {
            _ = root.CreateChild();
            root.Closed += () => throw new TestCleanupException();
            root.RequestClose();
        }));
        Check(SwealiumApplication.Windows.Count == 0, "Cleanup continues after a Closed callback throws");
        SwealiumApplication.Run(configure: root =>
        {
            _ = SwealiumApplication.CreateWindow();
            _ = root.ShowDialog();
            var quit = new Sdl.Event { Type = 0x100 };
            Sdl.Check(Sdl.SDL_PushEvent(in quit));
            root.Paint += (_, _) => throw new InvalidOperationException("Quit must close every window before drawing");
        });
        Check(SwealiumApplication.Windows.Count == 0, "SDL quit closes all independent windows and their dialogs");
    }

    internal static void MessageBoxes()
    {
        Check(Marshal.SizeOf<Sdl.MessageBoxData>() == (IntPtr.Size == 8 ? 56 : 28), "Native data size");
        Check(Marshal.OffsetOf<Sdl.MessageBoxData>("Window").ToInt32() == (IntPtr.Size == 8 ? 8 : 4), "Native parent alignment");
        Check(Marshal.OffsetOf<Sdl.MessageBoxData>("Buttons").ToInt32() == (IntPtr.Size == 8 ? 40 : 20), "Native button pointer alignment");
        foreach (var preset in Enum.GetValues<MessageBoxButtons>())
        {
            using var request = new NativeMessageBoxRequest(new()
            {
                Title = "警告 — Swealium", Message = "Salom 👋\nContinue?", Icon = MessageBoxIcon.Warning, Buttons = preset
            }, 123);
            Check(request.Data.Window == 123 && (request.Data.Flags & 0x20) != 0, "Warning flags and owner");
            Check(Marshal.PtrToStringUTF8(request.Data.Title) == "警告 — Swealium" &&
                Marshal.PtrToStringUTF8(request.Data.Message) == "Salom 👋\nContinue?", "UTF-8 lifetime");
            Check(request.Data.ButtonCount is >= 1 and <= 3, "Standard button presets");
            int defaults = 0, escapes = 0;
            for (int i = 0; i < request.Data.ButtonCount; i++)
            {
                var button = Marshal.PtrToStructure<Sdl.MessageBoxButton>(request.Data.Buttons + i * Marshal.SizeOf<Sdl.MessageBoxButton>());
                defaults += (button.Flags & 1) != 0 ? 1 : 0;
                escapes += (button.Flags & 2) != 0 ? 1 : 0;
                Check(!string.IsNullOrEmpty(Marshal.PtrToStringUTF8(button.Text)), "Button label");
                Check(request.Resolve(button.Id) == (DialogResult)button.Id, "Native result mapping");
            }
            Check(defaults == 1 && escapes == 1 && request.Resolve(-1) == DialogResult.Cancel, "Keyboard defaults and OS dismissal");
            Throws<InvalidOperationException>(() => request.Resolve(999));
        }
        Throws<ArgumentException>(() => new NativeMessageBoxRequest(new() { Buttons = MessageBoxButtons.Ok, DefaultButton = DialogResult.No }, 0));
        Throws<ArgumentException>(() => new NativeMessageBoxRequest(new() { Message = "bad\0text" }, 0));
        Throws<ArgumentOutOfRangeException>(() => new NativeMessageBoxRequest(new() { Icon = (MessageBoxIcon)999 }, 0));
    }
    private sealed class TestCleanupException : Exception;
}

