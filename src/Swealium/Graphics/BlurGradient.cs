namespace Swealium.Graphics;

/// <summary>Sampling outside the source of a blur.</summary>
public enum BlurEdgeMode { Transparent, Clamp, Repeat, Reflect }
public readonly record struct BlurStop(float Offset, float Radius);

/// <summary>Immutable spatial blur-radius ramp. Radius is Gaussian sigma in logical pixels.</summary>
public sealed class BlurGradient
{
    public IReadOnlyList<BlurStop> Stops { get; }
    public GradientKind Kind { get; }
    public GradientSpread Spread { get; }
    public GradientUnits Units { get; }
    public Point Start { get; }
    public Point End { get; }
    public Size Radius { get; }
    /// <summary>Number of Gaussian levels blended spatially; 2..32. Higher values cost more.</summary>
    public int Quality { get; }
    internal float Maximum { get; }
    private BlurGradient(GradientKind kind, Point start, Point end, Size radius, IEnumerable<BlurStop> stops,
        GradientSpread spread, GradientUnits units, int quality)
    {
        ArgumentNullException.ThrowIfNull(stops);
        var copy = stops.ToArray();
        if (copy.Length < 2 || copy.Length > 256) throw new ArgumentException("Supply 2..256 blur stops.", nameof(stops));
        float previous = 0;
        foreach (var stop in copy)
        {
            if (!float.IsFinite(stop.Offset) || stop.Offset < previous || stop.Offset > 1) throw new ArgumentException("Offsets must be ordered in [0,1].", nameof(stops));
            ValidateRadius(stop.Radius); previous = stop.Offset;
        }
        if (quality < 2 || quality > 32) throw new ArgumentOutOfRangeException(nameof(quality));
        Kind = kind; Start = start; End = end; Radius = radius; Spread = spread; Units = units; Quality = quality;
        Stops = Array.AsReadOnly(copy); Maximum = copy.Max(s => s.Radius);
        _ = MakeGradient(copy.Select(s => new GradientStop(s.Offset, Color.White))); // Validate geometry/spread/units.
    }
    internal static void ValidateRadius(float radius)
    { if (!float.IsFinite(radius) || radius < 0 || radius > 512) throw new ArgumentOutOfRangeException(nameof(radius), "Blur sigma must be in [0,512]."); }
    internal static void ValidateRadius(Size radius) { ValidateRadius(radius.Width); ValidateRadius(radius.Height); }
    public static BlurGradient Linear(Point start, Point end, IEnumerable<BlurStop> stops, GradientSpread spread = GradientSpread.Pad, GradientUnits units = GradientUnits.Relative, int quality = 8)
        => new(GradientKind.Linear, start, end, new(1,1), stops, spread, units, quality);
    public static BlurGradient Horizontal(params BlurStop[] stops) => Linear(new(0,0), new(1,0), stops);
    public static BlurGradient Vertical(params BlurStop[] stops) => Linear(new(0,0), new(0,1), stops);
    public static BlurGradient Radial(Point center, float radius, IEnumerable<BlurStop> stops, GradientSpread spread = GradientSpread.Pad, GradientUnits units = GradientUnits.Relative, int quality = 8)
        => new(GradientKind.Radial, center, center, new(radius,radius), stops, spread, units, quality);
    public static BlurGradient Elliptical(Point center, Size radius, IEnumerable<BlurStop> stops, GradientSpread spread = GradientSpread.Pad, GradientUnits units = GradientUnits.Relative, int quality = 8)
        => new(GradientKind.Elliptical, center, center, radius, stops, spread, units, quality);
    public static BlurGradient Sweep(Point center, IEnumerable<BlurStop> stops, GradientSpread spread = GradientSpread.Pad, GradientUnits units = GradientUnits.Relative, int quality = 8)
        => new(GradientKind.Sweep, center, center, new(1,1), stops, spread, units, quality);
    public static BlurGradient Diamond(Point center, Size radius, IEnumerable<BlurStop> stops, GradientSpread spread = GradientSpread.Pad, GradientUnits units = GradientUnits.Relative, int quality = 8)
        => new(GradientKind.Diamond, center, center, radius, stops, spread, units, quality);
    public static BlurGradient Square(Point center, Size radius, IEnumerable<BlurStop> stops, GradientSpread spread = GradientSpread.Pad, GradientUnits units = GradientUnits.Relative, int quality = 8)
        => new(GradientKind.Square, center, center, radius, stops, spread, units, quality);
    internal Gradient MakeGradient(IEnumerable<GradientStop> stops) => Kind switch
    {
        GradientKind.Linear => Gradient.Linear(Start, End, stops, Spread, Units),
        GradientKind.Radial => Gradient.Radial(Start, Radius.Width, stops, Spread, Units),
        GradientKind.Elliptical => Gradient.Elliptical(Start, Radius, stops, Spread, Units),
        GradientKind.Sweep => Gradient.Sweep(Start, stops, spread: Spread, units: Units),
        GradientKind.Diamond => Gradient.Diamond(Start, Radius, stops, Spread, Units),
        _ => Gradient.Square(Start, Radius, stops, Spread, Units)
    };
}
