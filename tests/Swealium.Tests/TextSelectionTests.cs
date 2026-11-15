using Swealium;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.Native;
using Swealium.UI;
using Swealium.Accessibility;
using Bitmap=Swealium.Graphics.Bitmap;
namespace Swealium.Tests;
internal static class TextSelectionTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static Point Character(TextLayout layout,int index,Point origin=default)
    {
        var a=layout.GetCaretRectangle(new(index));var b=layout.GetCaretRectangle(new(Math.Min(layout.PlainText.Length,index+1)));
        return new(origin.X+a.X+(b.X-a.X)*.75f,origin.Y+a.Y+a.Height/2);
    }
    private static void Click(InputManager input,Point point,int count=0)
    {input.ProcessPointer(new(PointerEventKind.Down,point,PointerButton.Primary,clickCount:count));input.ProcessPointer(new(PointerEventKind.Up,point,PointerButton.Primary,clickCount:count));}
    internal static void RangesAndClicks()
    {
        using(var words=new TextLayout("hello,world. foo_bar! e\u0301clair 😀 中文\nnext"))
        {
            Check(words.GetWordRange(3)==new TextSelection(0,5),"Word ends at comma");
            Check(words.GetWordRange(6)==new TextSelection(6,5),"Word starts after comma and ends at period");
            Check(words.GetWordRange(15)==new TextSelection(13,3),"Underscore is punctuation boundary");
            Check(words.GetWordRange(5)==new TextSelection(5,1),"Delimiter itself is selected independently");
            Check(words.GetWordRange(22).Length==7,"Combining sequence remains with its Unicode word");
        }
        var box=new TextBox{Multiline=true,Text="hello,world. next\nsecond line",Width=340,Height=110};
        LayoutEngine.Layout(box,new(340,110));var input=new InputManager{Root=box};
        using var layout=new TextLayout(box.Text,box.Style,new(){Wrapping=TextWrapping.Word},box.ContentBounds.Width);
        var point=Character(layout,2,new(box.ContentBounds.X,box.ContentBounds.Y));
        Click(input,point);Check(box.Selection.Length==0,"Single click places caret");
        Click(input,point);Check(box.SelectedText=="hello","Double click selects whole word even right half of a glyph");
        Click(input,point);Check(box.SelectedText=="hello,world. next","Triple click selects visual line: ["+box.SelectedText.Replace("\n","<LF>")+"] "+box.Selection);
        Click(input,point);Check(box.SelectedText==box.Text,"Fourth click selects all");
        Click(input,Character(layout,7,new(8,8)),2);Check(box.SelectedText=="world","Explicit host click count is honored");
        input.ProcessPointer(new(PointerEventKind.Down,point,PointerButton.Primary,clickCount:2));
        var end=Character(layout,14,new(8,8));input.ProcessPointer(new(PointerEventKind.Move,end));
        Check(box.SelectedText=="hello,world. next","Double-click drag expands by whole words");input.ProcessPointer(new(PointerEventKind.Up,end,PointerButton.Primary));
        input.ProcessPointer(new(PointerEventKind.Down,end,PointerButton.Primary,clickCount:2));input.ProcessPointer(new(PointerEventKind.Move,point));
        Check(box.SelectedText=="hello,world. next","Word drag works backwards");input.Cancel();
        var native=new Sdl.Event{Type=0x401,Button=1,Clicks=3,MouseX=point.X,MouseY=point.Y};SdlInput.Dispatch(native,input);
        Check(box.SelectedText=="hello,world. next","SDL click count decodes at native ABI offset");input.Cancel();
        var narrow=new TextBox{Multiline=true,Text="alpha beta gamma delta",Width=95,Height=150};LayoutEngine.Layout(narrow,new(95,150));
        using var wrapped=new TextLayout(narrow.Text,narrow.Style,new(){Wrapping=TextWrapping.Word},narrow.ContentBounds.Width);
        var target=Character(wrapped,12,new(8,8));Click(new InputManager{Root=narrow},target,3);
        Check(narrow.Selection==wrapped.GetLineRange(wrapped.HitTest(new(target.X-8,target.Y-8))),"Triple click uses wrapped visual line and caret affinity");
    }
    internal static void SelectableText()
    {
        var text=new Text{Content="<b>Hello</b>, <i>world</i>!",Markup=TextMarkup.Html,Selectable=true,Width=300,Height=65,Options=new(){VerticalAlignment=TextVerticalAlignment.Center}};
        LayoutEngine.Layout(text,new(300,65));var input=new InputManager{Root=text};Check(input.Focus(text),"Selectable text participates in keyboard focus");
        text.Select(0,5);Check(text.SelectedText=="Hello","HTML selection uses decoded plain text indices");
        input.ProcessText(new("mutate"));input.ProcessKey(new(Key.Backspace,true));input.ProcessKey(new(Key.X,true,KeyModifiers.Control));
        Check(text.Content=="<b>Hello</b>, <i>world</i>!"&&text.SelectedText=="Hello","Selectable normal text stays read-only");
        using var pixels=new Bitmap(300,65);pixels.Draw(text.Draw);
        Check(pixels.CopyPixels().Where((_,i)=>i%4==3).Any(a=>a>0),"Selectable HTML text draws glyphs and selection");
        using var layout=new TextLayout(text.Content,text.Style,text.Options,300,text.Markup);
        var point=Character(layout,8,new(0,(65-layout.Size.Height)/2));Click(input,point,2);Check(text.SelectedText=="world","Selectable HTML word hit testing respects vertical alignment");
        text.Content="changed";Check(text.TextEditor!.Text=="changed"&&text.Selection.Length==0,"Content changes synchronize and clear selection");
        text.Selectable=false;input.ProcessPointer(new(PointerEventKind.Down,new(5,5),PointerButton.Primary));Check(!text.IsFocusable&&!text.AcceptsTextInput&&text.Selection.Length==0,"Disabling selectable removes editing interaction");
        text.Selectable=true;text.SelectAll();Check(text.SelectedText=="changed","Selectable can be enabled again");
    }
    internal sealed class CustomEditor:Widget
    {
        internal CustomEditor(){Editor=new(this){Multiline=true};Padding=new(10);}
        internal TextEditor Editor {get;}
        protected override Size MeasureContent(Size size)=>Editor.Measure(size);
        public override void Draw(DrawingContext drawing){drawing.FillRectangle(new(0,0,ActualWidth,ActualHeight),new Color(245,245,250));Editor.Draw(drawing);}
    }
    internal static void CustomEditingAndPlaceholder()
    {
        var custom=new CustomEditor{Width=320,Height=120};var input=new InputManager{Root=custom};LayoutEngine.Layout(custom,new(320,120));input.Focus(custom);
        custom.Editor.Text="abc";custom.Editor.Select(1,1);input.ProcessComposition(new("日本",1,1));input.ProcessCandidates(new(new[]{"日本","二本"},0));
        Check(custom.Editor.Text=="abc"&&custom.Editor.CompositionText=="日本","Custom Widget receives IME preedit without TextBox inheritance");
        Check(custom.Editor.Candidates.Count==2&&custom.TextInputArea.Height>0,"Custom editor gets candidates and native caret area");
        using(var bitmap=new Bitmap(320,120))bitmap.Draw(custom.Draw);
        input.ProcessText(new("日本"));Check(custom.Editor.Text=="a日本c","Custom editor commits over original selection");custom.Editor.Undo();Check(custom.Editor.Text=="abc","Custom editor shares undo");
        custom.Editor.SelectAll();
        input.Focus(null);Check(custom.Editor.CompositionText.Length==0&&custom.Editor.Candidates.Count==0,"Custom editor focus loss resets IME");
        var placeholder=new TextBox{Placeholder="MMMM",PlaceholderColor=new(250,0,0),Background=Color.Transparent,Stroke=default,Width=180,Height=50};LayoutEngine.Layout(placeholder,new(180,50));
        using(var pixels=new Bitmap(180,50))
        {pixels.Draw(placeholder.Draw);var data=pixels.CopyPixels();Check(data.Where((_,i)=>i%4==0).Max()>230&&data.Where((_,i)=>i%4==1).Max()==0,"Placeholder uses configured color");}
        input.Root=null;
        SwealiumApplication.Run(new(){Width=360,Height=150},window=>
        {
            window.Content=custom;window.Input.Focus(custom);custom.Editor.SelectAll();window.Input.ProcessKey(new(Key.C,true,KeyModifiers.Control));Check(Clipboard.GetText()=="abc","Custom editor copy uses framework clipboard");window.Input.ProcessComposition(new("preedit"));window.Input.ProcessCandidates(new(new[]{"candidate"},0));window.Render();
            var node=window.Accessibility.CaptureSnapshot().Nodes.Single(n=>n.Id==custom.SemanticId);
            Check(node.Role==AccessibilityRole.TextBox&&node.Actions.HasFlag(AccessibilityActions.SetValue),"Custom editor inherits semantic editing boundary");
            window.Content=null;
            var label=new Text{Content="<b>copy me</b>",Markup=TextMarkup.Html,Selectable=true};window.Content=label;window.Input.Focus(label);label.SelectAll();
            window.Input.ProcessKey(new(Key.C,true,KeyModifiers.Control));Check(Clipboard.GetText()=="copy me","Selectable normal text copies decoded HTML through the native clipboard");
            window.RequestClose();
        });
    }
}
