using Swealium.Graphics;
namespace Swealium.UI;

/// <summary>Retained SVG or recorded-vector display. Source is borrowed.</summary>
public class VectorGraphics:Widget
{
    private VectorImage? source;
    private ImageStretch stretch=ImageStretch.Uniform;
    private Alignment horizontal=Alignment.Center,vertical=Alignment.Center;
    public VectorImage? Source {get=>source;set{source=value;InvalidateLayout();}}
    public ImageStretch Stretch {get=>stretch;set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));stretch=value;InvalidateLayout();}}
    public Alignment HorizontalContentAlignment {get=>horizontal;set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));horizontal=value;}}
    public Alignment VerticalContentAlignment {get=>vertical;set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));vertical=value;}}
    protected override Size MeasureContent(Size available)=>ImageLayout.Measure(Source?.Size??default,available,Stretch);
    public override void Draw(DrawingContext context)
    {if(Source is not null)context.DrawVector(Source,ImageLayout.Fit(Source.Size,ContentBounds,Stretch,HorizontalContentAlignment,VerticalContentAlignment),1,ContentBounds);}
}
