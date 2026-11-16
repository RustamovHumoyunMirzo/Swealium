using Swealium.Graphics;
using Swealium.UI;
namespace Swealium.Input;

public enum PointerButton { None, Primary, Middle, Secondary, Back, Forward }
public enum PointerEventKind { Move, Down, Up, Wheel, Enter, Leave, Cancel }
public enum PointerDeviceKind { Mouse, Touch, Pen }
public readonly record struct PointerId(PointerDeviceKind Device,ulong DeviceId=0,ulong ContactId=0)
{public static PointerId Mouse=>default;}
[Flags]
public enum PointerButtons { None=0,Primary=1,Middle=2,Secondary=4,Back=8,Forward=16 }
public sealed record PointerCaptureChangedEvent(PointerId PointerId,Widget? Previous,Widget? Current);
public sealed record PointerInfo(PointerId Id,Point Position,PointerButtons Buttons,float Pressure,bool IsPrimary,bool IsInContact,Widget? CapturedWidget,bool IsEraser=false,float TiltX=0,float TiltY=0);
[Flags]
public enum KeyModifiers { None = 0, Shift = 1, Control = 2, Alt = 4, Super = 8, CapsLock = 16, NumLock = 32 }
/// <summary>Physical character keys and semantic navigation keys (including keypad/remapped navigation). Use TextInput for typed characters.</summary>
public enum Key
{
    Unknown, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D1, D2, D3, D4, D5, D6, D7, D8, D9, D0,
    Enter, Escape, Backspace, Tab, Space, Left, Right, Up, Down, Home, End, PageUp, PageDown, Insert, Delete,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, LeftSuper, RightSuper
}
public abstract class InputEvent
{
    public Widget? Source { get; internal set; }
    public Widget? CurrentTarget { get; internal set; }
    public bool Handled { get; set; }
}
public sealed class PointerEvent(PointerEventKind kind, Point position, PointerButton button = PointerButton.None, Point wheelDelta = default, KeyModifiers? modifiers = null,
    PointerId pointerId=default,float pressure=0,bool? isPrimary=null,bool isEraser=false,float tiltX=0,float tiltY=0,int clickCount=0) : InputEvent
{
    public int ClickCount {get;}=clickCount>=0?clickCount:throw new ArgumentOutOfRangeException(nameof(clickCount));
    public PointerEventKind Kind { get; } = kind;
    public Point WindowPosition { get; } = position;
    public Point Position { get; internal set; } = position;
    public PointerButton Button { get; } = button;
    public Point WheelDelta { get; } = wheelDelta;
    public Point RemainingWheelDelta {get;internal set;}=wheelDelta;
    private HashSet<Widget>? scrollRecipients;
    internal HashSet<Widget> ScrollRecipients=>scrollRecipients??=[];
    public KeyModifiers Modifiers { get; internal set; } = modifiers ?? KeyModifiers.None;
    internal bool HasExplicitModifiers { get; } = modifiers.HasValue;
    public PointerId PointerId {get;}=pointerId;
    public PointerDeviceKind DeviceKind=>PointerId.Device;
    public float Pressure {get;}=pressure;
    public bool IsPrimary {get;internal set;}=isPrimary??true;
    internal bool HasExplicitPrimary {get;}=isPrimary.HasValue;
    public bool IsEraser {get;}=isEraser;
    public float TiltX {get;}=tiltX;
    public float TiltY {get;}=tiltY;
    public PointerButtons Buttons {get;internal set;}
}
public sealed class KeyEvent(Key key, bool isDown, KeyModifiers modifiers = KeyModifiers.None, bool isRepeat = false) : InputEvent
{
    public Key Key { get; } = key;
    public bool IsDown { get; } = isDown;
    public KeyModifiers Modifiers { get; internal set; } = modifiers;
    public bool IsRepeat { get; } = isRepeat;
}
public sealed class TextInputEvent(string text) : InputEvent
{
    public string Text { get; } = text;
}

