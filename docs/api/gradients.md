# Gradient API contracts

[Documentation index](../README.md) · [Usage and geometry](../gradients.md) · [Complete signatures](reference.md)

Namespace: Swealium.Graphics. Gradient is sealed and immutable, with factory methods and no public constructor or disposal requirement.

| Factory | Parameters after geometry | Result |
| --- | --- | --- |
| Linear(Point start, Point end, ...) | IEnumerable&lt;GradientStop&gt; stops, GradientSpread spread=Pad, GradientUnits units=Relative | Projection between distinct endpoints |
| Horizontal/Vertical/Diagonal(params Color[] colors) | At least two solid colors | Relative linear gradient with evenly spaced stops |
| Radial(Point center, float radius, ...) | stops, spread=Pad, units=Relative | Circular falloff |
| Elliptical(Point center, Size radius, ...) | stops, spread=Pad, units=Relative | Independent X/Y radii |
| Sweep(Point center, ...) | stops, float startAngle=0, float endAngle=360, spread=Pad, units=Relative | Clockwise angular falloff |
| TwoPointConical(Point start, float startRadius, Point end, float endRadius, ...) | stops, spread=Pad, units=Relative | Two-circle conical interpolation |
| Diamond(Point center, Size radius, ...) | stops, spread=Pad, units=Relative | Manhattan distance |
| Square(Point center, Size radius, ...) | stops, spread=Pad, units=Relative | Maximum-axis distance |

All descriptions expose get-only Kind, Stops, Spread, Units, Start, End, Radius, StartRadius, StartAngle, EndAngle. Start is the center for single-center kinds; End equals Start except for linear/conical gradients. Unused Radius defaults (1,1), StartRadius=0, angles=0/360. Stops is a read-only copied collection of GradientStop readonly records with Offset and Color init properties.

GradientKind: Linear, Radial, Elliptical, Sweep, TwoPointConical, Diamond, Square. GradientSpread: Pad, Repeat, Reflect, Decal. GradientUnits: Relative, Absolute. See the guide for exact coordinate domains and spread behavior.

Null stop/color collections throw ArgumentNullException. Between 2 and 4096 stops are required; offsets must be finite, ordered, and within [0,1], and colors solid. Violations throw ArgumentException. Invalid enum/point/radius/angle values throw ArgumentOutOfRangeException. Radius components and conical endRadius must be finite positive; startRadius finite nonnegative. Equal linear endpoints and identical conical circles throw ArgumentException. Angles must be finite with 0 &lt; end-start &lt;= 360. Duplicate stop offsets are allowed. Geometry outside [0,1] is permitted; coordinates that overflow when resolved against drawing bounds are rejected at draw time.

Color adds get-only Gradient? Gradient, bool IsGradient, and implicit conversion from Gradient (null throws ArgumentNullException). Gradient colors start with white channels and A=255. Changing A multiplies output alpha; R/G/B changes do not tint shaders. Record equality includes gradient reference identity. Existing solid-color APIs remain source-compatible; the Color struct now contains a managed reference and must not be treated as an unmanaged RGBA memory layout.

Gradients have no native handles in their public API. Their geometry and stop validation occurs on construction; device/shader errors propagate during drawing through the existing application cleanup path.
