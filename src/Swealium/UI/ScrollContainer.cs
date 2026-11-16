using Swealium.Graphics;
using Swealium.Input;
namespace Swealium.UI;

/// <summary>A clipped, scrollable Container with optional independently styled scrollbars.</summary>
public class ScrollContainer : Container
{
    private ScrollDirection direction=ScrollDirection.Vertical;
    private OverscrollBehavior overscroll;
    public OverscrollBehavior Overscroll {get=>overscroll;set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));overscroll=value;}}
    public event Action<ScrollBoundaryEvent>? BoundaryReached;
    private ScrollBarVisibility horizontalVisibility,verticalVisibility;
    private ScrollBarStyle horizontalStyle=new(),verticalStyle=new();
    private bool overlay,arranging;
    private float wheelStep=40;
    private Point offset;
    private Size extent;
    private Rect viewport;
    private bool horizontalShown,verticalShown;
    private readonly Dictionary<PointerId,(ScrollAxis Axis,float Grab)> drags=[];
    private readonly HashSet<PointerId> barPresses=[];
    private Point pointer=new(-1,-1);
    private Point reportedOffset;
    private Size reportedExtent;
    private Rect reportedViewport;
    public ScrollContainer() { IsFocusable=true; }
    /// <summary>When nested and overflowing, require focus inside this container to own wheel scrolling.</summary>
    public bool RequireFocusForNestedScrolling {get;set;}
    public ScrollDirection Direction
    {
        get=>direction;
        set { if ((value & ~ScrollDirection.Both)!=0) throw new ArgumentOutOfRangeException(nameof(value)); direction=value;InvalidateLayout(); }
    }
    public ScrollBarVisibility HorizontalScrollBarVisibility { get=>horizontalVisibility;set {ValidateVisibility(value);horizontalVisibility=value;InvalidateLayout();} }
    public ScrollBarVisibility VerticalScrollBarVisibility { get=>verticalVisibility;set {ValidateVisibility(value);verticalVisibility=value;InvalidateLayout();} }
    public ScrollBarStyle HorizontalScrollBarStyle { get=>horizontalStyle;set {ArgumentNullException.ThrowIfNull(value);value.Validate();horizontalStyle=value;InvalidateLayout();} }
    public ScrollBarStyle VerticalScrollBarStyle { get=>verticalStyle;set {ArgumentNullException.ThrowIfNull(value);value.Validate();verticalStyle=value;InvalidateLayout();} }
    /// <summary>True draws bars over the content; false reserves gutter space.</summary>
    public bool OverlayScrollBars { get=>overlay;set {overlay=value;InvalidateLayout();} }
    public float WheelStep { get=>wheelStep;set {if (!float.IsFinite(value)||value<0) throw new ArgumentOutOfRangeException(nameof(value));wheelStep=value;} }
    public Point Offset => offset;
    public float HorizontalOffset { get=>offset.X;set=>ScrollTo(value,offset.Y); }
    public float VerticalOffset { get=>offset.Y;set=>ScrollTo(offset.X,value); }
    public Size Extent => extent;
    public Rect Viewport => viewport;
    public Point MaximumOffset => new(Enabled(ScrollDirection.Horizontal)?Math.Max(0,extent.Width-viewport.Width):0,
        Enabled(ScrollDirection.Vertical)?Math.Max(0,extent.Height-viewport.Height):0);
    public ScrollBarInfo HorizontalScrollBar => Bar(ScrollAxis.Horizontal);
    public ScrollBarInfo VerticalScrollBar => Bar(ScrollAxis.Vertical);
    public event Action<ScrollChangedEvent>? ScrollChanged;
    internal override Rect? ChildrenClip => viewport;
    internal override bool CanHitChildren(Point local) => viewport.Contains(local) && !BarAt(local).HasValue;
    private bool Enabled(ScrollDirection axis) => (Direction & axis)!=0;
    private static void ValidateVisibility(ScrollBarVisibility value) {if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));}

    protected override Size MeasureContent(Size available)
    {
        var result=Resolve(new(0,0,available.Width,available.Height));
        float width=result.Extent.Width+(OverlayScrollBars||!result.Vertical?0:VerticalScrollBarStyle.Thickness);
        float height=result.Extent.Height+(OverlayScrollBars||!result.Horizontal?0:HorizontalScrollBarStyle.Thickness);
        return new(float.IsFinite(available.Width)?Math.Min(width,available.Width):width,
            float.IsFinite(available.Height)?Math.Min(height,available.Height):height);
    }
    private (Rect Viewport,Size Extent,bool Horizontal,bool Vertical) Resolve(Rect area)
    {
        bool h=Enabled(ScrollDirection.Horizontal)&&HorizontalScrollBarVisibility==ScrollBarVisibility.Always;
        bool v=Enabled(ScrollDirection.Vertical)&&VerticalScrollBarVisibility==ScrollBarVisibility.Always;
        Rect view=area;Size demand=default;
        // Adding one gutter may cause overflow on the other axis; visibility grows monotonically.
        for (int pass=0;pass<4;pass++)
        {
            view=new(area.X,area.Y,Math.Max(0,area.Width-(!OverlayScrollBars&&v?VerticalScrollBarStyle.Thickness:0)),
                Math.Max(0,area.Height-(!OverlayScrollBars&&h?HorizontalScrollBarStyle.Thickness:0)));
            demand=base.MeasureContent(new(Enabled(ScrollDirection.Horizontal)?float.PositiveInfinity:view.Width,
                Enabled(ScrollDirection.Vertical)?float.PositiveInfinity:view.Height));
            if (!float.IsFinite(view.Width)) view=view with {Width=demand.Width};
            if (!float.IsFinite(view.Height)) view=view with {Height=demand.Height};
            bool nextH=h || (Enabled(ScrollDirection.Horizontal)&&HorizontalScrollBarVisibility==ScrollBarVisibility.Auto&&demand.Width>view.Width+.01f);
            bool nextV=v || (Enabled(ScrollDirection.Vertical)&&VerticalScrollBarVisibility==ScrollBarVisibility.Auto&&demand.Height>view.Height+.01f);
            if (nextH==h && nextV==v) break;
            h=nextH;v=nextV;
        }
        // Disabled axes are viewport sized; enabled axes can grow beyond it.
        return (view,new(Enabled(ScrollDirection.Horizontal)?Math.Max(demand.Width,view.Width):view.Width,
            Enabled(ScrollDirection.Vertical)?Math.Max(demand.Height,view.Height):view.Height),h,v);
    }
    protected override void ArrangeContent(Rect area)
    {
        arranging=true;
        try
        {
            var result=Resolve(area);viewport=result.Viewport;extent=result.Extent;
            horizontalShown=result.Horizontal;verticalShown=result.Vertical;
            offset=Clamp(offset);ArrangeChildren();
        }
        finally {arranging=false;}
        ReportChange();
    }
    private void ArrangeChildren() => base.ArrangeContent(new(viewport.X-offset.X,viewport.Y-offset.Y,extent.Width,extent.Height));
    private Point Clamp(Point value)
    {
        var maximum=MaximumOffset;
        return new(Math.Clamp(value.X,0,maximum.X),Math.Clamp(value.Y,0,maximum.Y));
    }
    private void EnsureMetrics()
    {
        if (arranging) return;
        Widget root=this;while (root.Parent is not null) root=root.Parent;
        LayoutEngine.EnsureLayout(root);
    }
    public void ScrollTo(float horizontal,float vertical)
    {
        if (!float.IsFinite(horizontal)||!float.IsFinite(vertical)) throw new ArgumentOutOfRangeException(nameof(horizontal));
        EnsureMetrics();SetOffset(new(horizontal,vertical));
    }
    public void ScrollBy(float horizontal,float vertical)
    {
        if (!float.IsFinite(horizontal)||!float.IsFinite(vertical)) throw new ArgumentOutOfRangeException(nameof(horizontal));
        EnsureMetrics();
        SetOffset(new((float)Math.Clamp((double)offset.X+horizontal,0,MaximumOffset.X),(float)Math.Clamp((double)offset.Y+vertical,0,MaximumOffset.Y)));
    }
    private bool SetOffset(Point value)
    {
        Point next=Clamp(value);if (next==offset) return false;
        offset=next;
        if (!arranging && IsLayoutValid) ArrangeChildren();else InvalidateLayout();
        ReportChange();return true;
    }
    private void ReportChange()
    {
        if (reportedOffset==offset&&reportedExtent==extent&&reportedViewport==viewport) return;
        var previous=reportedOffset;reportedOffset=offset;reportedExtent=extent;reportedViewport=viewport;
        ScrollChanged?.Invoke(new(previous,offset,extent,viewport));
    }
    /// <summary>Reveal a rectangle in unscrolled content coordinates, minimally moving each enabled axis.</summary>
    public void BringIntoView(Rect bounds)
    {
        if (!float.IsFinite(bounds.X)||!float.IsFinite(bounds.Y)||!float.IsFinite(bounds.Width)||!float.IsFinite(bounds.Height)||bounds.Width<0||bounds.Height<0)
            throw new ArgumentOutOfRangeException(nameof(bounds));
        EnsureMetrics();
        float x=Reveal(bounds.X,bounds.Width,offset.X,viewport.Width,MaximumOffset.X);
        float y=Reveal(bounds.Y,bounds.Height,offset.Y,viewport.Height,MaximumOffset.Y);
        ScrollTo(x,y);
        static float Reveal(float start,float length,float current,float page,float maximum)
        {
            double end=(double)start+length,target=current;
            if (length>page && start<=current && end>=current+page) return current;
            if (start<current || length>page) target=start;
            else if (end>current+page) target=end-page;
            return (float)Math.Clamp(target,0,maximum);
        }
    }
    /// <summary>Reveal an attached descendant's arranged bounds.</summary>
    public void BringIntoView(Widget descendant)
    {
        ArgumentNullException.ThrowIfNull(descendant);EnsureMetrics();
        if (ReferenceEquals(descendant,this)) return;
        Widget? node=descendant;
        for(;node is not null&&!ReferenceEquals(node,this);node=node.Parent){}
        if(node is null)throw new ArgumentException("The widget must be a descendant of this scroll container.",nameof(descendant));
        if(!System.Numerics.Matrix3x2.Invert(WorldTransform,out var inverse))return;
        Rect bounds=Graphics.Transform.Map(new(0,0,descendant.ActualWidth,descendant.ActualHeight),descendant.WorldTransform*inverse);
        BringIntoView(bounds with{X=bounds.X-viewport.X+offset.X,Y=bounds.Y-viewport.Y+offset.Y});
    }
    private ScrollBarInfo Bar(ScrollAxis axis)
    {
        bool vertical=axis==ScrollAxis.Vertical,shown=vertical?verticalShown:horizontalShown;
        var style=vertical?VerticalScrollBarStyle:HorizontalScrollBarStyle;
        Rect area=ContentBounds;
        float maximum=vertical?MaximumOffset.Y:MaximumOffset.X,value=vertical?offset.Y:offset.X;
        float page=vertical?viewport.Height:viewport.Width,total=vertical?extent.Height:extent.Width;
        if (!shown) return new(axis,false,default,default,value,maximum,page,total,false,drags.Values.Any(d=>d.Axis==axis));
        float thickness=Math.Min(style.Thickness,vertical?area.Width:area.Height);
        Rect track=vertical?new(area.X+area.Width-thickness,area.Y,thickness,Math.Max(0,area.Height-(horizontalShown?Math.Min(HorizontalScrollBarStyle.Thickness,area.Height):0))):
            new(area.X,area.Y+area.Height-thickness,Math.Max(0,area.Width-(verticalShown?Math.Min(VerticalScrollBarStyle.Thickness,area.Width):0)),thickness);
        float inset=Math.Min(style.Inset,Math.Min(track.Width,track.Height)/2);
        track=new(track.X+inset,track.Y+inset,Math.Max(0,track.Width-inset*2),Math.Max(0,track.Height-inset*2));
        float length=vertical?track.Height:track.Width;
        float thumbLength=Math.Clamp(total<=0?length:length*(page/total),Math.Min(length,style.MinimumThumbLength),length);
        float position=maximum<=0?0:(length-thumbLength)*(value/maximum);
        Rect thumb=vertical?new(track.X,track.Y+position,track.Width,thumbLength):new(track.X+position,track.Y,thumbLength,track.Height);
        return new(axis,true,track,thumb,value,maximum,page,total,track.Contains(pointer),drags.Values.Any(d=>d.Axis==axis));
    }
    private ScrollAxis? BarAt(Point point)
    {
        var vertical=VerticalScrollBar;if (vertical.IsVisible&&vertical.Track.Contains(point)) return ScrollAxis.Vertical;
        var horizontal=HorizontalScrollBar;if (horizontal.IsVisible&&horizontal.Track.Contains(point)) return ScrollAxis.Horizontal;
        return null;
    }
    public override void DrawOverlay(DrawingContext context)
    {
        base.DrawOverlay(context);
        if (horizontalShown) DrawScrollBar(context,HorizontalScrollBar);
        if (verticalShown) DrawScrollBar(context,VerticalScrollBar);
    }
    /// <summary>Override all scrollbar drawing while retaining built-in hit geometry and input behavior.</summary>
    protected virtual void DrawScrollBar(DrawingContext context,ScrollBarInfo bar)
    {
        var style=bar.Axis==ScrollAxis.Vertical?VerticalScrollBarStyle:HorizontalScrollBarStyle;
        context.FillRectangle(bar.Track,style.TrackColor,style.TrackRadii);
        context.FillRectangle(bar.Thumb,bar.IsDragging?style.PressedThumbColor:bar.IsHovered?style.HoverThumbColor:style.ThumbColor,style.ThumbRadii);
    }
    protected override void OnPointerInput(PointerEvent e)
    {
        base.OnPointerInput(e);
        if (e.Kind==PointerEventKind.Leave) pointer=new(-1,-1);else pointer=e.Position;
        if (e.Kind==PointerEventKind.Cancel) {drags.Remove(e.PointerId);barPresses.Remove(e.PointerId);return;}
        if (e.Kind==PointerEventKind.Up && e.Button==PointerButton.Primary && barPresses.Remove(e.PointerId))
        {drags.Remove(e.PointerId);e.Handled=true;return;}
        if (e.Kind==PointerEventKind.Down && e.Button==PointerButton.Primary && ReferenceEquals(e.Source,this) && BarAt(e.Position) is { } axis)
        {
            var bar=Bar(axis);bool vertical=axis==ScrollAxis.Vertical;
            barPresses.Add(e.PointerId);
            if (bar.Thumb.Contains(e.Position))
            {if(!drags.Values.Any(d=>d.Axis==axis))drags[e.PointerId]=(axis,vertical?e.Position.Y-bar.Thumb.Y:e.Position.X-bar.Thumb.X);}
            else
            {
                float coordinate=vertical?e.Position.Y:e.Position.X,origin=vertical?bar.Thumb.Y:bar.Thumb.X;
                float delta=(coordinate<origin?-1:1)*bar.ViewportLength;
                ScrollBy(vertical?0:delta,vertical?delta:0);
            }
            e.Handled=true;return;
        }
        if (e.Kind==PointerEventKind.Move && drags.TryGetValue(e.PointerId,out var drag))
        {
            var active=drag.Axis;
            var bar=Bar(active);bool vertical=active==ScrollAxis.Vertical;
            float travel=vertical?bar.Track.Height-bar.Thumb.Height:bar.Track.Width-bar.Thumb.Width;
            float position=(vertical?e.Position.Y-bar.Track.Y:e.Position.X-bar.Track.X)-drag.Grab;
            float value=travel<=0?0:Math.Clamp(position/travel,0,1)*bar.MaximumOffset;
            SetOffset(vertical?new(offset.X,value):new(value,offset.Y));e.Handled=true;return;
        }
        if (e.Kind==PointerEventKind.Wheel)
        {
            if(e.ScrollRecipients.Contains(this))return;
            var focused=FocusedWheelOwner();
            if(focused is not null&&ContainsDescendant(focused)&&!e.ScrollRecipients.Contains(focused))
            {
                focused.ApplyWheel(e,true);
                if(e.Handled||ReferenceEquals(focused,this))return;
            }
            bool gated=FocusGated;
            if(gated&&!ContainsFocus())return;
            ApplyWheel(e,gated);
        }
    }
    private bool FocusGated=>RequireFocusForNestedScrolling&&(MaximumOffset.X>0||MaximumOffset.Y>0)&&Ancestors().Any(widget=>widget is ScrollContainer);
    private IEnumerable<Widget> Ancestors(){for(Widget? node=Parent;node is not null;node=node.Parent)yield return node;}
    private InputManager? TreeInput
    {get{Widget node=this;while(node.Parent is not null)node=node.Parent;return node.RootOwner;}}
    private bool ContainsDescendant(Widget widget)
    {for(Widget? node=widget;node is not null;node=node.Parent)if(ReferenceEquals(node,this))return true;return false;}
    private bool ContainsFocus()=>TreeInput?.FocusedWidget is {} focus&&ContainsDescendant(focus);
    private ScrollContainer? FocusedWheelOwner()
    {
        for(Widget? node=TreeInput?.FocusedWidget;node is not null;node=node.Parent)
            if(node is ScrollContainer scroll&&scroll.FocusGated)return scroll;
        return null;
    }
    private void ApplyWheel(PointerEvent e,bool own)
    {
        if(!e.ScrollRecipients.Add(this))return;
        Point incoming=e.RemainingWheelDelta;
        float x=-incoming.X*WheelStep,y=-incoming.Y*WheelStep;
        bool rerouted=false;
        if(e.Modifiers.HasFlag(KeyModifiers.Shift)){if(x==0){x=y;rerouted=true;}y=0;}
        else if(Direction==ScrollDirection.Horizontal&&x==0){x=y;y=0;rerouted=true;}
        Point old=offset;ScrollBy(x,y);
        float dx=offset.X-old.X,dy=offset.Y-old.Y;
        Point remaining=WheelStep==0?incoming:rerouted?new(incoming.X,incoming.Y+dx/WheelStep):new(incoming.X+dx/WheelStep,incoming.Y+dy/WheelStep);
        if(Math.Abs(remaining.X)<.00001f)remaining=remaining with{X=0};
        if(Math.Abs(remaining.Y)<.00001f)remaining=remaining with{Y=0};
        e.RemainingWheelDelta=remaining;
        bool boundary=remaining!=default;
        if(boundary&&Overscroll!=OverscrollBehavior.None)BoundaryReached?.Invoke(new(remaining,offset));
        e.Handled=Overscroll switch
        {
            OverscrollBehavior.Chain=>!boundary,
            OverscrollBehavior.Contain or OverscrollBehavior.None=>true,
            _=>own||old!=offset
        };
    }
    protected override void OnKeyboardInput(KeyEvent e)
    {
        if (!e.IsDown || !ReferenceEquals(e.Source,this)) return;
        Point old=offset;
        switch (e.Key)
        {
            case Key.Left:ScrollBy(-WheelStep,0);break;
            case Key.Right:ScrollBy(WheelStep,0);break;
            case Key.Up:ScrollBy(0,-WheelStep);break;
            case Key.Down:ScrollBy(0,WheelStep);break;
            case Key.PageUp:ScrollBy(Direction==ScrollDirection.Horizontal?-viewport.Width:0,Enabled(ScrollDirection.Vertical)?-viewport.Height:0);break;
            case Key.PageDown:ScrollBy(Direction==ScrollDirection.Horizontal?viewport.Width:0,Enabled(ScrollDirection.Vertical)?viewport.Height:0);break;
            case Key.Home:ScrollTo(0,0);break;
            case Key.End:ScrollTo(MaximumOffset.X,MaximumOffset.Y);break;
            default:return;
        }
        if (offset!=old) e.Handled=true;
    }
}
