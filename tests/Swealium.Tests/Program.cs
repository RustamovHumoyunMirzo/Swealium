using System.Reflection;
using System.Runtime.InteropServices;
using SkiaSharp;
using Swealium;
using Swealium.Graphics;
using Swealium.Input;
using Swealium.Native;
using Swealium.UI;

if (args.Contains("--gpu-rendering"))
{
    Swealium.Tests.RenderingOptionsTests.NativeGpu();
    Console.WriteLine("Native GPU rendering checks passed.");
    return 0;
}

if (args.Contains("--native-window-controls"))
{
    WindowControlTests.RequireNativeHandle = true;
    WindowControlTests.Controls();
    Console.WriteLine("Native window control checks passed.");
    return 0;
}
if (HeadlessTestEnvironment.RelaunchIfNeeded(args) is { } childExitCode)
    return childExitCode;
(int Passed, int Failed) result = (0, 0);
Run("headless drivers are visible to native SDL", HeadlessTestEnvironment.VerifyNativeDrivers);
Run("tree ownership, cycles, and replacement", Tree);
Run("hit testing, bubbling, capture, click, and cancellation", Pointer);
Run("focus navigation, keyboard activation, and Unicode text", Keyboard);
Run("native SDL mouse, wheel, key, and UTF-8 decoding", NativeInput);
Run("retained rendering: offsets, clipping, z order, and context lifetime", Rendering);
Run("public API contains no backend types", PublicApi);
Run("native window, rendering, event filtering, and exception cleanup", Window);
Run("layout spacing, auto size, and invalidation", LayoutTests.Spacing);
Run("layout alignment and positioning modes", LayoutTests.AlignmentAndModes);
Run("layout growth, constraints, and overflow", LayoutTests.GrowthAndConstraints);
Run("rounded corner hover, clicks, and parent clipping", LayoutTests.RoundedInput);
Run("z-index and asymmetric corner pixels", LayoutTests.PixelsAndZOrder);
Run("multiple windows: routing, independent lifetime, and owned cleanup", WindowTests.MultipleWindows);
Run("nested modal dialogs and focus restoration", WindowTests.Modality);
Run("async dialog completion resumes on UI thread", WindowTests.AsyncDialog);
Run("cancelable close and forced owner shutdown", WindowTests.Closing);
Run("native message-box ABI, presets, Unicode, and results", WindowTests.MessageBoxes);
Run("window controls, live getters, options, and validation", WindowControlTests.Controls);
Run("window event payloads, focus cleanup, and subclass hooks", WindowControlTests.Events);
Run("native window handle mapping for all supported systems", InteropTests.HandleMapping);
Run("explicit content/fill sizing and stack semantics", GridTests.Sizing);
Run("grid fixed/content/fraction tracks and padding/gaps", GridTests.Tracks);
Run("grid spanning and implicit tracks", GridTests.Spans);
Run("grid constrained fractions and extreme weights", GridTests.Constraints);
Run("grid dependent measurement, nesting, and invalidation", GridTests.Measurement);
Run("grid collection ownership and invalid arguments", GridTests.OwnershipAndValidation);
Run("grid cell rendering, hit testing, z order, and resize", GridTests.RenderingAndResize);
Run("wrap boundaries, orientation, gaps, and weighted allocation", TextTests.Flow);
Run("shaping, bidi, wrapping, fallback, HTML, and UTF-16 indices", TextTests.Shaping);
Run("grapheme editing, selection, undo, read-only, and IME commit", TextTests.Editing);
Run("native IME decoding, candidate ownership, clipboard, and rendering", TextTests.NativeIme);
Run("shaped text, preedit, and candidate raster rendering", TextTests.Rendering);
Run("Shift Home/End selection across lines and wrapped caret affinity", Swealium.Tests.ScrollStrokeTests.SelectionEdges);
Run("scroll extent, viewport, directions, gutters, clamping, and focus reveal", Swealium.Tests.ScrollStrokeTests.ScrollLayout);
Run("scroll wheel, nested bubbling, thumb capture, track paging, and keyboard", Swealium.Tests.ScrollStrokeTests.ScrollInput);
Run("scroll clipping, custom bars, and per-side rounded stroke pixels", Swealium.Tests.ScrollStrokeTests.Pixels);
Run("native held Shift, keypad Home/End, logical line edges, and selection pixels",Swealium.Tests.EditingStrokeRegressionTests.NativeSelection);
Run("native and nested Shift-wheel horizontal routing",Swealium.Tests.EditingStrokeRegressionTests.ShiftWheel);
Run("public focus strokes and explicit local stroke precedence",Swealium.Tests.EditingStrokeRegressionTests.FocusStrokes);
Run("solid/dashed/dotted/double/hidden stroke styles and blended corners",Swealium.Tests.EditingStrokeRegressionTests.StrokeStyles);
Run("raster format fixtures, encoders, streams, alpha, and decode limits",Swealium.Tests.MediaTests.BitmapFormats);
Run("bitmap drawing, source crops, opacity, pixel ownership, and context lifetime",Swealium.Tests.MediaTests.BitmapDrawing);
Run("SVG/SVGZ, recorded vectors, paths, resource policy, and rasterization",Swealium.Tests.MediaTests.VectorRendering);
Run("image/vector widget layout, aspect ratio, clipping, and input",Swealium.Tests.MediaTests.DisplayWidgets);
Run("system/custom cursors, inheritance, capture, snapshots, and disposal",Swealium.Tests.MediaTests.CursorRouting);
Run("native cursor caches, windows, headless fallback, and repeated session cleanup",Swealium.Tests.MediaTests.NativeCursors);
Run("anti-aliasing defaults, context overrides, text, and lifetime",Swealium.Tests.RenderingOptionsTests.AntiAliasing);
Run("software selection, GPU fallback, resize, and settings lifecycle",Swealium.Tests.RenderingOptionsTests.SoftwareAndFallback);
Run("subtree opacity, overlapping children, strokes, and image compatibility",Swealium.Tests.InteractionBoundaryTests.Opacity);
Run("base64/data URIs and bitmap resize/crop/rotate/flip/clear operations",Swealium.Tests.InteractionBoundaryTests.BitmapOperations);
Run("independent pointer capture, primary focus, aggregate hover, and cancellation",Swealium.Tests.InteractionBoundaryTests.MultiplePointers);
Run("SDL touch routing, normalized positions, pen metadata, and emulation filtering",Swealium.Tests.InteractionBoundaryTests.NativePointers);
Run("simultaneous scrollbar drags and single-owner text selection",Swealium.Tests.InteractionBoundaryTests.MultipleControlDrags);
Run("semantic tree, clipping, stable IDs, redaction, and accessibility actions",Swealium.Tests.InteractionBoundaryTests.AccessibilityTree);
Run("accessibility bridge attach/publish/detach, thread checks, and failure cleanup",Swealium.Tests.InteractionBoundaryTests.BridgeLifecycle);
Run("gradient kinds, spread modes, color compatibility, and bitmap/path integration",Swealium.Tests.GradientTests.Shapes);
Run("gradient glyphs, backgrounds, selections, strokes, and recorded vectors",Swealium.Tests.GradientTests.TextAndStrokes);
Console.WriteLine($"{result.Passed} passed; {result.Failed} failed.");
return result.Failed == 0 ? 0 : 1;

