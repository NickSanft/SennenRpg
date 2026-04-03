namespace SennenRpg.Core.Data;

/// <summary>
/// Path constants for UI sound effects. All files are optional —
/// AudioManager.PlaySfx silently skips missing resources.
/// </summary>
public static class UiSfx
{
    public const string Confirm = "res://assets/audio/sfx/ui_confirm.ogg";
    public const string Cancel  = "res://assets/audio/sfx/ui_cancel.ogg";
    public const string Cursor  = "res://assets/audio/sfx/ui_cursor.ogg";
    public const string Error   = "res://assets/audio/sfx/ui_error.ogg";
}
