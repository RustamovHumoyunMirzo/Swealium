using System.Runtime.InteropServices;
using Swealium.Input;
namespace Swealium.Native;

internal static class SdlCursorManager
{
    private static readonly Dictionary<Cursor,nint> handles=[];
    private static Cursor? current;
    private static nint currentWindow;
    private static int thread;
    internal static int ResourceCount=>handles.Count;
    private static void Verify()
    {if(thread!=0&&thread!=Environment.CurrentManagedThreadId)throw new InvalidOperationException("Native cursor operations require the host UI thread.");}
    internal static bool Apply(nint window,Cursor cursor)
    {
        Verify();thread=Environment.CurrentManagedThreadId;
        if(cursor.IsDisposed)cursor=Cursor.Default;
        if(ReferenceEquals(current,cursor)){currentWindow=window;return !handles.TryGetValue(cursor,out nint cached)||cached!=0;}
        current=cursor;currentWindow=window;
        if(cursor==Cursor.Hidden)return Sdl.SDL_HideCursor();
        _=Sdl.SDL_ShowCursor();
        if(!handles.TryGetValue(cursor,out nint handle))
        {
            handle=cursor.Kind==CursorKind.Custom?CreateBitmap(cursor):Sdl.SDL_CreateSystemCursor((int)cursor.Kind);
            handles.Add(cursor,handle);
        }
        nint selected=handle!=0?handle:Sdl.SDL_GetDefaultCursor();
        return selected!=0&&Sdl.SDL_SetCursor(selected)&&handle!=0;
    }
    private static nint CreateBitmap(Cursor cursor)
    {
        var bitmap=cursor.Bitmap!;byte[] rgba=bitmap.CopyPixels();var pin=GCHandle.Alloc(rgba,GCHandleType.Pinned);nint surface=0;
        try
        {
            uint format=BitConverter.IsLittleEndian?0x16762004u:0x16462004u;
            surface=Sdl.SDL_CreateSurfaceFrom(bitmap.Width,bitmap.Height,format,pin.AddrOfPinnedObject(),checked(bitmap.Width*4));
            return surface==0?0:Sdl.SDL_CreateColorCursor(surface,(int)cursor.HotSpot.X,(int)cursor.HotSpot.Y);
        }
        finally{if(surface!=0)Sdl.SDL_DestroySurface(surface);pin.Free();}
    }
    internal static void Release(Cursor cursor)
    {
        if(!handles.ContainsKey(cursor))return;Verify();
        if(ReferenceEquals(current,cursor))Reset();
        if(handles.Remove(cursor,out nint handle)&&handle!=0)Sdl.SDL_DestroyCursor(handle);
    }
    internal static void ReleaseWindow(nint window){if(thread!=0&&currentWindow==window){Verify();Reset();}}
    private static void Reset()
    {nint standard=Sdl.SDL_GetDefaultCursor();if(standard!=0)_=Sdl.SDL_SetCursor(standard);_=Sdl.SDL_ShowCursor();current=null;currentWindow=0;}
    internal static void Shutdown()
    {
        if(thread==0)return;Verify();Reset();
        foreach(nint handle in handles.Values)if(handle!=0)Sdl.SDL_DestroyCursor(handle);
        handles.Clear();thread=0;
    }
}
