using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Pause overlay shown when the player presses ESC (menu action) during the overworld.
/// Instantiated by OverworldBase so every map gets it automatically.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
	private Button _resumeButton   = null!;
	private Button _saveButton     = null!;
	private Button _mainMenuButton = null!;
	private bool   _transitioning  = false;

	public override void _Ready()
	{
		Layer   = 50; // Above GameHud (2), below SceneTransition (100)
		Visible = false;

		_resumeButton   = GetNode<Button>("Overlay/Panel/VBox/ResumeButton");
		_saveButton     = GetNode<Button>("Overlay/Panel/VBox/SaveButton");
		_mainMenuButton = GetNode<Button>("Overlay/Panel/VBox/MainMenuButton");

		_resumeButton.Pressed   += Resume;
		_saveButton.Pressed     += OnSavePressed;
		_mainMenuButton.Pressed += OnMainMenuPressed;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_transitioning) return;

		if (@event.IsActionPressed("menu"))
		{
			if (!Visible && GameManager.Instance.CurrentState == GameState.Overworld)
				Open();
			else if (Visible)
				Resume();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Open()
	{
		Visible = true;
		GameManager.Instance.SetState(GameState.Paused);
		_resumeButton.GrabFocus();
		GD.Print("[PauseMenu] Opened.");
	}

	private void Resume()
	{
		Visible = false;
		GameManager.Instance.SetState(GameState.Overworld);
		GD.Print("[PauseMenu] Resumed.");
	}

	private void OnSavePressed()
	{
		SaveManager.Instance.SaveGame();
		_saveButton.Text     = "Saved!";
		_saveButton.Disabled = true;
		GetTree().CreateTimer(1.2f).Timeout += () =>
		{
			if (IsInstanceValid(_saveButton))
			{
				_saveButton.Text     = "SAVE";
				_saveButton.Disabled = false;
			}
		};
	}

	private void OnMainMenuPressed()
	{
		if (_transitioning) return;
		_transitioning = true;
		Visible = false;
		_ = SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
	}
}
