# Text API contracts

[Documentation index](../README.md) · [Text guide and examples](../text.md) · [Complete signatures](reference.md)

Use these APIs on the UI thread. No backend text/font/HTML objects appear in public signatures.

## TextStyle and TextOptions

Both are immutable sealed records; use `with` to produce changes. They are validated when a layout is constructed or a widget property is assigned.

| TextStyle member | Default / contract |
| --- | --- |
| FontFamily | null: system default; unavailable families use backend/system fallback |
| FontSize | 16 logical units, finite and positive |
| FontWeight | 400; integer 1–1000 |
| Italic / Underline / StrikeThrough | false |
| Foreground / Background | Black / Transparent |
| LineHeight | 1; finite positive multiplier |
| LetterSpacing | 0; finite logical units, may be negative |
| Direction | Automatic; directional override for this run |
| Script | Normal; also Subscript and Superscript |

| TextOptions member | Default / contract |
| --- | --- |
| Wrapping | Word; None disables width wrapping, explicit newlines remain |
| Alignment | Start; Center/End/Left/Right are also available |
| VerticalAlignment | Top; Center/Bottom used by rectangle DrawText |
| Direction | Automatic; also LeftToRight/RightToLeft paragraph base direction |
| MaxLines | null (unlimited); otherwise positive integer |
| Ellipsis | false; when true, truncated lines can end with an ellipsis |

Start/End follow paragraph direction; Left/Right are physical. Enum values outside their declared range and invalid style sizes/weights throw ArgumentOutOfRangeException when validated.

## TextLayout, TextRun, TextPosition, TextSelection

Construct `TextLayout(string, TextStyle? = null, TextOptions? = null, float maxWidth = PositiveInfinity, TextMarkup = Plain)` or `TextLayout(IEnumerable<TextRun>, TextOptions? = null, float maxWidth = PositiveInfinity)`. Null arguments/runs/styles are rejected. Width must be nonnegative and not NaN; positive infinity means unbounded. A zero width uses a minimal layout boundary and drawing can clip it to zero.

Each TextRun contains literal `Text` and its `Style`. The enumerable is snapshotted. `Runs` is read-only; `PlainText` concatenates runs, or contains decoded HTML text when HTML parsing was requested. `Size` reports measured logical extent, `Baseline` the first-line baseline relative to the top, `LineCount` the laid-out number of lines, and `IsTruncated` indicates overflow beyond limits. Empty text and a trailing newline retain useful caret/font metrics. A private zero-width sentinel is excluded from public indices/text.

`HitTest(Point)` returns the nearest UTF-16 `TextPosition(Index, Alternate)`. `GetCaretRectangle(TextPosition)` returns local caret geometry of at least one unit width; an out-of-range index throws. Alternate encodes the second visual affinity at bidi/wrapped-line boundaries. `CaretIndices` returns a snapshot of shaped caret positions; these are not a count of .NET chars or glyphs. Callers should preserve surrogate/grapheme boundaries when editing.

`TextSelection(Start, Length)` uses UTF-16. Drawing validates nonnegative ranges within PlainText; malformed ranges throw. These record structs have ordinary generated equality/deconstruction. `Dispose` releases retained layout resources, is idempotent, and invalidates hit testing, caret enumeration, line/truncation queries, and painting with ObjectDisposedException. The immutable string/style/size snapshots remain readable. Do not use layouts concurrently.

## DrawingContext text overloads

`DrawText(string, Rect, TextStyle? = null, TextOptions? = null, TextMarkup = Plain)` creates a temporary layout at bounds.Width, aligns it vertically, draws at the top-left-derived origin, clips to bounds, then disposes it. `DrawTextLayout(TextLayout, Point, TextSelection? = null, Color? selectionColor = null, Rect? clip = null)` reuses a caller-owned layout and optionally paints selection and clips in local drawing coordinates. Default selection color is RGBA (70,110,210,120). It does not dispose the layout.

