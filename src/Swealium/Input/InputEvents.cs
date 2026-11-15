using Swealium.Graphics;
using Swealium.UI;
namespace Swealium.Input;

public enum PointerButton { None, Primary, Middle, Secondary, Back, Forward }
public enum PointerEventKind { Move, Down, Up, Wheel, Enter, Leave, Cancel }
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
public sealed class PointerEvent(PointerEventKind kind, Point position, PointerButton button = PointerButton.None, Point wheelDelta = default, KeyModifiers? modifiers = null) : InputEvent
{
    public PointerEventKind Kind { get; } = kind;
    public Point WindowPosition { get; } = position;
    public Point Position { get; internal set; } = position;
    public PointerButton Button { get; } = button;
    public Point WheelDelta { get; } = wheelDelta;
    public KeyModifiers Modifiers { get; internal set; } = modifiers ?? KeyModifiers.None;
    internal bool HasExplicitModifiers { get; } = modifiers.HasValue;
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

