using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;
using SkiaSharp;
namespace Swealium.Tests;

internal static class ScrollStrokeTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Near(float actual,float expected) => Check(Math.Abs(actual-expected)<.03f,$"Expected {expected}, got {actual}");
    internal static void SelectionEdges()
    {
        var editor = new TextBox { Text = "alpha beta gamma", Width=300, Height=50 };
        LayoutEngine.Layout(editor,new(300,50)); var input=new InputManager{Root=editor};input.Focus(editor);
        editor.Select(6,0);input.ProcessKey(new(Key.End,true,KeyModifiers.Shift));
        Check(editor.SelectedText=="beta gamma", "Shift+End extends to the final character");
        input.ProcessKey(new(Key.Home,true,KeyModifiers.Shift));
        Check(editor.SelectedText=="alpha ", "Shift+Home preserves the original anchor and reverses selection");
        editor.Multiline=true;editor.Text="first line\nsecond line\n";
        editor.Select(14,0);input.ProcessKey(new(Key.End,true,KeyModifiers.Shift));
        Check(editor.SelectedText=="ond line", "End excludes the newline");
        input.ProcessKey(new(Key.Home,true,KeyModifiers.Shift));
        Check(editor.SelectedText=="sec", "Home selects to the current line start");
        input.ProcessKey(new(Key.End,true,KeyModifiers.Control|KeyModifiers.Shift));
        Check(editor.Selection==new TextSelection(14,editor.Text.Length-14), "Ctrl+Shift+End extends to the document end");
        input.ProcessKey(new(Key.Home,true,KeyModifiers.Control|KeyModifiers.Shift));
        Check(editor.Selection==new TextSelection(0,14), "Ctrl+Shift+Home extends to document start");
        editor.Text="one two three four five six seven eight nine";editor.Width=110;LayoutEngine.Layout(editor,new(110,100));
        using var layout=new TextLayout(editor.Text, editor.Style, maxWidth:editor.ContentBounds.Width);
        int edge=layout.FromCodePoint(layout.NativeBlock.Lines[0].End);
        editor.Select(1,0);input.ProcessKey(new(Key.End,true,KeyModifiers.Shift));
        Check(editor.CaretIndex==edge,"Wrapped End reaches first visual line's boundary");
        input.ProcessKey(new(Key.Home,true,KeyModifiers.Shift));
        Check(editor.CaretIndex==0 && editor.Selection==new TextSelection(0,1),"Wrapped End affinity keeps subsequent Home on the same line");
        editor.IsReadOnly=true;editor.Select(2,0);input.ProcessKey(new(Key.End,true,KeyModifiers.Shift));
        Check(editor.Selection.Length>0,"Read-only editor supports extended selection");
        editor.IsReadOnly=false;editor.Multiline=false;editor.Text="";editor.Select(0,0);
        input.ProcessKey(new(Key.Home,true,KeyModifiers.Shift));input.ProcessKey(new(Key.End,true,KeyModifiers.Shift));
        Check(editor.Selection==default,"Empty paragraph line edges remain valid");
    }
    internal static void ScrollLayout()
    {
        var scroll=new ScrollContainer{Padding=new(10),Gap=5};
        for(int i=0;i<5;i++) scroll.Children.Add(new Box{Height=40});
        LayoutEngine.Layout(scroll,new(120,120));
        Check(scroll.VerticalScrollBar.IsVisible&&!scroll.HorizontalScrollBar.IsVisible,"Auto shows only overflowing enabled axis");
        Near(scroll.Viewport.Width,88);Near(scroll.Viewport.Height,100);Near(scroll.Extent.Height,220);Near(scroll.MaximumOffset.Y,120);
        int events=0;scroll.ScrollChanged+=_=>events++;
        scroll.ScrollTo(100,55);Near(scroll.Offset.X,0);Near(scroll.Offset.Y,55);Near(scroll.Children[0].Bounds.Y,-45);
        Check(events==1,"Changing offset emits one notification and immediately rearranges children");
        scroll.ScrollTo(0,1000);Near(scroll.Offset.Y,120);scroll.ScrollTo(0,-10);Near(scroll.Offset.Y,0);
        scroll.ScrollBy(0,20);scroll.Children.Clear();LayoutEngine.Layout(scroll,new(120,120));
        Check(scroll.Offset.Y==0&&!scroll.VerticalScrollBar.IsVisible,"Removing content clamps offset and removes auto bar");
        scroll.VerticalScrollBarVisibility=ScrollBarVisibility.Always;LayoutEngine.Layout(scroll,new(120,120));
        Check(scroll.VerticalScrollBar.IsVisible&&scroll.VerticalScrollBar.Thumb.Height==scroll.VerticalScrollBar.Track.Height,"Always bar fills track without overflow");
        scroll.VerticalScrollBarVisibility=ScrollBarVisibility.Hidden;scroll.Children.Add(new Box{Height=400});LayoutEngine.Layout(scroll,new(120,120));
        Near(scroll.Viewport.Width,100);scroll.ScrollTo(0,50);Check(scroll.Offset.Y==50&&!scroll.VerticalScrollBar.IsVisible,"Hidden bars preserve scrolling and reclaim gutter");
        scroll.Direction=ScrollDirection.Both;scroll.Children[0].Width=300;LayoutEngine.Layout(scroll,new(120,120));
        Check(scroll.HorizontalScrollBar.IsVisible,"Horizontal overflow is enabled independently");
        scroll.HorizontalScrollBarVisibility=ScrollBarVisibility.Auto;scroll.VerticalScrollBarVisibility=ScrollBarVisibility.Auto;
        scroll.OverlayScrollBars=true;LayoutEngine.Layout(scroll,new(120,120));Near(scroll.Viewport.Width,100);Near(scroll.Viewport.Height,100);
        scroll.BringIntoView(new Rect(270,370,20,20));Near(scroll.Offset.X,190);Near(scroll.Offset.Y,290);
        scroll.Direction=ScrollDirection.None;LayoutEngine.Layout(scroll,new(120,120));Check(scroll.Offset==default&&!scroll.VerticalScrollBar.IsVisible&&!scroll.HorizontalScrollBar.IsVisible,"Disabled axes reset offsets");
        scroll.Direction=ScrollDirection.Vertical;scroll.Measure(new(float.PositiveInfinity,float.PositiveInfinity));
        Check(float.IsFinite(scroll.DesiredSize.Width)&&float.IsFinite(scroll.DesiredSize.Height),"Unbounded measure returns finite content demand");
        var coupled=new ScrollContainer{Direction=ScrollDirection.Both,Layout=LayoutMode.Overlay};
        coupled.Children.Add(new Box{Width=95,Height=110});LayoutEngine.Layout(coupled,new(100,100));
        Check(coupled.HorizontalScrollBar.IsVisible&&coupled.VerticalScrollBar.IsVisible,"Vertical gutter can trigger horizontal overflow");
        var focus=new ScrollContainer{VerticalScrollBarVisibility=ScrollBarVisibility.Hidden};
        for(int i=0;i<8;i++) focus.Children.Add(new Button{Height=40});LayoutEngine.Layout(focus,new(100,100));
        var input=new InputManager{Root=focus};input.Focus(focus.Children[^1]);Check(focus.Offset.Y>200,"Focusing an offscreen descendant reveals it");
        bool threw=false;try{focus.BringIntoView(new Box());}catch(ArgumentException){threw=true;}Check(threw,"Unrelated widgets are rejected");
    }
    internal static void ScrollInput()
    {
        var scroll=new ScrollContainer{Direction=ScrollDirection.Both,Layout=LayoutMode.Overlay,Padding=new(4)};
        var child=new Box{Width=400,Height=500};scroll.Children.Add(child);LayoutEngine.Layout(scroll,new(140,120));
        var input=new InputManager{Root=scroll};
        input.ProcessPointer(new(PointerEventKind.Wheel,new(30,30),wheelDelta:new(0,-2)));Near(scroll.Offset.Y,80);
        Check(input.HitTest(new(30,30))==child,"Hit testing follows scrolled positions");
        Check(input.HitTest(new(2,30))==scroll,"Padding clips input even though child extends behind it");
        var bar=scroll.VerticalScrollBar;Point thumb=new(bar.Thumb.X+bar.Thumb.Width/2,bar.Thumb.Y+bar.Thumb.Height/2);
        Check(input.HitTest(thumb)==scroll,"Scrollbar overlays win hit testing over content");
        input.ProcessPointer(new(PointerEventKind.Down,thumb,PointerButton.Primary));Check(scroll.VerticalScrollBar.IsDragging,"Thumb starts dragging");
        input.ProcessPointer(new(PointerEventKind.Move,new(thumb.X,1000)));Near(scroll.Offset.Y,scroll.MaximumOffset.Y);
        input.ProcessPointer(new(PointerEventKind.Up,new(thumb.X,1000),PointerButton.Primary));Check(!scroll.VerticalScrollBar.IsDragging&&input.CapturedWidget is null,"Outside release ends capture and drag");
        scroll.ScrollTo(0,0);bar=scroll.VerticalScrollBar;
        input.ProcessPointer(new(PointerEventKind.Down,new(bar.Track.X+1,bar.Track.Y+bar.Track.Height-1),PointerButton.Primary));
        Check(scroll.Offset.Y>0,"Track click pages");input.Cancel();
        input.Focus(scroll);input.ProcessKey(new(Key.End,true));Near(scroll.Offset.Y,scroll.MaximumOffset.Y);input.ProcessKey(new(Key.Home,true));Check(scroll.Offset==default,"Keyboard endpoints scroll focused container");
        scroll.Direction=ScrollDirection.Horizontal;LayoutEngine.Layout(scroll,new(140,120));
        input.ProcessPointer(new(PointerEventKind.Wheel,new(30,30),wheelDelta:new(0,-1)));Near(scroll.Offset.X,40);
        var outer=new ScrollContainer{VerticalScrollBarVisibility=ScrollBarVisibility.Hidden};
        var inner=new ScrollContainer{Height=80,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden};inner.Children.Add(new Box{Height=200});
        outer.Children.Add(inner);outer.Children.Add(new Box{Height=300});LayoutEngine.Layout(outer,new(150,150));
        var nested=new InputManager{Root=outer};inner.ScrollTo(0,inner.MaximumOffset.Y);
        nested.ProcessPointer(new(PointerEventKind.Wheel,new(20,20),wheelDelta:new(0,-1)));Near(outer.Offset.Y,40);
        Check(inner.Offset.Y==inner.MaximumOffset.Y,"At an inner boundary wheel bubbles to outer scroll container");
    }
    internal static void Pixels()
    {
        using var bitmap=new SKBitmap(160,140);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        var scroll=new CustomScroll{Padding=new(10),Background=Color.White,ClipToBounds=false,VerticalScrollBarStyle=new(){TrackColor=new(0,0,255),ThumbColor=new(255,0,0),Inset=0}};
        scroll.Children.Add(new Box{Height=400,Fill=new(0,180,0)});LayoutEngine.Layout(scroll,new(160,140));scroll.ScrollTo(0,50);
        surface.Canvas.Clear(SKColors.Black);Swealium.Graphics.WidgetRenderer.Render(surface.Canvas,scroll);
        Check(bitmap.GetPixel(15,5)==SKColors.White&&bitmap.GetPixel(15,15).Green==180,"Content is clipped to padded viewport");
        var thumb=scroll.VerticalScrollBar.Thumb;
        Check(scroll.Calls==1&&bitmap.GetPixel((int)(thumb.X+thumb.Width/2),(int)(thumb.Y+thumb.Height/2))==SKColors.Red,"Custom scrollbar draw runs after children");
        scroll.Stroke=new(new StrokeSide(4,new(255,0,0)),new StrokeSide(6,new(0,0,255)),new StrokeSide(8,new(0,255,0)),new StrokeSide(10,new(255,255,0)));
        scroll.Radii=new(20,0,14,0);surface.Canvas.Clear(SKColors.Black);Swealium.Graphics.WidgetRenderer.Render(surface.Canvas,scroll);
        Check(bitmap.GetPixel(1,65)==SKColors.Red,"Left stroke width and color");Check(bitmap.GetPixel(80,3)==SKColors.Blue,"Top stroke");
        Check(bitmap.GetPixel(156,65)==SKColors.Lime,"Right stroke");Check(bitmap.GetPixel(80,135)==SKColors.Yellow,"Bottom stroke");
        Check(bitmap.GetPixel(1,1)==SKColors.Black,"Stroke respects asymmetric rounded outer corner");
        Check(bitmap.GetPixel(80,8)==SKColors.White,"Stroke leaves the inner area untouched");
        Check(scroll.Children[0].Bounds.Y==-40,"Decorative stroke does not consume layout space");
        scroll.VerticalScrollBarVisibility=ScrollBarVisibility.Hidden;LayoutEngine.Layout(scroll,new(160,140));scroll.Calls=0;
        Swealium.Graphics.WidgetRenderer.Render(surface.Canvas,scroll);Check(scroll.Calls==0,"Hidden bars do not invoke custom drawing hooks");
        var shape=new Box{Stroke=new Stroke(1000,new(255,0,0)),Radii=new(12)};LayoutEngine.Layout(shape,new(30,20));
        Swealium.Graphics.WidgetRenderer.Render(surface.Canvas,shape);Check(bitmap.GetPixel(15,10)==SKColors.Red,"Oversized opposing strokes clamp to bounds without inverted geometry");
        Directory.CreateDirectory("artifacts");using var image=SKImage.FromBitmap(bitmap);using var png=image.Encode(SKEncodedImageFormat.Png,100);using var file=File.Create("artifacts/scroll-stroke-preview.png");png.SaveTo(file);
    }
    private sealed class CustomScroll:ScrollContainer
    {
        public int Calls;
        protected override void DrawScrollBar(DrawingContext context,ScrollBarInfo bar){Calls++;base.DrawScrollBar(context,bar);}
    }
    private sealed class Box:Widget
    {
        public Color Fill {get;set;}=Color.White;
        protected override Size MeasureContent(Size available)=>new(20,20);
        public override void Draw(DrawingContext context)=>context.FillRectangle(new(0,0,ActualWidth,ActualHeight),Fill,Radii);
    }
}
