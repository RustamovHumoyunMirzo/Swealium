using Swealium.Graphics;
using Swealium.Input;
namespace Swealium.UI;

/// <summary>A retained visual. Bounds are relative to the parent; drawing and input are local.</summary>
public abstract class Widget
{
    private float x, y, width = float.NaN, height = float.NaN;
    private float minWidth, minHeight, maxWidth = float.PositiveInfinity, maxHeight = float.PositiveInfinity, grow;
    private Thickness margin, padding;
    private Alignment? horizontalAlignment, verticalAlignment;
    private bool isVisible = true, layoutDirty = true, arranged;
    private Rect layoutBounds;
    /// <summary>Offsets used by an Absolute container.</summary>
    public float X { get => x; set { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); x = value; InvalidateLayout(); } }
    public float Y { get => y; set { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); y = value; InvalidateLayout(); } }
    /// <summary>Requested size. NaN means auto/content size.</summary>
    public float Width { get => width; set { ValidateLength(value, true); width = value; InvalidateLayout(); } }
    public float Height { get => height; set { ValidateLength(value, true); height = value; InvalidateLayout(); } }
    public float MinWidth { get => minWidth; set { ValidateLength(value); minWidth = value; InvalidateLayout(); } }
    public float MinHeight { get => minHeight; set { ValidateLength(value); minHeight = value; InvalidateLayout(); } }
    public float MaxWidth { get => maxWidth; set { ValidateLength(value, infinity: true); maxWidth = value; InvalidateLayout(); } }
    public float MaxHeight { get => maxHeight; set { ValidateLength(value, infinity: true); maxHeight = value; InvalidateLayout(); } }
    public Thickness Margin { get => margin; set { margin = value; InvalidateLayout(); } }
    public Thickness Padding { get => padding; set { padding = value; InvalidateLayout(); } }
    /// <summary>Null inherits the parent container's content alignment.</summary>
    public Alignment? HorizontalAlignment { get => horizontalAlignment; set { horizontalAlignment = value; InvalidateLayout(); } }
    public Alignment? VerticalAlignment { get => verticalAlignment; set { verticalAlignment = value; InvalidateLayout(); } }
    /// <summary>Relative share of spare space along the parent's stack axis.</summary>
    public float Grow { get => grow; set { ValidateLength(value); grow = value; InvalidateLayout(); } }
    public int ZIndex { get; set; }
    public CornerRadii Radii { get; set; }
    public bool ClipToBounds { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get => isVisible; set { isVisible = value; InvalidateLayout(); } }
    public bool IsHitTestVisible { get; set; } = true;
    public bool IsFocusable { get; set; }
    /// <summary>Opt in to Unicode text events and the platform text input service when focused.</summary>
    public bool AcceptsTextInput { get; set; }
    public bool IsFocused { get; internal set; }
    public bool IsPointerOver { get; internal set; }
    public Container? Parent { get; internal set; }
    internal InputManager? RootOwner { get; set; }
    public Rect Bounds => arranged ? layoutBounds : new(X, Y, float.IsNaN(Width) ? 0 : Width, float.IsNaN(Height) ? 0 : Height);
    public float ActualWidth => Bounds.Width;
    public float ActualHeight => Bounds.Height;
    /// <summary>Measured outer size, including margins.</summary>
    public Size DesiredSize { get; private set; }
    public bool IsLayoutValid => arranged && !layoutDirty;
    internal Size? LayoutViewport { get; set; }
    public Rect ContentBounds => new(Padding.Left, Padding.Top,
        Math.Max(0, ActualWidth - Padding.Horizontal), Math.Max(0, ActualHeight - Padding.Vertical));

    public void InvalidateLayout() { layoutDirty = true; Parent?.InvalidateLayout(); }
    public void Measure(Size available)
    {
        LayoutEngine.ValidateSize(available, true);
        if (!IsVisible) { DesiredSize = default; return; }
        float availableWidth = Math.Min(Math.Max(MinWidth, MaxWidth), float.IsNaN(Width) ? Math.Max(0, available.Width - Margin.Horizontal) : Width);
        float availableHeight = Math.Min(Math.Max(MinHeight, MaxHeight), float.IsNaN(Height) ? Math.Max(0, available.Height - Margin.Vertical) : Height);
        Size content = MeasureContent(new(Math.Max(0, availableWidth - Padding.Horizontal), Math.Max(0, availableHeight - Padding.Vertical)));
        LayoutEngine.ValidateSize(content, false);
        DesiredSize = new(
            Constrain(float.IsNaN(Width) ? content.Width + Padding.Horizontal : Width, MinWidth, MaxWidth) + Margin.Horizontal,
            Constrain(float.IsNaN(Height) ? content.Height + Padding.Vertical : Height, MinHeight, MaxHeight) + Margin.Vertical);
    }
    protected virtual Size MeasureContent(Size available) => default;
    protected virtual void ArrangeContent(Rect contentBounds) { }
    /// <summary>Arrange within an outer slot, including margin. Call Measure first.</summary>
    public void Arrange(Rect slot) => ArrangeSlot(slot, Alignment.Stretch, Alignment.Stretch);
    internal void ArrangeSlot(Rect slot, Alignment inheritedHorizontal, Alignment inheritedVertical, bool forceWidth = false, bool forceHeight = false)
    {
        LayoutEngine.ValidateSize(new(slot.Width, slot.Height), false);
        if (!float.IsFinite(slot.X) || !float.IsFinite(slot.Y)) throw new ArgumentOutOfRangeException(nameof(slot));
        if (!IsVisible) { layoutBounds = new(slot.X, slot.Y, 0, 0); arranged = true; layoutDirty = false; return; }
        float availableWidth = Math.Max(0, slot.Width - Margin.Horizontal), availableHeight = Math.Max(0, slot.Height - Margin.Vertical);
        Alignment horizontal = HorizontalAlignment ?? inheritedHorizontal, vertical = VerticalAlignment ?? inheritedVertical;
        float actualWidth = Constrain(forceWidth || (float.IsNaN(Width) && horizontal == Alignment.Stretch) ? availableWidth : Math.Max(0, DesiredSize.Width - Margin.Horizontal), MinWidth, MaxWidth);
        float actualHeight = Constrain(forceHeight || (float.IsNaN(Height) && vertical == Alignment.Stretch) ? availableHeight : Math.Max(0, DesiredSize.Height - Margin.Vertical), MinHeight, MaxHeight);
        layoutBounds = new(slot.X + Margin.Left + LayoutEngine.Offset(availableWidth - actualWidth, horizontal),
            slot.Y + Margin.Top + LayoutEngine.Offset(availableHeight - actualHeight, vertical), actualWidth, actualHeight);
        arranged = true;
        ArrangeContent(ContentBounds);
        layoutDirty = false;
    }
    private static float Constrain(float value, float min, float max) => Math.Clamp(value, min, Math.Max(min, max));
    private static void ValidateLength(float value, bool auto = false, bool infinity = false)
    {
        if (auto && float.IsNaN(value)) return;
        if (value < 0 || float.IsNaN(value) || (!infinity && !float.IsFinite(value))) throw new ArgumentOutOfRangeException(nameof(value));
    }
    public virtual bool ContainsPoint(Point local) => Radii.Contains(local, new(ActualWidth, ActualHeight));
    public event Action? Click;
    public event Action<PointerEvent>? PointerInput;
    public event Action<KeyEvent>? KeyboardInput;
    public event Action<TextInputEvent>? TextInput;
    public event Action? FocusChanged;

    public abstract void Draw(DrawingContext context);
    public void PerformClick()
    {
        if (!IsInteractable) return;
        OnClick();
        Click?.Invoke();
    }
    protected virtual void OnClick() { }
    public virtual void OnMouseDown(float x, float y) { }
    public virtual void OnMouseUp(float x, float y) { }
    public virtual void OnMouseMove(float x, float y) { }
    protected virtual void OnPointerInput(PointerEvent e)
    {
        switch (e.Kind)
        {
            case PointerEventKind.Down: OnMouseDown(e.Position.X, e.Position.Y); break;
            case PointerEventKind.Up: OnMouseUp(e.Position.X, e.Position.Y); break;
            case PointerEventKind.Move: OnMouseMove(e.Position.X, e.Position.Y); break;
        }
    }
    protected virtual void OnKeyboardInput(KeyEvent e) { }
    protected virtual void OnTextInput(TextInputEvent e) { }
    protected virtual void OnFocusChanged() { }
    internal void RaisePointer(PointerEvent e) { OnPointerInput(e); PointerInput?.Invoke(e); }
    internal void RaiseKey(KeyEvent e) { OnKeyboardInput(e); KeyboardInput?.Invoke(e); }
    internal void RaiseText(TextInputEvent e) { OnTextInput(e); TextInput?.Invoke(e); }
    internal void RaiseFocus() { OnFocusChanged(); FocusChanged?.Invoke(); }
    internal bool IsInteractable => IsVisible && IsEnabled && (Parent?.IsInteractable ?? true);
    internal Point ToLocal(Point point)
    {
        for (Widget? node = this; node is not null; node = node.Parent)
            point = new(point.X - node.Bounds.X, point.Y - node.Bounds.Y);
        return point;
    }
}

