using Swealium;
using Swealium.Graphics;
using Swealium.UI;

// This separate app must restore against a packed/public Swealium, not the source project.
using var font=Font.Match();
using var glyphs=font.Shape("Swealium package",18);
using var bitmap=new Bitmap(220,70);
bitmap.Draw(drawing=>
{
    drawing.DrawShapedText(glyphs,new(4,30),Color.White);
    drawing.DrawText("<b>HTML dependency</b>",new Rect(4,35,200,30),markup:TextMarkup.Html);
});
if(!bitmap.CopyPixels().Where((_,i)=>i%4==3).Any(alpha=>alpha>0))throw new InvalidOperationException("Packaged font/raster dependencies did not render.");
using var vector=VectorImage.Parse("<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><circle cx='10' cy='10' r='8' fill='red'/></svg>");
using var png=Bitmap.Load(new MemoryStream(bitmap.Encode(BitmapFormat.Png)));
SwealiumApplication.HardwareAcceleration=false;
SwealiumApplication.Run(new(){Width=240,Height=100,Title="Swealium NuGet consumer"},window=>
{
    window.Content=new DrawingCanvas((drawing,_)=>
    {
        drawing.DrawBitmap(png,new Point(0,0));
        drawing.DrawVector(vector,new(215,5,20,20));
    });
    window.Render();window.RequestClose();
});
Console.WriteLine("PASS NuGet-only consumer: font/shaping, raster, HTML, SVG, PNG decoding, SDL window and presentation.");
