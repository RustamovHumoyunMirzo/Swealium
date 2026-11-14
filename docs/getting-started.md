# Getting started

[Documentation index](README.md) · [API reference](api/reference.md)

## Requirements

Use a .NET 10 SDK. The repository's global.json requests the 10.0 feature band and permits newer feature bands. A desktop graphical session is required for normal windows. Linux also needs SDL's platform dependencies, fontconfig, and an installed font.

No package has been published. Reference the project while developing against this checkout:

```sh
dotnet new console -n MyApp -f net10.0
dotnet add MyApp reference /path/to/Swealium/src/Swealium/Swealium.csproj
```

The path is an example; substitute your checkout location. Native assets flow through package references; do not copy arbitrary SDL2 or Skia binaries over the resolved SDL3 dependencies.

## First application

Replace Program.cs with this complete entry point:

```csharp
using Swealium;
using Swealium.Graphics;
using Swealium.UI;

SwealiumApplication.Run(new WindowOptions
{
    Title = "My application",
    Width = 800,
    Height = 500
}, window =>
{
    var root = new Container
    {
        Padding = new Thickness(24),
        Gap = 12,
        VerticalContentAlignment = Alignment.Center,
        HorizontalContentAlignment = Alignment.Center
    };
    var greeting = new Button
    {
        Text = "Click or use Tab and Enter",
        Radii = new CornerRadii(20, 4, 20, 4)
    };
    int count = 0;
    greeting.Click += () => greeting.Text = $"Clicked {++count} times";
    root.Children.Add(greeting);
    window.Content = root;
});
```

Run with dotnet run --project MyApp. Run must execute on the process main thread. It owns SDL initialization, dispatches native events, renders windows, and releases resources before shutting SDL down.

The configure callback runs once after the first window is created. Create widgets, subscribe to events, and create additional windows there or in later UI callbacks. Keep widget mutation on the same UI thread.

## Learn the three coordinate spaces

- WindowPosition uses integer desktop coordinates. Some window managers limit positioning.
- Widget layout and drawing use floating-point **logical window units**. A child's Bounds is relative to its parent.
- PixelSize reports physical drawable pixels. Swealium scales logical drawing to these pixels.

Use ActualWidth/ActualHeight inside Draw, and the local Position inside pointer handlers. Requested Width/Height are not the final arranged size.

## Run the repository examples and checks

```sh
dotnet build -c Release
dotnet run --project samples/Swealium.Sample
dotnet run --project tests/Swealium.Tests -c Release
dotnet run --project samples/Swealium.Sample -- --smoke-test
```

The test harness is an executable; dotnet test is not its entry point. Its normal run uses SDL's dummy/software drivers. If they are not already configured, it restarts itself with those variables in the child process's native environment; this is necessary on Unix because changing .NET's managed environment does not update native SDL's environment. CI supplies the variables before process startup. A regression check verifies the drivers from native SDL across repeated application sessions.

The sample smoke test opens a main, owned child, and independent window, then closes them automatically. It does not open blocking native message boxes. On a headless machine, supply SDL_VIDEO_DRIVER=dummy and SDL_RENDER_DRIVER=software before launching the sample; the test harness's automatic relaunch applies only to the tests.

For an actual desktop window-control check:

```sh
dotnet run --project tests/Swealium.Tests -c Release -- --native-window-controls
```

## Next steps

Read [layout](layout.md) before assigning manual X/Y offsets. Read [windows](windows.md) before implementing close prompts or changing thread behavior. Use [custom widgets](custom-widgets.md) to add visuals beyond Container and Button.

