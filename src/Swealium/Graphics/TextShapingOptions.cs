namespace Swealium.Graphics;

public enum FontHinting { None, Slight, Normal, Full }
/// <summary>An OpenType feature with a half-open UTF-16 source range. Null End covers the rest of the text.</summary>
public readonly record struct FontFeature
{
    public FontFeature(string tag,uint value=1,int start=0,int? end=null)
    {
        _=Font.Tag(tag);if(start<0||end<start)throw new ArgumentOutOfRangeException(nameof(start));Tag=tag;Value=value;Start=start;End=end;
    }
    public string Tag {get;}
    public uint Value {get;}
    public int Start {get;}
    public int? End {get;}
    public static FontFeature Kerning(bool enabled=true)=>new("kern",enabled?1u:0u);
    public static FontFeature Ligatures(bool enabled=true)=>new("liga",enabled?1u:0u);
    public static FontFeature StylisticSet(int set,bool enabled=true)
    {if(set<1||set>20)throw new ArgumentOutOfRangeException(nameof(set));return new("ss"+set.ToString("D2",System.Globalization.CultureInfo.InvariantCulture),enabled?1u:0u);}
}
/// <summary>Shaping of one horizontal, single-font script/direction segment. Use TextLayout for paragraph bidi/fallback/wrapping.</summary>
public sealed record TextShapingOptions
{
    public IReadOnlyList<FontFeature> Features {get;init;}=Array.Empty<FontFeature>();
    public TextDirection Direction {get;init;}
    public string? Language {get;init;}
    /// <summary>Four-letter ISO 15924 script tag, for example Latn or Arab. Null lets shaping infer it.</summary>
    public string? Script {get;init;}
    public float LetterSpacing {get;init;}
    public FontHinting Hinting {get;init;}=FontHinting.Normal;
    public bool SubpixelPositioning {get;init;}=true;
    public bool LinearMetrics {get;init;}=true;
    internal TextShapingOptions Snapshot(string text)
    {
        if(!Enum.IsDefined(Direction)||!Enum.IsDefined(Hinting)||!float.IsFinite(LetterSpacing))throw new ArgumentOutOfRangeException(nameof(TextShapingOptions));
        if(Script is not null&&(Script.Length!=4||Script.Any(c=>!char.IsAsciiLetter(c))))throw new ArgumentException("Script requires a four-letter ISO 15924 tag.",nameof(Script));
        if(Language is not null&&(Language.Length==0||Language.Length>128||Language.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-')))throw new ArgumentException("Language requires a language tag, for example en or ar.",nameof(Language));
        ArgumentNullException.ThrowIfNull(Features);var copy=Features.ToArray();if(copy.Length>256)throw new ArgumentException("At most 256 OpenType feature settings.",nameof(Features));
        foreach(var feature in copy)
        {
            _=Font.Tag(feature.Tag);
            if(feature.Start<0||feature.Start>text.Length||feature.End is {} end&&(end<feature.Start||end>text.Length))throw new ArgumentOutOfRangeException(nameof(Features));
            if(InsideSurrogate(feature.Start)||feature.End is {} limit&&InsideSurrogate(limit))throw new ArgumentException("Feature boundaries cannot split a surrogate pair.",nameof(Features));
        }
        return this with{Features=Array.AsReadOnly(copy)};
        bool InsideSurrogate(int index)=>index>0&&index<text.Length&&char.IsHighSurrogate(text[index-1])&&char.IsLowSurrogate(text[index]);
    }
}
/// <summary>A visual-order glyph; Cluster is a UTF-16 source index and geometry is baseline-relative.</summary>
public readonly record struct ShapedGlyph(ushort Id,int Cluster,Point Position,Point Advance);
