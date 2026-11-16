using System.Collections.ObjectModel;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.UI;
using UiList=Swealium.UI.List;
namespace Swealium.Tests;
internal static class ListTests
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidOperationException("Expected "+typeof(T).Name);}
    private sealed record Person(string Name,int Age);
    internal static void Sources()
    {
        var people=new System.Collections.Generic.List<Person>{new("Ada",36),new("Lin",29)};
        string prefix="Person";var list=UiList.Create(people,(person,index)=>new Button{Text=$"{prefix} {index}: {person.Name}, {person.Age}",Height=32});
        Check(list.Count==2&&((Button)list.ItemWidgets[1]).Text=="Person 1: Lin, 29","Typed models, index and captured constructor arguments populate repeated widgets");
        Check(list.Children.Count==2&&list.ItemWidgets.All(row=>row.Parent==list),"The list owns each generated row");
        people.Add(new("Sam",40));Check(list.Count==2,"Ordinary C# lists use an explicit refresh snapshot");list.Refresh();Check(list.Count==3,"Refresh reads ordinary collection updates");
        var old=list.ItemWidgets.ToArray();var oldSource=list.ItemsSource;var oldTemplate=list.ItemTemplate;
        Throws<InvalidOperationException>(()=>list.ItemTemplate=(_,_)=>throw new InvalidOperationException("template failed"));
        Check(list.ItemsSource==oldSource&&list.ItemTemplate==oldTemplate&&list.ItemWidgets.SequenceEqual(old),"Template failures preserve the old data and visual tree");
        var duplicate=new Text{Content="same"};Throws<InvalidOperationException>(()=>list.SetItems(new[]{1,2},_=>duplicate));
        Check(list.ItemWidgets.SequenceEqual(old),"Duplicate row widgets cannot partially replace the old list");
        var owner=new Container();var attached=new Text();owner.Children.Add(attached);Throws<InvalidOperationException>(()=>list.SetItems(new[]{1},_=>attached));
        Throws<InvalidOperationException>(()=>list.SetItems(new[]{1},_=>list));
        list.SetItems(new[]{"one","two"},word=>new Text{Content=word});Check(list.Count==2&&((Text)list.ItemWidgets[0]).Content=="one","Typed rebinding changes source and template together");
        list.ItemTemplate=null;list.ItemsSource=new object?[]{12,null,"hello"};Check(((Text)list.ItemWidgets[0]).Content=="12"&&((Text)list.ItemWidgets[1]).Content=="","Default rows render source values including null");
        list.ItemsSource=null;Check(list.Count==0&&list.Children.Count==0,"Null source clears rows");
        int enumerations=0;System.Collections.Generic.IEnumerable<int> Values(){enumerations++;yield return 4;yield return 8;}
        list.SetItems(Values(),n=>new Text{Content=n.ToString()});Check(enumerations==1,"Each rebuild enumerates a source once");
        var reused=list.ItemWidgets.ToArray();list.SetItems(new[]{0,1},index=>reused[index]);Check(list.ItemWidgets.SequenceEqual(reused),"A factory may reuse its current owned rows");
    }
    internal static void ObservableSources()
    {
        var source=new ObservableCollection<string>{"a","b"};var list=UiList.Create(source,(item,index)=>new Text{Content=$"{index}:{item}"});int changes=0;list.ItemsChanged+=()=>changes++;
        source.Add("c");Check(list.Count==3&&changes==1,"Observable additions refresh automatically");
        source.Move(2,0);Check(((Text)list.ItemWidgets[0]).Content=="0:c","Move regenerates arguments with current indices");
        source[1]="replace";Check(((Text)list.ItemWidgets[1]).Content=="1:replace","Observable replacement refreshes the row");
        source.RemoveAt(0);Check(list.Count==2,"Removal refreshes rows");
        list.ItemsSource=new[]{"new"};source.Clear();Check(list.Count==1&&((Text)list.ItemWidgets[0]).Content=="0:new","Rebinding detaches the previous collection listener");
        var weak=Unreferenced(source);for(int i=0;i<5&&weak.IsAlive;i++){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}
        Check(!weak.IsAlive,"An observable collection does not keep removed/unreferenced list widgets alive");source.Add("safe");
        Exception? failure=null;var worker=new Thread(()=>{try{list.Refresh();}catch(Exception error){failure=error;}});worker.Start();worker.Join();Check(failure is InvalidOperationException,"Rebuilding on a worker thread is rejected");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference Unreferenced(ObservableCollection<string> source)=>new(UiList.Create(source,s=>new Text{Content=s}));
    internal static void LayoutAndCustomization()
    {
        var list=UiList.Create(Enumerable.Range(0,20),index=>new Button{Text=index.ToString(),Height=30});list.Width=220;list.Height=110;list.Gap=4;list.Padding=new(6);
        list.VerticalScrollBarStyle=new(){Thickness=15,ThumbColor=new(230,70,80)};
        LayoutEngine.Layout(list,new(220,110));Check(list.MaximumOffset.Y>500&&list.VerticalScrollBar.IsVisible,"The list has its own finite viewport, extent and scrollbar");
        Check(list.Children[1].Bounds.Y-list.Children[0].Bounds.Y==34,"Container gap is applied between repeated rows");
        list.ScrollToItem(19);Check(list.VerticalOffset>500,"ScrollToItem reveals the requested generated widget");
        list.VerticalScrollBarVisibility=ScrollBarVisibility.Hidden;LayoutEngine.Layout(list,new(220,110));Check(!list.VerticalScrollBar.IsVisible&&list.MaximumOffset.Y>0,"Developers may disable bars while retaining scrolling/custom external controls");
        list.ItemsSource=Enumerable.Range(0,1);LayoutEngine.Layout(list,new(220,110));Check(list.VerticalOffset==0,"Shrinking the source clamps the offset on layout");
        Throws<ArgumentOutOfRangeException>(()=>list.ScrollToItem(2));
        var row=UiList.Create(new[]{1,2,3,4},n=>new Button{Text=n.ToString(),Width=80,Height=25});row.Layout=LayoutMode.Horizontal;row.Direction=ScrollDirection.Horizontal;row.Width=120;row.Height=60;
        LayoutEngine.Layout(row,new(120,60));Check(row.MaximumOffset.X>150&&row.MaximumOffset.Y==0,"Horizontal lists reuse directional scrolling");
    }
    internal static void NestedFocus()
    {
        var list=UiList.Create(Enumerable.Range(0,20),index=>new Button{Text=index.ToString(),Height=30});list.Height=100;
        var outer=new ScrollContainer{Width=260,Height=180,Gap=10};outer.Children.Add(list);outer.Children.Add(new Button{Text="Outer focus",Height=360});
        LayoutEngine.Layout(outer,new(260,180));var input=new InputManager{Root=outer};Point overList=new(20,20),overOuter=new(20,140);
        void Wheel(Point point)=>input.ProcessPointer(new(PointerEventKind.Wheel,point,wheelDelta:new(0,-1)));
        input.Focus(outer);Wheel(overList);Check(outer.VerticalOffset>0&&list.VerticalOffset==0,"With outer focus, a wheel over the nested overflowing list scrolls the outer container");
        outer.ScrollTo(0,0);input.Focus(list);Wheel(overList);Check(list.VerticalOffset>0&&outer.VerticalOffset==0,"Focusing the list transfers wheel ownership");
        Wheel(overOuter);Check(list.VerticalOffset>=80&&outer.VerticalOffset==0,"Wheel inside the outer container still follows focused list ownership");
        list.ScrollTo(0,list.MaximumOffset.Y);Wheel(overList);Check(outer.VerticalOffset==0,"Reaching the list boundary does not leak scrolling into its parent");
        input.Focus(outer);Wheel(overList);Check(outer.VerticalOffset>0,"Focusing the outer container explicitly transfers scrolling back");
        outer.ScrollTo(0,0);list.ScrollTo(0,0);input.Focus(list.ItemWidgets[0]);Wheel(overList);Check(list.VerticalOffset>0&&outer.VerticalOffset==0,"Focus on an interactive row belongs to its list");
        list.RequireFocusForNestedScrolling=false;list.ScrollTo(0,list.MaximumOffset.Y);Wheel(overList);Check(outer.VerticalOffset>0,"Opting out restores ordinary boundary bubbling");
    }
}
