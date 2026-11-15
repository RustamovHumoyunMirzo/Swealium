using Swealium.Graphics;
using Swealium.UI;
namespace Swealium.Input;

/// <summary>Routes framework input through a retained tree. Use only on the UI thread.</summary>
public sealed class InputManager
{
    public InputManager(){TextSelection=new(this);}
    public SharedTextSelection TextSelection {get;}
    private Widget? root;
    private sealed class Contact(PointerId id)
    {
        internal readonly PointerId Id=id;
        internal Widget? Captured,Pressed;
        internal readonly List<Widget> Hovered=[];
        internal Point Position;
        internal PointerButtons Buttons;
        internal float Pressure;
        internal bool Eraser;
        internal float TiltX,TiltY;
        internal bool Primary=true,Present=true;
        internal bool Cancelled;
    }
    private readonly Dictionary<PointerId,Contact> pointers=[];
    private PointerId lastMouse=PointerId.Mouse;
    private readonly HashSet<Key> heldModifiers=[];
    private Cursor defaultCursor=Cursor.Default;
    public Cursor DefaultCursor {get=>defaultCursor;set{ArgumentNullException.ThrowIfNull(value);value.Verify();defaultCursor=value;}}
    public Cursor CurrentCursor
    {
        get
        {
            pointers.TryGetValue(lastMouse,out var mouse);
            Widget? node=mouse?.Captured??mouse?.Hovered.FirstOrDefault();
            for(;node is not null;node=node.Parent)if(node.Cursor is {IsDisposed:false} cursor)return cursor;
            return DefaultCursor.IsDisposed?Cursor.Default:DefaultCursor;
        }
    }
    public KeyModifiers Modifiers { get; private set; }
    public Widget? Root
    {
        get => root;
        set
        {
            if (ReferenceEquals(root, value)) return;
            if (value?.Parent is not null) throw new InvalidOperationException("The root cannot have a parent.");
            if (value?.RootOwner is not null) throw new InvalidOperationException("The widget is already another input tree's root.");
            TextSelection.Clear();
            Cancel();
            if (root is not null) root.RootOwner = null;
            root = value;
            if (root is not null) root.RootOwner = this;
        }
    }
    public Widget? FocusedWidget { get; private set; }
    public Widget? CapturedWidget => GetCapturedWidget(lastMouse)??pointers.Values.FirstOrDefault(p=>p.Primary&&p.Captured is not null)?.Captured;
    public IReadOnlyList<PointerInfo> Pointers=>Array.AsReadOnly(pointers.Values.Select(Snapshot).ToArray());
    public PointerInfo? GetPointer(PointerId id)=>pointers.TryGetValue(id,out var state)?Snapshot(state):null;
    public Widget? GetCapturedWidget(PointerId id)=>pointers.TryGetValue(id,out var state)?state.Captured:null;
    public event Action<PointerCaptureChangedEvent>? PointerCaptureChanged;
    private static PointerInfo Snapshot(Contact p)=>new(p.Id,p.Position,p.Buttons,p.Pressure,p.Primary,p.Buttons!=PointerButtons.None,p.Captured,p.Eraser,p.TiltX,p.TiltY);
    internal event Action? FocusChanged;
    internal event Action? CompositionCancellationRequested;
    internal void CancelNativeComposition() => CompositionCancellationRequested?.Invoke();
    internal Func<bool>? CanProcessInput { get; set; }

