using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace Swealium.Native;

internal static partial class Sdl
{
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial uint SDL_GetWindowProperties(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SDL_GetPointerProperty(uint properties, string name, nint defaultValue);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial long SDL_GetNumberProperty(uint properties, string name, long defaultValue);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial nint SDL_GetWindowParent(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")] [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_PushEvent(in Event value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial uint SDL_CreateProperties();
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial void SDL_DestroyProperties(uint properties);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetStringProperty(uint properties, string name, string value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetNumberProperty(uint properties, string name, long value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetBooleanProperty(uint properties, string name, [MarshalAs(UnmanagedType.I1)] bool value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetPointerProperty(uint properties, string name, nint value);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    internal static partial nint SDL_CreateWindowWithProperties(uint properties);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_ShowWindow(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_RaiseWindow(nint window);
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("SDL3")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_ShowMessageBox(in MessageBoxData data, out int buttonId);
    [StructLayout(LayoutKind.Sequential)]
    internal struct MessageBoxButton
    {
        internal uint Flags;
        internal int Id;
        internal nint Text;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct MessageBoxData
    {
        internal uint Flags;
        internal nint Window, Title, Message;
        internal int ButtonCount;
        internal nint Buttons, ColorScheme;
    }
}

