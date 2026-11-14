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
    /// <summary>Draw single-line text at a baseline origin. Complex text shaping is not yet supported.</summary>
    public abstract void DrawText(string text, Point baseline, Color color, float fontSize = 16);
    public abstract Size MeasureText(string text, float fontSize = 16);
}

