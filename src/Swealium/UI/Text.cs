using Swealium.Graphics;
using Swealium.Input;
namespace Swealium.UI;

/// <summary>A retained holder for DrawingContext.DrawText. HTML is formatted text, not a web view.</summary>
public class Text : Widget
{
    private string content = "";
    private TextStyle style = new();
    private TextOptions options = new();
    private TextMarkup markup;
    private bool selectable;
    public bool Selectable
    {
        get=>selectable;
        set
        {
            if(selectable==value)return;selectable=value;
            if(value)
            {
                if(TextEditor is null)
                {
                    var editor=new TextEditor(this){Multiline=true,IsReadOnly=true};
                    editor.PreserveText=true;editor.LockReadOnly=true;editor.LayoutFactory=CreateLayout;editor.SelectionChanged+=()=>SelectionChanged?.Invoke();
                }
                TextEditor!.IsEnabled=true;IsFocusable=true;Cursor=Swealium.Input.Cursor.Text;SyncEditor();
            }
            else
            {
                TextEditor!.Select(0,0);TextEditor.IsEnabled=false;IsFocusable=false;Cursor=null;
            }
        }
    }
    public Color SelectionColor {get=>TextEditor?.SelectionColor??selectionColor;set{selectionColor=value;if(TextEditor is {} editor)editor.SelectionColor=value;}}
    private Color selectionColor=new(70,110,210,100);
    public TextSelection Selection=>TextEditor?.Selection??default;
    public string SelectedText=>TextEditor?.SharedSelection?.SelectedText??TextEditor?.SelectedText??"";
    public event Action? SelectionChanged;
    public void Select(int start,int length){if(!Selectable)throw new InvalidOperationException("Enable Selectable first.");TextEditor!.Select(start,length);}
    public void SelectAll(){if(Selectable)TextEditor!.SelectAll();}
    public void Copy(){if(Selectable)TextEditor!.Copy();}
    private TextLayout CreateLayout(float width)=>new(Content,Style,Options,width,Markup);
    private void SyncEditor()
    {
        if(!Selectable||TextEditor is not {} editor)return;
        using var layout=CreateLayout(float.PositiveInfinity);
        editor.Text=layout.PlainText;editor.Style=Style;editor.VerticalAlignment=Options.VerticalAlignment;editor.SelectionColor=selectionColor;
    }
    public string Content { get => content; set { ArgumentNullException.ThrowIfNull(value); content = value; SyncEditor();InvalidateLayout(); } }
    public TextStyle Style { get => style; set { ArgumentNullException.ThrowIfNull(value); value.Validate(); style = value; SyncEditor();InvalidateLayout(); } }
    public TextOptions Options { get => options; set { ArgumentNullException.ThrowIfNull(value); value.Validate(); options = value; SyncEditor();InvalidateLayout(); } }
    public TextMarkup Markup { get => markup; set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); markup = value; SyncEditor();InvalidateLayout(); } }
    protected override Size MeasureContent(Size available) { using var layout = new TextLayout(Content, Style, Options, available.Width, Markup); return layout.Size; }
    public override void Draw(DrawingContext context)
    {
        if(Selectable)TextEditor!.Draw(context);
        else context.DrawText(Content,ContentBounds,Style,Options,Markup);
    }
}
