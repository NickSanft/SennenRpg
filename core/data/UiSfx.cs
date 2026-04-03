namespace SennenRpg.Core.Data;

/// <summary>
/// Path constants for UI sound effects. All files are optional —
/// AudioManager.PlaySfx silently skips missing resources.
/// </summary>
public static class UiSfx
{
	public const string Confirm = "res://assets/audio/sfx/ui_confirm.wav";
	public const string Cancel  = "res://assets/audio/sfx/ui_cancel.wav";
	public const string Cursor  = "res://assets/audio/sfx/ui_cursor.wav";
	public const string Error   = "res://assets/audio/sfx/ui_error.wav";
}
