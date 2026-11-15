using SkiaSharp;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.Native;
using Swealium.UI;
namespace Swealium.Tests;

internal static class EditingStrokeRegressionTests
{
    private static void Check(bool value,string message) {if(!value) throw new InvalidOperationException(message);}
    private static void Send(InputManager input,int scan,bool down=true,ushort modifiers=0,uint key=0)=>
        SdlInput.Dispatch(new(){Type=down?0x300u:0x301u,ScanCode=scan,Modifiers=modifiers,KeyCode=key},input);
    internal static void NativeSelection()
    {
        var editor=new TextBox{Text="alpha beta gamma",Width=300,Height=80};
        LayoutEngine.Layout(editor,new(300,80));var input=new InputManager{Root=editor};
        using(var layout=new TextLayout(editor.Text))
        {
            Rect caret=layout.GetCaretRectangle(new(6));Point point=new(caret.X+editor.Padding.Left+.5f,editor.Padding.Top+caret.Height/2);
            input.ProcessPointer(new(PointerEventKind.Down,point,PointerButton.Primary));
            input.ProcessPointer(new(PointerEventKind.Up,point,PointerButton.Primary));
        }
        Check(editor.CaretIndex==6,"Pointer placement establishes the native selection anchor");
        Send(input,225);Send(input,77);
        Check(editor.SelectedText=="beta gamma"&&input.Modifiers.HasFlag(KeyModifiers.Shift),"Held Shift plus End selects even when navigation lacks a modifier snapshot");
        using(var bitmap=new SKBitmap(300,80))
        using(var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!)
        {
            surface.Canvas.Clear(SKColors.White);WidgetRenderer.Render(surface.Canvas,editor);
            int blue=0;for(int y=10;y<30;y++)for(int x=50;x<180;x++)
            {var pixel=bitmap.GetPixel(x,y);if(pixel.Blue>200&&pixel.Red<230&&pixel.Green>100)blue++;}
            Check(blue>150,"Native Shift+End selection is visibly painted inside the editor");
        }
        Send(input,74);Check(editor.SelectedText=="alpha ","Held Shift+Home preserves current selection's original anchor");
        Send(input,225,false);Send(input,77);Check(editor.Selection.Length==0&&editor.CaretIndex==editor.Text.Length,"Releasing Shift stops extension");
        editor.Select(6,0);Send(input,229);Send(input,95);
        Check(editor.SelectedText=="alpha ","Right Shift plus keypad Home works with NumLock off");
        Send(input,89);Check(editor.SelectedText=="beta gamma","Keypad End selects to the line end");
        Send(input,229,false);editor.Select(6,0);Send(input,95,modifiers:0x1000);
        Check(editor.CaretIndex==6,"NumLock-on keypad digits do not navigate");
        Send(input,0,modifiers:1,key:0x4000004a);Check(editor.SelectedText=="alpha ","Logical Home from a remapped navigation key works");
        input.Cancel();Check(input.Modifiers==KeyModifiers.None,"Focus cancellation clears held modifiers");input.Focus(editor);
        editor.Multiline=true;editor.Text="first\nsecond\nthird";editor.Select(9,0);
        Send(input,225);Send(input,77);Check(editor.SelectedText=="ond","Native End uses the current hard line's range");
        Send(input,74);Check(editor.SelectedText=="sec","Native Home uses the current hard line's range");Send(input,225,false);
        editor.Text="שלום עולם";editor.Select(3,0);Send(input,225);Send(input,74);
        Check(editor.Selection==new TextSelection(0,3),"RTL Home extends to logical line start");
        Send(input,77);Check(editor.Selection==new TextSelection(3,editor.Text.Length-3),"RTL End extends to logical line end");
    }
    internal static void ShiftWheel()
    {
        var scroll=new ScrollContainer{Direction=ScrollDirection.Both};scroll.Children.Add(new Box{Width=500,Height=500});
        LayoutEngine.Layout(scroll,new(160,120));var input=new InputManager{Root=scroll};input.Focus(scroll);
        Send(input,225);
        SdlInput.Dispatch(new(){Type=0x403,WheelY=-1,WheelMouseX=30,WheelMouseY=30},input);
        Check(scroll.Offset==new Point(40,0),"Native Shift+vertical wheel scrolls horizontally");
        SdlInput.Dispatch(new(){Type=0x403,WheelX=-1,WheelMouseX=30,WheelMouseY=30},input);
        Check(scroll.Offset==new Point(80,0),"Already-horizontal native delta is not swapped a second time");
        Send(input,225,false);
        input.ProcessPointer(new(PointerEventKind.Wheel,new(30,30),wheelDelta:new(0,-1)));
        Check(scroll.Offset==new Point(80,40),"Releasing Shift restores vertical wheel");
        var outer=new ScrollContainer{Direction=ScrollDirection.Horizontal,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden};
        var inner=new ScrollContainer{Width=100,Height=80,Direction=ScrollDirection.Vertical};inner.Children.Add(new Box{Height=300});
        outer.Layout=LayoutMode.Horizontal;outer.Children.Add(inner);outer.Children.Add(new Box{Width=400});LayoutEngine.Layout(outer,new(180,120));
        var nested=new InputManager{Root=outer};
        nested.ProcessPointer(new(PointerEventKind.Wheel,new(30,30),wheelDelta:new(0,-1),modifiers:KeyModifiers.Shift));
        Check(inner.Offset.Y==0&&outer.Offset.X==40,"Shift wheel bypasses a vertical-only inner scroller and reaches the horizontal parent");
        var editor=new TextBox{Text=new string('W',60),Width=160,Height=60};LayoutEngine.Layout(editor,new(160,60));
        var textInput=new InputManager{Root=editor};textInput.Focus(editor);_ = editor.TextInputArea;
        var wheel=new PointerEvent(PointerEventKind.Wheel,new(30,30),wheelDelta:new(0,-1),modifiers:KeyModifiers.Shift);
        textInput.ProcessPointer(wheel);Check(wheel.Handled,"Single-line text editor consumes horizontal Shift wheel when it has overflow");
    }
    internal static void FocusStrokes()
    {
        var button=new Button{Text="",Background=new(30,40,50)};LayoutEngine.Layout(button,new(160,80));var input=new InputManager{Root=button};
        Check(!button.IsStrokeSet&&button.Stroke==default,"Untouched button has no resting stroke");input.Focus(button);
        Check(button.Stroke==button.FocusStroke&&button.Stroke.Top.Width==2,"Default focus uses the public Swealium Stroke");
        var custom=new Stroke(5,new(255,0,0),StrokeStyle.Double);button.Stroke=custom;
        input.Focus(null);input.Focus(button);Check(button.Stroke==custom&&button.IsStrokeSet,"Explicit stroke suppresses focus styling across focus changes");
        button.FocusStroke=new(3,new(0,0,255),StrokeStyle.Dashed);Check(button.Stroke==custom,"Changing FocusStroke does not override a local Stroke");
        button.Stroke=default;Check(button.Stroke==default&&button.IsStrokeSet,"Explicit empty stroke disables default focus appearance");
        button.ResetStroke();Check(!button.IsStrokeSet&&button.Stroke.Top.Style==StrokeStyle.Dashed,"ResetStroke restores automatic focus styling");
        button.Stroke=new(4,Color.White,StrokeStyle.Hidden);
        using(var bitmap=new SKBitmap(160,80))using(var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!)
        {WidgetRenderer.Render(surface.Canvas,button);Check(bitmap.GetPixel(80,1)==new SKColor(30,40,50),"Hidden stroke leaves no unreachable white focus outline");}
        var editor=new TextBox{BorderColor=new(180,20,20)};LayoutEngine.Layout(editor,new(160,80));var textInput=new InputManager{Root=editor};
        Check(editor.Stroke.Top.Color==editor.BorderColor,"TextBox's resting border is its default Stroke");
        textInput.Focus(editor);editor.Stroke=default;Check(editor.Stroke==default,"TextBox explicit empty stroke also suppresses blue focus border");
        editor.ResetStroke();Check(editor.Stroke==editor.FocusStroke,"TextBox ResetStroke restores focus appearance");
    }
    internal static void StrokeStyles()
    {
        using var bitmap=new SKBitmap(960,540);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;
        surface.Canvas.Clear(SKColors.White);
        int index=0;
        foreach(var style in Enum.GetValues<StrokeStyle>())
        {
            var box=new Box{Stroke=new Stroke(6,new(30,80,200),style),Radii=new(24)};LayoutEngine.Layout(box,new(220,100));
            using var checkBitmap=new SKBitmap(220,100);using var checkSurface=SKSurface.Create(checkBitmap.Info,checkBitmap.GetPixels(),checkBitmap.RowBytes)!;
            checkSurface.Canvas.Clear(SKColors.White);WidgetRenderer.Render(checkSurface.Canvas,box);
            int ink=0,gap=0;for(int x=30;x<190;x++){var pixel=checkBitmap.GetPixel(x,3);if(pixel.Blue>150&&pixel.Red<100)ink++;if(pixel.Red>245&&pixel.Green>245)gap++;}
            if(style==StrokeStyle.Solid)Check(ink==160,"Solid side is continuous");
            if(style is StrokeStyle.Dashed or StrokeStyle.Dotted)Check(ink>15&&gap>15,$"{style} has visible marks and gaps");
            if(style==StrokeStyle.Double)Check(checkBitmap.GetPixel(110,1).Red<100&&checkBitmap.GetPixel(110,3)==SKColors.White&&checkBitmap.GetPixel(110,5).Red<100,"Double has outer and inner bands separated by a gap");
            if(style==StrokeStyle.Hidden)Check(ink==0&&gap==160,"Hidden side emits no paint");
            int save=surface.Canvas.Save();surface.Canvas.Translate(24+(index%3)*300,55+(index/3)*180);WidgetRenderer.Render(surface.Canvas,box);surface.Canvas.RestoreToCount(save);
            var label=new SkiaDrawingContext(surface.Canvas);label.DrawText(style.ToString(),new(24+(index%3)*300,40+(index/3)*180),Color.Black,22);label.End();index++;
        }
        var mixed=new Box{Radii=new(32,18,24,10),Stroke=new(new StrokeSide(14,new(255,0,0)),new StrokeSide(6,new(0,0,255)),new StrokeSide(10,new(0,255,0)),new StrokeSide(8,new(240,180,0)))};
        foreach(var style in new[]{StrokeStyle.Dashed,StrokeStyle.Dotted})
        {
            var wide=new Box{Stroke=new Stroke(2,new(30,80,200),style)};LayoutEngine.Layout(wide,new(100000,100));
            using var cropped=new SKBitmap(220,100);using var crop=SKSurface.Create(cropped.Info,cropped.GetPixels(),cropped.RowBytes)!;
            crop.Canvas.Clear(SKColors.White);crop.Canvas.Translate(-90000,0);WidgetRenderer.Render(crop.Canvas,wide);
            int ink=0,gaps=0;for(int x=0;x<220;x++){var pixel=cropped.GetPixel(x,1);if(pixel.Red<100)ink++;if(pixel.Red>245)gaps++;}
            Check(ink>15&&gaps>15,$"{style} remains patterned far into a large scrolled extent");
        }
        LayoutEngine.Layout(mixed,new(220,130));
        using(var small=new SKBitmap(220,130))using(var canvas=SKSurface.Create(small.Info,small.GetPixels(),small.RowBytes)!)
        {
            canvas.Canvas.Clear(SKColors.White);WidgetRenderer.Render(canvas.Canvas,mixed);
            bool blend=false;for(int y=6;y<27;y++)for(int x=6;x<27;x++){var p=small.GetPixel(x,y);if(p.Red>40&&p.Blue>40&&p.Green<30)blend=true;}
            Check(blend,"Adjacent side colors blend around rounded corners instead of a stationary diagonal partition");
            Check(small.GetPixel(1,1)==SKColors.White,"Rounded outer corner stays clear");
        }
        int saved=surface.Canvas.Save();surface.Canvas.Translate(624,235);WidgetRenderer.Render(surface.Canvas,mixed);surface.Canvas.RestoreToCount(saved);
        var title=new SkiaDrawingContext(surface.Canvas);title.DrawText("Blended sides",new(624,220),Color.Black,22);title.End();
        Directory.CreateDirectory("artifacts");using var image=SKImage.FromBitmap(bitmap);using var png=image.Encode(SKEncodedImageFormat.Png,100);using var file=File.Create("artifacts/stroke-styles-preview.png");png.SaveTo(file);
    }
    private sealed class Box:Widget
    {
        protected override Size MeasureContent(Size available)=>new(20,20);
        public override void Draw(DrawingContext context)=>context.FillRectangle(new(0,0,ActualWidth,ActualHeight),Color.White,Radii);
    }
}
