using Swealium;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;
using Swealium.UI.Effects;

bool smokeTest = args.Contains("--smoke-test");
if(args.Contains("--effects-demo"))
{
    SwealiumApplication.Run(new(){Title="Swealium - widget effects",Width=1060,Height=800},window=>
    {
        var root=new Container{Padding=new(24),Gap=14,Background=new(20,27,40)};
        root.Children.Add(new Text{Content="Widget effects: glass, progressive blur, shadows and color",Style=new(){FontSize=26,Foreground=Color.White}});
        var stage=new Container{Layout=LayoutMode.Absolute,Weight=1,WeightBasis=WeightBasis.Zero,Radii=new(16)};
        stage.Children.Add(new DrawingCanvas((c,b)=>
        {
            c.FillRectangle(b,Gradient.Diagonal(new(30,100,180),new(120,50,160),new(10,135,135)));
            for(int x=0;x<b.Width;x+=16)c.FillRectangle(new(x,0,6,b.Height),new(255,255,255,100));
            c.FillEllipse(new(90,45,210,210),new(255,160,80));c.FillEllipse(new(600,170,240,240),new(50,220,180));
        }){WidthSizing=SizingMode.Fill,HeightSizing=SizingMode.Fill,ConsumePointer=false});
        Container Card(float x,float y,string title)
        {
            var card=new Container{X=x,Y=y,Width=285,Height=170,Padding=new(20),Gap=10,Radii=new(20),Background=new(255,255,255,45),Stroke=new(1,new(255,255,255,100))};
            card.Children.Add(new Text{Content=title,Style=new(){FontSize=23,Foreground=Color.White}});
            card.Children.Add(new Text{Content="Live content and rounded edges",Style=new(){FontSize=16,Foreground=Color.White}});
            stage.Children.Add(card);return card;
        }
        var glass=Card(30,35,"Backdrop blur");glass.Effects.Add(new DropShadow{Radius=new(12,12),Offset=new(0,7)});glass.Effects.Add(new BackdropBlur(9));
        var progressive=Card(355,35,"Progressive backdrop");progressive.Effects.Add(new ProgressiveBlur(BlurGradient.Horizontal(new(0,0),new(.25f,0),new(1,14)),BlurTarget.Backdrop));
        var shadow=Card(680,35,"Alpha-shaped shadow");shadow.Background=new(45,60,100);shadow.Radii=new(40,5,40,5);shadow.Effects.Add(new DropShadow{Color=new(0,0,0,170),Radius=new(10,10),Offset=new(10,12)});
        var layer=Card(30,260,"Layer blur");layer.Background=new(80,140,220);layer.Effects.Add(new LayerBlur(2));
        var fading=Card(355,260,"Progressive layer");fading.Background=new(90,100,200);fading.Effects.Add(new ProgressiveBlur(BlurGradient.Vertical(new(0,0),new(.35f,0),new(1,9))){EdgeMode=BlurEdgeMode.Transparent});
        var gray=Card(680,260,"Grayscale subtree");gray.Background=new(220,100,45);gray.Effects.Add(new ColorFilter(ColorMatrix.Grayscale()));
        root.Children.Add(stage);
        var status=new Text{Content="Glass background alpha: 45/255. Widget opacity: 1. Blur radius stays constant.",Style=new(){Foreground=new(190,215,240)}};
        var controls=new Container{Layout=LayoutMode.Horizontal,Height=44,Gap=12};
        var alpha=new Button{Text="Toggle glass color alpha",Weight=1};alpha.Click+=()=>{glass.Background=glass.Background.A==255?new(255,255,255,45):Color.White;status.Content=$"Glass color alpha: {glass.Background.A}/255. Widget opacity: {glass.Opacity}.";};
        var opacity=new Button{Text="Toggle widget opacity",Weight=1};opacity.Click+=()=>{glass.Opacity=glass.Opacity==1?.5f:1;status.Content=$"Glass color alpha: {glass.Background.A}/255. Widget opacity: {glass.Opacity}.";};
        var enabled=new Button{Text="Enable / disable effects",Weight=1};bool effectsEnabled=true;enabled.Click+=()=>{effectsEnabled=!effectsEnabled;foreach(var card in new[]{glass,progressive,shadow,layer,fading,gray})foreach(var effect in card.Effects)effect.IsEnabled=effectsEnabled;};
        controls.Children.Add(alpha);controls.Children.Add(opacity);controls.Children.Add(enabled);root.Children.Add(controls);root.Children.Add(status);window.Content=root;
        if(smokeTest){int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();};}
    });
    return;
}
if(args.Contains("--canvas-demo"))
{
    SwealiumApplication.Run(new(){Title="Swealium - 100K+ immediate drawing",Width=1000,Height=720},window=>
    {
        PointBuffer curve=new(Array.Empty<Point>()),scatter=new(Array.Empty<Point>());int count=100_000;bool showScatter=false;
        void Generate(int size)
        {
            count=size;var line=new Point[size];var cloud=new Point[size];var random=new Random(42);
            for(int i=0;i<size;i++){float x=(float)i/(size-1);line[i]=new(x,.5f+MathF.Sin(x*60)*.28f+MathF.Sin(x*3000)*.07f);cloud[i]=new(random.NextSingle(),random.NextSingle());}
            line[size/2]=line[size/2] with{Y=.03f};line[size/2+1]=line[size/2+1] with{Y=.97f};curve=new(line);scatter=new(cloud);
        }
        Generate(count);
        var root=new Container{Padding=new(22),Gap=14,Background=new(21,27,38)};
        root.Children.Add(new Text{Content="DrawingCanvas: one widget, 100K to 1M samples",Style=new(){FontSize=27,Foreground=Color.White}});
        var status=new Text{Style=new(){Foreground=new(180,205,230)}};
        PointBuffer? projected=null,previousSource=null;Rect previousArea=default;Point? pointer=null;
        var canvas=new DrawingCanvas{Weight=1,WeightBasis=WeightBasis.Zero,Background=new(12,18,28),Radii=new(12),Stroke=new(1,new(65,90,125))};
        canvas.Paint+=(drawing,bounds)=>
        {
            var area=new Rect(bounds.X+14,bounds.Y+14,Math.Max(0,bounds.Width-28),Math.Max(0,bounds.Height-28));var source=showScatter?scatter:curve;
            if(projected is null||source!=previousSource||area!=previousArea)
            {projected=source.Project(area,new(0,0,1,1));previousSource=source;previousArea=area;}
            drawing.AntiAliasing=!showScatter;
            if(showScatter)drawing.DrawPoints(projected,new(80,195,235),1,clip:area);
            else drawing.DrawPolyline(projected,new(80,205,240),1,clip:area);
            if(pointer is {} position&&area.Contains(position))
            {drawing.DrawLine(new(position.X,area.Y),new(position.X,area.Y+area.Height),new(225,180,100,130));drawing.DrawLine(new(area.X,position.Y),new(area.X+area.Width,position.Y),new(225,180,100,130));}
        };
        canvas.PointerInput+=e=>{if(e.Kind==PointerEventKind.Move)pointer=e.Position;else if(e.Kind==PointerEventKind.Leave)pointer=null;};
        void Status()=>status.Content=$"{count:N0} samples | {(showScatter?"scatter (all points)":"ordered polyline")} | {canvas.Detail} | {(window.IsHardwareAccelerated?"GPU-backed Skia":"CPU raster")}";
        var controls=new Container{Layout=LayoutMode.Horizontal,Height=44,Gap=10};
        var smaller=new Button{Text="100K samples",Weight=1};smaller.Click+=()=>{Generate(100_000);Status();};
        var larger=new Button{Text="1M samples",Weight=1};larger.Click+=()=>{Generate(1_000_000);Status();};
        var mode=new Button{Text="Plot / scatter",Weight=1};mode.Click+=()=>{showScatter=!showScatter;Status();};
        var detail=new Button{Text="Auto / exact (slower)",Weight=1};detail.Click+=()=>{canvas.Detail=canvas.Detail==DrawingDetail.Automatic?DrawingDetail.Exact:DrawingDetail.Automatic;Status();};
        controls.Children.Add(smaller);controls.Children.Add(larger);controls.Children.Add(mode);controls.Children.Add(detail);
        root.Children.Add(controls);root.Children.Add(canvas);root.Children.Add(status);window.Content=root;Status();
        if(smokeTest){int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();};}
    });
    return;
}
if(args.Contains("--outline-demo"))
{
    SwealiumApplication.Run(new(){Title="Swealium - outlines and ghost pointers",Width=920,Height=760},window=>
    {
        var root=new Container{Padding=new(28),Gap=16,Background=new(24,30,42)};
        root.Children.Add(new Text{Content="Outlines and pointer-transparent overlays",Style=new(){Foreground=Color.White,FontSize=27}});
        var status=new Text{Content="Click the covered button. The ghost overlay passes pointer input through.",Style=new(){Foreground=new(185,210,235)}};
        var stage=new Container{Layout=LayoutMode.Absolute,Height=155,ClipToBounds=false};int clicks=0;
        var target=new Button{X=35,Y=35,Width=310,Height=70,Text="Click through the overlay"};target.Click+=()=>status.Content=$"Button clicked {++clicks} time(s)";
        var overlay=new Container{X=25,Y=25,Width=330,Height=90,Padding=new(12),Background=new(30,110,180,90),Radii=new(14),ConsumePointer=false,ZIndex=10,Outline=new(3,new(80,230,160),offset:4)};
        overlay.Children.Add(new Text{Content="Overlay",Style=new(){Foreground=Color.White}});stage.Children.Add(target);stage.Children.Add(overlay);root.Children.Add(stage);
        var toggle=new Button{Text="Overlay consumes pointer: false",Height=44};toggle.Click+=()=>
        {overlay.ConsumePointer=!overlay.ConsumePointer;toggle.Text=$"Overlay consumes pointer: {overlay.ConsumePointer}";overlay.Outline=new(3,overlay.ConsumePointer?new Color(245,130,90):new Color(80,230,160),offset:4);};root.Children.Add(toggle);
        var gallery=new WrapContainer{Gap=12,LineGap=12};
        foreach(var style in Enum.GetValues<StrokeStyle>())
        {
            var card=new Container{Width=140,Height=64,Padding=new(12),Margin=new(12),Radii=new(14),Background=new(45,60,85),Outline=new(4,Gradient.Horizontal(new(100,190,255),new(215,140,250)),style,offset:3)};
            card.Children.Add(new Text{Content=style.ToString(),Style=new(){Foreground=Color.White,FontSize=19}});gallery.Children.Add(card);
        }
        root.Children.Add(gallery);root.Children.Add(status);window.Content=root;
        if(smokeTest){int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();};}
    });
    return;
}
if(args.Contains("--transform-demo"))
{
    SwealiumApplication.Run(new(){Title="Swealium - transforms and overscroll",Width=900,Height=780},window=>
    {
        var root=new Container{Padding=new(20),Gap=14,Background=new(24,29,40)};
        root.Children.Add(new Text{Content="Transforms follow drawing and input. Overscroll controls chaining.",Style=new(){FontSize=25,Foreground=Color.White}});
        var status=new Text{Content="Click transformed controls; choose a scroll policy below.",Style=new(){Foreground=new(170,205,235)}};
        var canvas=new Container{Layout=LayoutMode.Absolute,Height=230,Background=new(35,44,61),Radii=new(12)};
        var transforms=new[]{Transform.Translate(25,10),Transform.ScaleBy(1.2f,.8f),Transform.Rotate(18),Transform.ScaleBy(-1,1)};
        var labels=new[]{"Translated","Scaled","Rotated","Mirrored"};
        for(int i=0;i<4;i++)
        {
            string label=labels[i];var button=new Button{Text=label,X=35+(i%3)*265,Y=i==3?150:45,Width=180,Height=55,Transform=transforms[i],Radii=new(12)};
            button.Click+=()=>status.Content=$"Clicked {label}: inverse-transformed hit testing";canvas.Children.Add(button);
        }
        root.Children.Add(canvas);
        var inner=Swealium.UI.List.Create(Enumerable.Range(1,20),i=>new Text{Content=$"Inner list item {i}",Height=28,Style=new(){Foreground=Color.White}});inner.Height=120;inner.Background=new(30,48,68);
        var outer=new ScrollContainer{Weight=1,WeightBasis=WeightBasis.Zero,Padding=new(8),Gap=14,Background=new(15,21,30)};
        outer.Children.Add(inner);outer.Children.Add(new Text{Content="Outer scroll content. Focus the outer region to transfer ownership.\nChain passes remaining input at an inner boundary.",Height=380,Style=new(){Foreground=new(185,210,235),FontSize=20}});
        var controls=new Container{Layout=LayoutMode.Horizontal,Gap=8,Height=44};
        foreach(var mode in Enum.GetValues<OverscrollBehavior>())
        {
            var button=new Button{Text=mode.ToString(),Weight=1};button.Click+=()=>{inner.Overscroll=mode;window.Input.Focus(inner);status.Content=$"Inner overscroll: {mode}";};controls.Children.Add(button);
        }
        var focusOuter=new Button{Text="Focus outer",Weight=1};focusOuter.Click+=()=>window.Input.Focus(outer);controls.Children.Add(focusOuter);
        inner.BoundaryReached+=e=>status.Content=$"{inner.Overscroll}: unused wheel {e.UnconsumedDelta.Y:0.##}";
        root.Children.Add(controls);root.Children.Add(outer);root.Children.Add(status);window.Content=root;
        if(smokeTest){window.Input.Focus(inner);int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();};}
    });
    return;
}
if(args.Contains("--list-demo"))
{
    var people=new System.Collections.ObjectModel.ObservableCollection<SampleListPerson>(Enumerable.Range(1,40).Select(i=>new SampleListPerson($"Person {i}",60+i%40)));
    SwealiumApplication.Run(new(){Title="Swealium - collection list",Width=880,Height=720},window=>
    {
        var root=new Container{Padding=new(22),Gap=14,Background=new(23,29,40)};
        root.Children.Add(new Text{Content="Collection templates, owned scrolling, focus routing",Style=new(){FontSize=24,Foreground=Color.White}});
        var status=new Text{Content="Choose an item or focus a scrolling region.",Style=new(){Foreground=new(180,205,230)}};
        var list=Swealium.UI.List.Create(people,(person,index)=>
        {
            var row=new Container{Layout=LayoutMode.Horizontal,Height=48,Padding=new(8),Gap=10,Background=index%2==0?new Color(38,48,65):new Color(30,39,55),Radii=new(6)};
            row.Children.Add(new Text{Content=$"{index+1}. {person.Name} - score {person.Score}",Weight=1,WeightBasis=WeightBasis.Zero,Style=new(){Foreground=Color.White}});
            var action=new Button{Text="Open",Width=85};action.Click+=()=>status.Content=$"Opened {person.Name}, score {person.Score}";row.Children.Add(action);return row;
        });
        list.Height=260;list.Gap=4;list.Padding=new(6);list.Radii=new(10);list.Stroke=new(2,new(65,110,165));
        list.VerticalScrollBarStyle=new(){Thickness=12,TrackColor=new(25,35,50),ThumbColor=new(70,155,225),HoverThumbColor=new(110,190,250)};
        var outer=new ScrollContainer{Weight=1,WeightBasis=WeightBasis.Zero,Padding=new(10),Gap=14,Background=new(18,23,32)};
        outer.Children.Add(list);outer.Children.Add(new Text{Height=320,Content="Outer scrolling content.\n\nClick Focus outer to scroll this region.\n\nClick a list row or Focus list to scroll the list.\n\nThe focused list keeps wheel ownership at its boundaries.",Style=new(){Foreground=new(180,200,225),FontSize=20}});
        var controls=new Container{Layout=LayoutMode.Horizontal,Gap=8,Height=44};
        var focusList=new Button{Text="Focus list",Weight=1};focusList.Click+=()=>window.Input.Focus(list);
        var focusOuter=new Button{Text="Focus outer",Weight=1};focusOuter.Click+=()=>window.Input.Focus(outer);
        var add=new Button{Text="Add row",Weight=1};add.Click+=()=>people.Add(new($"Added {people.Count+1}",100));
        var bars=new Button{Text="Toggle list bar",Weight=1};bars.Click+=()=>list.VerticalScrollBarVisibility=list.VerticalScrollBarVisibility==ScrollBarVisibility.Hidden?ScrollBarVisibility.Auto:ScrollBarVisibility.Hidden;
        controls.Children.Add(focusList);controls.Children.Add(focusOuter);controls.Children.Add(add);controls.Children.Add(bars);
        root.Children.Add(controls);root.Children.Add(outer);root.Children.Add(status);window.Content=root;
        if(smokeTest){people.Add(new("Observable update",100));window.Input.Focus(list);int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();};}
    });
    return;
}
if(args.Contains("--selection-demo"))
{
    SwealiumApplication.Run(new(){Title="Swealium - text selection and composition",Width=850,Height=620},window=>
    {
        var root=new Container{Padding=new(24),Gap=18,Background=new(25,30,42)};
        root.Children.Add(new Text{Content="Double click: word. Triple click: line. Fourth click: all.",Selectable=true,Style=new(){FontSize=23,Foreground=Color.White}});
        root.Children.Add(new Text{Content="<b>Drag from the heading into this paragraph</b>: one shared selection. Unicode: éclair, 日本語, مرحبا, 😀",Markup=TextMarkup.Html,Selectable=true,Style=new(){FontSize=20,Foreground=new(185,220,250)}});
        root.Children.Add(new TextBox{Placeholder="A custom placeholder color",PlaceholderColor=Gradient.Horizontal(new(160,70,210),new(20,145,170)),Height=50});
        root.Children.Add(new TextBox{Multiline=true,Text="Try word selection, whole-line dragging, or select all.\nThis is the standard TextBox.",Height=110});
        var custom=new ComposedSampleEditor{Height=150};custom.Editor.Text="This editor inherits Widget directly.\nEditing, clipboard, undo, selection, and IME come from TextEditor.";
        root.Children.Add(custom);window.Content=root;
        if(smokeTest){window.Input.Focus(custom);window.Input.ProcessComposition(new("preedit"));window.Input.ProcessCandidates(new(new[]{"candidate"},0));int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();};}
    });
    return;
}
if(args.Contains("--gradient-demo"))
{
    Color purple=new(145,80,235),teal=new(25,205,185),gold=new(255,190,65);
    GradientStop[] stops=[new(0,purple),new(.55f,teal),new(1,gold)];
    Gradient[] choices=[Gradient.Horizontal(purple,teal,gold),Gradient.Radial(new(.5f,.5f),.5f,stops),
        Gradient.Elliptical(new(.5f,.5f),new(.5f,.3f),stops),Gradient.Sweep(new(.5f,.5f),stops),
        Gradient.TwoPointConical(new(.2f,.4f),.03f,new(.6f,.5f),.6f,stops),
        Gradient.Diamond(new(.5f,.5f),new(.5f,.5f),stops),Gradient.Square(new(.5f,.5f),new(.5f,.5f),stops)];
    SwealiumApplication.Run(new(){Title="Swealium - gradients",Width=1000,Height=760,Background=Gradient.Vertical(new(15,20,35),new(35,20,50))},window=>
    {
        var root=new Container{Padding=new(24),Gap=16};
        root.Children.Add(new Text{Content="One Color property. Seven gradient kinds.",Style=new(){FontSize=30,Foreground=Gradient.Horizontal(teal,gold)}});
        var scroll=new ScrollContainer{Weight=1,WeightBasis=WeightBasis.Zero};
        var cards=new WrapContainer{Gap=16,LineGap=16};scroll.Children.Add(cards);root.Children.Add(scroll);
        foreach(var gradient in choices)
        {
            var card=new Container{Width=285,Height=170,Padding=new(16),Gap=10,Radii=new(20),Background=gradient,Stroke=new(3,Gradient.Vertical(Color.White,purple))};
            card.Children.Add(new Text{Content=gradient.Kind.ToString(),Style=new(){FontSize=22,Foreground=Color.White}});
            card.Children.Add(new Text{Content="Relative bounds, shared immutable stops",Style=new(){Foreground=Color.White}});
            cards.Children.Add(card);
        }
        root.Children.Add(new TextBox{Text="Select this text to see a gradient highlight",Height=54,SelectionColor=Gradient.Horizontal(purple,teal),Background=Gradient.Horizontal(Color.White,new(210,235,255))});
        window.Content=root;
        if(smokeTest){int frames=0;window.Paint+=(_,_)=>{if(++frames==3)window.RequestClose();};}
    });
    return;
}
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


internal sealed class ComposedSampleEditor:Widget
{
    public ComposedSampleEditor(){Editor=new(this){Multiline=true,Style=new(){FontSize=18,Foreground=new(235,240,255)}};Padding=new(12);Radii=new(10);Stroke=new(2,new(85,135,190));}
    public TextEditor Editor {get;}
    protected override Size MeasureContent(Size available)=>Editor.Measure(available);
    public override void Draw(DrawingContext drawing)
    {
        drawing.FillRectangle(new(0,0,ActualWidth,ActualHeight),new Color(40,50,70),Radii);
        Editor.Draw(drawing);
    }
}

internal sealed record SampleListPerson(string Name,int Score);
