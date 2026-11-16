using SkiaSharp;
using Swealium.Graphics;
using Swealium.UI;
using Swealium.UI.Effects;
namespace Swealium.Tests;

internal static class EffectTests
{
    private static void Check(bool value,string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T:Exception { try { action(); } catch(T) { return; } throw new InvalidOperationException("Expected "+typeof(T).Name); }
    private static void Place(Widget widget,Size available)
    { widget.Measure(available); widget.Arrange(new(widget.X,widget.Y,float.IsNaN(widget.Width)?available.Width:widget.Width,float.IsNaN(widget.Height)?available.Height:widget.Height)); }
    private sealed class Hook(string name, List<string> calls) : WidgetEffect
    {
        internal Action<DrawingContext,Widget>? Before;
        internal DrawingContext? Captured;
        public override void PrepareDraw(DrawingContext context,Widget widget) => calls.Add(name+" prepare");
        public override IDisposable Begin(DrawingContext context,Widget widget) { calls.Add(name+" begin"); Captured=context; return context.PushTranslation(default); }
        public override void PreDraw(DrawingContext context,Widget widget) { calls.Add(name+" pre"); Before?.Invoke(context,widget); }
        public override void PostDraw(DrawingContext context,Widget widget) => calls.Add(name+" post");
    }
    internal static void Lifecycle()
    {
        using var bitmap=new SKBitmap(100,80); using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        var calls=new List<string>(); var widget=new DrawingCanvas((_,_)=>calls.Add("draw")){Width=80,Height=60};
        var outer=new Hook("outer",calls);var inner=new Hook("inner",calls);widget.Effects.Add(outer);widget.Effects.Add(inner);
        Place(widget,new(100,80));int saved=surface.Canvas.SaveCount;WidgetRenderer.Render(surface.Canvas,widget);
        Check(string.Join(",",calls)=="outer prepare,inner prepare,outer begin,outer pre,inner begin,inner pre,draw,inner post,outer post","Preparation precedes effect scopes, which nest and unwind in reverse");
        Check(surface.Canvas.SaveCount==saved,"Effect scopes restore canvas state");Throws<ObjectDisposedException>(()=>outer.Captured!.FillEllipse(new(0,0,4,4),Color.White));
        calls.Clear();inner.IsEnabled=false;WidgetRenderer.Render(surface.Canvas,widget);Check(!calls.Any(c=>c.StartsWith("inner")),"Disabled effects have no hooks or scopes");
        outer.Before=(_,w)=>{Throws<InvalidOperationException>(()=>w.Effects.Clear());Throws<InvalidOperationException>(()=>w.Effects.Add(inner));};WidgetRenderer.Render(surface.Canvas,widget);
        outer.Before=(_,_)=>throw new ApplicationException("effect failure");Throws<ApplicationException>(()=>WidgetRenderer.Render(surface.Canvas,widget));Check(surface.Canvas.SaveCount==saved,"Hook failure releases all valid scopes and preserves the exception");
        outer.Before=(c,_)=>{c.PushClip(new(0,0,4,4));throw new ApplicationException("effect failure");};
        // A leaking custom scope is recovered at the context boundary, even if its outer Dispose rejects bad nesting.
        Throws<InvalidOperationException>(()=>WidgetRenderer.Render(surface.Canvas,widget));
        Check(surface.Canvas.SaveCount==saved,"Failed effects recover all native save state");
        widget.Effects.Clear();widget.Effects.Add(new LayerBlur(2));widget.Paint+=(_,_)=>throw new ApplicationException("draw failure");
        Throws<ApplicationException>(()=>WidgetRenderer.Render(surface.Canvas,widget));Check(surface.Canvas.SaveCount==saved,"Draw failure also restores effect layers");
        Throws<ArgumentNullException>(()=>new DrawingCanvas().Effects.Add(null!));Throws<ArgumentOutOfRangeException>(()=>new LayerBlur(float.NaN));
        Throws<ArgumentOutOfRangeException>(()=>new BackdropBlur(-1));Throws<ArgumentException>(()=>new DropShadow{Color=Gradient.Horizontal(Color.Black,Color.White)});
        Throws<ArgumentException>(()=>BlurGradient.Horizontal(new(0,2),new(.8f,4),new(.2f,3)));
        Throws<ArgumentOutOfRangeException>(()=>BlurGradient.Linear(new(0,0),new(1,0),[new(0,0),new(1,8)],quality:1));
        Throws<ArgumentException>(()=>new ColorMatrix([1,2]));
    }
    internal static void LayersAndShadows()
    {
        using var bitmap=new SKBitmap(100,80);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        var widget=new Container{X=25,Y=20,Width=20,Height=20,Background=new(255,0,0)};widget.Effects.Add(new LayerBlur(3));Place(widget,new(100,80));
        surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,widget);
        Check(bitmap.GetPixel(22,30).Alpha>0&&bitmap.GetPixel(35,30).Red>240,"Layer blur expands after the widget's own clip without losing source color");
        widget.Effects.Clear();widget.Effects.Add(new LayerBlur(new Size(4,0)));surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,widget);
        Check(bitmap.GetPixel(22,30).Alpha>0&&bitmap.GetPixel(35,18).Alpha==0,"Anisotropic blur can smooth one axis only");
        var shape=new DrawingCanvas((c,_)=>c.FillEllipse(new(2,2,12,12),new(255,0,0))){X=20,Y=20,Width=20,Height=20};
        shape.Effects.Add(new DropShadow{Offset=new(18,0),Radius=default,Color=new(0,0,255)});Place(shape,new(100,80));surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,shape);
        Check(bitmap.GetPixel(46,28).Blue>240&&bitmap.GetPixel(39,21).Alpha==0&&bitmap.GetPixel(28,28).Red>240,"Shadow follows source alpha, extends outside bounds and preserves foreground");
        ((DropShadow)shape.Effects[0]).ShadowOnly=true;surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,shape);Check(bitmap.GetPixel(28,28).Alpha==0&&bitmap.GetPixel(46,28).Blue>240,"ShadowOnly omits foreground");
        widget.Effects.Clear();widget.Effects.Add(new ColorFilter(ColorMatrix.Grayscale()));surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,widget);
        var gray=bitmap.GetPixel(35,30);Check(Math.Abs(gray.Red-gray.Green)<=1&&Math.Abs(gray.Red-gray.Blue)<=1&&gray.Alpha==255,"Color matrix applies to complete output");
        shape.Effects.Clear();var grayscale=new ColorFilter(ColorMatrix.Grayscale());var blueShadow=new DropShadow{Offset=new(18,0),Radius=default,Color=new(0,0,255)};
        shape.Effects.Add(grayscale);shape.Effects.Add(blueShadow);surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,shape);
        var filteredShadow=bitmap.GetPixel(46,28);Check(filteredShadow.Red==filteredShadow.Blue&&filteredShadow.Alpha>240,"First entry filters the completed inner shadow");
        shape.Effects.Clear();shape.Effects.Add(blueShadow);shape.Effects.Add(grayscale);surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,shape);
        Check(bitmap.GetPixel(46,28).Blue>240&&bitmap.GetPixel(46,28).Red==0,"An outer shadow keeps its color when its input alone is grayscaled");
        var group=new Container{Width=40,Height=40,Background=new(255,0,0),Layout=LayoutMode.Absolute,Outline=new(2,new(0,0,255)),ClipToBounds=false};
        group.Children.Add(new Container{X=10,Y=10,Width=15,Height=15,Background=new(0,255,0),Stroke=new(2,new(0,0,255))});group.Effects.Add(grayscale);Place(group,new(100,80));
        surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,group);
        foreach(var p in new[]{bitmap.GetPixel(5,5),bitmap.GetPixel(17,17),bitmap.GetPixel(10,17),bitmap.GetPixel(40,17)})Check(p.Alpha>0&&Math.Abs(p.Red-p.Blue)<=1&&Math.Abs(p.Red-p.Green)<=1,"Subtree, child stroke and outer outline share one effect layer");
        var parent=new Container{Width=35,Height=60,Layout=LayoutMode.Absolute};parent.Children.Add(shape);LayoutEngine.Layout(parent,new(35,60));surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,parent);
        Check(bitmap.GetPixel(46,28).Alpha==0,"Ancestor clipping still contains expanded effects");
    }
    private static void Stripes(SKCanvas canvas,byte alpha=255)
    {canvas.Clear(SKColors.Transparent);using var paint=new SKPaint{Color=new(255,0,0,alpha)};canvas.DrawRect(0,0,50,80,paint);paint.Color=new(0,0,255,alpha);canvas.DrawRect(50,0,50,80,paint);}
    internal static void Backdrops()
    {
        using var bitmap=new SKBitmap(100,80);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        var widget=new Container{X=30,Y=15,Width=40,Height=50,Radii=new(12)};widget.Effects.Add(new BackdropBlur(6));Place(widget,new(100,80));
        Stripes(surface.Canvas);WidgetRenderer.Render(surface.Canvas,widget);
        var blurred=bitmap.GetPixel(48,35);Check(blurred.Red>40&&blurred.Blue>40&&blurred.Alpha==255,"Transparent widget reveals blurred previous content");
        Check(bitmap.GetPixel(30,15).Red==255&&bitmap.GetPixel(72,35).Blue==255,"Backdrop is clipped to rounded widget bounds");
        widget.Effects.Insert(0,new DropShadow());Stripes(surface.Canvas);WidgetRenderer.Render(surface.Canvas,widget);
        Check(bitmap.GetPixel(48,35).Blue>40,"Backdrop preparation samples prior content before shadow/content isolation opens");widget.Effects.RemoveAt(0);
        widget.Background=Color.White;Stripes(surface.Canvas);WidgetRenderer.Render(surface.Canvas,widget);Check(bitmap.GetPixel(48,35)==SKColors.White,"Opaque background covers blur");
        widget.Background=new(255,255,255,128);Stripes(surface.Canvas);WidgetRenderer.Render(surface.Canvas,widget);var tinted=bitmap.GetPixel(48,35);Check(tinted.Blue>150&&tinted.Green>=127,"Background color transparency reveals the blurred backdrop");
        widget.Background=Color.White;widget.Opacity=.5f;Stripes(surface.Canvas);WidgetRenderer.Render(surface.Canvas,widget);
        var faded=bitmap.GetPixel(48,35);Check(faded.Red>250&&faded.Blue>=125&&faded.Blue<=130,"Widget opacity fades the opaque widget over the original, without exposing its hidden blur");
        widget.Background=Color.Transparent;Stripes(surface.Canvas);WidgetRenderer.Render(surface.Canvas,widget);Check(bitmap.GetPixel(48,35).Blue>15,"Transparent backdrop remains available inside an opacity group");
        Stripes(surface.Canvas,128);WidgetRenderer.Render(surface.Canvas,widget);Check(Math.Abs(bitmap.GetPixel(48,35).Alpha-128)<=2,"Backdrop/opacity composition preserves translucent destination alpha");
        widget.Opacity=1;var parent=new Container{Width=100,Height=80,Opacity=.5f,Layout=LayoutMode.Absolute};parent.Children.Add(widget);LayoutEngine.Layout(parent,new(100,80));
        Stripes(surface.Canvas);WidgetRenderer.Render(surface.Canvas,parent);Check(bitmap.GetPixel(48,35).Blue>15,"Ancestor opacity groups preserve descendant backdrop access");
        parent.Background=Color.Black;Stripes(surface.Canvas);WidgetRenderer.Render(surface.Canvas,parent);Check(bitmap.GetPixel(48,35).Blue<3,"Backdrop samples the parent's current isolated group, not a root-surface snapshot");
    }
    internal static void Progressive()
    {
        using var bitmap=new SKBitmap(120,80);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        var gradient=BlurGradient.Linear(new(0,0),new(1,0),[new(0,0),new(.3f,0),new(1,6)],quality:8);
        var widget=new DrawingCanvas((c,_)=>{for(int x=0;x<100;x+=4)c.FillRectangle(new(x,0,2,60),Color.White);}){X=10,Y=10,Width=100,Height=60};
        widget.Effects.Add(new ProgressiveBlur(gradient));Place(widget,new(120,80));surface.Canvas.Clear(SKColors.Black);WidgetRenderer.Render(surface.Canvas,widget);
        int contrastLeft=Math.Abs(bitmap.GetPixel(18,40).Red-bitmap.GetPixel(20,40).Red),contrastRight=Math.Abs(bitmap.GetPixel(98,40).Red-bitmap.GetPixel(100,40).Red);
        Check(contrastLeft>220&&contrastRight<50,$"Progressive blur varies actual blur radius, left/right contrast {contrastLeft}/{contrastRight}");
        foreach(var ramp in new[]{BlurGradient.Radial(new(.5f,.5f),.5f,[new(0,0),new(1,4)]),BlurGradient.Elliptical(new(.5f,.5f),new(.5f,.7f),[new(0,0),new(1,4)]),BlurGradient.Sweep(new(.5f,.5f),[new(0,0),new(1,4)]),BlurGradient.Diamond(new(.5f,.5f),new(.5f,.5f),[new(0,0),new(1,4)]),BlurGradient.Square(new(.5f,.5f),new(.5f,.5f),[new(0,0),new(1,4)])})
        {((ProgressiveBlur)widget.Effects[0]).Gradient=ramp;surface.Canvas.Clear(SKColors.Black);WidgetRenderer.Render(surface.Canvas,widget);Check(bitmap.GetPixel(60,40).Alpha==255,"Spatial blur gradients retain opaque compositing");}
        var back=new Container{Width=100,Height=80};back.Effects.Add(new ProgressiveBlur(BlurGradient.Vertical(new(0,0),new(.3f,0),new(1,6)),BlurTarget.Backdrop));Place(back,new(120,80));Stripes(surface.Canvas);WidgetRenderer.Render(surface.Canvas,back);
        Check(bitmap.GetPixel(48,5).Blue<3&&bitmap.GetPixel(48,70).Blue>30,"Progressive backdrop keeps one region sharp and blurs another");
        using var vector=VectorImage.Create(new(0,0,40,40),c=>{using(c.PushBlur(new(2,2)))c.FillRectangle(new(10,10,20,20),Color.White);});using var raster=vector.Rasterize(40,40);Check(raster.CopyPixels().Where((_,i)=>i%4==3).Any(a=>a>0&&a<255),"Public effect scopes also work in vector recordings");
        var context=new SkiaDrawingContext(surface.Canvas);int save=surface.Canvas.SaveCount;var clip=context.PushClip(new(0,0,8,8));var move=context.PushTranslation(new(2,2));Throws<InvalidOperationException>(()=>clip.Dispose());move.Dispose();clip.Dispose();clip.Dispose();Check(surface.Canvas.SaveCount==save,"Drawing scopes require LIFO and dispose idempotently");
        var abandoned=context.PushBlur(new(2,2));context.End();Check(surface.Canvas.SaveCount==save,"End recovers undisposed public drawing scopes");abandoned.Dispose();Throws<ObjectDisposedException>(()=>context.PushBlur(default));
    }
    internal static void Gpu(SKSurface surface)
    {
        var widget=new Container{Width=32,Height=32,Background=new(255,0,0),Opacity=.5f};widget.Effects.Add(new BackdropBlur(2));widget.Effects.Add(new ColorFilter(ColorMatrix.Grayscale()));Place(widget,new(32,32));surface.Canvas.Clear(SKColors.Blue);WidgetRenderer.Render(surface.Canvas,widget);
        using(var image=surface.Snapshot())using(var pixels=SKBitmap.FromImage(image)){var p=pixels.GetPixel(16,16);Check(p.Blue>p.Red&&p.Red>15,"GPU supports filters and backdrop group blending");}
        widget.Effects.Clear();widget.Opacity=1;widget.Background=Color.Transparent;widget.Effects.Add(new BackdropBlur(3));surface.Canvas.Clear(SKColors.Blue);
        using(var paint=new SKPaint{Color=SKColors.Red})surface.Canvas.DrawRect(0,0,16,32,paint);WidgetRenderer.Render(surface.Canvas,widget);
        using(var image=surface.Snapshot())using(var pixels=SKBitmap.FromImage(image))Check(pixels.GetPixel(14,16).Blue>30&&pixels.GetPixel(14,16).Red>30,"GPU backdrop actually mixes a sharp background edge");
        var bars=new DrawingCanvas((c,_)=>{for(int x=0;x<32;x+=4)c.FillRectangle(new(x,0,2,32),Color.White);}){Width=32,Height=32};
        bars.Effects.Add(new ProgressiveBlur(BlurGradient.Horizontal(new(0,0),new(.4f,0),new(1,4))));Place(bars,new(32,32));surface.Canvas.Clear(SKColors.Black);WidgetRenderer.Render(surface.Canvas,bars);
        using(var image=surface.Snapshot())using(var pixels=SKBitmap.FromImage(image))Check(Math.Abs(pixels.GetPixel(4,16).Red-pixels.GetPixel(6,16).Red)>220&&Math.Abs(pixels.GetPixel(24,16).Red-pixels.GetPixel(26,16).Red)<65,"GPU progressive blur varies spatial contrast");
    }
}
