# Text

[Widget hub](README.md) · [Inherited Widget API](widget.md) · [Text guide](../text.md) · [Full text contracts](../api/text.md)

`public class Text : Widget` is a retained holder for rectangle `DrawingContext.DrawText`. It measures and draws with the same shaped paragraph engine, supporting bidi, font fallback, wrapping, and optional HTML text markup. It is not focusable/editable by default.

| Public property | Default / behavior |
| --- | --- |
| Content | Empty, nonnull string |
| Style | New TextStyle: FontSize=16, Foreground=Black, FontWeight=400 |
| Options | New TextOptions: Word wrapping, Start alignment, Top vertical alignment, automatic direction |
| Markup | Plain; Html explicitly enables formatted text parsing |

All setters invalidate measurement. Null Content/Style/Options throw ArgumentNullException. Style/options are immutable records validated on assignment; invalid enums/lengths throw. HTML has a [documented formatting subset](../text.md#html-as-text-markup); it does not load resources or run scripts.

```csharp
var message = new Text
{
    Content = "<b>Hello</b> · مرحبا · שלום · 😀",
    Markup = TextMarkup.Html,
    Style = new TextStyle { FontSize = 22, Foreground = new(40, 60, 90) },
    Options = new TextOptions { MaxLines = 3, Ellipsis = true }
};
```

MeasureContent constrains shaping by available content width; Draw forwards Content/Style/Options/Markup into ContentBounds. Text in a stack/grid can therefore change height as its width changes. Horizontal scrolling measures unbounded width and will suppress width-based wrapping; use a vertical-only ScrollContainer to retain wrapping. System fonts determine glyph coverage. Margin/padding, named sizing, strokes, radii, clipping, and inherited events remain available. Text does not supply selection, clickable hyperlinks, or editor behavior; use [TextBox](text-box.md) for plain-text editing.
