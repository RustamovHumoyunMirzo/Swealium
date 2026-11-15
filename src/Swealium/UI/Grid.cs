using Swealium.Graphics;
namespace Swealium.UI;

/// <summary>Two-dimensional layout with fixed, content, and fractional tracks and explicit spanning cells.</summary>
public class Grid : Container
{
    private float? rowGap, columnGap;
    private float[] rowSizes = [], columnSizes = [];
    public Grid()
    {
        Rows = new(this); Columns = new(this);
        VerticalContentAlignment = Alignment.Stretch;
    }
    public GridTrackCollection Rows { get; }
    public GridTrackCollection Columns { get; }
    /// <summary>Null inherits Gap.</summary>
    public float? RowGap { get => rowGap; set { ValidateGap(value); rowGap = value; InvalidateLayout(); } }
    public float? ColumnGap { get => columnGap; set { ValidateGap(value); columnGap = value; InvalidateLayout(); } }
    public IReadOnlyList<float> RowSizes => Array.AsReadOnly((float[])rowSizes.Clone());
    public IReadOnlyList<float> ColumnSizes => Array.AsReadOnly((float[])columnSizes.Clone());

    protected override Size MeasureContent(Size available)
    {
        var result = Solve(new(
            WidthSizing == SizingMode.Content ? float.PositiveInfinity : available.Width,
            HeightSizing == SizingMode.Content ? float.PositiveInfinity : available.Height));
        float width = Extent(result.Widths, ColumnGap ?? Gap);
        if (WidthSizing == SizingMode.Content)
        {
            // Reflow height-dependent cells after the intrinsic width is known and
            // bounded. Without this pass, a max-width content grid can crop wrapped cells.
            width = Math.Min(width, available.Width);
            result = Solve(new(width, HeightSizing == SizingMode.Content ? float.PositiveInfinity : available.Height));
        }
        float height = Extent(result.Heights, RowGap ?? Gap);
        return new(width, HeightSizing == SizingMode.Content ? Math.Min(height, available.Height) : height);
    }
    protected override void ArrangeContent(Rect contentBounds)
    {
        var result = Solve(new(contentBounds.Width, contentBounds.Height));
        columnSizes = result.Widths; rowSizes = result.Heights;
        for (int i = 0; i < Columns.Count; i++) Columns[i].ActualSize = columnSizes[i];
        for (int i = 0; i < Rows.Count; i++) Rows[i].ActualSize = rowSizes[i];
        float x = contentBounds.X + LayoutEngine.Offset(contentBounds.Width - Extent(columnSizes, ColumnGap ?? Gap), HorizontalContentAlignment);
        float y = contentBounds.Y + LayoutEngine.Offset(contentBounds.Height - Extent(rowSizes, RowGap ?? Gap), VerticalContentAlignment);
        var xs = Offsets(columnSizes, ColumnGap ?? Gap, x);
        var ys = Offsets(rowSizes, RowGap ?? Gap, y);
        foreach (var child in Children.ToArray())
        {
            if (!child.IsVisible)
            {
                child.ArrangeSlot(new(contentBounds.X, contentBounds.Y, 0, 0), Alignment.Start, Alignment.Start);
                continue;
            }
            child.ArrangeSlot(new(xs[child.GridColumn], ys[child.GridRow],
                SpanSize(columnSizes, child.GridColumn, child.GridColumnSpan, ColumnGap ?? Gap),
                SpanSize(rowSizes, child.GridRow, child.GridRowSpan, RowGap ?? Gap)),
                HorizontalContentAlignment, VerticalContentAlignment);
        }
    }
    private Result Solve(Size available)
    {
        var children = Children.Where(child => child.IsVisible).ToArray();
        foreach (var child in Children.Where(child => !child.IsVisible)) child.Measure(default);
        var columns = Build(Columns, children, false);
        var rows = Build(Rows, children, true);
        float cg = ColumnGap ?? Gap, rg = RowGap ?? Gap;
        float[] cw = Baselines(columns), rh = Baselines(rows);
        for (int pass = 0; pass < 32; pass++)
        {
            float[] widths = Resolve(columns, cw, available.Width, cg);
            float[] heights = Resolve(rows, rh, available.Height, rg);
            foreach (var child in children)
                child.Measure(new(
                    Constraint(columns, widths, child.GridColumn, child.GridColumnSpan, available.Width, cg),
                    Constraint(rows, heights, child.GridRow, child.GridRowSpan, available.Height, rg)));
            float[] nextColumns = Demands(columns, children, false, cg);
            float[] nextRows = Demands(rows, children, true, rg);
            if (Equivalent(cw, nextColumns) && Equivalent(rh, nextRows))
                return new(widths, heights);
            cw = nextColumns; rh = nextRows;
        }
        throw new InvalidOperationException("Grid measurement did not converge. Custom MeasureContent implementations must return stable sizes for stable constraints.");
    }
    private static Track[] Build(GridTrackCollection definitions, Widget[] children, bool vertical)
    {
        int count = Math.Max(1, definitions.Count);
        foreach (var child in children)
        {
            int end;
            try { end = checked((vertical ? child.GridRow : child.GridColumn) + (vertical ? child.GridRowSpan : child.GridColumnSpan)); }
            catch (OverflowException) { throw new ArgumentOutOfRangeException(nameof(children), "A grid cell range exceeds supported indices."); }
            count = Math.Max(count, end);
        }
        var tracks = new Track[count];
        for (int i = 0; i < count; i++)
            tracks[i] = i < definitions.Count
                ? new(definitions[i].Length, definitions[i].MinSize, Math.Max(definitions[i].MinSize, definitions[i].MaxSize))
                : new(i == 0 && definitions.Count == 0 ? GridLength.Fraction() : GridLength.Content, 0, float.PositiveInfinity);
        return tracks;
    }
    private static float[] Baselines(Track[] tracks) => tracks.Select(track =>
        track.Length.Unit == GridUnit.Fixed ? Math.Clamp(track.Length.Value, track.Min, track.Max) : track.Min).ToArray();

