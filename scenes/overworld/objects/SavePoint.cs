using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;
using SennenRpg.Scenes.Hud;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Save point star — press interact to open the native save confirmation dialog.
/// No Dialogic dependency; uses SaveConfirmDialog (a simple CanvasLayer popup).
/// </summary>
public partial class SavePoint : Area2D, IInteractable
{
	[Export] public string SavePointId { get; set; } = "default";

	private InteractPromptBubble? _prompt;
	private bool                   _dialogOpen = false;

	public override void _Ready()
	{
		AddToGroup("interactable");

		_prompt = new InteractPromptBubble("[Z] Save");
		_prompt.Position = new Vector2(0, -24);
		AddChild(_prompt);
	}

	public void ShowPrompt() => _prompt?.ShowBubble();
	public void HidePrompt() => _prompt?.HideBubble();
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
		SpawnSaveToast();
		Close();
	}

	private void SpawnSaveToast()
	{
		const string path = "res://scenes/hud/AreaNameLabel.tscn";
		if (!ResourceLoader.Exists(path)) return;
		var toast = GD.Load<PackedScene>(path).Instantiate<AreaNameLabel>();
		GetTree().CurrentScene.AddChild(toast);
		toast.Show("Game Saved");
	}

	private void OnCancelled() => Close();

	private void Close()
	{
		_dialogOpen = false;
		GameManager.Instance.SetState(GameState.Overworld);
	}
}
