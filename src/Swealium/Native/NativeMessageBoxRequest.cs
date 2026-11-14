using System.Runtime.InteropServices;
namespace Swealium.Native;

/// <summary>Owns all UTF-8 strings and ABI-correct native button storage for one synchronous call.</summary>
internal sealed class NativeMessageBoxRequest : IDisposable
{
    private readonly List<nint> allocations = [];
    private readonly DialogResult[] results;
    internal readonly Sdl.MessageBoxData Data;

    internal NativeMessageBoxRequest(MessageBoxOptions options, nint parent)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateString(options.Title, nameof(options.Title));
        ValidateString(options.Message, nameof(options.Message));
        var buttons = options.Buttons switch
        {
            MessageBoxButtons.Ok => new[] { DialogResult.Ok },
            MessageBoxButtons.OkCancel => [DialogResult.Ok, DialogResult.Cancel],
            MessageBoxButtons.YesNo => [DialogResult.Yes, DialogResult.No],
            MessageBoxButtons.YesNoCancel => [DialogResult.Yes, DialogResult.No, DialogResult.Cancel],
            MessageBoxButtons.RetryCancel => [DialogResult.Retry, DialogResult.Cancel],
            MessageBoxButtons.AbortRetryIgnore => [DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore],
            _ => throw new ArgumentOutOfRangeException(nameof(options.Buttons))
        };
        results = buttons;
        uint flags = options.Icon switch
        {
            MessageBoxIcon.Information => 0x40u,
            MessageBoxIcon.Warning => 0x20u,
            MessageBoxIcon.Error => 0x10u,
            _ => throw new ArgumentOutOfRangeException(nameof(options.Icon))
        };
        DialogResult defaultButton = options.DefaultButton ?? buttons[0];
        DialogResult escapeButton = options.EscapeButton ?? (buttons.Contains(DialogResult.Cancel) ? DialogResult.Cancel
            : buttons.Contains(DialogResult.No) ? DialogResult.No : buttons[0]);
        if (!buttons.Contains(defaultButton)) throw new ArgumentException("DefaultButton must be one of the displayed buttons.", nameof(options));
        if (!buttons.Contains(escapeButton)) throw new ArgumentException("EscapeButton must be one of the displayed buttons.", nameof(options));
        try
        {
            int stride = Marshal.SizeOf<Sdl.MessageBoxButton>();
            nint nativeButtons = Marshal.AllocCoTaskMem(checked(stride * buttons.Length));
            allocations.Add(nativeButtons);
            for (int i = 0; i < buttons.Length; i++)
            {
                DialogResult result = buttons[i];
                var button = new Sdl.MessageBoxButton
                {
                    Flags = (result == defaultButton ? 1u : 0) | (result == escapeButton ? 2u : 0),
                    Id = (int)result,
                    Text = Utf8(result == DialogResult.Ok ? "OK" : result.ToString())
                };
                Marshal.StructureToPtr(button, nativeButtons + i * stride, false);
            }
            Data = new()
            {
                Flags = flags | 0x80u, Window = parent, Title = Utf8(options.Title), Message = Utf8(options.Message),
                ButtonCount = buttons.Length, Buttons = nativeButtons
            };
        }
        catch { Dispose(); throw; }
    }
    private nint Utf8(string value)
    {
        nint memory = Marshal.StringToCoTaskMemUTF8(value);
        allocations.Add(memory);
        return memory;
    }
    private static void ValidateString(string? value, string name)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        if (value.Contains('\0')) throw new ArgumentException("Text cannot contain a null character.", name);
    }
    internal DialogResult Resolve(int buttonId)
    {
        if (buttonId == -1) return DialogResult.Cancel;
        var result = (DialogResult)buttonId;
        if (!results.Contains(result)) throw new InvalidOperationException("The native message box returned an unknown button.");
        return result;
    }
    public void Dispose()
    {
        foreach (nint memory in allocations) Marshal.FreeCoTaskMem(memory);
        allocations.Clear();
    }
}