    private static float[] Resolve(Track[] tracks, float[] demand, float available, float gap)
    {
        var sizes = (float[])demand.Clone();
        if (float.IsPositiveInfinity(available)) return sizes;
        int[] fractions = Enumerable.Range(0, tracks.Length).Where(i => tracks[i].Length.Unit == GridUnit.Fraction).ToArray();
        if (fractions.Length == 0) return sizes;
        double budget = Math.Max(0, (double)available - gap * (tracks.Length - 1)
            - Enumerable.Range(0, tracks.Length).Where(i => tracks[i].Length.Unit != GridUnit.Fraction).Sum(i => (double)sizes[i]));
        double minimum = fractions.Sum(i => (double)tracks[i].Min);
        double maximum = fractions.Sum(i => (double)tracks[i].Max);
        if (budget <= minimum)
        {
            foreach (int i in fractions) sizes[i] = tracks[i].Min;
            return sizes;
        }
        if (budget >= maximum)
        {
            foreach (int i in fractions) sizes[i] = tracks[i].Max;
            return sizes;
        }
        // Solve sum(clamp(scale * weight, min, max)) = budget. Exponential bracketing
        // handles extreme weight ratios without locking a min/max track prematurely.
        double weightScale = fractions.Max(i => (double)tracks[i].Length.Value);
        double Sum(double scale) => fractions.Sum(i => Math.Clamp(scale * (tracks[i].Length.Value / weightScale), tracks[i].Min, tracks[i].Max));
        double low = 0, high = 1;
        while (Sum(high) < budget) high *= 2;
        for (int pass = 0; pass < 80; pass++)
        {
            double middle = low + (high - low) / 2;
            if (Sum(middle) < budget) low = middle; else high = middle;
        }
        foreach (int i in fractions)
            sizes[i] = (float)Math.Clamp(high * (tracks[i].Length.Value / weightScale), tracks[i].Min, tracks[i].Max);
        return sizes;
    }
    private static float Constraint(Track[] tracks, float[] sizes, int start, int span, float available, float gap)
    {
        bool content = false, fraction = false;
        for (int i = start; i < start + span; i++)
        {
            content |= tracks[i].Length.Unit == GridUnit.Content;
            fraction |= tracks[i].Length.Unit == GridUnit.Fraction;
        }
        if ((fraction && float.IsPositiveInfinity(available)) || (content && !fraction))
            return float.PositiveInfinity;
        return SpanSize(sizes, start, span, gap);
    }
    private static float[] Demands(Track[] tracks, Widget[] children, bool vertical, float gap)
    {
        float[] demand = Baselines(tracks);
        foreach (var child in children.OrderBy(child => vertical ? child.GridRowSpan : child.GridColumnSpan))
        {
            int start = vertical ? child.GridRow : child.GridColumn;
            int span = vertical ? child.GridRowSpan : child.GridColumnSpan;
            double required = vertical ? child.DesiredSize.Height : child.DesiredSize.Width;
            double deficit = required - SpanSize(demand, start, span, gap);
            if (deficit <= 0) continue;
            // Fractional cells absorb spanning demand first; fixed tracks never grow.
            deficit = GrowDemand(tracks, demand, start, span, deficit, GridUnit.Fraction);
            if (deficit > 0) _ = GrowDemand(tracks, demand, start, span, deficit, GridUnit.Content);
        }
        return demand;
    }
    private static double GrowDemand(Track[] tracks, float[] sizes, int start, int span, double remaining, GridUnit unit)
    {
        var active = Enumerable.Range(start, span).Where(i => tracks[i].Length.Unit == unit && sizes[i] < tracks[i].Max).ToList();
        while (remaining > 0.0001 && active.Count > 0)
        {
            double weightScale = unit == GridUnit.Fraction ? active.Max(i => (double)tracks[i].Length.Value) : 1;
            double Weight(int i) => unit == GridUnit.Fraction ? tracks[i].Length.Value / weightScale : 1;
            double total = active.Sum(Weight), before = remaining, distributed = 0;
            foreach (int i in active.ToArray())
            {
                double amount = Math.Min((double)tracks[i].Max - sizes[i], before * Weight(i) / total);
                float previous = sizes[i];
                sizes[i] = (float)Math.Min(float.MaxValue, sizes[i] + amount);
                distributed += sizes[i] - previous;
                if (sizes[i] >= tracks[i].Max) active.Remove(i);
            }
            remaining = Math.Max(0, remaining - distributed);
            if (distributed <= 0.0001) break;
        }
        return remaining;
    }
    private static bool Equivalent(float[] first, float[] second) =>
        first.Length == second.Length && first.Zip(second).All(pair => Math.Abs(pair.First - pair.Second) < 0.001f);
    private static float SpanSize(float[] sizes, int start, int span, float gap) =>
        CheckedExtent(Enumerable.Range(start, span).Sum(i => (double)sizes[i]) + (double)gap * (span - 1));
    private static float Extent(float[] sizes, float gap) =>
        CheckedExtent(sizes.Sum(value => (double)value) + (double)gap * Math.Max(0, sizes.Length - 1));
    private static float CheckedExtent(double value)
    {
        if (!double.IsFinite(value) || value > float.MaxValue) throw new InvalidOperationException("Grid extent exceeds finite layout coordinates.");
        return (float)value;
    }
    private static float[] Offsets(float[] sizes, float gap, float start)
    {
        var result = new float[sizes.Length];
        for (int i = 0; i < result.Length; i++) { result[i] = start; start += sizes[i] + gap; }
        return result;
    }
    private static void ValidateGap(float? value)
    {
        if (value is { } gap && (!float.IsFinite(gap) || gap < 0)) throw new ArgumentOutOfRangeException(nameof(value));
    }
    private readonly record struct Track(GridLength Length, float Min, float Max);
    private sealed record Result(float[] Widths, float[] Heights);
}

