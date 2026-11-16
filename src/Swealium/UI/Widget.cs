using Swealium.Graphics;
using System.Numerics;
using Swealium.Input;
using Swealium.Accessibility;
namespace Swealium.UI;

/// <summary>A retained visual. Bounds are relative to the parent; drawing and input are local.</summary>
public abstract class Widget
{
    private static long nextSemanticId;
    internal long SemanticId {get;}=Interlocked.Increment(ref nextSemanticId);
    public AccessibilityProperties Accessibility {get;}=new();
    protected virtual bool OnAccessibilityAction(AccessibilityActionRequest request)=>false;
    internal bool RaiseAccessibilityAction(AccessibilityActionRequest request)=>OnAccessibilityAction(request);
    private float x, y, width = float.NaN, height = float.NaN;
    private float minWidth, minHeight, maxWidth = float.PositiveInfinity, maxHeight = float.PositiveInfinity, grow;
    private Thickness margin, padding;
    private Stroke? localStroke;
    private Cursor? cursor;
    private float opacity=1;
    private Transform transform;
    public Transform Transform {get=>transform;set{value.Validate();transform=value;}}
    public Rect TransformedBounds=>Graphics.Transform.Map(new(0,0,ActualWidth,ActualHeight),WorldTransform);
    internal Matrix3x2 LocalTransform=>transform.Matrix(new(ActualWidth,ActualHeight),new(Bounds.X,Bounds.Y));
    internal Matrix3x2 WorldTransform
    {get{var result=Matrix3x2.Identity;for(Widget? node=this;node is not null;node=node.Parent)result*=node.LocalTransform;Graphics.Transform.Check(result);return result;}}
    public Point ToWindow(Point point)
    {var result=Vector2.Transform(new(point.X,point.Y),WorldTransform);return new(result.X,result.Y);}
    public Rect ToWindow(Rect bounds)=>Graphics.Transform.Map(bounds,WorldTransform);
    internal bool FromParent(Point point,out Point local)=>Invert(point,LocalTransform,out local);
    private static bool Invert(Point point,Matrix3x2 matrix,out Point local)
    {
        if(Matrix3x2.Invert(matrix,out var inverse))
        {var result=Vector2.Transform(new(point.X,point.Y),inverse);local=new(result.X,result.Y);return float.IsFinite(local.X)&&float.IsFinite(local.Y);}
        local=default;return false;
    }
    private Alignment? horizontalAlignment, verticalAlignment;
    private bool isVisible = true, layoutDirty = true, arranged;
    private Rect layoutBounds;
    private bool measureValid;
    private Size lastMeasureConstraint;
    private bool lastForceWidth, lastForceHeight;
    private WeightBasis weightBasis;
    private SizingMode widthSizing, heightSizing;
    private int gridRow, gridColumn, gridRowSpan = 1, gridColumnSpan = 1;
    /// <summary>Offsets used by an Absolute container.</summary>
    public float X { get => x; set { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); x = value; InvalidateLayout(); } }
    public float Y { get => y; set { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); y = value; InvalidateLayout(); } }
    /// <summary>Requested size. NaN means auto/content size.</summary>
    public float Width { get => width; set { ValidateLength(value, true); width = value; widthSizing = SizingMode.Automatic; InvalidateLayout(); } }
    public float Height { get => height; set { ValidateLength(value, true); height = value; heightSizing = SizingMode.Automatic; InvalidateLayout(); } }
    /// <summary>Content/Fill clear an explicit width. Setting Width returns this axis to Automatic.</summary>
    public SizingMode WidthSizing
    {
        get => widthSizing;
        set { ValidateSizing(value); widthSizing = value; if (value != SizingMode.Automatic) width = float.NaN; InvalidateLayout(); }
    }
    public SizingMode HeightSizing
    {
        get => heightSizing;
        set { ValidateSizing(value); heightSizing = value; if (value != SizingMode.Automatic) height = float.NaN; InvalidateLayout(); }
    }
    public int GridRow { get => gridRow; set { ValidateCell(value, false); gridRow = value; InvalidateLayout(); } }
    public int GridColumn { get => gridColumn; set { ValidateCell(value, false); gridColumn = value; InvalidateLayout(); } }
    public int GridRowSpan { get => gridRowSpan; set { ValidateCell(value, true); gridRowSpan = value; InvalidateLayout(); } }
    public int GridColumnSpan { get => gridColumnSpan; set { ValidateCell(value, true); gridColumnSpan = value; InvalidateLayout(); } }
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
    /// <summary>Stack/wrap weight. An alias for Grow; use WeightBasis.Zero for equal-share cells independent of content.</summary>
    public float Weight { get => Grow; set => Grow = value; }
    public WeightBasis WeightBasis
    {
        get => weightBasis;
        set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); weightBasis = value; InvalidateLayout(); }
    }
    public int ZIndex { get; set; }
    /// <summary>Group opacity of this visual, descendants, overlay and stroke. Does not change input/layout eligibility.</summary>
    public float Opacity {get=>opacity;set{ImageLayout.ValidateOpacity(value);opacity=value;}}
    public CornerRadii Radii { get; set; }
    /// <summary>Null inherits the nearest ancestor cursor, then the window default.</summary>
    public Cursor? Cursor {get=>cursor;set{value?.Verify();cursor=value;}}
    /// <summary>Effective stroke. Explicit assignment suppresses automatic default/focus styling until ResetStroke.</summary>
    public Stroke Stroke { get=>localStroke??(IsFocused?FocusStroke:GetDefaultStroke());set=>localStroke=value; }
    public Outline Outline {get;set;}
    public Stroke FocusStroke { get; set; }
    public bool IsStrokeSet => localStroke.HasValue;
    public void ResetStroke() => localStroke=null;
    protected virtual Stroke GetDefaultStroke() => default;
    public bool ClipToBounds { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get => isVisible; set { isVisible = value; InvalidateLayout(); } }
    /// <summary>False makes this visual subtree transparent to pointer targeting. Ancestor bubbling is unchanged.</summary>
    public bool ConsumePointer {get=>IsHitTestVisible;set=>IsHitTestVisible=value;}
    public bool IsHitTestVisible { get; set; } = true;
    public bool IsFocusable { get; set; }
    /// <summary>Opt in to Unicode text events and the platform text input service when focused.</summary>
    public bool AcceptsTextInput { get; set; }
    /// <summary>Local logical rectangle used to anchor native input-method UI.</summary>
    public TextEditor? TextEditor {get;internal set;}
    public virtual Rect TextInputArea => TextEditor is {IsEnabled:true} editor?editor.TextInputArea:ContentBounds;
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

    public void InvalidateLayout() { layoutDirty = true; measureValid = false; Parent?.InvalidateLayout(); }
    public void Measure(Size available) => MeasureAllocated(available, false, false);
    internal void MeasureAllocated(Size available, bool forceWidth, bool forceHeight)
    {
        LayoutEngine.ValidateSize(available, true);
        if (measureValid && lastMeasureConstraint == available && lastForceWidth == forceWidth && lastForceHeight == forceHeight) return;
        measureValid = false;
        if (!IsVisible) { DesiredSize = default; lastMeasureConstraint = available; lastForceWidth = forceWidth; lastForceHeight = forceHeight; measureValid = true; return; }
        float availableWidth = Constrain(forceWidth || float.IsNaN(Width) ? Math.Max(0, available.Width - Margin.Horizontal) : Width, MinWidth, MaxWidth);
        float availableHeight = Constrain(forceHeight || float.IsNaN(Height) ? Math.Max(0, available.Height - Margin.Vertical) : Height, MinHeight, MaxHeight);
        Size content = MeasureContent(new(Math.Max(0, availableWidth - Padding.Horizontal), Math.Max(0, availableHeight - Padding.Vertical)));
        LayoutEngine.ValidateSize(content, false);
        DesiredSize = new(
            Constrain((forceWidth || WidthSizing == SizingMode.Fill) && float.IsFinite(available.Width) ? availableWidth :
                float.IsNaN(Width) ? content.Width + Padding.Horizontal : Width, MinWidth, MaxWidth) + Margin.Horizontal,
            Constrain((forceHeight || HeightSizing == SizingMode.Fill) && float.IsFinite(available.Height) ? availableHeight :
                float.IsNaN(Height) ? content.Height + Padding.Vertical : Height, MinHeight, MaxHeight) + Margin.Vertical);
        lastMeasureConstraint = available;
        lastForceWidth = forceWidth; lastForceHeight = forceHeight;
        measureValid = true;
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
        float actualWidth = Constrain(forceWidth || WidthSizing == SizingMode.Fill || (WidthSizing == SizingMode.Automatic && float.IsNaN(Width) && horizontal == Alignment.Stretch) ? availableWidth : Math.Max(0, DesiredSize.Width - Margin.Horizontal), MinWidth, MaxWidth);
        float actualHeight = Constrain(forceHeight || HeightSizing == SizingMode.Fill || (HeightSizing == SizingMode.Automatic && float.IsNaN(Height) && vertical == Alignment.Stretch) ? availableHeight : Math.Max(0, DesiredSize.Height - Margin.Vertical), MinHeight, MaxHeight);
        layoutBounds = new(slot.X + Margin.Left + LayoutEngine.Offset(availableWidth - actualWidth, horizontal),
            slot.Y + Margin.Top + LayoutEngine.Offset(availableHeight - actualHeight, vertical), actualWidth, actualHeight);
        arranged = true;
        ArrangeContent(ContentBounds);
        layoutDirty = false;
    }
    private static float Constrain(float value, float min, float max) => Math.Clamp(value, min, Math.Max(min, max));
    internal float FillOuterLength(float available, bool vertical) => vertical
        ? Constrain(Math.Max(0, available - Margin.Vertical), MinHeight, MaxHeight) + Margin.Vertical
        : Constrain(Math.Max(0, available - Margin.Horizontal), MinWidth, MaxWidth) + Margin.Horizontal;
    private static void ValidateSizing(SizingMode value)
    {
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }
    private static void ValidateCell(int value, bool span)
    {
        if (value < (span ? 1 : 0) || value == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
    }
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
    public event Action<TextCompositionEvent>? CompositionInput;
    public event Action<TextCandidatesEvent>? CandidatesInput;
    public event Action? FocusChanged;

    public abstract void Draw(DrawingContext context);
    /// <summary>Draw after descendants and before the widget's decorative stroke.</summary>
    public virtual void DrawOverlay(DrawingContext context) { }
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
        if(!e.IsPrimary)return;
        switch (e.Kind)
        {
            case PointerEventKind.Down: OnMouseDown(e.Position.X, e.Position.Y); break;
            case PointerEventKind.Up: OnMouseUp(e.Position.X, e.Position.Y); break;
            case PointerEventKind.Move: OnMouseMove(e.Position.X, e.Position.Y); break;
        }
    }
    protected virtual void OnKeyboardInput(KeyEvent e) { }
    protected virtual void OnTextInput(TextInputEvent e) { }
    protected virtual void OnCompositionInput(TextCompositionEvent e) { }
    protected virtual void OnCandidatesInput(TextCandidatesEvent e) { }
    protected virtual void OnFocusChanged() { }
    internal void RaisePointer(PointerEvent e) { OnPointerInput(e); TextEditor?.ProcessPointer(e); PointerInput?.Invoke(e); }
    internal void RaiseKey(KeyEvent e) { OnKeyboardInput(e); TextEditor?.ProcessKey(e); KeyboardInput?.Invoke(e); }
    internal void RaiseText(TextInputEvent e) { OnTextInput(e); if(!e.Handled)TextEditor?.ProcessText(e); TextInput?.Invoke(e); }
    internal void RaiseComposition(TextCompositionEvent e) { OnCompositionInput(e); if(!e.Handled)TextEditor?.ProcessComposition(e); CompositionInput?.Invoke(e); }
    internal void RaiseCandidates(TextCandidatesEvent e) { OnCandidatesInput(e); if(!e.Handled)TextEditor?.ProcessCandidates(e); CandidatesInput?.Invoke(e); }
    internal void RaiseFocus() { OnFocusChanged(); TextEditor?.FocusChanged(); FocusChanged?.Invoke(); }
    internal bool IsInteractable => IsVisible && IsEnabled && Transform.Scale.Width!=0 && Transform.Scale.Height!=0 && (Parent?.IsInteractable ?? true);
    public Point ToLocal(Point point)
    {
        if(!Invert(point,WorldTransform,out var local))throw new InvalidOperationException("The widget transform is not invertible.");
        return local;
    }
}
