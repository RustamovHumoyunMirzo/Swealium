using Swealium.Graphics;
namespace Swealium.UI.Effects;

/// <summary>Gaussian blur of the complete widget subtree. Output can extend beyond its own clip.</summary>
public sealed class LayerBlur : WidgetEffect
{
    private Size radius = new(8,8);
    private BlurEdgeMode edgeMode;
    public LayerBlur(float radius = 8) : this(new Size(radius,radius)) { }
    public LayerBlur(Size radius) => Radius = radius;
    public Size Radius { get => radius; set { BlurGradient.ValidateRadius(value); radius = value; } }
    public BlurEdgeMode EdgeMode { get => edgeMode; set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); edgeMode = value; } }
    public override IDisposable Begin(DrawingContext context, Widget widget) => context.PushBlur(Radius, EdgeMode);
}

/// <summary>Gaussian blur of previously painted content, clipped to the widget's rounded bounds.</summary>
public sealed class BackdropBlur : WidgetEffect
{
    private Size radius = new(8,8);
    private BlurEdgeMode edgeMode = BlurEdgeMode.Clamp;
    public BackdropBlur(float radius = 8) : this(new Size(radius,radius)) { }
    public BackdropBlur(Size radius) => Radius = radius;
    public Size Radius { get => radius; set { BlurGradient.ValidateRadius(value); radius = value; } }
    public BlurEdgeMode EdgeMode { get => edgeMode; set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); edgeMode = value; } }
    public override bool ReadsBackdrop => true;
    public override void PrepareDraw(DrawingContext context, Widget widget) => context.DrawBackdropBlur(new(0,0,widget.ActualWidth,widget.ActualHeight), widget.Radii, Radius, EdgeMode);
}

public enum BlurTarget { Layer, Backdrop }

/// <summary>Spatially varying Gaussian blur, blended from a fixed number of blur levels.</summary>
public sealed class ProgressiveBlur : WidgetEffect
{
    private BlurGradient gradient;
    private BlurTarget target;
    private BlurEdgeMode edgeMode = BlurEdgeMode.Clamp;
    public ProgressiveBlur(BlurGradient gradient, BlurTarget target = BlurTarget.Layer) { this.gradient = gradient ?? throw new ArgumentNullException(nameof(gradient)); Target = target; }
    public BlurGradient Gradient { get => gradient; set => gradient = value ?? throw new ArgumentNullException(nameof(value)); }
    public BlurTarget Target { get => target; set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); target = value; } }
    public BlurEdgeMode EdgeMode { get => edgeMode; set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); edgeMode = value; } }
    public override bool ReadsBackdrop => Target == BlurTarget.Backdrop;
    public override IDisposable? Begin(DrawingContext context, Widget widget) => Target == BlurTarget.Layer ? context.PushProgressiveBlur(Gradient, new(0,0,widget.ActualWidth,widget.ActualHeight), EdgeMode) : null;
    public override void PrepareDraw(DrawingContext context, Widget widget)
    { if (Target == BlurTarget.Backdrop) context.DrawBackdropBlur(new(0,0,widget.ActualWidth,widget.ActualHeight), widget.Radii, Gradient, EdgeMode); }
}
