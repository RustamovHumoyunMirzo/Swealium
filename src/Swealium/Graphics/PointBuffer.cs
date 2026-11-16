using SkiaSharp;
namespace Swealium.Graphics;

public enum DrawingDetail { Exact, Automatic }
public enum PointShape { Square, Round }

/// <summary>Reusable, validated batch geometry. Updates of the same length reuse storage. No disposal is required.</summary>
public sealed class PointBuffer
{
    private SKPoint[] points=[];
    private readonly Reduction[] reductions=new Reduction[4];
    private int nextReduction;
    private readonly record struct Reduction(int Columns,Rect View,PointBuffer? Buffer);
    internal PointBuffer ForRendering(int columns,Rect view)
    {
        if(Count<=(long)columns*4)return this;
        foreach(var cached in reductions)if(cached.Columns==columns&&cached.View==view&&cached.Buffer is {} buffer)return buffer;
        var reduced=ReduceForWidth(columns,view);reductions[nextReduction]=new(columns,view,reduced);nextReduction=(nextReduction+1)%reductions.Length;return reduced;
    }
    internal SKPoint[] NativePoints=>points;
    public PointBuffer(ReadOnlySpan<Point> points)=>Update(points);
    private PointBuffer(SKPoint[] owned){points=owned;Analyze();Version=1;}
    public int Count=>points.Length;
    public Rect Bounds {get;private set;}
    public bool IsXOrdered {get;private set;}=true;
    public long Version {get;private set;}
    public Point this[int index]
    {
        get{if((uint)index>=(uint)points.Length)throw new ArgumentOutOfRangeException(nameof(index));var p=points[index];return new(p.X,p.Y);}
    }
    public void Update(ReadOnlySpan<Point> source)
    {
        float minX=float.PositiveInfinity,minY=float.PositiveInfinity,maxX=float.NegativeInfinity,maxY=float.NegativeInfinity,previous=float.NegativeInfinity;bool ordered=true;
        foreach(var p in source)
        {
            if(!float.IsFinite(p.X)||!float.IsFinite(p.Y))throw new ArgumentOutOfRangeException(nameof(source),"Points must be finite.");
            minX=Math.Min(minX,p.X);maxX=Math.Max(maxX,p.X);minY=Math.Min(minY,p.Y);maxY=Math.Max(maxY,p.Y);ordered&=p.X>=previous;previous=p.X;
        }
        Rect bounds=source.Length==0?default:MakeBounds(minX,minY,maxX,maxY);
        var target=points.Length==source.Length?points:new SKPoint[source.Length];
        for(int i=0;i<source.Length;i++)target[i]=new(source[i].X,source[i].Y);
        points=target;Bounds=bounds;IsXOrdered=ordered;Array.Clear(reductions);nextReduction=0;Version++;
    }
    public Point[] CopyPoints()
    {var copy=new Point[Count];for(int i=0;i<copy.Length;i++)copy[i]=new(points[i].X,points[i].Y);return copy;}
    private static Rect MakeBounds(float x,float y,float right,float bottom)
    {
        double w=(double)right-x,h=(double)bottom-y;
        if(w>float.MaxValue||h>float.MaxValue)throw new ArgumentOutOfRangeException("points","Point bounds exceed the drawable range.");
        return new(x,y,(float)w,(float)h);
    }
    private void Analyze()
    {
        if(Count==0){Bounds=default;IsXOrdered=true;return;}
        float x=points[0].X,y=points[0].Y,r=x,b=y,previous=x;bool ordered=true;
        foreach(var p in points){x=Math.Min(x,p.X);y=Math.Min(y,p.Y);r=Math.Max(r,p.X);b=Math.Max(b,p.Y);ordered&=p.X>=previous;previous=p.X;}
        Bounds=MakeBounds(x,y,r,b);IsXOrdered=ordered;
    }
    /// <summary>Project once into logical drawing coordinates; degenerate source axes map to the destination center.</summary>
    public PointBuffer Project(Rect destination,Rect? source=null)
    {
        ImageLayout.ValidateRect(destination);var domain=source??Bounds;ImageLayout.ValidateRect(domain);
        var result=new SKPoint[Count];
        for(int i=0;i<Count;i++)
        {
            double x=destination.X+(domain.Width==0?.5:((double)points[i].X-domain.X)/domain.Width)*destination.Width;
            double y=destination.Y+(domain.Height==0?.5:((double)points[i].Y-domain.Y)/domain.Height)*destination.Height;
            if(!double.IsFinite(x)||!double.IsFinite(y)||Math.Abs(x)>float.MaxValue||Math.Abs(y)>float.MaxValue)throw new ArgumentOutOfRangeException(nameof(destination));
            result[i]=new((float)x,(float)y);
        }
        return new(result);
    }
    /// <summary>Min/max envelope reduction for nondecreasing X. Keeps first/last/extrema in source order per column.</summary>
    public PointBuffer ReduceForWidth(int columns,Rect? viewBox=null)
    {
        if(columns<=0)throw new ArgumentOutOfRangeException(nameof(columns));
        if(!IsXOrdered)throw new InvalidOperationException("Pixel-width reduction requires nondecreasing X coordinates.");
        Rect domain=viewBox??Bounds;ImageLayout.ValidateRect(domain);
        if(Count==0)return new(Array.Empty<SKPoint>());
        int lower=Lower(domain.X),upper=Upper(domain.X+domain.Width);
        int start=lower,end=upper;
        var reduced=new System.Collections.Generic.List<SKPoint>(Math.Min(Count,(int)Math.Min(int.MaxValue,(long)columns*4+2)));
        if(lower>0)reduced.Add(points[lower-1]);
        int first=-1,last=-1,min=-1,max=-1,current=-1;
        for(int i=start;i<end;i++)
        {
            int column=domain.Width==0?0:(int)Math.Clamp(Math.Floor(((double)points[i].X-domain.X)/domain.Width*columns),0,columns-1);
            if(column!=current)
            {if(first>=0)Emit(first,min,max,last,reduced);first=last=min=max=i;current=column;}
            else{last=i;if(points[i].Y<points[min].Y)min=i;if(points[i].Y>points[max].Y)max=i;}
        }
        if(first>=0)Emit(first,min,max,last,reduced);
        if(upper<Count)reduced.Add(points[upper]);
        return new(reduced.ToArray());
    }
    private void Emit(int first,int min,int max,int last,System.Collections.Generic.List<SKPoint> target)
    {
        Span<int> indices=stackalloc int[]{first,min,max,last};indices.Sort();int previous=-1;
        foreach(int index in indices)if(index!=previous){target.Add(points[index]);previous=index;}
    }
    private int Lower(float x)
    {int a=0,b=Count;while(a<b){int m=a+(b-a)/2;if(points[m].X<x)a=m+1;else b=m;}return a;}
    private int Upper(float x)
    {int a=0,b=Count;while(a<b){int m=a+(b-a)/2;if(points[m].X<=x)a=m+1;else b=m;}return a;}
}
