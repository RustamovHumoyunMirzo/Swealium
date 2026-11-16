# Text API contracts

[Documentation index](../README.md) · [Text guide and examples](../text.md) · [Complete signatures](reference.md)

Use these APIs on the UI thread. No backend text/font/HTML objects appear in public signatures.

## TextStyle and TextOptions

Both are immutable sealed records; use `with` to produce changes. They are validated when a layout is constructed or a widget property is assigned.

| TextStyle member | Default / contract |
| --- | --- |
| Font | null; an explicit owned Font face overrides family/style matching; see [font contracts](../fonts.md) |
| FontFamily | null: system default; unavailable families use backend/system fallback |
| FontWidth / FontSlant | Normal / null; null slant preserves Italic, explicit slant takes precedence |
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

Loaded-font, TextStyle baseline, shaped-segment drawing, OpenType controls and extended measurement overloads have dedicated [font contracts](../fonts.md).

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
| Placeholder | Displayed only while committed and composition text are empty; assign a nonnull string |
| PlaceholderColor | RGB(140,140,145) by default; independent solid/gradient foreground |
| Editor | Get-only TextEditor controller, also exposed through Widget.TextEditor |
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

## Selection ranges

TextLayout.HitTestCharacter(Point) returns the UTF-16 character/grapheme position under a point rather than its nearest insertion edge, falling back to the nearest position outside glyphs. GetWordRange(int index) returns a TextSelection using Unicode letter/number/mark groups and whitespace/punctuation boundaries. GetLineRange(TextPosition) returns the containing visual line excluding CR/LF/Unicode line terminators and respecting wrapped caret affinity. Invalid indices throw ArgumentOutOfRangeException; empty text returns an empty range. Methods require an undisposed layout.

Text exposes Selectable (false), SelectionColor, Selection, SelectedText, Select(int,int), SelectAll(), Copy(), and SelectionChanged. Its attached controller is read-only and follows decoded PlainText indices for HTML. Selection snapping uses grapheme boundaries. See [Text widget](../widgets/text.md) and [selection rules](../text-selection.md).

## TextEditor composition controller

Namespace Swealium.Input. Sealed; constructor TextEditor(Widget owner) rejects null or an owner with an existing controller. It attaches once, updates Owner.IsFocusable/AcceptsTextInput/Cursor, and becomes available via Owner.TextEditor. No IDisposable contract; state shares its owner's UI-thread lifetime. Frame contexts are borrowed and never retained.

| Members | Contract |
| --- | --- |
| Owner | Get-only attached Widget |
| IsEnabled | true; suppresses automatic editing input when false and resets drag/composition; controls Owner.AcceptsTextInput together with IsReadOnly |
| Viewport | Rect? null; defaults Owner.ContentBounds, setter requires finite nonnegative local rectangle |
| LayoutFactory | Func&lt;float,TextLayout&gt;? null; read-only display override; fresh returned layout transfers ownership and must have PlainText equal Text |
| VerticalAlignment | Top; validates TextVerticalAlignment; offsets drawing and hit testing consistently |
| Text / Style / Multiline / IsReadOnly | Same text normalization, layout, read-only, and history contracts as TextBox; defaults empty/new TextStyle/false/false |
| Placeholder / PlaceholderColor / SelectionColor | Empty / RGB(140,140,145) / RGBA(70,110,210,100) |
| CandidateBackground / CandidateBorderColor | White / RGB(130,130,140); popup colors; Draw does not paint owner chrome |
| Selection / CaretIndex / SelectedText | Get-only UTF-16 model selection and active end |
| CompositionText / Candidates / SelectedCandidate / CandidatesHorizontal | Same preedit/candidate snapshots as TextBox |
| CanUndo / CanRedo | Get-only history availability |
| TextInputArea | Get-only local scrolled caret rectangle for Widget/native IME placement |
| Measure(Size available) | Shaped intrinsic size; existing editor minimum 80 wide and one/three lines in single/multiline mode |
| Draw(DrawingContext) | Paint text/placeholder/selection/caret inside Viewport; no owner background/stroke |
| DrawCandidates(DrawingContext,Size viewport) | Draw candidate overlay in window coordinates; SwealiumWindow calls it automatically for enabled focused controllers |
| Select / SelectAll / ReplaceSelection / Copy / Cut / Paste / Undo / Redo / CancelComposition | Same methods/validation as TextBox |
| TextChanged / SelectionChanged / Submitted | Same Action event semantics as TextBox |

Widget routes unhandled pointer/key/text/composition/candidate input automatically after protected hooks and before public input events. Focus notifications cancel transient state on loss. Raw Widget events still support independent editing engines. Set a custom Viewport during arrangement so IME and pointer input agree before painting. Editable controllers use the built-in plain-text layout; LayoutFactory is read-only-only behavior. Text widgets permanently lock their controller's IsReadOnly flag; setting it false throws InvalidOperationException.

Repeated clicks select word/visual line/all at counts 2/3/4+. SDL supplies PointerEvent.ClickCount; custom-host count zero uses the controller's 500 ms / five logical units per axis fallback for one pointer. Programmatic selection methods do not require focus. Clipboard methods require an initialized native host. See [composition example](../text-selection.md#composition-instead-of-inheritance).

## SharedTextSelection and TextSelectionPart

Swealium.Input.SharedTextSelection is sealed and obtained through InputManager.TextSelection; it has no public constructor. One instance is owned by each InputManager/window. Ranges returns an immutable snapshot of TextSelectionPart(Text Widget, Swealium.Graphics.TextSelection Range) records, including zero-length boundary fragments. SelectedText joins fragments with LF in depth-first insertion order. Changed is an Action event raised after shared changes. Clear() removes highlights and anchors; SelectAll() selects every eligible selectable Text in that input tree; Copy() writes the combined string through Clipboard if nonempty. Querying Ranges/SelectedText validates fragment eligibility and model bounds.

Each Text.Selection is its local range. Attached Text.SelectedText/Copy expose the global range; TextEditor.SelectedText remains local. Selection through Text.Select, pointer gestures, or keyboard navigation claims shared ownership and removes competing highlights. Editable controls keep their own editing models and do not join the read-only document. Root replacement, selected-fragment removal/hiding/disabling, or model changes invalidate the range. Each window is independent. Operations follow the input tree's UI-thread convention; Copy requires an active native host. See [shared selection behavior](../text-selection.md#one-shared-selection-per-window).
