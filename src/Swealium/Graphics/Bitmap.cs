using System.Runtime.InteropServices;
using SkiaSharp;
namespace Swealium.Graphics;

public enum BitmapFormat { Png, Jpeg, WebP }
public enum BitmapInterpolation { Nearest, Linear, Cubic }
public sealed record ImageLoadOptions
{
    public int MaximumEncodedBytes { get; init; } = 64*1024*1024;
    public long MaximumPixels { get; init; } = 64L*1024*1024;
    internal void Validate()
    {if(MaximumEncodedBytes<=0||MaximumPixels<=0)throw new ArgumentOutOfRangeException(nameof(ImageLoadOptions));}
}

/// <summary>An owned RGBA raster resource. Widgets borrow it; dispose after detaching all consumers.</summary>
public sealed partial class Bitmap:IDisposable
{
    private SKBitmap? pixels;
    private SKImage? snapshot;
    private readonly int width,height;
    private bool drawing;
    public Bitmap(int width,int height,Color background=default)
    {
        ValidateSize(width,height,new ImageLoadOptions().MaximumPixels);
        this.width=width;this.height=height;
        pixels=new(new SKImageInfo(width,height,SKColorType.Rgba8888,SKAlphaType.Premul));
        if(pixels.GetPixels()==0){pixels.Dispose();pixels=null;throw new InvalidOperationException("Could not allocate bitmap pixels.");}
        pixels.Erase(TextLayout.Native(background));
    }
    private Bitmap(SKBitmap pixels) {this.pixels=pixels;width=pixels.Width;height=pixels.Height;}
    public int Width { get {Verify();return width;} }
    public int Height { get {Verify();return height;} }
    public Size Size=>new(Width,Height);
    public bool IsDisposed=>pixels is null;
    internal SKImage NativeImage {get {Verify();return snapshot??=SKImage.FromBitmap(pixels!);}}
    private void Verify()=>ObjectDisposedException.ThrowIf(IsDisposed,this);
    private void Invalidate(){snapshot?.Dispose();snapshot=null;pixels?.NotifyPixelsChanged();}
    public Color GetPixel(int x,int y)
    {ValidatePixel(x,y);var c=pixels!.GetPixel(x,y);return new(c.Red,c.Green,c.Blue,c.Alpha);}
    public void SetPixel(int x,int y,Color color) {ValidatePixel(x,y);pixels!.SetPixel(x,y,TextLayout.Native(color));Invalidate();}
    private void ValidatePixel(int x,int y) {Verify();if(x<0||y<0||x>=width||y>=height)throw new ArgumentOutOfRangeException(nameof(x));}
    public void Draw(Action<DrawingContext> draw)
    {
        Verify();ArgumentNullException.ThrowIfNull(draw);
        if(drawing)throw new InvalidOperationException("Bitmap drawing cannot be reentered.");
        Invalidate();
        using var surface=SKSurface.Create(pixels!.Info,pixels.GetPixels(),pixels.RowBytes)??throw new InvalidOperationException("Could not create bitmap drawing surface.");
        var context=new SkiaDrawingContext(surface.Canvas);drawing=true;
        try{draw(context);surface.Canvas.Flush();}finally{context.End();drawing=false;Invalidate();}
    }
    public Bitmap Clone() {Verify();return new(pixels!.Copy());}
    public byte[] CopyPixels()
    {
        Verify();using var copy=new SKBitmap(new SKImageInfo(width,height,SKColorType.Rgba8888,SKAlphaType.Unpremul));
        if(!NativeImage.ReadPixels(copy.Info,copy.GetPixels(),copy.RowBytes,0,0))throw new InvalidOperationException("Could not read bitmap pixels.");
        byte[] data=new byte[checked(width*height*4)];Marshal.Copy(copy.GetPixels(),data,0,data.Length);return data;
    }
    public static Bitmap FromPixels(int width,int height,ReadOnlySpan<byte> rgba)
        =>FromPixelsCore(width,height,rgba,new ImageLoadOptions().MaximumPixels);
    internal static Bitmap FromPixelsCore(int width,int height,ReadOnlySpan<byte> rgba,long maximum)
    {
        ValidateSize(width,height,maximum);
        if(rgba.Length!=checked(width*height*4))throw new ArgumentException("Expected tightly packed unpremultiplied RGBA pixels.",nameof(rgba));
        var result=new Bitmap(new SKBitmap(new SKImageInfo(width,height,SKColorType.Rgba8888,SKAlphaType.Premul)));
        using var unpremul=new SKBitmap(new SKImageInfo(width,height,SKColorType.Rgba8888,SKAlphaType.Unpremul));
        Marshal.Copy(rgba.ToArray(),0,unpremul.GetPixels(),rgba.Length);
        using var image=SKImage.FromBitmap(unpremul);
        if(!image.ReadPixels(result.pixels!.Info,result.pixels.GetPixels(),result.pixels.RowBytes,0,0)){result.Dispose();throw new InvalidOperationException("Could not convert pixels.");}
        return result;
    }
    public static Bitmap Load(string path,ImageLoadOptions? options=null)
    {ArgumentException.ThrowIfNullOrEmpty(path);using var stream=File.OpenRead(path);return Load(stream,options);}
    public static Bitmap Load(Stream stream,ImageLoadOptions? options=null)=>Decode(ReadEncoded(stream,options??new()),options);
    public static Bitmap Decode(ReadOnlySpan<byte> encoded,ImageLoadOptions? options=null)
    {
        options??=new();options.Validate();
        if(encoded.Length==0||encoded.Length>options.MaximumEncodedBytes)throw new InvalidDataException("Empty image or encoded image exceeds the configured limit.");
        byte[] bytes=encoded.ToArray();
        if(bytes.Length>=3&&bytes[0]=='P'&&bytes[1] is >=(byte)'1' and <=(byte)'6'&&NetpbmDecoder.White(bytes[2]))return NetpbmDecoder.Decode(bytes,options);
        using var data=SKData.CreateCopy(bytes);using var codec=SKCodec.Create(data);
        if(codec is not null)
        {
            ValidateSize(codec.Info.Width,codec.Info.Height,options.MaximumPixels);
            var decoded=new SKBitmap(new SKImageInfo(codec.Info.Width,codec.Info.Height,SKColorType.Rgba8888,SKAlphaType.Premul));
            var result=codec.GetPixels(decoded.Info,decoded.GetPixels());
            if(result!=SKCodecResult.Success){decoded.Dispose();throw new InvalidDataException($"Could not decode image: {result}.");}
            return Orient(decoded,codec.EncodedOrigin);
        }
        // The managed fallback adds TGA, PSD, HDR, PIC, and binary PNM support.
        try
        {
            using var header=new MemoryStream(bytes,false);var info=StbImageSharp.ImageInfo.FromStream(header);
            if(info is null)throw new InvalidDataException("Unsupported or invalid image.");
            ValidateSize(info.Value.Width,info.Value.Height,options.MaximumPixels);
            var decoded=StbImageSharp.ImageResult.FromMemory(bytes,StbImageSharp.ColorComponents.RedGreenBlueAlpha);
            return FromPixelsCore(decoded.Width,decoded.Height,decoded.Data,options.MaximumPixels);
        }
        catch(Exception error) when(error is not InvalidDataException && error is not OutOfMemoryException)
        {throw new InvalidDataException("Unsupported or invalid image.",error);}
    }
    private static Bitmap Orient(SKBitmap source,SKEncodedOrigin origin)
    {
        if(origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)return new(source);
        bool swap=origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var target=new Bitmap(new SKBitmap(new SKImageInfo(swap?source.Height:source.Width,swap?source.Width:source.Height,SKColorType.Rgba8888,SKAlphaType.Premul)));
        try
        {
            using var canvas=new SKCanvas(target.pixels!);using var image=SKImage.FromBitmap(source);
            switch(origin)
            {
                case SKEncodedOrigin.TopRight:canvas.Translate(source.Width,0);canvas.Scale(-1,1);break;
                case SKEncodedOrigin.BottomRight:canvas.Translate(source.Width,source.Height);canvas.RotateDegrees(180);break;
                case SKEncodedOrigin.BottomLeft:canvas.Translate(0,source.Height);canvas.Scale(1,-1);break;
                case SKEncodedOrigin.LeftTop:canvas.RotateDegrees(90);canvas.Scale(1,-1);break;
                case SKEncodedOrigin.RightTop:canvas.Translate(source.Height,0);canvas.RotateDegrees(90);break;
                case SKEncodedOrigin.RightBottom:canvas.Translate(source.Height,source.Width);canvas.RotateDegrees(90);canvas.Scale(-1,1);break;
                case SKEncodedOrigin.LeftBottom:canvas.Translate(0,source.Width);canvas.RotateDegrees(-90);break;
            }
            canvas.DrawImage(image,0,0,new SKSamplingOptions(SKFilterMode.Nearest));canvas.Flush();return target;
        }
        catch{target.Dispose();throw;}finally{source.Dispose();}
    }
    public byte[] Encode(BitmapFormat format=BitmapFormat.Png,int quality=90)
    {
        Verify();if(!Enum.IsDefined(format)||quality<0||quality>100)throw new ArgumentOutOfRangeException(nameof(format));
        using var data=NativeImage.Encode(format switch{BitmapFormat.Jpeg=>SKEncodedImageFormat.Jpeg,BitmapFormat.WebP=>SKEncodedImageFormat.Webp,_=>SKEncodedImageFormat.Png},quality)
            ??throw new NotSupportedException("Image encoder is unavailable.");
        return data.ToArray();
    }
    public void Save(string path,BitmapFormat format=BitmapFormat.Png,int quality=90)=>File.WriteAllBytes(path,Encode(format,quality));
    public void Save(Stream stream,BitmapFormat format=BitmapFormat.Png,int quality=90)
    {ArgumentNullException.ThrowIfNull(stream);stream.Write(Encode(format,quality));}
    internal static byte[] ReadEncoded(Stream stream,ImageLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);options.Validate();using var output=new MemoryStream();byte[] buffer=new byte[8192];int count;
        while((count=stream.Read(buffer))>0){if(output.Length+count>options.MaximumEncodedBytes)throw new InvalidDataException("Encoded image exceeds the configured limit.");output.Write(buffer,0,count);}
        return output.ToArray();
    }
    internal static void ValidateSize(int width,int height,long maximum)
    {if(width<=0||height<=0||(long)width*height>maximum||(long)width*height>int.MaxValue/4)throw new InvalidDataException("Image dimensions are invalid or exceed the pixel limit.");}
    public void Dispose(){if(drawing)throw new InvalidOperationException("Cannot dispose a bitmap during its drawing callback.");Invalidate();pixels?.Dispose();pixels=null;}
}
