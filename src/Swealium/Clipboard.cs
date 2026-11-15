using System.Runtime.InteropServices;
using Swealium.Native;
namespace Swealium;

/// <summary>Native Unicode text clipboard. Call on the UI thread while the native host is running.</summary>
public static class Clipboard
{
    public static string GetText()
    {
        SwealiumApplication.VerifyAccess();
        nint value = Sdl.SDL_GetClipboardText();
        try { return Marshal.PtrToStringUTF8(value) ?? ""; }
        finally { Sdl.SDL_free(value); }
    }
    public static void SetText(string text)
    {
        SwealiumApplication.VerifyAccess(); ArgumentNullException.ThrowIfNull(text);
        if (text.Contains('\0')) throw new ArgumentException("Clipboard text cannot contain null characters.", nameof(text));
        Sdl.Check(Sdl.SDL_SetClipboardText(text));
    }
}
