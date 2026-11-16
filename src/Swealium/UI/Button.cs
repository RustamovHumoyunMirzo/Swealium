using Swealium.Graphics;
using Swealium.Input;
namespace Swealium.UI;

public class Button : Widget
{
    private readonly HashSet<PointerId> pressedPointers=[];
    private readonly HashSet<PointerId> insidePointers=[];
    private Key activationKey;
    private string text = "Button";
    private TextStyle style=new(){Foreground=Color.White};
    public TextStyle Style {get=>style;set{ArgumentNullException.ThrowIfNull(value);value.Validate();style=value;InvalidateLayout();}}
    public Button() { IsFocusable = true; Padding = new Thickness(16, 12); Radii = new CornerRadii(6); FocusStroke=new(2,Color.White); Cursor=Swealium.Input.Cursor.Pointer; }
    public string Text { get => text; set { ArgumentNullException.ThrowIfNull(value); text = value; InvalidateLayout(); } }
    public Color Background { get; set; } = new(52, 89, 160);
    public Color HoverBackground { get; set; } = new(65, 111, 193);
    public Color PressedBackground { get; set; } = new(38, 67, 125);
    public Color Foreground { get=>Style.Foreground;set=>Style=Style with{Foreground=value}; }
    public float FontSize
    {
        get => Style.FontSize;
        set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));Style=Style with{FontSize=value}; }
    }
    public bool IsPressed => IsInteractable && (pressedPointers.Any(insidePointers.Contains) || activationKey != Key.Unknown);

    public override void Draw(DrawingContext context)
    {
        Color background = !IsInteractable ? new(65, 65, 70) : IsPressed ? PressedBackground : IsPointerOver ? HoverBackground : Background;
        context.FillRectangle(new(0, 0, ActualWidth, ActualHeight), background, Radii);
        context.DrawText(Text, ContentBounds, Style,
            new TextOptions { Wrapping = TextWrapping.None, Alignment = TextAlignment.Center, VerticalAlignment = TextVerticalAlignment.Center });
    }
    protected override Size MeasureContent(Size available)
    {
        Size text = TextMetrics.Measure(Text, Style);
        return new(text.Width, Math.Max(20, text.Height));
    }
    protected override void OnPointerInput(PointerEvent e)
    {
        base.OnPointerInput(e);
        if(e.Kind==PointerEventKind.Enter)insidePointers.Add(e.PointerId);
        if(e.Kind==PointerEventKind.Leave)insidePointers.Remove(e.PointerId);
        if(e.Kind==PointerEventKind.Down&&e.Button==PointerButton.Primary)
        {pressedPointers.Add(e.PointerId);if(ContainsPoint(e.Position))insidePointers.Add(e.PointerId);}
        if(e.Kind==PointerEventKind.Move&&pressedPointers.Contains(e.PointerId))
        {if(ContainsPoint(e.Position))insidePointers.Add(e.PointerId);else insidePointers.Remove(e.PointerId);}
        if(e.Kind==PointerEventKind.Cancel||(e.Kind==PointerEventKind.Up&&e.Button==PointerButton.Primary))
        {pressedPointers.Remove(e.PointerId);if(e.DeviceKind==PointerDeviceKind.Touch)insidePointers.Remove(e.PointerId);}
    }
    protected override void OnKeyboardInput(KeyEvent e)
    {
        if (e.Key is not (Key.Space or Key.Enter)) return;
        if (e.IsDown && !e.IsRepeat) activationKey = e.Key;
        if (!e.IsDown && activationKey == e.Key)
        {
            activationKey = Key.Unknown;
            PerformClick();
        }
        e.Handled = true;
    }
    protected override void OnFocusChanged()
    {
        if (!IsFocused) activationKey = Key.Unknown;
    }
}

