using Swealium.Animation;
using Swealium.Graphics;
using Swealium.UI;
namespace Swealium.Tests;

internal static class AnimationTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Near(double actual,double expected)=>Check(Math.Abs(actual-expected)<.0001,$"Expected {expected}, got {actual}");
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    private static TimeSpan Ms(double value)=>TimeSpan.FromMilliseconds(value);
    internal static void Timeline()
    {
        using var animation=new AnimatorContext(10,30){Duration=Ms(100),Delay=Ms(50),RepeatCount=1,RepeatDelay=Ms(20),AutoReverse=true};
        var calls=new List<string>();animation.OnStarted+=()=>calls.Add("start");animation.OnUpdate+=v=>calls.Add($"value {v}");animation.OnRepeated+=i=>calls.Add($"repeat {i}");animation.OnCompleted+=()=>calls.Add("end");
        animation.Start(AnimationClock.Manual);Check(animation.State==AnimationState.Delayed&&calls.SequenceEqual(new[]{"start","value 10"}),"Start publishes initial value once during delay");
        animation.Advance(Ms(40));Check(calls.Count==2,"Delay does not repeatedly publish unchanged samples");animation.Advance(Ms(60));Near(animation.Value,20);Near(animation.Progress,.5);
        animation.Advance(Ms(50));Near(animation.Value,30);int count=calls.Count;animation.Advance(Ms(10));Check(calls.Count==count,"Repeat delay holds the endpoint without update churn");
        animation.Advance(Ms(10));Check(animation.Iteration==1&&calls.Contains("repeat 1"),"Exact boundary begins the next iteration");Near(animation.Value,30);
        animation.Advance(Ms(50));Near(animation.Value,20);animation.Advance(Ms(500));Near(animation.Value,10);
        Check(animation.State==AnimationState.Completed&&animation.Elapsed==Ms(270)&&animation.Completion!.Result==AnimationState.Completed&&calls[^1]=="end","Auto-reverse completes at exact final endpoint and clamps elapsed time");
        int completedCount=calls.Count;animation.Advance(Ms(10));Check(calls.Count==completedCount&&!animation.Finish()&&!animation.Cancel(),"Terminal operations are idempotent");
        animation.Direction=AnimationDirection.Reverse;animation.Delay=TimeSpan.Zero;animation.RepeatCount=0;animation.Start(AnimationClock.Manual);Near(animation.Value,30);animation.Advance(Ms(25));Near(animation.Value,25);animation.Finish();Near(animation.Value,10);
        using var huge=new AnimatorContext{Duration=TimeSpan.FromTicks(1),RepeatCount=int.MaxValue};int repeats=0;huge.OnRepeated+=_=>repeats++;huge.Start(AnimationClock.Manual);huge.Advance(TimeSpan.MaxValue);Check(huge.State==AnimationState.Completed&&repeats==1,"Large time skips coalesce repeat callbacks instead of looping billions of times");
    }
    internal static void Controls()
    {
        using var animation=new AnimatorContext{Duration=Ms(100),Delay=Ms(100)};animation.Start(AnimationClock.Manual);animation.Seek(.25);Check(animation.State==AnimationState.Running,"Seeking past delay updates playback state");Near(animation.Value,.25);
        Check(animation.Pause()&&!animation.Pause(),"Pause is idempotent");animation.Advance(Ms(200));Near(animation.Value,.25);animation.Seek(.5);Check(animation.State==AnimationState.Paused,"Seek preserves pause");Near(animation.Value,.5);
        animation.PlaybackRate=2;animation.Resume();Check(!animation.Resume(),"Resume is idempotent");animation.Advance(Ms(10));Near(animation.Value,.7);animation.Pause();animation.Finish();Near(animation.Value,1);Check(animation.State==AnimationState.Completed,"Finish bypasses pause and delay");
        using var infinite=new AnimatorContext{Duration=Ms(20),RepeatCount=-1,AutoReverse=true};infinite.Start(AnimationClock.Manual);infinite.Advance(Ms(30));Near(infinite.Value,.5);infinite.Finish();Near(infinite.Value,0);
        using var instant=new AnimatorContext(5,9){Duration=TimeSpan.Zero,RepeatCount=3,AutoReverse=true};int updates=0;instant.OnUpdate+=_=>updates++;instant.Start(AnimationClock.Manual);Check(instant.State==AnimationState.Completed&&updates==1&&instant.Value==5,"Zero duration finite playback completes without a callback loop");
        using var invalid=new AnimatorContext{Duration=TimeSpan.Zero,RepeatCount=-1};Throws<InvalidOperationException>(()=>invalid.Start(AnimationClock.Manual));invalid.RepeatDelay=Ms(5);invalid.Start(AnimationClock.Manual);invalid.Advance(Ms(12));Check(invalid.Iteration==2,"Infinite instantaneous iterations require nonzero repeat delay");
        using var token=new CancellationTokenSource();using var cancelled=new AnimatorContext();AnimationCancellationReason? reason=null;cancelled.OnCancelled+=r=>reason=r;cancelled.Start(AnimationClock.Manual,token.Token);cancelled.Pause();token.Cancel();cancelled.Advance(TimeSpan.Zero);Check(reason==AnimationCancellationReason.CancellationToken&&cancelled.Completion!.Result==AnimationState.Cancelled,"Cancellation tokens work while paused without worker-thread callbacks");
        cancelled.Start(AnimationClock.Manual,token.Token);Check(cancelled.State==AnimationState.Cancelled,"Already cancelled tokens do not publish updates");
        using var replacement=new AnimatorContext();replacement.OnCancelled+=r=>reason=r;replacement.Start(AnimationClock.Manual);var old=replacement.Completion!;replacement.Restart(AnimationClock.Manual);Check(old.Result==AnimationState.Cancelled&&reason==AnimationCancellationReason.Replaced&&replacement.IsActive,"Restart settles the previous run separately");
    }
    internal static void ValuesAndEasing()
    {
        using var point=new AnimatorContext<Point>(AnimationValues.Point){From=new(10,20),To=new(30,60),Duration=Ms(100),Interpolation=Easing.QuadraticIn};point.Start(AnimationClock.Manual);point.Advance(Ms(50));Check(point.Value==new Point(15,30),"Typed interpolation is separate from time easing");
        using var color=new AnimatorContext<Color>(AnimationValues.Color){From=new(0,50,100,0),To=new(200,150,0,200),Duration=Ms(100)};color.Start(AnimationClock.Manual);color.Advance(Ms(50));Check(color.Value==new Color(100,100,50,100),"Solid colors include alpha");
        Near(AnimationValues.Double(10,20,.5),15);Check(AnimationValues.Size(new(10,20),new(30,40),.5)==new Size(20,30),"Size interpolation");Throws<ArgumentException>(()=>AnimationValues.Color(Gradient.Horizontal(Color.Black,Color.White),Color.White,.5));
        foreach(Func<double,double> curve in new Func<double,double>[]{Easing.Linear,Easing.QuadraticIn,Easing.QuadraticOut,Easing.QuadraticInOut,Easing.CubicIn,Easing.CubicOut,Easing.CubicInOut,Easing.SineInOut,Easing.ExponentialOut,Easing.BackOut,Easing.BounceOut,Easing.CubicBezier(.42,0,.58,1)})
        {Near(curve(0),0);Near(curve(1),1);Check(double.IsFinite(curve(.25)),"All built-in curves are finite");}
        Near(Easing.CubicBezier(0,0,1,1)(.35),.35);Near(Easing.Steps(4)(.3),.25);Near(Easing.Steps(4,true)(0),.25);Check(Easing.BackOut(.7)>1,"Back easing supports overshoot");
        using var overshoot=new AnimatorContext{Duration=Ms(100),Interpolation=Easing.BackOut};overshoot.Start(AnimationClock.Manual);overshoot.Advance(Ms(70));Check(overshoot.Value>1,"Animator does not clamp valid easing overshoot");overshoot.Finish();Near(overshoot.Value,1);
        Throws<ArgumentOutOfRangeException>(()=>Easing.CubicBezier(-1,0,1,1));Throws<ArgumentOutOfRangeException>(()=>Easing.Steps(0));Throws<ArgumentOutOfRangeException>(()=>Easing.Linear(double.NaN));
        using var invalidPoint=new AnimatorContext<Point>(AnimationValues.Point){From=new(float.NaN,0)};Throws<ArgumentOutOfRangeException>(()=>invalidPoint.Start(AnimationClock.Manual));Check(invalidPoint.State==AnimationState.Faulted,"Typed helper validates invalid endpoints at the initial sample");_ =invalidPoint.Completion!.Exception;
    }
    internal static void ReentrancyAndFailure()
    {
        using var animation=new AnimatorContext{Duration=Ms(20)};int completed=0;animation.OnUpdate+=_=>{if(animation.Progress==1)animation.Cancel();};animation.OnCompleted+=()=>completed++;animation.Start(AnimationClock.Manual);animation.Advance(Ms(20));Check(animation.State==AnimationState.Cancelled&&completed==0,"Cancellation from final update wins over automatic completion");
        using var restarted=new AnimatorContext{Duration=Ms(20)};int ends=0;restarted.OnCompleted+=()=>{if(++ends==1)restarted.Start(AnimationClock.Manual);};var first=restarted.StartAsync(AnimationClock.Manual);restarted.Advance(Ms(20));Check(first.Result==AnimationState.Completed&&restarted.IsActive&&restarted.Progress==0,"A completion callback can start an independent next run");
        using var pauseAtEnd=new AnimatorContext{Duration=Ms(10)};pauseAtEnd.OnUpdate+=_=>{if(pauseAtEnd.Progress==1)pauseAtEnd.Pause();};pauseAtEnd.Start(AnimationClock.Manual);pauseAtEnd.Advance(Ms(10));Check(pauseAtEnd.State==AnimationState.Paused&&!pauseAtEnd.Completion!.IsCompleted,"Pause from callback prevents stale terminal transition");pauseAtEnd.Cancel();
        using var callbackRestart=new AnimatorContext();int starts=0;callbackRestart.OnStarted+=()=>starts++;callbackRestart.OnCancelled+=_=>{callbackRestart.Duration=TimeSpan.Zero;callbackRestart.Start(AnimationClock.Manual);};callbackRestart.Start(AnimationClock.Manual);callbackRestart.Restart(AnimationClock.Manual);Check(starts==2&&callbackRestart.State==AnimationState.Completed,"Restart respects a callback's replacement even when it completes immediately");
        using var disposedRestart=new AnimatorContext();disposedRestart.OnCancelled+=_=>disposedRestart.Dispose();disposedRestart.Start(AnimationClock.Manual);disposedRestart.Restart(AnimationClock.Manual);Check(disposedRestart.State==AnimationState.Disposed,"Disposal from cancellation prevents a replacement start");
        using var failing=new AnimatorContext{Duration=Ms(100),Interpolation=_=>double.NaN};Exception? observed=null;failing.OnFailed+=e=>observed=e;failing.Start(AnimationClock.Manual);Throws<InvalidOperationException>(()=>failing.Advance(Ms(50)));Check(failing.State==AnimationState.Faulted&&observed is InvalidOperationException&&failing.Completion!.IsFaulted,"Invalid easing faults the run and task");_ =failing.Completion!.Exception;
        using var throwing=new AnimatorContext();throwing.OnUpdate+=_=>throw new ApplicationException("update");Throws<ApplicationException>(()=>throwing.Start(AnimationClock.Manual));Check(throwing.State==AnimationState.Faulted,"Start callback exceptions settle the task");_ =throwing.Completion!.Exception;
        using var validation=new AnimatorContext();Throws<InvalidOperationException>(()=>validation.Start());Throws<ArgumentOutOfRangeException>(()=>validation.From=float.NaN);Throws<ArgumentOutOfRangeException>(()=>validation.Duration=Ms(-1));Throws<ArgumentOutOfRangeException>(()=>validation.PlaybackRate=0);validation.Start(AnimationClock.Manual);Throws<InvalidOperationException>(()=>validation.To=4);Throws<InvalidOperationException>(()=>validation.Start(AnimationClock.Manual));Throws<ArgumentOutOfRangeException>(()=>validation.Seek(2));Throws<ArgumentOutOfRangeException>(()=>validation.Advance(Ms(-1)));
        Task.Run(()=>Throws<InvalidOperationException>(()=>validation.Cancel())).GetAwaiter().GetResult();validation.Dispose();validation.Dispose();Check(validation.State==AnimationState.Disposed&&validation.Completion!.Result==AnimationState.Cancelled,"Dispose cancels and is idempotent");Throws<ObjectDisposedException>(()=>validation.Start(AnimationClock.Manual));
    }
    internal static void ApplicationClock()
    {
        int thread=Environment.CurrentManagedThreadId;Task? observer=null;AnimatorContext? animation=null;bool paintedFinal=false;int frames=0;
        SwealiumApplication.Run(new(){Width=64,Height=64},window=>
        {
            var content=new Container();window.Content=content;animation=new(){Duration=Ms(1)};
            animation.OnUpdate+=v=>{Check(Environment.CurrentManagedThreadId==thread,"Automatic updates stay on the UI thread");content.Opacity=.5f+v*.5f;};
            var completion=animation.StartAsync();observer=Observe();
            async Task Observe(){Check(await completion==AnimationState.Completed,"Await returns completed state");Check(Environment.CurrentManagedThreadId==thread,"Await captures the UI synchronization context");}
            window.Paint+=(_,_)=>{if(animation.State==AnimationState.Completed){paintedFinal=true;Near(content.Opacity,1);if(observer.IsCompleted)window.RequestClose();}if(++frames>100)throw new InvalidOperationException("Animation did not complete in the frame loop");};
        });
        observer!.GetAwaiter().GetResult();Check(paintedFinal&&animation!.State==AnimationState.Completed,"Animation advances before frame drawing");animation!.Dispose();
        using var stopped=new AnimatorContext{RepeatCount=-1};AnimationCancellationReason? reason=null;stopped.OnCancelled+=r=>reason=r;
        SwealiumApplication.Run(new(){Width=32,Height=32},window=>{stopped.Start();stopped.Pause();window.RequestClose();});
        Check(stopped.State==AnimationState.Cancelled&&reason==AnimationCancellationReason.ApplicationStopped,"Session shutdown cancels paused automatic animations");
        using var other=new AnimatorContext();using var bad=new AnimatorContext();
        bad.OnCancelled+=_=>throw new ApplicationException("cleanup callback");
        Throws<ApplicationException>(()=>SwealiumApplication.Run(new(){Width=32,Height=32},window=>{bad.Start();other.Start();window.RequestClose();}));
        Check(other.State==AnimationState.Cancelled,"One cleanup callback cannot prevent cancellation of other animations");
    }
}
