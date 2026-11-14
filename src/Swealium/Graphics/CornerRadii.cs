namespace Swealium.Graphics;

/// <summary>Circular corner radii in clockwise order: top-left, top-right, bottom-right, bottom-left.</summary>
public readonly record struct CornerRadii
{
    public CornerRadii(float uniform) : this(uniform, uniform, uniform, uniform) { }
    public CornerRadii(float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        Validate(topLeft); Validate(topRight); Validate(bottomRight); Validate(bottomLeft);
        TopLeft = topLeft; TopRight = topRight; BottomRight = bottomRight; BottomLeft = bottomLeft;
    }
    public float TopLeft { get; }
    public float TopRight { get; }
    public float BottomRight { get; }
    public float BottomLeft { get; }
    private static void Validate(float value)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
    }
    /// <summary>Scale all radii together when adjacent corners would overlap.</summary>
    public CornerRadii Normalize(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0) return default;
        double scale = 1;
        Limit(size.Width, (double)TopLeft + TopRight);
        Limit(size.Width, (double)BottomLeft + BottomRight);
        Limit(size.Height, (double)TopLeft + BottomLeft);
        Limit(size.Height, (double)TopRight + BottomRight);
        return new((float)(TopLeft * scale), (float)(TopRight * scale), (float)(BottomRight * scale), (float)(BottomLeft * scale));
        void Limit(double edge, double sum) { if (sum > 0) scale = Math.Min(scale, edge / sum); }
    }
    public CornerRadii Inset(float amount) => new(
        Math.Max(0, TopLeft - amount), Math.Max(0, TopRight - amount),
        Math.Max(0, BottomRight - amount), Math.Max(0, BottomLeft - amount));

    public bool Contains(Point local, Size size)
    {
        if (!new Rect(0, 0, size.Width, size.Height).Contains(local)) return false;
        var r = Normalize(size);
        if (local.X < r.TopLeft && local.Y < r.TopLeft) return InCircle(local.X - r.TopLeft, local.Y - r.TopLeft, r.TopLeft);
        if (local.X > size.Width - r.TopRight && local.Y < r.TopRight) return InCircle(local.X - size.Width + r.TopRight, local.Y - r.TopRight, r.TopRight);
        if (local.X > size.Width - r.BottomRight && local.Y > size.Height - r.BottomRight) return InCircle(local.X - size.Width + r.BottomRight, local.Y - size.Height + r.BottomRight, r.BottomRight);
        if (local.X < r.BottomLeft && local.Y > size.Height - r.BottomLeft) return InCircle(local.X - r.BottomLeft, local.Y - size.Height + r.BottomLeft, r.BottomLeft);
        return true;
    }
    private static bool InCircle(float x, float y, float radius) =>
        (double)x * x + (double)y * y <= (double)radius * radius;
}

