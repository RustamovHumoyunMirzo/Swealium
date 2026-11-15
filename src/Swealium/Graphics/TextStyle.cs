namespace Swealium.Graphics;

public enum TextWrapping { None, Word }
public enum TextAlignment { Start, Center, End, Left, Right }
public enum TextDirection { Automatic, LeftToRight, RightToLeft }
public enum TextMarkup { Plain, Html }
public enum TextScript { Normal, Subscript, Superscript }
public enum TextVerticalAlignment { Top, Center, Bottom }

public sealed record TextStyle
{
    public string? FontFamily { get; init; }
    public float FontSize { get; init; } = 16;
    public int FontWeight { get; init; } = 400;
    public bool Italic { get; init; }
    public bool Underline { get; init; }
    public bool StrikeThrough { get; init; }
    public Color Foreground { get; init; } = Color.Black;
    public Color Background { get; init; } = Color.Transparent;
    public float LineHeight { get; init; } = 1;
    public float LetterSpacing { get; init; }
    public TextDirection Direction { get; init; }
    public TextScript Script { get; init; }
    internal void Validate()
    {
        if (!float.IsFinite(FontSize) || FontSize <= 0 || FontWeight is < 1 or > 1000 ||
            !float.IsFinite(LineHeight) || LineHeight <= 0 || !float.IsFinite(LetterSpacing) ||
            !Enum.IsDefined(Direction) || !Enum.IsDefined(Script))
            throw new ArgumentOutOfRangeException(nameof(TextStyle));
    }
}
public sealed record TextOptions
{
    public TextWrapping Wrapping { get; init; } = TextWrapping.Word;
    public TextAlignment Alignment { get; init; } = TextAlignment.Start;
    public TextVerticalAlignment VerticalAlignment { get; init; }
    public TextDirection Direction { get; init; }
    public int? MaxLines { get; init; }
    public bool Ellipsis { get; init; }
    internal void Validate()
    {
        if (!Enum.IsDefined(Wrapping) || !Enum.IsDefined(Alignment) || !Enum.IsDefined(VerticalAlignment) ||
            !Enum.IsDefined(Direction) || MaxLines is <= 0) throw new ArgumentOutOfRangeException(nameof(TextOptions));
    }
}
public sealed record TextRun(string Text, TextStyle Style);
/// <summary>UTF-16 index and alternate visual affinity at a bidi/line boundary.</summary>
public readonly record struct TextPosition(int Index, bool Alternate = false);
public readonly record struct TextSelection(int Start, int Length);

