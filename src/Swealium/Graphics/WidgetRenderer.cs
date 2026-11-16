using SkiaSharp;
using Swealium.UI;
namespace Swealium.Graphics;

internal static class WidgetRenderer
{
    private static readonly Lazy<SKRuntimeEffect> opacityBlend = new(() =>
        SKRuntimeEffect.CreateBlender("uniform float amount; half4 main(half4 src, half4 dst) { return mix(dst, src, amount); }", out var error)
        ?? throw new NotSupportedException(error));
    private static bool ReadsBackdrop(Widget widget)
    {
        foreach (var effect in widget.Effects) if (effect.IsEnabled && effect.ReadsBackdrop) return true;
        if (widget is Container container)
            foreach (var child in container.Children) if (child.IsVisible && child.Opacity > 0 && ReadsBackdrop(child)) return true;
        return false;
    }
    internal static void Render(SKCanvas canvas, Widget root)
    {
        if (!root.IsVisible || root.Transform.Scale.Width==0 || root.Transform.Scale.Height==0 || root.Opacity==0 || (root.ClipToBounds && (root.ActualWidth <= 0 || root.ActualHeight <= 0))) return;
        int saved = canvas.Save();
        try
        {
            _=root.TransformedBounds; // Validate composed/resolved coordinates before passing them to the backend.
            var matrix=root.LocalTransform;
            canvas.Concat(new SKMatrix{ScaleX=matrix.M11,SkewX=matrix.M21,TransX=matrix.M31,SkewY=matrix.M12,ScaleY=matrix.M22,TransY=matrix.M32,Persp2=1});
            if(root.Opacity<1)
            {
                if (ReadsBackdrop(root))
                {
                    // Seed this isolated group with the previous pixels so descendant backdrop effects
                    // can sample them. Lerp the completed group, including alpha, exactly once.
                    using var uniforms = new SKRuntimeEffectUniforms(opacityBlend.Value);
                    uniforms["amount"] = root.Opacity;
                    using var blender = opacityBlend.Value.ToBlender(uniforms);
                    using var paint = new SKPaint { Blender = blender };
                    canvas.SaveLayer(new SKCanvasSaveLayerRec { Paint = paint, Flags = SKCanvasSaveLayerRecFlags.InitializeWithPrevious });
                }
                else
                {
                    using var opacity=new SKPaint{Color=SKColors.White.WithAlpha((byte)MathF.Round(root.Opacity*255))};
                    canvas.SaveLayer(opacity);
                }
            }
            if (root.Effects.Count == 0) RenderContent(canvas,root);
            else
            {
                root.Effects.Enter();
                var effects = new SkiaDrawingContext(canvas);
                try
                {
                    foreach (var effect in root.Effects) if (effect.IsEnabled) effect.PrepareDraw(effects,root);
                    RenderEffect(canvas,root,effects,0);
                }
                finally { effects.End(); root.Effects.Exit(); }
            }
        }
        finally { canvas.RestoreToCount(saved); }
    }
    private static void RenderEffect(SKCanvas canvas, Widget root, SkiaDrawingContext context, int index)
    {
        if (index == root.Effects.Count) { RenderContent(canvas,root); return; }
        var effect = root.Effects[index];
        if (!effect.IsEnabled) { RenderEffect(canvas,root,context,index+1); return; }
        using var scope = effect.Begin(context,root);
        effect.PreDraw(context,root);
        RenderEffect(canvas,root,context,index+1);
        effect.PostDraw(context,root);
    }
    private static void RenderContent(SKCanvas canvas, Widget root)
    {
        int ownClip=canvas.Save();
        try
        {
            if (root.ClipToBounds)
            {
                using var clip = SkiaDrawingContext.RoundRect(new(0, 0, root.ActualWidth, root.ActualHeight), root.Radii);
                canvas.ClipRoundRect(clip, SKClipOperation.Intersect, SwealiumApplication.AntiAliasing);
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
        finally{canvas.RestoreToCount(ownClip);}
        WidgetOutlineRenderer.Draw(canvas,root);
    }
}

