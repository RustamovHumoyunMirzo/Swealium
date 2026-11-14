using System.Collections.ObjectModel;
using Swealium.Graphics;
namespace Swealium.UI;

public class Container : Widget
{
    private LayoutMode layout = LayoutMode.Vertical;
    private float gap;
    private Alignment horizontalContentAlignment = Alignment.Stretch, verticalContentAlignment = Alignment.Start;
    public Container() => Children = new WidgetCollection(this);
    public LayoutMode Layout { get => layout; set { layout = value; InvalidateLayout(); } }
    public float Gap
    {
        get => gap;
        set { if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); gap = value; InvalidateLayout(); }
    }
    public Alignment HorizontalContentAlignment { get => horizontalContentAlignment; set { horizontalContentAlignment = value; InvalidateLayout(); } }
    public Alignment VerticalContentAlignment { get => verticalContentAlignment; set { verticalContentAlignment = value; InvalidateLayout(); } }
    /// <summary>Insertion order controls flow. ZIndex controls drawing and hit-testing order.</summary>
    public WidgetCollection Children { get; }
    internal Widget[] PaintOrder => Children.OrderBy(child => child.ZIndex).ToArray();
    public Color Background { get; set; } = Color.Transparent;
    public override void Draw(DrawingContext context) =>
        context.FillRectangle(new(0, 0, ActualWidth, ActualHeight), Background, Radii);

    protected override Size MeasureContent(Size available)
    {
        float width = 0, height = 0;
        int count = 0;
        foreach (var child in Children)
        {
            child.Measure(Layout switch
            {
                LayoutMode.Vertical => new(available.Width, float.PositiveInfinity),
                LayoutMode.Horizontal => new(float.PositiveInfinity, available.Height),
                _ => available
            });
            if (!child.IsVisible) continue;
            Size desired = child.DesiredSize;
            switch (Layout)
            {
                case LayoutMode.Vertical: width = Math.Max(width, desired.Width); height += desired.Height; break;
                case LayoutMode.Horizontal: width += desired.Width; height = Math.Max(height, desired.Height); break;
                case LayoutMode.Absolute:
                    width = Math.Max(width, child.X + desired.Width);
                    height = Math.Max(height, child.Y + desired.Height);
                    break;
                default: width = Math.Max(width, desired.Width); height = Math.Max(height, desired.Height); break;
            }
            count++;
        }
        if (Layout == LayoutMode.Vertical) height += Gap * Math.Max(0, count - 1);
        if (Layout == LayoutMode.Horizontal) width += Gap * Math.Max(0, count - 1);
        return new(width, height);
    }
    protected override void ArrangeContent(Rect area)
    {
        foreach (var hidden in Children.Where(child => !child.IsVisible))
            hidden.ArrangeSlot(new(area.X, area.Y, 0, 0), Alignment.Start, Alignment.Start);
        var children = Children.Where(child => child.IsVisible).ToArray();
        if (Layout is LayoutMode.Overlay or LayoutMode.Absolute)
        {
            foreach (var child in children)
            {
                Rect slot = Layout == LayoutMode.Absolute
                    ? new(area.X + child.X, area.Y + child.Y, Math.Max(0, area.Width - child.X), Math.Max(0, area.Height - child.Y))
                    : area;
                child.ArrangeSlot(slot, HorizontalContentAlignment, VerticalContentAlignment);
            }
            return;
        }
        bool vertical = Layout == LayoutMode.Vertical;
        float[] lengths = children.Select(child => vertical ? child.DesiredSize.Height : child.DesiredSize.Width).ToArray();
        float gaps = Gap * Math.Max(0, children.Length - 1);
        float free = Math.Max(0, (vertical ? area.Height : area.Width) - lengths.Sum() - gaps);
        // Redistribute unused shares when a growing child reaches its max size.
        var growing = Enumerable.Range(0, children.Length).Where(i => children[i].Grow > 0).ToList();
        while (free > 0.001f && growing.Count > 0)
        {
            double weightScale = growing.Max(i => children[i].Grow);
            double weightSum = growing.Sum(i => children[i].Grow / weightScale);
            float before = free, distributed = 0;
            foreach (int i in growing.ToArray())
            {
                Widget child = children[i];
                float maximum = vertical ? Math.Max(child.MinHeight, child.MaxHeight) + child.Margin.Vertical
                    : Math.Max(child.MinWidth, child.MaxWidth) + child.Margin.Horizontal;
                float amount = Math.Min(Math.Max(0, maximum - lengths[i]), (float)(before * (child.Grow / weightScale) / weightSum));
                lengths[i] += amount;
                distributed += amount;
                if (maximum - lengths[i] <= 0.001f) growing.Remove(i);
            }
            free = Math.Max(0, free - distributed);
            if (distributed <= 0.001f) break;
        }
        float remaining = (vertical ? area.Height : area.Width) - lengths.Sum() - gaps;
        float cursor = (vertical ? area.Y : area.X) + LayoutEngine.Offset(remaining, vertical ? VerticalContentAlignment : HorizontalContentAlignment);
        for (int i = 0; i < children.Length; i++)
        {
            Widget child = children[i];
            Rect slot = vertical ? new(area.X, cursor, area.Width, lengths[i]) : new(cursor, area.Y, lengths[i], area.Height);
            child.ArrangeSlot(slot, vertical ? HorizontalContentAlignment : Alignment.Start,
                vertical ? Alignment.Start : VerticalContentAlignment,
                forceWidth: !vertical && child.Grow > 0, forceHeight: vertical && child.Grow > 0);
            cursor += lengths[i] + Gap;
        }
    }
}

public sealed class WidgetCollection : Collection<Widget>
{
    private readonly Container owner;
    internal WidgetCollection(Container owner) => this.owner = owner;
    private void Validate(Widget item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Parent is not null) throw new InvalidOperationException("Remove a widget from its parent before attaching it again.");
        if (item.RootOwner is not null) throw new InvalidOperationException("Detach a window root before reparenting it.");
        for (Widget? node = owner; node is not null; node = node.Parent)
            if (ReferenceEquals(item, node)) throw new InvalidOperationException("A visual tree cannot contain cycles.");
    }
    protected override void InsertItem(int index, Widget item)
    {
        Validate(item);
        base.InsertItem(index, item);
        item.Parent = owner;
        owner.InvalidateLayout();
    }
    protected override void SetItem(int index, Widget item)
    {
        if (ReferenceEquals(this[index], item)) return;
        Validate(item);
        var previous = this[index];
        base.SetItem(index, item);
        previous.Parent = null;
        item.Parent = owner;
        owner.InvalidateLayout();
    }
    protected override void RemoveItem(int index)
    {
        var previous = this[index];
        base.RemoveItem(index);
        previous.Parent = null;
        owner.InvalidateLayout();
    }
    protected override void ClearItems()
    {
        foreach (var child in this) child.Parent = null;
        base.ClearItems();
        owner.InvalidateLayout();
    }
}

