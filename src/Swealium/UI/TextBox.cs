using System.Globalization;
using System.Diagnostics;
using Swealium.Graphics;
using Swealium.Input;
namespace Swealium.UI;

/// <summary>A plain-text editor with shaped display, grapheme editing, selection, undo and native IME composition.</summary>
public class TextBox : Widget
{
    private string text = "", composition = "";
    private string placeholder = "";
    private TextStyle style = new();
    private bool multiline, dragging, readOnly;
    private int anchor, caret, compositionStart, compositionLength, preeditStart, preeditLength;
    private float scrollX, scrollY;
    private bool alternate;
    private bool ensureCaret = true;
    private Size lastViewport;
    private long blink = Stopwatch.GetTimestamp();
    private readonly List<State> undo = [], redo = [];
    private IReadOnlyList<string> candidates = Array.Empty<string>();
    private sealed record State(string Text, int Anchor, int Caret);
    public TextBox() { IsFocusable = true; AcceptsTextInput = true; Padding = new(8); Radii = new(4); MinWidth = 80; }
    public string Text
    {
        get => text;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (text == value) return;
            text = Normalize(value); CancelComposition(); undo.Clear(); redo.Clear();
            anchor = caret = Snap(Math.Min(caret, text.Length)); Changed();
        }
    }
    public TextStyle Style { get => style; set { ArgumentNullException.ThrowIfNull(value); value.Validate(); style = value; InvalidateLayout(); } }
    public bool Multiline
    {
        get => multiline;
        set { if (multiline == value) return; multiline = value; Text = Normalize(text); InvalidateLayout(); }
    }
    public bool IsReadOnly { get => readOnly; set { readOnly = value; AcceptsTextInput = !value; if (value) CancelComposition(); } }
    public string Placeholder { get => placeholder; set { ArgumentNullException.ThrowIfNull(value); placeholder = value; } }
    public Color Background { get; set; } = Color.White;
    public Color BorderColor { get; set; } = new(130,130,140);
    public Color SelectionColor { get; set; } = new(70,110,210,100);
    public TextSelection Selection => new(Math.Min(anchor, caret), Math.Abs(anchor - caret));
    public int CaretIndex => caret;
    public string SelectedText => text.Substring(Selection.Start, Selection.Length);
    public string CompositionText => composition;
    public IReadOnlyList<string> Candidates => candidates;
    public int SelectedCandidate { get; private set; } = -1;
    public bool CandidatesHorizontal { get; private set; }
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public event Action? TextChanged;
    public event Action? SelectionChanged;
    public event Action? Submitted;

    public void Select(int start, int length)
    {
        if (start < 0 || length < 0 || start > text.Length - length) throw new ArgumentOutOfRangeException(nameof(start));
        CancelComposition(); anchor = Snap(start); caret = Snap(start + length); alternate = false; ResetCaret();
    }
    public void SelectAll() => Select(0, text.Length);
    public void ReplaceSelection(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (IsReadOnly) return;
        CancelComposition(); Replace(Selection.Start, Selection.Length, Normalize(value));
    }
    public void Copy() { if (Selection.Length > 0) Clipboard.SetText(SelectedText); }
    public void Cut() { if (!IsReadOnly && Selection.Length > 0) { Copy(); ReplaceSelection(""); } }
    public void Paste() { if (!IsReadOnly) ReplaceSelection(Clipboard.GetText()); }
    public void Undo() => Restore(undo, redo);
    public void Redo() => Restore(redo, undo);
    private void Restore(List<State> source, List<State> destination)
    {
        if (IsReadOnly || source.Count == 0) return;
        CancelComposition(); destination.Add(new(text, anchor, caret));
        State state = source[^1]; source.RemoveAt(source.Count - 1);
        text = state.Text; anchor = state.Anchor; caret = state.Caret; Changed();
    }
    private void Replace(int start, int length, string value)
    {
        if (length == 0 && value.Length == 0) return;
        undo.Add(new(text, anchor, caret)); if (undo.Count > 100) undo.RemoveAt(0); redo.Clear();
        text = text.Remove(start, length).Insert(start, value);
        caret = anchor = Snap(start + value.Length, forward: true); alternate = false; Changed();
    }
    private string Normalize(string value)
    {
        value = value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\0", "");
        return Multiline ? value : value.Replace('\n', ' ');
    }
    private int[] Boundaries() => StringInfo.ParseCombiningCharacters(text).Append(text.Length).Distinct().ToArray();
    private int Snap(int index, bool forward = false)
    {
        int[] boundaries = Boundaries(); int found = Array.BinarySearch(boundaries, Math.Clamp(index, 0, text.Length));
        return found >= 0 ? boundaries[found] : boundaries[Math.Clamp(forward ? ~found : ~found - 1, 0, boundaries.Length - 1)];
    }
    private int Previous(int index) => Boundaries().LastOrDefault(i => i < index);
    private int Next(int index) => Boundaries().FirstOrDefault(i => i > index, text.Length);
    private void Changed() { InvalidateLayout(); ResetCaret(); TextChanged?.Invoke(); }
    private void ResetCaret() { ensureCaret = true; blink = Stopwatch.GetTimestamp(); SelectionChanged?.Invoke(); }
    public void CancelComposition() => ClearComposition(true);
    private void ClearComposition(bool notifyNative)
    {
        bool active = composition.Length > 0 || candidates.Count > 0;
        composition = ""; preeditStart = preeditLength = 0;
        candidates = Array.Empty<string>(); SelectedCandidate = -1; CandidatesHorizontal = false;
        if (active && notifyNative)
        {
            Widget node = this; while (node.Parent is not null) node = node.Parent;
            node.RootOwner?.CancelNativeComposition();
        }
    }
    private string DisplayText => composition.Length == 0 ? text : text.Remove(compositionStart, compositionLength).Insert(compositionStart, composition);
    private int DisplayCaret => composition.Length == 0 ? caret : compositionStart + preeditStart + preeditLength;
    private TextLayout Layout(float width)
    {
        var options = new TextOptions { Wrapping = Multiline ? TextWrapping.Word : TextWrapping.None };
        if (composition.Length == 0) return new(text, Style, options, Multiline ? width : float.PositiveInfinity);
        List<TextRun> runs = [new(text[..compositionStart], Style)];
        var composing = Style with { Underline = true };
        runs.Add(new(composition[..preeditStart], composing));
        runs.Add(new(composition.Substring(preeditStart, preeditLength), composing with { Background = SelectionColor }));
        runs.Add(new(composition[(preeditStart + preeditLength)..], composing));
        runs.Add(new(text[(compositionStart + compositionLength)..], Style));
        return new(runs, options, Multiline ? width : float.PositiveInfinity);
    }
    protected override Size MeasureContent(Size available)
    {
        using var layout = Layout(available.Width);
        return new(Math.Max(80, layout.Size.Width), Math.Max(Style.FontSize * Style.LineHeight * (Multiline ? 3 : 1), layout.Size.Height));
    }
    private Rect Caret(TextLayout layout)
    {
        Rect caretRect = layout.GetCaretRectangle(new(DisplayCaret, alternate)); Rect area = ContentBounds;
        Size viewport = new(area.Width, area.Height);
        if (ensureCaret || viewport != lastViewport)
        {
        if (caretRect.X < scrollX) scrollX = caretRect.X;
        if (caretRect.X + caretRect.Width > scrollX + area.Width) scrollX = Math.Max(0, caretRect.X + caretRect.Width - area.Width);
        if (caretRect.Y < scrollY) scrollY = caretRect.Y;
        if (caretRect.Y + caretRect.Height > scrollY + area.Height) scrollY = Math.Max(0, caretRect.Y + caretRect.Height - area.Height);
        }
        lastViewport = viewport; ensureCaret = false;
        scrollX = Math.Clamp(scrollX, 0, Math.Max(0, layout.Size.Width + 1 - area.Width));
        scrollY = Math.Clamp(scrollY, 0, Math.Max(0, layout.Size.Height - area.Height));
        return new(area.X + caretRect.X - scrollX, area.Y + caretRect.Y - scrollY, 1, caretRect.Height);
    }
    public override Rect TextInputArea { get { using var layout = Layout(ContentBounds.Width); return Caret(layout); } }
    public override void Draw(DrawingContext context)
    {
        context.FillRectangle(new(0,0,ActualWidth,ActualHeight), Background, Radii);
        context.StrokeRectangle(new(.5f,.5f,Math.Max(0,ActualWidth-1),Math.Max(0,ActualHeight-1)), IsFocused ? new(60,110,220) : BorderColor, Radii, 1);
        using var layout = Layout(ContentBounds.Width); Rect caretRect = Caret(layout); Rect area = ContentBounds;
        if (text.Length == 0 && composition.Length == 0 && Placeholder.Length > 0)
            context.DrawText(Placeholder, area, Style with { Foreground = new(140,140,145) }, new() { Wrapping = TextWrapping.None });
        else context.DrawTextLayout(layout, new(area.X - scrollX, area.Y - scrollY), composition.Length == 0 ? Selection : null, SelectionColor, area);
        if (IsFocused && !IsReadOnly && Stopwatch.GetElapsedTime(blink).TotalMilliseconds % 1000 < 600)
        {
            float top = Math.Max(area.Y, caretRect.Y), bottom = Math.Min(area.Y + area.Height, caretRect.Y + caretRect.Height);
            if (bottom > top && caretRect.X >= area.X && caretRect.X <= area.X + area.Width)
                context.FillRectangle(new(caretRect.X, top, 1, bottom-top), Style.Foreground);
        }
    }
    protected override void OnPointerInput(PointerEvent e)
    {
        base.OnPointerInput(e);
        if (e.Kind == PointerEventKind.Down && e.Button == PointerButton.Primary)
        { CancelComposition(); dragging = true; Place(e.Position, false); e.Handled = true; }
        else if (e.Kind == PointerEventKind.Move && dragging) { Place(e.Position, true); e.Handled = true; }
        else if (e.Kind is PointerEventKind.Up or PointerEventKind.Cancel) dragging = false;
        else if (e.Kind == PointerEventKind.Wheel && Multiline) { scrollY = Math.Max(0, scrollY - e.WheelDelta.Y * Style.FontSize * 3); e.Handled = true; }
    }
    private void Place(Point point, bool extend)
    {
        using var layout = Layout(ContentBounds.Width);
        var hit = layout.HitTest(new(point.X - ContentBounds.X + scrollX, point.Y - ContentBounds.Y + scrollY));
        caret = Snap(hit.Index); alternate = hit.Alternate; if (!extend) anchor = caret; ResetCaret();
    }
    protected override void OnTextInput(TextInputEvent e)
    {
        if (IsReadOnly) return;
        if (composition.Length > 0)
        {
            int start = compositionStart, length = compositionLength;
            ClearComposition(false); Replace(start, length, Normalize(e.Text));
        }
        else ReplaceSelection(e.Text);
        e.Handled = true;
    }
    protected override void OnCompositionInput(TextCompositionEvent e)
    {
        if (IsReadOnly) return;
        if (e.Text.Length == 0) { ClearComposition(false); return; }
        if (composition.Length == 0) { compositionStart = Selection.Start; compositionLength = Selection.Length; }
        composition = e.Text; preeditStart = e.SelectionStart; preeditLength = e.SelectionLength;
        ensureCaret = true; blink = Stopwatch.GetTimestamp(); e.Handled = true;
    }
    protected override void OnCandidatesInput(TextCandidatesEvent e)
    {
        if (IsReadOnly) return;
        candidates = e.Candidates; SelectedCandidate = e.SelectedIndex; CandidatesHorizontal = e.Horizontal; e.Handled = true;
    }
    protected override void OnFocusChanged() { if (!IsFocused) { dragging = false; ClearComposition(false); } blink = Stopwatch.GetTimestamp(); }
    protected override void OnKeyboardInput(KeyEvent e)
    {
        if (!e.IsDown) return;
        if (composition.Length > 0) { if (e.Key == Key.Escape) CancelComposition(); return; }
        bool command = (e.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0 && !e.Modifiers.HasFlag(KeyModifiers.Alt);
        bool shift = e.Modifiers.HasFlag(KeyModifiers.Shift);
        if (command)
        {
            switch (e.Key)
            {
                case Key.A: SelectAll(); break;
                case Key.C: Copy(); break;
                case Key.X: Cut(); break;
                case Key.V: Paste(); break;
                case Key.Z: if (shift) Redo(); else Undo(); break;
                case Key.Y: Redo(); break;
                default: goto navigation;
            }
            e.Handled = true; return;
        }
        navigation:
        int target = caret;
        switch (e.Key)
        {
            case Key.Backspace:
            case Key.Delete:
                if (!IsReadOnly)
                {
                    if (Selection.Length > 0) ReplaceSelection("");
                    else if (e.Key == Key.Backspace) { int start = Previous(caret); Replace(start, caret-start, ""); }
                    else Replace(caret, Next(caret)-caret, "");
                }
                break;
            case Key.Enter: if (Multiline) ReplaceSelection("\n"); else Submitted?.Invoke(); break;
            case Key.Left:
            case Key.Right:
                bool right = e.Key == Key.Right;
                if (command)
                {
                    using var words = Layout(ContentBounds.Width);
                    var positions = words.NativeBlock.WordBoundaryIndicies.Select(words.FromCodePoint).Distinct().ToArray();
                    target = right ? positions.FirstOrDefault(i => i > caret, text.Length) : positions.LastOrDefault(i => i < caret);
                }
                else if (!shift && Selection.Length > 0) target = right ? Selection.Start + Selection.Length : Selection.Start;
                else target = VisualMove(right);
                Move(target, shift); break;
            case Key.Home:
            case Key.End:
            case Key.Up:
            case Key.Down:
            case Key.PageUp:
            case Key.PageDown:
                using (var layout = Layout(ContentBounds.Width))
                {
                    Rect rectangle = layout.GetCaretRectangle(new(caret, alternate));
                    if (command && e.Key is Key.Home or Key.End) target = e.Key == Key.Home ? 0 : text.Length;
                    else
                    {
                        float x = e.Key == Key.Home ? -float.MaxValue : e.Key == Key.End ? float.MaxValue : rectangle.X;
                        float y = rectangle.Y + rectangle.Height / 2;
                        if (e.Key == Key.Up) y -= rectangle.Height;
                        if (e.Key == Key.Down) y += rectangle.Height;
                        if (e.Key == Key.PageUp) y -= ContentBounds.Height;
                        if (e.Key == Key.PageDown) y += ContentBounds.Height;
                        var hit = layout.HitTest(new(x,y)); target = hit.Index; alternate = hit.Alternate;
                    }
                }
                Move(target, shift); break;
            default: return;
        }
        e.Handled = true;
    }
    private int VisualMove(bool right)
    {
        using var layout = Layout(ContentBounds.Width);
        Rect current = layout.GetCaretRectangle(new(caret, alternate));
        var positions = Boundaries().SelectMany(index => new[] { new TextPosition(index), new TextPosition(index, true) })
            .Select(position => (Position: position, Rect: layout.GetCaretRectangle(position)))
            .Where(item => Math.Abs(item.Rect.Y-current.Y) < .1f && (right ? item.Rect.X > current.X+.1f : item.Rect.X < current.X-.1f))
            .OrderBy(item => Math.Abs(item.Rect.X-current.X)).ToArray();
        if (positions.Length > 0) { alternate = positions[0].Position.Alternate; return positions[0].Position.Index; }
        alternate = false; return right ? Next(caret) : Previous(caret);
    }
    private void Move(int target, bool extend) { caret = Snap(target); if (!extend) anchor = caret; ResetCaret(); }

    // Candidate events are informational: the operating system owns conversion/selection/commit.
    internal void DrawCandidates(DrawingContext context, Size viewport)
    {
        if (Candidates.Count == 0 || !IsFocused) return;
        Rect caretRect = TextInputArea;
        for (Widget? node = this; node is not null; node = node.Parent)
            caretRect = caretRect with { X = caretRect.X + node.Bounds.X, Y = caretRect.Y + node.Bounds.Y };
        int first = Math.Max(0, SelectedCandidate), count = Math.Min(9, Candidates.Count - first);
        var labels = Enumerable.Range(first, count).Select(i => new TextLayout($"{i+1}. {Candidates[i]}", Style)).ToArray();
        try
        {
            float cellHeight = labels.Max(l => l.Size.Height) + 8;
            float width = CandidatesHorizontal ? labels.Sum(l => l.Size.Width+16) : labels.Max(l => l.Size.Width)+16;
            width = Math.Min(viewport.Width, width);
            float height = Math.Min(viewport.Height, CandidatesHorizontal ? cellHeight : count * cellHeight);
            float x = Math.Clamp(caretRect.X, 0, Math.Max(0, viewport.Width-width));
            float y = caretRect.Y + caretRect.Height;
            if (y+height > viewport.Height) y = Math.Max(0, caretRect.Y-height);
            Rect panel = new(x,y,width,height);
            context.FillRectangle(new(x,y,width,height), Background, 3);
            context.StrokeRectangle(new(x,y,width,height), BorderColor, cornerRadius: 3);
            for (int i=0; i<count; i++)
            {
                float w = CandidatesHorizontal ? labels[i].Size.Width+16 : width;
                float visibleWidth = Math.Max(0, Math.Min(w, panel.X+panel.Width-x));
                float visibleHeight = Math.Max(0, Math.Min(cellHeight, panel.Y+panel.Height-y));
                if (visibleWidth <= 0 || visibleHeight <= 0) break;
                var rect = new Rect(x,y,visibleWidth,visibleHeight);
                if (i+first == SelectedCandidate) context.FillRectangle(rect, SelectionColor);
                context.DrawTextLayout(labels[i], new(x+8,y+4), clip: rect);
                if (CandidatesHorizontal) x += w; else y += cellHeight;
            }
        }
        finally { foreach (var label in labels) label.Dispose(); }
    }
}
