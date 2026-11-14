using Swealium.Native;
namespace Swealium;

public enum MessageBoxIcon { Information, Warning, Error }
public enum MessageBoxButtons { Ok, OkCancel, YesNo, YesNoCancel, RetryCancel, AbortRetryIgnore }

public sealed record MessageBoxOptions
{
    public string Title { get; init; } = "Swealium";
    public string Message { get; init; } = string.Empty;
    public MessageBoxIcon Icon { get; init; } = MessageBoxIcon.Information;
    public MessageBoxButtons Buttons { get; init; } = MessageBoxButtons.Ok;
    public DialogResult? DefaultButton { get; init; }
    public DialogResult? EscapeButton { get; init; }
}

/// <summary>SDL-provided system message boxes. Call on the UI/main thread; Show blocks until dismissed.</summary>
public static class MessageBox
{
    public static DialogResult Show(MessageBoxOptions options, SwealiumWindow? owner = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        SwealiumApplication.VerifyAccess();
        nint parent = owner?.NativeHandle ?? 0;
        if (owner?.IsInputBlocked == true) throw new InvalidOperationException("Use the active dialog as the message box owner.");
        var previousFocus = owner?.Input.FocusedWidget;
        using var request = new NativeMessageBoxRequest(options, parent);
        owner?.Input.Cancel();
        try
        {
            Sdl.Check(Sdl.SDL_ShowMessageBox(in request.Data, out int result));
            return request.Resolve(result);
        }
        finally
        {
            if (owner is { IsDisposed: false, IsCloseRequested: false }) owner.Input.Focus(previousFocus);
        }
    }
    public static DialogResult Information(string title, string message, SwealiumWindow? owner = null) =>
        Show(new() { Title = title, Message = message }, owner);
    public static DialogResult Warning(string title, string message, SwealiumWindow? owner = null) =>
        Show(new() { Title = title, Message = message, Icon = MessageBoxIcon.Warning }, owner);
    public static DialogResult Error(string title, string message, SwealiumWindow? owner = null) =>
        Show(new() { Title = title, Message = message, Icon = MessageBoxIcon.Error }, owner);
    public static DialogResult Confirm(string title, string message, SwealiumWindow? owner = null) =>
        Show(new() { Title = title, Message = message, Buttons = MessageBoxButtons.YesNo,
            DefaultButton = DialogResult.No, EscapeButton = DialogResult.No }, owner);
}

