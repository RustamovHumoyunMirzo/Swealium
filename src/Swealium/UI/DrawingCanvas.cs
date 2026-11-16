using Swealium.Graphics;
namespace Swealium.UI;

/// <summary>An immediate-mode drawing callback within the retained tree. Batch large datasets with PointBuffer.</summary>
public class DrawingCanvas : Widget
{
    private Size preferredSize=new(300,150);
    private bool painting;
    private DrawingDetail detail=DrawingDetail.Automatic;
    public DrawingDetail Detail {get=>detail;set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));detail=value;}}
    public DrawingCanvas(){}
    public DrawingCanvas(Action<DrawingContext,Rect> paint)
    {ArgumentNullException.ThrowIfNull(paint);Paint+=paint;}
    public Color Background {get;set;}=Color.Transparent;
    public Size PreferredSize
    {
        get=>preferredSize;
        set{LayoutEngine.ValidateSize(value,false);preferredSize=value;InvalidateLayout();}
    }
    public event Action<DrawingContext,Rect>? Paint;
    protected override Size MeasureContent(Size available)=>PreferredSize;
    protected virtual void OnPaint(DrawingContext drawing,Rect bounds)=>Paint?.Invoke(drawing,bounds);
    public override void Draw(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if(painting)throw new InvalidOperationException("DrawingCanvas painting cannot be reentered.");
        painting=true;
        try
        {
            var previous=context.PolylineDetail;
            try
            {
                context.PolylineDetail=Detail;
                if(Background.A!=0)context.FillRectangle(new(0,0,ActualWidth,ActualHeight),Background,Radii);
                OnPaint(context,ContentBounds);
            }
            finally{context.PolylineDetail=previous;}
        }
        finally{painting=false;}
    }
}
