using SkiaSharp;
namespace Swealium.Graphics;

internal sealed partial class SkiaDrawingContext(SKCanvas canvas) : DrawingContext
{
    private readonly int initialSaveCount = canvas.SaveCount;
    private bool active = true;
    private DrawingDetail detail;
    public override DrawingDetail PolylineDetail {get{Verify();return detail;}set{Verify();if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));detail=value;}}
    private bool antiAliasing = SwealiumApplication.AntiAliasing;
    public override bool AntiAliasing { get { Verify(); return antiAliasing; } set { Verify(); antiAliasing = value; } }
    internal void End() { if (!active) return; canvas.RestoreToCount(initialSaveCount); active = false; }
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
    public override void DrawPoints(PointBuffer points,Color color,float size=1,PointShape shape=PointShape.Square,Rect? clip=null)
    {
        Verify();if(!Enum.IsDefined(shape))throw new ArgumentOutOfRangeException(nameof(shape));
        Batch(points,color,size,shape==PointShape.Round?PathLineCap.Round:PathLineCap.Square,SKPointMode.Points,false,clip);
    }
    public override void DrawLines(PointBuffer points,Color color,float width=1,PathLineCap cap=PathLineCap.Flat,Rect? clip=null)
    {
        Verify();ArgumentNullException.ThrowIfNull(points);if(points.Count%2!=0)throw new ArgumentException("Line batches require point pairs.",nameof(points));
        Batch(points,color,width,cap,SKPointMode.Lines,false,clip);
    }
    public override void DrawPolyline(PointBuffer points,Color color,float width=1,bool closed=false,PathLineCap cap=PathLineCap.Flat,Rect? clip=null)
        =>Batch(points,color,width,cap,SKPointMode.Polygon,closed,clip);
    private void Batch(PointBuffer points,Color color,float width,PathLineCap cap,SKPointMode mode,bool closed,Rect? clip)
    {
        Verify();ArgumentNullException.ThrowIfNull(points);
        if(!float.IsFinite(width)||width<0||!Enum.IsDefined(cap))throw new ArgumentOutOfRangeException(nameof(width));
        if(clip is {} area)ImageLayout.ValidateRect(area);
        if(points.Count==0||(mode!=SKPointMode.Points&&points.Count<2))return;
        using var paint=GradientPaint.Create(color,points.Bounds,AntiAliasing,width);
        paint.Style=SKPaintStyle.Stroke;paint.StrokeWidth=width;
        paint.StrokeCap=cap switch{PathLineCap.Round=>SKStrokeCap.Round,PathLineCap.Square=>SKStrokeCap.Square,_=>SKStrokeCap.Butt};
        int saved=canvas.Save();
        try
        {
            if(clip is {} bounds)canvas.ClipRect(ToRect(bounds));
            var geometry=points;
            if(mode==SKPointMode.Polygon&&!closed&&detail==DrawingDetail.Automatic&&points.IsXOrdered)
            {
                var localClip=canvas.LocalClipBounds;var matrix=canvas.TotalMatrix;
                float left=Math.Max(points.Bounds.X,localClip.Left-width),right=Math.Min(points.Bounds.X+points.Bounds.Width,localClip.Right+width);
                if(right<left)return;
                double pixels=(right-left)*Math.Sqrt((double)matrix.ScaleX*matrix.ScaleX+(double)matrix.SkewY*matrix.SkewY);
                int columns=(int)Math.Clamp(Math.Ceiling(pixels),1,32768);
                geometry=points.ForRendering(columns,new(left,0,Math.Max(0,right-left),0));
            }
            canvas.DrawPoints(mode,geometry.NativePoints,paint);
            if(closed&&points.Count>2)canvas.DrawLine(points.NativePoints[^1],points.NativePoints[0],paint);
        }
        finally{canvas.RestoreToCount(saved);}
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
    public override void DrawText(string text,Point baseline,TextStyle style){Verify();base.DrawText(text,baseline,style);}
    public override Size MeasureText(string text,TextStyle style,TextOptions? options=null,float maxWidth=float.PositiveInfinity,TextMarkup markup=TextMarkup.Plain)
    {Verify();return base.MeasureText(text,style,options,maxWidth,markup);}
    public override void DrawText(string text,Point baseline,Font font,Color color,float fontSize=16,TextShapingOptions? options=null)
    {Verify();base.DrawText(text,baseline,font,color,fontSize,options);}
    public override Size MeasureText(string text,Font font,float fontSize=16,TextShapingOptions? options=null)
    {Verify();return base.MeasureText(text,font,fontSize,options);}
    public override void DrawShapedText(ShapedText text,Point baseline,Color color,Rect? clip=null)
    {
        Verify();ArgumentNullException.ThrowIfNull(text);
        if(!float.IsFinite(baseline.X)||!float.IsFinite(baseline.Y))throw new ArgumentOutOfRangeException(nameof(baseline));
        if(clip is {} area)ImageLayout.ValidateRect(area);
        var blob=text.Blob(AntiAliasing);if(blob is null)return;
        int saved=canvas.Save();
        try
        {
            if(clip is {} bounds)canvas.ClipRect(ToRect(bounds));canvas.Translate(baseline.X,baseline.Y);
            using var paint=Paint(color,new(0,-text.Metrics.Ascent,Math.Max(text.Size.Width,.0001f),Math.Max(text.Size.Height,.0001f)));
            canvas.DrawText(blob,0,0,paint);
        }
        finally{canvas.RestoreToCount(saved);}
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

