# Widget and layout API contracts

[Documentation index](../README.md) · [Complete signatures](reference.md) · [Layout guide](../layout.md)

Namespace: Swealium.UI. All tree/property/input operations should occur on the UI thread. Most widget setters do not perform thread checks.

## Widget

Abstract retained visual. The protected default constructor creates no native resources. Derived types implement Draw(DrawingContext).

### Requested layout inputs

| Property | Default | Validation/meaning |
| --- | --- | --- |
| X, Y | 0 | Finite offsets used in Absolute containers only |
| Width, Height | NaN | Auto/content size; otherwise finite nonnegative request |
| WidthSizing, HeightSizing | SizingMode.Automatic | Content keeps intrinsic size; Fill occupies allocated slot; either clears numeric axis request |
| MinWidth, MinHeight | 0 | Finite nonnegative lower bound |
| MaxWidth, MaxHeight | positive infinity | Nonnegative bound; infinity allowed; min wins if contradictory |
| Margin | zero Thickness | Outer spacing, included in DesiredSize |
| Padding | zero Thickness | Inner spacing, added around MeasureContent |
| HorizontalAlignment, VerticalAlignment | null | Inherit parent container defaults; explicit Alignment overrides |
| Grow | 0 | Finite nonnegative spare-space weight on stack main axis |
| ZIndex | 0 | Signed sibling rendering/hit priority; does not change flow/traversal |
| Radii | zero CornerRadii | Rounded drawing clip/default hit region |
| ClipToBounds | true | Clip widget subtree drawing/input to its rounded boundary |
| GridRow, GridColumn | 0 | Nonnegative zero-based cell in a parent Grid |
| GridRowSpan, GridColumnSpan | 1 | Positive cell span in a parent Grid |

Layout-affecting setters invalidate ancestor layout. ZIndex/Radii/ClipToBounds affect traversal/geometry immediately rather than desired size.

Setting a numeric Width/Height (including NaN) returns that axis to SizingMode.Automatic. Setting Content/Fill clears the axis request to NaN. Named sizing changes and grid placement/span changes invalidate ancestor layout. Placement is ignored by non-Grid containers. Indices/spans reject negative/zero values as appropriate and int.MaxValue.

### Eligibility and observed state

| Property | Default/access | Meaning |
| --- | --- | --- |
| IsVisible | true, settable | false collapses layout and suppresses drawing/input |
| IsEnabled | true, settable | false suppresses subtree input/programmatic clicks but preserves rendering/space |
| IsHitTestVisible | true, settable | false excludes pointer testing for the entire subtree |
| IsFocusable | false, settable | Eligible for focus when attached and effectively enabled/visible |
| AcceptsTextInput | false, settable | Opt into native committed-text service while focused |
| IsFocused | false, read-only | InputManager-owned widget focus |
| IsPointerOver | false, read-only | Current hover-path membership |
| Parent | null, read-only | Collection-owned parent; not user-reparentable via assignment |

Bounds is the arranged parent-relative rectangle. Before first arrangement it falls back to requested X/Y and finite explicit sizes, with auto dimensions as zero. ActualWidth/ActualHeight come from Bounds. DesiredSize includes margin; ContentBounds is local, padding-inset, and nonnegative in extent. IsLayoutValid requires at least one arrangement and no pending invalidation.

### Layout and hit testing methods

| Method | Result/contract |
| --- | --- |
| InvalidateLayout() | Mark this visual and parent chain dirty |
| Measure(Size available) | Update DesiredSize; accepts nonnegative finite/infinite constraints, rejects NaN/negative |
| Arrange(Rect slot) | Assign bounds in a finite nonnegative outer slot; call Measure first for auto sizing |
| ContainsPoint(Point local) | Virtual analytic rounded-rectangle test using actual size/Radii |
| Draw(DrawingContext context) | Abstract; draw local coordinates with actual dimensions |

MeasureContent(Size available) is protected virtual, returns content size excluding margin/padding, default zero. It must return finite nonnegative dimensions. ArrangeContent(Rect contentBounds) is protected virtual and defaults to no action. A derived Container can use these to measure/arrange Children.

Measure caches the last result for identical constraints until InvalidateLayout. Intrinsic property changes in custom widgets must invalidate measurement; callbacks are not guaranteed on every pass.

Arrange consumes Margin, resolves alignment, applies constraints, sets actual geometry, and invokes ArrangeContent. Root viewport allocation and Grow can force a dimension beyond a requested size. Invalid sizes throw ArgumentOutOfRangeException. Per-axis max constraints are treated as at least the corresponding minimum.

