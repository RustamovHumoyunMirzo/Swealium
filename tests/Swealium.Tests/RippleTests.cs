using SkiaSharp;
using Swealium.Animation;
using Swealium.Graphics;
using Swealium.UI;
using Swealium.UI.Effects;
namespace Swealium.Tests;

internal static class RippleTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    internal static void Pixels()
    {
        using var ripple=new Ripple{Color=new(255,0,0),Duration=TimeSpan.FromSeconds(1),Radius=40,FadeStart=.5f,Interpolation=Easing.Linear};
        var widget=new Container{Radii=new(15)};widget.Effects.Add(ripple);LayoutEngine.Layout(widget,new(80,60));
        using var pixels=new SKBitmap(100,80);using var surface=SKSurface.Create(pixels.Info,pixels.GetPixels(),pixels.RowBytes)!;
        void Render(){surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,widget);}
        Check(ripple.Trigger(widget,new(8,8),AnimationClock.Manual)&&ripple.ActiveCount==1,"Trigger creates a manual wave on an attached widget");Render();Check(pixels.GetPixel(8,8).Alpha==0,"A new ripple starts at zero radius");
        ripple.Advance(TimeSpan.FromMilliseconds(500));Render();Check(pixels.GetPixel(22,8).Red>250&&pixels.GetPixel(22,8).Alpha==255&&pixels.GetPixel(40,8).Alpha==0,"Expansion is centered on the requested local point");
        Check(pixels.GetPixel(0,0).Alpha==0&&pixels.GetPixel(10,10).Alpha==255,"Rounded clipping follows the widget's radii");
        ripple.Advance(TimeSpan.FromMilliseconds(250));Render();Check(Math.Abs(pixels.GetPixel(10,10).Alpha-128)<=1,"Ripple fades according to normalized time and color alpha");
        ripple.Advance(TimeSpan.FromMilliseconds(250));Render();Check(ripple.ActiveCount==0&&pixels.GetPixel(10,10).Alpha==0,"Completion retires the wave and removes overlay pixels");
        ripple.Radius=float.NaN;ripple.FadeStart=1;widget.Radii=default;ripple.Trigger(widget,new(0,0),AnimationClock.Manual);ripple.Advance(TimeSpan.FromMilliseconds(999));Render();Check(pixels.GetPixel(79,59).Alpha==255,"Automatic radius reaches the farthest corner even from an edge");ripple.Clear();
        ripple.Radius=20;ripple.Trigger(widget,clock:AnimationClock.Manual);ripple.Advance(TimeSpan.FromMilliseconds(500));Render();Check(pixels.GetPixel(40,30).Alpha==255&&pixels.GetPixel(20,30).Alpha==0,"Omitting origin triggers at the widget center");
        widget.Opacity=.5f;Render();Check(Math.Abs(pixels.GetPixel(40,30).Alpha-128)<=1,"Widget group opacity applies once to its ripple");ripple.Clear();widget.Opacity=1;
        ripple.ClipToBounds=false;ripple.Radius=25;ripple.Trigger(widget,new(0,30),AnimationClock.Manual);ripple.Advance(TimeSpan.FromMilliseconds(500));
        surface.Canvas.Clear(SKColors.Transparent);surface.Canvas.Save();surface.Canvas.Translate(20,0);WidgetRenderer.Render(surface.Canvas,widget);surface.Canvas.Restore();Check(pixels.GetPixel(15,30).Alpha>0,"Optional unbounded ripples can extend beyond the widget's own bounds");
        ripple.Clear();ripple.ClipToBounds=true;ripple.Color=Gradient.Horizontal(new(255,0,0),new(0,0,255));ripple.Trigger(widget,clock:AnimationClock.Manual);ripple.Advance(TimeSpan.FromMilliseconds(500));Render();Check(pixels.GetPixel(35,30).Red>pixels.GetPixel(45,30).Red,"Ripple accepts gradient colors through the existing drawing API");
    }
    internal static void Lifecycle()
    {
        using var ripple=new Ripple{MaxRipples=2,Duration=TimeSpan.FromSeconds(1)};var first=new Container();var second=new Container();first.Effects.Add(ripple);second.Effects.Add(ripple);LayoutEngine.Layout(first,new(60,40));LayoutEngine.Layout(second,new(60,40));
        ripple.Trigger(first,clock:AnimationClock.Manual);ripple.Trigger(second,clock:AnimationClock.Manual);ripple.Trigger(second,new(5,5),AnimationClock.Manual);Check(ripple.ActiveCount==2,"Rapid triggers evict the oldest wave instead of allocating an unbounded queue");
        ripple.Clear(first);Check(ripple.ActiveCount==2,"The evicted first-widget wave is already gone");ripple.Clear(second);Check(ripple.ActiveCount==0,"Clear can target a single widget");
        ripple.Trigger(first,clock:AnimationClock.Manual);ripple.Trigger(second,clock:AnimationClock.Manual);ripple.MaxRipples=1;Check(ripple.ActiveCount==1,"Reducing the limit retires oldest active waves immediately");ripple.Clear();
        ripple.Trigger(first,clock:AnimationClock.Manual);first.Effects.Remove(ripple);ripple.Advance(TimeSpan.FromMilliseconds(1));Check(ripple.ActiveCount==0,"A removed effect drops its orphaned wave on the next update");first.Effects.Add(ripple);
        ripple.IsEnabled=false;Check(!ripple.Trigger(first,clock:AnimationClock.Manual),"Disabled effects reject new triggers");ripple.IsEnabled=true;first.IsVisible=false;Check(!ripple.Trigger(first,clock:AnimationClock.Manual),"Hidden widgets reject new triggers");first.IsVisible=true;
        var empty=new Container();empty.Effects.Add(ripple);Check(!ripple.Trigger(empty,clock:AnimationClock.Manual),"Unarranged empty widgets have no wave area");
        Throws<InvalidOperationException>(()=>ripple.Trigger(new Container(),clock:AnimationClock.Manual));Throws<ArgumentOutOfRangeException>(()=>ripple.Trigger(first,new(float.NaN,0),AnimationClock.Manual));Throws<InvalidOperationException>(()=>ripple.Trigger(first));
        Throws<ArgumentOutOfRangeException>(()=>ripple.MaxRipples=0);Throws<ArgumentOutOfRangeException>(()=>ripple.FadeStart=2);Throws<ArgumentOutOfRangeException>(()=>ripple.Radius=float.PositiveInfinity);Throws<ArgumentOutOfRangeException>(()=>ripple.Duration=TimeSpan.FromTicks(-1));Throws<ArgumentOutOfRangeException>(()=>ripple.Advance(TimeSpan.FromTicks(-1)));
        Task.Run(()=>Throws<InvalidOperationException>(()=>ripple.Clear())).GetAwaiter().GetResult();ripple.Duration=TimeSpan.Zero;ripple.Trigger(first,clock:AnimationClock.Manual);Check(ripple.ActiveCount==0,"Zero duration completes and retires synchronously");ripple.Dispose();ripple.Dispose();Throws<ObjectDisposedException>(()=>ripple.Trigger(first,clock:AnimationClock.Manual));
        using var automatic=new Ripple{Duration=TimeSpan.FromMilliseconds(1)};int frames=0;
        SwealiumApplication.Run(new(){Width=64,Height=64},window=>
        {
            var widget=new Container();widget.Effects.Add(automatic);window.Content=widget;LayoutEngine.Layout(widget,new(64,64));automatic.Trigger(widget);
            window.Paint+=(_,_)=>{if(++frames>1&&automatic.ActiveCount==0)window.RequestClose();if(frames>100)throw new InvalidOperationException("Automatic ripple failed to retire");};
        });
        Check(automatic.ActiveCount==0,"Automatic ripples use the application animator clock and retire naturally");
        automatic.Duration=TimeSpan.FromHours(1);
        SwealiumApplication.Run(new(){Width=32,Height=32},window=>{var widget=new Container();widget.Effects.Add(automatic);LayoutEngine.Layout(widget,new(32,32));automatic.Trigger(widget);window.RequestClose();});Check(automatic.ActiveCount==0,"Application shutdown releases active ripple animators");
    }
    internal static void Gpu(SKSurface surface)
    {
        using var ripple=new Ripple{Color=new(255,0,0),Radius=20,FadeStart=1,Duration=TimeSpan.FromSeconds(1),Interpolation=Easing.Linear};var widget=new Container{Radii=new(8)};widget.Effects.Add(ripple);LayoutEngine.Layout(widget,new(32,32));ripple.Trigger(widget,new(4,4),AnimationClock.Manual);ripple.Advance(TimeSpan.FromMilliseconds(500));surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,widget);
        using var image=surface.Snapshot();using var pixels=SKBitmap.FromImage(image);Check(pixels.GetPixel(5,5).Red>240&&pixels.GetPixel(0,0).Alpha==0&&pixels.GetPixel(25,25).Alpha==0,"GPU ripple expansion and rounded clipping match raster semantics");
    }
}
