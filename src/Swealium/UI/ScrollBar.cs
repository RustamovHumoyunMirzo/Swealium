using Swealium.Graphics;
namespace Swealium.UI;

[Flags]
public enum ScrollDirection { None=0, Horizontal=1, Vertical=2, Both=Horizontal|Vertical }
public enum ScrollBarVisibility { Auto, Always, Hidden }
public enum ScrollAxis { Horizontal, Vertical }

/// <summary>Immutable appearance and geometry for one scrollbar. Override DrawScrollBar for custom drawing.</summary>
public sealed record ScrollBarStyle
{
    public float Thickness { get; init; } = 12;
    public float Inset { get; init; } = 2;
    public float MinimumThumbLength { get; init; } = 24;
    public Color TrackColor { get; init; } = new(225,228,235);
    public Color ThumbColor { get; init; } = new(135,145,160);
    public Color HoverThumbColor { get; init; } = new(105,120,145);
    public Color PressedThumbColor { get; init; } = new(65,95,145);
    public CornerRadii TrackRadii { get; init; } = new(6);
    public CornerRadii ThumbRadii { get; init; } = new(6);
    internal void Validate()
    {
        if (!float.IsFinite(Thickness) || Thickness<=0 || !float.IsFinite(Inset) || Inset<0 ||
            !float.IsFinite(MinimumThumbLength) || MinimumThumbLength<0) throw new ArgumentOutOfRangeException(nameof(ScrollBarStyle));
    }
}

/// <summary>Local scrollbar geometry and state. Invisible bars have empty rectangles.</summary>
public readonly record struct ScrollBarInfo(ScrollAxis Axis, bool IsVisible, Rect Track, Rect Thumb,
    float Offset, float MaximumOffset, float ViewportLength, float ExtentLength, bool IsHovered, bool IsDragging);
public sealed record ScrollChangedEvent(Point PreviousOffset, Point Offset, Size Extent, Rect Viewport);
