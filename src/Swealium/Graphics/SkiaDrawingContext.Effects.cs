using SkiaSharp;
namespace Swealium.Graphics;

internal sealed partial class SkiaDrawingContext
{
    private SKCanvas Canvas => canvas;
    private sealed class Scope(SkiaDrawingContext owner, int saved) : IDisposable
    {
        private bool disposed;
        public void Dispose()
        {
            if (disposed || !owner.active) return;
            if (owner.Canvas.SaveCount != saved + 1) throw new InvalidOperationException("Dispose drawing scopes in reverse order.");
            owner.Canvas.RestoreToCount(saved); disposed = true;
        }
    }
    public override IDisposable PushClip(Rect bounds, CornerRadii radii = default)
    {
        Verify(); ImageLayout.ValidateRect(bounds); using var rounded = RoundRect(bounds, radii);
        int saved = canvas.Save();
        canvas.ClipRoundRect(rounded, SKClipOperation.Intersect, AntiAliasing); return new Scope(this, saved);
    }
    public override IDisposable PushTranslation(Point offset)
    {
        Verify(); if (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y)) throw new ArgumentOutOfRangeException(nameof(offset));
        int saved = canvas.Save(); canvas.Translate(offset.X, offset.Y); return new Scope(this, saved);
    }
    public override IDisposable PushOpacity(float opacity)
    {
        Verify(); ImageLayout.ValidateOpacity(opacity);
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)MathF.Round(opacity*255)) };
        return new Scope(this, canvas.SaveLayer(paint));
    }
    private IDisposable Layer(SKImageFilter filter)
    {
        using var paint = new SKPaint { ImageFilter = filter };
        return new Scope(this, canvas.SaveLayer(paint));
    }
    public override IDisposable PushBlur(Size radius, BlurEdgeMode edgeMode = BlurEdgeMode.Transparent)
    {
        Verify(); BlurGradient.ValidateRadius(radius);
        using var filter = SKImageFilter.CreateBlur(radius.Width, radius.Height, Tile(edgeMode)); return Layer(filter);
    }
    public override IDisposable PushProgressiveBlur(BlurGradient gradient, Rect bounds, BlurEdgeMode edgeMode = BlurEdgeMode.Clamp)
    {
        Verify(); ArgumentNullException.ThrowIfNull(gradient); ImageLayout.ValidateRect(bounds);
        using var filter = ProgressiveFilter(gradient, bounds, edgeMode); return Layer(filter);
    }
    public override IDisposable PushDropShadow(Point offset, Size radius, Color color, bool shadowOnly = false)
    {
        Verify(); BlurGradient.ValidateRadius(radius);
        if (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y)) throw new ArgumentOutOfRangeException(nameof(offset));
        if (color.IsGradient) throw new ArgumentException("Shadows require a solid color.", nameof(color));
        using var filter = shadowOnly ? SKImageFilter.CreateDropShadowOnly(offset.X,offset.Y,radius.Width,radius.Height,TextLayout.Native(color))
            : SKImageFilter.CreateDropShadow(offset.X,offset.Y,radius.Width,radius.Height,TextLayout.Native(color));
        return Layer(filter);
    }
    public override IDisposable PushColorMatrix(ColorMatrix matrix)
    {
        Verify(); ArgumentNullException.ThrowIfNull(matrix);
        using var color = SKColorFilter.CreateColorMatrix(matrix.Values); using var filter = SKImageFilter.CreateColorFilter(color); return Layer(filter);
    }
    public override void DrawBackdropBlur(Rect bounds, CornerRadii radii, Size radius, BlurEdgeMode edgeMode = BlurEdgeMode.Clamp)
    {
        Verify(); ImageLayout.ValidateRect(bounds); BlurGradient.ValidateRadius(radius);
        using var filter = SKImageFilter.CreateBlur(radius.Width, radius.Height, Tile(edgeMode)); Backdrop(bounds,radii,filter);
    }
    public override void DrawBackdropBlur(Rect bounds, CornerRadii radii, BlurGradient gradient, BlurEdgeMode edgeMode = BlurEdgeMode.Clamp)
    {
        Verify(); ArgumentNullException.ThrowIfNull(gradient); ImageLayout.ValidateRect(bounds);
        using var filter = ProgressiveFilter(gradient,bounds,edgeMode); Backdrop(bounds,radii,filter);
    }
    private void Backdrop(Rect bounds, CornerRadii radii, SKImageFilter filter)
    {
        int saved = canvas.Save();
        try
        {
            using var clip = RoundRect(bounds,radii); canvas.ClipRoundRect(clip,SKClipOperation.Intersect,AntiAliasing);
            // Replace the sampled backdrop rather than compositing its alpha over itself.
            using var paint = new SKPaint { BlendMode = SKBlendMode.Src };
            canvas.SaveLayer(new SKCanvasSaveLayerRec { Backdrop = filter, Paint = paint });
        }
        finally { canvas.RestoreToCount(saved); }
    }
    private static SKShaderTileMode Tile(BlurEdgeMode mode) => mode switch
    {
        BlurEdgeMode.Transparent => SKShaderTileMode.Decal, BlurEdgeMode.Clamp => SKShaderTileMode.Clamp,
        BlurEdgeMode.Repeat => SKShaderTileMode.Repeat, BlurEdgeMode.Reflect => SKShaderTileMode.Mirror,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
    private static SKImageFilter ProgressiveFilter(BlurGradient ramp, Rect bounds, BlurEdgeMode edgeMode)
    {
        var tile = Tile(edgeMode);
        if (ramp.Maximum == 0) return SKImageFilter.CreateBlur(0,0,tile);
        SKImageFilter? total = null;
        try
        {
            for (int level = 0; level < ramp.Quality; level++)
            {
                // Triangular weights sum to one. Insert each level crossing so large stop intervals
                // interpolate the blur radius rather than merely fading between two endpoint images.
                var stops = new List<GradientStop>();
                void Add(float offset, float radius)
                {
                    float weight = Math.Max(0,1-Math.Abs(radius/ramp.Maximum*(ramp.Quality-1)-level));
                    stops.Add(new(offset,new(255,255,255,(byte)MathF.Round(weight*255))));
                }
                Add(ramp.Stops[0].Offset,ramp.Stops[0].Radius);
                for (int i = 1; i < ramp.Stops.Count; i++)
                {
                    var a = ramp.Stops[i-1]; var b = ramp.Stops[i];
                    var crossings = new List<(float Offset,float Radius)>();
                    if (a.Radius != b.Radius && a.Offset != b.Offset)
                        for (int k = Math.Max(0,level-1); k <= Math.Min(ramp.Quality-1,level+1); k++)
                        {
                            float radius = ramp.Maximum*k/(ramp.Quality-1), t = (radius-a.Radius)/(b.Radius-a.Radius);
                            if (t > 0 && t < 1) crossings.Add((a.Offset+t*(b.Offset-a.Offset),radius));
                        }
                    foreach (var crossing in crossings.OrderBy(c => c.Offset)) Add(crossing.Offset,crossing.Radius);
                    Add(b.Offset,b.Radius);
                }
                if (stops.All(stop => stop.Color.A == 0)) continue;
                using var shader = GradientPaint.Shader(ramp.MakeGradient(stops),bounds);
                using var mask = SKImageFilter.CreateShader(shader);
                float sigma = ramp.Maximum*level/(ramp.Quality-1);
                using var blur = level == 0 ? null : SKImageFilter.CreateBlur(sigma,sigma,tile);
                using var weighted = SKImageFilter.CreateBlendMode(SKBlendMode.DstIn,blur,mask);
                var sum = total is null ? SKImageFilter.CreateOffset(0,0,weighted)
                    : SKImageFilter.CreateBlendMode(SKBlendMode.Plus,total,weighted);
                total?.Dispose(); total = sum;
            }
            var result = total!; total = null; return result;
        }
        finally { total?.Dispose(); }
    }
}
