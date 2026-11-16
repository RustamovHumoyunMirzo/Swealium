using System.Collections.ObjectModel;
using Swealium.Graphics;
namespace Swealium.UI;

/// <summary>Reusable widget decoration. The first entry is the outermost effect.</summary>
public abstract class WidgetEffect
{
    public bool IsEnabled { get; set; } = true;
    /// <summary>Override when reading previously drawn pixels. Enables correct ancestor opacity isolation.</summary>
    public virtual bool ReadsBackdrop => false;
    /// <summary>Paint/sample the backdrop before any content effect layers open. Called in stack order.</summary>
    public virtual void PrepareDraw(DrawingContext context, Widget widget) { }
    /// <summary>Open drawing scopes around this effect and the remaining subtree. Disposed even on failure.</summary>
    public virtual IDisposable? Begin(DrawingContext context, Widget widget) => null;
    /// <summary>Draw behind the widget, in widget-local coordinates.</summary>
    public virtual void PreDraw(DrawingContext context, Widget widget) { }
    /// <summary>Draw above the widget, descendants, stroke and outline.</summary>
    public virtual void PostDraw(DrawingContext context, Widget widget) { }
}

/// <summary>Ordered, non-null effects. Structural mutation during this widget's render is rejected.</summary>
public sealed class WidgetEffectCollection : Collection<WidgetEffect>
{
    internal WidgetEffectCollection() { }
    private int rendering;
    internal void Enter() => rendering++;
    internal void Exit() => rendering--;
    private void Check() { if (rendering != 0) throw new InvalidOperationException("Change the effect stack outside its render callbacks."); }
    protected override void InsertItem(int index, WidgetEffect item) { Check(); ArgumentNullException.ThrowIfNull(item); base.InsertItem(index, item); }
    protected override void SetItem(int index, WidgetEffect item) { Check(); ArgumentNullException.ThrowIfNull(item); base.SetItem(index, item); }
    protected override void RemoveItem(int index) { Check(); base.RemoveItem(index); }
    protected override void ClearItems() { Check(); base.ClearItems(); }
}
