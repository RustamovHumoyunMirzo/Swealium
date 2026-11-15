# WrapContainer

[Widget hub](README.md) · [Inherited Container API](container.md) · [Wrapping and weight guide](../layout.md#wrapping-at-a-boundary)

`public class WrapContainer : Container` flows children until the next item and gap exceed a finite primary boundary, then starts another row/column. Its inherited Layout setting is ignored; use Direction.

| Member | Default / contract |
| --- | --- |
| Direction | FlowDirection.Horizontal; Vertical wraps into columns |
| LineGap | null inherits Gap; finite nonnegative when supplied |
| LineCount | Last arranged line count, initially zero |
| Lines | Read-only snapshot of local rectangles from the last arrangement |
| MeasureContent / ArrangeContent | Protected overrides grouping, weighting, measuring, and arranging lines |

```csharp
var tags = new WrapContainer { Gap = 8, LineGap = 12, Padding = new(10) };
foreach (var caption in new[] { "First", "A longer caption", "Third", "Fourth" })
    tags.Children.Add(new Button { Text = caption });
```

Padding reduces the boundary; margins contribute to item length. An exactly fitting item stays on its line within floating-point tolerance. An oversized item gets a line of its own and can overflow. Infinite primary space produces one line. Hidden children contribute neither lines nor gaps. Insertion order controls flow; ZIndex affects drawing/hit testing.

Weights are allocated independently within each line after grouping. Content basis uses intrinsic sizes; Zero uses minimum+margin and then shares remaining space. Set useful minimums when zero-basis items should establish wrap boundaries. Weighted children remeasure at allocated lengths before computing cross sizes. Max constraints cap growth and redistribute spare space. Alignment positions each line's items and the group of lines, with inherited cross-axis alignment within a line.

Invalid Direction or negative/nonfinite LineGap throws ArgumentOutOfRangeException. Background, ownership, Gap, strokes, radii, sizing, and input are inherited. For vertical flow, supply a finite height to create columns. For wrapping text itself, use [Text](text.md); WrapContainer wraps widgets.
