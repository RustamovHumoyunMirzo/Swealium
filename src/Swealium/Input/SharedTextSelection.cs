using Swealium.Graphics;
using Swealium.UI;
namespace Swealium.Input;

public sealed record TextSelectionPart(Text Widget,TextSelection Range);

/// <summary>One read-only document selection per input tree, in visual-tree insertion order.</summary>
public sealed class SharedTextSelection
{
    private readonly InputManager input;
    private readonly List<TextSelectionPart> parts=[];
    private Text? anchorWidget,focusWidget;
    private int anchorIndex,focusIndex;
    private TextEditor? dragging;
    private PointerId pointer;
    private int unit;
    private TextSelection anchorRange;
    private bool updating;
    internal SharedTextSelection(InputManager input)=>this.input=input;
    public IReadOnlyList<TextSelectionPart> Ranges {get{Validate();return Array.AsReadOnly(parts.ToArray());}}
    public string SelectedText {get{Validate();return string.Join("\n",parts.Select(part=>part.Widget.TextEditor!.Text.Substring(part.Range.Start,part.Range.Length)));}}
    public event Action? Changed;
    private List<Text> Document()
    {
        List<Text> result=[];
        void Visit(Widget widget)
        {
            if(!widget.IsInteractable||!widget.IsHitTestVisible)return;
            if(widget is Text{Selectable:true,TextEditor:{IsEnabled:true}} text)result.Add(text);
            if(widget is Container container)foreach(var child in container.Children)Visit(child);
        }
        if(input.Root is {} root)Visit(root);
        return result;
    }
    internal void Claim(TextEditor editor)
    {
        if(updating)return;
        if(editor.Owner is not Text text){Clear();ClearOtherEditors(editor);return;}
        if(!Document().Contains(text))return;
        var selection=editor.Selection;
        anchorWidget=focusWidget=text;anchorIndex=selection.Start;focusIndex=selection.Start+selection.Length;
        anchorRange=selection;Apply();ClearOtherEditors(editor);
    }
    private void ClearOtherEditors(TextEditor keep)
    {
        void Visit(Widget widget)
        {
            if(widget.TextEditor is {} editor&&editor!=keep&&widget is not Text&&editor.Selection.Length>0)editor.SetSharedRange(editor.CaretIndex,editor.CaretIndex);
            if(widget is Container container)foreach(var child in container.Children)Visit(child);
        }
        if(input.Root is {} root)Visit(root);
    }
    internal void Begin(TextEditor editor,PointerEvent e,int selectionUnit)
    {
        if(editor.Owner is not Text text)return;
        bool extend=e.Modifiers.HasFlag(KeyModifiers.Shift)&&anchorWidget is not null;
        if(!extend){anchorWidget=text;anchorRange=editor.Selection;anchorIndex=anchorRange.Start;}
        dragging=editor;pointer=e.PointerId;unit=selectionUnit;
        focusWidget=text;focusIndex=editor.CaretIndex;
        if(unit>=4){SelectAll();return;}
        if(extend)ExtendTo(text,editor.CaretIndex);else Apply();
        ClearOtherEditors(editor);
    }
    internal void Drag(TextEditor editor,PointerEvent e)
    {
        if(dragging!=editor||pointer!=e.PointerId||anchorWidget is null)return;
        var document=Document();if(!document.Contains(anchorWidget)){Clear();return;}
        if(unit>=4)return;
        Text? target=input.HitTest(e.WindowPosition) as Text;
        if(target is null||!document.Contains(target))
            target=document.Where(text=>VisibleBounds(text) is {Width:>0,Height:>0}).OrderBy(text=>Distance(VisibleBounds(text),e.WindowPosition)).FirstOrDefault();
        if(target is null)return;
        var local=target.ToLocal(e.WindowPosition);
        bool backwards=document.IndexOf(target)<document.IndexOf(anchorWidget);
        if(unit==1)ExtendTo(target,target.TextEditor!.HitIndex(local));
        else
        {
            var range=target.TextEditor!.RangeAt(local,unit);
            if(target==anchorWidget)backwards=range.Start<anchorRange.Start;
            anchorIndex=backwards?anchorRange.Start+anchorRange.Length:anchorRange.Start;
            ExtendTo(target,backwards?range.Start:range.Start+range.Length);
        }
    }
    internal bool ProcessKey(KeyEvent e)
    {
        if(!e.IsDown||anchorWidget is null||focusWidget is null)return false;
        if(e.Key is not (Key.Left or Key.Right or Key.Home or Key.End or Key.Up or Key.Down or Key.PageUp or Key.PageDown))return false;
        var document=Document();int index=document.IndexOf(focusWidget);if(index<0){Clear();return false;}
        bool shift=e.Modifiers.HasFlag(KeyModifiers.Shift),command=(e.Modifiers&(KeyModifiers.Control|KeyModifiers.Super))!=0&&!e.Modifiers.HasFlag(KeyModifiers.Alt);
        bool backwards=e.Key is Key.Left or Key.Up or Key.PageUp or Key.Home;
        Text target=focusWidget;int position=focusIndex;
        if(!shift&&e.Key is Key.Left or Key.Right&&SelectedText.Length>0)
        {
            var edge=backwards?parts[0]:parts[^1];target=edge.Widget;position=edge.Range.Start+(backwards?0:edge.Range.Length);
        }
        else if(command&&e.Key is Key.Home or Key.End)
        {target=backwards?document[0]:document[^1];position=backwards?0:target.TextEditor!.Text.Length;}
        else
        {
            int next=target.TextEditor!.NavigationIndex(position,e.Key,command);
            bool cross=e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.PageUp or Key.PageDown;
            if(cross&&next==position&&((backwards&&position==0)||(!backwards&&position==target.TextEditor.Text.Length)))
            {
                int adjacent=index+(backwards?-1:1);
                if(adjacent>=0&&adjacent<document.Count){target=document[adjacent];next=backwards?target.TextEditor!.Text.Length:0;}
            }
            position=next;
        }
        dragging=null;
        if(!shift){anchorWidget=target;anchorIndex=position;anchorRange=new(position,0);}
        ExtendTo(target,position);input.Focus(target);return true;
    }
    private static double Distance(Rect bounds,Point point)
    {
        double dx=Math.Max(bounds.X-point.X,Math.Max(0,point.X-bounds.X-bounds.Width));
        double dy=Math.Max(bounds.Y-point.Y,Math.Max(0,point.Y-bounds.Y-bounds.Height));
        return dx*dx+dy*dy;
    }
    private static Rect VisibleBounds(Text text)
    {
        Rect bounds=text.TransformedBounds;
        for(Widget? node=text;node is not null;node=node.Parent)
        {
            if(node.ClipToBounds)bounds=Intersect(bounds,node.TransformedBounds);
            if(node is Container{ChildrenClip:{} clip})bounds=Intersect(bounds,node.ToWindow(clip));
        }
        return bounds;
    }
    private static Rect Intersect(Rect a,Rect b)
    {float x=Math.Max(a.X,b.X),y=Math.Max(a.Y,b.Y);return new(x,y,Math.Max(0,Math.Min(a.X+a.Width,b.X+b.Width)-x),Math.Max(0,Math.Min(a.Y+a.Height,b.Y+b.Height)-y));}
    internal void End(TextEditor editor,PointerId id){if(dragging==editor&&pointer==id)dragging=null;}
    private void ExtendTo(Text text,int index){focusWidget=text;focusIndex=index;Apply();}
    private void Apply()
    {
        if(updating||anchorWidget is null||focusWidget is null)return;
        var document=Document();int a=document.IndexOf(anchorWidget),b=document.IndexOf(focusWidget);
        if(a<0||b<0){Clear();return;}
        int start=anchorIndex,end=focusIndex;
        if(a>b||(a==b&&start>end)){(a,b)=(b,a);(start,end)=(end,start);}
        updating=true;
        try
        {
            var previous=parts.Select(p=>p.Widget).ToArray();var desired=new List<TextSelectionPart>();
            for(int i=0;i<document.Count;i++)
            {
                var text=document[i];var editor=text.TextEditor!;
                int first=i==a?start:0,last=i==b?end:editor.Text.Length;
                if(i<a||i>b)continue;
                first=Math.Clamp(first,0,editor.Text.Length);last=Math.Clamp(last,first,editor.Text.Length);
                desired.Add(new(text,new(first,last-first)));
            }
            parts.Clear();parts.AddRange(desired);
            var ranges=desired.ToDictionary(part=>part.Widget,part=>part.Range);
            foreach(var text in document)
            {
                var editor=text.TextEditor!;
                if(ranges.TryGetValue(text,out var range))editor.SetSharedRange(range.Start,range.Start+range.Length);
                else if(editor.Selection.Length>0)editor.SetSharedRange(editor.CaretIndex,editor.CaretIndex);
            }
            foreach(var removed in previous.Except(document))removed.TextEditor?.SetSharedRange(0,0);
        }
        finally {updating=false;}
        Changed?.Invoke();
    }
    public void Clear()
    {
        if(updating)return;
        var previous=parts.Select(part=>part.Widget).Concat(Document().Where(text=>text.Selection.Length>0)).Distinct().ToArray();
        bool changed=previous.Length>0;updating=true;
        try
        {
            parts.Clear();anchorWidget=focusWidget=null;dragging=null;
            foreach(var text in previous)text.TextEditor?.SetSharedRange(0,0);
        }
        finally {updating=false;}
        if(changed)Changed?.Invoke();
    }
    public void SelectAll()
    {
        var document=Document();if(document.Count==0){Clear();return;}
        anchorWidget=document[0];anchorIndex=0;focusWidget=document[^1];focusIndex=focusWidget.TextEditor!.Text.Length;Apply();
        ClearOtherEditors(anchorWidget.TextEditor!);
    }
    public void Copy(){string selected=SelectedText;if(selected.Length>0)Clipboard.SetText(selected);}
    internal void Invalidate(TextEditor editor){if(parts.Any(part=>part.Widget==editor.Owner))Clear();}
    internal void Validate(bool reconcile=false)
    {
        if(updating)return;
        var document=Document();
        if(parts.Count==0&&anchorWidget is null&&document.LastOrDefault(text=>text.Selection.Length>0) is {} preselected){Claim(preselected.TextEditor!);return;}
        if(parts.Any(part=>!document.Contains(part.Widget)||part.Range.Start+part.Range.Length>part.Widget.TextEditor!.Text.Length)){Clear();return;}
        if(reconcile)foreach(var text in document)
            if(text.Selection.Length>0&&!parts.Any(part=>part.Widget==text))text.TextEditor!.SetSharedRange(text.TextEditor.CaretIndex,text.TextEditor.CaretIndex);
    }
}