void Run(string name, Action test)
{
    try { test(); result.Passed++; Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { result.Failed++; Console.Error.WriteLine($"FAIL {name}: {error}"); }
}
static void Check(bool condition, string message = "Assertion failed")
{
    if (!condition) throw new InvalidOperationException(message);
}
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}");
}
static (Container Root, Container Panel, Button Button, InputManager Input) Fixture()
{
    var root = new Container { Width = 200, Height = 160, Layout = LayoutMode.Absolute };
    var panel = new Container { X = 10, Y = 20, Width = 100, Height = 100, Layout = LayoutMode.Absolute };
    var button = new Button { X = 5, Y = 6, Width = 60, Height = 40 };
    root.Children.Add(panel);
    panel.Children.Add(button);
    LayoutEngine.Layout(root, new(200, 160));
    return (root, panel, button, new InputManager { Root = root });
}
static void Tree()
{
    var (root, panel, button, input) = Fixture();
    Throws<InvalidOperationException>(() => new InputManager { Root = root });
    Throws<InvalidOperationException>(() => new Container().Children.Add(root));
    Throws<InvalidOperationException>(() => panel.Children.Add(root));
    Throws<InvalidOperationException>(() => root.Children.Add(button));
    Throws<InvalidOperationException>(() => panel.Children.Add(button));
    var replacement = new Button();
    panel.Children[0] = replacement;
    Check(button.Parent is null && replacement.Parent == panel);
    panel.Children.Clear();
    Check(replacement.Parent is null);
    root.Children.Remove(panel);
    Check(panel.Parent is null);
    input.Root = null;
    var nextOwner = new Container();
    nextOwner.Children.Add(root);
    Check(root.Parent == nextOwner);
}
static void Pointer()
{
    var (root, panel, button, input) = Fixture();
    int clicks = 0, bubbled = 0;
    button.Click += () => clicks++;
    Point local = default;
    button.PointerInput += e => { if (e.Kind == PointerEventKind.Down) local = e.Position; };
    panel.PointerInput += e => { if (e.Kind == PointerEventKind.Down) { bubbled++; Check(e.Position == new Point(10, 10)); } };
    input.ProcessPointer(new(PointerEventKind.Down, new(20, 30), PointerButton.Primary));
    Check(local == new Point(5, 4) && bubbled == 1 && button.IsPressed && input.FocusedWidget == button);
    input.ProcessPointer(new(PointerEventKind.Move, new(180, 140)));
    Check(input.CapturedWidget == button && !button.IsPointerOver && !button.IsPressed);
    input.ProcessPointer(new(PointerEventKind.Up, new(180, 140), PointerButton.Primary));
    Check(clicks == 0 && input.CapturedWidget is null);
    input.ProcessPointer(new(PointerEventKind.Down, new(20, 30), PointerButton.Primary));
    input.ProcessPointer(new(PointerEventKind.Up, new(20, 30), PointerButton.Primary));
    Check(clicks == 1 && !button.IsPressed);
    var top = new Button { X = 5, Y = 6, Width = 60, Height = 40 };
    panel.Children.Add(top);
    Check(input.HitTest(new(20, 30)) == top);
    top.IsEnabled = false;
    Check(input.HitTest(new(20, 30)) == button);
    top.IsEnabled = true;
    top.PointerInput += e => { if (e.Kind == PointerEventKind.Down) e.Handled = true; };
    input.ProcessPointer(new(PointerEventKind.Down, new(20, 30), PointerButton.Primary));
    Check(bubbled == 2, "Handled input must not bubble");
    input.Cancel();
    Check(!top.IsPressed && !top.IsFocused && !top.IsPointerOver);
    top.IsVisible = false;
    input.ProcessPointer(new(PointerEventKind.Down, new(20, 30), PointerButton.Primary));
    panel.IsEnabled = false;
    input.Validate();
    Check(input.CapturedWidget is null && input.FocusedWidget is null && !button.IsPressed);
    panel.IsEnabled = true;
    panel.Children.Remove(button);
    Check(input.HitTest(new(20, 30)) == panel);
    Check(input.HitTest(new(200, 160)) is null, "Bounds use exclusive right/bottom edges");
}
static void Keyboard()
{
    var (root, panel, button, input) = Fixture();
    var second = new Button { X = 70, Width = 20 };
    panel.Children.Add(second);
    input.ProcessKey(new(Key.Tab, true));
    Check(input.FocusedWidget == button);
    input.ProcessKey(new(Key.Tab, true));
    Check(input.FocusedWidget == second);
    input.ProcessKey(new(Key.Tab, true, KeyModifiers.Shift));
    Check(input.FocusedWidget == button);
    int clicks = 0;
    button.Click += () => clicks++;
    input.ProcessKey(new(Key.Space, true));
    input.ProcessKey(new(Key.Space, true, isRepeat: true));
    Check(button.IsPressed && clicks == 0);
    input.ProcessKey(new(Key.Space, false));
    Check(!button.IsPressed && clicks == 1);
    input.ProcessKey(new(Key.Enter, true));
    input.Cancel();
    input.Focus(button);
    input.ProcessKey(new(Key.Enter, false));
    Check(clicks == 1, "Focus loss cancels keyboard activation");
    string? text = null;
    button.TextInput += e => text = e.Text;
    input.ProcessText(new("ignored"));
    Check(text is null);
    button.AcceptsTextInput = true;
    input.ProcessText(new("Salom 世界 👋"));
    Check(text == "Salom 世界 👋");
    button.IsVisible = false;
    input.ProcessKey(new(Key.A, true));
    Check(input.FocusedWidget is null && !button.IsFocused);
    Check(!input.Focus(button));
    input.Root = new Container();
    Check(root.Parent is null);
}
static Sdl.Event NativeEvent(uint type, uint windowId = 1, params (int Offset, byte[] Data)[] fields)
{
    byte[] bytes = new byte[128];
    BitConverter.GetBytes(type).CopyTo(bytes, 0);
    BitConverter.GetBytes(windowId).CopyTo(bytes, 16);
    foreach (var field in fields) field.Data.CopyTo(bytes, field.Offset);
    return MemoryMarshal.Read<Sdl.Event>(bytes);
}
static void NativeInput()
{
    var (_, _, button, input) = Fixture();
    Point position = default, wheel = default;
    PointerButton pressed = PointerButton.None;
    Key key = Key.Unknown;
    KeyModifiers modifiers = KeyModifiers.None;
    bool repeat = false;
    string? text = null;
    button.PointerInput += e =>
    {
        if (e.Kind == PointerEventKind.Down) { position = e.Position; pressed = e.Button; }
        if (e.Kind == PointerEventKind.Wheel) wheel = e.WheelDelta;
    };
    button.KeyboardInput += e => { key = e.Key; modifiers = e.Modifiers; repeat = e.IsRepeat; };
    button.TextInput += e => text = e.Text;
    SdlInput.Dispatch(NativeEvent(0x401, 1, (24, [1]), (28, BitConverter.GetBytes(20f)), (32, BitConverter.GetBytes(30f))), input);
    Check(position == new Point(5, 4) && pressed == PointerButton.Primary);
    SdlInput.Dispatch(NativeEvent(0x402, 1, (24, [1]), (28, BitConverter.GetBytes(20f)), (32, BitConverter.GetBytes(30f))), input);
    SdlInput.Dispatch(NativeEvent(0x403, 1, (24, BitConverter.GetBytes(1.5f)), (28, BitConverter.GetBytes(-2f)),
        (32, BitConverter.GetBytes(1u)), (36, BitConverter.GetBytes(20f)), (40, BitConverter.GetBytes(30f))), input);
    Check(wheel == new Point(-1.5f, 2));
    SdlInput.Dispatch(NativeEvent(0x300, 1, (24, BitConverter.GetBytes(4)), (32, BitConverter.GetBytes((ushort)0xc3)), (37, [1])), input);
    Check(key == Key.A && modifiers == (KeyModifiers.Shift | KeyModifiers.Control) && repeat);
    button.AcceptsTextInput = true;
    nint utf8 = Marshal.StringToCoTaskMemUTF8("世界 👋");
    try
    {
        SdlInput.Dispatch(NativeEvent(0x303, 1, (IntPtr.Size == 8 ? 24 : 20,
            IntPtr.Size == 8 ? BitConverter.GetBytes(utf8.ToInt64()) : BitConverter.GetBytes(utf8.ToInt32()))), input);
        Check(text == "世界 👋");
    }
    finally { Marshal.FreeCoTaskMem(utf8); }
    SdlInput.Dispatch(NativeEvent(0x20d), input);
    Check(!button.IsPointerOver);
    SdlInput.Dispatch(NativeEvent(0x20f), input);
    Check(input.FocusedWidget is null);
}
static void Rendering()
{
    using var bitmap = new SKBitmap(new SKImageInfo(64, 64, SKColorType.Rgba8888, SKAlphaType.Premul));
    using var surface = SKSurface.Create(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes)!;
    surface.Canvas.Clear(SKColors.Black);
    var root = new Container { Width = 32, Height = 32, Layout = LayoutMode.Absolute };
    var panel = new Container { X = 5, Y = 6, Width = 10, Height = 10, Background = new(255, 0, 0), Layout = LayoutMode.Absolute };
    var child = new TestWidget { X = 7, Y = 0, Width = 10, Height = 10 };
    panel.Children.Add(child);
    root.Children.Add(panel);
    LayoutEngine.Layout(root, new(32, 32));
    surface.Canvas.Scale(2, 2);
    var drawing = new SkiaDrawingContext(surface.Canvas);
    Check(drawing.MeasureText("Swealium", 16).Width > 0, "A usable default font is required for text rendering");
    drawing.End();
    WidgetRenderer.Render(surface.Canvas, root);
    surface.Canvas.Flush();
    Check(bitmap.GetPixel(12, 16).Red == 255, "Parent translation and scale");
    Check(bitmap.GetPixel(26, 16).Blue == 255, "Nested child translation");
    Check(bitmap.GetPixel(32, 16) == SKColors.Black, "Child clipped to parent");
    Throws<ObjectDisposedException>(() => child.LastContext!.FillEllipse(new(0, 0, 1, 1), Color.White));
    child.IsVisible = false;
    surface.Canvas.Clear(SKColors.Black);
    WidgetRenderer.Render(surface.Canvas, root);
    Check(bitmap.GetPixel(26, 16).Red == 255, "Invisible children do not draw");
}
static void PublicApi()
{
    static void ValidateType(Type type)
    {
        if (type.IsByRef || type.IsArray || type.IsPointer) { ValidateType(type.GetElementType()!); return; }
        Check(!(type.Namespace ?? "").StartsWith("SkiaSharp", StringComparison.Ordinal) &&
              !(type.Namespace ?? "").StartsWith("Topten", StringComparison.Ordinal) &&
              !(type.Namespace ?? "").StartsWith("HarfBuzzSharp", StringComparison.Ordinal) &&
              !(type.Namespace ?? "").StartsWith("AngleSharp", StringComparison.Ordinal) &&
              !(type.Namespace ?? "").StartsWith("Svg", StringComparison.Ordinal) &&
              !(type.Namespace ?? "").StartsWith("StbImageSharp", StringComparison.Ordinal) &&
              !(type.Namespace ?? "").StartsWith("SDL", StringComparison.Ordinal) &&
              !(type.Namespace ?? "").StartsWith("Swealium.Native", StringComparison.Ordinal), $"Backend type leaked: {type}");
        foreach (var argument in type.GetGenericArguments()) ValidateType(argument);
    }
    foreach (var type in typeof(SwealiumWindow).Assembly.GetExportedTypes())
    {
        ValidateType(type);
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly))
        {
            ValidateType(method.ReturnType);
            foreach (var parameter in method.GetParameters()) ValidateType(parameter.ParameterType);
        }
        foreach (var constructor in type.GetConstructors())
            foreach (var parameter in constructor.GetParameters()) ValidateType(parameter.ParameterType);
        foreach (var field in type.GetFields()) ValidateType(field.FieldType);
    }
}
static void Window()
{
    int clicks = 0, frames = 0;
    SwealiumApplication.Run(new WindowOptions { Width = 120, Height = 80 }, window =>
    {
        var button = new Button { Text = "Test" };
        button.Click += () => clicks++;
        window.Content = button;
        window.Paint += (_, size) =>
        {
            Check(size == new Size(120, 80) && button.ActualWidth == 120 && button.ActualHeight == 80);
            frames++;
            if (frames == 1)
            {
                var down = NativeEvent(0x401, window.Id, (24, [1]), (28, BitConverter.GetBytes(10f)), (32, BitConverter.GetBytes(10f)));
                var up = NativeEvent(0x402, window.Id, (24, [1]), (28, BitConverter.GetBytes(10f)), (32, BitConverter.GetBytes(10f)));
                var foreign = down; foreign.WindowId++;
                window.HandleSdlEvent(foreign);
                Check(window.Input.CapturedWidget is null);
                window.HandleSdlEvent(down);
                window.HandleSdlEvent(up);
                Check(clicks == 1);
                Throws<InvalidOperationException>(window.Render);
                Throws<InvalidOperationException>(window.Dispose);
            }
            if (frames == 3) window.RequestClose();
        };
    });
    Check(frames == 3 && clicks == 1);
    Throws<InvalidOperationException>(() => SwealiumApplication.Run(configure: w =>
        w.Paint += (_, _) => throw new InvalidOperationException("Drawing failure")));
    SwealiumApplication.Run(configure: w => w.Paint += (_, _) => w.RequestClose());
}
sealed class TestWidget : Widget
{
    internal DrawingContext? LastContext;
    public override void Draw(DrawingContext context)
    {
        LastContext = context;
        context.FillRectangle(new(0, 0, ActualWidth, ActualHeight), new Color(0, 0, 255));
    }
}

