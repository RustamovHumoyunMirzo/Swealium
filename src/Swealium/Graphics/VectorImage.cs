using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SkiaSharp;
namespace Swealium.Graphics;

/// <summary>An owned, immutable vector recording loaded from SVG/SVGZ or recorded with Swealium drawing.</summary>
public sealed class VectorImage:IDisposable
{
    private SKPicture? picture;
    private Svg.Skia.SKSvg? owner;
    private VectorImage(SKPicture picture,Svg.Skia.SKSvg? owner=null)
    {
        this.picture=picture;this.owner=owner;
        var bounds=picture.CullRect;ViewBox=new(bounds.Left,bounds.Top,bounds.Width,bounds.Height);
        if(!float.IsFinite(bounds.Width)||!float.IsFinite(bounds.Height)||bounds.Width<=0||bounds.Height<=0)
            throw new InvalidDataException("Vector document must have finite positive dimensions.");
    }
    public Rect ViewBox { get; }
    public Size Size=>new(ViewBox.Width,ViewBox.Height);
    public bool IsDisposed=>picture is null;
    internal SKPicture NativePicture {get{ObjectDisposedException.ThrowIf(IsDisposed,this);return picture!;}}
    public static VectorImage Parse(string svg,ImageLoadOptions? options=null)
    {ArgumentNullException.ThrowIfNull(svg);using var stream=new MemoryStream(Encoding.UTF8.GetBytes(svg));return Load(stream,options);}
    public static VectorImage Load(string path,ImageLoadOptions? options=null)
    {ArgumentException.ThrowIfNullOrEmpty(path);using var stream=File.OpenRead(path);return Load(stream,options);}
    public static VectorImage Load(Stream stream,ImageLoadOptions? options=null)
    {
        options??=new();byte[] bytes=Bitmap.ReadEncoded(stream,options);
        if(bytes.Length>2&&bytes[0]==0x1f&&bytes[1]==0x8b)
        {using var input=new MemoryStream(bytes,false);using var gzip=new GZipStream(input,CompressionMode.Decompress);bytes=Bitmap.ReadEncoded(gzip,options);}
        try
        {
            // Validate XML before the renderer's parser, without DTD/entity or external resolution.
            using var input=new MemoryStream(bytes,false);
            using var reader=XmlReader.Create(input,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=options.MaximumEncodedBytes});
            var document=XDocument.Load(reader);
            if(document.Root?.Name.LocalName!="svg")throw new InvalidDataException("Expected an SVG root element.");
            byte[] clean=Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting));
            var owner=new Svg.Skia.SKSvg();
            try
            {
                using var source=new MemoryStream(clean,false);
                var parameters=new Svg.Model.SvgParameters(null,null,LoadOptions:new Svg.SvgDocumentLoadOptions
                {ProcessingMode=Svg.SvgProcessingMode.SecureStatic,ExternalResources=Svg.SvgExternalResourcePolicy.SameDocumentAndDataOnly});
                var picture=owner.Load(source,parameters)??throw new InvalidDataException("SVG document could not be rendered.");
                return new(picture,owner);
            }
            catch{owner.Dispose();throw;}
        }
        catch(Exception error)when(error is XmlException or ArgumentException or FormatException)
        {throw new InvalidDataException("Invalid SVG document.",error);}
    }
    public static VectorImage Create(Rect viewBox,Action<DrawingContext> draw)
    {
        ImageLayout.ValidateRect(viewBox);if(viewBox.Width<=0||viewBox.Height<=0)throw new ArgumentOutOfRangeException(nameof(viewBox));ArgumentNullException.ThrowIfNull(draw);
        using var recorder=new SKPictureRecorder();var canvas=recorder.BeginRecording(SKRect.Create(viewBox.X,viewBox.Y,viewBox.Width,viewBox.Height));
        var context=new SkiaDrawingContext(canvas);
        try{draw(context);context.End();return new(recorder.EndRecording());}finally{context.End();}
    }
    public Bitmap Rasterize(int width,int height,ImageStretch stretch=ImageStretch.Uniform)
    {
        _=NativePicture;if(!Enum.IsDefined(stretch))throw new ArgumentOutOfRangeException(nameof(stretch));
        var bitmap=new Bitmap(width,height);
        try{bitmap.Draw(context=>context.DrawVector(this,ImageLayout.Fit(Size,new(0,0,width,height),stretch,UI.Alignment.Center,UI.Alignment.Center)));return bitmap;}
        catch{bitmap.Dispose();throw;}
    }
    public void Dispose(){if(owner is not null)owner.Dispose();else picture?.Dispose();owner=null;picture=null;}
}
