using SkiaSharp;
using HB=HarfBuzzSharp;
namespace Swealium.Graphics;

/// <summary>Reusable, immutable shaped segment. Owns glyph blobs and a font lease; dispose after all consumers finish.</summary>
public sealed class ShapedText : IDisposable
{
    private Font.Lease? lease;
    private SKFont? nativeFont;
    private SKTextBlob? smoothed,aliased;
    private readonly ushort[] glyphIds;
    private readonly SKPoint[] positions;
    public ShapedText(string text,Font font,float fontSize=16,TextShapingOptions? options=null)
    {
        ArgumentNullException.ThrowIfNull(text);ArgumentNullException.ThrowIfNull(font);Font.SizeCheck(fontSize);
        if(text.Any(c=>c is '\n' or '\r' or '\t' or '\u2028' or '\u2029'))throw new ArgumentException("Shape one horizontal segment at a time; use TextLayout for paragraphs and tabs.",nameof(text));
        Text=text;FontSize=fontSize;Options=(options??new()).Snapshot(text);lease=font.Acquire();
        try
        {
            var typeface=lease.Typeface;
            nativeFont=new(typeface,fontSize){Hinting=(SKFontHinting)Options.Hinting,Subpixel=Options.SubpixelPositioning,LinearMetrics=Options.LinearMetrics};Metrics=Font.Metrics(nativeFont);
            using var tables=new TableSource(typeface);
            using var face=new HB.Face((_,tag)=>tables.Read((uint)tag));
            int units=typeface.UnitsPerEm>0?typeface.UnitsPerEm:1000;face.UnitsPerEm=units;
            using var shaper=new HB.Font(face);shaper.SetFunctionsOpenType();int scale=checked(units*64);shaper.SetScale(scale,scale);
            var variations=font.Variations.Select(v=>new HB.Variation{Tag=Font.Tag(v.Tag),Value=v.Value}).ToArray();if(variations.Length>0)shaper.SetVariations(variations);
            using var buffer=new HB.Buffer();buffer.AddUtf16(text);
            if(Options.Direction!=TextDirection.Automatic)buffer.Direction=Options.Direction==TextDirection.RightToLeft?HB.Direction.RightToLeft:HB.Direction.LeftToRight;
            if(Options.Language is {} language)buffer.Language=new HB.Language(language);
            if(Options.Script is {} script)buffer.Script=HB.Script.Parse(script);
            buffer.GuessSegmentProperties();Direction=buffer.Direction==HB.Direction.RightToLeft?TextDirection.RightToLeft:TextDirection.LeftToRight;
            var features=Options.Features.Select(f=>new HB.Feature(Font.Tag(f.Tag),f.Value,(uint)f.Start,f.End is {} end?(uint)end:uint.MaxValue)).ToArray();shaper.Shape(buffer,features);
            if(tables.Error is not null)throw new InvalidDataException("A font table could not be read during shaping.",tables.Error);
            var infos=buffer.GlyphInfos;var advances=buffer.GlyphPositions;var glyphs=new ShapedGlyph[infos.Length];glyphIds=new ushort[infos.Length];positions=new SKPoint[infos.Length];
            double x=0,y=0,factor=(double)fontSize/scale;
            for(int i=0;i<infos.Length;i++)
            {
                if(infos[i].Codepoint>ushort.MaxValue||infos[i].Cluster>text.Length)throw new InvalidDataException("The shaper returned an invalid glyph or source cluster.");
                float px=Finite(x+advances[i].XOffset*factor),py=Finite(y-advances[i].YOffset*factor);
                double dx=advances[i].XAdvance*factor,dy=-advances[i].YAdvance*factor;
                if(i+1<infos.Length&&infos[i].Cluster!=infos[i+1].Cluster)dx+=Options.LetterSpacing;
                glyphIds[i]=(ushort)infos[i].Codepoint;positions[i]=new(px,py);glyphs[i]=new(glyphIds[i],(int)infos[i].Cluster,new(px,py),new(Finite(dx),Finite(dy)));x+=dx;y+=dy;
            }
            Glyphs=Array.AsReadOnly(glyphs);HasMissingGlyphs=glyphIds.Contains((ushort)0);Advance=new(Finite(x),Finite(y));Size=new(Math.Abs(Advance.X),Metrics.LineHeight);
            if(glyphIds.Length>0){var bounds=Blob(true)!.Bounds;InkBounds=new(bounds.Left,bounds.Top,bounds.Width,bounds.Height);}
        }
        catch{Dispose();throw;}
    }
    private static float Finite(double value)
    {if(!double.IsFinite(value)||value>float.MaxValue||value< -float.MaxValue)throw new ArgumentOutOfRangeException(nameof(value),"Shaped geometry exceeds the drawable range.");return (float)value;}
    private static unsafe HB.Blob Table(SKTypeface face,uint tag)
    {
        if(face.GetTableSize(tag)==0)return HB.Blob.Empty;
        var bytes=face.GetTableData(tag);if(bytes is null||bytes.Length==0)return HB.Blob.Empty;
        fixed(byte* p=bytes)return new HB.Blob((nint)p,bytes.Length,HB.MemoryMode.Duplicate);
    }
    // Keep callback-owned blobs alive through shaping, then release them after the HB face/font.
    private sealed class TableSource(SKTypeface typeface):IDisposable
    {
        private readonly Dictionary<uint,HB.Blob> blobs=new();
        internal Exception? Error {get;private set;}
        internal HB.Blob Read(uint tag)
        {
            try
            {
                if(blobs.TryGetValue(tag,out var found))return found;
                if(typeface.GetTableSize(tag)==0)return HB.Blob.Empty;
                var blob=Table(typeface,tag);blobs.Add(tag,blob);return blob;
            }
            catch(Exception error){Error??=error;return HB.Blob.Empty;}
        }
        public void Dispose(){foreach(var blob in blobs.Values)blob.Dispose();blobs.Clear();}
    }
    public string Text {get;}
    public float FontSize {get;}
    public TextShapingOptions Options {get;}
    public TextDirection Direction {get;}
    public IReadOnlyList<ShapedGlyph> Glyphs {get;}
    public bool HasMissingGlyphs {get;}
    public Point Advance {get;}
    public Size Size {get;}
    public Rect InkBounds {get;}
    public FontMetrics Metrics {get;}
    public bool IsDisposed=>lease is null;
    internal SKTextBlob? Blob(bool antiAliasing)
    {
        ObjectDisposedException.ThrowIf(IsDisposed,this);if(glyphIds.Length==0)return null;
        ref var cached=ref (antiAliasing?ref smoothed:ref aliased);
        if(cached is null)
        {
            nativeFont!.Edging=antiAliasing?SKFontEdging.Antialias:SKFontEdging.Alias;
            using var builder=new SKTextBlobBuilder();builder.AddPositionedRun(glyphIds,nativeFont,positions);cached=builder.Build();
        }
        return cached;
    }
    public void Dispose(){smoothed?.Dispose();smoothed=null;aliased?.Dispose();aliased=null;nativeFont?.Dispose();nativeFont=null;lease?.Dispose();lease=null;}
}
