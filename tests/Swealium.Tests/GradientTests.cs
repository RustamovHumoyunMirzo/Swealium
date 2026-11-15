using Swealium.Graphics;
using Swealium.UI;
using SkiaSharp;
using Bitmap=Swealium.Graphics.Bitmap;
namespace Swealium.Tests;
internal static class GradientTests
{
    private static readonly Color Red=new(255,0,0),Blue=new(0,0,255);
    private static readonly GradientStop[] Stops=[new(0,Red),new(1,Blue)];
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    internal static Gradient[] Kinds()=>[
        Gradient.Horizontal(Red,Blue),Gradient.Radial(new(.5f,.5f),.5f,Stops),Gradient.Elliptical(new(.5f,.5f),new(.5f,.3f),Stops),
        Gradient.Sweep(new(.5f,.5f),Stops),Gradient.TwoPointConical(new(.3f,.5f),.05f,new(.5f,.5f),.5f,Stops),
        Gradient.Diamond(new(.5f,.5f),new(.5f,.5f),Stops),Gradient.Square(new(.5f,.5f),new(.5f,.5f),Stops)];
    internal static void Gpu(SKSurface surface)
    {
        foreach(var gradient in Kinds())
        {
            surface.Canvas.Clear(SKColors.Transparent);
            var context=new SkiaDrawingContext(surface.Canvas);
            try{context.FillRectangle(new(0,0,32,32),gradient);}finally{context.End();}
            using var image=surface.Snapshot();using var pixels=SKBitmap.FromImage(image);
            Check(pixels.GetPixel(10,16)!=pixels.GetPixel(28,16),"GPU gradient variation for "+gradient.Kind);
        }
        surface.Canvas.Clear(SKColors.Transparent);
        var box=new Container{Radii=new(6),Stroke=new(5,Gradient.Horizontal(Red,Blue))};LayoutEngine.Layout(box,new(32,32));
        WidgetRenderer.Render(surface.Canvas,box);
        using var borderImage=surface.Snapshot();using var borderPixels=SKBitmap.FromImage(borderImage);
        Check(borderPixels.GetPixel(8,2).Red>150&&borderPixels.GetPixel(24,2).Blue>150,"GPU vertex-shader border colors");
    }
    internal static void Shapes()
    {
        foreach(var gradient in Kinds())
        {
            using var bitmap=new Bitmap(100,80,gradient);
            var colors=bitmap.CopyPixels();Check(colors.Where((_,i)=>i%4==0).Distinct().Count()>12,"Continuous ramp for "+gradient.Kind);
            bitmap.Clear();bitmap.Draw(c=>c.FillEllipse(new(0,0,100,80),gradient));Check(bitmap.GetPixel(50,40).A>240,"Ellipse shader for "+gradient.Kind);
        }
        using var horizontal=new Bitmap(100,40,Gradient.Horizontal(Red,Blue));
        Check(horizontal.GetPixel(2,20).R>240&&horizontal.GetPixel(97,20).B>240,"Relative horizontal endpoints");
        using var sub=new Bitmap(100,40);sub.Draw(c=>c.FillRectangle(new(20,0,60,40),Gradient.Horizontal(Red,Blue)));
        Check(sub.GetPixel(22,20).R>240&&sub.GetPixel(77,20).B>240,"Relative paint origin");
        foreach(var spread in Enum.GetValues<GradientSpread>())
        {
            sub.Clear(Gradient.Linear(new(10,0),new(30,0),Stops,spread,GradientUnits.Absolute));
            var a=sub.GetPixel(5,20);var b=sub.GetPixel(45,20);
            if(spread==GradientSpread.Pad)Check(a.R>240&&b.B>240,"Pad clamps");
            if(spread==GradientSpread.Decal)Check(a.A==0&&b.A==0,"Decal clears outside");
            if(spread==GradientSpread.Repeat)Check(b.R>30&&b.B>30,"Repeat reuses ramp");
            if(spread==GradientSpread.Reflect)Check(a.R>150&&b.R>150,"Reflect reverses ramp");
        }
        Color opacity=Gradient.Horizontal(Red,Blue);opacity=opacity with{A=100};sub.Clear(opacity);Check(sub.GetPixel(50,20).A is >=99 and <=101,"Color alpha multiplies gradient alpha");
        using var path=new VectorPath();path.AddRectangle(new(10,0,80,40));sub.Clear();sub.Draw(c=>c.FillPath(path,Gradient.Horizontal(Red,Blue)));
        Check(sub.GetPixel(12,20).R>240&&sub.GetPixel(87,20).B>240,"Path bounds select coordinate domain");
        sub.SetPixel(20,20,Gradient.Horizontal(Red,Blue));Check(sub.GetPixel(20,20).R>150,"Pixel operations sample the bitmap domain");
        var stops=Stops.ToArray();var saved=Gradient.Horizontal(Red,Blue);var immutable=Gradient.Linear(new(0,0),new(1,0),stops);stops[0]=new(0,Color.Black);Check(immutable.Stops[0].Color==Red,"Stops are snapshotted");
        Check(((Color)saved).Gradient==saved&&default(Color).A==0,"Color compatibility and gradient identity");
        Throws<ArgumentException>(()=>Gradient.Horizontal(Red));Throws<ArgumentException>(()=>Gradient.Linear(new(),new(),Stops));
        Throws<ArgumentException>(()=>Gradient.Horizontal(saved,Blue));Throws<ArgumentException>(()=>Gradient.Radial(new(),1,[new(.8f,Red),new(.2f,Blue)]));
        Throws<ArgumentOutOfRangeException>(()=>Gradient.Radial(new(),float.NaN,Stops));
    }
    internal static void TextAndStrokes()
    {
        using var bitmap=new Bitmap(200,80);
        bitmap.Draw(c=>c.DrawText("MMMM",new Point(10,50),Gradient.Horizontal(Red,Blue),40));
        var bytes=bitmap.CopyPixels();Check(bytes.Where((_,i)=>i%4==0).Max()>220&&bytes.Where((_,i)=>i%4==2).Max()>220,"Gradient glyphs span the paragraph");
        using(var mixed=new TextLayout(new TextRun[]{new("MM",new(){FontSize=36,Foreground=Gradient.Horizontal(Red,Blue)}),new("MM",new(){FontSize=36,Foreground=Gradient.Horizontal(new(0,255,0),new(255,255,0))})}))
        {
            bitmap.Clear();bitmap.Draw(c=>c.DrawTextLayout(mixed,new(0,0)));
            Check(bitmap.CopyPixels().Where((_,i)=>i%4==1).Max()>220,"Distinct gradient text runs keep their own styles");
        }
        using var layout=new TextLayout("WWWW",new(){Foreground=Color.Transparent,Background=Gradient.Horizontal(Red,Blue),FontSize=36});
        bitmap.Clear();bitmap.Draw(c=>c.DrawTextLayout(layout,new(10,0)));
        Check(bitmap.GetPixel(12,10).R>200&&bitmap.GetPixel((int)layout.Size.Width,10).B>200,"Gradient text backgrounds");
        using var selected=new TextLayout("WWWW",new(){Foreground=Color.Transparent,FontSize=36});
        bitmap.Clear();bitmap.Draw(c=>c.DrawTextLayout(selected,new(10,0),new(0,4),Gradient.Horizontal(Red,Blue)));
        Check(bitmap.GetPixel(12,10).R>200&&bitmap.GetPixel((int)selected.Size.Width,10).B>200,"Gradient selection highlights");
        var box=new Container{Width=100,Height=70,Radii=new(12),Stroke=new Stroke(8,Gradient.Horizontal(Red,Blue))};
        LayoutEngine.Layout(box,new(100,70));
        using var raster=new SKBitmap(100,70);using var surface=SKSurface.Create(raster.Info,raster.GetPixels(),raster.RowBytes)!;
        WidgetRenderer.Render(surface.Canvas,box);
        Check(raster.GetPixel(20,2).Red>170&&raster.GetPixel(80,2).Blue>170,"Gradient border follows the full widget domain");
        foreach(var style in Enum.GetValues<StrokeStyle>())
        {
            box.Stroke=new(new StrokeSide(8,Gradient.Vertical(Red,Blue),style),new(8,Gradient.Horizontal(Blue,Red),style),new(8,Color.White,style),new(8,Color.Black,style));
            surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,box);
            Check(raster.GetPixel(50,35).Alpha==0,"Gradient strokes preserve hollow center for "+style);
        }
        using var recorded=VectorImage.Create(new(0,0,100,70),c=>c.FillRectangle(new(0,0,100,70),Gradient.Diamond(new(.5f,.5f),new(.5f,.5f),Stops)));
        using var output=recorded.Rasterize(100,70);Check(output.GetPixel(50,35).R>220&&output.GetPixel(2,35).B>220,"Recorded runtime gradient remains valid after drawing callback");
    }
}