The baseline DrawText overload and MeasureText also shape text, using an unwrapped default font style. Custom DrawingContext subclasses may override DrawTextLayout; its base implementation throws NotSupportedException to preserve compatibility with existing subclasses. The window's implementation supports it. Context frame-lifetime checks apply to all text drawing.

## Text widget

Subclassable Widget in Swealium.UI. Defaults: `Content=""`, new default Style/Options, `Markup=Plain`. Setters invalidate measurement; null Content/Style/Options throw. Invalid Markup throws. Measurement constructs a shaped paragraph constrained by available content width; Draw forwards to rectangle DrawText using ContentBounds. Padding, margins, sizing, grid cells, radii, and clipping are inherited. It is not focusable or editable by default.

## TextBox widget

Subclassable Widget. Defaults: Text/Placeholder empty, Multiline=false, IsReadOnly=false, default TextStyle, Padding=8, Radii=4, MinWidth=80, white Background, gray BorderColor, translucent blue SelectionColor. IsFocusable and AcceptsTextInput initially true. Setting IsReadOnly synchronizes AcceptsTextInput and cancels composition. Direct inherited input opt-in changes remain possible for custom hosts.

| Member | Contract |
| --- | --- |
| Text | Committed plain text; normalized LF (spaces for single-line), NUL removed; assigning a changed value clears undo/redo and composition |
| Style | Shared immutable paragraph style; setter validates and invalidates layout |
| Multiline | Enables width wrapping/newlines; changing to false normalizes existing line breaks |
| Placeholder | Displayed in gray only while committed and composition text are empty; assign a nonnull string |
| Selection / CaretIndex / SelectedText | Read-only UTF-16 range, active end, and selected substring |
| Select(start,length) / SelectAll | Validate range; snap endpoints to preceding grapheme boundaries; cancel composition |
| ReplaceSelection(string) | Undoable normalized edit; null rejected; no operation in read-only mode |
| Copy / Cut / Paste | Native Unicode clipboard on the UI thread; cut/paste disabled in read-only mode |
| Undo / Redo / CanUndo / CanRedo | Up to 100 history snapshots; no operation when unavailable or read-only |
| CompositionText / Candidates / SelectedCandidate / CandidatesHorizontal | Uncommitted preedit and immutable candidate snapshot; selected index -1 means none |
| CancelComposition | Clears transient composition/candidates without committing or adding history |
| TextInputArea | Local caret rectangle including scrolling/padding; used for native input-method placement |
| TextChanged | Action after committed text mutations, including undo/redo; preedit does not raise it |
| SelectionChanged | Action when the editing caret/selection resets; may fire even when endpoints did not change |
| Submitted | Action on Enter in single-line mode |

TextBox uses grapheme-safe deletion and visual line caret geometry. Selection, clipping, navigation, scroll, composition, and candidate-popup limitations are detailed in the [guide](../text.md). It is a plain-text editor; TextMarkup.Html applies to drawing/TextLayout/Text, not TextBox.

## Composition and candidates

`TextCompositionEvent(string text, int selectionStart=0, int selectionLength=0)` rejects null text and a negative/out-of-range selection. Text/SelectionStart/SelectionLength describe UTF-16 within preedit Text. `TextCandidatesEvent(IEnumerable<string>, int selectedIndex=-1, bool horizontal=false)` snapshots nonnull strings; selectedIndex must be -1 or within the list. Both inherit routable InputEvent members.

Widget exposes CompositionInput/CandidatesInput events and protected OnCompositionInput/OnCandidatesInput hooks. InputManager.ProcessComposition/ProcessCandidates validate attachment and route through focused AcceptsTextInput widgets and ancestors until handled. Native SDL character offsets convert to UTF-16 before delivery. Focus/window cancellation clears TextBox's transient state.

## Clipboard

`Swealium.Clipboard.GetText()` returns native clipboard Unicode text, or empty if SDL reports no text/failure. `SetText(string)` rejects null/NUL and throws InvalidOperationException on a native failure. Both require an initialized native host and its UI thread; SwealiumApplication checks thread ownership while running. There is no headless managed fallback or rich clipboard format API.
