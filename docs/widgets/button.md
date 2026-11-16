# Button

[Widget hub](README.md) · [Inherited Widget API](widget.md) · [Full contracts](../api/widgets.md#button)

`public class Button : Widget` measures a shaped unwrapped caption, draws state-dependent rounded fills, and supports pointer/keyboard activation. Constructor defaults: IsFocusable=true, Padding=(16 horizontal,12 vertical), Radii=6.

| Public property | Default / contract |
| --- | --- |
| Text | "Button"; null rejected; setter invalidates measure |
| Style | TextStyle with FontSize=16 and white Foreground; supports loaded/system fonts, weight, width, slant and spacing; setter validates and invalidates measure |
| FontSize | 16; finite positive; setter invalidates measure |
| Background | RGB(52,89,160) |
| HoverBackground | RGB(65,111,193) |
| PressedBackground | RGB(38,67,125) |
| Foreground | White |
| IsPressed | Read-only; effective input eligibility plus hovered pointer press or active keyboard press |

```csharp
var save = new Button { Text = "Save", Radii = new(16, 4, 16, 4) };
save.Stroke = new Stroke(2, new Color(130, 170, 235));
save.Click += SaveDocument;
root.Children.Add(save);
```

Primary down captures through InputManager. Moving outside the rounded hit shape removes pressed/hover appearance; releasing outside does not click. Release inside triggers Click unless the pointer event was handled. Space/Enter down arms keyboard activation, and matching key-up clicks. Repeat down does not start another activation. Focus loss clears keyboard activation; capture cancellation clears pointer state. Disabled buttons use a gray background and do not activate.

Text uses the shared shaping engine with no width wrapping, centered horizontally/vertically within ContentBounds, clipped there. Explicit newlines can still create lines. Intrinsic height is at least 20 plus padding. An untouched button uses public FocusStroke (2-unit white Solid) while focused, through the ordinary Stroke renderer. Setting Stroke suppresses that automatic appearance, including when set to default/Hidden. ResetStroke restores it. There is no separate internal focus outline. Override Draw, MeasureContent, or input/focus hooks for custom behavior; call base input hooks if preserving built-in activation.

FontSize and Foreground are aliases into Style; assigning either preserves the other style fields. See [font loading and typography boundaries](../fonts.md) for file fonts and advanced DrawingContext shaping.
