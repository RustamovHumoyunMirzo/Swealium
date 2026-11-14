namespace Swealium.Graphics;

/// <summary>Nonnegative spacing in left, top, right, bottom order.</summary>
public readonly record struct Thickness
{
    public Thickness(float uniform) : this(uniform, uniform, uniform, uniform) { }
    public Thickness(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }
    public Thickness(float left, float top, float right, float bottom)
    {
        Validate(left); Validate(top); Validate(right); Validate(bottom);
        Left = left; Top = top; Right = right; Bottom = bottom;
    }
    public float Left { get; }
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }
    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;
    private static void Validate(float value)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
    }
}

