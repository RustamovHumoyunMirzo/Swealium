using Swealium.Graphics;
using Swealium.UI;
using SkiaSharp;
namespace Swealium.Tests;

internal static class FontTests
{
    private static string PathFor(string name)=>Path.Combine(AppContext.BaseDirectory,"Fixtures","Fonts",name);
    private static Font Fixture()=>Font.Load(PathFor("SwealiumTestVariable.ttf"));
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Near(float actual,float expected)=>Check(Math.Abs(actual-expected)<.03f,$"Expected {expected}, got {actual}");
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    internal static void LoadingAndMetrics()
    {
        using var font=Fixture();Check(font.FamilyName=="Swealium Test Variable"&&font.GlyphCount==10&&font.UnitsPerEm==1000,"File loading exposes actual face metadata");
        Check(font.FeatureTags.Contains("kern")&&font.FeatureTags.Contains("liga")&&font.FeatureTags.Contains("ss01")&&font.VariationAxes.Single().Tag=="wght","Feature/axis discovery reads real OpenType tables");
        var metrics=font.GetMetrics(50);Near(metrics.Ascent,40);Near(metrics.Descent,10);Near(metrics.LineHeight,50);
        Check(font.ContainsGlyph('A')&&font.ContainsGlyphs("AV fi")&&!font.ContainsGlyph(0x1f600),"Coverage queries distinguish missing glyphs");
        using var stream=File.OpenRead(PathFor("Aldo.ttf"));using var aldo=Font.Load(stream);Check(stream.CanRead&&aldo.GlyphCount>100&&aldo.ContainsGlyphs("Swealium"),"Aldo loads without installation and input streams stay open");
        var bytes=File.ReadAllBytes(PathFor("Aldo.ttf"));using var copy=Font.FromBytes(bytes);Array.Clear(bytes);using var shaped=copy.Shape("Aldo 0123456789",32);Check(shaped.Size.Width>0&&!shaped.HasMissingGlyphs,"Fonts own their bytes independently from the caller");
        using var encoded=Font.FromBase64(Convert.ToBase64String(File.ReadAllBytes(PathFor("Aldo.ttf"))));Check(encoded.FamilyName==aldo.FamilyName,"Base64 loading");
        using var system=Font.Match();Check(system.GlyphCount>0&&Font.GetSystemFamilies().Count>0,"System font matching and family inventory");
        using var firstFace=Font.Load(PathFor("SwealiumTestCollection.ttc"),0);using var secondFace=Font.Load(PathFor("SwealiumTestCollection.ttc"),1);Check(firstFace.Weight==400&&secondFace.Weight==900,"Collection indices select distinct static faces");
        var systemOne=Font.Match();using var systemTwo=Font.Match();systemOne.Dispose();using var afterDispose=systemTwo.Shape("Font",16);Check(afterDispose.Size.Width>0,"Separately matched fonts have independent lifetimes");
        Throws<InvalidDataException>(()=>Font.FromBytes([1,2,3]));Throws<ArgumentOutOfRangeException>(()=>Font.FromBytes([1],-1));Throws<ArgumentOutOfRangeException>(()=>font.ContainsGlyph(0xd800));Throws<ArgumentOutOfRangeException>(()=>font.GetMetrics(0));
    }
    internal static void ShapingAndVariations()
    {
        using var font=Fixture();using var ligature=font.Shape("fi",50);using var separate=font.Shape("fi",50,new(){Features=[FontFeature.Ligatures(false)]});Check(ligature.Glyphs.Count==1&&separate.Glyphs.Count==2,"liga changes real glyph substitution");
        using var kerned=font.Shape("AV",50,new(){Language="en"});using var unkerned=font.Shape("AV",50,new(){Language="en",Features=[FontFeature.Kerning(false)]});Near(kerned.Size.Width,54);Near(unkerned.Size.Width,60);
        using var regular=font.Shape("A",50);using var alternate=font.Shape("A",50,new(){Features=[FontFeature.StylisticSet(1)]});Check(regular.Glyphs[0].Id!=alternate.Glyphs[0].Id,"ss01 selects a different glyph");
        using var number=font.Shape("1",50);using var tabular=font.Shape("1",50,new(){Features=[new("tnum")]});Near(tabular.Size.Width-number.Size.Width,5);
        using var ranged=font.Shape("fifi",50,new(){Features=[FontFeature.Ligatures(false),new("liga",1,2,4)]});Check(ranged.Glyphs.Count==3&&ranged.Glyphs[2].Cluster==2,"Feature ranges use half-open UTF-16 source offsets");
        using var tracked=font.Shape("AAA",50,new(){LetterSpacing=5});Near(tracked.Size.Width,100);Near(tracked.Glyphs[1].Position.X,35);
        using var rtl=font.Shape("AV",50,new(){Direction=TextDirection.RightToLeft,Language="en",Script="Latn"});Check(rtl.Direction==TextDirection.RightToLeft&&rtl.Glyphs[0].Cluster==1,"Explicit direction/script/language reaches the shaper");
        using var english=font.Shape("V",50,new(){Language="en",Script="Latn"});using var serbian=font.Shape("V",50,new(){Language="sr",Script="Latn"});Check(english.Glyphs[0].Id!=serbian.Glyphs[0].Id,"Language-specific locl substitution is applied");
        using var missing=font.Shape("A\ud83d\ude00",50);Check(missing.HasMissingGlyphs&&missing.Glyphs.Any(g=>g.Cluster==1),"Missing astral characters retain UTF-16 source clusters");
        using var heavy=font.WithVariations([new("wght",900)]);using var wider=heavy.Shape("A",50);Near(regular.Size.Width,30);Near(wider.Size.Width,45);Check(heavy.Variations.Any(v=>v.Tag=="wght"&&v.Value==900),"Variable axes affect shaping advances and font identity");
        Throws<NotSupportedException>(()=>new TextLayout("AAA",new(){Font=heavy,FontSize=50}));
        using var aldo=Font.Load(PathFor("Aldo.ttf"));using var aldoLayout=new TextLayout("A",new(){Font=aldo,FontSize=50});
        using var mixed=new TextLayout(new TextRun[]{new("A",new(){Font=font,FontSize=50}),new("A",new(){Font=aldo,FontSize=50})});Near(mixed.Size.Width,30+aldoLayout.Size.Width);
        var settings=new List<FontFeature>{FontFeature.Ligatures(false)};using var snapshot=font.Shape("fi",20,new(){Features=settings});settings.Clear();Check(snapshot.Options.Features.Count==1&&snapshot.Glyphs.Count==2,"Shaped segments snapshot caller-owned feature lists");
        using var empty=font.Shape("",20);Check(empty.Glyphs.Count==0&&empty.Size.Width==0,"Empty segments still supply line metrics");
        Throws<ArgumentException>(()=>font.WithVariations([new("nope",1)]));Throws<ArgumentOutOfRangeException>(()=>font.WithVariations([new("wght",901)]));Throws<ArgumentException>(()=>font.Shape("line\nnext"));Throws<ArgumentException>(()=>font.Shape("A\ud83d\ude00",20,new(){Features=[new("liga",0,2)]}));Throws<ArgumentException>(()=>font.Shape("A",20,new(){Features=[default]}));Throws<ArgumentOutOfRangeException>(()=>font.Shape("A",20,new(){Features=[new("liga",0,0,2)]}));
    }
    internal static void DrawingAndLifetime()
    {
        using var bitmap=new SKBitmap(300,160);using var surface=SKSurface.Create(bitmap.Info,bitmap.GetPixels(),bitmap.RowBytes)!;var font=Fixture();var shaped=font.Shape("AV fi",40);var layout=new TextLayout("AV fi",new(){Font=font,FontSize=40});font.Dispose();
        var drawing=new SkiaDrawingContext(surface.Canvas);surface.Canvas.Clear(SKColors.Transparent);drawing.DrawShapedText(shaped,new(10,55),Gradient.Horizontal(new(255,0,0),new(0,0,255)),clip:new(10,0,55,70));
        Check(bitmap.GetPixel(15,40).Alpha>0&&bitmap.GetPixel(90,40).Alpha==0,"Shaped drawing supports baseline placement, gradients and clipping after source font disposal");
        drawing.DrawTextLayout(layout,new(10,80));Check(bitmap.GetPixel(15,100).Alpha>0,"Existing paragraph layout also keeps a font lease");
        Throws<ObjectDisposedException>(()=>font.Shape("A"));Throws<ObjectDisposedException>(()=>new TextLayout("A",new(){Font=font}));shaped.Dispose();Throws<ObjectDisposedException>(()=>drawing.DrawShapedText(shaped,default,Color.White));layout.Dispose();
        using var aldo=Font.Load(PathFor("Aldo.ttf"));var style=new TextStyle{Font=aldo,FontSize=32,Foreground=Color.White};drawing.DrawText("Aldo",new Point(10,45),style);var measured=drawing.MeasureText("Aldo",style);Check(measured.Width>0&&measured.Height>20,"Style-based baseline drawing and measurement support loaded fonts");
        drawing.DrawText("Aldo",new Point(10,120),aldo,Color.White,32,new(){Features=[FontFeature.Kerning()]});Check(drawing.MeasureText("Aldo",aldo,32).Width>0,"Immediate explicit-font overloads use the shaping path");
        using var edges=aldo.Shape("Aldo",37,new(){Hinting=FontHinting.None});
        int PartialPixels(){int count=0;for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++){byte alpha=bitmap.GetPixel(x,y).Alpha;if(alpha>0&&alpha<255)count++;}return count;}
        surface.Canvas.Clear(SKColors.Transparent);drawing.AntiAliasing=true;drawing.DrawShapedText(edges,new(10.3f,60.7f),Color.White);Check(PartialPixels()>0,"Prepared glyphs respect antialiasing enabled at draw time");
        surface.Canvas.Clear(SKColors.Transparent);drawing.AntiAliasing=false;drawing.DrawShapedText(edges,new(10.3f,60.7f),Color.White);Check(PartialPixels()==0,"The same segment can reuse a separately cached aliased glyph blob");
        using var recorded=VectorImage.Create(new(0,0,180,70),d=>{using var cached=aldo.Shape("Aldo",40);d.DrawShapedText(cached,new(5,50),Color.White);});using var raster=recorded.Rasterize(180,70);Check(raster.CopyPixels().Where((_,i)=>i%4==3).Any(a=>a>0),"Vector recordings retain shaped drawing independently of disposed glyph blobs");
        drawing.End();Throws<ObjectDisposedException>(()=>drawing.DrawText("A",default,style));Throws<ObjectDisposedException>(()=>drawing.MeasureText("A",aldo));
        var root=new Container();var label=new Text{Content="Aldo loaded font",Style=style,Selectable=true};var editor=new TextBox{Text="Aldo editor",Style=style};var button=new Button{Text="Aldo button",Style=style};root.Children.Add(label);root.Children.Add(editor);root.Children.Add(button);LayoutEngine.Layout(root,new(300,300));Check(label.DesiredSize.Width>0&&editor.ActualHeight>0&&button.FontSize==32,"Text, selectable text, editors and buttons share font-aware layout");
        surface.Canvas.Clear(SKColors.Transparent);WidgetRenderer.Render(surface.Canvas,root);
        using var html=new TextLayout("Aldo <code>system mono</code>",style,markup:TextMarkup.Html);Check(html.Runs.First().Style.Font==aldo&&html.Runs.Any(r=>r.Style.Font is null&&r.Style.FontFamily=="monospace"),"Explicit HTML family changes override the inherited loaded face");
    }
    internal static void Gpu(SKSurface surface)
    {
        using var font=Fixture();using var shaped=font.Shape("A",24);surface.Canvas.Clear(SKColors.Transparent);var drawing=new SkiaDrawingContext(surface.Canvas);try{drawing.DrawShapedText(shaped,new(2,25),new(255,0,0));}finally{drawing.End();}
        using var image=surface.Snapshot();using var pixels=SKBitmap.FromImage(image);Check(pixels.GetPixel(8,15).Red>240&&pixels.GetPixel(28,28).Alpha==0,"GPU draws the same font glyph blob with baseline positioning");
    }
}
