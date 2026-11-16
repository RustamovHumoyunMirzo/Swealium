using System.Collections;
using System.Collections.Specialized;
namespace Swealium.UI;

/// <summary>A data-driven, scrollable stack of widgets created from collection items.</summary>
public class List : ScrollContainer
{
    private readonly int thread=Environment.CurrentManagedThreadId;
    private IEnumerable? source;
    private Func<object?,int,Widget>? template;
    private object?[] items=[];
    private Widget[] rows=[];
    private INotifyCollectionChanged? observed;
    private NotifyCollectionChangedEventHandler? listener;
    private bool refreshing;
    public List(){RequireFocusForNestedScrolling=true;}
    public List(IEnumerable items,Func<object?,int,Widget> itemTemplate):this()
    {
        ArgumentNullException.ThrowIfNull(items);ArgumentNullException.ThrowIfNull(itemTemplate);
        SetSource(items,itemTemplate);
    }
    public List(IEnumerable items,Func<object?,Widget> itemTemplate):this(items,Adapt(itemTemplate)){}
    private static Func<object?,int,Widget> Adapt(Func<object?,Widget> factory)
    {ArgumentNullException.ThrowIfNull(factory);return (item,_)=>factory(item);}
    public IEnumerable? ItemsSource {get=>source;set=>SetSource(value,template);}
    public Func<object?,int,Widget>? ItemTemplate {get=>template;set=>SetSource(source,value);}
    public IReadOnlyList<object?> Items=>Array.AsReadOnly(items);
    public IReadOnlyList<Widget> ItemWidgets=>Array.AsReadOnly(rows);
    /// <summary>Rows belong to the data source. Use ItemsSource/Refresh rather than editing children.</summary>
    public new IReadOnlyList<Widget> Children=>ItemWidgets;
    public int Count=>items.Length;
    public event Action? ItemsChanged;
    public static List Create<T>(IEnumerable<T> items,Func<T,int,Widget> itemTemplate)
    {
        ArgumentNullException.ThrowIfNull(items);ArgumentNullException.ThrowIfNull(itemTemplate);
        return new(items,(item,index)=>itemTemplate((T)item!,index));
    }
    public static List Create<T>(IEnumerable<T> items,Func<T,Widget> itemTemplate)
    {
        ArgumentNullException.ThrowIfNull(itemTemplate);
        return Create(items,(item,_)=>itemTemplate(item));
    }
    public void SetItems<T>(IEnumerable<T> items,Func<T,int,Widget> itemTemplate)
    {
        ArgumentNullException.ThrowIfNull(items);ArgumentNullException.ThrowIfNull(itemTemplate);
        SetSource(items,(item,index)=>itemTemplate((T)item!,index));
    }
    public void SetItems<T>(IEnumerable<T> items,Func<T,Widget> itemTemplate)
    {
        ArgumentNullException.ThrowIfNull(itemTemplate);
        SetItems(items,(item,_)=>itemTemplate(item));
    }
    /// <summary>Override the default text row when no ItemTemplate is supplied.</summary>
    protected virtual Widget CreateItem(object? item,int index)=>new Text{Content=item?.ToString()??""};
    public void Refresh()=>SetSource(source,template);
    public void ScrollToItem(int index)
    {
        VerifyThread();
        if(index<0||index>=rows.Length)throw new ArgumentOutOfRangeException(nameof(index));
        BringIntoView(rows[index]);
    }
    private void VerifyThread()
    {if(Environment.CurrentManagedThreadId!=thread)throw new InvalidOperationException("List operations require the creating UI thread.");}
    private void SetSource(IEnumerable? next,Func<object?,int,Widget>? factory)
    {
        VerifyThread();
        if(refreshing)throw new InvalidOperationException("Do not mutate the list or its source from an item factory.");
        refreshing=true;
        try
        {
            // Complete enumeration and validation before detaching any current rows/source listener.
            object?[] snapshot=next?.Cast<object?>().ToArray()??[];
            var widgets=new Widget[snapshot.Length];var unique=new HashSet<Widget>();
            for(int i=0;i<snapshot.Length;i++)
            {
                var widget=factory is null?CreateItem(snapshot[i],i):factory(snapshot[i],i);
                if(widget is null)throw new InvalidOperationException("The item factory returned null.");
                if(!unique.Add(widget))throw new InvalidOperationException("Each item requires a distinct widget.");
                if(widget.RootOwner is not null||(widget.Parent is not null&&!ReferenceEquals(widget.Parent,this)))
                    throw new InvalidOperationException("An item widget already belongs to another tree.");
                for(Widget? ancestor=this;ancestor is not null;ancestor=ancestor.Parent)
                    if(ReferenceEquals(widget,ancestor))throw new InvalidOperationException("An item cannot contain its owning list.");
                widgets[i]=widget;
            }
            if(observed is not null&&listener is not null)observed.CollectionChanged-=listener;
            base.Children.Clear();foreach(var widget in widgets)base.Children.Add(widget);
            source=next;template=factory;items=snapshot;rows=widgets;
            observed=next as INotifyCollectionChanged;listener=null;
            if(observed is {} notifier)
            {
                var weak=new WeakReference<List>(this);
                NotifyCollectionChangedEventHandler? callback=null;
                callback=(_,_)=>{if(weak.TryGetTarget(out var list))list.Refresh();else notifier.CollectionChanged-=callback;};
                listener=callback;notifier.CollectionChanged+=callback;
            }
            InvalidateLayout();
        }
        finally {refreshing=false;}
        ItemsChanged?.Invoke();
    }
}
