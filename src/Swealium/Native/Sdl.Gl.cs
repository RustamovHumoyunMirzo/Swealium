using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
namespace Swealium.Native;

internal static partial class Sdl
{
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])]
    internal static partial nint SDL_GL_GetCurrentContext();
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])]
    internal static partial nint SDL_GL_GetCurrentWindow();
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])]
    internal static partial void SDL_GL_ResetAttributes();
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][return:MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GL_SetAttribute(int attribute,int value);
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][return:MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GL_GetAttribute(int attribute,out int value);
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])]
    internal static partial nint SDL_GL_CreateContext(nint window);
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][return:MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GL_MakeCurrent(nint window,nint context);
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][return:MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GL_DestroyContext(nint context);
    [LibraryImport("SDL3",StringMarshalling=StringMarshalling.Utf8)][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])]
    internal static partial nint SDL_GL_GetProcAddress(string name);
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][return:MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GL_SetSwapInterval(int interval);
    [LibraryImport("SDL3")][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])][return:MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_GL_SwapWindow(nint window);
    [LibraryImport("SDL3",EntryPoint="SDL_CreateRenderer",StringMarshalling=StringMarshalling.Utf8)][UnmanagedCallConv(CallConvs=[typeof(CallConvCdecl)])]
    internal static partial nint SDL_CreateNamedRenderer(nint window,string name);
}
