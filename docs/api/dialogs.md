# Dialog and message-box API contracts

[Documentation index](../README.md) · [Complete signatures](reference.md) · [Dialog guide](../dialogs.md)

Namespace: Swealium.

## DialogResult

Values: None, Ok, Cancel, Yes, No, Retry, Abort, Ignore. None is an uncommitted/custom-dialog default, not a valid argument to CloseDialog. These are Swealium results, not native button IDs for external APIs.

For custom modal windows, native dismissal/owner destruction produces Cancel if no explicit result was already committed. Nonmodal Completion ordinarily produces None. A pending task completes only after disposal. A canceled Closing event leaves the task pending and CloseDialog restores the previous result.

For native message boxes, dismissing without a selected button produces Cancel even when no Cancel button was displayed.

## MessageBoxOptions

Sealed record; all strings use managed UTF-16 input and are copied to UTF-8 native storage for synchronous presentation.

| Property | Default | Contract |
| --- | --- | --- |
| Title | "Swealium" | Non-null; no embedded NUL |
| Message | empty string | Non-null; Unicode/newlines allowed; no embedded NUL |
| Icon | Information | Defined MessageBoxIcon |
| Buttons | Ok | Defined MessageBoxButtons preset |
| DefaultButton | null | Null picks first preset result; otherwise must be displayed |
| EscapeButton | null | Null picks Cancel if displayed, else No if displayed, else first result |

Undefined enums throw ArgumentOutOfRangeException. A default/Escape selection outside the preset or embedded NUL throws ArgumentException. Null text/options throws ArgumentNullException.

## MessageBoxButtons

| Preset | Displayed results, in requested order |
| --- | --- |
| Ok | Ok |
| OkCancel | Ok, Cancel |
| YesNo | Yes, No |
| YesNoCancel | Yes, No, Cancel |
| RetryCancel | Retry, Cancel |
| AbortRetryIgnore | Abort, Retry, Ignore |

SDL controls platform presentation/order details. Labels are currently English; Ok is displayed as OK. Default/escape button flags are distinct and may target the same result.

## MessageBoxIcon

Information, Warning, Error. Icon appearance is native-backend dependent.

## MessageBox

Static synchronous service. Show(MessageBoxOptions options, SwealiumWindow? owner = null) returns DialogResult. The native parent is optional; an owner must be live, on the UI thread, and unblocked by another modal dialog.

| Convenience method | Behavior |
| --- | --- |
| Information(title, message, owner = null) | Information icon, Ok preset |
| Warning(title, message, owner = null) | Warning icon, Ok preset |
| Error(title, message, owner = null) | Error icon, Ok preset |
| Confirm(title, message, owner = null) | Information icon, YesNo; default and Escape both No |

Each returns DialogResult. These calls block the UI thread until the native prompt is dismissed. They pause the managed application frame loop. They are not Task-returning async wrappers.

Before showing an owned prompt, framework interaction is canceled; prior widget focus is restored afterward when the owner remains alive/not closing and the widget is still eligible. A blocked owner is rejected with InvalidOperationException; use the active custom dialog as native owner.

Native failure/unsupported presentation throws InvalidOperationException with SDL error text. A parentless call may occur on the main thread before Run, subject to native library/platform availability. Native resource/text storage is released on success or failure.

Custom ShowDialog/ShowDialogAsync methods are documented on [SwealiumWindow](windowing.md#lifecycle-and-dialogs). They use Swealium rendering and keep independent windows responsive. Do not treat them as equivalent to the blocking native MessageBox service.

