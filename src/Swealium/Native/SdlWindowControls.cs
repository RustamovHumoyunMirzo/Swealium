using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace Swealium.Native;
internal static partial class Sdl
{
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial nint SDL_GetWindowTitle(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowTitle(nint window, string title);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial ulong SDL_GetWindowFlags(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GetWindowPosition(nint window, out int x, out int y);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowPosition(nint window, int x, int y);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowSize(nint window, int width, int height);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GetWindowSizeInPixels(nint window, out int width, out int height);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GetWindowMinimumSize(nint window, out int width, out int height);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowMinimumSize(nint window, int width, int height);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GetWindowMaximumSize(nint window, out int width, out int height);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowMaximumSize(nint window, int width, int height);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_HideWindow(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_MinimizeWindow(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_MaximizeWindow(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_RestoreWindow(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowFullscreen(nint window, [MarshalAs(UnmanagedType.I1)] bool value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowResizable(nint window, [MarshalAs(UnmanagedType.I1)] bool value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowBordered(nint window, [MarshalAs(UnmanagedType.I1)] bool value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowAlwaysOnTop(nint window, [MarshalAs(UnmanagedType.I1)] bool value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowFocusable(nint window, [MarshalAs(UnmanagedType.I1)] bool value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowMouseGrab(nint window, [MarshalAs(UnmanagedType.I1)] bool value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowKeyboardGrab(nint window, [MarshalAs(UnmanagedType.I1)] bool value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial float SDL_GetWindowOpacity(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowOpacity(nint window, float opacity);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial float SDL_GetWindowDisplayScale(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial float SDL_GetWindowPixelDensity(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_FlashWindow(nint window, int operation);
}

