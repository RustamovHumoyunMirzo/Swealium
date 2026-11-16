namespace Swealium.Graphics;

/// <summary>A uniform decorative outline. Positive offset leaves a gap; negative offset moves it inward.</summary>
public readonly record struct Outline
{
    public Outline(float width,Color color,StrokeStyle style=StrokeStyle.Solid,float offset=0)
    {
        _=new StrokeSide(width,color,style);
        if(!float.IsFinite(offset))throw new ArgumentOutOfRangeException(nameof(offset));
        Width=width;Color=color;Style=style;Offset=offset;
    }
    public Color Color {get;}
    public float Width {get;}
    public StrokeStyle Style {get;}
    public float Offset {get;}
}
