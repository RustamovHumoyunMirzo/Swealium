using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
namespace Swealium.Native;

internal static partial class Sdl
{
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial void SDL_SetMainReady();
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_Init(uint flags);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial void SDL_Quit();
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial nint SDL_GetError();
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SDL_CreateWindow(string title, int width, int height, ulong flags);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial void SDL_DestroyWindow(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial uint SDL_GetWindowID(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")] [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GetWindowSize(nint window, out int width, out int height);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")] [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_StartTextInput(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")] [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_StopTextInput(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial nint SDL_CreateRenderer(nint window, nint name);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial void SDL_DestroyRenderer(nint renderer);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GetRenderOutputSize(nint renderer, out int width, out int height);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetRenderVSync(nint renderer, int vsync);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial nint SDL_CreateTexture(nint renderer, uint format, int access, int width, int height);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial void SDL_DestroyTexture(nint texture);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_UpdateTexture(nint texture, nint rect, nint pixels, int pitch);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_RenderClear(nint renderer);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_RenderTexture(nint renderer, nint texture, nint source, nint destination);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_RenderPresent(nint renderer);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_PollEvent(out Event value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial void SDL_Delay(uint milliseconds);
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    internal struct Event
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(16)] public uint WindowId;
        [FieldOffset(16)] public ulong TouchId;
        [FieldOffset(24)] public ulong FingerId;
        [FieldOffset(32)] public float TouchX;
        [FieldOffset(36)] public float TouchY;
        [FieldOffset(48)] public float TouchPressure;
        [FieldOffset(52)] public uint TouchWindowId;
        [FieldOffset(24)] public uint PenState;
        [FieldOffset(36)] public byte PenButton;
        [FieldOffset(36)] public int PenAxis;
        [FieldOffset(40)] public float PenValue;
        public readonly uint RoutedWindowId=>Type is >=0x700 and <=0x703?TouchWindowId:WindowId;
        [FieldOffset(20)] public int Data1;
        [FieldOffset(24)] public int Data2;
        [FieldOffset(24)] public byte Button;
        [FieldOffset(26)] public byte Clicks;
        [FieldOffset(28)] public float MouseX;
        [FieldOffset(32)] public float MouseY;
        [FieldOffset(24)] public float WheelX;
        [FieldOffset(28)] public float WheelY;
        [FieldOffset(32)] public uint WheelDirection;
        [FieldOffset(36)] public float WheelMouseX;
        [FieldOffset(40)] public float WheelMouseY;
        [FieldOffset(24)] public int ScanCode;
        [FieldOffset(28)] public uint KeyCode;
        [FieldOffset(32)] public ushort Modifiers;
        [FieldOffset(37)] public byte Repeat;
        // const char* follows windowID; pointer alignment varies by architecture.
        [FieldOffset(24)] public nint Text64;
        [FieldOffset(20)] public nint Text32;
        public readonly nint Text => IntPtr.Size == 8 ? Text64 : Text32;
        [FieldOffset(32)] public int EditStart64;
        [FieldOffset(24)] public int EditStart32;
        [FieldOffset(36)] public int EditLength64;
        [FieldOffset(28)] public int EditLength32;
        [FieldOffset(40)] public byte CandidatesHorizontal64;
        [FieldOffset(32)] public byte CandidatesHorizontal32;
        public readonly int EditStart => IntPtr.Size == 8 ? EditStart64 : EditStart32;
        public readonly int EditLength => IntPtr.Size == 8 ? EditLength64 : EditLength32;
        public readonly bool CandidatesHorizontal => (IntPtr.Size == 8 ? CandidatesHorizontal64 : CandidatesHorizontal32) != 0;
    }

    internal static void Check(bool success)
    {
        if (!success) throw new InvalidOperationException($"SDL3: {Marshal.PtrToStringUTF8(SDL_GetError())}");
    }
    internal static nint Check(nint handle) { Check(handle != 0); return handle; }
}


