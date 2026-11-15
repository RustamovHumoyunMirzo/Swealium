namespace Swealium.Input;

/// <summary>Uncommitted IME text. Selection offsets are UTF-16 within Text.</summary>
public sealed class TextCompositionEvent : InputEvent
{
    public TextCompositionEvent(string text, int selectionStart = 0, int selectionLength = 0)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (selectionStart < 0 || selectionLength < 0 || selectionStart > text.Length - selectionLength) throw new ArgumentOutOfRangeException(nameof(selectionStart));
        Text = text; SelectionStart = selectionStart; SelectionLength = selectionLength;
    }
    public string Text { get; }
    public int SelectionStart { get; }
    public int SelectionLength { get; }
}

/// <summary>A copied snapshot of platform IME candidates. Selection is controlled by the IME.</summary>
public sealed class TextCandidatesEvent : InputEvent
{
    public TextCandidatesEvent(IEnumerable<string> candidates, int selectedIndex = -1, bool horizontal = false)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var values = candidates.ToArray();
        if (values.Any(value => value is null)) throw new ArgumentException("Candidates cannot contain null.", nameof(candidates));
        if (selectedIndex < -1 || selectedIndex >= values.Length) throw new ArgumentOutOfRangeException(nameof(selectedIndex));
        Candidates = Array.AsReadOnly(values); SelectedIndex = selectedIndex; Horizontal = horizontal;
    }
    public IReadOnlyList<string> Candidates { get; }
    public int SelectedIndex { get; }
    public bool Horizontal { get; }
}
