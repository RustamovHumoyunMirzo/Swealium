using SkiaSharp;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;
namespace Swealium.Tests;
internal static class PointerOutlineTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    private sealed class Fill:Widget
    {internal Color Color=new(0,0,255);public override void Draw(DrawingContext context)=>context.FillRectangle(new(0,0,ActualWidth,ActualHeight),Color,Radii);}
    internal static void GhostPointers()
    {
        var root=new Container{Layout=LayoutMode.Overlay};var bottom=new Button{Text="Under",HeightSizing=SizingMode.Fill};var top=new Fill{HeightSizing=SizingMode.Fill,ZIndex=2};root.Children.Add(bottom);root.Children.Add(top);
        LayoutEngine.Layout(root,new(120,80));var input=new InputManager{Root=root};int topDown=0,clicked=0,bubbled=0;
        top.PointerInput+=e=>{if(e.Kind==PointerEventKind.Down)topDown++;};bottom.Click+=()=>clicked++;root.PointerInput+=e=>{if(e.Kind==PointerEventKind.Down)bubbled++;};
        void Click(){input.ProcessPointer(new(PointerEventKind.Down,new(30,30),PointerButton.Primary));input.ProcessPointer(new(PointerEventKind.Up,new(30,30),PointerButton.Primary));}
        Check(top.ConsumePointer&&top.IsHitTestVisible,"ConsumePointer defaults true and shares the existing hit-test flag");Click();Check(topDown==1&&clicked==0&&bubbled==1,"Consuming blocks lower siblings while retaining ancestor routing");
        top.ConsumePointer=false;Click();Check(topDown==1&&clicked==1,"A ghost draws normally but lets pointer targeting reach lower siblings");
        top.IsHitTestVisible=true;Check(top.ConsumePointer,"Compatibility alias works both ways");top.ConsumePointer=false;
        int cancelled=0;bottom.PointerInput+=e=>{if(e.Kind==PointerEventKind.Cancel)cancelled++;};input.ProcessPointer(new(PointerEventKind.Down,new(30,30),PointerButton.Primary));
        bottom.ConsumePointer=false;input.Validate();Check(cancelled==1&&input.CapturedWidget is null,"Becoming a ghost cancels outstanding pointer capture");Check(!input.CapturePointer(PointerId.Mouse,bottom),"Ghosts cannot be manually captured");
        Check(input.FocusedWidget==bottom,"Ghost policy does not disable keyboard focus");bottom.ConsumePointer=true;
        root.ConsumePointer=false;Check(input.HitTest(new(30,30)) is null,"A ghost parent excludes its subtree from pointer targeting");
    }
    internal static void PixelsAndStyles()
    {
        using var bitmap=new SKBitmap(220,120);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        var root=new Container{Layout=LayoutMode.Absolute};var child=new Fill{X=30,Y=30,Width=100,Height=40,Radii=new(8,0,12,4),Outline=new(3,new(255,0,0),offset:5)};root.Children.Add(child);LayoutEngine.Layout(root,new(220,120));
        void Draw(){surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,root);}
        Draw();Check(bitmap.GetPixel(70,23).Red>240&&bitmap.GetPixel(70,27).Alpha==0&&bitmap.GetPixel(70,33).Blue>240,"Outline offset is a gap outside the widget's own clip");
        Check(bitmap.GetPixel(23,23).Alpha==0,"Expanded outline follows rounded corner geometry");
        child.Opacity=.5f;Draw();Check(bitmap.GetPixel(70,23).Alpha is >=126 and <=129&&bitmap.GetPixel(70,33).Alpha is >=126 and <=129,"The same group opacity applies to fill and external outline");child.Opacity=1;
        child.Radii=default;child.Color=Color.Transparent;
        foreach(var style in Enum.GetValues<StrokeStyle>())
        {
            child.Outline=new(6,Color.White,style);Draw();int painted=Enumerable.Range(30,100).Count(x=>bitmap.GetPixel(x,27).Alpha>20);
            if(style==StrokeStyle.Solid)Check(painted==100,"Solid outline is continuous");
            if(style is StrokeStyle.Dashed or StrokeStyle.Dotted)Check(painted>0&&painted<100,"Outline patterns have marks and gaps: "+style);
            if(style==StrokeStyle.Double)Check(bitmap.GetPixel(70,25).Alpha>200&&bitmap.GetPixel(70,27).Alpha==0&&bitmap.GetPixel(70,29).Alpha>200,"Double outline has two bands and a clear middle");
            if(style==StrokeStyle.Hidden)Check(painted==0,"Hidden outline draws nothing");
        }
        child.Color=new(0,0,255);child.Outline=new(2,new(255,0,0),offset:-4);Draw();Check(bitmap.GetPixel(70,32).Red>240&&bitmap.GetPixel(70,28).Alpha==0,"Negative offset insets the outline");
        child.Outline=new(4,Gradient.Horizontal(new(255,0,0),new(0,0,255)));Draw();Check(bitmap.GetPixel(40,28).Red>180&&bitmap.GetPixel(120,28).Blue>180,"Outline colors support gradients");
        var input=new InputManager{Root=root};Check(input.HitTest(new(70,28))!=child,"Decorative outlines do not enlarge the hit region");Check(child.ActualWidth==100&&child.ActualHeight==40,"Outline does not reserve layout space");
        Throws<ArgumentOutOfRangeException>(()=>_ =new Outline(-1,Color.White));Throws<ArgumentOutOfRangeException>(()=>_ =new Outline(1,Color.White,offset:float.NaN));Throws<ArgumentOutOfRangeException>(()=>_ =new Outline(1,Color.White,(StrokeStyle)99));
    }
    internal static void ParentClipAndTransforms()
    {
        using var bitmap=new SKBitmap(180,110);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        var root=new Container{Layout=LayoutMode.Absolute};var panel=new Container{X=50,Y=20,Width=70,Height=60,Layout=LayoutMode.Absolute};var child=new Fill{X=0,Y=15,Width=35,Height=25,Outline=new(3,new(255,0,0),offset:4)};panel.Children.Add(child);root.Children.Add(panel);LayoutEngine.Layout(root,new(180,110));
        surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,root);Check(bitmap.GetPixel(45,45).Alpha==0&&bitmap.GetPixel(70,30).Red>240,"Ancestor clips constrain the outline even though the owner's clip does not");
        panel.ClipToBounds=false;surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,root);Check(bitmap.GetPixel(45,45).Red>240,"Disabling ancestor clipping permits external outline painting");
        child.Transform=Transform.Translate(15,0);surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,root);Check(bitmap.GetPixel(60,45).Red>240,"Outline uses the owner's render transform");
    }
    internal static void Gpu(SKSurface surface)
    {
        var root=new Container{Layout=LayoutMode.Absolute};root.Children.Add(new Fill{X=10,Y=10,Width=12,Height=12,Outline=new(2,new(255,0,0),offset:2)});LayoutEngine.Layout(root,new(32,32));
        surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,root);using var image=surface.Snapshot();using var pixels=SKBitmap.FromImage(image);
        Check(pixels.GetPixel(15,7).Red>240&&pixels.GetPixel(15,9).Alpha==0&&pixels.GetPixel(15,12).Blue>240,"GPU outlines preserve the outer band, gap and fill");
    }
}
