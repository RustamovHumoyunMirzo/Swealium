using Swealium.Graphics;
using Swealium.Native;
namespace Swealium.Input;

public enum CursorKind
{
    Default,Text,Wait,Crosshair,Progress,NorthWestSouthEastResize,NorthEastSouthWestResize,EastWestResize,NorthSouthResize,
    Move,NotAllowed,Pointer,NorthWestResize,NorthResize,NorthEastResize,EastResize,SouthEastResize,SouthResize,SouthWestResize,WestResize,
    ContextMenu,Help,Cell,VerticalText,Alias,Copy,NoDrop,Grab,Grabbing,ColumnResize,RowResize,AllScroll,ZoomIn,ZoomOut,Custom,Hidden
}

/// <summary>Reusable system cursor or owned bitmap cursor. No native allocation occurs until a window uses it.</summary>
public sealed class Cursor:IDisposable
{
    private static readonly Cursor[] stock=Enumerable.Range(0,(int)CursorKind.Custom).Select(i=>new Cursor((CursorKind)i)).ToArray();
    private Bitmap? bitmap;
    private bool disposed;
    private Cursor(CursorKind kind){Kind=kind;}
    private Cursor(Bitmap source,int hotX,int hotY)
    {
        if(hotX<0||hotY<0||hotX>=source.Width||hotY>=source.Height)throw new ArgumentOutOfRangeException(nameof(hotX));
        Kind=CursorKind.Custom;bitmap=source.Clone();HotSpot=new(hotX,hotY);
    }
    public CursorKind Kind {get;}
    public Point HotSpot {get;}
    public bool IsDisposed=>disposed;
    public Size Size=>bitmap?.Size??default;
    internal Bitmap? Bitmap=>bitmap;
    internal void Verify()=>ObjectDisposedException.ThrowIf(disposed,this);
    public static Cursor FromBitmap(Bitmap bitmap,int hotX=0,int hotY=0)
    {ArgumentNullException.ThrowIfNull(bitmap);return new(bitmap,hotX,hotY);}
    public Bitmap CopyBitmap(){Verify();return bitmap?.Clone()??throw new InvalidOperationException("System cursors do not expose platform bitmap data.");}
    public static Cursor FromKind(CursorKind kind)
    {
        if(kind==CursorKind.Hidden)return Hidden;
        if((int)kind<0||(int)kind>=stock.Length)throw new ArgumentOutOfRangeException(nameof(kind));return stock[(int)kind];
    }
    public static IReadOnlyList<Cursor> SystemCursors {get;}=Array.AsReadOnly(stock);
    public static Cursor Default=>stock[0];
    public static Cursor Arrow=>Default;
    public static Cursor Text=>stock[1];
    public static Cursor Wait=>stock[2];
    public static Cursor Crosshair=>stock[3];
    public static Cursor Progress=>stock[4];
    public static Cursor NorthWestSouthEastResize=>stock[5];
    public static Cursor NorthEastSouthWestResize=>stock[6];
    public static Cursor EastWestResize=>stock[7];
    public static Cursor NorthSouthResize=>stock[8];
    public static Cursor Move=>stock[9];
    public static Cursor NotAllowed=>stock[10];
    public static Cursor Pointer=>stock[11];
    public static Cursor NorthWestResize=>stock[12];
    public static Cursor NorthResize=>stock[13];
    public static Cursor NorthEastResize=>stock[14];
    public static Cursor EastResize=>stock[15];
    public static Cursor SouthEastResize=>stock[16];
    public static Cursor SouthResize=>stock[17];
    public static Cursor SouthWestResize=>stock[18];
    public static Cursor WestResize=>stock[19];
    public static Cursor ContextMenu=>stock[20];
    public static Cursor Help=>stock[21];
    public static Cursor Cell=>stock[22];
    public static Cursor VerticalText=>stock[23];
    public static Cursor Alias=>stock[24];
    public static Cursor Copy=>stock[25];
    public static Cursor NoDrop=>stock[26];
    public static Cursor Grab=>stock[27];
    public static Cursor Grabbing=>stock[28];
    public static Cursor ColumnResize=>stock[29];
    public static Cursor RowResize=>stock[30];
    /// <summary>Vertical splitter bar, resized left/right. Uses the platform column-resize cursor.</summary>
    public static Cursor VSplit=>ColumnResize;
    /// <summary>Horizontal splitter bar, resized up/down. Uses the platform row-resize cursor.</summary>
    public static Cursor HSplit=>RowResize;
    public static Cursor AllScroll=>stock[31];
    public static Cursor ZoomIn=>stock[32];
    public static Cursor ZoomOut=>stock[33];
    public static Cursor Hidden {get;}=new(CursorKind.Hidden);
    /// <summary>External native hosts call this on their UI thread before shutting down video. Run handles it automatically.</summary>
    public static void ReleasePlatformResources()=>SdlCursorManager.Shutdown();
    public void Dispose()
    {
        if(Kind!=CursorKind.Custom||disposed)return;
        SdlCursorManager.Release(this);bitmap?.Dispose();bitmap=null;disposed=true;
    }
}
