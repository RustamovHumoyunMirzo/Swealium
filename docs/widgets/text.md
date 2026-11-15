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

MeasureContent constrains shaping by available content width; Draw forwards Content/Style/Options/Markup into ContentBounds. Text in a stack/grid can therefore change height as its width changes. Horizontal scrolling measures unbounded width and will suppress width-based wrapping; use a vertical-only ScrollContainer to retain wrapping. System fonts determine glyph coverage. Margin/padding, named sizing, strokes, radii, clipping, and inherited events remain available. Set Selectable=true for read-only selection/copy, including HTML plain-text selection. Clickable hyperlinks and rich-document editing are not supplied; use [TextBox](text-box.md) for plain-text editing.

## Read-only selection

Selectable defaults false. Enabling it attaches an immutable read-only mode of TextEditor, makes the widget focusable, and supplies the text cursor. SelectionColor defaults RGBA(70,110,210,100). Selection and SelectedText are read-only; Select(start,length), SelectAll(), Copy(), and SelectionChanged expose selection/copy. Select rejects disabled selection; SelectAll/Copy do nothing when disabled. Clipboard copy requires a running native host. Selection indices refer to decoded PlainText for HTML. Input cannot change Content. Disabling Selectable clears selection and focus eligibility. See [selection gestures and custom editors](../text-selection.md).

Selectable Text widgets in a window share one connected selection through InputManager.TextSelection. Drag across widgets to select intermediate fragments; starting another selection clears the previous highlights. Selection is local for drawing; SelectedText/Copy represent the complete shared document, with LF between fragments. SelectAll selects the selectable-text document when attached. See [shared selection](../text-selection.md#one-shared-selection-per-window).
