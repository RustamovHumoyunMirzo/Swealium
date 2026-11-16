using Swealium.Graphics;
namespace Swealium.UI.Effects;

/// <summary>Shadow follows rendered alpha, including children and rounded/transparent silhouettes.</summary>
public sealed class DropShadow : WidgetEffect
{
    private Size radius = new(8,8);
    private Point offset = new(0,4);
    private Color color = new(0,0,0,110);
    public Size Radius { get => radius; set { BlurGradient.ValidateRadius(value); radius = value; } }
    public Point Offset { get => offset; set { if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(nameof(value)); offset = value; } }
    public Color Color { get => color; set { if (value.IsGradient) throw new ArgumentException("Shadows require a solid color.", nameof(value)); color = value; } }
    public bool ShadowOnly { get; set; }
    public override IDisposable Begin(DrawingContext context, Widget widget) => context.PushDropShadow(Offset, Radius, Color, ShadowOnly);
}

/// <summary>RGBA color matrix applied once to the complete widget subtree.</summary>
public sealed class ColorFilter : WidgetEffect
{
    private ColorMatrix matrix = ColorMatrix.Identity;
    public ColorFilter() { }
    public ColorFilter(ColorMatrix matrix) => Matrix = matrix;
    public ColorMatrix Matrix { get => matrix; set => matrix = value ?? throw new ArgumentNullException(nameof(value)); }
    public override IDisposable Begin(DrawingContext context, Widget widget) => context.PushColorMatrix(Matrix);
}
