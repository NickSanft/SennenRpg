using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Save point star — press interact to open the save dialog.
/// Uses a Dialogic timeline with a Choice block. The "Save" choice fires
/// Signal Event "flag:confirmed_save", which DialogicBridge converts into a flag.
/// OnSaveDialogEnded() checks that flag and calls SaveManager if set.
/// </summary>
public partial class SavePoint : Area2D, IInteractable
{
	[Export] public string SavePointId { get; set; } = "default";
	[Export] public string TimelinePath { get; set; } = "res://dialog/timelines/save_point_prompt.dtl";

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

	private Label? _promptLabel;

	public void ShowPrompt() { if (_promptLabel != null) _promptLabel.Visible = true; }
	public void HidePrompt() { if (_promptLabel != null) _promptLabel.Visible = false; }

	public void Interact(Node player)
	{
		if (DialogicBridge.Instance.IsRunning()) return;

		GameManager.Instance.SetLastSavePoint(SavePointId);
		GameManager.Instance.SetState(GameState.Dialog);

		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnSaveDialogEnded));

		DialogicBridge.Instance.StartTimeline(TimelinePath);
	}

	public string GetInteractPrompt() => "Save";

	private void OnSaveDialogEnded()
	{
		bool confirmed = GameManager.Instance.GetFlag("confirmed_save");
		if (confirmed)
		{
			GameManager.Instance.SetFlag("confirmed_save", false); // reset for next time
			SaveManager.Instance.SaveGame();
			GD.Print($"[SavePoint] Game saved at '{SavePointId}'.");
		}
		GameManager.Instance.SetState(GameState.Overworld);
	}
}
