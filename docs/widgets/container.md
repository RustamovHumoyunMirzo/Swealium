# Container

[Widget hub](README.md) · [Inherited Widget API](widget.md) · [Complete contracts](../api/widgets.md#container)

`public class Container : Widget` owns traversable children through a read-only `WidgetCollection Children` reference. Add/remove/reorder collection items on the UI thread. The renderer and InputManager traverse Container subclasses automatically.

| Member | Default / behavior |
| --- | --- |
| Layout | Vertical; also Horizontal, Overlay, Absolute |
| Gap | 0; finite nonnegative separation between visible stack margin boxes |
| HorizontalContentAlignment | Stretch |
| VerticalContentAlignment | Start |
| Background | Color.Transparent; fills rounded actual bounds |
| Children | Empty owned collection created by constructor |

Vertical stacks flow down; Horizontal stacks flow right. Main-axis intrinsic measurement is unbounded, with finite weight allocation followed by dependent remeasurement. Cross-axis alignment places children within the available content area. Overlay allocates the content slot to every child. Absolute uses child X/Y offsets. Hidden children take neither space nor gaps. Disabled children retain layout space. Layout order is insertion order; ZIndex changes paint and hit order independently.

```csharp
var row = new Container { Layout = LayoutMode.Horizontal, Gap = 12, Padding = new(16) };
row.Children.Add(new Button { Text = "One", Weight = 1, WeightBasis = WeightBasis.Zero });
row.Children.Add(new Button { Text = "Two", Weight = 2, WeightBasis = WeightBasis.Zero });
```

Gap and widget Weight/Grow apply to stacks; overlay/absolute ignore them. Content alignment shifts an entire stack on its primary axis and controls individual cross-axis placement. Weights distribute spare finite space subject to min/max constraints; they do not shrink overflowing bases. See [layout](../layout.md).

`MeasureContent`/`ArrangeContent` are protected overrides and may be overridden for custom composites. `Draw` fills Background; inherited DrawOverlay runs above descendants and Stroke runs last. Container adds no input behavior beyond Widget.

## Child ownership

WidgetCollection inherits Collection&lt;Widget&gt; operations: Count, indexer, Add/Insert/Remove/RemoveAt/Clear/Contains/IndexOf/CopyTo/enumeration. Null, cycles, already-parented widgets, and another input tree's root are rejected. Remove/detach before reparenting. Replacing an item with itself is a no-op. Removal invalidates layout and clears Parent, but does not dispose a widget. Standard collection index/array exceptions apply.
