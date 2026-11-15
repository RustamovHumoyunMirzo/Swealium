namespace Swealium.Graphics;

public enum GradientKind { Linear, Radial, Elliptical, Sweep, TwoPointConical, Diamond, Square }
public enum GradientSpread { Pad, Repeat, Reflect, Decal }
public enum GradientUnits { Relative, Absolute }
/// <summary>A solid color at a normalized gradient offset.</summary>
public readonly record struct GradientStop(float Offset, Color Color);

/// <summary>Immutable, reusable gradient description. No disposal or backend resources required.</summary>
public sealed class Gradient
{
    public GradientKind Kind { get; }
    public IReadOnlyList<GradientStop> Stops { get; }
    public GradientSpread Spread { get; }
    public GradientUnits Units { get; }
    public Point Start { get; }
    public Point End { get; }
    public Size Radius { get; }
    public float StartRadius { get; }
    public float StartAngle { get; }
    public float EndAngle { get; }
    private Gradient(GradientKind kind,IEnumerable<GradientStop> stops,GradientSpread spread,GradientUnits units,
        Point start,Point end,Size radius,float startRadius=0,float startAngle=0,float endAngle=360)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if(!Enum.IsDefined(spread)||!Enum.IsDefined(units))throw new ArgumentOutOfRangeException(nameof(spread));
        PointCheck(start);PointCheck(end);
        if(!float.IsFinite(radius.Width)||!float.IsFinite(radius.Height)||radius.Width<=0||radius.Height<=0||!float.IsFinite(startRadius)||startRadius<0)
            throw new ArgumentOutOfRangeException(nameof(radius));
        if(!float.IsFinite(startAngle)||!float.IsFinite(endAngle)||endAngle<=startAngle||endAngle-startAngle>360)
            throw new ArgumentOutOfRangeException(nameof(endAngle));
        var copy=stops.ToArray();
        if(copy.Length<2||copy.Length>4096)throw new ArgumentException("Supply between 2 and 4096 gradient stops.",nameof(stops));
        float previous=0;
        foreach(var stop in copy)
        {
            if(!float.IsFinite(stop.Offset)||stop.Offset<previous||stop.Offset>1||stop.Color.IsGradient)
                throw new ArgumentException("Stops require ordered offsets in [0,1] and solid colors.",nameof(stops));
            previous=stop.Offset;
        }
        if(kind==GradientKind.Linear&&start==end)throw new ArgumentException("Linear gradient endpoints must differ.");
        if(kind==GradientKind.TwoPointConical&&start==end&&startRadius==radius.Width)throw new ArgumentException("Conical circles must differ.");
        Kind=kind;Stops=Array.AsReadOnly(copy);Spread=spread;Units=units;Start=start;End=end;Radius=radius;StartRadius=startRadius;StartAngle=startAngle;EndAngle=endAngle;
    }
    private static void PointCheck(Point point)
    {if(!float.IsFinite(point.X)||!float.IsFinite(point.Y))throw new ArgumentOutOfRangeException(nameof(point));}
    public static Gradient Linear(Point start,Point end,IEnumerable<GradientStop> stops,GradientSpread spread=GradientSpread.Pad,GradientUnits units=GradientUnits.Relative)
        =>new(GradientKind.Linear,stops,spread,units,start,end,new(1,1));
    public static Gradient Horizontal(params Color[] colors)=>Linear(new(0,0),new(1,0),Even(colors));
    public static Gradient Vertical(params Color[] colors)=>Linear(new(0,0),new(0,1),Even(colors));
    public static Gradient Diagonal(params Color[] colors)=>Linear(new(0,0),new(1,1),Even(colors));
    public static Gradient Radial(Point center,float radius,IEnumerable<GradientStop> stops,GradientSpread spread=GradientSpread.Pad,GradientUnits units=GradientUnits.Relative)
        =>new(GradientKind.Radial,stops,spread,units,center,center,new(radius,radius));
    public static Gradient Elliptical(Point center,Size radius,IEnumerable<GradientStop> stops,GradientSpread spread=GradientSpread.Pad,GradientUnits units=GradientUnits.Relative)
        =>new(GradientKind.Elliptical,stops,spread,units,center,center,radius);
    public static Gradient Sweep(Point center,IEnumerable<GradientStop> stops,float startAngle=0,float endAngle=360,GradientSpread spread=GradientSpread.Pad,GradientUnits units=GradientUnits.Relative)
        =>new(GradientKind.Sweep,stops,spread,units,center,center,new(1,1),startAngle:startAngle,endAngle:endAngle);
    public static Gradient TwoPointConical(Point start,float startRadius,Point end,float endRadius,IEnumerable<GradientStop> stops,GradientSpread spread=GradientSpread.Pad,GradientUnits units=GradientUnits.Relative)
        =>new(GradientKind.TwoPointConical,stops,spread,units,start,end,new(endRadius,endRadius),startRadius);
    public static Gradient Diamond(Point center,Size radius,IEnumerable<GradientStop> stops,GradientSpread spread=GradientSpread.Pad,GradientUnits units=GradientUnits.Relative)
        =>new(GradientKind.Diamond,stops,spread,units,center,center,radius);
    public static Gradient Square(Point center,Size radius,IEnumerable<GradientStop> stops,GradientSpread spread=GradientSpread.Pad,GradientUnits units=GradientUnits.Relative)
        =>new(GradientKind.Square,stops,spread,units,center,center,radius);
    private static IEnumerable<GradientStop> Even(Color[] colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if(colors.Length<2)throw new ArgumentException("Supply at least two colors.",nameof(colors));
        return colors.Select((color,index)=>new GradientStop((float)index/(colors.Length-1),color));
    }
}
