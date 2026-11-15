# Grid

[Widget hub](README.md) · [Inherited Container API](container.md) · [Grid guide](../grid.md) · [Full track contracts](../api/grid.md)

`public class Grid : Container` places children into explicit rows/columns with fixed, content, and fractional tracks. It sets VerticalContentAlignment=Stretch; horizontal is inherited Stretch. Layout, child X/Y, and child Weight/Grow do not control its cell algorithm.

| Public member | Contract |
| --- | --- |
| Rows / Columns | Read-only references to mutable GridTrackCollection definitions |
| RowGap / ColumnGap | null inherits Gap; otherwise finite nonnegative logical length |
| RowSizes / ColumnSizes | Snapshots of the last arranged sizes, including implicit tracks |
| MeasureContent / ArrangeContent | Protected overrides implementing constraint/span solving and dependent remeasurement |

```csharp
var grid = new Grid { Gap = 10, Padding = new(16) };
grid.Columns.Add(new GridTrack(GridLength.Fixed(120)));
grid.Columns.Add(new GridTrack(GridLength.Fraction()));
grid.Rows.Add(new GridTrack(GridLength.Content));
grid.Rows.Add(new GridTrack(GridLength.Fraction()));
grid.Children.Add(new Text { Content = "Settings", GridColumnSpan = 2 });
grid.Children.Add(new Text { Content = "Name", GridRow = 1 });
grid.Children.Add(new TextBox { GridRow = 1, GridColumn = 1, WidthSizing = SizingMode.Fill });
```

GridRow/GridColumn default to 0 and spans to 1. Undefined cells create implicit content tracks; an empty axis begins with one fractional track. Spans include internal gaps. There is no automatic sequential placement: assign cells explicitly to avoid overlapping children.

GridTrack has Length (default Fraction(1)), MinSize=0, MaxSize=PositiveInfinity, and read-only ActualSize. GridLength.Fixed uses logical units, Content measures intrinsic demand, and Fraction(weight=1) divides finite remaining budget. Track lengths/limits invalidate the owning grid; tracks cannot be shared between collections. Invalid lengths, fractions, cells, spans, and ownership throw as described in [track contracts](../api/grid.md).

Intrinsic spanning demand can enlarge fractional/content tracks; fixed tracks remain fixed. Fractional tracks become intrinsic when unbounded. Conflicting min/max constraints favor minimums. Grid can remeasure cells multiple times for wrapping-dependent heights. Nonconvergence, coordinate overflow, and overflowing cell ranges throw. Cell slots have no separate clipping; use widget clipping or a ScrollContainer for overflow. Padding, margins, drawing, stroke, focus, and input are inherited.
