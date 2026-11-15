using System.IO.Compression;
using System.Text;
using SkiaSharp;
using Swealium;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.Native;
using Swealium.UI;
using Bitmap=Swealium.Graphics.Bitmap;
using Image=Swealium.UI.Image;
namespace Swealium.Tests;

internal static class MediaTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException($"Expected {typeof(T).Name}");}
    internal static void BitmapFormats()
    {
        using var original=Bitmap.FromPixels(2,2,new byte[]{255,0,0,255,0,255,0,255,0,0,255,255,255,255,255,100});
        foreach(var format in Enum.GetValues<BitmapFormat>())
        {
            byte[] encoded=original.Encode(format,100);using var decoded=Bitmap.Decode(encoded);
            Check(decoded.Width==2&&decoded.Height==2,$"{format} encoding/decoding");
            if(format==BitmapFormat.Png)Check(decoded.GetPixel(0,0)==new Color(255,0,0)&&Math.Abs(decoded.GetPixel(1,1).A-100)<=1,"PNG round trip preserves RGBA");
        }
        var fixtures=new Dictionary<string,byte[]>();
        fixtures["GIF"]=new byte[]{71,73,70,56,57,97,1,0,1,0,128,0,0,0,0,0,255,255,255,44,0,0,0,0,1,0,1,0,0,2,2,68,1,0,59};
        fixtures["TGA"]=new byte[]{0,0,2,0,0,0,0,0,0,0,0,0,1,0,1,0,24,32,0,0,255};
        fixtures["PNM"]=Encoding.ASCII.GetBytes("P6\n1 1\n255\n").Concat(new byte[]{255,0,0}).ToArray();
        fixtures["WBMP"]=new byte[]{0,0,1,1,0};
        using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
        {
            writer.Write(Encoding.ASCII.GetBytes("BM"));writer.Write(58);writer.Write(0);writer.Write(54);writer.Write(40);writer.Write(1);writer.Write(1);
            writer.Write((ushort)1);writer.Write((ushort)24);writer.Write(0);writer.Write(4);writer.Write(new byte[16]);writer.Write(new byte[]{0,0,255,0});fixtures["BMP"]=stream.ToArray();
        }
        using(var one=new Bitmap(1,1,new(255,0,0)))
        using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
        {
            byte[] png=one.Encode();writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)1);writer.Write(new byte[]{1,1,0,0});
            writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(png.Length);writer.Write(22);writer.Write(png);fixtures["ICO"]=stream.ToArray();
        }
        using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
        {
            writer.Write(Encoding.ASCII.GetBytes("8BPS"));writer.Write(new byte[]{0,1});writer.Write(new byte[6]);writer.Write(new byte[]{0,3});
            writer.Write(new byte[]{0,0,0,1,0,0,0,1,0,8,0,3});writer.Write(new byte[12]);writer.Write(new byte[]{0,0,255,0,0});fixtures["PSD"]=stream.ToArray();
        }
        fixtures["HDR"]=Encoding.ASCII.GetBytes("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 1 +X 1\n").Concat(new byte[]{128,64,32,129}).ToArray();
        foreach(var fixture in fixtures)
        {
            Bitmap decoded;
            try{decoded=Bitmap.Decode(fixture.Value);}catch(Exception error){throw new InvalidOperationException($"{fixture.Key} fixture failed",error);}
            using var owned=decoded;Check(decoded.Width==1&&decoded.Height==1,$"{fixture.Key} fixture dimensions");
            if(fixture.Key is "TGA" or "PNM" or "PSD" or "BMP" or "ICO")Check(decoded.GetPixel(0,0).R==255&&decoded.GetPixel(0,0).G==0,$"{fixture.Key} pixel decoding");
        }
        using var input=new MemoryStream(original.Encode());using var loaded=Bitmap.Load(input);Check(input.CanRead,"Caller stream remains open");
        using(var ascii=Bitmap.Decode(Encoding.ASCII.GetBytes("P2\n# comment\n2 1\n15\n0 15\n")))
            Check(ascii.GetPixel(0,0).R==0&&ascii.GetPixel(1,0).R==255,"ASCII PGM comments/sample scaling");
        using(var packed=Bitmap.Decode(Encoding.ASCII.GetBytes("P4\n9 1\n").Concat(new byte[]{128,0}).ToArray()))
            Check(packed.GetPixel(0,0).R==0&&packed.GetPixel(8,0).R==255,"Packed PBM row boundaries");
        using(var high=Bitmap.Decode(Encoding.ASCII.GetBytes("P5\n1 1\n65535\n").Concat(new byte[]{128,0}).ToArray()))
            Check(high.GetPixel(0,0).R==128,"16-bit Netpbm samples convert to 8-bit");
        CheckExif();
        Throws<InvalidDataException>(()=>Bitmap.Decode(new byte[]{1,2,3}));
        Throws<InvalidDataException>(()=>Bitmap.Decode(original.Encode(),new(){MaximumPixels=1}));
        Throws<InvalidDataException>(()=>Bitmap.Load(new MemoryStream(original.Encode()),new(){MaximumEncodedBytes=4}));
    }
    private static void CheckExif()
    {
        using var pattern=Bitmap.FromPixels(3,2,new byte[]{255,0,0,255,0,255,0,255,0,0,255,255,255,255,0,255,255,0,255,255,0,255,255,255});
        byte[] jpeg=pattern.Encode(BitmapFormat.Jpeg,100);using var plain=Bitmap.Decode(jpeg);
        for(int orientation=1;orientation<=8;orientation++)
        {
            byte[] exif=Encoding.ASCII.GetBytes("Exif\0\0").Concat(new byte[]{73,73,42,0,8,0,0,0,1,0,18,1,3,0,1,0,0,0,(byte)orientation,0,0,0,0,0,0,0}).ToArray();
            byte[] tagged=jpeg[..2].Concat(new byte[]{255,225,(byte)((exif.Length+2)>>8),(byte)(exif.Length+2)}).Concat(exif).Concat(jpeg[2..]).ToArray();
            using var decoded=Bitmap.Decode(tagged);Check(decoded.Width==(orientation>=5?2:3)&&decoded.Height==(orientation>=5?3:2),$"EXIF {orientation} dimensions");
            for(int y=0;y<2;y++)for(int x=0;x<3;x++)
            {
                var destination=orientation switch{2=>(2-x,y),3=>(2-x,1-y),4=>(x,1-y),5=>(y,x),6=>(1-y,x),7=>(1-y,2-x),8=>(y,2-x),_=>(x,y)};
                Check(decoded.GetPixel(destination.Item1,destination.Item2)==plain.GetPixel(x,y),$"EXIF {orientation} rotates/mirrors pixels correctly");
            }
        }
    }
    internal static void BitmapDrawing()
    {
        using var source=new Bitmap(4,4,new(255,0,0));source.SetPixel(3,3,new(0,0,255));
        using var clone=source.Clone();source.SetPixel(0,0,new(0,255,0));Check(clone.GetPixel(0,0).R==255,"Clones are independent");
        using var target=new Bitmap(60,50,Color.White);DrawingContext? expired=null;
        target.Draw(context=>
        {
            expired=context;context.DrawBitmap(source,new Rect(5,5,20,20),new Rect(3,3,1,1),interpolation:BitmapInterpolation.Nearest);
            context.DrawBitmap(source,new Rect(30,5,20,20),new Rect(0,0,1,1),opacity:.5f);
        });
        Check(target.GetPixel(10,10)==new Color(0,0,255),"Source rectangle crops and scales bitmap");
        Check(target.GetPixel(35,10).G==255&&target.GetPixel(35,10).R is >=126 and <=129,"Opacity composites over the target");
        source.SetPixel(3,3,new(255,255,0));target.Draw(context=>context.DrawBitmap(source,new Rect(5,5,20,20),new Rect(3,3,1,1),interpolation:BitmapInterpolation.Nearest));
        Check(target.GetPixel(10,10)==new Color(255,255,0),"Pixel mutation invalidates an already-used native image snapshot");
        Throws<ObjectDisposedException>(()=>expired!.DrawBitmap(source,new Rect(0,0,1,1)));
        Throws<InvalidOperationException>(()=>target.Draw(_=>target.Dispose()));
        Throws<InvalidOperationException>(()=>target.Draw(_=>target.Draw(_=>{})));
        Throws<ArgumentOutOfRangeException>(()=>target.Draw(c=>c.DrawBitmap(source,new Rect(0,0,10,10),new Rect(9,0,1,1))));
        using var stream=new MemoryStream();target.Save(stream);Check(stream.CanWrite,"Output stream stays open");
        source.Dispose();Throws<ObjectDisposedException>(()=>target.Draw(c=>c.DrawBitmap(source,new Rect(0,0,1,1))));
    }
    internal static void VectorRendering()
    {
        const string xml="<svg xmlns='http://www.w3.org/2000/svg' width='100' height='50' viewBox='10 20 100 50'><defs><linearGradient id='g'><stop stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs><rect x='10' y='20' width='100' height='50' fill='url(#g)'/><circle cx='60' cy='45' r='10' fill='lime'/></svg>";
        using var svg=VectorImage.Parse(xml);Check(svg.Size==new Size(100,50),"SVG dimensions and viewBox");
        using var raster=svg.Rasterize(200,100);Check(raster.GetPixel(100,50).G>200&&raster.GetPixel(3,3).R>200,"SVG gradients, transforms, and geometry rasterize");
        using var gzip=new MemoryStream();using(var compressed=new GZipStream(gzip,CompressionMode.Compress,true)){compressed.Write(Encoding.UTF8.GetBytes(xml));}
        gzip.Position=0;using var svgz=VectorImage.Load(gzip);Check(svgz.Size==svg.Size&&gzip.CanRead,"SVGZ decoding preserves caller stream");
        using var path=VectorPath.Parse("M 5 5 L 35 5 L 35 35 L 5 35 Z");Check(path.Contains(new(20,20))&&!path.Contains(new(50,20)),"Public path containment");
        path.AddRectangle(new(12,12,16,16));path.FillRule=PathFillRule.EvenOdd;Check(!path.Contains(new(20,20)),"EvenOdd path creates holes");
        using var vector=VectorImage.Create(new(10,20,80,60),context=>{context.FillRectangle(new(10,20,80,60),new(255,0,0));context.FillPath(path,new(0,0,255));context.StrokePath(path,Color.White,2,PathLineCap.Round,PathLineJoin.Round);});
        using var recorded=vector.Rasterize(160,120);Check(recorded.GetPixel(100,60).R==255,"Recorded vector handles a nonzero origin");
        using var texture=new Bitmap(1,1,new(255,0,0));
        using var retained=VectorImage.Create(new(0,0,10,10),drawing=>drawing.DrawBitmap(texture,new Rect(0,0,10,10)));
        texture.SetPixel(0,0,new(0,0,255));texture.Dispose();
        using var snapshot=retained.Rasterize(10,10);Check(snapshot.GetPixel(5,5).R==255,"Recorded vectors retain the image snapshot after source mutation/disposal");
        Directory.CreateDirectory("artifacts");using var green=new Bitmap(20,20,new(0,255,0));green.Save("artifacts/external-image.png");
        string uri=new Uri(Path.GetFullPath("artifacts/external-image.png")).AbsoluteUri;
        using var external=VectorImage.Parse($"<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><image href='{uri}' width='20' height='20'/><rect width='5' height='5' fill='red'/></svg>");
        using var blocked=external.Rasterize(20,20);Check(blocked.GetPixel(15,15).G<200&&blocked.GetPixel(2,2).R==255,"SVG blocks an existing external image without blocking local shapes");
        using var embedded=VectorImage.Parse($"<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><image href='data:image/png;base64,{Convert.ToBase64String(green.Encode())}' width='20' height='20'/></svg>");
        using var inline=embedded.Rasterize(20,20);Check(inline.GetPixel(10,10).G==255,"SVG embedded bitmap data is supported");
        Throws<InvalidDataException>(()=>VectorImage.Parse("<!DOCTYPE svg [<!ENTITY x SYSTEM 'file:///unused'>]><svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'>&x;</svg>"));
        Throws<FormatException>(()=>VectorPath.Parse("not a path"));path.Dispose();Throws<ObjectDisposedException>(()=>path.Contains(default));
        svg.Dispose();Throws<ObjectDisposedException>(()=>svg.Rasterize(1,1));
    }
    internal static void DisplayWidgets()
    {
        using var bitmap=new Bitmap(100,50,new(255,0,0));
        var image=new Image{Source=bitmap,Width=80,Padding=new(10),Stretch=ImageStretch.Uniform};image.Measure(new(80,float.PositiveInfinity));
        Check(Math.Abs(image.DesiredSize.Height-50)<.02,"Image measure preserves aspect ratio at a constrained width plus padding");
        var root=new Container{Layout=LayoutMode.Absolute,Background=Color.White};image.Height=100;image.Stretch=ImageStretch.UniformToFill;root.Children.Add(image);
        LayoutEngine.Layout(root,new(300,200));using var target=new SKBitmap(300,200);using var surface=SKSurface.Create(target.Info,target.GetPixels(),target.RowBytes)!;
        WidgetRenderer.Render(surface.Canvas,root);
        Check(target.GetPixel(3,20)==SKColors.White&&target.GetPixel(20,20)==SKColors.Red,"Cover scaling clips to the padded content area");
        using var vector=VectorImage.Create(new(0,0,40,20),c=>c.FillEllipse(new(0,0,40,20),new(0,0,255)));
        var graphic=new VectorGraphics{Source=vector,X=110,Width=100,Height=100,Padding=new(10),Stretch=ImageStretch.Uniform};root.Children.Add(graphic);
        LayoutEngine.Layout(root,new(300,200));WidgetRenderer.Render(surface.Canvas,root);Check(target.GetPixel(160,50)==SKColors.Blue&&target.GetPixel(160,15)==SKColors.White,"Vector widget letterboxes with centered alignment");
        var input=new InputManager{Root=root};Check(input.HitTest(new(160,50))==graphic,"Vector widget participates in normal input and layout");
        Directory.CreateDirectory("artifacts");using var preview=SKImage.FromBitmap(target);using var png=preview.Encode(SKEncodedImageFormat.Png,100);using var file=File.Create("artifacts/media-preview.png");png.SaveTo(file);
    }
    internal static void CursorRouting()
    {
        Check(Cursor.SystemCursors.Count==34&&Cursor.SystemCursors.Select(c=>c.Kind).Distinct().Count()==34,"All SDL system cursor descriptors are available");
        Check(Cursor.Arrow==Cursor.Default&&Cursor.Pointer.Kind==CursorKind.Pointer,"Named cursor descriptors");
        Check(Cursor.VSplit==Cursor.ColumnResize&&Cursor.HSplit==Cursor.RowResize,"Split cursors map to platform column/row resize descriptors");
        var root=new Container{Cursor=Cursor.Crosshair};var child=new Container{Height=80};var button=new Button{Height=40};child.Children.Add(button);root.Children.Add(child);
        LayoutEngine.Layout(root,new(200,200));var input=new InputManager{Root=root,DefaultCursor=Cursor.Wait};
        input.ProcessPointer(new(PointerEventKind.Move,new(20,20)));Check(input.CurrentCursor==Cursor.Pointer,"Button defaults to pointer");
        button.Cursor=null;Check(input.CurrentCursor==Cursor.Crosshair,"Cursor inherits through parents");
        button.Cursor=Cursor.Grab;input.ProcessPointer(new(PointerEventKind.Down,new(20,20),PointerButton.Primary));input.ProcessPointer(new(PointerEventKind.Move,new(190,190)));
        Check(input.CurrentCursor==Cursor.Grab,"Capture retains the dragged widget cursor outside its bounds");input.Cancel();Check(input.CurrentCursor==Cursor.Wait,"Cancel returns window default cursor");
        using var bitmap=new Bitmap(8,8,new(255,0,0));using var custom=Cursor.FromBitmap(bitmap,2,3);bitmap.SetPixel(0,0,new(0,0,255));
        using var copy=custom.CopyBitmap();Check(copy.GetPixel(0,0).R==255&&custom.HotSpot==new Point(2,3),"Custom cursor snapshots source pixels and hotspot");
        input.DefaultCursor=custom;custom.Dispose();Check(input.CurrentCursor==Cursor.Default,"Disposed default custom cursor safely falls back");
        Throws<ObjectDisposedException>(()=>button.Cursor=custom);Throws<ArgumentOutOfRangeException>(()=>Cursor.FromBitmap(bitmap,8,0));Cursor.Pointer.Dispose();Check(!Cursor.Pointer.IsDisposed,"System cursor disposal cannot invalidate shared descriptors");
    }
    internal static void NativeCursors()
    {
        using var pixels=new Bitmap(16,16);pixels.Draw(c=>c.FillEllipse(new(0,0,14,14),new(255,0,0)));using var custom=Cursor.FromBitmap(pixels,4,4);
        for(int session=0;session<2;session++)
        {
            SwealiumApplication.Run(new(){Width=180,Height=120},window=>
            {
                window.Cursor=Cursor.Crosshair;Check(window.EffectiveCursor==Cursor.Crosshair,"Window default cursor getter/setter");
                foreach(var system in Cursor.SystemCursors)_=SdlCursorManager.Apply(window.NativeHandle,system);
                _=SdlCursorManager.Apply(window.NativeHandle,custom);_=SdlCursorManager.Apply(window.NativeHandle,Cursor.Hidden);_=SdlCursorManager.Apply(window.NativeHandle,Cursor.Default);
                Check(SdlCursorManager.ResourceCount>=34,"Native system/custom descriptors are cached even on cursorless headless backends");
                var child=window.CreateChild(new(){Width=100,Height=80});child.Cursor=Cursor.Help;child.Render();child.RequestClose();window.RequestClose();
            });
            Check(SdlCursorManager.ResourceCount==0,"Cursor handles are released before SDL shutdown and can be recreated in another session");
        }
    }
}
