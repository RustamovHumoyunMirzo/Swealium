using System.Runtime.InteropServices;
using SkiaSharp;
using Swealium;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.Native;
using Swealium.UI;
using Text = Swealium.UI.Text;

internal static class TextTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Near(float actual, float expected) => Check(Math.Abs(actual-expected) < .03f, $"Expected {expected}, got {actual}");
    internal static void Flow()
    {
        var wrap = new WrapContainer { Gap = 5, LineGap = 9, Padding = new(10) };
        foreach (int width in new[] { 40, 40, 40, 120 }) wrap.Children.Add(new Button { Width = width, Height = 20, Padding = default });
        LayoutEngine.Layout(wrap, new(110,150));
        Check(wrap.LineCount == 3, "Two fit exactly; next wraps; oversized child occupies its own line");
        Near(wrap.Children[1].Bounds.X,55); Near(wrap.Children[2].Bounds.Y,39); Near(wrap.Children[3].Bounds.Y,68);
        wrap.Children[1].IsVisible = false; LayoutEngine.Layout(wrap,new(110,150));
        Check(wrap.LineCount == 2,"Hidden children do not take a gap or line");
        wrap.Direction = FlowDirection.Vertical; LayoutEngine.Layout(wrap,new(200,70));
        Check(wrap.LineCount == 2,"Vertical flow wraps at height");
        var row = new Container { Layout = LayoutMode.Horizontal, Gap = 10 };
        row.Children.Add(new Button { Text="tiny", Weight=1, WeightBasis=WeightBasis.Zero });
        row.Children.Add(new Button { Text="Much longer content", Weight=2, WeightBasis=WeightBasis.Zero });
        LayoutEngine.Layout(row,new(310,60)); Near(row.Children[0].ActualWidth,100); Near(row.Children[1].ActualWidth,200);
        row.Children[0].MaxWidth = 60; LayoutEngine.Layout(row,new(310,60));
        Near(row.Children[0].ActualWidth,60); Near(row.Children[1].ActualWidth,240);
        row.Children[0].MaxWidth = float.PositiveInfinity; row.Measure(new(float.PositiveInfinity,60));
        Check(float.IsFinite(row.DesiredSize.Width) && row.DesiredSize.Width > 0,"Unbounded weights retain intrinsic content");
        var weightedWrap = new WrapContainer { Gap=10 };
        weightedWrap.Children.Add(new Button { Width=60, Weight=1, Height=20 });
        weightedWrap.Children.Add(new Button { Width=60, Weight=1, Height=20 });
        weightedWrap.Children.Add(new Button { Width=60, Weight=1, Height=20 });
        LayoutEngine.Layout(weightedWrap,new(150,100));
        Check(weightedWrap.LineCount==2,"Content basis establishes wrap boundaries before per-line distribution");
        Near(weightedWrap.Children[0].ActualWidth,70); Near(weightedWrap.Children[2].ActualWidth,150);
        var textRow = new Container {Layout=LayoutMode.Horizontal,Gap=10};
        var label = new Text{Content="one two three four five six seven eight",Weight=1,WeightBasis=WeightBasis.Zero};
        textRow.Children.Add(label);textRow.Children.Add(new Button{Width=50,Height=20});
        textRow.Measure(new(140,float.PositiveInfinity));
        Check(textRow.DesiredSize.Height>40,"Weight allocation remeasures wrapped text before cross-axis measurement");
        LayoutEngine.Layout(textRow,new(140,textRow.DesiredSize.Height));Near(label.ActualWidth,80);
    }
    internal static void Shaping()
    {
        using var empty = new TextLayout(""); Check(empty.Size.Height > 0,"Empty paragraph has caret metrics");
        using var trailing = new TextLayout("a\n"); Check(trailing.LineCount == 2,"Trailing newline preserves empty last line");
        using var latin = new TextLayout("one two three four five six", maxWidth:90);
        Check(latin.LineCount > 1 && latin.Size.Width <= 90.1f,"Paragraph wraps within finite boundary");
        using var rtl = new TextLayout("سلام", new() {FontSize=30}, new() {Direction=TextDirection.RightToLeft});
        Check(rtl.GetCaretRectangle(new(0)).X > rtl.GetCaretRectangle(new(4)).X,"Arabic caret order is RTL");
        Check(rtl.NativeBlock.FontRuns.Any(run => run.Direction == Topten.RichTextKit.TextDirection.RTL),"Arabic shaping uses RTL font runs");
        var arabicRun = rtl.NativeBlock.FontRuns.First(run => run.Length==4);
        using (var font = new SKFont(arabicRun.Typeface,30))
            Check(!arabicRun.Glyphs.SequenceEqual("سلام".Select(character=>font.GetGlyph(character)).Reverse()),"Contextual Arabic glyphs differ from isolated glyph drawing");
        using var end = new TextLayout("سلام", options:new(){Alignment=TextAlignment.End},maxWidth:200);
        Check(end.GetCaretRectangle(new(0)).X < 100,"Automatic RTL end alignment is left aligned");
        using var mixed = new TextLayout("Hello שלום مرحبا 😀 e\u0301", new() { FontSize=24 }, maxWidth:400);
        Check(mixed.Size.Width > 0 && mixed.CaretIndices.All(i => i >= 0 && i <= mixed.PlainText.Length),"Mixed scripts and surrogate indices");
        int emoji = mixed.PlainText.IndexOf("😀", StringComparison.Ordinal);
        Check(!mixed.CaretIndices.Contains(emoji+1),"No caret inside a surrogate pair");
        using var ellipsis = new TextLayout("one two three four five six seven eight nine", options:new(){MaxLines=1,Ellipsis=true},maxWidth:60);
        Check(ellipsis.IsTruncated && ellipsis.LineCount==1,"MaxLines and ellipsis");
        using var html = new TextLayout("<p>Hello <b>bold &amp; brave</b><br><i>italic</i></p><script>bad()</script><p><span style='color:#ff0000;font-size:24px'>red</span></p>",markup:TextMarkup.Html);
        Check(html.PlainText=="Hello bold & brave\nitalic\nred",$"HTML text and paragraphs: {html.PlainText}");
        Check(html.Runs.Any(run=>run.Style.FontWeight==700) && html.Runs.Any(run=>run.Style.Italic) && html.Runs.Any(run=>run.Style.Foreground.R==255 && run.Style.FontSize==24),"HTML style inheritance");
        using var literal = new TextLayout("<b>literal</b>"); Check(literal.PlainText.Contains("<b>"),"Plain is never implicitly HTML");
        using var rgba = new TextLayout("<span style='color:#f008'>rgba</span>",markup:TextMarkup.Html);
        Check(rgba.Runs[0].Style.Foreground==new Color(255,0,0,136),"HTML hex alpha is CSS RGBA order");
        var disposed = new TextLayout("disposed"); disposed.Dispose();
        try { disposed.HitTest(default); throw new Exception("Expected disposed failure"); } catch (ObjectDisposedException) { }
    }
    internal static void Editing()
    {
        var box = new TextBox { Width=240,Height=110,Multiline=true,Text="A😀e\u0301👨‍👩‍👧‍👦Z" };
        LayoutEngine.Layout(box,new(240,110)); var input = new InputManager {Root=box}; input.Focus(box);
        box.Select(3,0); input.ProcessKey(new(Key.Backspace,true));
        Check(box.Text=="Ae\u0301👨‍👩‍👧‍👦Z","Backspace removes a whole surrogate grapheme");
        box.Select(3,0); input.ProcessKey(new(Key.Backspace,true));
        Check(box.Text=="A👨‍👩‍👧‍👦Z","Backspace removes combining sequence");
        box.Select(1,0); input.ProcessKey(new(Key.Delete,true)); Check(box.Text=="AZ","Delete removes entire joined family emoji");
        box.Undo(); Check(box.Text.Contains("👨"),"Undo restores text and selection"); box.Redo(); Check(box.Text=="AZ","Redo");
        box.Select(1,1); input.ProcessComposition(new("にほん",1,1));
        Check(box.Text=="AZ" && box.CompositionText=="にほん","Preedit does not change committed text");
        input.ProcessCandidates(new(new[]{"日本","二本"},1)); Check(box.SelectedCandidate==1 && box.Candidates.Count==2,"Candidate snapshot");
        input.ProcessText(new("日本")); Check(box.Text=="A日本" && box.CompositionText=="" && box.Candidates.Count==0,"Commit replaces the original selection once");
        box.Undo(); Check(box.Text=="AZ","One IME commit is one undo operation");
        input.ProcessComposition(new("temp")); input.Focus(null); Check(box.CompositionText=="" && box.Text=="AZ","Focus loss cancels preedit");
        input.Focus(box); box.SelectAll(); box.IsReadOnly=true; input.ProcessText(new("bad")); box.ReplaceSelection("bad"); Check(box.Text=="AZ","Read-only blocks native and programmatic edits");
        box.IsReadOnly=false; input.ProcessText(new("x\r\ny")); Check(box.Text=="x\ny","Newline normalization");
        input.ProcessKey(new(Key.Home,true,KeyModifiers.Control)); Check(box.CaretIndex==0,"Document home");
        input.ProcessKey(new(Key.Right,true,KeyModifiers.Shift)); Check(box.Selection.Length==1,"Shift extends selection");
        input.ProcessPointer(new(PointerEventKind.Down,new(10,10),PointerButton.Primary));
        input.ProcessPointer(new(PointerEventKind.Move,new(100,35))); input.ProcessPointer(new(PointerEventKind.Up,new(100,35),PointerButton.Primary));
        Check(input.CapturedWidget is null,"Pointer selection releases capture");
    }
    internal static void NativeIme()
    {
        var box = new TextBox {Width=200,Height=50}; LayoutEngine.Layout(box,new(200,50));
        var input = new InputManager {Root=box}; input.Focus(box);
        nint value = Marshal.StringToCoTaskMemUTF8("😀ab");
        try
        {
            var e = new Sdl.Event {Type=0x302};
            if (IntPtr.Size==8) {e.Text64=value;e.EditStart64=1;e.EditLength64=1;}
            else {e.Text32=value;e.EditStart32=1;e.EditLength32=1;}
            int start=-1,length=-1; box.CompositionInput += composition=>{start=composition.SelectionStart;length=composition.SelectionLength;};
            SdlInput.Dispatch(e,input); Check(start==2 && length==1,"Native composition character offsets convert to UTF-16");
            var pointers = new[]{Marshal.StringToCoTaskMemUTF8("候補"),Marshal.StringToCoTaskMemUTF8("😀")};
            nint list = Marshal.AllocHGlobal(IntPtr.Size*2);
            try
            {
                Marshal.Copy(pointers,0,list,2); e.Type=0x304;
                if (IntPtr.Size==8) {e.Text64=list;e.EditStart64=2;e.EditLength64=1;e.CandidatesHorizontal64=1;}
                else {e.Text32=list;e.EditStart32=2;e.EditLength32=1;e.CandidatesHorizontal32=1;}
                SdlInput.Dispatch(e,input);
            }
            finally { Marshal.FreeHGlobal(list); foreach(nint pointer in pointers) Marshal.FreeCoTaskMem(pointer); }
            Check(box.Candidates.SequenceEqual(new[]{"候補","😀"}) && box.SelectedCandidate==1 && box.CandidatesHorizontal,"Copied candidate pointer array survives native release");
        }
        finally {Marshal.FreeCoTaskMem(value);}
        input.Root = null;
        SwealiumApplication.Run(new(){Width=360,Height=200},window=>
        {
            window.Content=box; window.Input.Focus(box); Clipboard.SetText("clipboard 😀 العربية"); Check(Clipboard.GetText()=="clipboard 😀 العربية","Unicode native clipboard");
            box.Paste(); Check(box.Text.Contains("clipboard"),"Editor pastes native clipboard");
            window.Input.ProcessComposition(new("にほん",1,1)); window.Input.ProcessCandidates(new(new[]{"日本","二本"},0));
            window.Render(); window.RequestClose();
        });
    }
    internal static void Rendering()
    {
        using var bitmap = new SKBitmap(760,420); using var surface = SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes);
        surface.Canvas.Clear(new SKColor(242,245,250));
        var root = new Container {Padding=new(24),Gap=16};
        root.Children.Add(new Text { Content="<h2>Shaped text, wrapping &amp; editing</h2><p><b>Bold</b>, <i>italic</i>, <u>underline</u> — مرحبا بالعالم · שלום · 😀</p>",Markup=TextMarkup.Html });
        var box = new TextBox {Multiline=true, Height=140,Text="Edit this paragraph. Arabic: مرحبا بالعالم\nEmoji: 😀 👨‍👩‍👧‍👦 — combining: é",Style=new(){FontSize=22}};
        root.Children.Add(box); LayoutEngine.Layout(root,new(760,420)); var input = new InputManager{Root=root};input.Focus(box);box.Select(5,14);
        WidgetRenderer.Render(surface.Canvas,root);
        var drawing = new SkiaDrawingContext(surface.Canvas);
        input.ProcessComposition(new("にほん",1,1)); input.ProcessCandidates(new(new[]{"日本","二本","ニホン"},0));
        WidgetRenderer.Render(surface.Canvas,root);box.DrawCandidates(drawing,new(760,420));drawing.End();
        int ink = 0;
        for (int y=24; y<85; y++) for (int x=24; x<730; x++)
        { var pixel=bitmap.GetPixel(x,y); if (pixel.Red<80 && pixel.Green<80 && pixel.Blue<80) ink++; }
        Check(ink>150,"Shaped headings and mixed-script text produce visible raster glyphs");
        Directory.CreateDirectory("artifacts"); using var image=SKImage.FromBitmap(bitmap); using var png=image.Encode(SKEncodedImageFormat.Png,100); using var file=File.Create("artifacts/text-preview.png"); png.SaveTo(file);
    }
}
