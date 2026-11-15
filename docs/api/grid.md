# Grid API contracts

[Documentation index](../README.md) · [Grid guide](../grid.md) · [Complete signatures](reference.md)

Namespace: Swealium.UI. All mutation/layout follows the UI-thread convention.

## Grid

Subclassable Container. Constructor creates Rows/Columns and sets VerticalContentAlignment=Stretch; horizontal already defaults to Stretch. Grid overrides MeasureContent and ArrangeContent. Inherited Layout, X/Y placement, and child Grow do not change its cell algorithm.

| Member | Default / contract |
| --- | --- |
| Rows, Columns | GridTrackCollection, read-only references to mutable explicit definitions |
| RowGap, ColumnGap | float? get/set; null inherits Gap; finite nonnegative overrides |
| RowSizes, ColumnSizes | IReadOnlyList<float>; copies of last-arranged sizes, including implicit tracks |
| MeasureContent(Size available) | Protected override; fixed/content/fraction and spanning demand measurement |
| ArrangeContent(Rect contentBounds) | Protected override; remeasure at final content size, record actual track sizes, allocate cell/span slots |

Children use Widget.GridRow/GridColumn/GridRowSpan/GridColumnSpan. Default placement is (0,0), span (1,1). Positive spans can create implicit content tracks after explicit definitions. If an axis is empty, its first implicit track is Fraction(1). Hidden children do not extend axes.

Finite fractional allocation solves the remaining budget using weights plus min/max limits. Fractional tracks act as intrinsic tracks for unbounded measurement. Intrinsic spans prioritize fractional then content tracks; fixed tracks never expand. With no available budget, track minimums can still cause overflow.

Measure/arrange can call child Measure more than once. Stable custom measurement is required. Nonconvergence after 32 solver passes, coordinate extents beyond finite float representation, and overflowing cell index ranges report exceptions. Allocating unreasonably large explicit/implicit grids is the caller's responsibility.

Padding, margins, radii, visibility, clipping, drawing, z-order, focus, and input are inherited. Explicit dimensions and minimum sizes can overflow cell slots; no additional per-cell clip is installed. Cells can overlap. Stack Grow, sequential auto-placement, shared-size groups, scrolling, and virtualization are not part of Grid.

## GridLength and GridUnit

GridLength is a readonly record struct with read-only Unit and Value.

| API | Contract |
| --- | --- |
| GridLength(GridUnit unit, float value = 1) | Defined unit and finite nonnegative value; Fraction requires positive value; Content stores Value=0 |
| Fixed(float length) | Fixed logical length; zero is valid |
| Content | Static intrinsic content length |
| Fraction(float weight = 1) | Static weighted fractional length |
| default(GridLength) | Fixed zero, not Fraction(1) |

GridUnit values: Fixed, Content, Fraction. Invalid units, negative/nonfinite values, or zero fractional weight throw ArgumentOutOfRangeException.

Record-generated equality/operators/formatting are included in the signature inventory. There is no implicit scalar/string conversion.

## GridTrack

Sealed mutable definition.

| Member | Default / validation |
| --- | --- |
| GridTrack() | Length=Fraction(1), MinSize=0, MaxSize=positive infinity |
| GridTrack(GridLength length) | Uses supplied immutable length |
| Length | GridLength get/set; change invalidates owning grid |
| MinSize | float get/set; finite nonnegative |
| MaxSize | float get/set; nonnegative, infinity allowed |
| ActualSize | float get; set by arrangement, zero initially or after detachment |

Minimum takes precedence over a smaller maximum. Fixed length is clamped to those limits. ActualSize remains the last arranged value while invalidation is pending. No dimension/gap is included beyond the track itself.

## GridTrackCollection

Sealed Collection<GridTrack> subclass. Obtain through Grid.Rows/Columns; constructor is internal.

Inherited operations include Count, index get/set, Add, Insert, Remove, RemoveAt, Clear, Contains, IndexOf, CopyTo, and enumeration. Null tracks and tracks already belonging to any collection are rejected. Insertion/replacement/removal/clear and attached definition changes invalidate the owning Grid and ancestors.

A track cannot be shared between row and column collections even in the same grid. Remove first; detachment clears ownership and ActualSize. Replacing an item with itself is a no-op. Standard Collection index/array argument behavior applies.

