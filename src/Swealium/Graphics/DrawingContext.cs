namespace Swealium.Graphics;

/// <summary>Frame-scoped drawing in logical coordinates. Do not retain a context after Draw returns.</summary>
public abstract class DrawingContext
{
    /// <summary>Scoped state must be disposed in reverse order before the callback returns.</summary>
    public virtual IDisposable PushClip(Rect bounds, CornerRadii radii = default) => throw new NotSupportedException();
    public virtual IDisposable PushTranslation(Point offset) => throw new NotSupportedException();
    public virtual IDisposable PushOpacity(float opacity) => throw new NotSupportedException();
    public virtual IDisposable PushBlur(Size radius, BlurEdgeMode edgeMode = BlurEdgeMode.Transparent) => throw new NotSupportedException();
    public virtual IDisposable PushProgressiveBlur(BlurGradient gradient, Rect bounds, BlurEdgeMode edgeMode = BlurEdgeMode.Clamp) => throw new NotSupportedException();
    public virtual IDisposable PushDropShadow(Point offset, Size radius, Color color, bool shadowOnly = false) => throw new NotSupportedException();
    public virtual IDisposable PushColorMatrix(ColorMatrix matrix) => throw new NotSupportedException();
    public virtual void DrawBackdropBlur(Rect bounds, CornerRadii radii, Size radius, BlurEdgeMode edgeMode = BlurEdgeMode.Clamp) => throw new NotSupportedException();
    public virtual void DrawBackdropBlur(Rect bounds, CornerRadii radii, BlurGradient gradient, BlurEdgeMode edgeMode = BlurEdgeMode.Clamp) => throw new NotSupportedException();
    /// <summary>Dense polyline detail. Exact by default; DrawingCanvas scopes its own policy.</summary>
    public virtual DrawingDetail PolylineDetail {get;set;}=DrawingDetail.Exact;
    /// <summary>Edge smoothing for subsequent operations in this context. Initialized from the application default.</summary>
    public virtual bool AntiAliasing { get; set; } = SwealiumApplication.AntiAliasing;
    public void FillRectangle(Rect bounds, Color color, float cornerRadius = 0) => FillRectangle(bounds, color, new CornerRadii(cornerRadius));
    public abstract void FillRectangle(Rect bounds, Color color, CornerRadii radii);
    public void StrokeRectangle(Rect bounds, Color color, float thickness = 1, float cornerRadius = 0) =>
        StrokeRectangle(bounds, color, new CornerRadii(cornerRadius), thickness);
    public abstract void StrokeRectangle(Rect bounds, Color color, CornerRadii radii, float thickness = 1);
    public abstract void FillEllipse(Rect bounds, Color color);
    public virtual void DrawPoints(PointBuffer points,Color color,float size=1,PointShape shape=PointShape.Square,Rect? clip=null)=>throw new NotSupportedException("This context does not implement point batches.");
    public virtual void DrawLines(PointBuffer points,Color color,float width=1,PathLineCap cap=PathLineCap.Flat,Rect? clip=null)=>throw new NotSupportedException("This context does not implement line batches.");
    public virtual void DrawPolyline(PointBuffer points,Color color,float width=1,bool closed=false,PathLineCap cap=PathLineCap.Flat,Rect? clip=null)=>throw new NotSupportedException("This context does not implement polyline batches.");
    public abstract void DrawLine(Point start, Point end, Color color, float thickness = 1);
    /// <summary>Draw shaped text at a baseline origin.</summary>
    public abstract void DrawText(string text, Point baseline, Color color, float fontSize = 16);
    public abstract Size MeasureText(string text, float fontSize = 16);
    /// <summary>Draw a shaped paragraph-style line at a baseline with loaded/system font selection.</summary>
    public virtual void DrawText(string text,Point baseline,TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);using var layout=new TextLayout(text,style,new(){Wrapping=TextWrapping.None});
        DrawTextLayout(layout,new(baseline.X,baseline.Y-layout.Baseline));
    }
    public virtual Size MeasureText(string text,TextStyle style,TextOptions? options=null,float maxWidth=float.PositiveInfinity,TextMarkup markup=TextMarkup.Plain)
    {ArgumentNullException.ThrowIfNull(style);using var layout=new TextLayout(text,style,options,maxWidth,markup);return layout.Size;}
    /// <summary>Draw one single-font script/direction segment with explicit OpenType shaping controls.</summary>
    public virtual void DrawText(string text,Point baseline,Font font,Color color,float fontSize=16,TextShapingOptions? options=null)
    {using var shaped=new ShapedText(text,font,fontSize,options);DrawShapedText(shaped,baseline,color);}
    public virtual Size MeasureText(string text,Font font,float fontSize=16,TextShapingOptions? options=null)
    {using var shaped=new ShapedText(text,font,fontSize,options);return shaped.Size;}
    public virtual void DrawShapedText(ShapedText text,Point baseline,Color color,Rect? clip=null)=>throw new NotSupportedException("This context does not implement shaped glyph segments.");
    public virtual void DrawVector(VectorImage image,Rect destination,float opacity=1,Rect? clip=null)=>throw new NotSupportedException("This drawing context does not implement vector images.");
    public virtual void FillPath(VectorPath path,Color color)=>throw new NotSupportedException("This drawing context does not implement vector paths.");
    public virtual void StrokePath(VectorPath path,Color color,float width=1,PathLineCap cap=PathLineCap.Flat,PathLineJoin join=PathLineJoin.Miter)=>throw new NotSupportedException("This drawing context does not implement vector paths.");
    public virtual void DrawBitmap(Bitmap bitmap,Rect destination,Rect? source=null,float opacity=1,BitmapInterpolation interpolation=BitmapInterpolation.Linear,Rect? clip=null)=>
        throw new NotSupportedException("This drawing context does not implement bitmaps.");
    public void DrawBitmap(Bitmap bitmap,Point origin,float opacity=1,BitmapInterpolation interpolation=BitmapInterpolation.Linear)=>
        DrawBitmap(bitmap,new Rect(origin.X,origin.Y,bitmap.Width,bitmap.Height),opacity:opacity,interpolation:interpolation);
    public virtual void DrawTextLayout(TextLayout layout, Point origin, TextSelection? selection = null, Color? selectionColor = null, Rect? clip = null) =>
        throw new NotSupportedException("This drawing context does not implement shaped paragraphs.");
    public void DrawText(string text, Rect bounds, TextStyle? style = null, TextOptions? options = null, TextMarkup markup = TextMarkup.Plain)
    {
        options ??= new();
        using var layout = new TextLayout(text, style, options, bounds.Width, markup);
        float y = options.VerticalAlignment switch { TextVerticalAlignment.Center => (bounds.Height-layout.Size.Height)/2, TextVerticalAlignment.Bottom => bounds.Height-layout.Size.Height, _ => 0 };
        DrawTextLayout(layout, new(bounds.X, bounds.Y + y), clip: bounds);
    }
}

