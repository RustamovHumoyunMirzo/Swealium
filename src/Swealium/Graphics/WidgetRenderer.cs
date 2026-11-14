using SkiaSharp;
using Swealium.UI;
namespace Swealium.Graphics;

internal static class WidgetRenderer
{
    internal static void Render(SKCanvas canvas, Widget root)
    {
        if (!root.IsVisible || (root.ClipToBounds && (root.ActualWidth <= 0 || root.ActualHeight <= 0))) return;
        int saved = canvas.Save();
        try
        {
            canvas.Translate(root.Bounds.X, root.Bounds.Y);
            if (root.ClipToBounds)
            {
                using var clip = SkiaDrawingContext.RoundRect(new(0, 0, root.ActualWidth, root.ActualHeight), root.Radii);
                canvas.ClipRoundRect(clip, SKClipOperation.Intersect, true);
            }
            var context = new SkiaDrawingContext(canvas);
            try { root.Draw(context); }
            finally { context.End(); }
            if (root is Container container)
                foreach (var child in container.PaintOrder) Render(canvas, child);
        }
        finally { canvas.RestoreToCount(saved); }
    }
}

