using SkiaSharp;
namespace Swealium.Graphics;

internal static class TextMetrics
{
    internal static Size Measure(string text, float fontSize)
    {
        using var font = new SKFont(SKTypeface.Default, fontSize);
        return new(font.MeasureText(text), font.Metrics.Descent - font.Metrics.Ascent);
    }
}
