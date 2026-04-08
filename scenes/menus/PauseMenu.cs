using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Pause overlay shown when the player presses ESC (menu action) during the overworld.
/// Instantiated by OverworldBase so every map gets it automatically.
/// Manages InventoryMenu as a sibling — opening it hides PauseMenu until the player backs out.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
	private Button _resumeButton    = null!;
	private Button _saveButton      = null!;
	private Button _settingsButton  = null!;
	private Button _itemsButton     = null!;
	private Button _cookButton      = null!;
	private Button _forageryButton  = null!;
	private Button _bestiaryButton  = null!;
	private Button _partyButton     = null!;
	private Button _equipmentButton = null!;
	private Button _spellsButton    = null!;
	private Button _statsButton     = null!;
	private Button _mainMenuButton  = null!;
	private bool   _transitioning   = false;

	private InventoryMenu?  _inventoryMenu;
	private CookingMenu?    _cookingMenu;
	private ForageryMenu?   _forageryMenu;
	private BestiaryMenu?   _bestiaryMenu;
	private PartyMenu?      _partyMenu;
	private EquipmentMenu?  _equipmentMenu;
	private SpellsMenu?     _spellsMenu;
	private StatsMenu?      _statsMenu;
	private SettingsMenu?   _settingsMenu;

	public override void _Ready()
	{
		Layer   = 50; // Above GameHud (2), below SceneTransition (100)
		Visible = false;

		_resumeButton    = GetNode<Button>("Overlay/Panel/VBox/ResumeButton");
		_saveButton      = GetNode<Button>("Overlay/Panel/VBox/SaveButton");
		_settingsButton  = GetNode<Button>("Overlay/Panel/VBox/SettingsButton");
		_itemsButton     = GetNode<Button>("Overlay/Panel/VBox/ItemsButton");
		_cookButton      = GetNode<Button>("Overlay/Panel/VBox/CookButton");
		_forageryButton  = GetNode<Button>("Overlay/Panel/VBox/ForageryButton");
		_bestiaryButton  = GetNode<Button>("Overlay/Panel/VBox/BestiaryButton");
		_partyButton     = GetNode<Button>("Overlay/Panel/VBox/PartyButton");
		_equipmentButton = GetNode<Button>("Overlay/Panel/VBox/EquipmentButton");
		_spellsButton    = GetNode<Button>("Overlay/Panel/VBox/SpellsButton");
		_statsButton     = GetNode<Button>("Overlay/Panel/VBox/StatsButton");
		_mainMenuButton  = GetNode<Button>("Overlay/Panel/VBox/MainMenuButton");

		_resumeButton.Pressed    += Resume;
		_saveButton.Pressed      += OnSavePressed;
		_settingsButton.Pressed  += OnSettingsPressed;
		_itemsButton.Pressed     += OnItemsPressed;
		_cookButton.Pressed      += OnCookPressed;
		_forageryButton.Pressed  += OnForageryPressed;
		_bestiaryButton.Pressed  += OnBestiaryPressed;
		_partyButton.Pressed     += OnPartyPressed;
		_equipmentButton.Pressed += OnEquipmentPressed;
		_spellsButton.Pressed    += OnSpellsPressed;
		_statsButton.Pressed     += OnStatsPressed;
		_mainMenuButton.Pressed  += OnMainMenuPressed;

		// Apply SNES theme to all buttons
		UiTheme.ApplyToAllButtons(this);
		UiTheme.ApplyPixelFontToAll(this);

		// Cursor SFX on focus change
		foreach (var btn in new[] { _resumeButton, _saveButton, _settingsButton,
			_itemsButton, _cookButton, _forageryButton, _bestiaryButton, _partyButton, _equipmentButton, _spellsButton, _statsButton, _mainMenuButton })
			btn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);

		// Instantiate InventoryMenu as a sibling so its visibility is independent of PauseMenu
		const string invPath = "res://scenes/menus/InventoryMenu.tscn";
		if (ResourceLoader.Exists(invPath))
		{
			_inventoryMenu = GD.Load<PackedScene>(invPath).Instantiate<InventoryMenu>();
			_inventoryMenu.Closed += OnInventoryClosed;
			AddSibling(_inventoryMenu);
		}
		else
		{
			GD.PushWarning("[PauseMenu] InventoryMenu.tscn not found — ITEMS button will be disabled.");
			_itemsButton.Disabled = true;
		}

		const string cookPath = "res://scenes/menus/CookingMenu.tscn";
		if (ResourceLoader.Exists(cookPath))
		{
			_cookingMenu = GD.Load<PackedScene>(cookPath).Instantiate<CookingMenu>();
			_cookingMenu.Closed += OnCookingClosed;
			AddSibling(_cookingMenu);
		}
		else
		{
			GD.PushWarning("[PauseMenu] CookingMenu.tscn not found — COOK button will be disabled.");
			_cookButton.Disabled = true;
		}

		const string forageryPath = "res://scenes/menus/ForageryMenu.tscn";
		if (ResourceLoader.Exists(forageryPath))
		{
			_forageryMenu = GD.Load<PackedScene>(forageryPath).Instantiate<ForageryMenu>();
			_forageryMenu.Closed += OnForageryClosed;
			AddSibling(_forageryMenu);
		}
		else
		{
			GD.PushWarning("[PauseMenu] ForageryMenu.tscn not found — FORAGERY button will be disabled.");
			_forageryButton.Disabled = true;
		}

		const string bestiaryPath = "res://scenes/menus/BestiaryMenu.tscn";
		if (ResourceLoader.Exists(bestiaryPath))
		{
			_bestiaryMenu = GD.Load<PackedScene>(bestiaryPath).Instantiate<BestiaryMenu>();
			_bestiaryMenu.Closed += OnBestiaryClosed;
			AddSibling(_bestiaryMenu);
		}
		else
		{
			GD.PushWarning("[PauseMenu] BestiaryMenu.tscn not found — BESTIARY button will be disabled.");
			_bestiaryButton.Disabled = true;
		}

		const string partyPath = "res://scenes/menus/PartyMenu.tscn";
		if (ResourceLoader.Exists(partyPath))
		{
			_partyMenu = GD.Load<PackedScene>(partyPath).Instantiate<PartyMenu>();
			_partyMenu.Closed += OnPartyClosed;
			AddSibling(_partyMenu);
		}
		else
		{
			GD.PushWarning("[PauseMenu] PartyMenu.tscn not found — PARTY button will be disabled.");
			_partyButton.Disabled = true;
		}

		const string eqPath = "res://scenes/menus/EquipmentMenu.tscn";
		if (ResourceLoader.Exists(eqPath))
		{
			_equipmentMenu = GD.Load<PackedScene>(eqPath).Instantiate<EquipmentMenu>();
			_equipmentMenu.Closed += OnEquipmentClosed;
			AddSibling(_equipmentMenu);
		}
		else
		{
			GD.PushWarning("[PauseMenu] EquipmentMenu.tscn not found — EQUIPMENT button will be disabled.");
			_equipmentButton.Disabled = true;
		}

		const string spellsPath = "res://scenes/menus/SpellsMenu.tscn";
		if (ResourceLoader.Exists(spellsPath))
		{
			_spellsMenu = GD.Load<PackedScene>(spellsPath).Instantiate<SpellsMenu>();
			_spellsMenu.Closed += OnSpellsClosed;
			AddSibling(_spellsMenu);
		}
		else
		{
			GD.PushWarning("[PauseMenu] SpellsMenu.tscn not found — SPELLS button will be disabled.");
			_spellsButton.Disabled = true;
		}

		const string statsPath = "res://scenes/menus/StatsMenu.tscn";
		if (ResourceLoader.Exists(statsPath))
		{
			_statsMenu = GD.Load<PackedScene>(statsPath).Instantiate<StatsMenu>();
			_statsMenu.Closed += OnStatsClosed;
			AddSibling(_statsMenu);
		}
		else
		{
			GD.PushWarning("[PauseMenu] StatsMenu.tscn not found — STATS button will be disabled.");
			_statsButton.Disabled = true;
		}

		const string settingsPath = "res://scenes/menus/SettingsMenu.tscn";
		if (ResourceLoader.Exists(settingsPath))
		{
			_settingsMenu = GD.Load<PackedScene>(settingsPath).Instantiate<SettingsMenu>();
			_settingsMenu.Closed += OnSettingsClosed;
			AddSibling(_settingsMenu);
		}
		else
		{
			GD.PushWarning("[PauseMenu] SettingsMenu.tscn not found — SETTINGS button will be disabled.");
			_settingsButton.Disabled = true;
		}
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
		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		Visible = true;
		GameManager.Instance.SetState(GameState.Paused);
		_resumeButton.GrabFocus();
		GD.Print("[PauseMenu] Opened.");
	}

	private void Resume()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
		Visible = false;
		GameManager.Instance.SetState(GameState.Overworld);
		GD.Print("[PauseMenu] Resumed.");
	}

	private void OnSavePressed()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
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

	private void OnSettingsPressed()
	{
		if (_settingsMenu == null) return;
		Visible = false;
		_settingsMenu.Open();
		GD.Print("[PauseMenu] Settings menu opened.");
	}

	private void OnSettingsClosed()
	{
		Visible = true;
		_settingsButton.GrabFocus();
		GD.Print("[PauseMenu] Settings menu closed, PauseMenu restored.");
	}

	private void OnItemsPressed()
	{
		if (_inventoryMenu == null) return;
		Visible = false; // hide PauseMenu while inventory is open
		_inventoryMenu.Open();
		GD.Print("[PauseMenu] Inventory opened.");
	}

	private void OnInventoryClosed()
	{
		// Re-show PauseMenu when the player backs out of inventory
		Visible = true;
		_itemsButton.GrabFocus();
		GD.Print("[PauseMenu] Inventory closed, PauseMenu restored.");
	}

	private void OnCookPressed()
	{
		if (_cookingMenu == null) return;
		Visible = false;
		_cookingMenu.Open();
		GD.Print("[PauseMenu] Cooking menu opened.");
	}

	private void OnCookingClosed()
	{
		Visible = true;
		_cookButton.GrabFocus();
		GD.Print("[PauseMenu] Cooking menu closed, PauseMenu restored.");
	}

	private void OnForageryPressed()
	{
		if (_forageryMenu == null) return;
		Visible = false;
		_forageryMenu.Open();
		GD.Print("[PauseMenu] Foragery menu opened.");
	}

	private void OnForageryClosed()
	{
		Visible = true;
		_forageryButton.GrabFocus();
		GD.Print("[PauseMenu] Foragery menu closed, PauseMenu restored.");
	}

	private void OnBestiaryPressed()
	{
		if (_bestiaryMenu == null) return;
		Visible = false;
		_bestiaryMenu.Open();
		GD.Print("[PauseMenu] Bestiary menu opened.");
	}

	private void OnBestiaryClosed()
	{
		Visible = true;
		_bestiaryButton.GrabFocus();
		GD.Print("[PauseMenu] Bestiary menu closed, PauseMenu restored.");
	}

	private void OnPartyPressed()
	{
		if (_partyMenu == null) return;
		Visible = false;
		_partyMenu.Open();
		GD.Print("[PauseMenu] Party menu opened.");
	}

	private void OnPartyClosed()
	{
		Visible = true;
		_partyButton.GrabFocus();
		GD.Print("[PauseMenu] Party menu closed, PauseMenu restored.");
	}

	private void OnEquipmentPressed()
	{
		if (_equipmentMenu == null) return;
		Visible = false;
		_equipmentMenu.Open();
		GD.Print("[PauseMenu] Equipment menu opened.");
	}

	private void OnEquipmentClosed()
	{
		Visible = true;
		_equipmentButton.GrabFocus();
		GD.Print("[PauseMenu] Equipment menu closed, PauseMenu restored.");
	}

	private void OnSpellsPressed()
	{
		if (_spellsMenu == null) return;
		Visible = false;
		_spellsMenu.Open();
		GD.Print("[PauseMenu] Spells menu opened.");
	}

	private void OnSpellsClosed()
	{
		Visible = true;
		_spellsButton.GrabFocus();
		GD.Print("[PauseMenu] Spells menu closed, PauseMenu restored.");
	}

	private void OnStatsPressed()
	{
		if (_statsMenu == null) return;
		Visible = false;
		_statsMenu.Open();
		GD.Print("[PauseMenu] Stats menu opened.");
	}

	private void OnStatsClosed()
	{
		Visible = true;
		_statsButton.GrabFocus();
		GD.Print("[PauseMenu] Stats menu closed, PauseMenu restored.");
	}

	private void OnMainMenuPressed()
	{
		if (_transitioning) return;
		_transitioning = true;
		Visible = false;
		_ = SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
	}
}
