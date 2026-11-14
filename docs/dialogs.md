# Dialogs, warnings, and confirmation

[Documentation index](README.md) · [API contracts](api/dialogs.md)

## Choose the correct kind

| Need | API | Behavior |
| --- | --- | --- |
| Custom widget content, responsive independent windows | ShowDialog / ShowDialogAsync | A Swealium window with native parent/modal properties and framework input blocking |
| System warning/error/information | MessageBox.Warning / Error / Information | SDL-provided blocking system prompt |
| Standard response buttons | MessageBox.Show | Blocking native prompt with typed preset/results |
| Safe Yes/No confirmation | MessageBox.Confirm | Yes/No; default and Escape both No |

Custom dialogs have Swealium-rendered content; they are not native toolkit controls. System message boxes are native where SDL supports it. On X11 SDL draws an X11 dialog instead of using GTK or Qt. [SDL documents these platform differences](https://wiki.libsdl.org/SDL3/SDL_ShowMessageBox).

## Await a custom dialog

```csharp
openButton.Click += async () =>
{
    DialogResult result = await main.ShowDialogAsync(
        new WindowOptions { Title = "Confirm", Width = 360, Height = 200, Resizable = false },
        dialog =>
        {
            var panel = new Container { Padding = new(24), Gap = 12 };
            var ok = new Button { Text = "Accept" };
            var cancel = new Button { Text = "Cancel" };
            ok.Click += () => dialog.CloseDialog(DialogResult.Ok);
            cancel.Click += () => dialog.CloseDialog(DialogResult.Cancel);
            panel.Children.Add(ok);
            panel.Children.Add(cancel);
            dialog.Content = panel;
        });
    openButton.Text = $"Result: {result}";
};
```

ShowDialog returns a window immediately; its Completion task is also available. ShowDialogAsync returns that task. Configure callbacks run synchronously; a thrown configuration error disposes the new dialog and releases modality.

A modal window blocks its ownership group, not independent top-level windows. Nested dialogs become the active modal target. Open children/additional dialogs from the active dialog, rather than the blocked owner. Hidden modal windows still exist; do not leave a hidden modal dialog blocking its owner without a way to show or close it.

CloseDialog validates the result and requests closing. If Closing cancels, the result rolls back and Completion stays pending. A native close/forced destruction defaults a modal result to Cancel when no explicit result was committed. Completion occurs after resource disposal. The prior owner's widget focus is restored if it remains eligible and the owner is alive/unblocked.

Do not block with Wait or Result before completion. Await in a UI callback captures the application's synchronization context; an external host must provide its own.

## Use standard native buttons

```csharp
DialogResult result = MessageBox.Show(new MessageBoxOptions
{
    Title = "Unsaved changes",
    Message = "Continue without saving?",
    Icon = MessageBoxIcon.Warning,
    Buttons = MessageBoxButtons.YesNoCancel,
    DefaultButton = DialogResult.No,
    EscapeButton = DialogResult.Cancel
}, main);
```

DefaultButton and EscapeButton must be among the preset's displayed results. Omit them to use the first displayed default button and the first applicable Escape choice: Cancel, then No, then the first button. Confirm explicitly chooses No for both.

Native Show blocks the UI thread until a response; managed frames and custom dialog animations pause. An optional owner supplies the native parent. If a modal dialog is already active, use it as owner; passing its blocked owner is rejected.

Message/title text is UTF-8, may contain Unicode/newlines, and cannot contain embedded null characters. Preset labels are currently English. A native dismissal without a button maps to Cancel, even if the selected preset does not display a Cancel button.

## Errors and shutdown

Unsupported native backends throw with SDL's error; Swealium does not silently substitute a custom dialog. A parentless message box can be used on the main thread before Run for a startup error, subject to native library availability and SDL platform support.

Owner destruction overrides child close vetoes. Handle recoverable errors in async event handlers; an exception escaping the UI synchronization context terminates Run after cleanup.

The automated suite validates message-box ABI layout, UTF-8 storage, flags, presets, and result mapping without displaying blocking prompts. Test interactive native presentation through the sample on each target platform.

