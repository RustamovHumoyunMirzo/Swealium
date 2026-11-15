# TextBox

[Widget hub](README.md) · [Inherited Widget API](widget.md) · [Text/IME guide](../text.md) · [Full text contracts](../api/text.md#textbox-widget)

`public class TextBox : Widget` edits plain text with shaped display, grapheme-safe deletion, selection, clipboard, undo/redo, scrolling, and IME preedit/candidates. It does not interpret HTML. Constructor sets IsFocusable/AcceptsTextInput=true, Padding=8, Radii=4, MinWidth=80.

| Public property | Default / contract |
| --- | --- |
| Text | Empty committed string; null rejected; normalize CRLF/CR to LF, remove NUL, replace newlines with spaces in single-line mode |
| Style | Default immutable TextStyle; validated; invalidates layout |
| Multiline | false; enables newlines and width wrapping; false normalizes existing line breaks |
| IsReadOnly | false; blocks editing/history/cut/paste, leaves selection/copy available; updates AcceptsTextInput and cancels composition |
| Placeholder | Empty nonnull string; shown gray while committed and preedit text are empty |
| Background / BorderColor | White / RGB(130,130,140); BorderColor supplies the resting default Stroke |
| FocusStroke | 1-unit blue Solid; ordinary Stroke rendering, used only when Stroke has not been explicitly set |
| SelectionColor | RGBA(70,110,210,100) |
| Selection / CaretIndex / SelectedText | Read-only UTF-16 range, active end, and selected substring |
| CompositionText | Read-only uncommitted preedit; initially empty |
| Candidates / SelectedCandidate / CandidatesHorizontal | Read-only snapshot, -1 for no selection, false initial orientation |
| CanUndo / CanRedo | Read-only history availability |
| TextInputArea | Override returning local scrolled caret rectangle for IME placement |

```csharp
var editor = new TextBox { Multiline = true, Height = 180, Text = "A draft\nمرحبا" };
editor.TextChanged += () => SaveDraft(editor.Text);
root.Children.Add(editor);
window.Content = root;
window.Input.Focus(editor);
```

## Editing methods and events

Select(start,length) validates a UTF-16 range, snaps endpoints to preceding grapheme boundaries, and cancels composition. SelectAll selects the document. ReplaceSelection(string) normalizes and inserts text as one undoable operation, unless read-only. Text assignment clears history when changed; Undo/Redo restore text and selection using up to 100 edit snapshots. Copy/Cut/Paste use the native Unicode clipboard on the running UI thread. CancelComposition clears transient state and requests native composition dismissal without committing.

TextChanged fires after committed text mutation, not preedit. SelectionChanged fires when caret/selection resets and can repeat unchanged endpoints. Submitted fires on Enter in single-line mode. Inherited pointer/key/text/composition/candidate/focus hooks remain overridable and corresponding public events still run afterward.

## Input behavior

- Primary pointer down places the caret; drag extends selection under InputManager capture.
- Left/Right move visually within shaped lines. Shift extends from the preserved anchor.
- Home/End use the logical text start/end of the current shaped visual line, excluding its newline. This follows text order for RTL too. Shift+Home/End extends from the original caret anchor, preserving wrapped-line affinity for subsequent movement. Ctrl/Cmd+Home/End targets the document; adding Shift selects to that boundary. Held left/right Shift and NumLock-off keypad navigation are supported through native decoding.
- Up/Down, PageUp/PageDown, Ctrl/Cmd+Left/Right word movement, and select-all are supported.
- Ctrl/Cmd+C/X/V/Z/Y provide clipboard/history; Shift+Ctrl/Cmd+Z redoes. AltGr combinations are not treated as command shortcuts.
- Backspace/Delete remove whole extended graphemes, including surrogate pairs, combining sequences, and joined emoji.
- Enter inserts LF in multiline mode; Tab remains focus traversal.

The control scrolls internally to reveal the caret and clips to ContentBounds. Shift+wheel scrolls horizontally; already-horizontal deltas stay horizontal. Single-line editors also map a vertical wheel into horizontal movement. Wheel movement bubbles to an enclosing ScrollContainer when the applicable internal range is exhausted. ScrollContainer focus traversal reveals offscreen editor bounds. Its internal scrolling currently has no public scrollbar style/offset API; ScrollContainer's customizable bars control the outer content viewport.

Default and focus borders are ordinary Swealium Stroke values. Assigning Stroke explicitly suppresses both state defaults, even if the assigned stroke is empty/transparent/Hidden. Customize FocusStroke for the default focused appearance, or call ResetStroke to restore defaults after an explicit assignment. No hidden Draw-time border remains.

Preedit is underlined with selected-segment background. A native commit replaces the selection captured when composition started as one undoable edit. Focus loss, empty updates, Escape/cancellation clear transient state. Candidate rendering overlays the window tree; the OS IME owns navigation/conversion/commit. Provider behavior still requires manual testing. See the [IME guide](../text.md#ime-preedit-and-candidates) for platform limits.

The editor is not virtualized and has no rich-document editing, password masking or syntax-highlighting API. Use finite Height for a bounded editor viewport; otherwise content participates in intrinsic sizing (minimum one line, or three lines in multiline mode).

Selection dragging belongs to one pointer until release/cancellation. Semantic value and selection actions are available through the [accessibility boundary](../accessibility.md); native screen-reader adapters are not bundled.
