using System.Runtime.InteropServices;
using SkiaSharp;
using Swealium.Native;
namespace Swealium.Graphics;

/// <summary>One GL context and Skia device per window. Never shared across windows or threads.</summary>
internal sealed class GpuWindowSurface : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void GetInteger(uint name,out int value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void BindFramebuffer(uint target,uint framebuffer);
    private BindFramebuffer? bindFramebuffer;
    private BindFramebuffer? bindRenderbuffer;
    private readonly nint window;
    private nint context;
    private GRGlInterface? gl;
    private GRContext? device;
    private GRBackendRenderTarget? target;
    private SKSurface? surface;
    private GetInteger? getInteger;
    private int width,height,framebuffer;
    internal readonly struct CurrentScope : IDisposable
    {
        private readonly nint window, context;
        private readonly bool active;
        internal CurrentScope(nint exclude)
        {
            active=true;
            context=Sdl.SDL_GL_GetCurrentContext();
            window=Sdl.SDL_GL_GetCurrentWindow();
            if(context==exclude) { context=0; window=0; }
        }
        public void Dispose() { if(active) _=Sdl.SDL_GL_MakeCurrent(window,context); }
    }
    internal static void Configure()
    {
        Sdl.SDL_GL_ResetAttributes();
        bool es=OperatingSystem.IsAndroid()||OperatingSystem.IsIOS();
        Sdl.Check(Sdl.SDL_GL_SetAttribute(17,3)); // major
        Sdl.Check(Sdl.SDL_GL_SetAttribute(18,es?0:OperatingSystem.IsMacOS()?2:3));
        Sdl.Check(Sdl.SDL_GL_SetAttribute(21,es?4:1)); // ES / core
        Sdl.Check(Sdl.SDL_GL_SetAttribute(5,1)); // double buffered
        Sdl.Check(Sdl.SDL_GL_SetAttribute(6,0)); // no depth
        Sdl.Check(Sdl.SDL_GL_SetAttribute(7,8)); // stencil for Skia paths
        Sdl.Check(Sdl.SDL_GL_SetAttribute(15,1)); // accelerated visual
    }
    internal GpuWindowSurface(nint window)
    {
        this.window=window;
        using var previous=new CurrentScope(0);
        try
        {
            context=Sdl.Check(Sdl.SDL_GL_CreateContext(window));
            MakeCurrent();
            _=Sdl.SDL_GL_SetSwapInterval(0);
            gl=GRGlInterface.Create(Sdl.SDL_GL_GetProcAddress) ?? throw new NotSupportedException("Skia could not bind OpenGL.");
            device=GRContext.CreateGl(gl) ?? throw new NotSupportedException("Skia GPU rendering is unavailable.");
            nint address=Sdl.SDL_GL_GetProcAddress("glGetIntegerv");
            if(address==0)throw new NotSupportedException("OpenGL query functions are unavailable.");
            getInteger=Marshal.GetDelegateForFunctionPointer<GetInteger>(address);
            bindFramebuffer=Marshal.GetDelegateForFunctionPointer<BindFramebuffer>(Sdl.SDL_GL_GetProcAddress("glBindFramebuffer"));
            if(OperatingSystem.IsIOS())
                bindRenderbuffer=Marshal.GetDelegateForFunctionPointer<BindFramebuffer>(Sdl.SDL_GL_GetProcAddress("glBindRenderbuffer"));
            _=Acquire(); // Validate the swapchain before publishing this window.
        }
        catch { Dispose(); throw; }
    }
    private void MakeCurrent()=>Sdl.Check(Sdl.SDL_GL_MakeCurrent(window,context));
    internal SKSurface? Acquire()
    {
        MakeCurrent();
        if(device!.IsAbandoned)throw new InvalidOperationException("The GPU device was lost.");
        Sdl.Check(Sdl.SDL_GetWindowSizeInPixels(window,out int w,out int h));
        if(w<=0||h<=0)return null;
        int fbo=OperatingSystem.IsIOS() ? checked((int)Sdl.SDL_GetNumberProperty(Sdl.SDL_GetWindowProperties(window),"SDL.window.uikit.opengl.framebuffer",0)) : 0;
        bindFramebuffer!(0x8D40,unchecked((uint)fbo));
        device.ResetContext();
        if(surface is null||w!=width||h!=height||fbo!=framebuffer)
        {
            ReleaseSurface();
            getInteger!(0x80A9,out int samples);
            Sdl.Check(Sdl.SDL_GL_GetAttribute(7,out int stencil));
            target=new(w,h,samples,stencil,new GRGlFramebufferInfo(unchecked((uint)fbo),0x8058)); // RGBA8
            surface=SKSurface.Create(device,target,GRSurfaceOrigin.BottomLeft,SKColorType.Rgba8888)
                ?? throw new NotSupportedException("Skia could not wrap the window framebuffer.");
            width=w;height=h;framebuffer=fbo;
        }
        return surface;
    }
    internal (int Width,int Height) Size=>(width,height);
    internal void Present()
    {
        MakeCurrent();
        device!.Flush(true,false);
        bindFramebuffer!(0x8D40,unchecked((uint)framebuffer));
        if(OperatingSystem.IsIOS())
            bindRenderbuffer!(0x8D41,checked((uint)Sdl.SDL_GetNumberProperty(Sdl.SDL_GetWindowProperties(window),"SDL.window.uikit.opengl.renderbuffer",0)));
        Sdl.Check(Sdl.SDL_GL_SwapWindow(window));
    }
    internal void ReleaseSurface()
    {
        using var previous=new CurrentScope(0);
        if(context!=0)MakeCurrent();
        surface?.Dispose();surface=null;
        target?.Dispose();target=null;
    }
    public void Dispose()
    {
        if(context==0)return;
        using var previous=new CurrentScope(context);
        // If the platform lost the context, abandon GPU resources rather than using another window's device.
        bool current=Sdl.SDL_GL_MakeCurrent(window,context);
        if(!current)device?.AbandonContext();
        surface?.Dispose();surface=null;target?.Dispose();target=null;
        device?.Dispose();device=null;gl?.Dispose();gl=null;
        _=Sdl.SDL_GL_DestroyContext(context);context=0;
    }
}
