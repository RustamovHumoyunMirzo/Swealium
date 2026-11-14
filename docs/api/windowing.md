# Windowing and application API contracts

[Documentation index](../README.md) · [Complete signatures](reference.md) · [Window guide](../windows.md)

Namespace: Swealium. Native operations require the creating UI/main thread and a live window. Cached identity/lifecycle fields remain readable after disposal. Mutable widgets/status collections still follow the UI-thread convention even where no runtime guard exists.

## SwealiumApplication

Static owner of one managed SDL session.

| Member | Parameters/result | Contract |
| --- | --- | --- |
| Run(options = null, configure = null) | Optional WindowOptions and Action<SwealiumWindow>; returns void when the session ends | Initializes SDL video, creates/registers first window, installs UI synchronization context, invokes configure, pumps every registered window, cleans up before shutdown |
| CreateWindow(options = null) | Returns a newly registered independent SwealiumWindow | Requires active Run and UI thread |
| Windows | IReadOnlyList<SwealiumWindow>, read-only | New snapshot on each access; empty outside a session |
| Exit() | void | Requests closure of all current windows; Closing can cancel |

Only one Run may be active. Nested/concurrent Run throws InvalidOperationException. Android/iOS Run throws PlatformNotSupportedException because native hosts are missing. Exceptions escaping configure/UI callbacks terminate the session; cleanup is attempted before the first failure is rethrown.

The synchronization context supports asynchronous Post while alive. Cross-thread Send is unsupported. It does not keep a session alive after all windows and queued callbacks are gone. Do not post detached UI work after shutdown.

## WindowOptions

Sealed record; use object initializers or with-expressions. These values are creation requests, not a mutable live binding to a window.

| Property | Default | Meaning/validation |
| --- | --- | --- |
| Title | "Swealium" | Non-null UTF-8 title without embedded null |
| Width, Height | 960, 640 | Positive logical client integers |
| Background | Color(24, 26, 30) | Initial framework clear color |
| Resizable | true | Native resize capability request |
| Borderless | false | Omit window decoration |
| AlwaysOnTop | false | Window-manager topmost request |
| ShowInTaskbar | true | false requests utility-window treatment |
| Visible | true | Window is initially shown; false leaves it registered/hidden |
| Focusable | true | Native keyboard focus eligibility |
| HighPixelDensity | true | High-density back-buffer request |
| Position | null | Optional WindowPosition; null delegates placement to OS |
| MinimumSize | (0, 0) | Nonnegative native logical minimum |
| MaximumSize | (0, 0) | Nonnegative maximum; zero axis means unlimited |
| InitialState | Normal | Defined WindowState value |
| IsModal | false | Requires a non-null native/framework owner |

Invalid dimensions, enums, text, or contradictory limits are rejected before creation. OS/backend creation errors surface as InvalidOperationException with SDL's error text.

## SwealiumWindow

Subclassable partial class implementing IDisposable.

### Construction and identity

SwealiumWindow(WindowOptions? options = null, SwealiumWindow? owner = null) creates the native window, renderer, input manager, and viewport. Within Run it registers automatically. Outside Run it assumes an already initialized external native host; it does not own SDL initialization/shutdown.

| Member | Access | Contract |
| --- | --- | --- |
| Id | get | Nonzero portable uint event-routing ID. Cached; not an OS handle or persistent identity |
| Owner | get | Immutable owner, or null for independent windows |
| OwnedWindows | get | Snapshot of direct owned windows, not all descendants |
| IsDisposed | get | True after native cleanup |
| IsCloseRequested | get | True after an accepted RequestClose |
| IsModal | get | Creation-time framework modality |
| IsInputBlocked | get | Whether the active modal in this ownership group excludes this window |
| Result | get | Dialog result; None for an ordinary window unless modal result logic applies |
| Completion | get | Task<DialogResult> completed after disposal, asynchronously scheduling continuations |

An owner must be live, on the same UI thread, not closing, and not blocked. Inside Run an owner must belong to that session. Ownership cannot be changed. Children are destroyed before native owners.

### Content and drawing

| Member | Access/result | Contract |
| --- | --- | --- |
| Content | get/set Widget? | Owns Input.Root; assigning validates root ownership and arranges the viewport |
| Input | get InputManager | Per-window input tree/routing/focus manager |
| ClientSize | get Graphics.Size | Cached logical viewport; refreshed by rendering/resize operations |
| Background | get/set Color | Surface clear color; alpha does not enable OS transparency |
| Paint | event Action<DrawingContext, Graphics.Size> | Frame-scoped drawing behind Content |
| Render() | void | Main-thread single frame; hidden/minimized windows skip; reentry is rejected |

Run invokes Render automatically. Do not drive a second frame loop alongside it. Drawing contexts expire after callbacks. A drawing error propagates after context cleanup.

### Native getters and controls

| Member | Type / access | Contract |
| --- | --- | --- |
| Title | string get/set | UTF-8; null/NUL rejected |
| Position | WindowPosition get/set | Desktop logical integer coordinates; setter requests placement |
| Size | WindowSize get/set | Logical client integers; positive requested dimensions; relayout queries actual native size |
| PixelSize | WindowSize get | Physical drawable dimensions |
| MinimumSize | WindowSize get/set | Nonnegative minimum; validated against current nonzero maxima |
| MaximumSize | WindowSize get/set | Nonnegative upper limits; zero removes an axis limit |
| Flags | WindowFlags get | Live mapped native flag snapshot, not an SDL mask |
| IsVisible | bool get/set | Getter observes hidden flag; setter calls Show/Hide |
| IsMinimized, IsMaximized | bool get | Live native state flags |
| IsFullscreen | bool get/set | Live flag / native fullscreen request |
| IsOccluded | bool get | Native occlusion observation |
| Resizable | bool get/set | Native resizability |
| Borderless | bool get/set | Inverse of native bordered setting |
| AlwaysOnTop | bool get/set | Native topmost request |
| Focusable | bool get/set | Disabling also cancels framework interaction |
| MouseGrabbed, KeyboardGrabbed | bool get/set | Native per-window grabs; distinct from widget capture |
| ShowInTaskbar | bool get | Inverse of native utility flag; creation-only option |
| HighPixelDensity | bool get | Observed creation/native density capability |
| Opacity | float get/set | Setter requires finite 0..1; backend support varies |
| DisplayScale, PixelDensity | float get | Native positive scale values |
| HasFocus, HasMouseFocus | bool get | Cached latest native focus notifications |
| State | WindowState get/set | Getter priority: Minimized, Fullscreen, Maximized, Normal |

