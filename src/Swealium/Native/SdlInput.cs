using System.Runtime.InteropServices;
using Swealium.Graphics;
using Swealium.Input;
namespace Swealium.Native;

internal static class SdlInput
{
    internal static void Dispatch(Sdl.Event e, InputManager input)
    {
        switch (e.Type)
        {
            case 0x400: input.ProcessPointer(new(PointerEventKind.Move, new(e.MouseX, e.MouseY))); break;
            case 0x401:
            case 0x402:
                input.ProcessPointer(new(e.Type == 0x401 ? PointerEventKind.Down : PointerEventKind.Up,
                    new(e.MouseX, e.MouseY), Button(e.Button)));
                break;
            case 0x403:
                float direction = e.WheelDirection == 1 ? -1 : 1;
                input.ProcessPointer(new(PointerEventKind.Wheel, new(e.WheelMouseX, e.WheelMouseY),
                    wheelDelta: new Point(e.WheelX * direction, e.WheelY * direction)));
                break;
            case 0x300:
            case 0x301:
                input.ProcessKey(new(MapKey(e.ScanCode), e.Type == 0x300, Modifiers(e.Modifiers), e.Repeat != 0));
                break;
            case 0x303:
                // SDL owns this pointer; copy UTF-8 before polling another event.
                input.ProcessText(new(Marshal.PtrToStringUTF8(e.Text) ?? string.Empty));
                break;
            case 0x20d: input.ProcessPointer(new(PointerEventKind.Leave, default)); break;
            case 0x20f: input.Cancel(); break;
        }
    }
    private static PointerButton Button(byte button) => button switch
    {
        1 => PointerButton.Primary, 2 => PointerButton.Middle, 3 => PointerButton.Secondary,
        4 => PointerButton.Back, 5 => PointerButton.Forward, _ => PointerButton.None
    };
    private static KeyModifiers Modifiers(ushort value) =>
        ((value & 0x0003) != 0 ? KeyModifiers.Shift : 0) |
        ((value & 0x00c0) != 0 ? KeyModifiers.Control : 0) |
        ((value & 0x0300) != 0 ? KeyModifiers.Alt : 0) |
        ((value & 0x0c00) != 0 ? KeyModifiers.Super : 0) |
        ((value & 0x2000) != 0 ? KeyModifiers.CapsLock : 0) |
        ((value & 0x1000) != 0 ? KeyModifiers.NumLock : 0);

    private static Key MapKey(int scan) => scan switch
    {
        >= 4 and <= 29 => Key.A + (scan - 4),
        >= 30 and <= 39 => Key.D1 + (scan - 30),
        >= 58 and <= 69 => Key.F1 + (scan - 58),
        40 => Key.Enter, 41 => Key.Escape, 42 => Key.Backspace, 43 => Key.Tab, 44 => Key.Space,
        73 => Key.Insert, 74 => Key.Home, 75 => Key.PageUp, 76 => Key.Delete, 77 => Key.End, 78 => Key.PageDown,
        79 => Key.Right, 80 => Key.Left, 81 => Key.Down, 82 => Key.Up,
        224 => Key.LeftControl, 225 => Key.LeftShift, 226 => Key.LeftAlt, 227 => Key.LeftSuper,
        228 => Key.RightControl, 229 => Key.RightShift, 230 => Key.RightAlt, 231 => Key.RightSuper,
        _ => Key.Unknown
    };
}

