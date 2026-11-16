using Swealium.Graphics;
namespace Swealium.Animation;

/// <summary>Typed value interpolation. Color interpolation uses solid sRGB channels, not linear-light mixing.</summary>
public static class AnimationValues
{
    public static double Double(double from, double to, double progress)
    {
        if (!double.IsFinite(from) || !double.IsFinite(to) || !double.IsFinite(progress)) throw new ArgumentOutOfRangeException(nameof(progress));
        double result = from*(1-progress)+to*progress;
        if (!double.IsFinite(result)) throw new ArgumentOutOfRangeException(nameof(progress));
        return result;
    }
    public static float Float(float from, float to, double progress)
    {
        double value = Double(from,to,progress);
        if (value < -float.MaxValue || value > float.MaxValue) throw new ArgumentOutOfRangeException(nameof(progress));
        return (float)value;
    }
    public static Point Point(Point from, Point to, double progress) => new(Float(from.X,to.X,progress),Float(from.Y,to.Y,progress));
    public static Size Size(Size from, Size to, double progress) => new(Float(from.Width,to.Width,progress),Float(from.Height,to.Height,progress));
    public static Color Color(Color from, Color to, double progress)
    {
        if (from.IsGradient || to.IsGradient) throw new ArgumentException("Animate solid colors, or interpolate a gradient description explicitly.");
        byte Channel(byte a,byte b) => (byte)Math.Clamp(Math.Round(Double(a,b,progress)),0,255);
        return new(Channel(from.R,to.R),Channel(from.G,to.G),Channel(from.B,to.B),Channel(from.A,to.A));
    }
}
