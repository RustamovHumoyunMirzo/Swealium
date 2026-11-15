using Swealium.Graphics;
namespace Swealium.UI;

public enum Alignment { Start, Center, End, Stretch }
public enum LayoutMode { Vertical, Horizontal, Overlay, Absolute }
/// <summary>Automatic preserves numeric/NaN sizing; Content keeps intrinsic size; Fill occupies the allocated parent slot.</summary>
public enum SizingMode { Automatic, Content, Fill }

/// <summary>Runs measure/arrange in logical units. Windows invoke this automatically before input and rendering.</summary>
public static class LayoutEngine
{
    public static void Layout(Widget root, Size viewport)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (root.Parent is not null) throw new InvalidOperationException("Layout must start at the tree root.");
        ValidateSize(viewport, false);
        if (root.LayoutViewport == viewport && root.IsLayoutValid) return;
        root.LayoutViewport = viewport;
        root.Measure(viewport);
        // The window's content root owns the viewport; its requested size is not overwritten.
        root.ArrangeSlot(new(0, 0, viewport.Width, viewport.Height), Alignment.Stretch, Alignment.Stretch, true, true);
    }
    internal static void EnsureLayout(Widget root)
    {
        if (root.LayoutViewport is { } viewport && !root.IsLayoutValid) Layout(root, viewport);
    }
    internal static void ValidateSize(Size value, bool allowInfinity)
    {
        if (float.IsNaN(value.Width) || float.IsNaN(value.Height) || value.Width < 0 || value.Height < 0 ||
            (!allowInfinity && (!float.IsFinite(value.Width) || !float.IsFinite(value.Height))))
            throw new ArgumentOutOfRangeException(nameof(value));
    }
    internal static float Offset(float extra, Alignment alignment) => alignment switch
    {
        Alignment.Center => Math.Max(0, extra) / 2,
        Alignment.End => Math.Max(0, extra),
        _ => 0
    };
}

