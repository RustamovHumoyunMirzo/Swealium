# Cursors

[Documentation index](README.md) · [Input contracts](api/input.md) · [Bitmap/vector resources](images-and-vectors.md) · [Complete signatures](api/reference.md)

Swealium.Input.Cursor represents a system cursor, Hidden, or a custom bitmap snapshot. Public APIs contain no SDL cursor/surface pointers. System descriptors are shared and reusable; custom cursors own their bitmap copy and implement IDisposable.

```csharp
window.Cursor = Cursor.Default;       // fallback for this window
button.Cursor = Cursor.Pointer;      // button constructor already selects Pointer
editor.Cursor = Cursor.Text;         // TextBox constructor already selects Text
panel.Cursor = Cursor.Crosshair;
child.Cursor = null;                 // inherit nearest ancestor, then window default
```

## System cursor types

All current SDL system cursor types are exposed; platform appearances/fallback shapes vary. Availability requires a desktop pointer backend; headless/mobile backends may supply no visible cursor.

| Category | Static Cursor properties / CursorKind values |
| --- | --- |
| Standard | Default (Arrow alias), Pointer, Text, VerticalText, Crosshair |
| Status/help | Wait, Progress, NotAllowed, ContextMenu, Help |
| Drag/drop | Move, Alias, Copy, NoDrop, Grab, Grabbing |
| Selection/scroll | Cell, ColumnResize, RowResize, AllScroll |
| Splitter aliases | VSplit = ColumnResize (vertical divider, left/right movement); HSplit = RowResize (horizontal divider, up/down movement) |
| Axis resize | EastWestResize, NorthSouthResize |
| Diagonal resize | NorthWestSouthEastResize, NorthEastSouthWestResize |
| Directional resize | NorthWestResize, NorthResize, NorthEastResize, EastResize, SouthEastResize, SouthResize, SouthWestResize, WestResize |
| Zoom | ZoomIn, ZoomOut |
| Hidden | Hidden hides the pointer while active in the application window |

SystemCursors is a read-only snapshot containing the 34 system descriptors (not Hidden/Custom). FromKind(CursorKind) returns shared descriptors, including Hidden; Custom/invalid values throw ArgumentOutOfRangeException because custom creation needs pixels/hotspot. Arrow aliases Default. Dispose on shared system/Hidden cursors is a no-op.

## Resolution, capture, and window lifetime

Widget.Cursor defaults null, meaning inheritance. Buttons default to Pointer and TextBox to Text; setting their Cursor to null restores inheritance. InputManager.DefaultCursor defaults Default. InputManager.CurrentCursor resolves the captured widget first, otherwise the deepest hovered hit, then nearest ancestor cursor, then the default. Hidden/disabled/clipped hit regions follow ordinary hit testing. Capture keeps its cursor outside the pressed widget until release/cancellation. Stationary pointer appearance refreshes during normal frames when properties/layout change.

SwealiumWindow.Cursor wraps its input manager's default and requires the creating UI thread. EffectiveCursor returns the resolved descriptor. Non-hovered windows do not overwrite the mouse-focused window's native cursor: updates are gated by native mouse focus. Window leave/disposal resets an owned active appearance. Missing backend cursor resources fall back to the native default when possible rather than terminating the application.

## Bitmap cursors

```csharp
using var pixels = new Bitmap(32, 32);
pixels.Draw(drawing => drawing.FillEllipse(new Rect(2, 2, 26, 26), new Color(220, 70, 90)));
using var cursor = Cursor.FromBitmap(pixels, hotX: 15, hotY: 15);
window.Cursor = cursor;
```

FromBitmap copies pixels immediately. Editing/disposing the source does not change the cursor. Hotspot coordinates are integer pixel indices strictly inside the bitmap. Alpha converts to straight RGBA for native surface creation. OS size/scale limits may affect acceptance or appearance. The API uses one bitmap representation; animated cursors and multiple DPI-specific representations are not exposed.

Kind, HotSpot, Size, and IsDisposed describe the cursor. System cursor Size/HotSpot are zero because platform pixels are not exposed. CopyBitmap() returns an independent owned copy for a custom cursor; system/Hidden cursors throw InvalidOperationException. Disposed custom cursors throw ObjectDisposedException from CopyBitmap; their Size becomes zero.

Native handles are allocated lazily, cached during the host session, and released before SDL shutdown by Run. Dispose custom cursors on the host UI thread after native materialization; unused custom cursors need no native host. Disposal resets an active cursor before freeing it. Retained disposed widget descriptors are skipped during inheritance; disposed defaults fall back to Default. Assigning a disposed cursor is rejected.

External native hosts must call Cursor.ReleasePlatformResources() on the UI thread before shutting down SDL video; Run handles this automatically. This releases native handles without disposing managed descriptions, which can be reused in a later host session. Dispose custom descriptors separately when finished.

Vectors can become cursors too:

```csharp
using var vector = VectorImage.Load("cursor.svg");
using var bitmap = vector.Rasterize(32, 32);
using var cursor = Cursor.FromBitmap(bitmap, 4, 4);
```

Routing, coverage, ownership, headless fallback, and repeated-session cleanup are tested. Native appearance/scaling still requires real desktop checks on each target platform.
