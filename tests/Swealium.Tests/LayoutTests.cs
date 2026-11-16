using SkiaSharp;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;

internal static class LayoutTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Near(float actual, float expected, string message) =>
        Check(Math.Abs(actual - expected) < 0.01f, $"{message}: expected {expected}, got {actual}");
    private static void Bounds(Widget widget, float x, float y, float width, float height)
    {
        Near(widget.Bounds.X, x, "X"); Near(widget.Bounds.Y, y, "Y");
        Near(widget.ActualWidth, width, "Width"); Near(widget.ActualHeight, height, "Height");
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }

    internal static void ContentControlRows()
    {
        foreach(float fontSize in new[]{14f,16f,24f})
        {
            var root=new Container{Padding=new(7.25f),Gap=3.125f,Background=Color.Black};
            root.Children.Add(new Box{Height=10.375f});
            var row=new Container{Layout=LayoutMode.Horizontal,HeightSizing=SizingMode.Content,MinHeight=44,Padding=new(2.5f,.75f),Gap=9.5f};
            var first=new Button{Text="Trigger both centers",Weight=1,FontSize=fontSize,Background=new(255,0,0),Margin=new(.375f)};
            var second=new Button{Text="Clear ripples",Weight=1,FontSize=fontSize,Background=new(255,0,0)};
            row.Children.Add(first);row.Children.Add(second);root.Children.Add(row);LayoutEngine.Layout(root,new(500,220));
            foreach(var button in row.Children)
            {
                Check(button.Bounds.Y+button.ActualHeight+button.Margin.Bottom<=row.ActualHeight-row.Padding.Bottom+.001f,"Content-sized rows include complete button height, padding and margins");
                Near(button.ActualHeight,button.DesiredSize.Height-button.Margin.Vertical,"Cross-axis content height preserves font metrics");
            }
            foreach(float scale in new[]{1f,1.25f,1.5f,2f})
            {
                using var pixels=new SKBitmap((int)(500*scale),(int)(220*scale));using var surface=SKSurface.Create(pixels.Info,pixels.GetPixels(),pixels.RowBytes)!;surface.Canvas.Scale(scale,scale);WidgetRenderer.Render(surface.Canvas,root);
                foreach(var button in row.Children)
                {
                    var bounds=button.ToWindow(new Rect(0,0,button.ActualWidth,button.ActualHeight));int x=(int)((bounds.X+bounds.Width/2)*scale),y=(int)MathF.Floor((bounds.Y+bounds.Height)*scale)-1;
                    Check(pixels.GetPixel(x,y).Red>250,"The last fully covered button row is preserved at fractional positions and DPI scales");
                }
            }
            first.FontSize=fontSize+5;LayoutEngine.Layout(root,new(500,220));Check(first.Bounds.Y+first.ActualHeight<=row.ActualHeight,"Text metric changes remeasure content-sized control rows");
        }
    }

    internal static void Spacing()
    {
        var root = new Container { Padding = new(10, 12, 14, 16), Gap = 7 };
        var first = new Box { Width = 30, Height = 20, Margin = new(1, 2, 3, 4) };
        var second = new Box { Width = 40, Height = 10, Margin = new(5) };
        root.Children.Add(first); root.Children.Add(second);
        LayoutEngine.Layout(root, new(200, 200));
        Bounds(first, 11, 14, 30, 20);
        Bounds(second, 15, 50, 40, 10);
        Check(root.DesiredSize == new Size(74, 81), "Measure includes padding, child margins, and one gap");
        first.IsVisible = false;
        Check(!root.IsLayoutValid, "Visibility invalidates parent layout");
        LayoutEngine.Layout(root, new(200, 200));
        Bounds(second, 15, 17, 40, 10);
        Check(first.ActualWidth == 0 && root.DesiredSize == new Size(74, 48), "Hidden children collapse without a gap");

        var outer = new Container { Padding = new(5) };
        var inner = new Container { Padding = new(4), Gap = 2 };
        inner.Children.Add(new Box { Width = 10, Height = 10 });
        inner.Children.Add(new Box { Width = 20, Height = 12 });
        outer.Children.Add(inner);
        LayoutEngine.Layout(outer, new(100, 100));
        Check(outer.DesiredSize == new Size(38, 42), "Nested auto measure");
        Bounds(inner, 5, 5, 90, 32);
        Bounds(inner.Children[1], 4, 16, 20, 12);
        var input = new InputManager { Root = outer };
        inner.Padding = new Thickness(8);
        Check(!outer.IsLayoutValid, "Padding invalidates ancestors");
        _ = input.HitTest(new(20, 20));
        Bounds(inner, 5, 5, 90, 40);
        inner.Children.RemoveAt(0);
        _ = input.HitTest(new(20, 20));
        Bounds(inner.Children[0], 8, 8, 20, 12);
        inner.Children.Clear();
        LayoutEngine.Layout(outer, new(100, 100));
        Near(inner.DesiredSize.Height, 16, "Empty container has only padding");

        var button = new Button { HorizontalAlignment = Alignment.Start };
        outer.Children.Add(button);
        LayoutEngine.Layout(outer, new(400, 200));
        float previous = button.ActualWidth;
        button.Text = "A substantially longer button label";
        _ = input.HitTest(new(20, 20));
        Check(button.ActualWidth > previous && float.IsNaN(button.Width), "Button measures text without mutating requested auto size");
    }

    internal static void AlignmentAndModes()
    {
        var row = new Container
        {
            Layout = LayoutMode.Horizontal, Padding = new(10), Gap = 4,
            HorizontalContentAlignment = Alignment.Center, VerticalContentAlignment = Alignment.End
        };
        var first = new Box { Width = 20, Height = 10 };
        var second = new Box { Width = 10, Height = 20, VerticalAlignment = Alignment.Center };
        row.Children.Add(first); row.Children.Add(second);
        LayoutEngine.Layout(row, new(100, 80));
        Bounds(first, 33, 60, 20, 10);
        Bounds(second, 57, 30, 10, 20);
        row.HorizontalContentAlignment = Alignment.End;
        LayoutEngine.Layout(row, new(100, 80));
        Bounds(first, 56, 60, 20, 10);

        var overlay = new Container
        {
            Layout = LayoutMode.Overlay, Padding = new(10),
            HorizontalContentAlignment = Alignment.Center, VerticalContentAlignment = Alignment.End
        };
        var box = new Box { Width = 20, Height = 10 };
        overlay.Children.Add(box);
        LayoutEngine.Layout(overlay, new(100, 80));
        Bounds(box, 40, 60, 20, 10);
        box.HorizontalAlignment = Alignment.End; box.VerticalAlignment = Alignment.Start;
        LayoutEngine.Layout(overlay, new(100, 80));
        Bounds(box, 70, 10, 20, 10);

        overlay.Layout = LayoutMode.Absolute;
        overlay.HorizontalContentAlignment = Alignment.Start;
        overlay.VerticalContentAlignment = Alignment.Start;
        box.HorizontalAlignment = null; box.VerticalAlignment = null;
        box.X = 7; box.Y = 9; box.Margin = new(2, 3, 4, 5);
        LayoutEngine.Layout(overlay, new(100, 80));
        Bounds(box, 19, 22, 20, 10);

        box.Width = float.NaN; box.Height = float.NaN; box.Margin = default;
        box.HorizontalAlignment = Alignment.Stretch; box.VerticalAlignment = Alignment.Stretch;
        LayoutEngine.Layout(overlay, new(100, 80));
        Bounds(box, 17, 19, 73, 51);
        // Resizing recalculates slots without overwriting requested positions or auto sizing.
        LayoutEngine.Layout(overlay, new(140, 100));
        Bounds(box, 17, 19, 113, 71);
        Check(box.X == 7 && float.IsNaN(box.Width), "Requested geometry survives layout");
    }

    internal static void GrowthAndConstraints()
    {
        var root = new Container { Gap = 10 };
        var first = new Box { Height = 20, Grow = 1, MaxHeight = 30, Margin = new(0, 2, 0, 3) };
        var second = new Box { Height = 20, Grow = 1 };
        root.Children.Add(first); root.Children.Add(second);
        LayoutEngine.Layout(root, new(100, 140));
        Bounds(first, 0, 2, 100, 30);
        Bounds(second, 0, 45, 100, 95);
        Near(first.Height, 20, "Grow leaves requested size intact");
        root.Layout = LayoutMode.Horizontal;
        first.Margin = default; first.Height = float.NaN; first.Width = 20;
        first.MaxWidth = 30; second.Width = 20; second.Height = float.NaN;
        root.VerticalContentAlignment = Alignment.Stretch;
        LayoutEngine.Layout(root, new(140, 100));
        Bounds(first, 0, 0, 30, 30); // MaxHeight remains 30.
        Bounds(second, 40, 0, 100, 100);

        root.Children.Clear();
        var constrained = new Box { Width = 100, Height = 4, MinWidth = 30, MaxWidth = 40, MinHeight = 12 };
        root.Children.Add(constrained);
        root.Layout = LayoutMode.Vertical;
        LayoutEngine.Layout(root, new(20, 10));
        Bounds(constrained, 0, 0, 40, 12);
        constrained.MinWidth = 60;
        LayoutEngine.Layout(root, new(20, 10));
        Near(constrained.ActualWidth, 60, "Minimum takes precedence over maximum");
        Throws<ArgumentOutOfRangeException>(() => constrained.Width = -1);
        Throws<ArgumentOutOfRangeException>(() => constrained.Width = float.PositiveInfinity);
        Throws<ArgumentOutOfRangeException>(() => root.Gap = float.NaN);
        Throws<ArgumentOutOfRangeException>(() => new Thickness(-1));
        Throws<ArgumentOutOfRangeException>(() => new CornerRadii(float.PositiveInfinity));
        Throws<ArgumentOutOfRangeException>(() => LayoutEngine.Layout(root, new(float.PositiveInfinity, 100)));
    }

    internal static void RoundedInput()
    {
        var root = new Container { Layout = LayoutMode.Overlay, HorizontalContentAlignment = Alignment.Start };
        var button = new Button { Width = 40, Height = 40, Radii = new(20, 10, 16, 4) };
        root.Children.Add(button);
        LayoutEngine.Layout(root, new(60, 60));
        var input = new InputManager { Root = root };
        foreach (Point corner in new Point[] { new(1, 1), new(39, 1), new(39, 39), new(1, 39) })
        {
            Check(input.HitTest(corner) == root, "All four cut-out corners reject hits");
            input.ProcessPointer(new(PointerEventKind.Move, corner));
            Check(!button.IsPointerOver, "Cut-out corner cannot hover");
        }
        int clicks = 0;
        button.Click += () => clicks++;
        input.ProcessPointer(new(PointerEventKind.Down, new(20, 20), PointerButton.Primary));
        input.ProcessPointer(new(PointerEventKind.Up, new(1, 1), PointerButton.Primary));
        Check(clicks == 0 && !button.IsPointerOver && !button.IsPressed, "Release in rounded cut-out does not click");
        button.Radii = new(20, 0, 0, 0);
        Check(input.HitTest(new(39, 1)) == button, "Square corners remain interactive");
        input.ProcessPointer(new(PointerEventKind.Move, new(1, 1)));
        button.Radii = default;
        input.Validate();
        Check(button.IsPointerOver, "Radii changes update hover under a stationary pointer");
        button.Margin = new(10);
        input.Validate();
        Check(!button.IsPointerOver, "Relayout updates stationary hover");
        Check(new CornerRadii(100).Normalize(new(40, 20)) == new CornerRadii(10), "Oversized radii normalize proportionally");

        var panel = new Container { Width = 40, Height = 40, Radii = new(20), Layout = LayoutMode.Overlay };
        var child = new Box { Width = 40, Height = 40 };
        panel.Children.Add(child);
        root.Children.Clear(); root.Children.Add(panel);
        LayoutEngine.Layout(root, new(60, 60));
        Check(input.HitTest(new(1, 1)) == root, "Rounded parent clips child hit region");
        panel.ClipToBounds = false;
        Check(input.HitTest(new(1, 1)) == child, "Disabling clipping permits child input outside parent shape");
    }

    internal static void PixelsAndZOrder()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(60, 60, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var surface = SKSurface.Create(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes)!;
        var root = new Container { Layout = LayoutMode.Overlay, Background = Color.Black, HorizontalContentAlignment = Alignment.Start };
        var red = new Box { Width = 40, Height = 40, Fill = new(255, 0, 0), ZIndex = 10 };
        var blue = new Box { Width = 40, Height = 40, Fill = new(0, 0, 255), ZIndex = -1 };
        root.Children.Add(red); root.Children.Add(blue);
        LayoutEngine.Layout(root, new(60, 60));
        var input = new InputManager { Root = root };
        WidgetRenderer.Render(surface.Canvas, root);
        Check(bitmap.GetPixel(20, 20).Red == 255 && input.HitTest(new(20, 20)) == red, "Draw order matches hit-test order");
        red.ZIndex = -2;
        WidgetRenderer.Render(surface.Canvas, root);
        Check(bitmap.GetPixel(20, 20).Blue == 255 && input.HitTest(new(20, 20)) == blue, "ZIndex changes immediately");
        blue.ZIndex = -2;
        Check(input.HitTest(new(20, 20)) == blue, "Equal ZIndex uses insertion order");
        root.Children.Remove(blue);
        red.Radii = new(20, 0, 16, 0);
        red.ClipToBounds = false; // Test the drawing primitive as well as clipping.
        WidgetRenderer.Render(surface.Canvas, root);
        Check(bitmap.GetPixel(1, 1) == SKColors.Black && bitmap.GetPixel(38, 38) == SKColors.Black, "Asymmetric rounded corners leave background visible");
        Check(bitmap.GetPixel(38, 1).Red == 255 && bitmap.GetPixel(1, 38).Red == 255, "Other corners remain square");
        Check(input.HitTest(new(1, 1)) == root && input.HitTest(new(38, 1)) == red, "Pixels and analytic hit shape agree");

        var parent = new Container { Width = 40, Height = 40, Radii = new(20), Layout = LayoutMode.Overlay };
        parent.Children.Add(new Box { Width = 40, Height = 40, Fill = new(0, 0, 255) });
        root.Children.Clear(); root.Children.Add(parent);
        LayoutEngine.Layout(root, new(60, 60));
        WidgetRenderer.Render(surface.Canvas, root);
        Check(bitmap.GetPixel(1, 1) == SKColors.Black && bitmap.GetPixel(20, 20).Blue == 255, "Rounded clip applies to descendants");
    }

    private sealed class Box : Widget
    {
        public Color Fill { get; init; } = Color.White;
        protected override Size MeasureContent(Size available) => new(8, 9);
        public override void Draw(DrawingContext context) =>
            context.FillRectangle(new(0, 0, ActualWidth, ActualHeight), Fill, Radii);
    }
}

