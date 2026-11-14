using Swealium.Graphics;
using Swealium.UI;
namespace Swealium.Input;

/// <summary>Routes framework input through a retained tree. Use only on the UI thread.</summary>
public sealed class InputManager
{
    private Widget? root;
    private Widget? captured;
    private readonly List<Widget> hovered = [];
    private Point lastPosition;
    private bool hasPointer;
    public Widget? Root
    {
        get => root;
        set
        {
            if (ReferenceEquals(root, value)) return;
            if (value?.Parent is not null) throw new InvalidOperationException("The root cannot have a parent.");
            if (value?.RootOwner is not null) throw new InvalidOperationException("The widget is already another input tree's root.");
            Cancel();
            if (root is not null) root.RootOwner = null;
            root = value;
            if (root is not null) root.RootOwner = this;
        }
    }
    public Widget? FocusedWidget { get; private set; }
    public Widget? CapturedWidget => captured;
    internal event Action? FocusChanged;
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
        if (widget is Container container)
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
        FocusedWidget = widget;
        if (previous is not null) { previous.IsFocused = false; previous.RaiseFocus(); }
        if (widget is not null) { widget.IsFocused = true; widget.RaiseFocus(); }
        FocusChanged?.Invoke();
        return true;
    }
    internal void Validate()
    {
        if (CanProcessInput?.Invoke() == false) { Cancel(); return; }
        if (FocusedWidget is not null && (!IsAttached(FocusedWidget) || !FocusedWidget.IsFocusable)) Focus(null);
        if (captured is not null && !IsAttached(captured)) CancelCapture();
        foreach (var widget in hovered.ToArray())
            if (!IsAttached(widget))
            {
                hovered.Remove(widget);
                widget.IsPointerOver = false;
                Deliver(widget, new PointerEvent(PointerEventKind.Leave, lastPosition));
            }
        if (hasPointer) UpdateHover(HitTest(lastPosition));
    }
    public void ProcessPointer(PointerEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (e.Kind == PointerEventKind.Cancel) { Cancel(); return; }
        if (CanProcessInput?.Invoke() == false) return;
        if (e.Kind == PointerEventKind.Leave) { hasPointer = false; ClearHover(); return; }
        lastPosition = e.WindowPosition;
        hasPointer = true;
        Validate();
        Widget? hit = HitTest(e.WindowPosition);
        UpdateHover(hit);
        Widget? target = captured ?? hit;
        if (e.Kind == PointerEventKind.Down && e.Button == PointerButton.Primary)
        {
            CancelCapture();
            captured = hit;
            target = hit;
            Widget? focusTarget = hit;
            while (focusTarget is not null && !focusTarget.IsFocusable) focusTarget = focusTarget.Parent;
            Focus(focusTarget);
        }
        Widget? pressed = captured;
        bool release = e.Kind == PointerEventKind.Up && e.Button == PointerButton.Primary;
        if (release) captured = null;
        if (target is not null) Route(target, e, widget => Deliver(widget, e));
        if (release && !e.Handled && pressed is not null && ReferenceEquals(pressed, hit) && IsAttached(pressed))
            pressed.PerformClick();
    }
    public void ProcessKey(KeyEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (CanProcessInput?.Invoke() == false) return;
        Validate();
        if (FocusedWidget is { } focused) Route(focused, e, widget => widget.RaiseKey(e));
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
    private void UpdateHover(Widget? hit)
    {
        var next = Path(hit);
        foreach (var widget in hovered.ToArray())
            if (!next.Contains(widget))
            {
                widget.IsPointerOver = false;
                Deliver(widget, new PointerEvent(PointerEventKind.Leave, lastPosition));
            }
        foreach (var widget in next)
            if (!hovered.Contains(widget))
            {
                widget.IsPointerOver = true;
                Deliver(widget, new PointerEvent(PointerEventKind.Enter, lastPosition));
            }
        hovered.Clear();
        hovered.AddRange(next);
    }
    private void ClearHover()
    {
        var previous = hovered.ToArray();
        hovered.Clear();
        foreach (var widget in previous)
        {
            widget.IsPointerOver = false;
            Deliver(widget, new PointerEvent(PointerEventKind.Leave, lastPosition));
        }
    }
    private void CancelCapture()
    {
        var previous = captured;
        captured = null;
        if (previous is not null) Deliver(previous, new PointerEvent(PointerEventKind.Cancel, lastPosition));
    }
    /// <summary>Release capture, hover, and keyboard focus after window deactivation or root replacement.</summary>
    public void Cancel() { hasPointer = false; CancelCapture(); ClearHover(); Focus(null); }
}

