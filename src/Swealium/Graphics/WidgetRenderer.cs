using SkiaSharp;
using Swealium.UI;
namespace Swealium.Graphics;

internal static class WidgetRenderer
{
    internal static void Render(SKCanvas canvas, Widget root)
    {
        if (!root.IsVisible || root.Opacity==0 || (root.ClipToBounds && (root.ActualWidth <= 0 || root.ActualHeight <= 0))) return;
        int saved = canvas.Save();
        try
        {
            canvas.Translate(root.Bounds.X, root.Bounds.Y);
            if(root.Opacity<1)
            {
                using var opacity=new SKPaint{Color=SKColors.White.WithAlpha((byte)MathF.Round(root.Opacity*255))};
                canvas.SaveLayer(opacity);
            }
            if (root.ClipToBounds)
            {
                using var clip = SkiaDrawingContext.RoundRect(new(0, 0, root.ActualWidth, root.ActualHeight), root.Radii);
                canvas.ClipRoundRect(clip, SKClipOperation.Intersect, true);
            }
            var context = new SkiaDrawingContext(canvas);
            try { root.Draw(context); }
            finally { context.End(); }
            if (root is Container container)
            {
                int childrenSave = canvas.Save();
                try
                {
                    if (container.ChildrenClip is { } area) canvas.ClipRect(SKRect.Create(area.X,area.Y,area.Width,area.Height));
                    foreach (var child in container.PaintOrder) Render(canvas, child);
                }
                finally { canvas.RestoreToCount(childrenSave); }
            }
            var overlay = new SkiaDrawingContext(canvas);
            try { root.DrawOverlay(overlay); }
            finally { overlay.End(); }
            WidgetStrokeRenderer.Draw(canvas, root);
        }
        finally { canvas.RestoreToCount(saved); }
    }
}

