using Swealium.Graphics;
using Swealium.Input;
namespace Swealium.UI;

/// <summary>A plain-text editor widget. Editor is also reusable by custom widgets without inheritance.</summary>
public class TextBox : Widget
{
    public TextBox()
    {
        Editor=new(this);Padding=new(8);Radii=new(4);MinWidth=80;FocusStroke=new(1,new(60,110,220));
    }
    public TextEditor Editor {get;}
    protected override Stroke GetDefaultStroke()=>new(1,BorderColor);
    public string Text {get=>Editor.Text;set=>Editor.Text=value;}
    public TextStyle Style {get=>Editor.Style;set=>Editor.Style=value;}
    public bool Multiline {get=>Editor.Multiline;set=>Editor.Multiline=value;}
    public bool IsReadOnly {get=>Editor.IsReadOnly;set=>Editor.IsReadOnly=value;}
    public string Placeholder {get=>Editor.Placeholder;set=>Editor.Placeholder=value;}
    public Color PlaceholderColor {get=>Editor.PlaceholderColor;set=>Editor.PlaceholderColor=value;}
    public Color Background {get=>Editor.CandidateBackground;set=>Editor.CandidateBackground=value;}
    public Color BorderColor {get=>Editor.CandidateBorderColor;set=>Editor.CandidateBorderColor=value;}
    public Color SelectionColor {get=>Editor.SelectionColor;set=>Editor.SelectionColor=value;}
    public TextSelection Selection=>Editor.Selection;
    public int CaretIndex=>Editor.CaretIndex;
    public string SelectedText=>Editor.SelectedText;
    public string CompositionText=>Editor.CompositionText;
    public IReadOnlyList<string> Candidates=>Editor.Candidates;
    public int SelectedCandidate=>Editor.SelectedCandidate;
    public bool CandidatesHorizontal=>Editor.CandidatesHorizontal;
    public bool CanUndo=>Editor.CanUndo;
    public bool CanRedo=>Editor.CanRedo;
    public event Action? TextChanged {add=>Editor.TextChanged+=value;remove=>Editor.TextChanged-=value;}
    public event Action? SelectionChanged {add=>Editor.SelectionChanged+=value;remove=>Editor.SelectionChanged-=value;}
    public event Action? Submitted {add=>Editor.Submitted+=value;remove=>Editor.Submitted-=value;}
    public void Select(int start,int length)=>Editor.Select(start,length);
    public void SelectAll()=>Editor.SelectAll();
    public void ReplaceSelection(string value)=>Editor.ReplaceSelection(value);
    public void Copy()=>Editor.Copy();
    public void Cut()=>Editor.Cut();
    public void Paste()=>Editor.Paste();
    public void Undo()=>Editor.Undo();
    public void Redo()=>Editor.Redo();
    public void CancelComposition()=>Editor.CancelComposition();
    protected override Size MeasureContent(Size available)=>Editor.Measure(available);
    public override void Draw(DrawingContext context)
    {
        context.FillRectangle(new(0,0,ActualWidth,ActualHeight),Background,Radii);
        Editor.Draw(context);
    }
    internal void DrawCandidates(DrawingContext context,Size viewport)=>Editor.DrawCandidates(context,viewport);
}
