using Swealium.Graphics;
namespace Swealium.UI;

/// <summary>Retained bitmap display. Source is borrowed and must outlive the widget's use.</summary>
public class Image:Widget
{
    private Bitmap? source;
    private Rect? sourceRectangle;
    private ImageStretch stretch=ImageStretch.Uniform;
    private Alignment horizontal=Alignment.Center,vertical=Alignment.Center;
    private BitmapInterpolation interpolation=BitmapInterpolation.Linear;
    public Bitmap? Source {get=>source;set{source=value;InvalidateLayout();}}
    public Rect? SourceRectangle {get=>sourceRectangle;set{if(value is {} rect)ImageLayout.ValidateRect(rect);sourceRectangle=value;InvalidateLayout();}}
    public ImageStretch Stretch {get=>stretch;set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));stretch=value;InvalidateLayout();}}
    public BitmapInterpolation Interpolation {get=>interpolation;set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));interpolation=value;}}
    public Alignment HorizontalContentAlignment {get=>horizontal;set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));horizontal=value;}}
    public Alignment VerticalContentAlignment {get=>vertical;set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));vertical=value;}}
    private Size Intrinsic=>SourceRectangle is {} crop?new(crop.Width,crop.Height):Source?.Size??default;
    protected override Size MeasureContent(Size available)=>ImageLayout.Measure(Intrinsic,available,Stretch);
    public override void Draw(DrawingContext context)
    {
        if(Source is null)return;
        context.DrawBitmap(Source,ImageLayout.Fit(Intrinsic,ContentBounds,Stretch,HorizontalContentAlignment,VerticalContentAlignment),SourceRectangle,1,Interpolation,ContentBounds);
    }
}
