using System.Diagnostics;
namespace Swealium.Animation;

public enum AnimationClock { Application, Manual }
public enum AnimationState { Idle, Delayed, Running, Paused, Completed, Cancelled, Faulted, Disposed }
public enum AnimationDirection { Forward, Reverse }
public enum AnimationCancellationReason { Requested, CancellationToken, Replaced, ApplicationStopped, Disposed }

/// <summary>A float animator, defaulting to 0..1 over 300 ms. Call Start inside Run, or use the manual clock.</summary>
public sealed class AnimatorContext : AnimatorContext<float>
{
    public AnimatorContext(float from=0,float to=1):base(AnimationValues.Float) { From=from;To=to; }
    protected override void ValidateValue(float value) { if(!float.IsFinite(value))throw new ArgumentOutOfRangeException(nameof(value)); }
}

/// <summary>Creating-thread-owned animation. Configuration is frozen during an active run; callbacks run on that thread.</summary>
public class AnimatorContext<T> : IDisposable
{
    private readonly int threadId=Environment.CurrentManagedThreadId;
    private readonly Func<T,T,double,T> valueInterpolation;
    private readonly AnimationRegistration registration;
    private T from=default!,to=default!;
    private TimeSpan duration=TimeSpan.FromMilliseconds(300),delay,repeatDelay;
    private Func<double,double> interpolation=Easing.Linear;
    private int repeatCount;
    private bool autoReverse;
    private AnimationDirection direction;
    private double playbackRate=1,elapsedTicks;
    private long revision,runId,lastTimestamp;
    private bool registered,inGap;
    private AnimationClock clock;
    private AnimationState resumeState;
    private CancellationToken cancellation;
    private TaskCompletionSource<AnimationState>? completion;
    public AnimatorContext(Func<T,T,double,T> valueInterpolation)
    {
        this.valueInterpolation=valueInterpolation??throw new ArgumentNullException(nameof(valueInterpolation));
        registration=new(Tick,()=>CancelCore(AnimationCancellationReason.ApplicationStopped));
    }
    private void Verify()
    {
        if(Environment.CurrentManagedThreadId!=threadId)throw new InvalidOperationException("Animator operations require the creating thread.");
        ObjectDisposedException.ThrowIf(State==AnimationState.Disposed,this);
    }
    private void Configure() { Verify();if(IsActive)throw new InvalidOperationException("Change animation configuration after cancelling or completing the current run."); }
    protected virtual void ValidateValue(T value) { }
    public T From {get=>from;set{Configure();ValidateValue(value);from=value;}}
    public T To {get=>to;set{Configure();ValidateValue(value);to=value;}}
    public TimeSpan Duration {get=>duration;set{Configure();if(value<TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(value));duration=value;}}
    public TimeSpan Delay {get=>delay;set{Configure();if(value<TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(value));delay=value;}}
    public TimeSpan RepeatDelay {get=>repeatDelay;set{Configure();if(value<TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(value));repeatDelay=value;}}
    /// <summary>Additional iterations after the first. -1 repeats indefinitely.</summary>
    public int RepeatCount {get=>repeatCount;set{Configure();if(value< -1)throw new ArgumentOutOfRangeException(nameof(value));repeatCount=value;}}
    public bool AutoReverse {get=>autoReverse;set{Configure();autoReverse=value;}}
    public AnimationDirection Direction {get=>direction;set{Configure();if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));direction=value;}}
    /// <summary>Normalized time easing, separate from typed value interpolation. Finite overshoot is allowed.</summary>
    public Func<double,double> Interpolation {get=>interpolation;set{Configure();interpolation=value??throw new ArgumentNullException(nameof(value));}}
    /// <summary>Positive finite playback multiplier, changeable during playback. Applies to subsequent elapsed time.</summary>
    public double PlaybackRate {get=>playbackRate;set{Verify();if(!double.IsFinite(value)||value<=0)throw new ArgumentOutOfRangeException(nameof(value));playbackRate=value;}}
    public AnimationState State {get;private set;}
    public bool IsActive=>State is AnimationState.Running or AnimationState.Delayed or AnimationState.Paused;
    public T Value {get;private set;}=default!;
    /// <summary>Current iteration's forward clock progress in [0,1], before direction and easing.</summary>
    public double Progress {get;private set;}
    /// <summary>Directed easing fraction passed to the typed interpolator; may overshoot.</summary>
    public double EasedProgress {get;private set;}
    public long Iteration {get;private set;}
    public TimeSpan Elapsed=>elapsedTicks>=TimeSpan.MaxValue.Ticks?TimeSpan.MaxValue:TimeSpan.FromTicks((long)elapsedTicks);
    public Task<AnimationState>? Completion=>completion?.Task;
    public event Action? OnStarted;
    public event Action<T>? OnUpdate;
    public event Action? OnCompleted;
    public event Action<AnimationCancellationReason>? OnCancelled;
    public event Action? OnPaused;
    public event Action? OnResumed;
    /// <summary>Latest zero-based iteration after a boundary; skipped frames coalesce into one notification.</summary>
    public event Action<long>? OnRepeated;
    public event Action<Exception>? OnFailed;

    public void Start(AnimationClock clock=AnimationClock.Application,CancellationToken cancellationToken=default)=>_ = StartCore(clock,cancellationToken);
    public Task<AnimationState> StartAsync(AnimationClock clock=AnimationClock.Application,CancellationToken cancellationToken=default)=>StartCore(clock,cancellationToken);
    private Task<AnimationState> StartCore(AnimationClock selectedClock,CancellationToken token)
    {
        Verify();if(IsActive)throw new InvalidOperationException("Already active. Use Restart or Cancel before Start.");
        if(!Enum.IsDefined(selectedClock))throw new ArgumentOutOfRangeException(nameof(selectedClock));
        if(repeatCount==-1&&duration==TimeSpan.Zero&&repeatDelay==TimeSpan.Zero)throw new InvalidOperationException("An infinite animation needs a nonzero duration or repeat delay.");
        if(selectedClock==AnimationClock.Application)SwealiumApplication.VerifyAnimationAccess();
        ValidateValue(from);ValidateValue(to);
        Value=direction==AnimationDirection.Forward?from:to;
        clock=selectedClock;cancellation=token;elapsedTicks=0;Iteration=0;Progress=0;EasedProgress=direction==AnimationDirection.Forward?0:1;inGap=false;
        completion=new(TaskCreationOptions.RunContinuationsAsynchronously);var task=completion.Task;runId++;revision++;long run=runId,version=revision;
        State=delay>TimeSpan.Zero?AnimationState.Delayed:AnimationState.Running;lastTimestamp=Stopwatch.GetTimestamp();
        if(selectedClock==AnimationClock.Application){SwealiumApplication.RegisterAnimation(registration);registered=true;}
        if(CheckCancellation())return task;
        try{OnStarted?.Invoke();if(revision==version&&IsActive)Evaluate(true);}
        catch(Exception error){Fault(error,run);throw;}
        return task;
    }
    public void Restart(AnimationClock clock=AnimationClock.Application,CancellationToken cancellationToken=default)
    {
        Verify();if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        if(clock==AnimationClock.Application)SwealiumApplication.VerifyAnimationAccess();
        long previousRun=runId;
        if(IsActive)CancelCore(AnimationCancellationReason.Replaced);
        // A cancellation callback may explicitly start a new run; do not replace it again.
        if(runId==previousRun&&State!=AnimationState.Disposed)Start(clock,cancellationToken);
    }
    public bool Pause()
    {
        Verify();if(State is not (AnimationState.Running or AnimationState.Delayed))return false;
        resumeState=State;State=AnimationState.Paused;revision++;long run=runId;
        try{OnPaused?.Invoke();}catch(Exception error){Fault(error,run);throw;}return true;
    }
    public bool Resume()
    {
        Verify();if(State!=AnimationState.Paused)return false;
        State=resumeState;revision++;lastTimestamp=Stopwatch.GetTimestamp();long run=runId;
        try{OnResumed?.Invoke();}catch(Exception error){Fault(error,run);throw;}return true;
    }
    public bool Cancel() { Verify();return CancelCore(AnimationCancellationReason.Requested); }
    private bool CancelCore(AnimationCancellationReason reason)
    {
        if(!IsActive)return false;
        Detach();State=AnimationState.Cancelled;revision++;completion!.TrySetResult(AnimationState.Cancelled);OnCancelled?.Invoke(reason);return true;
    }
    private bool CheckCancellation() {if(!cancellation.IsCancellationRequested)return false;CancelCore(AnimationCancellationReason.CancellationToken);return true;}
    /// <summary>Manually advance a started manual-clock run. Paused runs do not consume time.</summary>
    public void Advance(TimeSpan delta)
    {
        Verify();if(clock!=AnimationClock.Manual||State==AnimationState.Idle)throw new InvalidOperationException("Start with AnimationClock.Manual before advancing.");
        if(delta<TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(delta));AdvanceCore(delta.Ticks);
    }
    private void Tick(long now)
    {
        if(now<lastTimestamp)return;
        double ticks=Stopwatch.GetElapsedTime(lastTimestamp,now).Ticks;lastTimestamp=now;AdvanceCore(ticks);
    }
    private void AdvanceCore(double ticks)
    {
        if(!IsActive||CheckCancellation()||State==AnimationState.Paused)return;
        double next=elapsedTicks+ticks*playbackRate;if(!double.IsFinite(next))throw new ArgumentOutOfRangeException(nameof(ticks));
        elapsedTicks=next;long run=runId;
        try{Evaluate(false);}catch(Exception error){Fault(error,run);throw;}
    }
    /// <summary>Seek within the current iteration without completing it. A paused run stays paused.</summary>
    public void Seek(double progress)
    {
        Verify();if(!IsActive)throw new InvalidOperationException("Seek requires an active run.");
        if(!double.IsFinite(progress)||progress<0||progress>1)throw new ArgumentOutOfRangeException(nameof(progress));
        if(CheckCancellation())return;
        elapsedTicks=delay.Ticks+Iteration*((double)duration.Ticks+repeatDelay.Ticks)+progress*duration.Ticks;
        if(State==AnimationState.Paused)resumeState=AnimationState.Running;else State=AnimationState.Running;
        revision++;lastTimestamp=Stopwatch.GetTimestamp();inGap=false;long run=runId;
        try{Sample(progress,Iteration);}catch(Exception error){Fault(error,run);throw;}
    }
    /// <summary>Publish the final endpoint and complete. Infinite playback finishes its current iteration.</summary>
    public bool Finish()
    {
        Verify();if(!IsActive||CheckCancellation())return false;
        revision++;long run=runId,version=revision;long final=repeatCount<0?Iteration:repeatCount;
        elapsedTicks=delay.Ticks+final*((double)duration.Ticks+repeatDelay.Ticks)+duration.Ticks;
        try
        {
            Sample(1,final);
            if(revision==version&&IsActive)Complete();
        }
        catch(Exception error){Fault(error,run);throw;}return true;
    }
    private void Evaluate(bool initial)
    {
        double active=elapsedTicks-delay.Ticks,d=duration.Ticks,gap=repeatDelay.Ticks;
        if(active<0){State=AnimationState.Delayed;if(initial)Sample(0,0);return;}
        State=AnimationState.Running;
        double total=repeatCount<0?double.PositiveInfinity:d*(repeatCount+1d)+gap*repeatCount;
        if(active>=total)
        {
            elapsedTicks=delay.Ticks+total;
            long version=revision;Sample(1,repeatCount);
            if(version==revision&&IsActive)Complete();return;
        }
        double cycle=d+gap;
        double iteration=cycle==0?0:Math.Floor(active/cycle);
        if(iteration>=(double)long.MaxValue)throw new InvalidOperationException("Animation iteration count exceeds Int64.");
        long index=(long)iteration;double within=active-index*cycle;
        bool waiting=within>=d;
        if(waiting&&inGap&&index==Iteration&&!initial)return;
        inGap=waiting;double progress=d==0?1:Math.Clamp(within/d,0,1);Sample(progress,index);
    }
    private void Sample(double progress,long index)
    {
        long version=revision;
        double eased=progress==0?0:progress==1?1:interpolation(progress);
        if(!double.IsFinite(eased))throw new InvalidOperationException("Interpolation must return a finite value.");
        if(version!=revision||!IsActive)return;
        bool reverse=(direction==AnimationDirection.Reverse)^(autoReverse&&(index&1)!=0);
        double fraction=reverse?1-eased:eased;
        T value=valueInterpolation(from,to,fraction);
        // Still evaluate the typed interpolator at endpoints for validation, then preserve exact endpoints.
        if(fraction==0)value=from;else if(fraction==1)value=to;
        if(version!=revision||!IsActive)return;
        ValidateValue(value);long previous=Iteration;Iteration=index;Progress=progress;EasedProgress=fraction;Value=value;
        if(index!=previous){OnRepeated?.Invoke(index);if(version!=revision||!IsActive)return;}
        OnUpdate?.Invoke(value);
    }
    private void Complete()
    {
        Detach();State=AnimationState.Completed;revision++;completion!.TrySetResult(AnimationState.Completed);OnCompleted?.Invoke();
    }
    private void Fault(Exception error,long run)
    {
        if(runId!=run||!IsActive)return;
        Detach();State=AnimationState.Faulted;revision++;completion!.TrySetException(error);
        try{OnFailed?.Invoke(error);}catch{/* Preserve the original callback/interpolation exception. */}
    }
    private void Detach(){if(registered){SwealiumApplication.UnregisterAnimation(registration);registered=false;}}
    public void Dispose()
    {
        if(Environment.CurrentManagedThreadId!=threadId)throw new InvalidOperationException("Animator operations require the creating thread.");
        if(State==AnimationState.Disposed)return;
        bool wasActive=IsActive;Detach();State=AnimationState.Disposed;revision++;
        if(wasActive){completion!.TrySetResult(AnimationState.Cancelled);OnCancelled?.Invoke(AnimationCancellationReason.Disposed);}
    }
}

internal sealed class AnimationRegistration(Action<long> tick,Action stop)
{
    internal void Tick(long now)=>tick(now);
    internal void Stop()=>stop();
}
