using SkiaSharp;
using Swealium;
using Swealium.Accessibility;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.Native;
using Swealium.UI;
using Bitmap=Swealium.Graphics.Bitmap;
namespace Swealium.Tests;

internal static class InteractionBoundaryTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException($"Expected {typeof(T).Name}");}
    private static PointerId Touch(ulong id)=>new(PointerDeviceKind.Touch,17,id);
    private static PointerEvent Finger(PointerEventKind kind,PointerId id,Point position)=>new(kind,position,kind is PointerEventKind.Down or PointerEventKind.Up?PointerButton.Primary:PointerButton.None,pointerId:id,pressure:.6f);
    internal static void Opacity()
    {
        using var bitmap=new SKBitmap(80,60);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        var group=new Container{Layout=LayoutMode.Overlay,VerticalContentAlignment=Alignment.Stretch,Opacity=.5f};var a=new Box{Fill=new(255,0,0)};var b=new Box{Fill=new(255,0,0)};
        group.Children.Add(a);group.Children.Add(b);LayoutEngine.Layout(group,new(80,60));surface.Canvas.Clear(SKColors.White);WidgetRenderer.Render(surface.Canvas,group);
        Check(bitmap.GetPixel(20,20).Green is >=126 and <=129,"Parent opacity composites overlapping children as one group");
        a.IsVisible=false;b.Opacity=.5f;LayoutEngine.Layout(group,new(80,60));surface.Canvas.Clear(SKColors.White);WidgetRenderer.Render(surface.Canvas,group);
        Check(bitmap.GetPixel(20,20).Green is >=190 and <=192,"Nested opacity multiplies once per group");
        group.Stroke=new(4,new(0,0,255));surface.Canvas.Clear(SKColors.White);WidgetRenderer.Render(surface.Canvas,group);
        Check(bitmap.GetPixel(20,1).Red is >=126 and <=129&&bitmap.GetPixel(20,1).Blue==255,"Widget stroke participates in group opacity");
        group.Opacity=0;var input=new InputManager{Root=group};Check(input.HitTest(new(20,20))==b,"Zero opacity preserves hit testing/layout");
        surface.Canvas.Clear(SKColors.White);WidgetRenderer.Render(surface.Canvas,group);Check(bitmap.GetPixel(20,20)==SKColors.White,"Zero opacity paints nothing");
        using var source=new Bitmap(1,1,new(255,0,0));var image=new Image{Source=source,Opacity=.5f};LayoutEngine.Layout(image,new(80,60));
        surface.Canvas.Clear(SKColors.White);WidgetRenderer.Render(surface.Canvas,image);Check(bitmap.GetPixel(30,30).Green is >=126 and <=129,"Image inherits group opacity without applying it twice");
        Throws<ArgumentOutOfRangeException>(()=>group.Opacity=float.NaN);Throws<ArgumentOutOfRangeException>(()=>group.Opacity=1.1f);
    }
    internal static void BitmapOperations()
    {
        using var source=Bitmap.FromPixels(3,2,new byte[]{255,0,0,255,0,255,0,255,0,0,255,255,255,255,0,255,255,0,255,255,0,255,255,255});
        using var plain=Bitmap.FromBase64(source.ToBase64());using var uri=Bitmap.FromBase64(source.ToDataUri());Check(plain.CopyPixels().SequenceEqual(source.CopyPixels())&&uri.Size==source.Size,"Base64 and data URI round trips");
        Throws<InvalidDataException>(()=>Bitmap.FromBase64("not base64!"));Throws<InvalidDataException>(()=>Bitmap.FromBase64("data:text/plain;base64,AAAA"));
        Throws<InvalidDataException>(()=>Bitmap.FromBase64(source.ToBase64(),new(){MaximumEncodedBytes=1}));
        using var crop=source.Crop(1,1,1,1);Check(crop.GetPixel(0,0)==new Color(255,0,255),"Crop returns exact independent pixels");
        using var resized=source.Resize(6,4,BitmapInterpolation.Nearest);Check(resized.GetPixel(2,2)==source.GetPixel(1,1),"Nearest resize maps source pixels");
        foreach(var rotation in Enum.GetValues<BitmapRotation>())
        {
            using var rotated=source.Rotate(rotation);Check(rotated.Width==(rotation is BitmapRotation.Clockwise90 or BitmapRotation.Clockwise270?2:3),"Rotated dimensions");
            for(int y=0;y<2;y++)for(int x=0;x<3;x++)
            {
                var p=rotation switch{BitmapRotation.Clockwise90=>(1-y,x),BitmapRotation.Clockwise180=>(2-x,1-y),BitmapRotation.Clockwise270=>(y,2-x),_=>(x,y)};
                Check(rotated.GetPixel(p.Item1,p.Item2)==source.GetPixel(x,y),"Quarter-turn pixel mapping");
            }
        }
        using var flipped=source.Flip(BitmapFlip.Both);Check(flipped.GetPixel(0,0)==source.GetPixel(2,1),"Flip both axes");
        using var translucent=source.WithOpacity(.25f);Check(translucent.GetPixel(0,0).A is >=63 and <=65,"Opacity copy preserves original pixels");
        crop.Clear(new(2,3,4));Check(crop.GetPixel(0,0)==new Color(2,3,4)&&source.GetPixel(1,1).R==255,"Clear affects only returned bitmap");
        Throws<ArgumentOutOfRangeException>(()=>source.Crop(2,1,2,1));
    }
    internal static void MultiplePointers()
    {
        var root=new Container{Layout=LayoutMode.Horizontal};var left=new Button{Width=80,Height=60,Text="A"};var right=new Button{Width=80,Height=60,Text="B"};root.Children.Add(left);root.Children.Add(right);
        LayoutEngine.Layout(root,new(180,80));var input=new InputManager{Root=root};int a=0,b=0;left.Click+=()=>a++;right.Click+=()=>b++;
        var one=Touch(1);var two=Touch(2);input.ProcessPointer(Finger(PointerEventKind.Down,one,new(20,20)));input.ProcessPointer(Finger(PointerEventKind.Down,two,new(100,20)));
        Check(input.GetCapturedWidget(one)==left&&input.GetCapturedWidget(two)==right&&left.IsPressed&&right.IsPressed,"Simultaneous contacts capture and press different widgets independently");
        Check(input.GetPointer(one)!.IsPrimary&&!input.GetPointer(two)!.IsPrimary&&input.FocusedWidget==left,"Secondary touch does not steal keyboard focus");
        input.ProcessPointer(Finger(PointerEventKind.Up,one,new(20,20)));Check(a==1&&b==0&&right.IsPressed&&input.GetCapturedWidget(two)==right,"One release does not clear another capture");
        input.ProcessPointer(Finger(PointerEventKind.Up,two,new(100,20)));Check(b==1&&input.Pointers.Count==0&&!right.IsPointerOver,"Touch lifetime ends after up");
        input.ProcessPointer(Finger(PointerEventKind.Down,one,new(20,20)));input.ProcessPointer(Finger(PointerEventKind.Down,two,new(20,20)));
        input.CancelPointer(one);Check(left.IsPressed&&left.IsPointerOver&&input.GetCapturedWidget(two)==left,"Hover and pressed state aggregate remaining contacts");
        input.Cancel();Check(!left.IsPressed&&!left.IsPointerOver&&input.Pointers.Count==0,"Window cancellation clears all contacts");
        input.ProcessPointer(Finger(PointerEventKind.Down,one,new(20,20)));Check(input.CapturePointer(one,right)&&!left.IsPressed,"Explicit capture transfer cancels old press");
        input.ReleasePointerCapture(one);input.ProcessPointer(Finger(PointerEventKind.Up,one,new(20,20)));Check(a==1,"Capture release prevents old-target click");
        input.ProcessPointer(Finger(PointerEventKind.Down,one,new(20,20)));root.Children.Remove(left);input.Validate();Check(input.GetCapturedWidget(one)==null&&!left.IsPressed,"Detachment cancels only affected capture");input.Cancel();
    }
    internal static void NativePointers()
    {
        SwealiumApplication.Run(new(){Width=200,Height=100},window=>
        {
            var button=new Button();window.Content=button;
            var touch=new Sdl.Event{Type=0x700,TouchId=987654321,FingerId=ulong.MaxValue-10,TouchX=.25f,TouchY=.5f,TouchPressure=.8f,TouchWindowId=window.Id};
            Check(touch.RoutedWindowId==window.Id,"Touch window ID comes from its own ABI field");window.HandleSdlEvent(touch);
            var id=new PointerId(PointerDeviceKind.Touch,touch.TouchId,touch.FingerId);Check(window.Input.GetPointer(id)!.Position==new Point(50,50),"Native normalized coordinates become logical window coordinates");
            var other=window.CreateChild(new(){Width=120,Height=80});other.Content=new Button();other.HandleSdlEvent(touch);Check(other.Input.Pointers.Count==0,"Touch event filters by target window, not touch-device bytes");
            touch.Type=0x702;touch.TouchX=1.2f;window.HandleSdlEvent(touch);Check(window.Input.GetPointer(id)!.Position.X>200,"Out-of-bounds touch motion is retained during capture");
            touch.Type=0x703;window.HandleSdlEvent(touch);Check(window.Input.GetPointer(id)==null,"Native cancellation ends one contact");
            var pen=new Sdl.Event{Type=0x1302,WindowId=window.Id,Data1=71,PenState=1u|(1u<<30),MouseX=40,MouseY=20};window.HandleSdlEvent(pen);
            var penId=new PointerId(PointerDeviceKind.Pen,71);Check(window.Input.GetPointer(penId)!.IsEraser,"Native eraser flag is decoded");
            pen.Type=0x1307;pen.PenAxis=0;pen.PenValue=.75f;window.HandleSdlEvent(pen);pen.PenAxis=1;pen.PenValue=25;window.HandleSdlEvent(pen);
            Check(window.Input.GetPointer(penId) is{Pressure:.75f,TiltX:25},"Pen axis state preserves pressure and tilt");
            pen.Type=0x1304;pen.PenState=3;pen.PenButton=1;window.HandleSdlEvent(pen);Check(!window.Input.GetPointer(penId)!.IsEraser,"Pen barrel-button flag is not confused with eraser");
            pen.Type=0x1301;window.HandleSdlEvent(pen);Check(window.Input.GetPointer(penId)==null,"Pen proximity out cancels state");
            window.HandleSdlEvent(new(){Type=0x401,WindowId=window.Id,Data1=-1,Button=1,MouseX=20,MouseY=20});
            window.HandleSdlEvent(new(){Type=0x401,WindowId=window.Id,Data1=-2,Button=1,MouseX=20,MouseY=20});Check(window.Input.Pointers.Count==0,"Touch/pen mouse emulation does not duplicate pointers");other.RequestClose();window.RequestClose();
        });
    }
    internal static void MultipleControlDrags()
    {
        var scroll=new ScrollContainer{Direction=ScrollDirection.Both};scroll.Children.Add(new Box{Width=500,Height=500});LayoutEngine.Layout(scroll,new(160,120));var input=new InputManager{Root=scroll};
        var v=scroll.VerticalScrollBar.Thumb;var h=scroll.HorizontalScrollBar.Thumb;var one=Touch(1);var two=Touch(2);
        input.ProcessPointer(Finger(PointerEventKind.Down,one,new(v.X+v.Width/2,v.Y+v.Height/2)));input.ProcessPointer(Finger(PointerEventKind.Down,two,new(h.X+h.Width/2,h.Y+h.Height/2)));
        Check(scroll.VerticalScrollBar.IsDragging&&scroll.HorizontalScrollBar.IsDragging,"Two scrollbar axes can be dragged independently");
        input.ProcessPointer(Finger(PointerEventKind.Move,one,new(v.X,1000)));input.ProcessPointer(Finger(PointerEventKind.Move,two,new(1000,h.Y)));
        Check(scroll.Offset==scroll.MaximumOffset,"Independent drags reach both range endpoints");input.ProcessPointer(Finger(PointerEventKind.Up,one,new(v.X,1000)));
        Check(!scroll.VerticalScrollBar.IsDragging&&scroll.HorizontalScrollBar.IsDragging,"One up preserves the other scrollbar drag");input.Cancel();
        var editor=new TextBox{Text="abcdef ghi",Width=180,Height=60};LayoutEngine.Layout(editor,new(180,60));var edit=new InputManager{Root=editor};
        edit.ProcessPointer(Finger(PointerEventKind.Down,one,new(10,20)));int caret=editor.CaretIndex;edit.ProcessPointer(Finger(PointerEventKind.Down,two,new(100,20)));
        Check(editor.CaretIndex==caret,"Secondary touch cannot replace the active text selection anchor");edit.Cancel();
    }
    internal static void AccessibilityTree()
    {
        SwealiumApplication.Run(new(){Width=360,Height=240},window=>
        {
            var root=new Container{Layout=LayoutMode.Absolute};var scroll=new ScrollContainer{Width=140,Height=80};
            scroll.Children.Add(new Box{Height=120});var button=new Button{Height=40,Text="Invoke me"};button.Accessibility.AutomationId="target";scroll.Children.Add(button);root.Children.Add(scroll);
            var editor=new TextBox{X=160,Width=160,Height=60,Text="private",Placeholder="Name"};editor.Accessibility.ExposeValue=false;editor.Accessibility.AutomationId="edit";root.Children.Add(editor);
            var label=new Text{Content="<b>Live</b> message",Markup=TextMarkup.Html,Y=100};label.Accessibility.LiveRegion=AccessibilityLiveRegion.Polite;root.Children.Add(label);
            var hidden=new Container{Y=140};hidden.Accessibility.IsHidden=true;hidden.Children.Add(new Button{Text="Excluded"});root.Children.Add(hidden);window.Content=root;
            var host=window.Accessibility;var snapshot=host.CaptureSnapshot();var node=snapshot.Nodes.Single(n=>n.AutomationId=="target");var editNode=snapshot.Nodes.Single(n=>n.AutomationId=="edit");
            Check(snapshot.Nodes.Any(n=>n.Role==AccessibilityRole.Window)&&snapshot.Nodes.Any(n=>n.Name=="Live message")&&!snapshot.Nodes.Any(n=>n.Name=="Excluded"),"Semantic roles, HTML plain labels, and hidden subtree filtering");
            Check(node.State.HasFlag(AccessibilityState.Offscreen)&&editNode.Value is null,"Viewport bounds and value redaction");
            int invokes=0;button.Click+=()=>invokes++;Check(host.TryPerform(new(node.Id,AccessibilityAction.Invoke))&&invokes==1,"Semantic invoke uses normal widget activation");
            Check(host.TryPerform(new(node.Id,AccessibilityAction.Focus))&&window.Input.FocusedWidget==button&&scroll.Offset.Y>0,"Accessibility focus reveals offscreen nodes");
            Check(host.CaptureSnapshot().Nodes.Single(n=>n.Id==node.Id).VisibleBounds.Height>0,"Semantic bounds track scrolling");
            Check(host.TryPerform(new(editNode.Id,AccessibilityAction.SetValue,"updated"))&&editor.Text=="updated","Semantic value editing");
            Check(host.TryPerform(new(editNode.Id,AccessibilityAction.SetSelection,SelectionStart:1,SelectionLength:3))&&editor.SelectedText=="pda","Semantic text selection");
            editor.IsReadOnly=true;Check(!host.TryPerform(new(editNode.Id,AccessibilityAction.SetValue,"bad")),"Read-only semantic value action is rejected");
            button.IsEnabled=false;Check(!host.TryPerform(new(node.Id,AccessibilityAction.Invoke)),"Disabled nodes cannot be invoked");
            root.Children.Remove(scroll);Check(!host.TryPerform(new(node.Id,AccessibilityAction.Focus)),"Stale node actions fail safely");window.RequestClose();
        });
    }
    internal static void BridgeLifecycle()
    {
        var bridge=new TestBridge();AccessibilityHost? retained=null;
        SwealiumApplication.Run(new(){Width=200,Height=120},window=>
        {
            var text=new Text{Content="before"};text.Accessibility.LiveRegion=AccessibilityLiveRegion.Assertive;window.Content=text;var host=window.Accessibility;retained=host;
            host.AttachBridge(bridge);Check(bridge.Attached&&bridge.Updates.Count==1,"Bridge receives initial tree");
            text.Content="after";window.Render();Check(bridge.Updates.Last().Changes.Any(c=>c.Kind==AccessibilityChangeKind.LiveRegionChanged),"Observed frames publish semantic live changes");
            int count=bridge.Updates.Count;window.Render();Check(bridge.Updates.Count==count,"Unchanged snapshots do not flood the adapter");
            Throws<InvalidOperationException>(()=>host.AttachBridge(new TestBridge()));
            var crossThread=Task.Run(()=>{try{host.CaptureSnapshot();return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(crossThread,"Bridge queries enforce UI-thread ownership");
            host.DetachBridge();Check(bridge.Detached&&bridge.Disposed&&!host.IsBridgeAttached,"Bridge detach disposes owned adapter");
            var failed=new TestBridge{FailAttach=true};Throws<InvalidOperationException>(()=>host.AttachBridge(failed));Check(failed.Detached&&failed.Disposed&&!host.IsBridgeAttached,"Failed attachment cleans up adapter");
            bridge=new TestBridge();host.AttachBridge(bridge);window.RequestClose();
        });
        Check(bridge.Detached&&bridge.Disposed,"Window closure releases bridge before native shutdown");Throws<ObjectDisposedException>(()=>retained!.CaptureSnapshot());
    }
    private sealed class TestBridge:IAccessibilityBridge
    {
        internal bool Attached,Detached,Disposed,FailAttach;
        internal List<AccessibilityUpdate> Updates=[];
        public void Attach(AccessibilityHost host){Attached=true;if(FailAttach)throw new InvalidOperationException("test");}
        public void Publish(AccessibilityUpdate update)=>Updates.Add(update);
        public void Detach()=>Detached=true;
        public void Dispose()=>Disposed=true;
    }
    private sealed class Box:Widget
    {public Color Fill{get;set;}=Color.White;protected override Size MeasureContent(Size available)=>new(20,20);public override void Draw(DrawingContext context)=>context.FillRectangle(new(0,0,ActualWidth,ActualHeight),Fill);}
}
