using SkiaSharp;
using Swealium;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;

internal static class GridTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Near(float actual, float expected, string message = "size")
    {
        if (Math.Abs(actual - expected) > 0.02f) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
    }
    private static void Bounds(Widget widget, float x, float y, float width, float height)
    {
        Near(widget.Bounds.X, x, "x"); Near(widget.Bounds.Y, y, "y");
        Near(widget.ActualWidth, width, "width"); Near(widget.ActualHeight, height, "height");
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }

    internal static void Sizing()
    {
        var root = new Container { Layout = LayoutMode.Overlay, VerticalContentAlignment = Alignment.Stretch };
        var child = new Probe(30, 18)
        {
            Padding = new(2, 3), Margin = new(4),
            WidthSizing = SizingMode.Content, HeightSizing = SizingMode.Content
        };
        root.Children.Add(child);
        LayoutEngine.Layout(root, new(100, 80));
        Bounds(child, 4, 4, 34, 24);
        child.WidthSizing = SizingMode.Fill; child.HeightSizing = SizingMode.Fill;
        LayoutEngine.Layout(root, new(100, 80));
        Bounds(child, 4, 4, 92, 72);
        child.MaxWidth = 50; child.HorizontalAlignment = Alignment.Center;
        LayoutEngine.Layout(root, new(100, 80));
        Bounds(child, 25, 4, 50, 72);
        child.Width = 25;
        Check(child.WidthSizing == SizingMode.Automatic, "Numeric assignment restores legacy sizing");
        LayoutEngine.Layout(root, new(100, 80));
        Bounds(child, 37.5f, 4, 25, 72);
        child.WidthSizing = SizingMode.Content;
        Check(float.IsNaN(child.Width), "Named sizing clears numeric size");
        child.Width = float.NaN;
        Check(child.WidthSizing == SizingMode.Automatic, "NaN restores legacy auto/alignment sizing");

        var stack = new Container { Padding = new(10), Gap = 5 };
        var fill = new Probe(30, 18) { WidthSizing = SizingMode.Content, HeightSizing = SizingMode.Fill, Margin = new(2) };
        var after = new Probe(10, 10);
        stack.Children.Add(fill); stack.Children.Add(after);
        LayoutEngine.Layout(stack, new(100, 120));
        Bounds(fill, 12, 12, 30, 96);
        Near(after.Bounds.Y, 115, "Fill uses the entire padded parent main axis; it is not Grow");
        fill.Measure(new(float.PositiveInfinity, float.PositiveInfinity));
        Check(fill.DesiredSize == new Size(34, 22), "Unbounded fill falls back to finite intrinsic size");
        var cappedFill = new Probe(10, 8)
        {
            WidthSizing = SizingMode.Fill, HeightSizing = SizingMode.Fill, MaxWidth = 50, MaxHeight = 40
        };
        cappedFill.Measure(new(float.PositiveInfinity, float.PositiveInfinity));
        Check(cappedFill.DesiredSize == new Size(10, 8), "A maximum is a bound, not parent space to occupy during unbounded fill measurement");
        Throws<ArgumentOutOfRangeException>(() => fill.WidthSizing = (SizingMode)99);
    }

    internal static void Tracks()
    {
        var grid = new Grid { Padding = new(10), Gap = 10 };
        grid.Columns.Add(new(GridLength.Fixed(50)));
        grid.Columns.Add(new(GridLength.Content));
        grid.Columns.Add(new(GridLength.Fraction()));
        grid.Columns.Add(new(GridLength.Fraction(2)));
        grid.Rows.Add(new(GridLength.Fixed(30)));
        grid.Rows.Add(new(GridLength.Content));
        grid.Rows.Add(new(GridLength.Fraction()));
        var label = new Probe(70, 40) { GridRow = 1, GridColumn = 1 };
        var body = new Probe(8, 9) { GridRow = 2, GridColumn = 3, WidthSizing = SizingMode.Fill, HeightSizing = SizingMode.Fill };
        grid.Children.Add(label); grid.Children.Add(body);
        LayoutEngine.Layout(grid, new(400, 240));
        Bounds(label, 70, 50, 70, 40);
        Near(grid.ColumnSizes[2], 230f / 3);
        Near(grid.ColumnSizes[3], 460f / 3);
        Bounds(body, 236.66667f, 100, 153.33333f, 130);
        Near(grid.Rows[2].ActualSize, 130);
        Near(grid.Columns[1].ActualSize, 70);

        grid.RowGap = 4; grid.ColumnGap = 6;
        LayoutEngine.Layout(grid, new(400, 240));
        Near(label.Bounds.X, 66); Near(label.Bounds.Y, 44);
        Near(grid.RowSizes[2], 142);
        grid.RowGap = null; grid.ColumnGap = null;
        LayoutEngine.Layout(grid, new(400, 240));
        Bounds(label, 70, 50, 70, 40);
        label.HorizontalAlignment = Alignment.End; label.Width = 20;
        label.VerticalAlignment = Alignment.Center; label.Height = 10;
        grid.Columns[1].MinSize = 70; grid.Rows[1].MinSize = 40;
        LayoutEngine.Layout(grid, new(400, 240));
        Bounds(label, 120, 65, 20, 10);
    }

    internal static void Spans()
    {
        var grid = new Grid { Padding = new(10), ColumnGap = 10, RowGap = 5 };
        for (int i = 0; i < 3; i++) grid.Columns.Add(new(GridLength.Content));
        grid.Rows.Add(new(GridLength.Content)); grid.Rows.Add(new(GridLength.Content));
        grid.Children.Add(new Probe(20, 10));
        grid.Children.Add(new Probe(40, 10) { GridColumn = 1 });
        var spanning = new Probe(180, 30) { GridRow = 1, GridColumnSpan = 3 };
        grid.Children.Add(spanning);
        LayoutEngine.Layout(grid, new(300, 200));
        Near(grid.ColumnSizes[0], 53.33333f);
        Near(grid.ColumnSizes[1], 73.33333f);
        Near(grid.ColumnSizes[2], 33.33333f);
        Bounds(spanning, 10, 25, 180, 30);

        var implicitGrid = new Grid { Gap = 5 };
        var implicitChild = new Probe(80, 20) { GridRow = 2, GridColumn = 1, GridColumnSpan = 2 };
        implicitGrid.Children.Add(implicitChild);
        implicitGrid.Children.Add(new Probe(999, 999) { GridRow = 8, GridColumn = 8, IsVisible = false });
        LayoutEngine.Layout(implicitGrid, new(200, 100));
        Check(implicitGrid.RowSizes.Count == 3 && implicitGrid.ColumnSizes.Count == 3, "Implicit tracks cover visible cells and spans only");
        Bounds(implicitChild, 120, 80, 80, 20);
        implicitChild.GridRowSpan = 2;
        LayoutEngine.Layout(implicitGrid, new(200, 100));
        Check(implicitGrid.RowSizes.Count == 4, "Implicit tracks expand with span changes");

        var fixedGrid = new Grid { Gap = 5 };
        fixedGrid.Columns.Add(new(GridLength.Fixed(20)));
        fixedGrid.Columns.Add(new(GridLength.Content) { MaxSize = 40 });
        fixedGrid.Columns.Add(new(GridLength.Content) { MaxSize = 60 });
        var capped = new Probe(200, 10) { GridColumnSpan = 3 };
        fixedGrid.Children.Add(capped);
        LayoutEngine.Layout(fixedGrid, new(300, 50));
        Check(fixedGrid.ColumnSizes.SequenceEqual(new float[] { 20, 40, 60 }), "Spans respect fixed tracks and auto maxima");
        Near(capped.ActualWidth, 130, "Cell span includes internal gaps");
    }

    internal static void Constraints()
    {
        var grid = new Grid();
        grid.Columns.Add(new(GridLength.Fixed(100)) { MaxSize = 60 });
        grid.Columns.Add(new(GridLength.Fraction()) { MinSize = 40, MaxSize = 50 });
        grid.Columns.Add(new(GridLength.Fraction(3)));
        LayoutEngine.Layout(grid, new(200, 100));
        Near(grid.ColumnSizes[0], 60); Near(grid.ColumnSizes[1], 40); Near(grid.ColumnSizes[2], 100);
        LayoutEngine.Layout(grid, new(400, 100));
        Near(grid.ColumnSizes[1], 50); Near(grid.ColumnSizes[2], 290);
        LayoutEngine.Layout(grid, new(40, 100));
        Near(grid.ColumnSizes[0], 60); Near(grid.ColumnSizes[1], 40); Near(grid.ColumnSizes[2], 0);
        grid.Columns[0].MinSize = 80;
        LayoutEngine.Layout(grid, new(40, 100));
        Near(grid.ColumnSizes[0], 80, "Track minimum wins over maximum");

        var extreme = new Grid();
        extreme.Columns.Add(new(GridLength.Fraction(float.Epsilon)));
        extreme.Columns.Add(new(GridLength.Fraction(float.MaxValue)) { MaxSize = 10 });
        LayoutEngine.Layout(extreme, new(100, 50));
        Near(extreme.ColumnSizes[0], 90); Near(extreme.ColumnSizes[1], 10);

        var aligned = new Grid { HorizontalContentAlignment = Alignment.Center, VerticalContentAlignment = Alignment.End };
        aligned.Columns.Add(new(GridLength.Fixed(20)));
        aligned.Rows.Add(new(GridLength.Fixed(10)));
        var child = new Probe(8, 9) { WidthSizing = SizingMode.Fill, HeightSizing = SizingMode.Fill };
        aligned.Children.Add(child);
        LayoutEngine.Layout(aligned, new(100, 80));
        Bounds(child, 40, 70, 20, 10);
    }

    internal static void Measurement()
    {
        var measured = new Probe(10, 10);
        measured.Measure(new(100, 100)); measured.Measure(new(100, 100));
        Check(measured.MeasureCount == 1, "Stable constraints reuse intrinsic measurements");
        measured.Natural = new(20, 10);
        measured.Measure(new(100, 100));
        Check(measured.MeasureCount == 2 && measured.DesiredSize.Width == 20, "Intrinsic invalidation clears measurement cache");
        measured.Measure(new(200, 100));
        Check(measured.MeasureCount == 3, "Changed constraints remeasure");
        var parent = new Container();
        var grid = new Grid { ColumnGap = 10, WidthSizing = SizingMode.Fill, HeightSizing = SizingMode.Content };
        grid.Columns.Add(new(GridLength.Content)); grid.Columns.Add(new(GridLength.Fraction()));
        grid.Rows.Add(new(GridLength.Content));
        var label = new Probe(200, 10);
        var wrapping = new WrapProbe { GridColumn = 1 };
        grid.Children.Add(label); grid.Children.Add(wrapping);
        parent.Children.Add(grid);
        var after = new Probe(10, 10);
        parent.Children.Add(after);
        LayoutEngine.Layout(parent, new(300, 200));
        Bounds(grid, 0, 0, 300, 60);
        Near(grid.ColumnSizes[1], 90); Near(wrapping.ActualHeight, 60); Near(after.Bounds.Y, 60);
        label.Natural = new(50, 10);
        Check(!parent.IsLayoutValid, "Intrinsic cell changes invalidate ancestors");
        var input = new InputManager { Root = parent };
        _ = input.HitTest(new(1, 1));
        Bounds(grid, 0, 0, 300, 30);
        Near(after.Bounds.Y, 30);
        label.IsVisible = false;
        LayoutEngine.Layout(parent, new(300, 200));
        Near(grid.Columns[0].ActualSize, 0);
        Near(wrapping.ActualHeight, 20);

        var shrink = new Container { Layout = LayoutMode.Overlay };
        var contentGrid = new Grid { WidthSizing = SizingMode.Content, HeightSizing = SizingMode.Content, Padding = new(3) };
        contentGrid.Children.Add(new Probe(30, 20));
        shrink.Children.Add(contentGrid);
        LayoutEngine.Layout(shrink, new(200, 100));
        Bounds(contentGrid, 0, 0, 36, 26);
        var nested = new Grid { WidthSizing = SizingMode.Fill, HeightSizing = SizingMode.Fill };
        nested.Columns.Add(new(GridLength.Fraction()));
        nested.Columns.Add(new(GridLength.Fraction()));
        nested.Children.Add(new Probe(10, 10) { GridColumn = 1 });
        contentGrid.Children.Clear();
        contentGrid.Children.Add(nested);
        contentGrid.WidthSizing = SizingMode.Fill; contentGrid.HeightSizing = SizingMode.Fill;
        LayoutEngine.Layout(shrink, new(200, 100));
        Near(nested.ColumnSizes[0], 97); Near(nested.ColumnSizes[1], 97);
        Near(nested.ActualHeight, 94);
        var constrainedContent = new Grid
        {
            WidthSizing = SizingMode.Content, HeightSizing = SizingMode.Content, MaxWidth = 120
        };
        constrainedContent.Rows.Add(new(GridLength.Content));
        constrainedContent.Children.Add(new WrapProbe());
        shrink.Children.Clear();
        shrink.Children.Add(constrainedContent);
        LayoutEngine.Layout(shrink, new(300, 200));
        Bounds(constrainedContent, 0, 0, 120, 50);
    }

    internal static void OwnershipAndValidation()
    {
        var first = new Grid(); var second = new Grid();
        var track = new GridTrack(GridLength.Fixed(20));
        first.Columns.Add(track);
        Throws<InvalidOperationException>(() => first.Rows.Add(track));
        Throws<InvalidOperationException>(() => second.Columns.Add(track));
        LayoutEngine.Layout(first, new(100, 100));
        Near(track.ActualSize, 20);
        first.Columns.Remove(track);
        Near(track.ActualSize, 0);
        second.Rows.Add(track);
        LayoutEngine.Layout(second, new(100, 100));
        track.Length = GridLength.Fixed(40);
        Check(!second.IsLayoutValid, "Definition mutation invalidates its grid");
        LayoutEngine.Layout(second, new(100, 100));
        Near(track.ActualSize, 40);
        second.Rows.Clear();
        Near(track.ActualSize, 0);
        first.Columns.Add(track);
        var replacement = new GridTrack();
        first.Columns[0] = replacement;
        second.Columns.Add(track);
        Throws<ArgumentOutOfRangeException>(() => GridLength.Fraction(0));
        Throws<ArgumentOutOfRangeException>(() => GridLength.Fixed(float.NaN));
        Throws<ArgumentOutOfRangeException>(() => new GridLength((GridUnit)99));
        Throws<ArgumentOutOfRangeException>(() => replacement.MinSize = float.PositiveInfinity);
        Throws<ArgumentOutOfRangeException>(() => replacement.MaxSize = -1);
        Throws<ArgumentOutOfRangeException>(() => first.RowGap = -1);
        var cell = new Probe(1, 1);
        Throws<ArgumentOutOfRangeException>(() => cell.GridRow = -1);
        Throws<ArgumentOutOfRangeException>(() => cell.GridColumnSpan = 0);
        Throws<ArgumentOutOfRangeException>(() => cell.GridRowSpan = int.MaxValue);
    }

    internal static void RenderingAndResize()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(65, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var surface = SKSurface.Create(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes)!;
        var grid = new Grid { Background = Color.Black, ColumnGap = 5 };
        grid.Columns.Add(new(GridLength.Fixed(20))); grid.Columns.Add(new(GridLength.Fraction()));
        var red = new Probe(8, 9) { Fill = new(255, 0, 0) };
        var blue = new Probe(8, 9) { Fill = new(0, 0, 255), GridColumn = 1 };
        grid.Children.Add(red); grid.Children.Add(blue);
        LayoutEngine.Layout(grid, new(65, 40));
        WidgetRenderer.Render(surface.Canvas, grid);
        var input = new InputManager { Root = grid };
        Check(bitmap.GetPixel(10, 20).Red == 255 && input.HitTest(new(10, 20)) == red, "First cell pixels and hits");
        Check(bitmap.GetPixel(22, 20) == SKColors.Black && input.HitTest(new(22, 20)) == grid, "Grid gaps remain unoccupied");
        Check(bitmap.GetPixel(30, 20).Blue == 255 && input.HitTest(new(30, 20)) == blue, "Second cell pixels and hits");
        var front = new Probe(8, 9) { Height = 10, GridColumnSpan = 2, ZIndex = 5, Fill = new(0, 255, 0) };
        grid.Children.Add(front);
        _ = input.HitTest(new(30, 5));
        WidgetRenderer.Render(surface.Canvas, grid);
        Check(bitmap.GetPixel(30, 5).Green == 255 && input.HitTest(new(30, 5)) == front, "Spanning cells preserve z-order");
        SwealiumApplication.Run(new() { Width = 120, Height = 80 }, window =>
        {
            var live = new Grid();
            live.Columns.Add(new(GridLength.Fraction())); live.Columns.Add(new(GridLength.Fraction(2)));
            window.Content = live;
            Near(live.ColumnSizes[0], 40);
            window.Size = new(240, 100);
            Near(live.ColumnSizes[0], 80); Near(live.ColumnSizes[1], 160);
            window.RequestClose();
        });
    }

    private sealed class Probe(float width, float height) : Widget
    {
        internal int MeasureCount;
        private Size natural = new(width, height);
        internal Size Natural { get => natural; set { natural = value; InvalidateLayout(); } }
        internal Color Fill { get; init; } = Color.White;
        protected override Size MeasureContent(Size available) { MeasureCount++; return natural; }
        public override void Draw(DrawingContext drawing) =>
            drawing.FillRectangle(new(0, 0, ActualWidth, ActualHeight), Fill, Radii);
    }
    private sealed class WrapProbe : Widget
    {
        protected override Size MeasureContent(Size available)
        {
            float width = float.IsPositiveInfinity(available.Width) ? 520 : Math.Max(1, available.Width);
            return new(Math.Min(520, width), MathF.Ceiling(520 / width) * 10);
        }
        public override void Draw(DrawingContext drawing) { }
    }
}