    public Widget? HitTest(Point point)
    {
        if (root is null || CanProcessInput?.Invoke() == false) return null;
        LayoutEngine.EnsureLayout(root);
        return HitTest(root, point);
    }
    private static Widget? HitTest(Widget widget, Point parentPosition)
    {
        if (!widget.IsVisible || !widget.IsEnabled || !widget.IsHitTestVisible) return null;
        Point local = new(parentPosition.X - widget.Bounds.X, parentPosition.Y - widget.Bounds.Y);
        bool inside = widget.ContainsPoint(local);
        if (widget.ClipToBounds && !inside) return null;
        if (widget is Container container && container.CanHitChildren(local))
        {
            var children = container.PaintOrder;
            for (int i = children.Length - 1; i >= 0; i--)
                if (HitTest(children[i], local) is { } hit) return hit;
        }
        return inside ? widget : null;
    }
    private bool IsAttached(Widget? widget)
    {
        if (widget is null || !widget.IsInteractable) return false;
        while (widget.Parent is not null) widget = widget.Parent;
        return ReferenceEquals(widget, root);
    }
    public bool Focus(Widget? widget)
    {
        if (widget is not null && CanProcessInput?.Invoke() == false) return false;
        if (widget is not null && (!widget.IsFocusable || !IsAttached(widget))) return false;
        if (ReferenceEquals(FocusedWidget, widget)) return true;
        Widget? previous = FocusedWidget;
        if (previous?.AcceptsTextInput == true) CancelNativeComposition();
        FocusedWidget = widget;
        if (previous is not null) { previous.IsFocused = false; previous.RaiseFocus(); }
        if (widget is not null) { widget.IsFocused = true; widget.RaiseFocus(); }
        if (widget is not null)
            for (Container? ancestor=widget.Parent;ancestor is not null;ancestor=ancestor.Parent)
                if (ancestor is ScrollContainer scroll) scroll.BringIntoView(widget);
        FocusChanged?.Invoke();
        return true;
    }
    internal void Validate()
    {
        if (CanProcessInput?.Invoke() == false) { Cancel(); return; }
        TextSelection.Validate(true);
        if (FocusedWidget is not null && (!IsAttached(FocusedWidget) || !FocusedWidget.IsFocusable)) Focus(null);
        foreach(var pointer in pointers.Values.ToArray())
        {
            if(pointer.Captured is not null&&!IsAttached(pointer.Captured))ChangeCapture(pointer,null,true);
            UpdateHover(pointer,pointer.Present?HitTest(pointer.Position):null);
        }
    }
    public void ProcessPointer(PointerEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (e.Kind == PointerEventKind.Cancel) {CancelPointer(e.PointerId);return;}
        if (CanProcessInput?.Invoke() == false) return;
        if(!Enum.IsDefined(e.DeviceKind)||!float.IsFinite(e.WindowPosition.X)||!float.IsFinite(e.WindowPosition.Y)||!float.IsFinite(e.Pressure)||e.Pressure<0||e.Pressure>1||!float.IsFinite(e.TiltX)||!float.IsFinite(e.TiltY))throw new ArgumentOutOfRangeException(nameof(e));
        if (!e.HasExplicitModifiers) e.Modifiers=Modifiers;
        Validate();
        if(!pointers.TryGetValue(e.PointerId,out var state))
        {
            state=new(e.PointerId);pointers.Add(e.PointerId,state);
            state.Primary=e.HasExplicitPrimary?e.IsPrimary:e.DeviceKind==PointerDeviceKind.Mouse||!pointers.Values.Any(p=>p!=state&&p.Id.Device==e.DeviceKind&&p.Buttons!=PointerButtons.None);
        }
        if(e.DeviceKind==PointerDeviceKind.Mouse)lastMouse=e.PointerId;
        if(e.Kind==PointerEventKind.Leave){state.Present=false;UpdateHover(state,null);return;}
        state.Position=e.WindowPosition;state.Pressure=e.Pressure;state.Present=true;state.Eraser=e.IsEraser;state.TiltX=e.TiltX;state.TiltY=e.TiltY;
        if(e.Kind==PointerEventKind.Down&&state.Buttons==PointerButtons.None&&!e.HasExplicitPrimary)
            state.Primary=e.DeviceKind==PointerDeviceKind.Mouse||!pointers.Values.Any(p=>p!=state&&p.Id.Device==e.DeviceKind&&p.Buttons!=PointerButtons.None);
        e.IsPrimary=e.HasExplicitPrimary?e.IsPrimary:state.Primary;
        if(e.Kind==PointerEventKind.Down)state.Buttons|=ButtonFlag(e.Button);
        if(e.Kind==PointerEventKind.Up)state.Buttons&=~ButtonFlag(e.Button);
        e.Buttons=state.Buttons;
        Widget? hit = HitTest(e.WindowPosition);
        UpdateHover(state,hit);
        Widget? target = state.Captured ?? hit;
        if (e.Kind == PointerEventKind.Down && e.Button == PointerButton.Primary)
        {
            if(e.IsPrimary&&hit is not Text{Selectable:true})TextSelection.Clear();
            ChangeCapture(state,null,true);state.Pressed=hit;ChangeCapture(state,hit,false);
            target = hit;
            Widget? focusTarget = hit;
            while (focusTarget is not null && !focusTarget.IsFocusable) focusTarget = focusTarget.Parent;
            if(e.IsPrimary)Focus(focusTarget);
        }
        Widget? pressed = state.Pressed;
        bool release = e.Kind == PointerEventKind.Up && e.Button == PointerButton.Primary;
        if (release){state.Pressed=null;ChangeCapture(state,null,false);}
        if (target is not null) Route(target, e, widget => Deliver(widget, e));
        if (release && !state.Cancelled && !e.Handled && pressed is not null && ReferenceEquals(pressed, hit) && IsAttached(pressed))
            pressed.PerformClick();
        if(release&&e.DeviceKind==PointerDeviceKind.Touch){UpdateHover(state,null);pointers.Remove(e.PointerId);}
    }
    public void ProcessKey(KeyEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (CanProcessInput?.Invoke() == false) return;
        if (Modifier(e.Key)!=KeyModifiers.None)
        {
            if (e.IsDown) heldModifiers.Add(e.Key);else heldModifiers.Remove(e.Key);
        }
        e.Modifiers |= heldModifiers.Aggregate(KeyModifiers.None,(value,key)=>value|Modifier(key));
        Modifiers=e.Modifiers;
        Validate();
        if (FocusedWidget is { } focused) Route(focused, e, widget => widget.RaiseKey(e));
        bool command=(e.Modifiers&(KeyModifiers.Control|KeyModifiers.Super))!=0&&!e.Modifiers.HasFlag(KeyModifiers.Alt);
        if(!e.Handled&&e.IsDown&&command&&e.Key==Key.C&&TextSelection.SelectedText.Length>0){TextSelection.Copy();e.Handled=true;}
        if (!e.Handled && e.IsDown && !e.IsRepeat && e.Key == Key.Tab)
        {
            List<Widget> candidates = [];
            if (root is not null) CollectFocusable(root, candidates);
            if (candidates.Count > 0)
            {
                int current = FocusedWidget is null ? -1 : candidates.IndexOf(FocusedWidget);
                bool reverse = e.Modifiers.HasFlag(KeyModifiers.Shift);
                int next = current < 0 ? (reverse ? candidates.Count - 1 : 0) : (current + (reverse ? -1 : 1) + candidates.Count) % candidates.Count;
                Focus(candidates[next]);
                e.Handled = true;
            }
        }
    }
    public void ProcessText(TextInputEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (CanProcessInput?.Invoke() == false) return;
        Validate();
        if (FocusedWidget is { AcceptsTextInput: true } focused)
            Route(focused, e, widget => widget.RaiseText(e));
    }
    public void ProcessComposition(TextCompositionEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (CanProcessInput?.Invoke() == false) return;
        Validate();
        if (FocusedWidget is { AcceptsTextInput: true } focused) Route(focused, e, widget => widget.RaiseComposition(e));
    }
    public void ProcessCandidates(TextCandidatesEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (CanProcessInput?.Invoke() == false) return;
        Validate();
        if (FocusedWidget is { AcceptsTextInput: true } focused) Route(focused, e, widget => widget.RaiseCandidates(e));
    }
    private static void CollectFocusable(Widget widget, List<Widget> result)
    {
        if (!widget.IsVisible || !widget.IsEnabled) return;
        if (widget.IsFocusable) result.Add(widget);
        if (widget is Container container)
            foreach (var child in container.Children) CollectFocusable(child, result);
    }
    private static List<Widget> Path(Widget? widget)
    {
        List<Widget> path = [];
        for (; widget is not null; widget = widget.Parent) path.Add(widget);
        return path;
    }
    private static void Route(Widget target, InputEvent e, Action<Widget> deliver)
    {
        e.Source = target;
        foreach (var widget in Path(target))
        {
            if (e.Handled) break;
            e.CurrentTarget = widget;
            deliver(widget);
        }
    }
    private static void Deliver(Widget widget, PointerEvent e)
    {
        e.Source ??= widget;
        e.CurrentTarget = widget;
        e.Position = widget.ToLocal(e.WindowPosition);
        widget.RaisePointer(e);
    }
    private void UpdateHover(Contact pointer,Widget? hit)
    {
        var next = Path(hit);
        var previous=pointer.Hovered.ToArray();pointer.Hovered.Clear();pointer.Hovered.AddRange(next);
        foreach (var widget in previous)
            if (!next.Contains(widget))
            {
                widget.IsPointerOver = pointers.Values.Any(p=>p.Hovered.Contains(widget));
                Deliver(widget, PointerNotification(pointer,PointerEventKind.Leave));
            }
        foreach (var widget in next)
            if (!previous.Contains(widget))
            {
                widget.IsPointerOver = true;
                Deliver(widget, PointerNotification(pointer,PointerEventKind.Enter));
            }
    }
    private PointerEvent PointerNotification(Contact pointer,PointerEventKind kind)=>new(kind,pointer.Position,modifiers:Modifiers,pointerId:pointer.Id,pressure:pointer.Pressure,isPrimary:pointer.Primary,isEraser:pointer.Eraser,tiltX:pointer.TiltX,tiltY:pointer.TiltY){Buttons=pointer.Buttons};
    private void ChangeCapture(Contact pointer,Widget? target,bool cancel)
    {
        var previous=pointer.Captured;if(ReferenceEquals(previous,target))return;
        pointer.Captured=target;
        if(cancel){pointer.Pressed=null;if(previous is not null)Deliver(previous,PointerNotification(pointer,PointerEventKind.Cancel));}
        PointerCaptureChanged?.Invoke(new(pointer.Id,previous,target));
    }
    public bool CapturePointer(PointerId id,Widget widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        if(!IsAttached(widget)||CanProcessInput?.Invoke()==false||!pointers.TryGetValue(id,out var pointer))return false;
        ChangeCapture(pointer,widget,true);return true;
    }
    public void ReleasePointerCapture(PointerId id){if(pointers.TryGetValue(id,out var pointer))ChangeCapture(pointer,null,true);}
    public void CancelPointer(PointerId id)
    {
        if(!pointers.TryGetValue(id,out var pointer))return;
        pointer.Cancelled=true;
        ChangeCapture(pointer,null,true);UpdateHover(pointer,null);pointers.Remove(id);
    }
    /// <summary>Release capture, hover, and keyboard focus after window deactivation or root replacement.</summary>
    public void Cancel() {heldModifiers.Clear();Modifiers=KeyModifiers.None;foreach(var id in pointers.Keys.ToArray())CancelPointer(id);Focus(null);}
    private static PointerButtons ButtonFlag(PointerButton button)=>button switch
    {PointerButton.Primary=>PointerButtons.Primary,PointerButton.Middle=>PointerButtons.Middle,PointerButton.Secondary=>PointerButtons.Secondary,PointerButton.Back=>PointerButtons.Back,PointerButton.Forward=>PointerButtons.Forward,_=>PointerButtons.None};
    private static KeyModifiers Modifier(Key key)=>key switch
    {
        Key.LeftShift or Key.RightShift=>KeyModifiers.Shift,
        Key.LeftControl or Key.RightControl=>KeyModifiers.Control,
        Key.LeftAlt or Key.RightAlt=>KeyModifiers.Alt,
        Key.LeftSuper or Key.RightSuper=>KeyModifiers.Super,
        _=>KeyModifiers.None
    };
}

