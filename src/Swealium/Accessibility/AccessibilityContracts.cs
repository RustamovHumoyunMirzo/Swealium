using Swealium.Graphics;
namespace Swealium.Accessibility;

public enum AccessibilityRole { Automatic,Window,Group,Button,Text,TextBox,Image,ScrollArea,Custom }
public enum AccessibilityLiveRegion { Off,Polite,Assertive }
[Flags]
public enum AccessibilityState { None=0,Enabled=1,Focusable=2,Focused=4,ReadOnly=8,Offscreen=16 }
[Flags]
public enum AccessibilityActions { None=0,Invoke=1,Focus=2,SetValue=4,SetSelection=8,ScrollIntoView=16,ScrollBy=32 }
public enum AccessibilityAction { Invoke,Focus,SetValue,SetSelection,ScrollIntoView,ScrollBy }
public enum AccessibilityChangeKind { Added,Removed,PropertiesChanged,FocusChanged,LiveRegionChanged,StructureChanged }

/// <summary>UI-thread semantic overrides. Hidden excludes the entire semantic subtree, not its visual rendering.</summary>
public sealed class AccessibilityProperties
{
    public string? Name {get;set;}
    public string? Description {get;set;}
    public string? AutomationId {get;set;}
    public string? HelpText {get;set;}
    public AccessibilityRole Role {get;set;}
    public AccessibilityLiveRegion LiveRegion {get;set;}
    public AccessibilityActions? Actions {get;set;}
    public bool IsHidden {get;set;}
    public bool ExposeValue {get;set;}=true;
}
public sealed record AccessibilityActionRequest(long NodeId,AccessibilityAction Action,string? Value=null,int SelectionStart=0,int SelectionLength=0,Point ScrollDelta=default);
public sealed record AccessibilityNode(long Id,long? ParentId,AccessibilityRole Role,string Name,string Description,string AutomationId,string HelpText,
    Rect Bounds,Rect VisibleBounds,AccessibilityState State,AccessibilityActions Actions,string? Value,int SelectionStart,int SelectionLength,AccessibilityLiveRegion LiveRegion);
public sealed record AccessibilitySnapshot(uint WindowId,long Revision,IReadOnlyList<AccessibilityNode> Nodes);
public sealed record AccessibilityChange(long NodeId,AccessibilityChangeKind Kind);
public sealed record AccessibilityUpdate(AccessibilitySnapshot Snapshot,IReadOnlyList<AccessibilityChange> Changes);

/// <summary>Platform adapter boundary. Calls occur on the window's UI thread. No native providers ship with this interface.</summary>
public interface IAccessibilityBridge:IDisposable
{
    void Attach(AccessibilityHost host);
    void Publish(AccessibilityUpdate update);
    void Detach();
}
