using SkiaSharp;
namespace Swealium.Graphics;

internal static class GradientPaint
{
    private static readonly Lazy<SKRuntimeEffect> diamond=new(()=>Effect("abs(q.x)+abs(q.y)"));
    private static readonly Lazy<SKRuntimeEffect> square=new(()=>Effect("max(abs(q.x),abs(q.y))"));
    private static SKRuntimeEffect Effect(string distance)=>SKRuntimeEffect.CreateShader(
        "uniform shader ramp; uniform float2 center; uniform float2 radius; half4 main(float2 p) { float2 q=(p-center)/radius; return ramp.eval(float2("+distance+",0)); }",out var error)
        ?? throw new NotSupportedException(error);
    internal static SKPaint Create(Color color,Rect bounds,bool antiAliasing=true,float width=0)
    {
        var paint=new SKPaint{Color=TextLayout.Native(color),IsAntialias=antiAliasing,Style=width>0?SKPaintStyle.Stroke:SKPaintStyle.Fill,StrokeWidth=width};
        try { Apply(paint,color,bounds);return paint; } catch {paint.Dispose();throw;}
    }
    internal static void Apply(SKPaint paint,Color color,Rect bounds)
    {
        if(color.Gradient is not {} gradient){paint.Shader=null;paint.Color=TextLayout.Native(color);return;}
        using var shader=Shader(gradient,bounds);
        paint.Color=SKColors.White.WithAlpha(color.A);paint.Shader=shader;
    }
    internal static SKShader Shader(Gradient g,Rect bounds)
    {
        float w=g.Units==GradientUnits.Relative?Math.Max(bounds.Width,.0001f):1;
        float h=g.Units==GradientUnits.Relative?Math.Max(bounds.Height,.0001f):1;
        SKPoint Position(Point p)=>g.Units==GradientUnits.Relative?new(bounds.X+p.X*w,bounds.Y+p.Y*h):new(p.X,p.Y);
        var a=Position(g.Start);var b=Position(g.End);
        float radius=g.Radius.Width*(g.Units==GradientUnits.Relative?Math.Min(w,h):1);
        if(!float.IsFinite(a.X)||!float.IsFinite(a.Y)||!float.IsFinite(b.X)||!float.IsFinite(b.Y)||!float.IsFinite(radius)||
            !float.IsFinite(g.Radius.Width*w)||!float.IsFinite(g.Radius.Height*h)||!float.IsFinite(g.StartRadius*Math.Min(w,h)))
            throw new ArgumentOutOfRangeException(nameof(bounds),"Gradient coordinates exceed the drawable range.");
        var colors=g.Stops.Select(s=>TextLayout.Native(s.Color)).ToArray();var offsets=g.Stops.Select(s=>s.Offset).ToArray();
        var tile=g.Spread switch{GradientSpread.Repeat=>SKShaderTileMode.Repeat,GradientSpread.Reflect=>SKShaderTileMode.Mirror,GradientSpread.Decal=>SKShaderTileMode.Decal,_=>SKShaderTileMode.Clamp};
        switch(g.Kind)
        {
            case GradientKind.Linear:return SKShader.CreateLinearGradient(a,b,colors,offsets,tile);
            case GradientKind.Radial:return SKShader.CreateRadialGradient(a,radius,colors,offsets,tile);
            case GradientKind.Elliptical:
                return SKShader.CreateRadialGradient(new(0,0),1,colors,offsets,tile,new SKMatrix{ScaleX=g.Radius.Width*w,ScaleY=g.Radius.Height*h,TransX=a.X,TransY=a.Y,Persp2=1});
            case GradientKind.Sweep:
                return SKShader.CreateSweepGradient(a,colors,offsets,tile,0,g.EndAngle-g.StartAngle,SKMatrix.CreateRotationDegrees(g.StartAngle,a.X,a.Y));
            case GradientKind.TwoPointConical:return SKShader.CreateTwoPointConicalGradient(a,g.StartRadius*(g.Units==GradientUnits.Relative?Math.Min(w,h):1),b,radius,colors,offsets,tile);
            default:
                var effect=g.Kind==GradientKind.Diamond?diamond.Value:square.Value;
                using(var ramp=SKShader.CreateLinearGradient(new(0,0),new(1,0),colors,offsets,tile))
                using(var uniforms=new SKRuntimeEffectUniforms(effect))
                using(var children=new SKRuntimeEffectChildren(effect))
                {
                    uniforms["center"]=new[]{a.X,a.Y};uniforms["radius"]=new[]{g.Radius.Width*w,g.Radius.Height*h};children["ramp"]=ramp;
                    return effect.ToShader(uniforms,children);
                }
        }
    }
    internal static void Clear(SKCanvas canvas,Color color,Rect bounds)
    {
        if(!color.IsGradient){canvas.Clear(TextLayout.Native(color));return;}
        using var paint=Create(color,bounds);paint.BlendMode=SKBlendMode.Src;canvas.DrawRect(SKRect.Create(bounds.X,bounds.Y,bounds.Width,bounds.Height),paint);
    }
}
