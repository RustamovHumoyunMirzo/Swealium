# Input API contracts

[Documentation index](../README.md) · [Complete signatures](reference.md) · [Routing guide](../input.md)

Namespace: Swealium.Input. Processing/routing is mutable and should stay on the UI thread.

## InputManager

Sealed, public parameterless constructor. Each window exposes its own instance.

| Member | Result / contract |
| --- | --- |
| Root | Widget? get/set | Unparented unique input-tree root; replacement cancels existing interaction and releases previous root ownership |
| FocusedWidget | Widget? get | Current eligible widget focus |
| CapturedWidget | Widget? get | Automatically captured primary-press target |
| HitTest(Point point) | Widget? | Topmost eligible hit in logical window coordinates; ensures known-root invalidated layout first |
| Focus(Widget? widget) | bool | true when focus request succeeds/no change; false for ineligible/detached/blocked targets; null clears |
| ProcessPointer(PointerEvent e) | void | Validates interaction, hover/capture, routes pointer event, and optionally synthesizes primary-release click |
| ProcessKey(KeyEvent e) | void | Routes to focused widget; default unhandled non-repeat Tab navigates |
| ProcessText(TextInputEvent e) | void | Routes only if focused widget opts into text input |
| Cancel() | void | Release capture, hover, and widget focus; sends direct cancel/leave notifications as needed |

Null event objects throw ArgumentNullException. Root assignment rejects an already-parented widget or a root owned by another input manager. HitTest may return null for no root/no hit/window policy blocking.

Window-owned managers suppress input while the window is hidden/minimized/closing/disposed or modal-blocked. Standalone managers have no native window guard; configure Root and run LayoutEngine.Layout yourself.

### Hit testing and capture

Eligibility excludes hidden, disabled, and IsHitTestVisible=false subtrees. The rounded ContainsPoint boundary clips descendants when ClipToBounds=true. Sibling order follows descending ZIndex with later insertion first for ties.

Primary Down captures its hit target and focuses the nearest focusable ancestor. Motion/Up routes to capture until primary release. Click is synthesized only if primary release hits the same target, is unhandled, and the target is still attached/eligible. Other buttons do not synthesize clicks.

Cancel goes directly to the captured widget; Enter/Leave go directly to each affected hover-path member. Bubbling input uses a snapshot of the source-to-parent path.

### Focus and navigation

Eligible focus requires attachment, effective IsVisible/IsEnabled, and IsFocusable. IsHitTestVisible is a pointer policy, not focus eligibility. Default Tab order is depth-first insertion order, independent of ZIndex. Shift reverses it; navigation wraps.

Focus removal updates IsFocused and calls the widget's focus hook/event. Changing Root cancels all interaction. Removed/disabled/invisible targets are invalidated during subsequent processing/render validation.

## InputEvent

Abstract base with protected constructor.

| Property | Access | Contract |
| --- | --- | --- |
| Source | get Widget? | Original target set during dispatch |
| CurrentTarget | get Widget? | Widget currently receiving the event |
| Handled | get/set bool, false initially | Stop later bubbling/default processing where applicable |

Source/CurrentTarget setters are internal. Fresh events begin untargeted. Reusing an event is discouraged because routing mutates its fields.

## PointerEvent

Constructor: PointerEvent(PointerEventKind kind, Point position, PointerButton button = None, Point wheelDelta = default).

| Property | Meaning |
| --- | --- |
| Kind | Move, Down, Up, Wheel, Enter, Leave, or Cancel |
| WindowPosition | Original supplied logical window position |
| Position | CurrentTarget-local position during routing; initially the supplied position |
| Button | Changed pointer button, or None |
| WheelDelta | Scroll amounts; SDL's flipped direction is normalized by native decoding |

Properties are read-only to callers. This is currently a single-pointer/mouse-style API; it has no pointer ID, pressure, multiple-touch routing, or public manual capture API. Touch synthesized as mouse by a native backend may still generate mouse-style input.

PointerButton values: None, Primary, Middle, Secondary, Back, Forward. Native unknown buttons map to None.

## KeyEvent

Constructor: KeyEvent(Key key, bool isDown, KeyModifiers modifiers = None, bool isRepeat = false). Read-only Key/IsDown/Modifiers/IsRepeat describe one physical keyboard transition/repeat.

Key is physical identity, not a character or an OS keycode. Supported named groups are A–Z, D1–D9/D0, Enter/Escape/Backspace/Tab/Space, arrows, Home/End/PageUp/PageDown/Insert/Delete, F1–F12, and left/right Shift/Control/Alt/Super. Other native scancodes map to Unknown. See the generated [Key enum](reference.md#swealium-input-key) for every name/value.

KeyModifiers is a flags enum: None=0, Shift=1, Control=2, Alt=4, Super=8, CapsLock=16, NumLock=32. Left/right native modifiers collapse to their corresponding combined Swealium flag.

## TextInputEvent

Constructor: TextInputEvent(string text). Read-only Text contains committed Unicode text. Native decoding copies the UTF-8 payload into a managed string before polling another event. The constructor itself is a simple data carrier and does not sanitize arbitrary manually supplied strings.

A focused widget must set AcceptsTextInput=true. Native text-service activation is synchronized with that opt-in. IME preedit, composition selection/candidates, and editor behavior are not exposed yet.

