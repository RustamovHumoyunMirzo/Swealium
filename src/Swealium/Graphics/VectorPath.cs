using SkiaSharp;
namespace Swealium.Graphics;

public enum PathFillRule { Winding, EvenOdd }
public enum PathLineCap { Flat, Round, Square }
public enum PathLineJoin { Miter, Round, Bevel }

/// <summary>An owned editable path builder. Drawing takes a snapshot of the current path.</summary>
public sealed class VectorPath:IDisposable
{
    private SKPathBuilder? builder=new();
    private PathFillRule fillRule;
    public PathFillRule FillRule {get=>fillRule;set{Verify();if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));fillRule=value;}}
    public bool IsDisposed=>builder is null;
    private void Verify()=>ObjectDisposedException.ThrowIf(IsDisposed,this);
    private static void Validate(Point p){if(!float.IsFinite(p.X)||!float.IsFinite(p.Y))throw new ArgumentOutOfRangeException(nameof(p));}
    public VectorPath MoveTo(Point point){Verify();Validate(point);builder!.MoveTo(point.X,point.Y);return this;}
    public VectorPath LineTo(Point point){Verify();Validate(point);builder!.LineTo(point.X,point.Y);return this;}
    public VectorPath QuadraticTo(Point control,Point end){Verify();Validate(control);Validate(end);builder!.QuadTo(control.X,control.Y,end.X,end.Y);return this;}
    public VectorPath CubicTo(Point first,Point second,Point end){Verify();Validate(first);Validate(second);Validate(end);builder!.CubicTo(first.X,first.Y,second.X,second.Y,end.X,end.Y);return this;}
    public VectorPath ArcTo(Rect oval,float startDegrees,float sweepDegrees,bool startNewFigure=false)
    {Verify();ImageLayout.ValidateRect(oval);if(!float.IsFinite(startDegrees)||!float.IsFinite(sweepDegrees))throw new ArgumentOutOfRangeException(nameof(startDegrees));builder!.ArcTo(SKRect.Create(oval.X,oval.Y,oval.Width,oval.Height),startDegrees,sweepDegrees,startNewFigure);return this;}
    public VectorPath Close(){Verify();builder!.Close();return this;}
    public VectorPath AddRectangle(Rect rect){Verify();ImageLayout.ValidateRect(rect);builder!.AddRect(SKRect.Create(rect.X,rect.Y,rect.Width,rect.Height));return this;}
    public VectorPath AddEllipse(Rect rect){Verify();ImageLayout.ValidateRect(rect);builder!.AddOval(SKRect.Create(rect.X,rect.Y,rect.Width,rect.Height));return this;}
    public VectorPath Clear(){Verify();builder!.Reset();return this;}
    internal SKPath Snapshot(){Verify();using var source=builder!.Snapshot();using var copy=new SKPathBuilder(source){FillType=FillRule==PathFillRule.EvenOdd?SKPathFillType.EvenOdd:SKPathFillType.Winding};return copy.Detach();}
    public Rect Bounds {get{using var path=Snapshot();var r=path.Bounds;return new(r.Left,r.Top,r.Width,r.Height);}}
    public bool Contains(Point point){Validate(point);using var path=Snapshot();return path.Contains(point.X,point.Y);}
    public static VectorPath Parse(string svgPathData)
    {
        ArgumentNullException.ThrowIfNull(svgPathData);using var path=SKPath.ParseSvgPathData(svgPathData)??throw new FormatException("Invalid SVG path data.");
        var result=new VectorPath();result.builder!.AddPath(path);return result;
    }
    public void Dispose(){builder?.Dispose();builder=null;}
}
