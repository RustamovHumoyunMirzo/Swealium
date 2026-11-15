using Swealium.Graphics;
namespace Swealium.UI;

/// <summary>Flows children across a finite boundary, then starts another row or column.</summary>
public class WrapContainer : Container
{
    private FlowDirection direction;
    private float? lineGap;
    private Rect[] lines = [];
    public FlowDirection Direction
    {
        get => direction;
        set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); direction = value; InvalidateLayout(); }
    }
    public float? LineGap
    {
        get => lineGap;
        set { if (value is { } v && (!float.IsFinite(v) || v < 0)) throw new ArgumentOutOfRangeException(nameof(value)); lineGap = value; InvalidateLayout(); }
    }
    public int LineCount => lines.Length;
    public IReadOnlyList<Rect> Lines => Array.AsReadOnly((Rect[])lines.Clone());

    protected override Size MeasureContent(Size available)
    {
        var groups = BuildLines(available);
        bool vertical = Direction == FlowDirection.Vertical;
        float main = groups.Select(g => g.Lengths.Sum() + Gap * Math.Max(0, g.Children.Length - 1)).DefaultIfEmpty().Max();
        float cross = groups.Sum(g => g.Cross) + (LineGap ?? Gap) * Math.Max(0, groups.Count - 1);
        return vertical ? new(cross, main) : new(main, cross);
    }
    protected override void ArrangeContent(Rect area)
    {
        bool vertical = Direction == FlowDirection.Vertical;
        var groups = BuildLines(new(area.Width, area.Height));
        float totalCross = groups.Sum(g => g.Cross) + (LineGap ?? Gap) * Math.Max(0, groups.Count - 1);
        float cross = (vertical ? area.X : area.Y) + LayoutEngine.Offset(
            (vertical ? area.Width : area.Height) - totalCross, vertical ? HorizontalContentAlignment : VerticalContentAlignment);
        var rectangles = new List<Rect>();
        foreach (var group in groups)
        {
            float used = group.Lengths.Sum() + Gap * Math.Max(0, group.Children.Length - 1);
            float main = (vertical ? area.Y : area.X) + LayoutEngine.Offset(
                (vertical ? area.Height : area.Width) - used, vertical ? VerticalContentAlignment : HorizontalContentAlignment);
            rectangles.Add(vertical ? new(cross, main, group.Cross, used) : new(main, cross, used, group.Cross));
            for (int i = 0; i < group.Children.Length; i++)
            {
                var child = group.Children[i];
                var slot = vertical ? new Rect(cross, main, group.Cross, group.Lengths[i])
                    : new Rect(main, cross, group.Lengths[i], group.Cross);
                child.ArrangeSlot(slot, vertical ? HorizontalContentAlignment : Alignment.Start,
                    vertical ? Alignment.Start : VerticalContentAlignment,
                    !vertical && child.Weight > 0, vertical && child.Weight > 0);
                main += group.Lengths[i] + Gap;
            }
            cross += group.Cross + (LineGap ?? Gap);
        }
        foreach (var hidden in Children.Where(child => !child.IsVisible))
            hidden.ArrangeSlot(new(area.X, area.Y, 0, 0), Alignment.Start, Alignment.Start);
        lines = rectangles.ToArray();
    }
    private List<Line> BuildLines(Size available)
    {
        bool vertical = Direction == FlowDirection.Vertical;
        float boundary = vertical ? available.Height : available.Width;
        var groups = new List<Widget[]>();
        var current = new List<Widget>();
        float used = 0;
        foreach (var child in Children)
        {
            child.Measure(vertical ? new(float.PositiveInfinity, available.Height) : new(available.Width, float.PositiveInfinity));
            if (!child.IsVisible) continue;
            float length = child.Weight > 0 && child.WeightBasis == WeightBasis.Zero && float.IsFinite(boundary)
                ? vertical ? child.MinHeight + child.Margin.Vertical : child.MinWidth + child.Margin.Horizontal
                : vertical ? child.DesiredSize.Height : child.DesiredSize.Width;
            float next = used + (current.Count > 0 ? Gap : 0) + length;
            if (current.Count > 0 && float.IsFinite(boundary) && next > boundary + 0.001f)
            {
                groups.Add(current.ToArray()); current.Clear(); used = 0;
            }
            used += (current.Count > 0 ? Gap : 0) + length;
            current.Add(child);
        }
        if (current.Count > 0) groups.Add(current.ToArray());
        var result = new List<Line>();
        foreach (var children in groups)
        {
            float[] lengths = LayoutWeights.Allocate(children, boundary, Gap, vertical);
            for (int i = 0; i < children.Length; i++)
                if (children[i].Weight > 0 && float.IsFinite(boundary))
                    children[i].MeasureAllocated(vertical ? new(float.PositiveInfinity, lengths[i]) : new(lengths[i], float.PositiveInfinity), !vertical, vertical);
            float cross = children.Select(child => vertical ? child.DesiredSize.Width : child.DesiredSize.Height).DefaultIfEmpty().Max();
            result.Add(new(children, lengths, cross));
        }
        return result;
    }
    private sealed record Line(Widget[] Children, float[] Lengths, float Cross);
}

