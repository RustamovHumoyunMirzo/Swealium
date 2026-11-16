using System.Diagnostics;
using System.Runtime.InteropServices;
using SkiaSharp;
using Swealium;
using Swealium.Graphics;
using Swealium.Native;
using Swealium.UI;
namespace Swealium.Tests;
internal static class DrawingCanvasTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    internal static void Buffers()
    {
        Point[] source=[new(0,2),new(5,2),new(10,2)];var points=new PointBuffer(source);source[0]=new(100,100);
        Check(points[0]==new Point(0,2)&&points.Bounds==new Rect(0,2,10,0),"Buffers own a validated source copy");
        var storage=points.NativePoints;long version=points.Version;points.Update([new(1,1),new(2,2),new(3,3)]);
        Check(ReferenceEquals(storage,points.NativePoints)&&points.Version==version+1,"Streaming updates reuse storage when count is unchanged");
        Throws<ArgumentOutOfRangeException>(()=>points.Update([new(float.NaN,0)]));Check(points.Count==3&&points[0]==new Point(1,1),"Failed updates retain the previous data");
        var projected=points.Project(new(10,20,100,80));Check(projected[0]==new Point(10,20)&&projected[2]==new Point(110,100),"Projection maps data into logical canvas bounds once");
        var flat=new PointBuffer(new Point[]{new(0,2),new(10,2)}).Project(new(0,0,100,50));Check(flat[0].Y==25,"Flat source axes project to the viewport center");
        Throws<ArgumentOutOfRangeException>(()=>_ =points[-1]);
        var large=new Point[100_000];for(int i=0;i<large.Length;i++)large[i]=new(i,MathF.Sin(i*.01f));large[43210]=new(43210,100);large[43211]=new(43211,-100);
        var full=new PointBuffer(large);var reduced=full.ReduceForWidth(800);
        Check(reduced.Count<=3202&&reduced[0]==full[0]&&reduced[reduced.Count-1]==full[full.Count-1],"Reduction bounds dense plot work by screen width and retains endpoints");
        Check(reduced.CopyPoints().Any(p=>p==large[43210])&&reduced.CopyPoints().Any(p=>p==large[43211]),"Reduction preserves narrow positive and negative spikes");
        var visible=full.ReduceForWidth(300,new(40_000,-100,10_000,200));Check(visible[0].X==39999&&visible[visible.Count-1].X==50001,"Viewport reduction keeps adjacent samples for connecting segments");
        var unordered=new PointBuffer(new Point[]{new(2,0),new(1,0)});Throws<InvalidOperationException>(()=>unordered.ReduceForWidth(20));
    }
    internal static void PixelsAndLifetime()
    {
        using var bitmap=new SKBitmap(80,60);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        DrawingContext? captured=null;int paints=0;var points=new PointBuffer(new Point[]{new(10,10),new(30,30)});
        var canvas=new DrawingCanvas((drawing,bounds)=>{paints++;captured=drawing;drawing.AntiAliasing=false;drawing.DrawPoints(points,new(255,0,0),8,PointShape.Square,clip:new(0,0,20,20));}){Width=80,Height=60};
        LayoutEngine.Layout(canvas,new(80,60));surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,canvas);
        Check(paints==1&&bitmap.GetPixel(7,7).Red>240&&bitmap.GetPixel(30,30).Alpha==0,"Immediate callback batches markers and honors a local clip");
        Throws<ObjectDisposedException>(()=>captured!.DrawPoints(points,Color.White));
        var context=new SkiaDrawingContext(surface.Canvas);surface.Canvas.Clear(SKColors.Transparent);context.AntiAliasing=false;
        context.DrawPoints(new PointBuffer(new Point[]{new(10,10)}),Color.White,8,PointShape.Round);Check(bitmap.GetPixel(6,6).Alpha==0&&bitmap.GetPixel(10,10).Alpha>240,"Round marker batches use circular coverage");
        surface.Canvas.Clear(SKColors.Transparent);var triangle=new PointBuffer(new Point[]{new(10,10),new(30,10),new(30,30)});context.DrawPolyline(triangle,Color.White,2,closed:true);
        Check(bitmap.GetPixel(20,20).Alpha>0&&bitmap.GetPixel(20,15).Alpha==0,"Closed polylines connect endpoints without filling polygons");
        surface.Canvas.Clear(SKColors.Transparent);context.DrawLines(new PointBuffer(new Point[]{new(5,40),new(15,40),new(30,40),new(40,40)}),Color.White,2);
        Check(bitmap.GetPixel(10,40).Alpha>0&&bitmap.GetPixel(22,40).Alpha==0&&bitmap.GetPixel(35,40).Alpha>0,"Paired line batches do not connect separate segments");
        Throws<ArgumentException>(()=>context.DrawLines(triangle,Color.White));Throws<ArgumentOutOfRangeException>(()=>context.DrawPoints(points,Color.White,-1));context.End();
        bool entered=false;var recursive=new DrawingCanvas();recursive.Paint+=(c,_)=>{if(!entered){entered=true;Throws<InvalidOperationException>(()=>recursive.Draw(c));}};LayoutEngine.Layout(recursive,new(80,60));WidgetRenderer.Render(surface.Canvas,recursive);
        var failed=new DrawingCanvas((_,_)=>throw new InvalidOperationException("paint failed"));LayoutEngine.Layout(failed,new(80,60));Throws<InvalidOperationException>(()=>WidgetRenderer.Render(surface.Canvas,failed));Throws<InvalidOperationException>(()=>WidgetRenderer.Render(surface.Canvas,failed));
    }
    internal static void LargeBatches()
    {
        var data=Scatter(100_000,256,256);var points=new PointBuffer(data);var canvas=new DrawingCanvas((drawing,_)=>{drawing.AntiAliasing=false;drawing.DrawPoints(points,Color.White);});
        LayoutEngine.Layout(canvas,new(256,256));using var bitmap=new SKBitmap(256,256);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        for(int i=0;i<3;i++)WidgetRenderer.Render(surface.Canvas,canvas);
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;i++)WidgetRenderer.Render(surface.Canvas,canvas);long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Check(allocated<128*1024,$"Prepared 100K frames allocate O(1) managed memory, got {allocated} bytes for ten frames");
        using var vector=VectorImage.Create(new(0,0,256,256),drawing=>drawing.DrawPoints(points,new(255,0,0),2));points.Update(new Point[data.Length]);using var output=vector.Rasterize(256,256);
        Check(output.CopyPixels().Where((_,i)=>i%4==3).Count(a=>a>0)>10_000,"Recorded batches retain their geometry after a mutable point buffer changes");
    }
    internal static void AutomaticDetail()
    {
        var source=new Point[100_000];for(int i=0;i<source.Length;i++)source[i]=new((float)i/(source.Length-1)*800,20);
        var points=new PointBuffer(source);DrawingDetail seen=DrawingDetail.Exact;
        var canvas=new DrawingCanvas((drawing,_)=>{seen=drawing.PolylineDetail;drawing.AntiAliasing=false;drawing.DrawPolyline(points,Color.White);});LayoutEngine.Layout(canvas,new(800,128));
        using var bitmap=new SKBitmap(800,128);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        for(int i=0;i<3;i++)WidgetRenderer.Render(surface.Canvas,canvas);
        Check(seen==DrawingDetail.Automatic,"Canvas enables automatic dense polyline detail by default");
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;i++)WidgetRenderer.Render(surface.Canvas,canvas);
        Check(GC.GetAllocatedBytesForCurrentThread()-before<64*1024,"Automatic reduction is cached instead of reallocating sampled geometry each frame");
        for(int i=0;i<source.Length;i++)source[i]=source[i] with{Y=80};points.Update(source);surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,canvas);
        Check(bitmap.GetPixel(100,79).Alpha>0||bitmap.GetPixel(100,80).Alpha>0,"Updating a source buffer invalidates cached automatic geometry");
        Check(bitmap.GetPixel(100,19).Alpha==0&&bitmap.GetPixel(100,20).Alpha==0,"Stale reduced data is not replayed after update");
        canvas.Detail=DrawingDetail.Exact;using var output=new Swealium.Graphics.Bitmap(800,128);output.Draw(drawing=>{drawing.PolylineDetail=DrawingDetail.Exact;canvas.Draw(drawing);Check(drawing.PolylineDetail==DrawingDetail.Exact,"Canvas restores the caller's detail policy");});
        Check(seen==DrawingDetail.Exact,"Exact opt-out reaches the drawing callback");
    }
    internal static void Gpu(SKSurface surface)
    {
        var points=new PointBuffer(new Point[]{new(8,8),new(24,24)});
        var canvas=new DrawingCanvas((drawing,_)=>drawing.DrawPoints(points,new(255,0,0),4));LayoutEngine.Layout(canvas,new(32,32));surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,canvas);
        using var image=surface.Snapshot();using var pixels=SKBitmap.FromImage(image);Check(pixels.GetPixel(8,8).Red>240&&pixels.GetPixel(24,24).Red>240,"GPU immediate canvas point batches render correctly");
    }
    private static Point[] Scatter(int count,int width,int height)
    {var result=new Point[count];var random=new Random(42);for(int i=0;i<count;i++)result[i]=new(random.NextSingle()*width,random.NextSingle()*height);return result;}
    private static Point[] Curve(int count,int width,int height)
    {var result=new Point[count];for(int i=0;i<count;i++)result[i]=new((float)i/(count-1)*width,height*.5f+MathF.Sin(i*.0002f)*height*.3f+MathF.Sin(i*.007f)*height*.04f);return result;}
    internal static void Benchmark()
    {
        Console.WriteLine($"Canvas CPU raster benchmark: {RuntimeInformation.RuntimeIdentifier}, {RuntimeInformation.FrameworkDescription}; 800x450; 3 warmup + 16 measured frames; excludes SDL presentation");
        using var bitmap=new SKBitmap(800,450);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        foreach(int count in new[]{100_000,1_000_000})
        {
            var scatter=new PointBuffer(Scatter(count,800,450));Measure($"scatter {count:N0}, square 1, AA off",scatter,false,false,surface,null);
            var curve=new PointBuffer(Curve(count,800,450));Measure($"polyline auto {count:N0}, width 1, AA on",curve,true,true,surface,null);
            if(count==100_000)Measure($"polyline exact {count:N0}, AA on",curve,true,true,surface,null);
            long start=Stopwatch.GetTimestamp();var reduced=curve.ReduceForWidth(800);Console.WriteLine($"reduce {count:N0} -> {reduced.Count:N0}: {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F2} ms preparation");
            Measure($"polyline reduced {reduced.Count:N0}, AA on",reduced,true,true,surface,null);
        }
    }
    private static void Measure(string name,PointBuffer points,bool line,bool aa,SKSurface surface,Action? submit)
    {
        var canvas=new DrawingCanvas((drawing,_)=>{drawing.AntiAliasing=aa;if(line)drawing.DrawPolyline(points,new(80,200,240));else drawing.DrawPoints(points,new(80,200,240));}){Detail=name.Contains("exact",StringComparison.Ordinal)?DrawingDetail.Exact:DrawingDetail.Automatic};LayoutEngine.Layout(canvas,new(800,450));
        void Frame(){surface.Canvas.Clear(SKColors.Black);WidgetRenderer.Render(surface.Canvas,canvas);surface.Canvas.Flush();submit?.Invoke();}
        for(int i=0;i<3;i++)Frame();var samples=new double[16];long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<samples.Length;i++){long start=Stopwatch.GetTimestamp();Frame();samples[i]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;}
        long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Array.Sort(samples);
        Console.WriteLine($"{name}: median {samples[8]:F2} ms, p95 {samples[15]:F2} ms; managed {bytes/16:N0} B/frame");
    }
    internal static void BenchmarkGpu()
    {
        Console.WriteLine("Canvas GPU benchmark: 800x450; synchronous GPU flush; excludes swap/compositor; 3 warmup + 16 measured frames");
        SwealiumApplication.Run(new(){Visible=false},window=>
        {
            GpuWindowSurface.Configure();nint native=Sdl.Check(Sdl.SDL_CreateWindow("Canvas benchmark",800,450,0xA));
            try
            {
                using var gpu=new GpuWindowSurface(native);var surface=gpu.Acquire()!;
                Action submit=()=>surface.Flush(true,true);
                foreach(int count in new[]{100_000,1_000_000})
                {
                    var scatter=new PointBuffer(Scatter(count,800,450));Measure($"scatter {count:N0}, square 1, AA off",scatter,false,false,surface,submit);
                    var curve=new PointBuffer(Curve(count,800,450));Measure($"polyline auto {count:N0}, width 1, AA on",curve,true,true,surface,submit);
                    if(count==100_000)Measure($"polyline exact {count:N0}, AA on",curve,true,true,surface,submit);
                    var reduced=curve.ReduceForWidth(800);Measure($"polyline reduced {reduced.Count:N0}, AA on",reduced,true,true,surface,submit);
                }
            }
            finally{Sdl.SDL_DestroyWindow(native);}window.RequestClose();
        });
    }
}
