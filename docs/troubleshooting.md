# Troubleshooting

[Documentation index](README.md)

## A window opens but nothing is visible

Confirm the content is assigned to the correct window, is visible, and has a nonzero arranged size. Width/Height are requests; use Bounds and ActualWidth/ActualHeight to diagnose arrangement. Custom Draw implementations should use actual dimensions because auto Width/Height remain NaN.

A plain Widget does not expose a traversed child collection. Derive composite visuals from Container. Hidden/minimized windows skip rendering but stay registered.

## X/Y or alignment seems ignored

X/Y only position children in LayoutMode.Absolute. A default Container stacks vertically. Main-axis content alignment moves the stack as a group; a child's main-axis alignment only acts inside its allocated slot. Stretch applies to automatic dimensions, not explicit sizes. Grow adds spare space rather than enforcing a total-size ratio.

WidthSizing/HeightSizing=Content opts out of Stretch; Fill takes the entire allocated dimension. Assigning a numeric Width/Height afterward resets that axis to Automatic. Multiple main-axis Fill children in a stack can each consume the parent extent; use Grow or fractional grid tracks to share space instead.

Grid placement is explicit; all new children default to cell (0,0). Set GridRow/GridColumn or they overlap. Only Grid uses those properties. GridLength.Content tracks do not automatically divide spare space; Fraction tracks do. A span includes internal gaps. Track minimums or fixed child dimensions can overflow a cell, subject to normal ancestor clipping.

Check margins, padding, min/max constraints, and ancestor clipping. A minimum larger than its maximum wins.

## Hover/click behaves unexpectedly

Input uses rounded geometry, not the entire bounding rectangle. A point in a rounded cut-out intentionally does not hover/click. A parent with rounded clipping also excludes its descendants there. ZIndex decides topmost sibling hits; transparent background does not automatically make a container input-transparent.

IsHitTestVisible=false excludes a whole subtree from pointer testing. IsEnabled=false excludes input but preserves layout. Captured motion/up can continue arriving after the pointer leaves a widget; hover appearance and capture are separate.

## Keys arrive but characters are missing

Key is physical keyboard identity, not typed text. The widget must be attached, focusable, focused, and AcceptsTextInput=true to receive committed TextInput. Window OS focus and widget focus are different. IME composition editing/candidates and a built-in text editor are not implemented.

## Native library cannot load

Restore dependencies and publish with an appropriate RID. Inspect the full loader error: an existing native library may still have a missing system dependency. On Linux, check fontconfig, SDL backend/display libraries, and the graphical session. Do not mix SDL2 with SDL3 or independently replace pinned native binaries.

Use a concrete runtime test on the deployment machine. Building an AnyCPU assembly is insufficient evidence for a new architecture.

## CI reports "No available video device"

A headless runner has no desktop video device. Set SDL_VIDEO_DRIVER=dummy and SDL_RENDER_DRIVER=software in the workflow step's env block before launching the test/sample process. On Unix, Environment.SetEnvironmentVariable inside .NET updates a managed cache and is insufficient for native SDL driver selection. The test harness automatically restarts with a native process environment when necessary, and verifies the selected drivers through SDL.

This is different from a missing native-library error: SDL loaded successfully but could not initialize an available video backend. Installing unrelated graphics libraries or skipping the window tests does not address driver selection.

## Shapes render but text is blank

The default system typeface may be unavailable. Install fonts and fontconfig on Linux. The package uses fontconfig-enabled Skia native assets and internal HarfBuzz shaping. TextStyle.FontFamily requests a family; system fallback still requires fonts containing the desired glyphs. See [text](text.md) for style and IME limitations.

## Window controls throw or appear delayed

Swealium surfaces SDL errors for unsupported operations. A headless driver has no full window manager. Wayland, macOS, and desktop policies can constrain positioning, opacity, focus activation, grabbing, fullscreen, or always-on-top behavior.

Setters request native changes; observe events/live getters afterward. Cached HasFocus follows the event stream. Events can describe an earlier queued notification than the state a live getter now observes.

## Closing the first window does not exit

Independent windows remain alive, including hidden ones. Inspect SwealiumApplication.Windows and use Exit to request closure of the current window set. Closing an owner destroys descendants, not independent windows. A Closing handler may cancel.

## Dialog task never completes

The dialog completes after native disposal. Do not block its UI thread with Wait/Result. Keep Run alive and allow its loop to process RequestClose. Check Closing cancellation. A hidden modal dialog can block its group until shown/closed. Use the active dialog as owner for further dialogs/message boxes.

## Native ID is rejected by an OS API

Id is a portable event-routing identifier. Use TryGetNativeWindowHandle, inspect Kind, and pass its borrowed Value/Display only to the matching native API. A false result is normal on a dummy/unsupported backend. Old copied descriptors are not safe after disposal or native object replacement.

## Thread or disposed-window exception

Native properties and methods enforce UI-thread/live-window access. Await normally preserves the UI context inside Run; Task.Run and ConfigureAwait(false) do not. Return to the captured UI context before mutating windows or widgets. Do not use a drawing context outside its callback.

For reproducible reports, include SDK/RID, OS/window backend, exact exception, a minimal visual tree, and whether the issue reproduces in the normal/headless or native-control test.

