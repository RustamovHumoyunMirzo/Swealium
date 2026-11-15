using SkiaSharp;
using Swealium.UI;
namespace Swealium.Graphics;

internal static class WidgetStrokeRenderer
{
    private readonly record struct Sample(SKPoint Outer,SKPoint Inner,SKColor Color,StrokeStyle Style,float Width);
    internal static void Draw(SKCanvas canvas,Widget widget)
    {
        Stroke stroke=widget.Stroke;float w=widget.ActualWidth,h=widget.ActualHeight;
        float l=Width(stroke.Left),t=Width(stroke.Top),r=Width(stroke.Right),b=Width(stroke.Bottom);
        if (w<=0||h<=0||(l==0&&t==0&&r==0&&b==0)) return;
        double horizontal=(double)l+r,vertical=(double)t+b;
        if (horizontal>w) {float scale=(float)(w/horizontal);l*=scale;r*=scale;}
        if (vertical>h) {float scale=(float)(h/vertical);t*=scale;b*=scale;}
        var radius=widget.Radii.Normalize(new(w,h));
        float iw=Math.Max(0,w-l-r),ih=Math.Max(0,h-t-b);
        SKPoint[] innerRadii=[new(Math.Max(0,radius.TopLeft-l),Math.Max(0,radius.TopLeft-t)),
            new(Math.Max(0,radius.TopRight-r),Math.Max(0,radius.TopRight-t)),
            new(Math.Max(0,radius.BottomRight-r),Math.Max(0,radius.BottomRight-b)),
            new(Math.Max(0,radius.BottomLeft-l),Math.Max(0,radius.BottomLeft-b))];
        double innerScale=1;
        Limit(iw,innerRadii[0].X+innerRadii[1].X);Limit(iw,innerRadii[3].X+innerRadii[2].X);
        Limit(ih,innerRadii[0].Y+innerRadii[3].Y);Limit(ih,innerRadii[1].Y+innerRadii[2].Y);
        for(int i=0;i<4;i++) innerRadii[i]=new((float)(innerRadii[i].X*innerScale),(float)(innerRadii[i].Y*innerScale));
        var contour=new List<Sample>();
        Corner(new(radius.TopLeft,radius.TopLeft),radius.TopLeft,new(l+innerRadii[0].X,t+innerRadii[0].Y),innerRadii[0],180,stroke.Left,stroke.Top,l,t);
        Corner(new(w-radius.TopRight,radius.TopRight),radius.TopRight,new(w-r-innerRadii[1].X,t+innerRadii[1].Y),innerRadii[1],270,stroke.Top,stroke.Right,t,r);
        Corner(new(w-radius.BottomRight,h-radius.BottomRight),radius.BottomRight,new(w-r-innerRadii[2].X,h-b-innerRadii[2].Y),innerRadii[2],0,stroke.Right,stroke.Bottom,r,b);
        Corner(new(radius.BottomLeft,h-radius.BottomLeft),radius.BottomLeft,new(l+innerRadii[3].X,h-b-innerRadii[3].Y),innerRadii[3],90,stroke.Bottom,stroke.Left,b,l);
        contour.Add(contour[0]);
        int saved=canvas.Save();
        try
        {
            using var outer=SkiaDrawingContext.RoundRect(new(0,0,w,h),radius);
            canvas.ClipRoundRect(outer,SKClipOperation.Intersect,true);
            if (iw>0&&ih>0)
            {
                using var inner=new SKRoundRect();inner.SetRectRadii(SKRect.Create(l,t,iw,ih),innerRadii);
                canvas.ClipRoundRect(inner,SKClipOperation.Difference,true);
            }
            var vertices=new List<SKPoint>();var colors=new List<SKColor>();
            var dots=new List<(SKPoint Center,float Radius,SKColor Color)>();
            SKRect clip=canvas.LocalClipBounds;
            double phase=0;
            for(int i=1;i<contour.Count;i++)
            {
                Sample a=contour[i-1],z=contour[i];
                float length=Distance(Center(a),Center(z));
                if (length<.0001f) continue;
                float unit=Math.Max(Math.Max(.5f,(a.Width+z.Width)/2),Math.Max(clip.Width,clip.Height)/4096);
                double end=phase+length/unit;
                StrokeStyle style=a.Style;
                bool visible=VisibleInterval(a,z,clip,out float first,out float last);
                double visibleStart=phase+(end-phase)*first,visibleEnd=phase+(end-phase)*last;
                if (style==StrokeStyle.Solid) Band(a,z,0,1);
                else if (style==StrokeStyle.Double) {Band(a,z,0,1f/3);Band(a,z,2f/3,1);}
                else if (style==StrokeStyle.Dashed&&visible)
                {
                    double at=visibleStart;
                    while(at<visibleEnd)
                    {
                        double stop=Math.Min(visibleEnd,(Math.Floor(at/3)+1)*3);
                        if (((long)Math.Floor(at/3)&1)==0)
                            Band(Mix(a,z,(float)((at-phase)/(end-phase))),Mix(a,z,(float)((stop-phase)/(end-phase))),0,1);
                        if (stop<=at) break;at=stop;
                    }
                }
                else if (style==StrokeStyle.Dotted&&visible)
                {
                    for(double at=Math.Ceiling(visibleStart/2)*2;at<visibleEnd;)
                    {
                        Sample dot=Mix(a,z,(float)((at-phase)/(end-phase)));
                        if(dot.Width>0&&dot.Color.Alpha>0) dots.Add((Center(dot),dot.Width/2,dot.Color));
                        double next=at+2;if(next<=at) break;at=next;
                    }
                }
                phase=end;
            }
            using var paint=new SKPaint{Color=SKColors.White,IsAntialias=true};
            if(vertices.Count>0)
            {
                using var mesh=SKVertices.CreateCopy(SKVertexMode.Triangles,vertices.ToArray(),colors.ToArray());
                canvas.DrawVertices(mesh,SKBlendMode.Modulate,paint);
            }
            foreach(var dot in dots) {paint.Color=dot.Color;canvas.DrawCircle(dot.Center,dot.Radius,paint);}

            void Band(Sample a,Sample z,float start,float end)
            {
                if (a.Color.Alpha==0&&z.Color.Alpha==0) return;
                SKPoint a0=Point(a.Outer,a.Inner,start),a1=Point(a.Outer,a.Inner,end),z0=Point(z.Outer,z.Inner,start),z1=Point(z.Outer,z.Inner,end);
                vertices.AddRange([a0,z0,z1,a0,z1,a1]);colors.AddRange([a.Color,z.Color,z.Color,a.Color,z.Color,a.Color]);
            }
        }
        finally {canvas.RestoreToCount(saved);}

        void Limit(float edge,double sum) {if(sum>0) innerScale=Math.Min(innerScale,edge/sum);}
        void Corner(SKPoint outerCenter,float outerRadius,SKPoint innerCenter,SKPoint innerRadius,float degrees,StrokeSide before,StrokeSide after,float beforeWidth,float afterWidth)
        {
            // Colors transition around the curved perimeter rather than across a fixed diagonal wedge.
            int steps=(int)Math.Clamp(Math.Ceiling(Math.Sqrt(Math.Max(outerRadius,Math.Max(innerRadius.X,innerRadius.Y)))*3),8,96);
            for(int i=0;i<=steps;i++)
            {
                float fraction=(float)i/steps,angle=(degrees+fraction*90)*MathF.PI/180;
                float c=MathF.Cos(angle),s=MathF.Sin(angle),blend=fraction*fraction*(3-2*fraction);
                contour.Add(new(new(outerCenter.X+outerRadius*c,outerCenter.Y+outerRadius*s),
                    new(innerCenter.X+innerRadius.X*c,innerCenter.Y+innerRadius.Y*s),
                    Color(before.Color,after.Color,blend),fraction<.5f?before.Style:after.Style,beforeWidth+(afterWidth-beforeWidth)*blend));
            }
        }
    }
    private static float Width(StrokeSide side)=>side.Style==StrokeStyle.Hidden?0:side.Width;
    private static SKPoint Point(SKPoint a,SKPoint z,float t)=>new(a.X+(z.X-a.X)*t,a.Y+(z.Y-a.Y)*t);
    private static SKPoint Center(Sample sample)=>Point(sample.Outer,sample.Inner,.5f);
    private static float Distance(SKPoint a,SKPoint z)=> (float)Math.Sqrt(((double)z.X-a.X)*(z.X-a.X)+((double)z.Y-a.Y)*(z.Y-a.Y));
    private static bool VisibleInterval(Sample a,Sample z,SKRect clip,out float first,out float last)
    {
        SKPoint p=Center(a),q=Center(z);double start=0,end=1;
        double spread=Math.Max(a.Width,z.Width)+1;
        bool visible=Axis(p.X,q.X-p.X,clip.Left-spread,clip.Right+spread)&&Axis(p.Y,q.Y-p.Y,clip.Top-spread,clip.Bottom+spread);
        first=(float)start;last=(float)end;return visible;
        bool Axis(double origin,double delta,double minimum,double maximum)
        {
            if(Math.Abs(delta)<.000001) return origin>=minimum&&origin<=maximum;
            double low=(minimum-origin)/delta,high=(maximum-origin)/delta;
            if(low>high) (low,high)=(high,low);
            start=Math.Max(start,low);end=Math.Min(end,high);return start<=end;
        }
    }
    private static SKColor Color(Color a,Color z,float t)=>new(Channel(a.R,z.R,t),Channel(a.G,z.G,t),Channel(a.B,z.B,t),Channel(a.A,z.A,t));
    private static byte Channel(byte a,byte z,float t)=>(byte)Math.Clamp((int)MathF.Round(a+(z-a)*t),0,255);
    private static Sample Mix(Sample a,Sample z,float t)=>new(Point(a.Outer,z.Outer,t),Point(a.Inner,z.Inner,t),
        new(Channel(a.Color.Red,z.Color.Red,t),Channel(a.Color.Green,z.Color.Green,t),Channel(a.Color.Blue,z.Color.Blue,t),Channel(a.Color.Alpha,z.Color.Alpha,t)),a.Style,a.Width+(z.Width-a.Width)*t);
}
