using Swealium.Graphics;
namespace Swealium;

/// <summary>Window dimensions are logical screen coordinates.</summary>
public sealed record WindowOptions
{
    public string Title { get; init; } = "Swealium";
    public int Width { get; init; } = 960;
    public int Height { get; init; } = 640;
    public Color Background { get; init; } = new(24, 26, 30);
    public bool Resizable { get; init; } = true;
    public bool Borderless { get; init; }
    public bool AlwaysOnTop { get; init; }
    public bool ShowInTaskbar { get; init; } = true;
    public bool Visible { get; init; } = true;
    public bool Focusable { get; init; } = true;
    public bool HighPixelDensity { get; init; } = true;
    public WindowPosition? Position { get; init; }
    public WindowSize MinimumSize { get; init; }
    /// <summary>Zero on an axis means no upper limit.</summary>
    public WindowSize MaximumSize { get; init; }
    public WindowState InitialState { get; init; } = WindowState.Normal;
    /// <summary>Requires an owner. Use ShowDialog for custom modal dialogs.</summary>
    public bool IsModal { get; init; }
}
