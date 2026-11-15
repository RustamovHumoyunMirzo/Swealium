# Accessibility API contracts

[Guide and provider boundary](../accessibility.md) · [Complete signatures](reference.md)

Namespace Swealium.Accessibility. All host operations require the owning window UI thread. No native accessibility backend types are exposed, and no platform provider is enabled by default.

## Metadata and records

Widget.Accessibility is a read-only reference to a mutable AccessibilityProperties object: nullable Name/Description/AutomationId/HelpText, Role=Automatic, LiveRegion=Off, nullable Actions override, IsHidden=false, ExposeValue=true. Role/live enum values are checked when capturing. Automatic semantic roles infer built-ins; unknown custom widgets use Custom. Hidden suppresses the semantic subtree. Value redaction suppresses committed TextBox value and selection snapshot fields, not editing capability or visual display.

AccessibilityRole: Automatic, Window, Group, Button, Text, TextBox, Image, ScrollArea, Custom. AccessibilityLiveRegion: Off, Polite, Assertive. AccessibilityState flags: Enabled, Focusable, Focused, ReadOnly, Offscreen. AccessibilityActions flags: Invoke, Focus, SetValue, SetSelection, ScrollIntoView, ScrollBy; None=0.

AccessibilityNode is an immutable record of Id, nullable ParentId, Role, Name/Description/AutomationId/HelpText, Bounds, VisibleBounds, State, Actions, nullable Value, SelectionStart/Length, and LiveRegion. Bounds use logical window coordinates; VisibleBounds is an axis-aligned ancestor/scroll clip intersection. IDs remain stable for a widget object's lifetime; root ID 0 belongs to the snapshot's WindowId. Selection indices are UTF-16 and snapshot fields are zero when redacted.

AccessibilitySnapshot contains WindowId, increasing Revision, and read-only Nodes. Capture order is semantic insertion order; native hit testing uses paint order separately. AccessibilityChange contains NodeId and Kind: Added, Removed, PropertiesChanged, FocusChanged, LiveRegionChanged, StructureChanged. AccessibilityUpdate contains Snapshot and read-only Changes. These snapshots contain values rather than mutable Widget references.

## AccessibilityHost

Window-owned sealed class, constructed internally and exposed by SwealiumWindow.Accessibility. Window gives the adapter access to ordinary public window APIs/native descriptors. IsBridgeAttached reports adapter presence. Changed is Action&lt;AccessibilityUpdate&gt;.

| Method | Contract |
| --- | --- |
| CaptureSnapshot() | Ensure known-root invalidated layout, build detached semantic tree, increment revision |
| HitTest(Point) | Current semantic node under a finite logical point; respects rounded/scroll clipping and Z order; can identify disabled nodes |
| TryPerform(AccessibilityActionRequest) | Validate live node/action eligibility, run custom hook or built-in action, refresh on success; bool result |
| AttachBridge(IAccessibilityBridge) | Reject null/second adapter, take ownership, attach and publish initial tree; clean up failed attachment |
| DetachBridge() | Detach/dispose owned adapter; no-op if absent |
| Refresh() | Diff against last published capture, notify only when semantic data changes; reentrant refresh during publishing is deferred until a later refresh |

After window closure, host operations throw ObjectDisposedException; wrong-thread operations throw InvalidOperationException. Platform callbacks must marshal themselves to the UI thread. Capture does not depend on a native adapter. Automatic refresh occurs only if a bridge/Changed subscriber observes the host, so unused accessibility does not build a tree each frame.

AccessibilityActionRequest is an immutable record: NodeId, Action, nullable Value=null, SelectionStart/Length=0, ScrollDelta=default. AccessibilityAction enum: Invoke, Focus, SetValue, SetSelection, ScrollIntoView, ScrollBy. Undefined actions, unknown/stale IDs, excluded/disabled controls, invalid selection/delta, unsupported actions, and blocked/hidden/minimized/closing windows return false. SetValue requires a nonnull string and writable TextBox; it follows ordinary Text normalization/history behavior. SetSelection validates UTF-16 and uses TextBox.Select's grapheme snapping. ScrollBy requires finite delta. Focus uses InputManager; Invoke uses PerformClick. Custom Widget.OnAccessibilityAction may handle advertised actions first.

## IAccessibilityBridge

Public IDisposable interface with Attach(AccessibilityHost), Publish(AccessibilityUpdate), Detach(). Calls occur on the window UI thread. Host owns the successfully attached adapter and also disposes a failed attempt. Detach is followed by Dispose even if Detach throws. Window cleanup releases the bridge before native destruction. Implementations must release native registration/provider resources and avoid forcing layout recursively from Publish. No Windows UIA/macOS AX/Linux AT-SPI/mobile implementation is included; this is the provider boundary for those adapters.
