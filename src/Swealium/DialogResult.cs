namespace Swealium;

public enum DialogResult { None, Ok, Cancel, Yes, No, Retry, Abort, Ignore }

public sealed class WindowClosingEvent
{
    public bool Cancel { get; set; }
}
