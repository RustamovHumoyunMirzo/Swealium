# Implementing widgets and custom layouts

[Documentation index](README.md) · [Widget API](api/widgets.md)

## A measured visual

This complete widget has an intrinsic desired size and a rounded drawing surface:

```csharp
using Swealium.Graphics;
using Swealium.UI;

public sealed class StatusBadge : Widget
{
    private Color fill = new(35, 110, 80);
    public StatusBadge()
    {
        Padding = new Thickness(12, 8);
        Radii = new CornerRadii(10);
    }
    public Color Fill { get => fill; set => fill = value; }

    protected override Size MeasureContent(Size available) => new(120, 24);

    public override void Draw(DrawingContext drawing)
    {
        drawing.FillRectangle(new Rect(0, 0, ActualWidth, ActualHeight), Fill, Radii);
        drawing.DrawText("Ready", new Point(ContentBounds.X, ContentBounds.Y + 18),
            Color.White, 16);
    }
}
```

MeasureContent receives content space after the widget's own margin, requested constraints, and padding have been considered. Return a finite nonnegative content size, excluding padding and margin. Base Measure adds those and applies min/max constraints. A stack may supply positive infinity on its main axis; do not return infinity just because it is available.

When a custom property changes intrinsic size, call InvalidateLayout from its setter. Pure drawing properties can be changed without layout invalidation because visible frames render continuously. Use ActualWidth/ActualHeight for drawing; Width/Height may still be NaN or differ after Grow.

## Add interaction

Set IsFocusable for keyboard focus. Override OnPointerInput/OnKeyboardInput/OnTextInput for typed events; the legacy OnMouseDown/Up/Move hooks also remain. Virtual hooks run before public input events.

Use OnClick for activation behavior rather than manually emulating capture. PerformClick checks effective visible/enabled ancestry but is programmatic activation, not a complete synthetic pointer gesture. For buttons, derive from Button to reuse press/release and keyboard activation. Call base overrides when preserving built-in state or legacy hooks is intended.

OnFocusChanged on Widget is about focus within its window. OnFocusChanged on SwealiumWindow is about OS focus. They have different sources and lifetimes.

## Custom containers

Derive from Container to retain the built-in child ownership, drawing traversal, z-order, and input traversal. A plain Widget with an unrelated custom child list is not traversed by the renderer/input manager.

Override MeasureContent to measure each child and compute a finite desired content size. Child DesiredSize includes its margin. Override ArrangeContent to allocate outer slots; child.Arrange consumes its own margin and alignment. ContentBounds is already padding-inset. Arrange must follow Measure for auto sizing to have meaningful desired sizes.

```csharp
public sealed class TwoColumnPanel : Container
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
            Children[i].Arrange(new Rect(content.X + (i % 2) * half,
                content.Y, half, content.Height));
    }
}
```

This example intentionally shares two column slots among any extra children; it is a custom-layout example, not the built-in Grid. Use [Grid](grid.md) for definitions, spans, constraints, and implicit tracks. A custom layout should define overflow, hidden children, gaps, and how it treats more than two children.

Keep child list changes out of MeasureContent and ArrangeContent themselves. Ownership setters and collection operations already invalidate ancestors. Layout does not automatically detect arbitrary custom state changes.

## Shape customization

Override ContainsPoint(local) for a custom analytic hit shape. Respect ActualWidth/ActualHeight and return false outside the intended visual region. Drawing that shape remains your responsibility. Setting Radii customizes the built-in rounded clipping and hit region for all widgets without a custom override.

## Errors and resource ownership

Do not dispose or store DrawingContext. Dispose application-owned external resources when appropriate, and unsubscribe long-lived event sources if they retain removed widgets. Removing a widget detaches it from its parent but does not dispose it; Widget has no IDisposable lifecycle. A window's Content root cannot be shared with another input tree or parent at the same time.


## Custom text editors

Attach a Swealium.Input.TextEditor to your Widget and call Editor.Draw during drawing. Widget dispatch automatically supplies selection, editing, clipboard/history, IME preedit, candidate overlays, and caret positioning without TextBox inheritance. See the [complete composition example](text-selection.md#composition-instead-of-inheritance). Raw text/composition/candidate events remain available for a fully independent engine.
