using System.Collections.ObjectModel;
namespace Swealium.UI;

public enum GridUnit { Fixed, Content, Fraction }

/// <summary>A fixed logical length, intrinsic content track, or positive weighted share.</summary>
public readonly record struct GridLength
{
    public GridLength(GridUnit unit, float value = 1)
    {
        if (!Enum.IsDefined(unit)) throw new ArgumentOutOfRangeException(nameof(unit));
        if (!float.IsFinite(value) || value < 0 || (unit == GridUnit.Fraction && value == 0))
            throw new ArgumentOutOfRangeException(nameof(value));
        Unit = unit;
        Value = unit == GridUnit.Content ? 0 : value;
    }
    public GridUnit Unit { get; }
    public float Value { get; }
    public static GridLength Fixed(float length) => new(GridUnit.Fixed, length);
    public static GridLength Content => new(GridUnit.Content);
    public static GridLength Fraction(float weight = 1) => new(GridUnit.Fraction, weight);
}

public sealed class GridTrack
{
    private GridLength length = GridLength.Fraction();
    private float minSize, maxSize = float.PositiveInfinity;
    internal GridTrackCollection? Collection { get; set; }
    public GridTrack() { }
    public GridTrack(GridLength length) => this.length = length;
    public GridLength Length { get => length; set { length = value; Collection?.Invalidate(); } }
    public float MinSize
    {
        get => minSize;
        set { Validate(value, false); minSize = value; Collection?.Invalidate(); }
    }
    public float MaxSize
    {
        get => maxSize;
        set { Validate(value, true); maxSize = value; Collection?.Invalidate(); }
    }
    public float ActualSize { get; internal set; }
    private static void Validate(float value, bool infinity)
    {
        if (float.IsNaN(value) || value < 0 || (!infinity && !float.IsFinite(value)))
            throw new ArgumentOutOfRangeException(nameof(value));
    }
}

/// <summary>A grid-owned, non-shareable collection of row or column definitions.</summary>
public sealed class GridTrackCollection : Collection<GridTrack>
{
    private readonly Grid owner;
    internal GridTrackCollection(Grid owner) => this.owner = owner;
    internal void Invalidate() => owner.InvalidateLayout();
    private static void Validate(GridTrack item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Collection is not null) throw new InvalidOperationException("Remove a track from its collection before reusing it.");
    }
    protected override void InsertItem(int index, GridTrack item)
    {
        Validate(item); base.InsertItem(index, item); item.Collection = this; Invalidate();
    }
    protected override void SetItem(int index, GridTrack item)
    {
        if (ReferenceEquals(this[index], item)) return;
        Validate(item);
        var old = this[index]; base.SetItem(index, item);
        old.Collection = null; old.ActualSize = 0; item.Collection = this; Invalidate();
    }
    protected override void RemoveItem(int index)
    {
        var old = this[index]; base.RemoveItem(index);
        old.Collection = null; old.ActualSize = 0; Invalidate();
    }
    protected override void ClearItems()
    {
        foreach (var track in this) { track.Collection = null; track.ActualSize = 0; }
        base.ClearItems(); Invalidate();
    }
}

