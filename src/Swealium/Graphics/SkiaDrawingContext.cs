using SkiaSharp;
namespace Swealium.Graphics;

internal sealed class SkiaDrawingContext(SKCanvas canvas) : DrawingContext
{
    private bool active = true;
    private bool antiAliasing = SwealiumApplication.AntiAliasing;
    public override bool AntiAliasing { get { Verify(); return antiAliasing; } set { Verify(); antiAliasing = value; } }
    internal void End() => active = false;
    private void Verify() => ObjectDisposedException.ThrowIf(!active, this);
    private static SKRect ToRect(Rect r) => SKRect.Create(r.X, r.Y, r.Width, r.Height);
    private SKPaint Paint(Color color,Rect bounds,float thickness=0)=>GradientPaint.Create(color,bounds,AntiAliasing,thickness);
    internal static SKRoundRect RoundRect(Rect bounds, CornerRadii radii)
    {
        var r = radii.Normalize(new(bounds.Width, bounds.Height));
        var rounded = new SKRoundRect();
        rounded.SetRectRadii(ToRect(bounds), [
            new SKPoint(r.TopLeft, r.TopLeft), new SKPoint(r.TopRight, r.TopRight),
            new SKPoint(r.BottomRight, r.BottomRight), new SKPoint(r.BottomLeft, r.BottomLeft)]);
        return rounded;
    }
    public override void FillRectangle(Rect bounds, Color color, CornerRadii radii)
    {
        Verify();
        using var paint = Paint(color, bounds);
        using var rounded = RoundRect(bounds, radii);
        canvas.DrawRoundRect(rounded, paint);
    }
    public override void StrokeRectangle(Rect bounds, Color color, CornerRadii radii, float thickness = 1)
    {
        Verify();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(thickness);
        using var paint = Paint(color, bounds, thickness);
        using var rounded = RoundRect(bounds, radii);
        canvas.DrawRoundRect(rounded, paint);
    }
    public override void FillEllipse(Rect bounds, Color color)
    {
        Verify();
        using var paint = Paint(color, bounds);
        canvas.DrawOval(ToRect(bounds), paint);
    }
    public override void DrawLine(Point start, Point end, Color color, float thickness = 1)
    {
        Verify();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(thickness);
        using var paint = Paint(color, new(Math.Min(start.X,end.X),Math.Min(start.Y,end.Y),Math.Max(thickness,Math.Abs(end.X-start.X)),Math.Max(thickness,Math.Abs(end.Y-start.Y))), thickness);
        canvas.DrawLine(start.X, start.Y, end.X, end.Y, paint);
    }
    public override void DrawText(string text, Point baseline, Color color, float fontSize = 16)
    {
        Verify();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fontSize);
        using var layout = new TextLayout(text, new() { Foreground = color, FontSize = fontSize }, new() { Wrapping = TextWrapping.None });
        layout.Paint(canvas, new(baseline.X, baseline.Y - layout.Baseline), null, null, AntiAliasing);
    }
    public override Size MeasureText(string text, float fontSize = 16)
    {
        Verify();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fontSize);
        return TextMetrics.Measure(text, fontSize);
    }
    public override void DrawBitmap(Bitmap bitmap,Rect destination,Rect? source=null,float opacity=1,BitmapInterpolation interpolation=BitmapInterpolation.Linear,Rect? clip=null)
    {
        Verify();ArgumentNullException.ThrowIfNull(bitmap);ImageLayout.ValidateRect(destination);ImageLayout.ValidateOpacity(opacity);
        if(!Enum.IsDefined(interpolation))throw new ArgumentOutOfRangeException(nameof(interpolation));
        Rect crop=source??new(0,0,bitmap.Width,bitmap.Height);ImageLayout.ValidateRect(crop);
        if(crop.X<0||crop.Y<0||crop.X+crop.Width>bitmap.Width||crop.Y+crop.Height>bitmap.Height)throw new ArgumentOutOfRangeException(nameof(source));
        var image=bitmap.NativeImage;
        if(destination.Width==0||destination.Height==0||crop.Width==0||crop.Height==0||opacity==0)return;
        var sampling=interpolation switch{BitmapInterpolation.Nearest=>new SKSamplingOptions(SKFilterMode.Nearest),BitmapInterpolation.Cubic=>new SKSamplingOptions(SKCubicResampler.Mitchell),_=>new SKSamplingOptions(SKFilterMode.Linear)};
        using var paint=new SKPaint{Color=SKColors.White.WithAlpha((byte)MathF.Round(opacity*255)),IsAntialias=AntiAliasing};
        int saved=canvas.Save();
        try{if(clip is {} bounds){ImageLayout.ValidateRect(bounds);canvas.ClipRect(ToRect(bounds));}canvas.DrawImage(image,ToRect(crop),ToRect(destination),sampling,paint);}
        finally{canvas.RestoreToCount(saved);}
    }
    public override void DrawVector(VectorImage image,Rect destination,float opacity=1,Rect? clip=null)
    {
        Verify();ArgumentNullException.ThrowIfNull(image);ImageLayout.ValidateRect(destination);ImageLayout.ValidateOpacity(opacity);var picture=image.NativePicture;
        if(destination.Width==0||destination.Height==0||opacity==0)return;
        int saved=canvas.Save();
        try
        {
            if(clip is {} bounds){ImageLayout.ValidateRect(bounds);canvas.ClipRect(ToRect(bounds));}
            canvas.ClipRect(ToRect(destination));canvas.Translate(destination.X,destination.Y);
            canvas.Scale(destination.Width/image.ViewBox.Width,destination.Height/image.ViewBox.Height);canvas.Translate(-image.ViewBox.X,-image.ViewBox.Y);
            using var paint=new SKPaint{Color=SKColors.White.WithAlpha((byte)MathF.Round(opacity*255))};canvas.DrawPicture(picture,paint);
        }
        finally{canvas.RestoreToCount(saved);}
    }
    public override void FillPath(VectorPath path,Color color)
    {Verify();ArgumentNullException.ThrowIfNull(path);using var snapshot=path.Snapshot();using var paint=Paint(color, new(snapshot.Bounds.Left,snapshot.Bounds.Top,snapshot.Bounds.Width,snapshot.Bounds.Height));canvas.DrawPath(snapshot,paint);}
    public override void StrokePath(VectorPath path,Color color,float width=1,PathLineCap cap=PathLineCap.Flat,PathLineJoin join=PathLineJoin.Miter)
    {
        Verify();ArgumentNullException.ThrowIfNull(path);if(!float.IsFinite(width)||width<=0||!Enum.IsDefined(cap)||!Enum.IsDefined(join))throw new ArgumentOutOfRangeException(nameof(width));
        using var snapshot=path.Snapshot();using var paint=Paint(color,new(snapshot.Bounds.Left,snapshot.Bounds.Top,snapshot.Bounds.Width,snapshot.Bounds.Height),width);
        paint.StrokeCap=cap switch{PathLineCap.Round=>SKStrokeCap.Round,PathLineCap.Square=>SKStrokeCap.Square,_=>SKStrokeCap.Butt};
        paint.StrokeJoin=join switch{PathLineJoin.Round=>SKStrokeJoin.Round,PathLineJoin.Bevel=>SKStrokeJoin.Bevel,_=>SKStrokeJoin.Miter};canvas.DrawPath(snapshot,paint);
    }
    public override void DrawTextLayout(TextLayout layout, Point origin, TextSelection? selection = null, Color? selectionColor = null, Rect? clip = null)
    {
        Verify(); ArgumentNullException.ThrowIfNull(layout);
        int save = canvas.Save();
        try { if (clip is { } bounds) canvas.ClipRect(ToRect(bounds)); layout.Paint(canvas, origin, selection, selectionColor, AntiAliasing); }
        finally { canvas.RestoreToCount(save); }
    }
}

