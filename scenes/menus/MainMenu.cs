using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Title screen. "New Game" resets state and goes to the first map.
/// "Continue" is enabled only when a save file exists.
/// </summary>
public partial class MainMenu : Node2D
{
	private Button _newGameButton  = null!;
	private Button _continueButton = null!;
	private Button _quitButton     = null!;
	private bool   _transitioning  = false;

	public override void _Ready()
	{
		_newGameButton  = GetNode<Button>("UI/Center/VBox/NewGameButton");
		_continueButton = GetNode<Button>("UI/Center/VBox/ContinueButton");
		_quitButton     = GetNode<Button>("UI/Center/VBox/QuitButton");

		_continueButton.Disabled = !SaveManager.Instance.HasSave();

		_newGameButton.Pressed  += OnNewGamePressed;
		_continueButton.Pressed += OnContinuePressed;
		_quitButton.Pressed     += () => GetTree().Quit();
	}

	private void OnNewGamePressed()
	{
		if (_transitioning) return;
		_transitioning = true;
		GameManager.Instance.ResetForNewGame();
		_ = SceneTransition.Instance.GoToAsync("res://scenes/overworld/TestRoom.tscn");
	}

	private void OnContinuePressed()
	{
		if (_transitioning) return;
		var data = SaveManager.Instance.LoadGame();
		if (data == null)
		{
			GD.PushWarning("[MainMenu] LoadGame returned null — no valid save.");
			return;
		}
		_transitioning = true;
		SaveManager.Instance.ApplyLoadedData(data);
		string map = string.IsNullOrEmpty(data.LastMapPath)
			? "res://scenes/overworld/TestRoom.tscn"
			: data.LastMapPath;
		_ = SceneTransition.Instance.GoToAsync(map);
	}
}
