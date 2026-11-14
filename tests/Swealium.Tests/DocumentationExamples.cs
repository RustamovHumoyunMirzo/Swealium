using Swealium;
using Swealium.Graphics;
using Swealium.UI;

// Compile representative public documentation examples with every solution build.
internal static class DocumentationExamples
{
    internal static void FirstApplication() => SwealiumApplication.Run(new WindowOptions
    {
        Title = "My application", Width = 800, Height = 500
    }, window =>
    {
        var root = new Container
        {
            Padding = new Thickness(24), Gap = 12,
            VerticalContentAlignment = Alignment.Center, HorizontalContentAlignment = Alignment.Center
        };
        var greeting = new Button { Text = "Click or use Tab and Enter", Radii = new CornerRadii(20, 4, 20, 4) };
        int count = 0;
        greeting.Click += () => greeting.Text = $"Clicked {++count} times";
        root.Children.Add(greeting);
        window.Content = root;
    });

    internal static void NativeInterop(SwealiumWindow window)
    {
        Console.WriteLine($"Portable routing ID: {window.Id}");
        if (window.TryGetNativeWindowHandle(out NativeWindowHandle handle))
        {
            Console.WriteLine($"System: {handle.Kind}; value: 0x{handle.Value:X}");
            Console.WriteLine($"Display connection: 0x{handle.Display:X}");
        }
    }

    internal sealed class StatusBadge : Widget
    {
        private Color fill = new(35, 110, 80);
        public StatusBadge() { Padding = new Thickness(12, 8); Radii = new CornerRadii(10); }
        public Color Fill { get => fill; set => fill = value; }
        protected override Size MeasureContent(Size available) => new(120, 24);
        public override void Draw(DrawingContext drawing)
        {
            drawing.FillRectangle(new Rect(0, 0, ActualWidth, ActualHeight), Fill, Radii);
            drawing.DrawText("Ready", new Point(ContentBounds.X, ContentBounds.Y + 18), Color.White, 16);
        }
    }

    internal sealed class TwoColumnPanel : Container
    {
        protected override Size MeasureContent(Size available)
        {
            float width = float.IsFinite(available.Width) ? available.Width / 2 : 200;
            float height = 0;
            foreach (var child in Children)
            {
                child.Measure(new Size(width, available.Height));
                if (child.IsVisible) height = Math.Max(height, child.DesiredSize.Height);
            }
            return new Size(width * 2, height);
        }
        protected override void ArrangeContent(Rect content)
        {
            float half = content.Width / 2;
            for (int i = 0; i < Children.Count; i++)
                Children[i].Arrange(new Rect(content.X + (i % 2) * half, content.Y, half, content.Height));
        }
    }
}
