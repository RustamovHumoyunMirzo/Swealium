using Swealium;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;

bool smokeTest = args.Contains("--smoke-test");
if(args.Contains("--media-demo"))
{
    using var pixels=Bitmap.Load(Path.Combine(AppContext.BaseDirectory,"cat.png"));
    using var svg=VectorImage.Parse("<svg xmlns='http://www.w3.org/2000/svg' width='240' height='140'><defs><linearGradient id='g'><stop stop-color='#8d61d8'/><stop offset='1' stop-color='#3aafae'/></linearGradient></defs><rect width='240' height='140' rx='22' fill='url(#g)'/><path d='M45 70 L95 108 L195 30' fill='none' stroke='white' stroke-width='12' stroke-linecap='round' stroke-linejoin='round'/></svg>");
    using var path=new VectorPath();path.MoveTo(new(10,80)).CubicTo(new(70,-30),new(160,170),new(230,45));
    using var recorded=VectorImage.Create(new(0,0,240,140),drawing=>
    {drawing.FillRectangle(new(0,0,240,140),new Color(35,42,58));drawing.StrokePath(path,new Color(240,190,100),8,PathLineCap.Round);drawing.DrawText("Recorded vector",new Point(15,128),Color.White,18);});
    using var cursorPixels=svg.Rasterize(32,32);using var custom=Cursor.FromBitmap(cursorPixels,4,4);
    SwealiumApplication.HardwareAcceleration = true;
    SwealiumApplication.AntiAliasing = true;
    SwealiumApplication.Run(new(){Title="Swealium — bitmaps, vectors, and cursors",Width=980,Height=720},window=>
    {
        var root=new Container{Padding=new(24),Gap=16,Background=new(23,27,36)};
        root.Children.Add(new Text{Content="Bitmap · SVG · Recorded vector · Custom cursors",Style=new(){FontSize=25,Foreground=Color.White}});
        var row=new Container{Layout=LayoutMode.Horizontal,Gap=16,Height=210};
        row.Children.Add(new Image{Source=pixels,Weight=1,WeightBasis=WeightBasis.Zero,Padding=new(8),Radii=new(16),Stroke=new Stroke(2,new(90,125,175))});
        row.Children.Add(new VectorGraphics{Source=svg,Weight=1,WeightBasis=WeightBasis.Zero,Padding=new(8),Radii=new(16),Stroke=new Stroke(2,new(150,100,210))});
        row.Children.Add(new VectorGraphics{Source=recorded,Weight=1,WeightBasis=WeightBasis.Zero,Padding=new(8),Radii=new(16),Stroke=new Stroke(2,new(210,160,80))});
        root.Children.Add(row);
        root.Children.Add(new Text{Content="Hover to preview a cursor. Click to set the window default. Scroll for all system types.",Style=new(){Foreground=new(190,205,225)}});
        var scroll=new ScrollContainer{Weight=1,WeightBasis=WeightBasis.Zero};var choices=new WrapContainer{Gap=8,LineGap=8};scroll.Children.Add(choices);
        var status=new Text{Content="Window cursor: Default",Style=new(){Foreground=Color.White}};
        foreach(var cursor in Cursor.SystemCursors)AddCursorChoice(cursor,cursor.Kind.ToString());
        AddCursorChoice(Cursor.VSplit,"VSplit");AddCursorChoice(Cursor.HSplit,"HSplit");
        AddCursorChoice(custom,"Bitmap cursor");AddCursorChoice(Cursor.Hidden,"Hidden");
        void AddCursorChoice(Cursor cursor,string name)
        {
            var button=new Button{Text=name,Cursor=cursor,Width=175,Height=44,FontSize=13};
            button.Click+=()=>{window.Cursor=cursor;status.Content=$"Window cursor: {name}";};choices.Children.Add(button);
        }
        root.Children.Add(scroll);root.Children.Add(status);window.Content=root;
        if(smokeTest){window.Cursor=custom;int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();};}
    });
    return;
}
if (args.Contains("--scroll-demo"))
{
    SwealiumApplication.Run(new(){Title="Swealium — scrolling and per-side strokes",Width=820,Height=620},window=>
    {
        var root=new Container{Padding=new(24),Gap=12,Background=new(240,243,249)};
        root.Children.Add(new Text{Content="Scroll vertically · Shift+wheel horizontally · Drag a thumb",Style=new(){FontSize=24}});
        var scroll=new ScrollContainer{Direction=ScrollDirection.Both,Weight=1,WeightBasis=WeightBasis.Zero,Gap=12,Padding=new(16),Background=Color.White,Radii=new(18),Stroke=new Stroke(2,new(160,175,200)),
            VerticalScrollBarStyle=new(){Thickness=16,ThumbColor=new(70,110,170),ThumbRadii=new(8,2,8,2)}};
        for(int i=0;i<20;i++) scroll.Children.Add(new Button{Text=$"Item {i+1} — a wide row with four independent border sides",Width=1050,Height=60,Radii=new(16,4,16,4),
            Stroke=new(new StrokeSide(5,new(235,100,100)),new StrokeSide(2,new(100,150,240)),new StrokeSide(8,new(80,190,140),(StrokeStyle)(i%5)),new StrokeSide(6,new(235,195,90),StrokeStyle.Double))});
        root.Children.Add(scroll);
        var controls=new Container{Layout=LayoutMode.Horizontal,Gap=8};
        var next=new Button{Text="Next page"};next.Click+=()=>scroll.ScrollBy(0,scroll.Viewport.Height);
        var top=new Button{Text="Top"};top.Click+=()=>scroll.ScrollTo(0,0);
        var hidden=new Button{Text="Toggle bars"};hidden.Click+=()=>
        {
            var visibility=scroll.VerticalScrollBarVisibility==ScrollBarVisibility.Hidden?ScrollBarVisibility.Auto:ScrollBarVisibility.Hidden;
            scroll.HorizontalScrollBarVisibility=scroll.VerticalScrollBarVisibility=visibility;
        };
        controls.Children.Add(top);controls.Children.Add(next);controls.Children.Add(hidden);root.Children.Add(controls);
        var status=new Text();root.Children.Add(status);
        scroll.ScrollChanged+=e=>status.Content=$"Offset {e.Offset.X:0}, {e.Offset.Y:0} · Extent {e.Extent.Width:0} × {e.Extent.Height:0}";
        window.Content=root;window.Input.Focus(scroll);
        if(smokeTest){scroll.ScrollTo(60,120);int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();};}
    });
    return;
}
if (args.Contains("--text-demo"))
{
    SwealiumApplication.Run(new(){Title="Swealium — shaped text and editing",Width=850,Height=650}, window=>
    {
        var content = new Container {Padding=new(24),Gap=18,Background=new(242,245,250)};
        content.Children.Add(new Text {Content="<h1>Text in Swealium</h1><p><b>Shaped</b>, <i>formatted</i>, <u>wrapped</u> — مرحبا بالعالم · שלום · 😀</p>",Markup=TextMarkup.Html,Style=new(){FontSize=20}});
        var flow = new WrapContainer {Gap=8,LineGap=10};
        foreach(string label in new[]{"Resize the window","Wrapping chips","Content-based size","Horizontal flow","More items"})
            flow.Children.Add(new Button{Text=label});
        content.Children.Add(flow);
        var weights = new Container{Layout=LayoutMode.Horizontal,Gap=10};
        weights.Children.Add(new Button{Text="1 share",Weight=1,WeightBasis=WeightBasis.Zero});
        weights.Children.Add(new Button{Text="2 shares",Weight=2,WeightBasis=WeightBasis.Zero});
        content.Children.Add(weights);
        var editor = new TextBox{Multiline=true,Height=180,Style=new(){FontSize=22},Text="Edit text here. Try your platform IME.\nArabic: مرحبا بالعالم\nEmoji: 😀 — combining: é"};
        content.Children.Add(editor);
        content.Children.Add(new Text{Content="Select and drag · Ctrl/Cmd+A/C/X/V/Z · Shift+Home/End · IME conversion",Style=new(){Foreground=new(80,85,100)}});
        window.Content=content;window.Input.Focus(editor);
        if(smokeTest) { int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();}; }
    });
    return;
}
SwealiumApplication.Run(new WindowOptions { Title = "Swealium - Layout and rounded input" }, window =>
{
    var root = new Container
    {
        Padding = new(32, 72, 32, 32), Gap = 20,
        VerticalContentAlignment = Alignment.Center, Background = new(24, 26, 30)
    };
    var row = new Container
    {
        Layout = LayoutMode.Horizontal, Padding = new(20), Gap = 12, Height = 108,
        VerticalContentAlignment = Alignment.Center, HorizontalContentAlignment = Alignment.Center,
        Background = new(34, 38, 48), Radii = new(24, 6, 24, 6)
    };
    var button = new Button
    {
        Text = "Try the cut-out corners", Grow = 1, MaxWidth = 360, Height = 56,
        Radii = new(28, 4, 28, 4)
    };
    int clicks = 0;
    button.Click += () => button.Text = $"Clicked {++clicks} times";
    row.Children.Add(button);
    row.Children.Add(new Button { Text = "Equal growth", Grow = 1, MaxWidth = 360, Height = 56, Radii = new(28) });

    var stack = new Container
    {
        Padding = new(20), Gap = 12, Background = new(34, 38, 48), Radii = new(16),
        HorizontalContentAlignment = Alignment.Center
    };
    stack.Children.Add(new Button { Text = "Content size, centered", WidthSizing = SizingMode.Content });
    stack.Children.Add(new Button
    {
        Text = "Child placement: end", HorizontalAlignment = Alignment.End,
        Margin = new(8, 0), Radii = new(4, 20, 4, 20)
    });

    var grid = new Grid
    {
        Height = 160, Padding = new(20), Gap = 8,
        Background = new(34, 38, 48), Radii = new(16)
    };
    grid.Columns.Add(new(GridLength.Fixed(140)));
    grid.Columns.Add(new(GridLength.Fraction()));
    grid.Columns.Add(new(GridLength.Fraction(2)));
    grid.Rows.Add(new(GridLength.Content));
    grid.Rows.Add(new(GridLength.Fraction()));
    grid.Children.Add(new Button
    {
        Text = "Grid — fixed + weighted columns, spanning header",
        GridColumnSpan = 3, WidthSizing = SizingMode.Fill, HeightSizing = SizingMode.Content
    });
    grid.Children.Add(new Button { Text = "140 units", GridRow = 1, WidthSizing = SizingMode.Fill, HeightSizing = SizingMode.Fill });
    grid.Children.Add(new Button { Text = "1 share", GridRow = 1, GridColumn = 1, WidthSizing = SizingMode.Fill, HeightSizing = SizingMode.Fill });
    grid.Children.Add(new Button
    {
        Text = "2 shares", GridRow = 1, GridColumn = 2,
        WidthSizing = SizingMode.Fill, HeightSizing = SizingMode.Fill, Background = new(95, 62, 130)
    });
    var windows = new Container
    {
        Layout = LayoutMode.Horizontal, Gap = 8, Padding = new(12),
        HorizontalContentAlignment = Alignment.Center, Radii = new(12), Background = new(34, 38, 48)
    };
    AddAction("New window", () =>
    {
        var extra = SwealiumApplication.CreateWindow(new() { Title = "Independent window", Width = 420, Height = 240 });
        extra.Content = CloseContent(extra, "Close this window");
    });
    AddAction("Child", () =>
    {
        var child = window.CreateChild(new() { Title = "Owned child", Width = 360, Height = 200, ShowInTaskbar = false });
        child.Content = CloseContent(child, "Close child");
    });
    var openDialog = new Button { Text = "Dialog" };
    openDialog.Click += async () =>
    {
        DialogResult result = await window.ShowDialogAsync(configure: dialog =>
        {
            var buttons = new Container { Padding = new(24), Gap = 12, VerticalContentAlignment = Alignment.Center };
            var accept = new Button { Text = "Accept" };
            accept.Click += () => dialog.CloseDialog(DialogResult.Ok);
            var cancel = new Button { Text = "Cancel" };
            cancel.Click += () => dialog.CloseDialog(DialogResult.Cancel);
            buttons.Children.Add(accept); buttons.Children.Add(cancel);
            dialog.Content = buttons;
        });
        openDialog.Text = $"Dialog: {result}";
    };
    windows.Children.Add(openDialog);
    AddAction("Warning", () => MessageBox.Warning("Warning", "This is an SDL-provided system warning.", window));
    AddAction("Message box", () =>
    {
        DialogResult result = MessageBox.Show(new()
        {
            Title = "Confirm", Message = "Continue with this action?",
            Buttons = MessageBoxButtons.YesNoCancel, DefaultButton = DialogResult.No
        }, window);
        button.Text = $"Message box: {result}";
    });
    root.Children.Add(row);
    root.Children.Add(stack);
    root.Children.Add(grid);
    root.Children.Add(windows);
    window.Content = root;
    window.FocusChanged += focused => window.Title = focused ? "Swealium - Active" : "Swealium - Inactive";
    window.Paint += (drawing, _) => drawing.DrawText("Swealium — layout, windows, and dialogs", new(32, 44), Color.White, 24);
    if (smokeTest)
    {
        var child = window.CreateChild(new() { Title = "Child smoke test", Width = 240, Height = 160 });
        child.Content = CloseContent(child, "Child surface");
        var extra = SwealiumApplication.CreateWindow(new() { Title = "Independent smoke test", Width = 240, Height = 160 });
        extra.Content = CloseContent(extra, "Independent surface");
        extra.Paint += (_, _) => { if (window.IsDisposed) extra.RequestClose(); };
    }
    if (!smokeTest) return;
    int frames = 0;
    window.Paint += (_, _) => { if (++frames == 3) window.RequestClose(); };

    void AddAction(string text, Action action)
    {
        var item = new Button { Text = text };
        item.Click += action;
        windows.Children.Add(item);
    }
});

static Container CloseContent(SwealiumWindow target, string text)
{
    var content = new Container { Padding = new(24), Gap = 12, VerticalContentAlignment = Alignment.Center };
    var controls = new Container { Layout = LayoutMode.Horizontal, Gap = 8 };
    var maximize = new Button { Text = "Maximize", Grow = 1 };
    maximize.Click += () => { if (target.IsMaximized) target.Restore(); else target.Maximize(); };
    target.StateChanged += state => maximize.Text = state == WindowState.Maximized ? "Restore" : "Maximize";
    var center = new Button { Text = "Center", Grow = 1 };
    center.Click += target.Center;
    controls.Children.Add(maximize);
    controls.Children.Add(center);
    content.Children.Add(controls);
    var close = new Button { Text = text };
    close.Click += () => target.RequestClose();
    content.Children.Add(close);
    return content;
}

