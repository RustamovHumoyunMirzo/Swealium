using SkiaSharp;
using Swealium.UI;
namespace Swealium.Graphics;

internal static class WidgetOutlineRenderer
{
    internal static void Draw(SKCanvas canvas,Widget widget)
    {
        var outline=widget.Outline;
        if(outline.Width==0||outline.Color.A==0||outline.Style==StrokeStyle.Hidden||widget.ActualWidth<=0||widget.ActualHeight<=0)return;
        double expansion=(double)outline.Offset+outline.Width;
        double width=widget.ActualWidth+2*expansion,height=widget.ActualHeight+2*expansion;
        if(width<=0||height<=0)return;
        if(width>float.MaxValue||height>float.MaxValue||Math.Abs(expansion)>float.MaxValue)
            throw new InvalidOperationException("The resolved outline exceeds the drawable coordinate range.");
        var radius=widget.Radii.Normalize(new(widget.ActualWidth,widget.ActualHeight));
        float Grow(float value)=>value==0?0:(float)Math.Max(0,value+expansion);
        var bounds=new Rect(-(float)expansion,-(float)expansion,(float)width,(float)height);
        _=widget.ToWindow(bounds);
        WidgetStrokeRenderer.Draw(canvas,new Stroke(outline.Width,outline.Color,outline.Style),
            bounds,
            new(Grow(radius.TopLeft),Grow(radius.TopRight),Grow(radius.BottomRight),Grow(radius.BottomLeft)));
    }
}
