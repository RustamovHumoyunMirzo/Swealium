using SkiaSharp;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;
using Bitmap=Swealium.Graphics.Bitmap;
namespace Swealium.Tests;
internal static class TransformOverscrollTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Near(float actual,float expected)=>Check(Math.Abs(actual-expected)<.05f,$"Expected {expected}, got {actual}");
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    private sealed class Fill:Widget
    {public Color Color {get;set;}=new(255,0,0);public override void Draw(DrawingContext drawing)=>drawing.FillRectangle(new(0,0,ActualWidth,ActualHeight),Color,Radii);}
    internal static void Gpu(SKSurface surface)
    {
        var root=new Container{Layout=LayoutMode.Absolute};
        root.Children.Add(new Fill{X=1,Y=1,Width=10,Height=6,Transform=new(new(7,6),rotation:90,origin:new(0,0))});
        LayoutEngine.Layout(root,new(32,32));surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,root);
        using var image=surface.Snapshot();using var raster=SKBitmap.FromImage(image);
        Check(raster.GetPixel(5,10).Red>240&&raster.GetPixel(12,3).Alpha==0,"GPU widget transforms match logical raster coordinates");
    }
    internal static void CoordinatesAndPixels()
    {
        var child=new Fill{X=10,Y=15,Width=40,Height=20,Transform=new(new(5,4),new(2,1),90)};
        var root=new Container{Layout=LayoutMode.Absolute,Transform=Transform.Translate(20,10)};root.Children.Add(child);LayoutEngine.Layout(root,new(180,160));
        var layoutBounds=child.Bounds;var world=child.ToWindow(new Point(0,0));Near(world.X,65);Near(world.Y,-1);
        var local=child.ToLocal(world);Near(local.X,0);Near(local.Y,0);Near(child.TransformedBounds.Width,20);Near(child.TransformedBounds.Height,80);
        child.Transform=Transform.Translate(35,25);Check(child.Bounds==layoutBounds&&child.IsLayoutValid,"Render transforms preserve layout bounds and measure cache");
        using var bitmap=new SKBitmap(220,180);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        WidgetRenderer.Render(surface.Canvas,root);Check(bitmap.GetPixel(70,60).Red>240&&bitmap.GetPixel(30,30).Alpha==0,"Translation affects retained raster drawing");
        var input=new InputManager{Root=root};Check(input.HitTest(new(70,60))==child,"Translated hit testing follows the painted area");
        Point? routed=null;child.PointerInput+=e=>{if(e.Kind==PointerEventKind.Down)routed=e.Position;};input.ProcessPointer(new(PointerEventKind.Down,child.ToWindow(new Point(8,6)),PointerButton.Primary));
        Check(routed is not null,"Transformed input routed");Near(routed!.Value.X,8);Near(routed.Value.Y,6);input.Cancel();
        child.Transform=new(new(50,30),new(-1,1),25);var mirrored=child.ToWindow(new Point(5,5));Check(input.HitTest(mirrored)==child,"Rotation and negative scale use inverse hit geometry");
        int cancelled=0;child.PointerInput+=e=>{if(e.Kind==PointerEventKind.Cancel)cancelled++;};
        input.ProcessPointer(new(PointerEventKind.Down,mirrored,PointerButton.Primary));
        child.Transform=Transform.ScaleBy(0,1);input.Validate();Check(input.CapturedWidget is null&&cancelled==1,"Making a captured transform singular cancels interaction safely");Check(input.HitTest(new(70,60))!=child,"Singular transforms cannot receive input");Throws<InvalidOperationException>(()=>child.ToLocal(default));
        surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,root);Check(bitmap.GetPixel(70,60).Alpha==0,"Zero scale suppresses painting");
        Throws<ArgumentOutOfRangeException>(()=>child.Transform=new(){Rotation=float.NaN});
        Check(default(Transform).Scale==new Size(1,1)&&default(Transform).Origin==new Point(.5f,.5f),"Default transform is identity about the center");
    }
    internal static void ClippingAndServices()
    {
        var first=new Text{Content="Alpha",Selectable=true,X=20,Y=20,Width=120,Height=40,Transform=Transform.Rotate(8)};
        var second=new Text{Content="Beta",Selectable=true,X=20,Y=100,Width=120,Height=40,Transform=Transform.ScaleBy(1.1f,1.1f)};
        var document=new Container{Layout=LayoutMode.Absolute};document.Children.Add(first);document.Children.Add(second);LayoutEngine.Layout(document,new(350,220));
        Point At(Text label,int index){using var layout=new TextLayout(label.Content,label.Style,label.Options,label.ContentBounds.Width);var caret=layout.GetCaretRectangle(new(index));return label.ToWindow(new Point(caret.X+.01f,caret.Y+caret.Height/2));}
        var selectionInput=new InputManager{Root=document};var start=At(first,2);var end=At(second,3);
        selectionInput.ProcessPointer(new(PointerEventKind.Down,start,PointerButton.Primary,clickCount:1));selectionInput.ProcessPointer(new(PointerEventKind.Move,end));selectionInput.ProcessPointer(new(PointerEventKind.Up,end,PointerButton.Primary));
        Check(selectionInput.TextSelection.SelectedText=="pha\nBet","Shared text selection connects transformed widgets in local text coordinates");first.Transform=Transform.ScaleBy(0,1);
        Check(selectionInput.TextSelection.Ranges.Count==0,"Singular selectable fragments invalidate shared selection without inverse failures");

        var clipped=new Container{Layout=LayoutMode.Absolute,Width=80,Height=80,Radii=new(10),Transform=Transform.Rotate(25)};
        var child=new Fill{X=60,Y=20,Width=60,Height=30};clipped.Children.Add(child);LayoutEngine.Layout(clipped,new(80,80));
        var input=new InputManager{Root=clipped};Check(input.HitTest(clipped.ToWindow(new Point(90,30))) is null,"Rotated parent clipping remains local and exact");
        var editor=new TextBox{Text="hello",Width=160,Height=45,Transform=new(new(30,20),new(1.2f,1.2f),10)};
        input.Root=null;
        Swealium.SwealiumApplication.Run(new(){Width=350,Height=220},window=>
        {
            window.Content=editor;window.Input.Focus(editor);editor.Select(2,0);window.Render();
            var node=window.Accessibility.CaptureSnapshot().Nodes.Single(n=>n.Id==editor.SemanticId);
            Near(node.Bounds.X,editor.TransformedBounds.X);Near(node.Bounds.Width,editor.TransformedBounds.Width);
            Check(window.Accessibility.HitTest(editor.ToWindow(new Point(30,20)))?.Id==editor.SemanticId,"Accessibility hit testing follows transforms");
            window.Input.ProcessComposition(new("preedit"));window.Input.ProcessCandidates(new(new[]{"candidate"},0));window.Render();
            Check(editor.ToWindow(editor.TextInputArea).Height>editor.TextInputArea.Height,"IME anchor uses transformed axis-aligned bounds");window.RequestClose();
        });
    }
    private static (ScrollContainer Outer,ScrollContainer Inner,InputManager Input) Nested()
    {
        var inner=new ScrollContainer{Height=100,WheelStep=40};inner.Children.Add(new Fill{Height=300});
        var outer=new ScrollContainer{Width=240,Height=180,WheelStep=20};outer.Children.Add(inner);outer.Children.Add(new Fill{Height=350});LayoutEngine.Layout(outer,new(240,180));
        return(outer,inner,new InputManager{Root=outer});
    }
    internal static void OverscrollModes()
    {
        var (outer,inner,input)=Nested();int boundaries=0;Point residual=default;inner.BoundaryReached+=e=>{boundaries++;residual=e.UnconsumedDelta;};
        PointerEvent Wheel()=>new(PointerEventKind.Wheel,new(15,20),wheelDelta:new(0,-1));
        inner.ScrollTo(0,inner.MaximumOffset.Y-10);inner.Overscroll=OverscrollBehavior.Chain;var wheel=Wheel();input.ProcessPointer(wheel);
        Near(inner.VerticalOffset,inner.MaximumOffset.Y);Near(outer.VerticalOffset,15);Near(residual.Y,-.75f);Near(wheel.RemainingWheelDelta.Y,0);
        Check(boundaries==1,"Chain reports normalized unused input and lets a differently configured parent consume it once");
        foreach(var mode in new[]{OverscrollBehavior.Contain,OverscrollBehavior.None})
        {
            outer.ScrollTo(0,0);inner.ScrollTo(0,inner.MaximumOffset.Y);inner.Overscroll=mode;int before=boundaries;input.ProcessPointer(Wheel());
            Check(outer.VerticalOffset==0,"Containing policies suppress parent scrolling: "+mode);
            Check(boundaries-before==(mode==OverscrollBehavior.None?0:1),"None suppresses boundary feedback while Contain allows it");
        }
        outer.ScrollTo(0,0);inner.ScrollTo(0,0);inner.Overscroll=OverscrollBehavior.None;input.ProcessPointer(Wheel());Near(inner.VerticalOffset,40);Check(outer.VerticalOffset==0,"None still permits ordinary in-range scrolling");
        inner.ScrollTo(0,inner.MaximumOffset.Y);
        outer.ScrollTo(0,0);inner.Overscroll=OverscrollBehavior.Auto;input.ProcessPointer(Wheel());Check(outer.VerticalOffset==20,"Auto preserves ordinary boundary bubbling");
        inner.RequireFocusForNestedScrolling=true;inner.Overscroll=OverscrollBehavior.Chain;outer.ScrollTo(0,0);input.Focus(inner);input.ProcessPointer(Wheel());Near(outer.VerticalOffset,20);
        inner.Overscroll=OverscrollBehavior.Auto;outer.ScrollTo(0,0);input.ProcessPointer(Wheel());Check(outer.VerticalOffset==0,"Auto preserves focus-owned list/nested scroll containment");
        var list=Swealium.UI.List.Create(new[]{1,2,3},i=>new Fill{Height=60});list.Overscroll=OverscrollBehavior.None;Check(list.Overscroll==OverscrollBehavior.None,"Lists inherit overscroll behavior");
        Throws<ArgumentOutOfRangeException>(()=>inner.Overscroll=(OverscrollBehavior)99);
    }
}
