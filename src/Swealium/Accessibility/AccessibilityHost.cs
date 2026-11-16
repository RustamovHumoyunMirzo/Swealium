using Swealium.Graphics;
using Swealium.UI;
namespace Swealium.Accessibility;

/// <summary>Window-owned semantic tree, actions and adapter lifecycle. All operations require the UI thread.</summary>
public sealed class AccessibilityHost
{
    private readonly int thread=Environment.CurrentManagedThreadId;
    private readonly Dictionary<long,Widget> widgets=[];
    private IAccessibilityBridge? bridge;
    private AccessibilitySnapshot? previous;
    private long revision;
    private bool closed,publishing;
    internal AccessibilityHost(SwealiumWindow window){Window=window;}
    public SwealiumWindow Window {get;}
    public bool IsBridgeAttached=>bridge is not null;
    public event Action<AccessibilityUpdate>? Changed;
    private void Verify(){ObjectDisposedException.ThrowIf(closed||Window.IsDisposed,this);if(thread!=Environment.CurrentManagedThreadId)throw new InvalidOperationException("Accessibility operations require the window UI thread.");}
    public AccessibilitySnapshot CaptureSnapshot()
    {
        Verify();if(Window.Content is {} root)LayoutEngine.EnsureLayout(root);
        widgets.Clear();var nodes=new List<AccessibilityNode>();Rect viewport=new(0,0,Window.ClientSize.Width,Window.ClientSize.Height);
        bool available=Window.IsVisible&&!Window.IsMinimized&&!Window.IsInputBlocked&&!Window.IsCloseRequested;
        var windowState=available?AccessibilityState.Enabled:AccessibilityState.None;
        if(Window.Focusable)windowState|=AccessibilityState.Focusable;if(Window.HasFocus)windowState|=AccessibilityState.Focused;
        if(!Window.IsVisible||Window.IsMinimized)windowState|=AccessibilityState.Offscreen;
        nodes.Add(new(0,null,AccessibilityRole.Window,Window.Title,"","","",viewport,viewport,windowState,AccessibilityActions.None,null,0,0,AccessibilityLiveRegion.Off));
        if(Window.Content is {} content)Visit(content,0,viewport,available);
        return new(Window.Id,++revision,Array.AsReadOnly(nodes.ToArray()));
        void Visit(Widget widget,long parent,Rect clip,bool enabled)
        {
            var a=widget.Accessibility;if(!widget.IsVisible||a.IsHidden)return;
            if(!Enum.IsDefined(a.Role)||!Enum.IsDefined(a.LiveRegion))throw new ArgumentOutOfRangeException(nameof(widget.Accessibility));
            Rect bounds=widget.TransformedBounds;
            Rect visible=Intersection(bounds,clip);
            if(!System.Numerics.Matrix3x2.Invert(widget.WorldTransform,out _))visible=default;
            enabled&=widget.IsInteractable;
            var state=enabled?AccessibilityState.Enabled:AccessibilityState.None;
            if(widget.IsFocusable)state|=AccessibilityState.Focusable;if(widget.IsFocused)state|=AccessibilityState.Focused;
            if(widget.TextEditor is {IsEnabled:true,IsReadOnly:true})state|=AccessibilityState.ReadOnly;
            if(!Window.IsVisible||visible.Width<=0||visible.Height<=0)state|=AccessibilityState.Offscreen;
            string text=widget switch{Button b=>b.Text,Text t=>t.Markup==TextMarkup.Html?string.Concat(HtmlText.Parse(t.Content,t.Style).Select(r=>r.Text)):t.Content,_=>""};
            string name=a.Name??(widget is Text?text:widget.TextEditor is {IsEnabled:true} editor?editor.Placeholder:text);
            string? value=a.ExposeValue&&widget.TextEditor is {IsEnabled:true} box?box.Text:null;
            var selection=widget.TextEditor is {IsEnabled:true} select?select.Selection:default;
            var role=a.Role==AccessibilityRole.Automatic?widget switch{Button=>AccessibilityRole.Button,Text=>AccessibilityRole.Text,TextBox=>AccessibilityRole.TextBox,Image or VectorGraphics=>AccessibilityRole.Image,ScrollContainer=>AccessibilityRole.ScrollArea,Container=>AccessibilityRole.Group,_ when widget.TextEditor is {IsEnabled:true}=>AccessibilityRole.TextBox,_=>AccessibilityRole.Custom}:a.Role;
            var actions=Actions(widget);
            nodes.Add(new(widget.SemanticId,parent,role,name,a.Description??"",a.AutomationId??"",a.HelpText??"",bounds,visible,state,actions,value,
                a.ExposeValue?selection.Start:0,a.ExposeValue?selection.Length:0,a.LiveRegion));widgets.Add(widget.SemanticId,widget);
            if(widget is Container container)
            {
                Rect childClip=widget.ClipToBounds?visible:clip;
                if(container.ChildrenClip is {} local)childClip=Intersection(childClip,widget.ToWindow(local));
                foreach(var child in container.Children)Visit(child,widget.SemanticId,childClip,enabled);
            }
        }
    }
    private static AccessibilityActions Actions(Widget widget)
    {
        if(widget.Accessibility.Actions is {} custom)return custom;
        var actions=AccessibilityActions.ScrollIntoView;
        if(widget.IsFocusable)actions|=AccessibilityActions.Focus;
        if(widget is Button)actions|=AccessibilityActions.Invoke;
        if(widget.TextEditor is {IsEnabled:true} box){actions|=AccessibilityActions.SetSelection;if(!box.IsReadOnly)actions|=AccessibilityActions.SetValue;}
        if(widget is ScrollContainer)actions|=AccessibilityActions.ScrollBy;
        return actions;
    }
    public AccessibilityNode? HitTest(Point point)
    {
        if(!float.IsFinite(point.X)||!float.IsFinite(point.Y))throw new ArgumentOutOfRangeException(nameof(point));
        var snapshot=CaptureSnapshot();if(!Window.IsVisible||Window.IsMinimized)return null;
        Widget? hit=Window.Content is {} content?Find(content,point):null;
        return hit is not null?snapshot.Nodes.FirstOrDefault(n=>n.Id==hit.SemanticId):snapshot.Nodes.FirstOrDefault(n=>n.Id==0&&n.Bounds.Contains(point));
        static Widget? Find(Widget widget,Point parentPoint)
        {
            if(!widget.IsVisible||widget.Accessibility.IsHidden)return null;
            if(!widget.FromParent(parentPoint,out var local))return null;bool inside=widget.ContainsPoint(local);
            if(widget.ClipToBounds&&!inside)return null;
            if(widget is Container container&&container.CanHitChildren(local))
                foreach(var child in container.PaintOrder.Reverse())if(Find(child,local) is {} found)return found;
            return inside?widget:null;
        }
    }
    public bool TryPerform(AccessibilityActionRequest request)
    {
        Verify();ArgumentNullException.ThrowIfNull(request);if(!Enum.IsDefined(request.Action))return false;
        _=CaptureSnapshot();if(Window.IsInputBlocked||Window.IsCloseRequested||Window.IsMinimized||!Window.IsVisible||!widgets.TryGetValue(request.NodeId,out var widget)||!widget.IsInteractable)return false;
        var flag=request.Action switch{AccessibilityAction.Invoke=>AccessibilityActions.Invoke,AccessibilityAction.Focus=>AccessibilityActions.Focus,AccessibilityAction.SetValue=>AccessibilityActions.SetValue,
            AccessibilityAction.SetSelection=>AccessibilityActions.SetSelection,AccessibilityAction.ScrollBy=>AccessibilityActions.ScrollBy,_=>AccessibilityActions.ScrollIntoView};
        if(!Actions(widget).HasFlag(flag))return false;
        bool handled=widget.RaiseAccessibilityAction(request);
        if(!handled)
        {
            switch(request.Action)
            {
                case AccessibilityAction.Invoke when widget is Button:widget.PerformClick();handled=true;break;
                case AccessibilityAction.Focus:handled=Window.Input.Focus(widget);break;
                case AccessibilityAction.SetValue when widget.TextEditor is {IsEnabled:true,IsReadOnly:false} editor&&request.Value is not null:editor.Text=request.Value;handled=true;break;
                case AccessibilityAction.SetSelection when widget.TextEditor is {IsEnabled:true} editor:
                    if(request.SelectionStart<0||request.SelectionLength<0||request.SelectionStart>editor.Text.Length-request.SelectionLength)return false;
                    editor.Select(request.SelectionStart,request.SelectionLength);handled=true;break;
                case AccessibilityAction.ScrollIntoView:
                    for(var parent=widget.Parent;parent is not null;parent=parent.Parent)if(parent is ScrollContainer scroll)scroll.BringIntoView(widget);
                    handled=true;break;
                case AccessibilityAction.ScrollBy when widget is ScrollContainer scroll:
                    if(!float.IsFinite(request.ScrollDelta.X)||!float.IsFinite(request.ScrollDelta.Y))return false;
                    scroll.ScrollBy(request.ScrollDelta.X,request.ScrollDelta.Y);handled=true;break;
            }
        }
        if(handled)Refresh();return handled;
    }
    public void AttachBridge(IAccessibilityBridge adapter)
    {
        Verify();ArgumentNullException.ThrowIfNull(adapter);if(bridge is not null)throw new InvalidOperationException("Detach the existing bridge before attaching another.");
        bridge=adapter;previous=null;
        try{adapter.Attach(this);Refresh();}
        catch{bridge=null;try{adapter.Detach();}finally{adapter.Dispose();}throw;}
    }
    public void DetachBridge()
    {
        Verify();var old=bridge;bridge=null;if(old is null)return;
        try{old.Detach();}finally{old.Dispose();}
    }
    public void Refresh()
    {
        Verify();if(publishing)return;
        var snapshot=CaptureSnapshot();var changes=new List<AccessibilityChange>();
        var old=previous?.Nodes.ToDictionary(n=>n.Id)??[];var current=snapshot.Nodes.ToDictionary(n=>n.Id);
        foreach(var node in snapshot.Nodes)
        {
            if(!old.TryGetValue(node.Id,out var before)){changes.Add(new(node.Id,AccessibilityChangeKind.Added));continue;}
            if(node!=before)
            {
                changes.Add(new(node.Id,AccessibilityChangeKind.PropertiesChanged));
                if(node.State.HasFlag(AccessibilityState.Focused)!=before.State.HasFlag(AccessibilityState.Focused))changes.Add(new(node.Id,AccessibilityChangeKind.FocusChanged));
                if(node.LiveRegion!=AccessibilityLiveRegion.Off&&(node.Name!=before.Name||node.Value!=before.Value))changes.Add(new(node.Id,AccessibilityChangeKind.LiveRegionChanged));
            }
        }
        foreach(long removed in old.Keys.Except(current.Keys))changes.Add(new(removed,AccessibilityChangeKind.Removed));
        if(previous is not null&&!previous.Nodes.Select(n=>n.Id).SequenceEqual(snapshot.Nodes.Select(n=>n.Id)))changes.Add(new(0,AccessibilityChangeKind.StructureChanged));
        previous=snapshot;if(changes.Count==0)return;
        var update=new AccessibilityUpdate(snapshot,Array.AsReadOnly(changes.ToArray()));publishing=true;
        try{bridge?.Publish(update);Changed?.Invoke(update);}finally{publishing=false;}
    }
    internal void RefreshIfObserved(){if(!closed&&(bridge is not null||Changed is not null))Refresh();}
    internal void Close(){if(closed)return;try{DetachBridge();}finally{closed=true;widgets.Clear();previous=null;Changed=null;}}
    private static Rect Intersection(Rect a,Rect b)
    {float x=Math.Max(a.X,b.X),y=Math.Max(a.Y,b.Y);return new(x,y,Math.Max(0,Math.Min(a.X+a.Width,b.X+b.Width)-x),Math.Max(0,Math.Min(a.Y+a.Height,b.Y+b.Height)-y));}
}
