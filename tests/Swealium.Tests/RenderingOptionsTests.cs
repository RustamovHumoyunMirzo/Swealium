using SkiaSharp;
using Swealium;
using Swealium.Graphics;
using Swealium.Native;
using Swealium.UI;
using Bitmap = Swealium.Graphics.Bitmap;
namespace Swealium.Tests;

internal static class RenderingOptionsTests
{
    private static void Check(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
    internal static void AntiAliasing()
    {
        bool previous=SwealiumApplication.AntiAliasing;
        try
        {
            using var bitmap=new Bitmap(40,40);
            DrawingContext? expired=null;
            SwealiumApplication.AntiAliasing=false;
            bitmap.Draw(c=>{Check(!c.AntiAliasing,"Application default reaches offscreen contexts");expired=c;c.FillEllipse(new(3.2f,3.2f,29.4f,29.4f),Color.White);});
            Check(!Partial(bitmap),"Disabled ellipse edges have no coverage smoothing");
            bool threw=false;try{expired!.AntiAliasing=true;}catch(ObjectDisposedException){threw=true;}Check(threw,"Expired context setter is rejected");
            bitmap.Clear();bitmap.Draw(c=>{c.AntiAliasing=true;c.FillEllipse(new(3.2f,3.2f,29.4f,29.4f),Color.White);});
            Check(Partial(bitmap),"Context override enables smoothed edges");
            bitmap.Draw(c=>Check(!c.AntiAliasing,"Context override does not leak"));
            SwealiumApplication.AntiAliasing=true;
            bitmap.Clear();bitmap.Draw(c=>{Check(c.AntiAliasing,"Updated default reaches new contexts");c.AntiAliasing=false;c.DrawText("S",new Point(2,30),Color.White,30);});
            Check(!Partial(bitmap),"Aliased shaped text has binary glyph coverage");
            bitmap.Clear();bitmap.Draw(c=>c.DrawText("S",new Point(2,30),Color.White,30));
            Check(Partial(bitmap),"Shaped text follows enabled default");
        }
        finally { SwealiumApplication.AntiAliasing=previous; }
    }
    private static bool Partial(Bitmap bitmap)=>bitmap.CopyPixels().Where((_,i)=>i%4==3).Any(a=>a is >0 and <255);
    internal static void SoftwareAndFallback()
    {
        bool previous=SwealiumApplication.HardwareAcceleration;
        try
        {
            foreach(bool enabled in new[]{false,true})
            {
                SwealiumApplication.HardwareAcceleration=enabled;
                SwealiumApplication.Run(new(){Width=64,Height=64},window=>
                {
                    Check(!window.IsHardwareAccelerated,"Dummy driver uses raster");
                    Check((window.HardwareAccelerationFallbackReason is not null)==enabled,"Fallback reason distinguishes disabled and unavailable GPU");
                    bool threw=false;try{SwealiumApplication.HardwareAcceleration=!enabled;}catch(InvalidOperationException){threw=true;}Check(threw,"Backend policy cannot change during Run");
                    window.Content=new Button{Text="Raster"};window.Render();
                    window.Size=new(80,72);window.Render();window.RequestClose();
                });
            }
        }
        finally { SwealiumApplication.HardwareAcceleration=previous; }
    }
    internal static void NativeGpu()
    {
        bool previous=SwealiumApplication.HardwareAcceleration;
        SwealiumApplication.HardwareAcceleration=true;
        try
        {
            // This explicit test requires a graphical desktop and fails rather than silently skipping GPU setup.
            SwealiumApplication.Run(new(){Width=100,Height=80,Visible=true},first=>
            {
                Check(first.IsHardwareAccelerated,first.HardwareAccelerationFallbackReason??"GPU requested");
                first.Content=new Button{Text="GPU",Opacity=.7f};
                using(var second=SwealiumApplication.CreateWindow(new(){Width=90,Height=70}))
                {
                    Check(second.IsHardwareAccelerated,second.HardwareAccelerationFallbackReason??"Second GPU window");
                    second.Content=new Button{Text="Second"};
                    Action<DrawingContext,Size> nested=(drawing,_)=>
                    {
                        nint current=Sdl.SDL_GL_GetCurrentContext();
                        second.Render();
                        Check(Sdl.SDL_GL_GetCurrentContext()==current,"Nested window rendering restores the outer context");
                        using(var temporary=SwealiumApplication.CreateWindow(new(){Visible=false,Width=32,Height=32}))
                            Check(Sdl.SDL_GL_GetCurrentContext()==current,"Window creation restores the outer context");
                        Check(Sdl.SDL_GL_GetCurrentContext()==current,"Window disposal restores the outer context");
                        drawing.FillEllipse(new(1,1,10,10),Color.White);
                    };
                    first.Paint+=nested;first.Render();first.Paint-=nested;
                    for(int i=0;i<3;i++){first.Render();second.Render();}
                    first.Size=new(120,90);first.Render();second.Render();
                    Check(first.HardwareAccelerationFallbackReason is null,"GPU has no fallback");
                }
                first.Render();
                // Read back a known GPU-rendered pixel and verify orientation and framebuffer wrapping.
                GpuWindowSurface.Configure();
                nint native=Sdl.Check(Sdl.SDL_CreateWindow("GPU readback",32,32,0xA));
                try
                {
                    using var gpu=new GpuWindowSurface(native);
                    var surface=gpu.Acquire()!;
                    surface.Canvas.Clear(SKColors.Blue);
                    var context=new SkiaDrawingContext(surface.Canvas);
                    try{context.FillRectangle(new(0,0,32,16),new(255,0,0));}finally{context.End();}
                    using var image=surface.Snapshot();using var raster=SKBitmap.FromImage(image);
                    Check(raster.GetPixel(8,4).Red>240&&raster.GetPixel(8,24).Blue>240,"GPU pixels and top-left drawing orientation");
                    GradientTests.Gpu(surface);
                    TransformOverscrollTests.Gpu(surface);
                    PointerOutlineTests.Gpu(surface);
                    DrawingCanvasTests.Gpu(surface);
                    EffectTests.Gpu(surface);
                    RippleTests.Gpu(surface);
                    gpu.Present();
                }
                finally { Sdl.SDL_DestroyWindow(native); }
                first.RequestClose();
            });
        }
        finally { SwealiumApplication.HardwareAcceleration=previous; }
    }
}
