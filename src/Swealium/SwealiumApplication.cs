using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Diagnostics;
using Swealium.Native;
namespace Swealium;

/// <summary>Owns SDL and the event loop for all application windows. Invoke Run on the process main thread.</summary>
public static class SwealiumApplication
{
    private static int running;
    private static bool antiAliasing = true, hardwareAcceleration = true;
    /// <summary>Default edge smoothing for new drawing contexts. May change on the UI thread between frames.</summary>
    public static bool AntiAliasing { get => Volatile.Read(ref antiAliasing); set { VerifyAccess(); Volatile.Write(ref antiAliasing, value); } }
    /// <summary>Prefer GPU-backed Skia for new windows, falling back to raster if unavailable. Set before Run.</summary>
    public static bool HardwareAcceleration
    {
        get { VerifyAccess(); return hardwareAcceleration; }
        set { VerifyAccess(); if (Volatile.Read(ref running) != 0) throw new InvalidOperationException("Set HardwareAcceleration before Run."); hardwareAcceleration = value; }
    }
    private static Session? current;
    /// <summary>A snapshot of application-managed windows. Access on the UI thread.</summary>
    public static IReadOnlyList<SwealiumWindow> Windows
    {
        get { VerifyAccess(); return current?.Windows.Values.ToArray() ?? []; }
    }
    public static SwealiumWindow CreateWindow(WindowOptions? options = null)
    {
        VerifyAccess();
        if (current is null) throw new InvalidOperationException("Create windows inside SwealiumApplication.Run.");
        return new(options);
    }
    public static void Exit()
    {
        VerifyAccess();
        if (current is null) return;
        foreach (var window in current.Windows.Values.ToArray())
            if (!window.IsDisposed) window.RequestClose();
    }
    internal static void VerifyAccess()
    {
        if (current is { } session && Environment.CurrentManagedThreadId != session.ThreadId)
            throw new InvalidOperationException("Application window operations require the UI thread.");
    }
    internal static void Register(SwealiumWindow window)
    {
        VerifyAccess();
        if (current is not { } session) return; // External native host owns its loop and SDL lifetime.
        if (session.Stopping) throw new InvalidOperationException("The application is shutting down.");
        if (window.Owner is { } owner && !session.Windows.ContainsKey(owner.Id))
            throw new InvalidOperationException("The owner must belong to this application.");
        session.Windows.Add(window.Id, window);
    }
    internal static void Unregister(SwealiumWindow window) => current?.Windows.Remove(window.Id);

    public static void Run(WindowOptions? options = null, Action<SwealiumWindow>? configure = null)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
            throw new PlatformNotSupportedException("Mobile requires a native lifecycle host.");
        if (Interlocked.Exchange(ref running, 1) != 0)
            throw new InvalidOperationException("Only one Swealium application can run at a time.");
        var previousContext = SynchronizationContext.Current;
        var session = new Session();
        Exception? failure = null;
        bool initialized = false;
        current = session;
        try
        {
            Sdl.SDL_SetMainReady();
            _ = Sdl.SDL_SetHint("SDL_IME_IMPLEMENTED_UI", "composition,candidates");
            Sdl.Check(Sdl.SDL_Init(0x20));
            initialized = true;
            SynchronizationContext.SetSynchronizationContext(session);
            var first = new SwealiumWindow(options);
            configure?.Invoke(first);
            while (session.Windows.Count > 0 || session.HasPendingCallbacks)
            {
                long frameStart = Stopwatch.GetTimestamp();
                session.Drain();
                session.CloseRequested();
                while (Sdl.SDL_PollEvent(out var e))
                {
                    if (e.Type == 0x100) Exit();
                    else if(e.Type==0x1301&&e.WindowId==0)
                    {foreach(var member in session.Windows.Values.ToArray())member.Input.CancelPointer(new(Input.PointerDeviceKind.Pen,(uint)e.Data1));}
                    else if (session.Windows.TryGetValue(e.RoutedWindowId, out var target) && !target.IsDisposed && !target.IsCloseRequested)
                        target.HandleSdlEvent(e);
                }
                session.CloseRequested();
                foreach (var window in session.Windows.Values.ToArray())
                    if (!window.IsDisposed && !window.IsCloseRequested) window.Render();
                session.CloseRequested();
                Sdl.SDL_Delay((uint)Math.Max(1, 16 - Stopwatch.GetElapsedTime(frameStart).TotalMilliseconds));
            }
        }
        catch (Exception error) { failure = error; }
        finally
        {
            session.Stopping = true;
            foreach (var window in session.Windows.Values.ToArray())
            {
                try { window.Dispose(); }
                catch (Exception error) { failure ??= error; }
            }
            SynchronizationContext.SetSynchronizationContext(previousContext);
            current = null;
            try { if (initialized) {try{SdlCursorManager.Shutdown();}finally{Sdl.SDL_Quit();}} }
            finally { Volatile.Write(ref running, 0); }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private sealed class Session : SynchronizationContext
    {
        internal readonly int ThreadId = Environment.CurrentManagedThreadId;
        internal readonly Dictionary<uint, SwealiumWindow> Windows = [];
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> callbacks = new();
        internal bool Stopping;
        internal bool HasPendingCallbacks => !callbacks.IsEmpty;
        public override void Post(SendOrPostCallback callback, object? state)
        {
            ArgumentNullException.ThrowIfNull(callback);
            if (Stopping) return;
            callbacks.Enqueue((callback, state));
        }
        public override void Send(SendOrPostCallback callback, object? state)
        {
            if (Environment.CurrentManagedThreadId != ThreadId)
                throw new NotSupportedException("Use asynchronous UI dispatch instead of cross-thread Send.");
            callback(state);
        }
        public override SynchronizationContext CreateCopy() => this;
        internal void Drain()
        {
            // Bound each pass so a producer cannot starve native events and rendering.
            for (int i = 0; i < 256 && callbacks.TryDequeue(out var entry); i++) entry.Callback(entry.State);
        }
        internal void CloseRequested()
        {
            foreach (var window in Windows.Values.ToArray())
                if (!window.IsDisposed && window.IsCloseRequested) window.Dispose();
        }
    }
}

