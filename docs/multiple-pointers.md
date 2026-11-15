# Multiple pointers

[Documentation index](README.md) · [Input contracts](api/input.md)

PointerEvent now represents mouse devices, touch contacts, and pens. Existing mouse calls remain valid: omitted identity is PointerId.Mouse (Mouse device 0/contact 0). Routing, hover paths, press targets, and capture are independent for every PointerId.

```csharp
var first = new PointerId(PointerDeviceKind.Touch, 10, 1);
var second = new PointerId(PointerDeviceKind.Touch, 10, 2);
input.ProcessPointer(new PointerEvent(PointerEventKind.Down, new Point(30, 40),
    PointerButton.Primary, pointerId: first, pressure: 0.7f));
input.ProcessPointer(new PointerEvent(PointerEventKind.Down, new Point(160, 40),
    PointerButton.Primary, pointerId: second, pressure: 0.5f));
```

PointerId is a readonly record of Device (Mouse/Touch/Pen), unsigned 64-bit DeviceId, and unsigned 64-bit ContactId. These IDs are scoped to the input/native session, not persistent hardware identifiers. Mouse/pen use a device identity with ContactId=0; touch uses both touch-device and finger IDs, preserving all bits on each architecture.

## Events and state

PointerEvent retains Kind, WindowPosition, local Position, Button, WheelDelta, and Modifiers. It adds PointerId, DeviceKind, Pressure, IsPrimary, IsEraser, TiltX/TiltY, and current Buttons. Pressure is finite [0,1]; tilt is finite and expressed in degrees. Mouse pressure defaults zero because it is not measured. Buttons is a PointerButtons flags value (None/Primary/Middle/Secondary/Back/Forward), updated during routing. Button still identifies the changed button. Coordinates are logical window units and may be outside the viewport during capture.

Optional isPrimary overrides host classification. Otherwise mice retain primary compatibility and the first active contact of a non-mouse device kind is primary. Additional touches do not steal keyboard focus, but still receive typed input, capture, and clicks. Primary designation is retained for a touch's lifetime; ending the primary touch does not transfer focus to another already-active finger. Legacy OnMouseDown/Up/Move hooks run only for primary events; typed PointerInput/OnPointerInput receive all contacts.

InputManager.Pointers is a read-only snapshot of PointerInfo records: identity, position, buttons, pressure, primary/contact state, captured widget, eraser, and tilt. GetPointer(id) returns one snapshot or null. Hovering mice/pens remain tracked after Up; touch records are removed after Up or cancellation. GetCapturedWidget(id) is the precise capture query. The older CapturedWidget property is a compatibility view of the current mouse or a primary captured contact.

## Capture and cancellation

Primary-button Down automatically captures its hit target independently per pointer. Matching Up releases that capture, routes Up to the captured target, and clicks only the original eligible pressed target when released inside it and unhandled. One pointer's Up/Cancel does not reset another's state.

```csharp
input.CapturePointer(pointerId, dragTarget); // false if contact/target is unavailable
input.ReleasePointerCapture(pointerId);     // cancels old target's press
input.CancelPointer(pointerId);             // ends only this contact and hover path
input.Cancel();                            // window-level reset: all contacts and focus
```

Capture transfer/release sends a typed Cancel to the old target and prevents an old-target click. PointerCaptureChanged carries PointerId, Previous, and Current for automatic/manual changes. Detachment/disabled ancestry cancels affected capture during validation. Window deactivation, modal blocking, root replacement, and disposal reset all contacts. PointerEventKind.Cancel is per-pointer; use InputManager.Cancel for a global reset.

IsPointerOver aggregates hover across all contacts. Button keeps separate pressed/inside sets, so releasing one finger does not clear another press. ScrollContainer allows simultaneous drags of different scrollbar axes, with one drag owner per axis. TextBox has one selection owner and ignores competing secondary contacts rather than moving a shared caret. Widgets and pointer callbacks remain UI-thread operations.

Hardware cursor resolution uses the most recent mouse device, not a finger's hover path. Capture preserves that mouse's dragged-widget cursor. Touch contacts do not move or replace the hardware cursor.

## Native SDL boundary

Native mouse IDs, touch down/up/motion/cancel, pen tip/button/motion/axis/proximity-out are decoded. Touch coordinates are normalized by SDL and converted using the target window's logical viewport. Touch events have a different ABI window-ID location; both application routing and window filtering use that field. Out-of-range normalized coordinates remain out of bounds, supporting captured motion beyond the window.

Pen decoding exposes pressure, horizontal/vertical tilt, eraser tip and mapped barrel buttons. Other pen axes (distance, rotation, slider, tangential pressure) and device capability enumeration are not currently exposed. Mouse/touch emulation generated from touch/pen is filtered to avoid duplicate contacts. Pen proximity-out with no window association cancels the device across managed windows.

This is contact routing/capture, not a gesture recognizer. Pinch/rotate gestures, touch panning/inertia, contact-area ellipses, and platform pen capability UI are not included. Custom hosts can inject typed events without SDL. Physical devices/backends still require target-platform testing; automated tests cover ABI fields, normalization, multiple contacts, capture lifetime, controls, and cancellation.
