using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Swealium;

internal static class HeadlessTestEnvironment
{
    internal static int? RelaunchIfNeeded(string[] args)
    {
        if (Environment.GetEnvironmentVariable("SDL_VIDEO_DRIVER") == "dummy" &&
            Environment.GetEnvironmentVariable("SDL_RENDER_DRIVER") == "software")
            return null;

        // On Unix, .NET Environment.SetEnvironmentVariable changes a managed cache, not
        // libc's environment. Supply the OS environment at process creation so native
        // SDL sees the same drivers, including after repeated SDL_Init/SDL_Quit cycles.
        string executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot identify the test executable.");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        foreach (string argument in args) start.ArgumentList.Add(argument);
        start.Environment["SDL_VIDEO_DRIVER"] = "dummy";
        start.Environment["SDL_RENDER_DRIVER"] = "software";
        Console.WriteLine("Starting tests with native SDL dummy/software drivers.");
        using var child = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start the headless test process.");
        child.WaitForExit();
        return child.ExitCode;
    }

    internal static void VerifyNativeDrivers()
    {
        // Repeated application sessions exercise SDL environment teardown/reinitialization.
        for (int i = 0; i < 2; i++)
            SwealiumApplication.Run(new() { Width = 80, Height = 60 }, window =>
            {
                if (Marshal.PtrToStringUTF8(GetCurrentVideoDriver()) != "dummy" ||
                    Marshal.PtrToStringUTF8(GetHint("SDL_RENDER_DRIVER")) != "software")
                    throw new InvalidOperationException("Native SDL did not receive the headless driver configuration.");
                window.RequestClose();
            });
    }

    [DllImport("SDL3", EntryPoint = "SDL_GetCurrentVideoDriver", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetCurrentVideoDriver();

    [DllImport("SDL3", EntryPoint = "SDL_GetHint", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetHint([MarshalAs(UnmanagedType.LPUTF8Str)] string name);
}
