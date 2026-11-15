using Swealium.Input;
using Swealium.Native;
namespace Swealium;

public partial class SwealiumWindow
{
    /// <summary>Default cursor used when no hovered/captured widget supplies one.</summary>
    public Cursor Cursor {get{VerifyAccess();return Input.DefaultCursor;}set{VerifyAccess();Input.DefaultCursor=value;SyncCursor();}}
    public Cursor EffectiveCursor {get{VerifyAccess();return Input.CurrentCursor;}}
    private void SyncCursor()
    {
        if(window!=0&&Sdl.SDL_GetMouseFocus()==window)SdlCursorManager.Apply(window,Input.CurrentCursor);
    }
}
