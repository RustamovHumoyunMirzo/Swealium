using SkiaSharp;
using Kit = Topten.RichTextKit;
namespace Swealium.Graphics;

/// <summary>Immutable shaped paragraph layout. Indices use .NET UTF-16; geometry uses logical units.</summary>
public sealed class TextLayout : IDisposable
{
    private readonly Kit.TextBlock block = new();
    private readonly Dictionary<Kit.IStyle,TextStyle> sourceStyles = new();
    private readonly int[] codePointOffsets;
    private readonly TextStyle baseStyle;
    private bool disposed;
    internal Kit.TextBlock NativeBlock => block;
    public TextLayout(string text, TextStyle? style = null, TextOptions? options = null,
        float maxWidth = float.PositiveInfinity, TextMarkup markup = TextMarkup.Plain)
        : this(Parse(text, style ?? new(), markup), options, maxWidth) { }

    public TextLayout(IEnumerable<TextRun> runs, TextOptions? options = null, float maxWidth = float.PositiveInfinity)
    {
        ArgumentNullException.ThrowIfNull(runs);
        options ??= new(); options.Validate();
        if (float.IsNaN(maxWidth) || maxWidth < 0) throw new ArgumentOutOfRangeException(nameof(maxWidth));
        var snapshot = runs.ToArray();
        foreach (var run in snapshot)
        {
            ArgumentNullException.ThrowIfNull(run); ArgumentNullException.ThrowIfNull(run.Text);
            ArgumentNullException.ThrowIfNull(run.Style); run.Style.Validate();
        }
        baseStyle = snapshot.FirstOrDefault()?.Style ?? new();
        Runs = Array.AsReadOnly(snapshot);
        PlainText = string.Concat(snapshot.Select(run => run.Text));
        var offsets = new List<int> { 0 };
        int position = 0;
        foreach (var rune in PlainText.EnumerateRunes()) { position += rune.Utf16SequenceLength; offsets.Add(position); }
        codePointOffsets = offsets.ToArray();
        block.MaxWidth = options.Wrapping == TextWrapping.Word && float.IsFinite(maxWidth) ? Math.Max(1, maxWidth) : null;
        block.RenderWidth = float.IsFinite(maxWidth) ? maxWidth : null;
        block.MaxLines = options.MaxLines;
        block.EllipsisEnabled = options.Ellipsis;
        block.BaseDirection = Direction(options.Direction);
        bool rtl = options.Direction == TextDirection.RightToLeft;
        block.Alignment = options.Alignment switch
        {
            TextAlignment.Center => Kit.TextAlignment.Center,
            TextAlignment.Left => Kit.TextAlignment.Left,
            TextAlignment.Right => Kit.TextAlignment.Right,
            TextAlignment.End => options.Direction == TextDirection.Automatic ? Kit.TextAlignment.Auto : rtl ? Kit.TextAlignment.Left : Kit.TextAlignment.Right,
            _ => options.Direction == TextDirection.Automatic ? Kit.TextAlignment.Auto : rtl ? Kit.TextAlignment.Right : Kit.TextAlignment.Left
        };
        foreach (var run in snapshot) block.AddText(run.Text, ToNative(run.Style));
        // A zero-width final run supplies caret/font metrics for empty and trailing-newline paragraphs.
        block.AddText("\u200b", ToNative(snapshot.LastOrDefault()?.Style ?? baseStyle));
        block.Layout();
        if (options.Alignment == TextAlignment.End && options.Direction == TextDirection.Automatic && float.IsFinite(maxWidth))
        {
            // Auto alignment encodes the engine's resolved paragraph direction. A short
            // line has a positive left inset only when its natural alignment is right.
            bool naturalRight = block.Lines.Any(line => line.Width < maxWidth - .01f &&
                line.Runs.Count > 0 && line.Runs.Min(run => run.XCoord) > .01f);
            block.Alignment = naturalRight ? Kit.TextAlignment.Left : Kit.TextAlignment.Right;
            block.Layout();
        }
        Size = new(block.MeasuredWidth, Math.Max(block.MeasuredHeight, baseStyle.FontSize * baseStyle.LineHeight));
        Baseline = block.Lines.Count == 0 ? baseStyle.FontSize : block.Lines[0].BaseLine;
    }
    public string PlainText { get; }
    public IReadOnlyList<TextRun> Runs { get; }
    public Size Size { get; }
    public float Baseline { get; }
    public int LineCount { get { Verify(); return block.LineCount; } }
    public bool IsTruncated { get { Verify(); return block.Truncated; } }
    public TextPosition HitTest(Point point)
    {
        Verify();
        var hit = block.HitTest(point.X, point.Y);
        return new(FromCodePoint(hit.ClosestCodePointIndex), hit.AltCaretPosition);
    }
    public Rect GetCaretRectangle(TextPosition position)
    {
        Verify(); ValidateIndex(position.Index);
        var info = block.GetCaretInfo(new Kit.CaretPosition(ToCodePoint(position.Index), position.Alternate));
        if (info.IsNone) return new(0, 0, 1, baseStyle.FontSize * baseStyle.LineHeight);
        var r = info.CaretRectangle;
        return new(r.Left, r.Top, Math.Max(1, r.Width), Math.Max(baseStyle.FontSize, r.Height));
    }
    public IReadOnlyList<int> CaretIndices
    {
        get { Verify(); return block.CaretIndicies.Select(FromCodePoint).Distinct().ToArray(); }
    }
    internal int ToCodePoint(int index)
    {
        int found = Array.BinarySearch(codePointOffsets, Math.Clamp(index, 0, PlainText.Length));
        return found >= 0 ? found : Math.Max(0, ~found - 1);
    }
    internal int FromCodePoint(int index) => codePointOffsets[Math.Clamp(index, 0, codePointOffsets.Length - 1)];
    internal TextPosition LineEdge(TextPosition position, bool end)
    {
        Verify(); ValidateIndex(position.Index);
        var caret = block.GetCaretInfo(new Kit.CaretPosition(ToCodePoint(position.Index), position.Alternate));
        int lineIndex = Math.Clamp(caret.LineIndex, 0, block.Lines.Count - 1);
        var line = block.Lines[lineIndex];
        int start=FromCodePoint(line.Start),limit=FromCodePoint(line.End);
        int edge=end?limit:start;
        if (end && edge>start && PlainText[edge-1] is '\n' or '\r' or '\u2028' or '\u2029') edge--;
        return new(edge,end&&edge==limit&&lineIndex<block.Lines.Count-1);
    }
    internal void Paint(SKCanvas canvas, Point origin, TextSelection? selection, Color? selectionColor, bool antiAliasing = true)
    {
        Verify();
        if (selection is { } range && (range.Length < 0 || range.Start < 0 || range.Start > PlainText.Length-range.Length))
            throw new ArgumentOutOfRangeException(nameof(selection));
        var shaders = new Dictionary<Color,SKShader>();
        var bounds = new Rect(0,0,Math.Max(block.RenderWidth??Size.Width,.0001f),Math.Max(Size.Height,.0001f));
        var options = new Kit.TextPaintOptions { SelectionHandleScale=0, Edging=antiAliasing?SKFontEdging.Antialias:SKFontEdging.Alias };
        SKShader Shader(Color color)
        {if(!shaders.TryGetValue(color,out var shader))shaders.Add(color,shader=GradientPaint.Shader(color.Gradient!,bounds));return shader;}
        options.ConfigurePaint=(paint,style,halo)=>
        {
            paint.IsAntialias=antiAliasing;
            if(!halo&&sourceStyles.TryGetValue(style,out var source)&&source.Foreground.IsGradient)
            {paint.Color=SKColors.White.WithAlpha(source.Foreground.A);paint.Shader=Shader(source.Foreground);}
        };
        if(selection is {} selected)
        {
            options.Selection=new Kit.TextRange(ToCodePoint(selected.Start),ToCodePoint(selected.Start+selected.Length));
            options.SelectionColor=Native(selectionColor??new Color(70,110,210,120));
        }
        bool custom=sourceStyles.Values.Any(style=>style.Background.IsGradient)||(selectionColor?.IsGradient==true&&selection is not null);
        int saved=canvas.Save();
        try
        {
            canvas.Translate(origin.X,origin.Y);
            if(custom)
            {
                var nativeStyles=sourceStyles.Keys.Cast<Kit.Style>().ToArray();
                try
                {
                    foreach(var color in sourceStyles.Values.Select(style=>style.Background).Where(c=>c.A>0).Distinct())
                    {
                        foreach(var native in nativeStyles)native.BackgroundColor=sourceStyles[native].Background==color?SKColors.White:SKColors.Transparent;
                        Mask(color,null);
                    }
                    foreach(var native in nativeStyles)native.BackgroundColor=SKColors.Transparent;
                    if(selection is not null)Mask(selectionColor??new Color(70,110,210,120),options.Selection);
                    options.Selection=null;
                    block.Paint(canvas,options);
                }
                finally {foreach(var native in nativeStyles)native.BackgroundColor=Native(sourceStyles[native].Background);}
            }
            else block.Paint(canvas,options);
        }
        finally {canvas.RestoreToCount(saved);foreach(var shader in shaders.Values)shader.Dispose();}
        void Mask(Color color,Kit.TextRange? range)
        {
            int layer=canvas.SaveLayer();
            try
            {
                block.Paint(canvas,new Kit.TextPaintOptions { SelectionHandleScale=0,Selection=range,SelectionColor=SKColors.White,
                    ConfigurePaint=(paint,_,_)=>{paint.Shader=null;paint.Color=SKColors.Transparent;} });
                using var paint=GradientPaint.Create(color,bounds,antiAliasing);paint.BlendMode=SKBlendMode.SrcIn;
                canvas.DrawPaint(paint);
            }
            finally {canvas.RestoreToCount(layer);}
        }
    }
    private void ValidateIndex(int index)
    {
        if (index < 0 || index > PlainText.Length) throw new ArgumentOutOfRangeException(nameof(index));
    }
    private void Verify() => ObjectDisposedException.ThrowIf(disposed, this);
    public void Dispose()
    {
        if (disposed) return;
        block.Clear(); block.Layout(); disposed = true;
        GC.SuppressFinalize(this);
    }
    private static IEnumerable<TextRun> Parse(string text, TextStyle style, TextMarkup markup)
    {
        ArgumentNullException.ThrowIfNull(text); style.Validate();
        return markup switch
        {
            TextMarkup.Plain => [new TextRun(text, style)],
            TextMarkup.Html => HtmlText.Parse(text, style),
            _ => throw new ArgumentOutOfRangeException(nameof(markup))
        };
    }
    internal static SKColor Native(Color c) => new(c.R, c.G, c.B, c.A);
    private static Kit.TextDirection Direction(TextDirection direction) => direction switch
    {
        TextDirection.LeftToRight => Kit.TextDirection.LTR,
        TextDirection.RightToLeft => Kit.TextDirection.RTL,
        _ => Kit.TextDirection.Auto
    };
    private Kit.Style ToNative(TextStyle style)
    {
        var native = new Kit.Style()
    {
        FontFamily = style.FontFamily ?? SKTypeface.Default.FamilyName,
        FontSize = style.FontSize, FontWeight = style.FontWeight, FontItalic = style.Italic,
        TextColor = Native(style.Foreground), BackgroundColor = Native(style.Background),
        Underline = style.Underline ? Kit.UnderlineStyle.Solid : Kit.UnderlineStyle.None,
        StrikeThrough = style.StrikeThrough ? Kit.StrikeThroughStyle.Solid : Kit.StrikeThroughStyle.None,
        LineHeight = style.LineHeight, LetterSpacing = style.LetterSpacing, TextDirection = Direction(style.Direction),
        FontVariant = style.Script switch { TextScript.Subscript => Kit.FontVariant.SubScript, TextScript.Superscript => Kit.FontVariant.SuperScript, _ => Kit.FontVariant.Normal }
        };
        sourceStyles.Add(native,style);
        return native;
    }
}

