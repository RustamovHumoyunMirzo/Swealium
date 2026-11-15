# Widget reference hub

[Documentation index](../README.md) · [All public API signatures](../api/reference.md) · [Layout](../layout.md)

Every exported widget is documented here, including the abstract base. Each page describes its own surface, defaults, inherited behavior, input, customization, and examples. All widgets live in `Swealium.UI`; drawing values live in `Swealium.Graphics`. Mutate the visual tree on the UI thread.

| Widget | Base | Purpose |
| --- | --- | --- |
| [Widget](widget.md) | — | Measurement, arrangement, drawing, strokes, rounded clipping, focus and routed input |
| [Container](container.md) | Widget | Owned children; vertical/horizontal stack, overlay, absolute placement |
| [Grid](grid.md) | Container | Fixed/content/fraction tracks, explicit cells and spanning |
| [WrapContainer](wrap-container.md) | Container | Horizontal rows or vertical columns with finite boundary wrapping |
| [ScrollContainer](scroll-container.md) | Container | Clipped scrolling, directional policy, styled or custom scrollbars |
| [Button](button.md) | Widget | Pointer/keyboard activation and shaped caption |
| [Text](text.md) | Widget | Shaped paragraphs, bidi, wrapping, styled HTML text |
| [TextBox](text-box.md) | Widget | Plain-text editing, selection, clipboard, history, preedit and candidates |

WidgetCollection, GridTrack/Collection, ScrollBarStyle/Info, and layout enums are supporting APIs rather than widgets. Their contracts are linked from the relevant pages. The API generator verifies that every exported Widget subclass has a page here, preventing undocumented widget additions.

Choose a Container for one-dimensional stacks; WrapContainer when items should flow across a boundary; Grid for explicit aligned rows/columns; ScrollContainer when content should extend beyond a finite viewport. ScrollContainer uses inherited Container.Layout to arrange its content; nest a Grid or WrapContainer inside it for those layout algorithms.

All widgets inherit `Stroke`, `Radii`, margin/padding, visibility, enablement, and sizing. Decorative strokes draw inside the rounded outer boundary after descendants and overlays. They do not change measurement or consume padding. See [Widget strokes](widget.md#per-side-strokes).