Content suppresses automatic stretching. Fill measures to finite available size and fills its arranged slot; under infinite measurement it falls back to intrinsic size. In stacks, a main-axis Fill slot uses the entire padded parent extent, not a share of spare space. Explicit root/Grow allocation can override Content. See [SizingMode](#sizingmode).

### Activation, events, and hooks

PerformClick checks effective visibility/enabled ancestry, calls OnClick, then raises Click. It does not require attachment, focus, or a pointer gesture. It is not equivalent to ProcessPointer.

| Event | Type | Hook before public event |
| --- | --- | --- |
| Click | Action | protected virtual OnClick() |
| PointerInput | Action<PointerEvent> | protected virtual OnPointerInput(PointerEvent) |
| KeyboardInput | Action<KeyEvent> | protected virtual OnKeyboardInput(KeyEvent) |
| TextInput | Action<TextInputEvent> | protected virtual OnTextInput(TextInputEvent) |
| FocusChanged | Action | protected virtual OnFocusChanged() |

Public virtual OnMouseDown(float x, float y), OnMouseUp, and OnMouseMove are legacy local-coordinate hooks. Base OnPointerInput invokes them for Down/Up/Move; use typed pointer events when button/wheel identity matters. Other base hooks do nothing. Public input events still occur after the virtual hook; Handled controls subsequent bubbling.

## Container

Concrete Widget and the built-in traversed composite type.

| Member | Default / contract |
| --- | --- |
| Container() | Creates an empty owned WidgetCollection |
| Children | Read-only collection reference; collection contents are mutable |
| Layout | Vertical |
| Gap | 0; finite nonnegative separation of visible margin boxes in stacks |
| HorizontalContentAlignment | Stretch |
| VerticalContentAlignment | Start |
| Background | Color.Transparent |
| Draw(context) | Fills actual rounded bounds with Background |

MeasureContent/ArrangeContent implement stack, overlay, and absolute behavior and can be overridden. Children flow in insertion order, paint ascending ZIndex, and hit-test reverse paint order. Hidden children collapse with no extra gap.

Vertical/Horizontal stack measurement is unbounded on the main axis. Grow expands measured bases into spare main-axis space, redistributing after maximums. Main-axis content alignment moves the group; cross-axis defaults/child overrides place individual children. Overlay/Absolute ignore Gap and Grow.

## WidgetCollection

Sealed Collection<Widget> subclass, owned by one Container. Users obtain it through Children; its constructor is internal.

Inherited public surface includes Count, indexed get/set, Add, Insert, Remove, RemoveAt, Clear, Contains, IndexOf, CopyTo, and enumeration. Standard index/array argument rules come from Collection<Widget>. Add/Insert/replacement reject null, already-parented items, live input-tree roots, and cycles. Remove first before reparenting.

Replacing an item with itself is a no-op. Removing/replacing/clearing updates Parent and invalidates the owning container. Removing a widget does not dispose it or native resources; Widget is not IDisposable. The protected collection override signatures appear in the generated inventory because they implement this ownership behavior; the collection itself is sealed.

## Button

Concrete subclassable Widget with intrinsic single-line text measurement and activation behavior.

| Property | Default |
| --- | --- |
| Text | "Button"; non-null; changing invalidates measure |
| FontSize | 16; finite positive; changing invalidates measure |
| Background | Color(52, 89, 160) |
| HoverBackground | Color(65, 111, 193) |
| PressedBackground | Color(38, 67, 125) |
| Foreground | Color.White |
| IsPressed | Read-only; effective eligibility plus hovered pointer press or active keyboard press |

Constructor also sets IsFocusable=true, Padding=(16,12), and Radii=6. Width/Height remain auto. MeasureContent uses text advance width and at least 20 logical units of text height, excluding padding.

Draw selects normal/hover/pressed fill; disabled ancestry uses Color(65,65,70). It draws a white focus outline when large enough and centers text approximately around the content area using a baseline. Foreground is not automatically changed for disabled state.

Primary pointer Down/Up/Cancel updates internal pressed state; InputManager supplies capture and click synthesis. Enter/Space activation occurs on matching release after a non-repeat press. It marks those keyboard events handled. Losing focus clears keyboard activation. Protected MeasureContent, OnPointerInput, OnKeyboardInput, and OnFocusChanged are override points; call base to preserve built-in behavior.

## Alignment and LayoutMode

Alignment values: Start, Center, End, Stretch. Stretch fills an automatic dimension; explicit dimensions stay requested unless Grow/root allocation forces them.

LayoutMode values: Vertical, Horizontal, Overlay, Absolute. [Layout guide](../layout.md) explains measurement/slots and provides diagrams.

## LayoutEngine

Static Layout(Widget root, Size viewport) validates a non-null unparented root and finite nonnegative viewport, then runs measurement and viewport arrangement. It reuses a valid layout when viewport is unchanged. Zero viewport dimensions are valid; child overflow/constraints may still exist but clipping can exclude them.

A window calls this automatically. Standalone trees must provide a viewport. Mutation of custom measurement data requires InvalidateLayout. Custom layout code may call Measure/Arrange directly but is responsible for ordering, hidden children, and consistent slots.

## SizingMode

Automatic, Content, Fill. These are per-axis Widget sizing policies, not track lengths or positional alignment. Content measures intrinsic dimensions; Fill occupies available/allocated dimensions within limits; Automatic preserves numeric/NaN-plus-alignment behavior. The last Width/WidthSizing or Height/HeightSizing assignment wins.

## Grid types

Grid, GridLength, GridUnit, GridTrack, and GridTrackCollection are documented in the [Grid API reference](grid.md).

