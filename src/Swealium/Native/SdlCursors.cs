using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
namespace Swealium.Native;

internal static partial class Sdl
{
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")]
    internal static partial nint SDL_CreateSystemCursor(int kind);
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")]
    internal static partial nint SDL_CreateColorCursor(nint surface,int hotX,int hotY);
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")]
    internal static partial nint SDL_CreateSurfaceFrom(int width,int height,uint format,nint pixels,int pitch);
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")]
    internal static partial void SDL_DestroySurface(nint surface);
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")]
    internal static partial void SDL_DestroyCursor(nint cursor);
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")][return:MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetCursor(nint cursor);
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")]
    internal static partial nint SDL_GetDefaultCursor();
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")]
    internal static partial nint SDL_GetMouseFocus();
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")][return:MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_ShowCursor();
    [UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][LibraryImport("SDL3")][return:MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_HideCursor();
}