All native setters check SDL success. The OS may adjust/defer a request; no setter promises its requested geometry/state is already observed. Unsupported operations throw with native error details.

| Method | Effect |
| --- | --- |
| Show() | Request visibility |
| Hide() | Request hiding and cancel framework interaction |
| Activate() | Show and request native raising/focus; blocked modal owners rejected |
| Center() | Request primary-display centering |
| Minimize(), Maximize() | Request corresponding native state |
| Restore() | Leave fullscreen if needed, then restore |
| RequestAttention(attention = Briefly) | Flash/attention operation; undefined enum values rejected |

### Lifecycle and dialogs

| Method | Result/contract |
| --- | --- |
| RequestClose() | Raises cancelable Closing; recursive/already-pending requests are ignored |
| Dispose() | Immediate, idempotent cleanup; ignores close veto; rejects rendering window/owner disposal |
| CreateChild(options = null) | New owned window; default title Child window, 640x480 |
| ShowDialog(options = null, configure = null) | Immediate modal window; default title Dialog, 480x240, not resizable |
| ShowDialogAsync(options = null, configure = null) | Same creation/configuration, returns Completion |
| CloseDialog(result) | Modal-only; requires defined non-None result; requests close and rolls result back if canceled |

See [dialog contracts](dialogs.md) for result semantics. Any native window/control method requiring a live handle throws ObjectDisposedException after disposal or InvalidOperationException off-thread. Dispose also rejects destruction while this window or an owned descendant is rendering.

### Events and extension hooks

Hooks are protected virtual. Base implementations raise their corresponding event; call base when overriding to preserve subscriptions.

| Event | Payload | Hook / ordering |
| --- | --- | --- |
| FocusChanged | bool | OnFocusChanged(); HasFocus updated and input canceled on loss before notification |
| MouseFocusChanged | bool | OnMouseFocusChanged(); HasMouseFocus updated first; leaving clears hover |
| VisibilityChanged | bool | OnVisibilityChanged(bool); hidden notification cancels input first |
| Moved | WindowPosition | OnMoved(WindowPosition); signed native payload |
| Resized | Graphics.Size | OnResized(Graphics.Size); logical dimensions; viewport queried/arranged first |
| PixelSizeChanged | WindowSize | OnPixelSizeChanged(WindowSize); physical payload; viewport refreshed |
| StateChanged | WindowState | OnStateChanged(WindowState); minimize cancels input first |
| DisplayChanged | uint | OnDisplayChanged(uint); native display identifier |
| DisplayScaleChanged | float | OnDisplayScaleChanged(float); viewport refreshed before notification |
| Exposed | no payload | OnExposed(); repaint/exposure notification |
| Paint | DrawingContext, Graphics.Size | OnPaint(...); before visual tree rendering |
| Closing | WindowClosingEvent | OnClosing(...); cancellation allowed |
| Closed | no payload | OnClosed(); after native cleanup/unregister |

Identical focus/mouse-focus/visibility/state transitions are suppressed. Move/resize payloads describe their queued notification, while live getters can already reflect newer changes. Native OS close input to a blocked owner is ignored; programmatic RequestClose is still allowed.

### Native interop

TryGetNativeWindowHandle(out NativeWindowHandle handle) returns false/default when no supported handle is available. GetNativeWindowHandle() throws PlatformNotSupportedException in that case. Both enforce UI-thread/liveness; other SDL query failures still throw. Neither exposes an SDL_Window pointer.

NativeWindowHandle is a readonly positional record struct with Kind, nuint Value, optional nint Display=0, and IsValid. Its constructor does not validate that a manually supplied OS object is live. IsValid means Kind != None and Value != 0 only.

NativeWindowKind values: None, Win32, Cocoa, UIKit, Android, X11, Wayland. Value is HWND/NSWindow*/UIWindow*/ANativeWindow*/X11 Window ID/wl_surface* respectively. Display is the borrowed X11/Wayland connection when provided. See [interop lifetime rules](../native-interop.md).

## Window value types and enums

WindowPosition(int X, int Y) and WindowSize(int Width, int Height) are readonly positional records; their constructors alone do not enforce geometry constraints. Window setters validate them.

WindowState: Normal, Minimized, Maximized, Fullscreen. WindowAttention: Cancel, Briefly, UntilFocused.

WindowFlags is a flags enum: None, Visible, Resizable, Borderless, AlwaysOnTop, Minimized, Maximized, Fullscreen, Focused, MouseFocused, MouseGrabbed, KeyboardGrabbed, Modal, HighPixelDensity, Utility, Focusable, Occluded. Numeric values are listed in the [signature inventory](reference.md#swealium-windowflags). Do not cast these values to SDL constants.

WindowClosingEvent is a sealed mutable event object with Cancel=false by default. Set Cancel inside Closing to veto a request; it cannot veto Dispose or forced owner teardown.

