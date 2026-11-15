namespace Swealium.Graphics;

/// <summary>Frame-scoped drawing in logical coordinates. Do not retain a context after Draw returns.</summary>
public abstract class DrawingContext
{
    public void FillRectangle(Rect bounds, Color color, float cornerRadius = 0) => FillRectangle(bounds, color, new CornerRadii(cornerRadius));
    public abstract void FillRectangle(Rect bounds, Color color, CornerRadii radii);
    public void StrokeRectangle(Rect bounds, Color color, float thickness = 1, float cornerRadius = 0) =>
        StrokeRectangle(bounds, color, new CornerRadii(cornerRadius), thickness);
    public abstract void StrokeRectangle(Rect bounds, Color color, CornerRadii radii, float thickness = 1);
    public abstract void FillEllipse(Rect bounds, Color color);
    public abstract void DrawLine(Point start, Point end, Color color, float thickness = 1);
    /// <summary>Draw shaped text at a baseline origin.</summary>
    public abstract void DrawText(string text, Point baseline, Color color, float fontSize = 16);
    public abstract Size MeasureText(string text, float fontSize = 16);
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

