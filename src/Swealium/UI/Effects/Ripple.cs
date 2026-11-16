using Swealium.Animation;
using Swealium.Graphics;
namespace Swealium.UI.Effects;

/// <summary>Explicitly triggered, expanding and fading circles over a widget. Instances may be shared.</summary>
public sealed class Ripple : WidgetEffect, IDisposable
{
    private sealed class Wave(Widget widget,Point origin,Color color,float radius,float fadeStart,TimeSpan duration,Func<double,double> interpolation,AnimationClock clock)
    {
        internal readonly WeakReference<Widget> Target=new(widget);
        internal readonly Point Origin=origin;
        internal readonly Color Color=color;
        internal readonly float Radius=radius,FadeStart=fadeStart;
        internal readonly AnimationClock Clock=clock;
        internal readonly AnimatorContext Animation=new(){Duration=duration,Interpolation=interpolation};
        internal float Expansion,Alpha=1;
    }
    private readonly int threadId=Environment.CurrentManagedThreadId;
    private readonly System.Collections.Generic.List<Wave> waves=[];
    private TimeSpan duration=TimeSpan.FromMilliseconds(500);
    private float radius=float.NaN,fadeStart=.35f;
    private int maxRipples=8;
    private Func<double,double> interpolation=Easing.QuadraticOut;
    private Color color=new(255,255,255,80);
    private bool clipToBounds=true;
    public bool IsDisposed {get;private set;}
    public int ActiveCount=>waves.Count;
    public Color Color {get=>color;set{Verify();color=value;}}
    public TimeSpan Duration {get=>duration;set{Verify();if(value<TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(value));duration=value;}}
    /// <summary>NaN automatically reaches the farthest corner of the widget; otherwise a fixed logical radius.</summary>
    public float Radius {get=>radius;set{Verify();if(!float.IsNaN(value)&&(!float.IsFinite(value)||value<0))throw new ArgumentOutOfRangeException(nameof(value));radius=value;}}
    /// <summary>Normalized time when linear fading starts, in [0,1].</summary>
    public float FadeStart {get=>fadeStart;set{Verify();ImageLayout.ValidateOpacity(value);fadeStart=value;}}
    public Func<double,double> Interpolation {get=>interpolation;set{Verify();interpolation=value??throw new ArgumentNullException(nameof(value));}}
    public bool ClipToBounds {get=>clipToBounds;set{Verify();clipToBounds=value;}}
    /// <summary>Maximum simultaneous waves across all widgets sharing this effect, 1..256. Oldest waves retire first.</summary>
    public int MaxRipples
    {
        get=>maxRipples;
        set{Verify();if(value<1||value>256)throw new ArgumentOutOfRangeException(nameof(value));maxRipples=value;while(waves.Count>value)Retire(waves[0]);}
    }
    private void Verify()
    {
        if(Environment.CurrentManagedThreadId!=threadId)throw new InvalidOperationException("Ripple operations require the creating thread.");
        ObjectDisposedException.ThrowIf(IsDisposed,this);
    }
    /// <summary>Trigger at a widget-local point, or its center. Attach this effect and arrange the widget first.</summary>
    public bool Trigger(Widget widget,Point? origin=null,AnimationClock clock=AnimationClock.Application)
    {
        Verify();ArgumentNullException.ThrowIfNull(widget);
        if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        if(origin is {} p&&(!float.IsFinite(p.X)||!float.IsFinite(p.Y)))throw new ArgumentOutOfRangeException(nameof(origin));
        if(!widget.Effects.Contains(this))throw new InvalidOperationException("Attach the Ripple to the widget's Effects before triggering it.");
        if(!IsEnabled||!widget.IsVisible||widget.ActualWidth<=0||widget.ActualHeight<=0)return false;
        var center=origin??new Point(widget.ActualWidth/2,widget.ActualHeight/2);
        _ = Circle(center,ResolveRadius(center,widget,Radius));
        if(clock==AnimationClock.Application)SwealiumApplication.VerifyAnimationAccess();
        while(waves.Count>=MaxRipples)Retire(waves[0]);
        var wave=new Wave(widget,center,Color,Radius,FadeStart,Duration,Interpolation,clock);
        wave.Animation.OnUpdate+=value=>
        {
            if(!wave.Target.TryGetTarget(out var target)||!target.Effects.Contains(this)){Retire(wave);return;}
            wave.Expansion=Math.Clamp(value,0,1);
            double progress=wave.Animation.Progress;
            wave.Alpha=progress>=1?0:wave.FadeStart>=1?1:(float)(1-Math.Clamp((progress-wave.FadeStart)/(1-wave.FadeStart),0,1));
        };
        wave.Animation.OnCompleted+=()=>Retire(wave);
        wave.Animation.OnCancelled+=_=>Retire(wave);
        wave.Animation.OnFailed+=_=>Retire(wave);
        waves.Add(wave);
        try{wave.Animation.Start(clock);return true;}
        catch{Retire(wave);throw;}
    }
    /// <summary>Advance manual-clock waves only. Application-clock waves continue on the UI frame loop.</summary>
    public void Advance(TimeSpan delta)
    {
        Verify();if(delta<TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(delta));
        foreach(var wave in waves.ToArray())if(wave.Clock==AnimationClock.Manual&&waves.Contains(wave))wave.Animation.Advance(delta);
    }
    /// <summary>Cancel all waves, or only waves belonging to a particular widget.</summary>
    public void Clear(Widget? widget=null)
    {
        Verify();for(int i=waves.Count-1;i>=0;i--)if(widget is null||waves[i].Target.TryGetTarget(out var target)&&ReferenceEquals(target,widget))Retire(waves[i]);
    }
    private void Retire(Wave wave)
    {
        waves.Remove(wave);
        if(wave.Animation.Completion is {IsFaulted:true} failed)_=failed.Exception;
        wave.Animation.Dispose();
    }
    private static double ResolveRadius(Point origin,Widget widget,float requested)
    {
        if(!float.IsNaN(requested))return requested;
        double x=Math.Max(Math.Abs((double)origin.X),Math.Abs(widget.ActualWidth-(double)origin.X));
        double y=Math.Max(Math.Abs((double)origin.Y),Math.Abs(widget.ActualHeight-(double)origin.Y));
        return Math.Sqrt(x*x+y*y);
    }
    private static Rect Circle(Point origin,double radius)
    {
        double left=origin.X-radius,top=origin.Y-radius,size=radius*2;
        if(!double.IsFinite(radius)||size>float.MaxValue||left< -float.MaxValue||top< -float.MaxValue||origin.X+radius>float.MaxValue||origin.Y+radius>float.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(origin),"Ripple geometry exceeds the drawable range.");
        return new((float)left,(float)top,(float)size,(float)size);
    }
    public override void PostDraw(DrawingContext context,Widget widget)
    {
        if(IsDisposed)return;Verify();
        if(!IsEnabled||waves.Count==0)return;
        using var clip=ClipToBounds?context.PushClip(new(0,0,widget.ActualWidth,widget.ActualHeight),widget.Radii):null;
        foreach(var wave in waves)
        {
            if(!wave.Target.TryGetTarget(out var target)||!ReferenceEquals(target,widget)||wave.Expansion<=0||wave.Alpha<=0)continue;
            var bounds=Circle(wave.Origin,ResolveRadius(wave.Origin,widget,wave.Radius)*wave.Expansion);
            if(bounds.Width>0)context.FillEllipse(bounds,wave.Color with{A=(byte)MathF.Round(wave.Color.A*wave.Alpha)});
        }
    }
    public void Dispose()
    {
        if(Environment.CurrentManagedThreadId!=threadId)throw new InvalidOperationException("Ripple operations require the creating thread.");
        if(IsDisposed)return;Clear();IsDisposed=true;
    }
}
