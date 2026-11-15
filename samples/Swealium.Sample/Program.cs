using Swealium;
using Swealium.Graphics;
using Swealium.UI;

bool smokeTest = args.Contains("--smoke-test");
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

