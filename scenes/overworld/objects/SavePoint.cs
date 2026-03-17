using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Save point star — press interact to open the native save confirmation dialog.
/// No Dialogic dependency; uses SaveConfirmDialog (a simple CanvasLayer popup).
/// </summary>
public partial class SavePoint : Area2D, IInteractable
{
	[Export] public string SavePointId { get; set; } = "default";

	private Label? _promptLabel;
	private bool   _dialogOpen = false;

	public override void _Ready()
	{
		AddToGroup("interactable");

		_promptLabel = new Label();
		_promptLabel.Text = "[Z] Save";
		_promptLabel.Position = new Vector2(-20, -24);
		_promptLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		_promptLabel.AddThemeFontSizeOverride("font_size", 8);
		_promptLabel.Visible = false;
		AddChild(_promptLabel);
	}

	public void ShowPrompt() { if (_promptLabel != null) _promptLabel.Visible = true; }
	public void HidePrompt() { if (_promptLabel != null) _promptLabel.Visible = false; }
	public string GetInteractPrompt() => "Save";

	public void Interact(Node player)
	{
		if (_dialogOpen) return;
		_dialogOpen = true;

		GameManager.Instance.SetLastSavePoint(SavePointId);
		GameManager.Instance.SetState(GameState.Paused);

		var dialog = new SaveConfirmDialog();
		dialog.Confirmed  += OnConfirmed;
		dialog.Cancelled  += OnCancelled;
		AddChild(dialog);
	}

	private void OnConfirmed()
	{
		SaveManager.Instance.SaveGame();
		GD.Print($"[SavePoint] Game saved at '{SavePointId}'.");
		Close();
	}

	private void OnCancelled() => Close();

	private void Close()
	{
		_dialogOpen = false;
		GameManager.Instance.SetState(GameState.Overworld);
	}
}
