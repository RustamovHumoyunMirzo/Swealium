using SkiaSharp;
namespace Swealium.Graphics;

public enum BitmapRotation { None, Clockwise90, Clockwise180, Clockwise270 }
[Flags]
public enum BitmapFlip { None=0, Horizontal=1, Vertical=2, Both=3 }

public sealed partial class Bitmap
{
    public static Bitmap FromBase64(string base64,ImageLoadOptions? options=null)
    {
        ArgumentNullException.ThrowIfNull(base64);options??=new();options.Validate();
        if(base64.StartsWith("data:",StringComparison.OrdinalIgnoreCase))
        {
            int comma=base64.IndexOf(',');
            if(comma<0||!base64.StartsWith("data:image/",StringComparison.OrdinalIgnoreCase)||!base64[..comma].EndsWith(";base64",StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Expected a base64 image data URI.");
            base64=base64[(comma+1)..];
        }
        long length=base64.Count(c=>!char.IsWhiteSpace(c));
        if(length>((long)options.MaximumEncodedBytes+2)/3*4)throw new InvalidDataException("Base64 image exceeds the configured encoded-byte limit.");
        try{return Decode(Convert.FromBase64String(base64),options);}
        catch(FormatException error){throw new InvalidDataException("Invalid image base64.",error);}
    }
    public string ToBase64(BitmapFormat format=BitmapFormat.Png,int quality=90)=>Convert.ToBase64String(Encode(format,quality));
    public string ToDataUri(BitmapFormat format=BitmapFormat.Png,int quality=90)
    {string encoded=ToBase64(format,quality);return $"data:image/{(format==BitmapFormat.Jpeg?"jpeg":format==BitmapFormat.WebP?"webp":"png")};base64,{encoded}";}
    public void Clear(Color color=default){Verify();pixels!.Erase(TextLayout.Native(color));Invalidate();}
    public Bitmap Resize(int width,int height,BitmapInterpolation interpolation=BitmapInterpolation.Linear)
    {
        Verify();if(!Enum.IsDefined(interpolation))throw new ArgumentOutOfRangeException(nameof(interpolation));var result=new Bitmap(width,height);
        try{result.Draw(c=>c.DrawBitmap(this,new Rect(0,0,width,height),interpolation:interpolation));return result;}catch{result.Dispose();throw;}
    }
    public Bitmap Crop(int x,int y,int width,int height)
    {
        Verify();if(x<0||y<0||width<=0||height<=0||(long)x+width>this.width||(long)y+height>this.height)throw new ArgumentOutOfRangeException(nameof(x));
        var result=new Bitmap(width,height);
        try{result.Draw(c=>c.DrawBitmap(this,new Rect(0,0,width,height),new Rect(x,y,width,height),interpolation:BitmapInterpolation.Nearest));return result;}catch{result.Dispose();throw;}
    }
    public Bitmap Rotate(BitmapRotation rotation)
    {
        Verify();if(!Enum.IsDefined(rotation))throw new ArgumentOutOfRangeException(nameof(rotation));
        bool swap=rotation is BitmapRotation.Clockwise90 or BitmapRotation.Clockwise270;var result=new Bitmap(swap?height:width,swap?width:height);
        try
        {
            using var canvas=new SKCanvas(result.pixels!);
            switch(rotation)
            {
                case BitmapRotation.Clockwise90:canvas.Translate(height,0);canvas.RotateDegrees(90);break;
                case BitmapRotation.Clockwise180:canvas.Translate(width,height);canvas.RotateDegrees(180);break;
                case BitmapRotation.Clockwise270:canvas.Translate(0,width);canvas.RotateDegrees(270);break;
            }
            canvas.DrawImage(NativeImage,0,0,new SKSamplingOptions(SKFilterMode.Nearest));canvas.Flush();result.Invalidate();return result;
        }
        catch{result.Dispose();throw;}
    }
    public Bitmap Flip(BitmapFlip flip)
    {
        Verify();if((flip&~BitmapFlip.Both)!=0)throw new ArgumentOutOfRangeException(nameof(flip));var result=new Bitmap(width,height);
        try
        {
            using var canvas=new SKCanvas(result.pixels!);bool horizontal=flip.HasFlag(BitmapFlip.Horizontal),vertical=flip.HasFlag(BitmapFlip.Vertical);
            canvas.Translate(horizontal?width:0,vertical?height:0);canvas.Scale(horizontal?-1:1,vertical?-1:1);
            canvas.DrawImage(NativeImage,0,0,new SKSamplingOptions(SKFilterMode.Nearest));canvas.Flush();result.Invalidate();return result;
        }
        catch{result.Dispose();throw;}
    }
    public Bitmap WithOpacity(float opacity)
    {
        Verify();ImageLayout.ValidateOpacity(opacity);var result=new Bitmap(width,height);
        try{result.Draw(c=>c.DrawBitmap(this,new Rect(0,0,width,height),opacity:opacity));return result;}catch{result.Dispose();throw;}
    }
}
