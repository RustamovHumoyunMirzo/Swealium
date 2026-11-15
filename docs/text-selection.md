# Selection and custom text editors

[Documentation index](README.md) · [TextBox](widgets/text-box.md) · [Text](widgets/text.md) · [Text contracts](api/text.md)

## Repeated clicks and drag selection

TextBox and selectable Text share these gestures:

| Click count | Selection |
| --- | --- |
| One | Place the caret; Shift extends the existing selection |
| Two | Select the word under the pointer |
| Three | Select the visual line, including a wrapped line, excluding its terminator |
| Four or more | Select all text |

Double-click dragging expands by complete words in either direction. Triple-click dragging expands by lines. A quadruple-click drag keeps everything selected. Each active shared drag has one pointer owner; a new primary selection replaces the old shared range.

Words contain contiguous Unicode letters, numbers, and combining marks. Spaces, tabs, punctuation (including comma, period, underscore, and apostrophe), symbols, and line boundaries separate them. Clicking punctuation/symbols selects the grapheme itself; clicking horizontal whitespace selects that whitespace run. Emoji and combining sequences are never split. This is a predictable delimiter rule, not a language-specific word-segmentation dictionary.

SDL mouse events provide ClickCount using the native click sequence. Custom hosts can pass clickCount to PointerEvent; zero requests local fallback tracking: within 500 ms, the same pointer, and at most five logical units on each axis. Keyboard activity, edits, focus loss, cancellation, or dragging outside that distance reset the local sequence. TextLayout.GetWordRange, GetLineRange, and HitTestCharacter expose the shared boundary operations for other controls. HitTestCharacter uses the character under the pointer rather than the nearest caret edge.

## Selectable read-only text

```csharp
var label = new Text
{
    Content = "<b>Select and copy</b> this text.",
    Markup = TextMarkup.Html,
    Selectable = true,
    SelectionColor = new Color(80, 130, 230, 100)
};
label.SelectAll();
// Call label.Copy() while the SDL/native host is active.
```

Selectable defaults false. Enabling it makes Text focusable, selects a text cursor, and attaches a read-only controller. Selection, SelectedText, Select(start,length), SelectAll(), Copy(), and SelectionChanged are available; keyboard navigation, Shift selection, Ctrl/Super+A and C work. Cut, paste, deletion, committed input, and IME cannot edit Content. HTML is copied as decoded plain text. UTF-16 selection indices refer to that plain text, not markup source. The original run styles, wrapping, bidi, vertical alignment, and gradients remain visible.

Plain content changes synchronize the model and clear its shared selection; changing only styles/options preserves selection. Disabling Selectable clears selection and removes its focus/input eligibility. Normal Text remains read-only; its attached controller cannot be switched to editable mode.

TextBox.PlaceholderColor defaults to RGB(140,140,145) and accepts solid colors or gradients independently of Style.Foreground. Placeholder visibility is unchanged: empty committed and preedit text only.

## Composition instead of inheritance

Attaching a Swealium.Input.TextEditor to a Widget supplies the complete editing engine. Draw-time code does not need to recreate keyboard, mouse, clipboard, grapheme editing, history, scrolling, or IME behavior:

```csharp
using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;

public sealed class MyEditor : Widget
{
    public MyEditor()
    {
        Editor = new TextEditor(this) { Multiline = true };
        Padding = new Thickness(10);
    }
    public TextEditor Editor { get; }
    protected override Size MeasureContent(Size available) => Editor.Measure(available);
    public override void Draw(DrawingContext drawing)
    {
        drawing.FillRectangle(new Rect(0, 0, ActualWidth, ActualHeight), Color.White);
        Editor.Draw(drawing);
    }
}
```

The controller attaches once, sets focus/native text-service eligibility, and is exposed through Widget.TextEditor. Widget dispatch runs its hooks, then the attached controller if the event is unhandled, then public input events. No manual event forwarding is needed. Hooks can mark events handled to replace a default behavior. TextBox itself delegates to this same engine and exposes it as Editor.

Viewport defaults to Owner.ContentBounds. Set a custom local Rect during arrangement if text occupies a different area. TextInputArea automatically returns the controller's scrolled caret rectangle through Widget's virtual property. The window presents candidate overlays for any focused widget with an enabled controller. A custom native host can call DrawCandidates with window coordinates after drawing the tree; avoid drawing it twice when using SwealiumWindow.

Draw paints text, placeholder, selection, and caret; it leaves chrome/background/strokes to the owner. CandidateBackground/CandidateBorderColor style the candidate popup. Style, Multiline, IsReadOnly, Placeholder, PlaceholderColor, SelectionColor, VerticalAlignment, text/history methods, and editing events are reusable on the controller. The controller retains managed state but owns no frame-scoped DrawingContext. It shares its owner's UI-thread lifetime and requires no disposal; a second controller on the same owner throws InvalidOperationException.

For read-only formatted displays, LayoutFactory can create a fresh TextLayout for each requested width. Ownership transfers to the controller, which disposes it. Its PlainText must exactly match controller.Text or drawing/measurement throws InvalidOperationException. Editable documents use the controller's built-in plain-text layout; rich-document editing is not supplied. Existing lower-level Widget TextInput/CompositionInput/CandidatesInput events remain available if implementing a different editing engine.

Accessibility semantic values, selection, read-only state, and editing actions also recognize attached controllers, including custom widgets. Native accessibility provider adapters remain separate.

Run `dotnet run --project samples/Swealium.Sample -- --selection-demo` for selectable HTML, colored placeholders, repeated-click selection, and a direct Widget-based editor.

## One shared selection per window

Selectable Text widgets form one read-only document in their input tree. Dragging from one widget into another highlights the first widget's tail, every intervening widget, and the last widget's prefix. Reverse dragging keeps the same document copy order. Single clicks and new programmatic selections clear earlier highlights. Shift-click and Shift-arrow can extend across widget boundaries; double/triple-click dragging retains word/line granularity. Four clicks and Ctrl/Super+A select the whole selectable-text document.

Use `window.Input.TextSelection` (SharedTextSelection) for the complete selection. Ranges is a read-only snapshot of TextSelectionPart records containing Widget and its local UTF-16 Range. SelectedText joins those selected fragments with LF separators in depth-first child insertion order. This is semantic tree order, independent of ZIndex. Text.Selection still describes that widget's local highlighted slice; Text.SelectedText and Text.Copy use the complete shared document selection when attached. TextEditor.SelectedText remains the controller's local model substring.

```csharp
var selection = window.Input.TextSelection;
string selectedText = selection.SelectedText;
selection.SelectAll();
selection.Copy(); // Active native host required.
selection.Clear();
selection.Changed += () => UpdateSelectionStatus(selection.SelectedText);
```

An input tree owns one SharedTextSelection; it cannot be constructed separately. Its Changed event reports shared updates. The range list includes zero-length boundary fragments/carets; a zero-length fragment between selected widgets still represents their LF boundary. Copy does nothing for an empty combined string. Ctrl/Super+C copies the shared text, including from a context whose key handler otherwise leaves copy unhandled.

Hidden, disabled, removed, or no-longer-selectable fragments invalidate the connected range. Model content changes and root replacement clear it; independent windows have independent scopes. Editable TextBox/custom-editor models do not join the read-only document. Starting a read-only selection clears previous editable highlights, and selecting in an editable model clears the read-only document. Drawing stays per-widget, using each widget's own SelectionColor and clipping, while ownership and copy are shared. Dragging through gaps uses the closest visible selectable text; automatic container edge scrolling is not supplied.
