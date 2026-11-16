using System.Numerics;
namespace Swealium.Graphics;

/// <summary>Render-only scale, clockwise rotation in degrees, then translation. Origin is relative to widget size.</summary>
public readonly record struct Transform
{
    private readonly Size? scale;
    private readonly Point? origin;
    public Point Translation {get;init;}
    public Size Scale {get=>scale??new(1,1);init=>scale=value;}
    public float Rotation {get;init;}
    public Point Origin {get=>origin??new(.5f,.5f);init=>origin=value;}
    public static Transform Identity=>default;
    public Transform(Point translation,Size? scale=null,float rotation=0,Point? origin=null)
    {Translation=translation;this.scale=scale;Rotation=rotation;this.origin=origin;Validate();}
    public static Transform Translate(float x,float y)=>new(new(x,y));
    public static Transform ScaleBy(float x,float y)=>new(default,new(x,y));
    public static Transform Rotate(float degrees)=>new(default,rotation:degrees);
    internal void Validate()
    {
        if(!float.IsFinite(Translation.X)||!float.IsFinite(Translation.Y)||!float.IsFinite(Scale.Width)||!float.IsFinite(Scale.Height)||!float.IsFinite(Rotation)||!float.IsFinite(Origin.X)||!float.IsFinite(Origin.Y))
            throw new ArgumentOutOfRangeException(nameof(Transform),"Transform components must be finite.");
    }
    internal Matrix3x2 Matrix(Size size,Point position)
    {
        var pivot=new Vector2(Origin.X*size.Width,Origin.Y*size.Height);
        var matrix=Matrix3x2.CreateScale(Scale.Width,Scale.Height)*Matrix3x2.CreateRotation((Rotation%360)*MathF.PI/180);
        matrix.Translation=pivot-Vector2.Transform(pivot,matrix)+new Vector2(Translation.X+position.X,Translation.Y+position.Y);
        Check(matrix);return matrix;
    }
    internal static void Check(Matrix3x2 matrix)
    {
        if(!float.IsFinite(matrix.M11)||!float.IsFinite(matrix.M12)||!float.IsFinite(matrix.M21)||!float.IsFinite(matrix.M22)||!float.IsFinite(matrix.M31)||!float.IsFinite(matrix.M32))
            throw new InvalidOperationException("The resolved transform exceeds the drawable coordinate range.");
    }
    internal static Rect Map(Rect bounds,Matrix3x2 matrix)
    {
        var a=Vector2.Transform(new(bounds.X,bounds.Y),matrix);var b=Vector2.Transform(new(bounds.X+bounds.Width,bounds.Y),matrix);
        var c=Vector2.Transform(new(bounds.X,bounds.Y+bounds.Height),matrix);var d=Vector2.Transform(new(bounds.X+bounds.Width,bounds.Y+bounds.Height),matrix);
        if(!float.IsFinite(a.X)||!float.IsFinite(a.Y)||!float.IsFinite(b.X)||!float.IsFinite(b.Y)||!float.IsFinite(c.X)||!float.IsFinite(c.Y)||!float.IsFinite(d.X)||!float.IsFinite(d.Y))
            throw new InvalidOperationException("The transformed geometry exceeds the drawable coordinate range.");
        float x=Math.Min(Math.Min(a.X,b.X),Math.Min(c.X,d.X)),y=Math.Min(Math.Min(a.Y,b.Y),Math.Min(c.Y,d.Y));
        return new(x,y,Math.Max(Math.Max(a.X,b.X),Math.Max(c.X,d.X))-x,Math.Max(Math.Max(a.Y,b.Y),Math.Max(c.Y,d.Y))-y);
    }
}
