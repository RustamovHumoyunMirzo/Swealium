using SkiaSharp;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.Native;
using Swealium.UI;
using Swealium.Accessibility;
using System.Runtime.ExceptionServices;
namespace Swealium;

/// <summary>An SDL main-thread window with a retained visual tree and logical-coordinate drawing.</summary>
/// <remarks>The platform host must initialize video before creating a window.</remarks>
public partial class SwealiumWindow : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly WindowOptions options;
    private nint window, renderer, texture;
    private GpuWindowSurface? gpu;
    private SKSurface? surface;
    private SKBitmap? bitmap;
    private bool disposed, rendering, textInputActive;
    private bool disposing, requestingClose;
    private static long nextSequence;
    private readonly long sequence = Interlocked.Increment(ref nextSequence);
    private readonly List<SwealiumWindow> ownedWindows = [];
    private readonly TaskCompletionSource<DialogResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Widget? previousOwnerFocus;
    public SwealiumWindow(WindowOptions? options = null, SwealiumWindow? owner = null)
    {
        Accessibility=new(this);
        SwealiumApplication.VerifyAccess();
        this.options = options ?? new();
        ArgumentNullException.ThrowIfNull(this.options.Title);
        if (this.options.Title.Contains('\0')) throw new ArgumentException("Title cannot contain a null character.", nameof(options));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.options.Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.options.Height);
        ValidateWindowSize(this.options.MinimumSize, true);
        ValidateWindowSize(this.options.MaximumSize, true);
        ValidateLimits(this.options.MinimumSize, this.options.MaximumSize);
        if (!Enum.IsDefined(this.options.InitialState)) throw new ArgumentOutOfRangeException(nameof(options));
        background = this.options.Background;
        owner?.VerifyAccess();
        if (owner is { IsCloseRequested: true } || owner?.disposing == true)
            throw new InvalidOperationException("Cannot create a window owned by a closing window.");
        if (owner?.IsInputBlocked == true) throw new InvalidOperationException("Create windows from the active modal dialog instead of its blocked owner.");
        if (this.options.IsModal && owner is null) throw new ArgumentException("A modal window requires an owner.", nameof(owner));
        Owner = owner;
        Input.CanProcessInput = () => !disposed && !disposing && window != 0 && !IsCloseRequested && IsVisible && !IsMinimized && !IsInputBlocked;
        if (this.options.IsModal) previousOwnerFocus = owner?.Input.FocusedWidget;
        try
        {
            if (SwealiumApplication.HardwareAcceleration)
            {
                try
                {
                    GpuWindowSurface.Configure();
                    window = CreateNativeWindow(true);
                    gpu = new GpuWindowSurface(window);
                }
                catch (Exception error) when (error is InvalidOperationException or NotSupportedException)
                {
                    HardwareAccelerationFallbackReason = error.Message;
                    gpu?.Dispose(); gpu = null;
                    if (window != 0) Sdl.SDL_DestroyWindow(window);
                    window = 0;
                }
            }
            if (gpu is null)
            {
                window = CreateNativeWindow();
                renderer = Sdl.Check(Sdl.SDL_CreateNamedRenderer(window, "software"));
                _ = Sdl.SDL_SetRenderVSync(renderer, 0);
            } // One application-wide frame clock serves every window.
            Id = Sdl.SDL_GetWindowID(window);
            Input.FocusChanged += SyncTextInput;
            Input.CompositionCancellationRequested += CancelNativeComposition;
            UpdateViewport();
            if (this.options.MinimumSize != default) MinimumSize = this.options.MinimumSize;
            if (this.options.MaximumSize != default) MaximumSize = this.options.MaximumSize;
            if (this.options.Visible) Sdl.Check(Sdl.SDL_ShowWindow(window));
            InitializeWindowObservations();
            SwealiumApplication.Register(this);
            owner?.ownedWindows.Add(this);
            if (IsModal)
                foreach (var member in TopLevel.DescendantsAndSelf().ToArray())
                    if (member.IsInputBlocked) member.Input.Cancel();
        }
        catch { Dispose(); throw; }
    }
    /// <summary>True when this window uses a GPU-backed Skia surface.</summary>
    public bool IsHardwareAccelerated { get { VerifyAccess(); return gpu is not null; } }
    /// <summary>Initialization failure that caused raster fallback, or null if not attempted/successful.</summary>
    public string? HardwareAccelerationFallbackReason { get; private set; }
    private nint CreateNativeWindow(bool openGl = false)
    {
        uint properties = Sdl.SDL_CreateProperties();
        Sdl.Check(properties != 0);
        try
        {
            Sdl.Check(Sdl.SDL_SetStringProperty(properties, "SDL.window.create.title", options.Title));
            Sdl.Check(Sdl.SDL_SetNumberProperty(properties, "SDL.window.create.width", options.Width));
            Sdl.Check(Sdl.SDL_SetNumberProperty(properties, "SDL.window.create.height", options.Height));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.hidden", true));
            if (openGl) Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.opengl", true));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.high_pixel_density", options.HighPixelDensity));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.focusable", options.Focusable));
            if (options.Position is { } position)
            {
                Sdl.Check(Sdl.SDL_SetNumberProperty(properties, "SDL.window.create.x", position.X));
                Sdl.Check(Sdl.SDL_SetNumberProperty(properties, "SDL.window.create.y", position.Y));
            }
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.minimized", options.InitialState == WindowState.Minimized));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.maximized", options.InitialState == WindowState.Maximized));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.fullscreen", options.InitialState == WindowState.Fullscreen));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.resizable", options.Resizable));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.borderless", options.Borderless));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.always_on_top", options.AlwaysOnTop));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.utility", !options.ShowInTaskbar));
            if (Owner is not null)
                Sdl.Check(Sdl.SDL_SetPointerProperty(properties, "SDL.window.create.parent", Owner.window));
            Sdl.Check(Sdl.SDL_SetBooleanProperty(properties, "SDL.window.create.modal", options.IsModal));
            return Sdl.Check(Sdl.SDL_CreateWindowWithProperties(properties));
        }
        finally { Sdl.SDL_DestroyProperties(properties); }
    }
    /// <summary>The portable SDL-assigned event-routing ID. This is not an operating-system handle.</summary>
    public uint Id { get; }
    public bool IsDisposed => disposed;
    public SwealiumWindow? Owner { get; }
    public IReadOnlyList<SwealiumWindow> OwnedWindows => ownedWindows.ToArray();
    public bool IsModal => options.IsModal;
    public DialogResult Result { get; private set; }
    public Task<DialogResult> Completion => completion.Task;
    public event Action<WindowClosingEvent>? Closing;
    public event Action? Closed;
    public bool IsInputBlocked
    {
        get
        {
            var modal = TopLevel.DescendantsAndSelf().Where(w => w.IsModal && !w.disposed).MaxBy(w => w.sequence);
            return modal is not null && !IsDescendantOrSelfOf(modal);
        }
    }
    private SwealiumWindow TopLevel => Owner?.TopLevel ?? this;
    private bool IsDescendantOrSelfOf(SwealiumWindow ancestor)
    {
        for (SwealiumWindow? current = this; current is not null; current = current.Owner)
            if (ReferenceEquals(current, ancestor)) return true;
        return false;
    }
    private IEnumerable<SwealiumWindow> DescendantsAndSelf()
    {
        yield return this;
        foreach (var child in ownedWindows)
            foreach (var nested in child.DescendantsAndSelf()) yield return nested;
    }
    public SwealiumWindow CreateChild(WindowOptions? options = null)
    {
        VerifyAccess();
        return new(options ?? new WindowOptions { Title = "Child window", Width = 640, Height = 480 }, this);
    }
    /// <summary>Show a nonblocking custom modal window. Populate Content or use the configure callback.</summary>
    public SwealiumWindow ShowDialog(WindowOptions? options = null, Action<SwealiumWindow>? configure = null)
    {
        VerifyAccess();
        var dialog = new SwealiumWindow((options ?? new WindowOptions { Title = "Dialog", Width = 480, Height = 240, Resizable = false }) with { IsModal = true }, this);
        try { configure?.Invoke(dialog); return dialog; }
        catch { dialog.Dispose(); throw; }
    }
    public Task<DialogResult> ShowDialogAsync(WindowOptions? options = null, Action<SwealiumWindow>? configure = null) =>
        ShowDialog(options, configure).Completion;
    public void CloseDialog(DialogResult result)
    {
        VerifyAccess();
        if (!IsModal) throw new InvalidOperationException("This window is not a modal dialog.");
        if (!Enum.IsDefined(result) || result == DialogResult.None) throw new ArgumentOutOfRangeException(nameof(result));
        var previous = Result;
        Result = result;
        try { RequestClose(); }
        finally { if (!IsCloseRequested && !disposed) Result = previous; }
    }
    public bool IsCloseRequested { get; private set; }
    public Size ClientSize { get; private set; }
    public InputManager Input { get; } = new();
    public AccessibilityHost Accessibility {get;}
    /// <summary>The root fills the logical client area. Children use parent-relative bounds.</summary>
    public Widget? Content
    {
        get => Input.Root;
        set { VerifyAccess(); Input.Root = value; UpdateViewport(); }
    }
    /// <summary>Draw behind the widget tree using a frame-scoped Swealium context.</summary>
    public event Action<DrawingContext, Size>? Paint;
    public void RequestClose()
    {
        VerifyAccess();
        if (IsCloseRequested || requestingClose || disposing) return;
        requestingClose = true;
        try
        {
            var e = new WindowClosingEvent();
            OnClosing(e);
            if (!e.Cancel) IsCloseRequested = true;
        }
        finally { requestingClose = false; }
    }

    private void UpdateViewport()
    {
        Sdl.Check(Sdl.SDL_GetWindowSize(window, out int width, out int height));
        ClientSize = new(width, height);
        if (Content is { } root) LayoutEngine.Layout(root, ClientSize);
    }
    /// <summary>Render one frame on the UI thread.</summary>
    public void Render()
    {
        VerifyAccess();
        if (rendering) throw new InvalidOperationException("Rendering cannot be reentered.");
        if (!IsVisible || IsMinimized) return;
        using var previousGpuContext = gpu is null ? default(GpuWindowSurface.CurrentScope) : new GpuWindowSurface.CurrentScope(0);
        rendering = true;
        try
        {
            UpdateViewport();
            Input.Validate();
            SyncCursor();
            SyncTextInput();
            Accessibility.RefreshIfObserved();
            int width, height;
            SKSurface? frame;
            if (gpu is not null)
            {
                frame = gpu.Acquire();
                (width, height) = gpu.Size;
            }
            else
            {
                Sdl.Check(Sdl.SDL_GetRenderOutputSize(renderer, out width, out height));
                if (width <= 0 || height <= 0) return;
                if (bitmap is null || bitmap.Width != width || bitmap.Height != height) ResizeSurface(width, height);
                frame = surface;
            }
            if (frame is null || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            var canvas = frame.Canvas;
            var background = Background;
            canvas.Clear(SKColors.Transparent);
            int saved = canvas.Save();
            try
            {
                canvas.Scale(width / ClientSize.Width, height / ClientSize.Height);
                GradientPaint.Clear(canvas,background,new(0,0,ClientSize.Width,ClientSize.Height));
                var context = new SkiaDrawingContext(canvas);
                try { OnPaint(context, ClientSize); }
                finally { context.End(); }
                if (Content is { } root) WidgetRenderer.Render(canvas, root);
                if (Input.FocusedWidget?.TextEditor is {IsEnabled:true} editor)
                {
                    var overlay = new SkiaDrawingContext(canvas);
                    try { editor.DrawCandidates(overlay, ClientSize); }
                    finally { overlay.End(); }
                }
            }
            finally { canvas.RestoreToCount(saved); }
            canvas.Flush();
            if (gpu is not null) gpu.Present();
            else
            {
                Sdl.Check(Sdl.SDL_UpdateTexture(texture, 0, bitmap!.GetPixels(), bitmap.RowBytes));
                Sdl.Check(Sdl.SDL_RenderClear(renderer));
                Sdl.Check(Sdl.SDL_RenderTexture(renderer, texture, 0, 0));
                Sdl.Check(Sdl.SDL_RenderPresent(renderer));
            }
        }
        finally { rendering = false; }
    }
    private void ResizeSurface(int width, int height)
    {
        ReleaseSurface();
        try
        {
            bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            surface = SKSurface.Create(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes)
                ?? throw new InvalidOperationException("Could not create drawing surface.");
            uint format = BitConverter.IsLittleEndian ? 0x16762004u : 0x16462004u;
            texture = Sdl.Check(Sdl.SDL_CreateTexture(renderer, format, 1, width, height));
        }
        catch { ReleaseSurface(); throw; }
    }
    private void SyncTextInput()
    {
        bool requested = !IsInputBlocked && Input.FocusedWidget?.AcceptsTextInput == true;
        if (window == 0) return;
        if (requested != textInputActive)
        {
            Sdl.Check(requested ? Sdl.SDL_StartTextInput(window) : Sdl.SDL_StopTextInput(window));
            textInputActive = requested;
        }
        if (requested && Input.FocusedWidget is { } focused)
        {
            Rect area = focused.TextInputArea;
            area=focused.ToWindow(area);
            var rect = new Sdl.InputRect { X = (int)MathF.Floor(area.X), Y = (int)MathF.Floor(area.Y), Width = Math.Max(1, (int)MathF.Ceiling(area.Width)), Height = Math.Max(1, (int)MathF.Ceiling(area.Height)) };
            Sdl.Check(Sdl.SDL_SetTextInputArea(window, in rect, 0));
        }
    }
    private void CancelNativeComposition()
    {
        if (window != 0 && textInputActive) _ = Sdl.SDL_ClearComposition(window);
    }
    private void VerifyAccess()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Window operations require the creating UI thread.");
    }
    internal nint NativeHandle { get { VerifyAccess(); return window; } }
    private void ReleaseSurface()
    {
        gpu?.ReleaseSurface();
        if (texture != 0) Sdl.SDL_DestroyTexture(texture);
        texture = 0;
        surface?.Dispose(); surface = null;
        bitmap?.Dispose(); bitmap = null;
    }
    public void Dispose()
    {
        if (disposed || disposing) return;
        VerifyAccess();
        if (DescendantsAndSelf().Any(w => w.rendering))
            throw new InvalidOperationException("Cannot dispose a rendering window or its owner. Use RequestClose instead.");
        disposing = true;
        Exception? failure = null;
        foreach (var child in ownedWindows.ToArray())
        {
            try { child.Dispose(); } catch (Exception error) { failure ??= error; }
        }
        try { Input.Root = null; } catch (Exception error) { failure ??= error; }
        try { Accessibility.Close(); } catch (Exception error) { failure ??= error; }
        Input.FocusChanged -= SyncTextInput;
        Input.CompositionCancellationRequested -= CancelNativeComposition;
        SdlCursorManager.ReleaseWindow(window);
        gpu?.Dispose(); gpu = null;
        ReleaseSurface();
        if (renderer != 0) Sdl.SDL_DestroyRenderer(renderer);
        if (window != 0) Sdl.SDL_DestroyWindow(window);
        renderer = window = 0;
        disposed = true;
        Owner?.ownedWindows.Remove(this);
        SwealiumApplication.Unregister(this);
        if (IsModal && Result == DialogResult.None) Result = DialogResult.Cancel;
        try
        {
            if (IsModal && Owner is { disposed: false, disposing: false, IsCloseRequested: false, IsInputBlocked: false } owner)
            {
                owner.Input.Focus(previousOwnerFocus);
                _ = Sdl.SDL_RaiseWindow(owner.window);
            }
            OnClosed();
        }
        catch (Exception error) { failure ??= error; }
        finally { completion.TrySetResult(Result); GC.SuppressFinalize(this); }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
    internal void HandleSdlEvent(Sdl.Event e)
    {
        VerifyAccess();
        if (e.RoutedWindowId != Id) return;
        if (IsInputBlocked && (e.Type == 0x210 || e.Type is >= 0x300 and < 0x800 ||e.Type is >=0x1300 and <=0x1307)) return;
        if (e.Type == 0x210) { RequestClose(); return; }
        if (e.Type is 0x2000 or 0x2001) ReleaseSurface();
        HandleWindowEvent(e);
        if (disposed || IsCloseRequested) return;
        if (e.Type is 0x20d or 0x20f) {if(Sdl.SDL_GetMouseFocus()==0)SdlCursorManager.ReleaseWindow(window);return;}
        SdlInput.Dispatch(e, Input);
        SyncCursor();
    }
    protected virtual void OnClosing(WindowClosingEvent e) => Closing?.Invoke(e);
    protected virtual void OnClosed() => Closed?.Invoke();
    protected virtual void OnPaint(DrawingContext drawing, Size size) => Paint?.Invoke(drawing, size);
}

