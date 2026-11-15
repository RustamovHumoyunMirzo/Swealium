using Swealium.Graphics;
using Swealium.Input;
namespace Swealium.UI;

public class Button : Widget
{
    private bool pointerPressed;
    private Key activationKey;
    private string text = "Button";
    private float fontSize = 16;
    public Button() { IsFocusable = true; Padding = new Thickness(16, 12); Radii = new CornerRadii(6); FocusStroke=new(2,Color.White); }
    public string Text { get => text; set { ArgumentNullException.ThrowIfNull(value); text = value; InvalidateLayout(); } }
    public Color Background { get; set; } = new(52, 89, 160);
    public Color HoverBackground { get; set; } = new(65, 111, 193);
    public Color PressedBackground { get; set; } = new(38, 67, 125);
    public Color Foreground { get; set; } = Color.White;
    public float FontSize
    {
        get => fontSize;
        set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); fontSize = value; InvalidateLayout(); }
    }
    public bool IsPressed => IsInteractable && ((pointerPressed && IsPointerOver) || activationKey != Key.Unknown);

    public override void Draw(DrawingContext context)
    {
        Color background = !IsInteractable ? new(65, 65, 70) : IsPressed ? PressedBackground : IsPointerOver ? HoverBackground : Background;
        context.FillRectangle(new(0, 0, ActualWidth, ActualHeight), background, Radii);
        context.DrawText(Text, ContentBounds, new TextStyle { Foreground = Foreground, FontSize = FontSize },
            new TextOptions { Wrapping = TextWrapping.None, Alignment = TextAlignment.Center, VerticalAlignment = TextVerticalAlignment.Center });
    }
    protected override Size MeasureContent(Size available)
    {
        Size text = TextMetrics.Measure(Text, FontSize);
        return new(text.Width, Math.Max(20, text.Height));
    }
    protected override void OnPointerInput(PointerEvent e)
    {
        base.OnPointerInput(e);
        if (e.Kind == PointerEventKind.Down && e.Button == PointerButton.Primary) pointerPressed = true;
        if (e.Kind == PointerEventKind.Cancel || (e.Kind == PointerEventKind.Up && e.Button == PointerButton.Primary)) pointerPressed = false;
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

