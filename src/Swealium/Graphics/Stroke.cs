namespace Swealium.Graphics;

public enum StrokeStyle { Solid, Dashed, Dotted, Double, Hidden }

/// <summary>One inside border edge; its width is finite and nonnegative.</summary>
public readonly record struct StrokeSide
{
    public StrokeSide(float width, Color color, StrokeStyle style = StrokeStyle.Solid)
    {
        if (!float.IsFinite(width) || width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(style)) throw new ArgumentOutOfRangeException(nameof(style));
        Width = width; Color = color; Style=style;
    }
    public float Width { get; }
    public Color Color { get; }
    public StrokeStyle Style { get; }
}

/// <summary>Inside decorative border in left, top, right, bottom order. It does not consume layout space.</summary>
public readonly record struct Stroke
{
    public Stroke(float width, Color color, StrokeStyle style = StrokeStyle.Solid) : this(new StrokeSide(width,color,style)) { }
    public Stroke(StrokeSide uniform) : this(uniform,uniform,uniform,uniform) { }
    public Stroke(Thickness widths, Color color, StrokeStyle style = StrokeStyle.Solid) : this(new(widths.Left,color,style),new(widths.Top,color,style),new(widths.Right,color,style),new(widths.Bottom,color,style)) { }
    public Stroke(StrokeSide left, StrokeSide top, StrokeSide right, StrokeSide bottom)
    { Left=left; Top=top; Right=right; Bottom=bottom; }
    public StrokeSide Left { get; }
    public StrokeSide Top { get; }
    public StrokeSide Right { get; }
    public StrokeSide Bottom { get; }
}
