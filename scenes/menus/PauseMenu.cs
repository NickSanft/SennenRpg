using Godot;
using SennenRpg.Autoloads;

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
	private Button _itemsButton     = null!;
	private Button _equipmentButton = null!;
	private Button _mainMenuButton  = null!;
	private bool   _transitioning   = false;

	private InventoryMenu?  _inventoryMenu;
	private EquipmentMenu?  _equipmentMenu;

	public override void _Ready()
	{
		Layer   = 50; // Above GameHud (2), below SceneTransition (100)
		Visible = false;

		_resumeButton    = GetNode<Button>("Overlay/Panel/VBox/ResumeButton");
		_saveButton      = GetNode<Button>("Overlay/Panel/VBox/SaveButton");
		_itemsButton     = GetNode<Button>("Overlay/Panel/VBox/ItemsButton");
		_equipmentButton = GetNode<Button>("Overlay/Panel/VBox/EquipmentButton");
		_mainMenuButton  = GetNode<Button>("Overlay/Panel/VBox/MainMenuButton");

		_resumeButton.Pressed    += Resume;
		_saveButton.Pressed      += OnSavePressed;
		_itemsButton.Pressed     += OnItemsPressed;
		_equipmentButton.Pressed += OnEquipmentPressed;
		_mainMenuButton.Pressed  += OnMainMenuPressed;

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

	private void OnMainMenuPressed()
	{
		if (_transitioning) return;
		_transitioning = true;
		Visible = false;
		_ = SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
	}
}
