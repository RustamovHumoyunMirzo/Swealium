using System.Buffers.Binary;
using System.Text;
using SkiaSharp;
namespace Swealium.Graphics;

public enum FontWidth { UltraCondensed=1, ExtraCondensed, Condensed, SemiCondensed, Normal, SemiExpanded, Expanded, ExtraExpanded, UltraExpanded }
public enum FontSlant { Upright, Italic, Oblique }
public readonly record struct FontVariationAxis(string Tag,float Minimum,float Default,float Maximum,bool IsHidden);
public readonly record struct FontVariation(string Tag,float Value);
/// <summary>Logical font metrics; ascent/descent are positive distances from the baseline.</summary>
public readonly record struct FontMetrics(float Ascent,float Descent,float Leading,float CapHeight,float XHeight)
{ public float LineHeight=>Math.Max(0,Ascent+Descent+Leading); }

/// <summary>Owned immutable font face. Existing layouts retain a lease; new uses reject disposed fonts.</summary>
public sealed class Font : IDisposable
{
    private static long nextId;
    internal string MappingName {get;}="Swealium-font-"+Interlocked.Increment(ref nextId);
    private readonly object gate=new();
    private SKTypeface? native;
    private int leases;
    private bool disposed;
    private Font(SKTypeface typeface)
    {
        native=typeface;
        try
        {
            FamilyName=typeface.FamilyName;Weight=typeface.FontStyle.Weight;Width=(FontWidth)typeface.FontStyle.Width;Slant=(FontSlant)typeface.FontStyle.Slant;
            GlyphCount=typeface.GlyphCount;UnitsPerEm=typeface.UnitsPerEm;IsFixedPitch=typeface.IsFixedPitch;
            VariationAxes=Array.AsReadOnly(typeface.VariationDesignParameters.Select(a=>new FontVariationAxis(a.Tag.ToString(),a.Min,a.Default,a.Max,a.IsHidden)).ToArray());
            Variations=Array.AsReadOnly(typeface.VariationDesignPosition.Select(a=>new FontVariation(a.Axis.ToString(),a.Value)).ToArray());
            var tags=new SortedSet<string>(StringComparer.Ordinal);
            foreach(string table in new[]{"GSUB","GPOS"})
            {
                uint tag=Tag(table);if(typeface.GetTableSize(tag)<10)continue;
                var bytes=typeface.GetTableData(tag);if(bytes is null||bytes.Length<10)continue;
                int offset=BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(6));if(offset>bytes.Length-2)continue;
                int count=BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset));
                if(count>(bytes.Length-offset-2)/6)continue;
                for(int i=0;i<count;i++)tags.Add(Encoding.ASCII.GetString(bytes,offset+2+i*6,4));
            }
            if(typeface.GetTableSize(Tag("kern"))>0)tags.Add("kern");
            FeatureTags=Array.AsReadOnly(tags.ToArray());
        }
        catch{native=null;typeface.Dispose();throw;}
    }
    public string FamilyName {get;}
    public int Weight {get;}
    public FontWidth Width {get;}
    public FontSlant Slant {get;}
    public int GlyphCount {get;}
    public int UnitsPerEm {get;}
    public bool IsFixedPitch {get;}
    public bool IsDisposed {get{lock(gate)return disposed;}}
    public IReadOnlyList<FontVariationAxis> VariationAxes {get;}
    public IReadOnlyList<FontVariation> Variations {get;}
    public IReadOnlyList<string> FeatureTags {get;}
    internal void VerifyParagraphSupport()
    {
        if(Variations.Any(v=>VariationAxes.Any(a=>a.Tag==v.Tag&&v.Value!=a.Default)))
            throw new NotSupportedException("Non-default font variation coordinates require Font.Shape or DrawingContext's explicit-font shaping overload. The paragraph engine cannot measure these coordinates correctly.");
    }
    internal static uint Tag(string tag)
    {
        if(tag is null||tag.Length!=4||tag.Any(c=>c<32||c>126))throw new ArgumentException("Tags require exactly four printable ASCII characters.",nameof(tag));
        return ((uint)tag[0]<<24)|((uint)tag[1]<<16)|((uint)tag[2]<<8)|tag[3];
    }
    internal static void SizeCheck(float size) {if(!float.IsFinite(size)||size<=0)throw new ArgumentOutOfRangeException(nameof(size));}
    public static Font Load(string path,int faceIndex=0)
    {ArgumentException.ThrowIfNullOrEmpty(path);using var stream=File.OpenRead(path);return Load(stream,faceIndex);}
    /// <summary>Reads at most 64 MiB and leaves the input stream open.</summary>
    public static Font Load(Stream stream,int faceIndex=0)
    {ArgumentNullException.ThrowIfNull(stream);return FromBytes(Bitmap.ReadEncoded(stream,new(){MaximumEncodedBytes=64*1024*1024}),faceIndex);}
    public static Font FromBytes(ReadOnlySpan<byte> bytes,int faceIndex=0)
    {
        if(faceIndex<0)throw new ArgumentOutOfRangeException(nameof(faceIndex));
        if(bytes.Length==0||bytes.Length>64*1024*1024)throw new InvalidDataException("Font data must contain 1..64 MiB of encoded bytes.");
        using var data=SKData.CreateCopy(bytes);return new(SKTypeface.FromData(data,faceIndex)??throw new InvalidDataException("Unsupported or invalid font data/collection index."));
    }
    public static Font FromBase64(string base64,int faceIndex=0)
    {
        ArgumentNullException.ThrowIfNull(base64);if(base64.Length>90*1024*1024)throw new InvalidDataException("Encoded font exceeds the input limit.");
        return FromBytes(Convert.FromBase64String(base64),faceIndex);
    }
    /// <summary>Match a system family/style. Missing families use the platform's fallback face.</summary>
    public static Font Match(string? family=null,int weight=400,FontWidth width=FontWidth.Normal,FontSlant slant=FontSlant.Upright)
    {
        if(weight is <1 or >1000||!Enum.IsDefined(width)||!Enum.IsDefined(slant))throw new ArgumentOutOfRangeException(nameof(weight));
        return new(SKTypeface.FromFamilyName(family,new SKFontStyle(weight,(int)width,(SKFontStyleSlant)slant))??throw new InvalidOperationException("No matching system font is available."));
    }
    public static IReadOnlyList<string> GetSystemFamilies()=>Array.AsReadOnly(SKFontManager.Default.FontFamilies.Order(StringComparer.OrdinalIgnoreCase).ToArray());
    public Font WithVariations(IEnumerable<FontVariation> variations)
    {
        ArgumentNullException.ThrowIfNull(variations);using var lease=Acquire();
        var copy=variations.ToArray();if(copy.Length>64)throw new ArgumentException("At most 64 variation coordinates.",nameof(variations));
        var positions=Variations.ToDictionary(v=>v.Tag,v=>v.Value,StringComparer.Ordinal);var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var value in copy)
        {
            _=Tag(value.Tag);var axis=VariationAxes.FirstOrDefault(a=>a.Tag==value.Tag);
            if(axis.Tag is null||!seen.Add(value.Tag))throw new ArgumentException("Unknown or duplicate font variation axis.",nameof(variations));
            if(!float.IsFinite(value.Value)||value.Value<axis.Minimum||value.Value>axis.Maximum)throw new ArgumentOutOfRangeException(nameof(variations));positions[value.Tag]=value.Value;
        }
        var nativePositions=positions.Select(p=>new SKFontVariationPositionCoordinate{Axis=Tag(p.Key),Value=p.Value}).ToArray();
        return new(lease.Typeface.Clone(nativePositions)??throw new InvalidOperationException("The font variation could not be created."));
    }
    public bool ContainsGlyph(int codePoint)
    {if(!Rune.IsValid(codePoint))throw new ArgumentOutOfRangeException(nameof(codePoint));using var lease=Acquire();using var font=new SKFont(lease.Typeface);return font.ContainsGlyph(codePoint);}
    public bool ContainsGlyphs(string text)
    {ArgumentNullException.ThrowIfNull(text);using var lease=Acquire();using var font=new SKFont(lease.Typeface);return font.ContainsGlyphs(text);}
    public FontMetrics GetMetrics(float size=16)
    {SizeCheck(size);using var lease=Acquire();using var font=new SKFont(lease.Typeface,size);return Metrics(font);}
    internal static FontMetrics Metrics(SKFont font)
    {var m=font.Metrics;return new(-m.Ascent,m.Descent,m.Leading,m.CapHeight,m.XHeight);}
    public ShapedText Shape(string text,float fontSize=16,TextShapingOptions? options=null)=>new(text,this,fontSize,options);
    internal Lease Acquire()
    {lock(gate){ObjectDisposedException.ThrowIf(disposed,this);leases++;return new(this,native!);}}
    internal sealed class Lease(Font owner,SKTypeface typeface) : IDisposable
    {
        private Font? owner=owner;
        internal SKTypeface Typeface {get;}=typeface;
        public void Dispose(){Interlocked.Exchange(ref owner,null)?.Release();}
    }
    private void Release(){lock(gate){leases--;if(disposed&&leases==0){native?.Dispose();native=null;}}}
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;if(leases==0){native?.Dispose();native=null;}}}
}
