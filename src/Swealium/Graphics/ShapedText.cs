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
            using var fontData=OpenFontData(typeface,out int collectionIndex);
            using var face=new HB.Face(fontData,collectionIndex);
            int units=typeface.UnitsPerEm>0?typeface.UnitsPerEm:1000;face.UnitsPerEm=units;
            using var shaper=new HB.Font(face);shaper.SetFunctionsOpenType();int scale=checked(units*64);shaper.SetScale(scale,scale);
            var variations=font.Variations.Select(v=>new HB.Variation{Tag=Font.Tag(v.Tag),Value=v.Value}).ToArray();if(variations.Length>0)shaper.SetVariations(variations);
            using var buffer=new HB.Buffer();buffer.AddUtf16(text);
            if(Options.Direction!=TextDirection.Automatic)buffer.Direction=Options.Direction==TextDirection.RightToLeft?HB.Direction.RightToLeft:HB.Direction.LeftToRight;
            if(Options.Language is {} language)buffer.Language=new HB.Language(language);
            if(Options.Script is {} script)buffer.Script=HB.Script.Parse(script);
            buffer.GuessSegmentProperties();Direction=buffer.Direction==HB.Direction.RightToLeft?TextDirection.RightToLeft:TextDirection.LeftToRight;
            var features=Options.Features.Select(f=>new HB.Feature(Font.Tag(f.Tag),f.Value,(uint)f.Start,f.End is {} end?(uint)end:uint.MaxValue)).ToArray();shaper.Shape(buffer,features);
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
    private static HB.Blob OpenFontData(SKTypeface typeface,out int collectionIndex)
    {
        // Own one complete font-data blob, rather than transferring callback-owned
        // table handles between two native reference-counting systems. HarfBuzz's
        // face retains this blob; disposal unwinds face -> blob -> backing stream/data.
        var stream=typeface.OpenStream(out collectionIndex)??throw new InvalidDataException("The font cannot provide its encoded data.");
        try
        {
            nint memory=stream.GetMemoryBase();
            if(memory!=0)return new HB.Blob(memory,stream.Length,HB.MemoryMode.ReadOnly,stream.Dispose);
            var data=SKData.Create(stream)??throw new InvalidDataException("The font stream cannot be read.");
            stream.Dispose();
            try{return new HB.Blob(data.Data,checked((int)data.Size),HB.MemoryMode.ReadOnly,data.Dispose);}
            catch{data.Dispose();throw;}
        }
        catch{stream.Dispose();throw;}
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
