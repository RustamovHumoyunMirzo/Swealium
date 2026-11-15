namespace Swealium.Graphics;

internal static class TextMetrics
{
    internal static Size Measure(string text, float fontSize)
    {
        using var layout = new TextLayout(text, new() { FontSize = fontSize }, new() { Wrapping = TextWrapping.None });
        return layout.Size;
    }
}
