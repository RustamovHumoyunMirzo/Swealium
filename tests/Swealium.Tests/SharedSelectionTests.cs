using Swealium;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;
namespace Swealium.Tests;
internal static class SharedSelectionTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static Text Label(string content)=>new(){Content=content,Selectable=true,Width=250,Height=32};
    private static (Container Root,Text[] Labels,InputManager Input) Create()
    {
        var labels=new[]{Label("Alpha one"),Label("Beta two"),Label("Gamma three")};
        labels[1].Markup=TextMarkup.Html;labels[1].Content="<b>Beta</b> two";
        var root=new Container{Padding=new(12),Gap=8};foreach(var text in labels)root.Children.Add(text);
        LayoutEngine.Layout(root,new(280,180));return(root,labels,new InputManager{Root=root});
    }
    private static Point At(Text text,int index)
    {
        using var layout=new TextLayout(text.Content,text.Style,text.Options,text.ContentBounds.Width,text.Markup);
        var caret=layout.GetCaretRectangle(new(index));float x=text.ContentBounds.X+caret.X+.01f,y=text.ContentBounds.Y+caret.Y+caret.Height/2;
        for(Widget? node=text;node is not null;node=node.Parent){x+=node.Bounds.X;y+=node.Bounds.Y;}
        return new(x,y);
    }
    private static void Down(InputManager input,Point point,int count=1,KeyModifiers modifiers=KeyModifiers.None)=>input.ProcessPointer(new(PointerEventKind.Down,point,PointerButton.Primary,modifiers:modifiers,clickCount:count));
    private static void Up(InputManager input,Point point)=>input.ProcessPointer(new(PointerEventKind.Up,point,PointerButton.Primary));
    private static void Drag(InputManager input,Text from,int start,Text to,int end,int clicks=1)
    {Down(input,At(from,start),clicks);input.ProcessPointer(new(PointerEventKind.Move,At(to,end)));Up(input,At(to,end));}
    internal static void DragAndOwnership()
    {
        var (root,labels,input)=Create();var a=labels[0];var b=labels[1];var c=labels[2];
        Drag(input,a,2,c,4);
        Check(a.Selection==new TextSelection(2,7)&&b.Selection==new TextSelection(0,8)&&c.Selection==new TextSelection(0,4),"One drag distributes a contiguous range over three separate widgets");
        Check(input.TextSelection.SelectedText=="pha one\nBeta two\nGamm"&&a.SelectedText==input.TextSelection.SelectedText,"Shared text joins partial endpoints and intermediate text in tree order");
        Check(input.TextSelection.Ranges.Count==3,"Public range snapshot describes each selected text");
        Down(input,At(b,1));Up(input,At(b,1));Check(labels.All(text=>text.Selection.Length==0)&&input.TextSelection.SelectedText=="","A new collapsed selection clears every old highlight");
        b.Select(0,4);a.Select(0,5);Check(a.Selection.Length==5&&b.Selection.Length==0&&c.Selection.Length==0,"Programmatic selection also claims shared ownership");
        Drag(input,c,4,a,2);Check(input.TextSelection.SelectedText=="pha one\nBeta two\nGamm","Reverse dragging preserves document copy order");
        Drag(input,a,7,c,8,2);Check(input.TextSelection.SelectedText=="one\nBeta two\nGamma three","Double-click word drag spans independent text widgets");
        Drag(input,a,2,c,3,3);Check(input.TextSelection.SelectedText=="Alpha one\nBeta two\nGamma three","Triple-click line drag spans the document");
        a.Select(2,0);Down(input,At(c,4),modifiers:KeyModifiers.Shift);Up(input,At(c,4));
        Check(input.TextSelection.SelectedText=="pha one\nBeta two\nGamm","Shift-click extends the original anchor across widgets");
        var other=Create();other.Labels[0].Select(0,2);Check(input.TextSelection.SelectedText.Length>2&&other.Input.TextSelection.SelectedText=="Al","Independent windows/input trees retain independent document selections");
        b.IsVisible=false;input.Validate();Check(input.TextSelection.Ranges.Count==0&&a.Selection.Length==0&&c.Selection.Length==0,"Removing a selected document fragment invalidates the shared range");
        b.IsVisible=true;a.SelectAll();input.Root=null;Check(labels.All(text=>text.Selection.Length==0),"Replacing the root releases all highlights and selection references");
        a.Select(0,2);b.Select(0,2);input.Root=root;
        Check(input.TextSelection.SelectedText=="Be"&&a.Selection.Length==0,"Preselected widgets adopt one owner when attached to the shared tree");
    }
    internal static void KeyboardAndCopy()
    {
        var (root,labels,input)=Create();var a=labels[0];var b=labels[1];var c=labels[2];
        input.Focus(a);a.Select(a.Content.Length,0);input.ProcessKey(new(Key.Right,true,KeyModifiers.Shift));input.ProcessKey(new(Key.Right,true,KeyModifiers.Shift));
        Check(input.TextSelection.SelectedText=="\nB"&&input.FocusedWidget==b,"Shift-arrow continues across the shared text boundary");
        input.ProcessKey(new(Key.A,true,KeyModifiers.Control));Check(input.TextSelection.SelectedText=="Alpha one\nBeta two\nGamma three","Ctrl+A selects the read-only text document");
        input.ProcessKey(new(Key.Left,true));Check(labels.All(text=>text.Selection.Length==0)&&input.FocusedWidget==a,"Arrow collapses a shared selection at its document edge");
        var box=new TextBox{Text="editable",Height=40};root.Children.Add(box);box.SelectAll();a.Select(0,2);Check(box.Selection.Length==0,"Starting a text document selection clears a previous editable selection");
        input.Root=null;
        SwealiumApplication.Run(new(){Width=290,Height=220},window=>
        {
            window.Content=root;window.Input.Focus(a);Drag(window.Input,a,2,c,4);window.Input.ProcessKey(new(Key.C,true,KeyModifiers.Control));
            Check(Clipboard.GetText()=="pha one\nBeta two\nGamm","Ctrl+C copies the connected selection, not only the focused widget");
            c.Copy();Check(Clipboard.GetText()==window.Input.TextSelection.SelectedText,"Copy from any participating text uses the shared buffer");
            window.Input.Focus(null);window.Input.ProcessKey(new(Key.C,true,KeyModifiers.Control));Check(Clipboard.GetText()=="pha one\nBeta two\nGamm","Shared document copying does not depend on one text retaining keyboard focus");
            Down(window.Input,At(a,2),4);Up(window.Input,At(a,2));Check(window.Input.TextSelection.Ranges.Count==3,"Four clicks select all selectable text, excluding editable controls");
            window.Render();window.RequestClose();
        });
    }
}
