using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Title screen. Both New Game and Continue route through SaveSlotMenu so the
/// player can choose which of the three save slots to use.
/// </summary>
public partial class MainMenu : Node2D
{
	private Button _newGameButton  = null!;
	private Button _continueButton = null!;
	private Button _creditsButton  = null!;
	private Button _quitButton     = null!;
	private bool   _transitioning  = false;

	public override void _Ready()
	{
		_newGameButton  = GetNode<Button>("UI/Center/VBox/NewGameButton");
		_continueButton = GetNode<Button>("UI/Center/VBox/ContinueButton");
		_creditsButton  = GetNode<Button>("UI/Center/VBox/CreditsButton");
		_quitButton     = GetNode<Button>("UI/Center/VBox/QuitButton");

		_continueButton.Disabled = !SaveManager.Instance.HasAnySave();

		_newGameButton.Pressed  += OnNewGamePressed;
		_continueButton.Pressed += OnContinuePressed;
		_creditsButton.Pressed  += OnCreditsPressed;
		_quitButton.Pressed     += () => GetTree().Quit();

		// Apply SNES theme to all buttons and title
		UiTheme.ApplyToAllButtons(this);
		var titleLabel = GetNodeOrNull<Label>("UI/Center/VBox/Title");
		if (titleLabel != null)
		{
			var font = UiTheme.LoadPixelFont();
			if (font != null) titleLabel.AddThemeFontOverride("font", font);
			titleLabel.AddThemeColorOverride("font_color", UiTheme.Gold);
		}
	}

	private void OnNewGamePressed()
	{
		if (_transitioning) return;
		_transitioning = true;
		SaveSlotMenu.PendingMode = SaveSlotMenu.MenuMode.NewGame;
		_ = SceneTransition.Instance.GoToAsync("res://scenes/menus/SaveSlotMenu.tscn");
	}

	private void OnContinuePressed()
	{
		if (_transitioning) return;
		_transitioning = true;
		SaveSlotMenu.PendingMode = SaveSlotMenu.MenuMode.Continue;
		_ = SceneTransition.Instance.GoToAsync("res://scenes/menus/SaveSlotMenu.tscn");
	}

	private void OnCreditsPressed()
	{
		if (_transitioning) return;
		_transitioning = true;
		_ = SceneTransition.Instance.GoToAsync("res://scenes/menus/CreditsMenu.tscn");
	}
}
