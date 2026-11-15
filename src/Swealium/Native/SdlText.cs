using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
namespace Swealium.Native;

internal static partial class Sdl
{
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial ushort SDL_GetModState();
    [StructLayout(LayoutKind.Sequential)]
    internal struct InputRect { public int X, Y, Width, Height; }
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")] [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetTextInputArea(nint window, in InputRect rect, int cursor);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")] [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_ClearComposition(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)] [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetHint(string name, string value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)] [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetClipboardText(string text);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial nint SDL_GetClipboardText();
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial void SDL_free(nint memory);
}
