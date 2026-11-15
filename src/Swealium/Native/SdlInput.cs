using System.Runtime.InteropServices;
using Swealium.Graphics;
using Swealium.Input;
namespace Swealium.Native;

internal static class SdlInput
{
    internal static void Dispatch(Sdl.Event e, InputManager input)
    {
        if(e.Type is >=0x400 and <=0x403 && (uint)e.Data1>=uint.MaxValue-1)return; // touch/pen-emulated mouse
        var mouse=new PointerId(PointerDeviceKind.Mouse,(uint)e.Data1);
        switch (e.Type)
        {
            case 0x400: input.ProcessPointer(new(PointerEventKind.Move, new(e.MouseX, e.MouseY), modifiers:PointerModifiers(input),pointerId:mouse)); break;
            case 0x401:
            case 0x402:
                input.ProcessPointer(new(e.Type == 0x401 ? PointerEventKind.Down : PointerEventKind.Up,
                    new(e.MouseX, e.MouseY), Button(e.Button), modifiers:PointerModifiers(input),pointerId:mouse,clickCount:e.Clicks));
                break;
            case 0x403:
                float direction = e.WheelDirection == 1 ? -1 : 1;
                input.ProcessPointer(new(PointerEventKind.Wheel, new(e.WheelMouseX, e.WheelMouseY),
                    wheelDelta: new Point(e.WheelX * direction, e.WheelY * direction),modifiers:PointerModifiers(input),pointerId:mouse));
                break;
            case >=0x700 and <=0x703:
                if(e.TouchId>=ulong.MaxValue-1)break; // mouse/pen-emulated touch
                Size viewport=input.Root?.LayoutViewport??new Size(input.Root?.ActualWidth??0,input.Root?.ActualHeight??0);
                var touch=new PointerId(PointerDeviceKind.Touch,e.TouchId,e.FingerId);
                if(e.Type==0x703){input.CancelPointer(touch);break;}
                if(!float.IsFinite(e.TouchX)||!float.IsFinite(e.TouchY)||!float.IsFinite(e.TouchPressure))break;
                input.ProcessPointer(new(e.Type==0x700?PointerEventKind.Down:e.Type==0x701?PointerEventKind.Up:PointerEventKind.Move,
                    new(e.TouchX*viewport.Width,e.TouchY*viewport.Height),e.Type==0x702?PointerButton.None:PointerButton.Primary,
                    modifiers:PointerModifiers(input),pointerId:touch,pressure:Math.Clamp(e.TouchPressure,0,1)));
                break;
            case >=0x1300 and <=0x1307:
                DispatchPen(e,input);break;
            case 0x300:
            case 0x301:
                input.ProcessKey(new(MapKey(e.ScanCode,e.KeyCode,e.Modifiers), e.Type == 0x300, Modifiers(e.Modifiers), e.Repeat != 0));
                break;
            case 0x302:
                string composition = Marshal.PtrToStringUTF8(e.Text) ?? "";
                int start = Utf16Offset(composition, Math.Max(0, e.EditStart));
                int end = Utf16Offset(composition, (int)Math.Min(int.MaxValue, (long)Math.Max(0, e.EditStart) + Math.Max(0, e.EditLength)));
                input.ProcessComposition(new(composition, start, end - start));
                break;
            case 0x304:
                int count = e.Text == 0 ? 0 : Math.Clamp(e.EditStart, 0, 65536);
                var candidates = new string[count];
                for (int i = 0; i < count; i++) candidates[i] = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(e.Text, i * IntPtr.Size)) ?? "";
                int selected = e.EditLength >= 0 && e.EditLength < count ? e.EditLength : -1;
                input.ProcessCandidates(new(candidates, selected, e.CandidatesHorizontal));
                break;
            case 0x303:
                // SDL owns this pointer; copy UTF-8 before polling another event.
                input.ProcessText(new(Marshal.PtrToStringUTF8(e.Text) ?? string.Empty));
                break;
            case 0x20d: input.ProcessPointer(new(PointerEventKind.Leave, default)); break;
            case 0x20f: input.Cancel(); break;
        }
    }
    private static void DispatchPen(Sdl.Event e,InputManager input)
    {
        var id=new PointerId(PointerDeviceKind.Pen,(uint)e.Data1);
        if(e.Type==0x1301){input.CancelPointer(id);return;}
        if(e.Type==0x1300)return;
        if(!float.IsFinite(e.MouseX)||!float.IsFinite(e.MouseY))return;
        var previous=input.GetPointer(id);float pressure=previous?.Pressure??0,tiltX=previous?.TiltX??0,tiltY=previous?.TiltY??0;
        bool eraser=(e.PenState&(1u<<30))!=0||(e.Type is 0x1302 or 0x1303&&e.PenButton!=0);
        if(e.Type==0x1307)
        {
            if(!float.IsFinite(e.PenValue))return;
            if(e.PenAxis==0)pressure=Math.Clamp(e.PenValue,0,1);
            if(e.PenAxis==1)tiltX=e.PenValue;if(e.PenAxis==2)tiltY=e.PenValue;
        }
        PointerEventKind kind=e.Type is 0x1302 or 0x1304?PointerEventKind.Down:e.Type is 0x1303 or 0x1305?PointerEventKind.Up:PointerEventKind.Move;
        PointerButton button=e.Type is 0x1302 or 0x1303?PointerButton.Primary:e.Type is 0x1304 or 0x1305?(e.PenButton==1?PointerButton.Secondary:e.PenButton==2?PointerButton.Middle:PointerButton.None):PointerButton.None;
        input.ProcessPointer(new(kind,new(e.MouseX,e.MouseY),button,modifiers:PointerModifiers(input),pointerId:id,pressure:pressure,isEraser:eraser,tiltX:tiltX,tiltY:tiltY));
    }
    private static int Utf16Offset(string text, int characters)
    {
        int offset = 0;
        foreach (var rune in text.EnumerateRunes()) { if (characters-- <= 0) break; offset += rune.Utf16SequenceLength; }
        return offset;
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

    private static KeyModifiers PointerModifiers(InputManager input)=>input.Modifiers|Modifiers(Sdl.SDL_GetModState());
    private static Key MapKey(int scan,uint keyCode,ushort modifiers)
    {
        // Navigation may be supplied by a keypad or a platform-remapped key.
        Key logical=keyCode switch
        {
            0x4000004a=>Key.Home,0x4000004d=>Key.End,0x4000004b=>Key.PageUp,0x4000004e=>Key.PageDown,
            0x40000050=>Key.Left,0x4000004f=>Key.Right,0x40000052=>Key.Up,0x40000051=>Key.Down,
            _=>Key.Unknown
        };
        if (logical!=Key.Unknown) return logical;
        if ((modifiers&0x1000)==0)
        {
            Key keypad=scan switch {89=>Key.End,95=>Key.Home,91=>Key.PageDown,97=>Key.PageUp,92=>Key.Left,94=>Key.Right,90=>Key.Down,96=>Key.Up,99=>Key.Delete,98=>Key.Insert,_=>Key.Unknown};
            if (keypad!=Key.Unknown) return keypad;
        }
        return scan switch
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
}

