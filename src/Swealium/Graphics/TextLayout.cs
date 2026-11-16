using SkiaSharp;
using System.Globalization;
using System.Text;
using Kit = Topten.RichTextKit;
namespace Swealium.Graphics;

/// <summary>Immutable shaped paragraph layout. Indices use .NET UTF-16; geometry uses logical units.</summary>
public sealed class TextLayout : IDisposable
{
    private readonly Kit.TextBlock block = new();
    private readonly Dictionary<Kit.IStyle,TextStyle> sourceStyles = new();
    private readonly Dictionary<Font,Font.Lease> fontLeases=new();
    private readonly Dictionary<string,SKTypeface> fontFaces=new(StringComparer.Ordinal);
    private sealed class LoadedFontMapper(Dictionary<string,SKTypeface> faces,Dictionary<Kit.IStyle,TextStyle> styles) : Kit.FontMapper
    {
        public override SKTypeface TypefaceFromStyle(Kit.IStyle style,bool ignoreFontVariants)
        {
            if(faces.TryGetValue(style.FontFamily,out var face))return face;
            if(styles.TryGetValue(style,out var source)&&source.FontSlant==Graphics.FontSlant.Oblique)
                return SKTypeface.FromFamilyName(style.FontFamily,new SKFontStyle(style.FontWeight,(int)style.FontWidth,SKFontStyleSlant.Oblique));
            return base.TypefaceFromStyle(style,ignoreFontVariants);
        }
    }
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
        block.FontMapper=new LoadedFontMapper(fontFaces,sourceStyles);
        try
        {
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
        catch
        {
            try{block.Clear();block.Layout();}finally{foreach(var lease in fontLeases.Values)lease.Dispose();fontLeases.Clear();}
            throw;
        }
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
    /// <summary>UTF-16 index of the grapheme under the point, rather than its nearest insertion edge.</summary>
    public int HitTestCharacter(Point point)
    {
        Verify();var hit=block.HitTest(point.X,point.Y);
        return FromCodePoint(hit.OverCodePointIndex>=0?hit.OverCodePointIndex:hit.ClosestCodePointIndex);
    }
    public TextSelection GetWordRange(int index)
    {
        Verify();ValidateIndex(index);if(PlainText.Length==0)return default;
        var boundaries=StringInfo.ParseCombiningCharacters(PlainText).Append(PlainText.Length).ToArray();
        int at=Array.BinarySearch(boundaries,index);at=at>=0?Math.Min(at,boundaries.Length-2):Math.Max(0,~at-1);
        int kind=Category(boundaries[at]);int first=at,last=at+1;
        if(kind!=2)
        {
            while(first>0&&Category(boundaries[first-1])==kind)first--;
            while(last<boundaries.Length-1&&Category(boundaries[last])==kind)last++;
        }
        return new(boundaries[first],boundaries[last]-boundaries[first]);
        int Category(int position)
        {
            var rune=Rune.TryGetRuneAt(PlainText,position,out var found)?found:Rune.ReplacementChar;
            if(rune.Value is '\n' or '\r' or 0x2028 or 0x2029)return 2;
            if(Rune.IsWhiteSpace(rune))return 1;
            return Rune.GetUnicodeCategory(rune) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.LetterNumber or UnicodeCategory.OtherNumber or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark?0:2;
        }
    }
    /// <summary>The visual line containing a caret, excluding its line terminator.</summary>
    public TextSelection GetLineRange(TextPosition position)
    {
        Verify();ValidateIndex(position.Index);var start=LineEdge(position,false);var end=LineEdge(position,true);
        return new(start.Index,Math.Max(0,end.Index-start.Index));
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
        while (end && edge>start && PlainText[edge-1] is '\n' or '\r' or '\u2028' or '\u2029') edge--;
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
        try{block.Clear();block.Layout();}
        finally{foreach(var lease in fontLeases.Values)lease.Dispose();fontLeases.Clear();fontFaces.Clear();sourceStyles.Clear();disposed=true;}
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
        string family=style.FontFamily??SKTypeface.Default.FamilyName;
        if(style.Font is {} loaded)
        {
            loaded.VerifyParagraphSupport();
            if(!fontLeases.TryGetValue(loaded,out var lease)){lease=loaded.Acquire();fontLeases.Add(loaded,lease);}
            family=loaded.MappingName;fontFaces[family]=lease.Typeface;
        }
        var native = new Kit.Style()
    {
        FontFamily = family,FontWidth=(SKFontStyleWidth)style.FontWidth,
        FontSize = style.FontSize, FontWeight = style.FontWeight, FontItalic = style.FontSlant is {} slant?slant!=Graphics.FontSlant.Upright:style.Italic,
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

