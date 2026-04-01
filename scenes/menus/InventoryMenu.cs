using System.Linq;
using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Full-screen inventory overlay. Opened by PauseMenu when the player selects ITEMS.
/// Lists every item in GameManager.InventoryItemPaths; healing items can be used in-place.
/// Emits Closed when the player backs out, allowing PauseMenu to reappear.
/// </summary>
public partial class InventoryMenu : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	private Label           _emptyLabel    = null!;
	private VBoxContainer   _itemRows      = null!;
	private Label           _feedbackLabel = null!;
	private Button          _backButton    = null!;
	private Label           _descLabel     = null!;

	public override void _Ready()
	{
		Layer   = 51; // above PauseMenu (50), below SceneTransition (100)
		Visible = false;

		_emptyLabel    = GetNode<Label>("Overlay/Panel/VBox/EmptyLabel");
		_itemRows      = GetNode<VBoxContainer>("Overlay/Panel/VBox/ItemRows");
		_feedbackLabel = GetNode<Label>("Overlay/Panel/VBox/FeedbackLabel");
		_backButton    = GetNode<Button>("Overlay/Panel/VBox/BackButton");

		// Description area inserted before FeedbackLabel
		var vbox = GetNode<VBoxContainer>("Overlay/Panel/VBox");
		_descLabel = new Label
		{
			Text                = "",
			AutowrapMode        = TextServer.AutowrapMode.Word,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_descLabel.AddThemeFontSizeOverride("font_size", 8);
		_descLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
		_descLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		vbox.AddChild(_descLabel);
		vbox.MoveChild(_descLabel, _feedbackLabel.GetIndex());

		_backButton.Pressed += Close;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible) return;
		if (@event.IsActionPressed("menu") || @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	/// <summary>Populates item list and makes the menu visible.</summary>
	public void Open()
	{
		Refresh();
		Visible = true;
	}

	private void Close()
	{
		Visible = false;
		EmitSignal(SignalName.Closed);
	}

	private void Refresh()
	{
		foreach (var child in _itemRows.GetChildren())
			child.QueueFree();

		_feedbackLabel.Text = "";
		_descLabel.Text     = "";

		var gm    = GameManager.Instance;
		var paths = gm.InventoryItemPaths;
		var dynItems = gm.DynamicEquipmentInventory;

		_emptyLabel.Visible = paths.Count == 0 && dynItems.Count == 0;

		bool focusGrabbed = false;
		foreach (var path in paths)
		{
			if (!ResourceLoader.Exists(path)) continue;
			var item = GD.Load<ItemData>(path);
			if (item == null) continue;

			var row = BuildItemRow(item, path);
			_itemRows.AddChild(row);

			if (!focusGrabbed)
			{
				row.GetNodeOrNull<Button>("UseButton")?.GrabFocus();
				focusGrabbed = true;
			}
		}

		if (dynItems.Count > 0)
		{
			if (paths.Count > 0)
				_itemRows.AddChild(new HSeparator());

			var header = new Label { Text = "— EQUIPMENT (Lily's Forge) —" };
			header.HorizontalAlignment = HorizontalAlignment.Center;
			header.AddThemeFontSizeOverride("font_size", 9);
			header.AddThemeColorOverride("font_color", new Color(0.8f, 0.7f, 0.3f));
			_itemRows.AddChild(header);

			foreach (var dynItem in dynItems)
			{
				bool equipped = gm.EquippedDynamicItemIds.ContainsValue(dynItem.Id);
				var row = BuildDynEquipRow(dynItem, equipped);
				_itemRows.AddChild(row);

				if (!focusGrabbed)
				{
					row.GetNodeOrNull<Button>("EquipHint")?.GrabFocus();
					focusGrabbed = true;
				}
			}
		}

		if (!focusGrabbed)
			_backButton.GrabFocus();
	}

	private HBoxContainer BuildItemRow(ItemData item, string path)
	{
		var row = new HBoxContainer();

		var nameLabel = new Label { Text = item.DisplayName };
		nameLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		nameLabel.AddThemeFontSizeOverride("font_size", 10);

		var hpLabel = new Label();
		hpLabel.Text = item.HealAmount > 0 ? $"+{item.HealAmount} HP" : "";
		hpLabel.AddThemeFontSizeOverride("font_size", 10);
		hpLabel.AddThemeColorOverride("font_color", new Color(0.5f, 1f, 0.5f));

		var useButton = new Button { Text = "USE", Name = "UseButton" };
		useButton.Disabled = !ItemLogic.CanUseItem(
			item.HealAmount,
			GameManager.Instance.PlayerStats.CurrentHp,
			GameManager.Instance.PlayerStats.MaxHp);
		useButton.Pressed      += () => OnUseItem(item, path);
		useButton.FocusEntered += () => ShowDesc(item);

		row.AddChild(nameLabel);
		row.AddChild(hpLabel);
		row.AddChild(useButton);
		return row;
	}

	private HBoxContainer BuildDynEquipRow(DynamicEquipmentSave dynItem, bool equipped)
	{
		var row = new HBoxContainer();

		var nameLabel = new Label { Text = dynItem.DisplayName };
		nameLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		nameLabel.AddThemeFontSizeOverride("font_size", 10);

		var slotLabel = new Label { Text = dynItem.Slot.ToString() };
		slotLabel.AddThemeFontSizeOverride("font_size", 9);
		slotLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));

		var hint = new Button
		{
			Text     = equipped ? "EQUIPPED" : "Equip in Equipment menu",
			Name     = "EquipHint",
			Disabled = true,
		};
		hint.AddThemeFontSizeOverride("font_size", 9);
		hint.FocusEntered += () => _descLabel.Text = dynItem.Description;

		row.AddChild(nameLabel);
		row.AddChild(slotLabel);
		row.AddChild(hint);
		return row;
	}

	private void ShowDesc(ItemData item)
	{
		_descLabel.Text = !string.IsNullOrEmpty(item.Description) ? item.Description : item.DisplayName;
	}

	private void OnUseItem(ItemData item, string path)
	{
		int actual = ItemLogic.ActualHeal(
			item.HealAmount,
			GameManager.Instance.PlayerStats.CurrentHp,
			GameManager.Instance.PlayerStats.MaxHp);

		GameManager.Instance.HealPlayer(item.HealAmount);
		GameManager.Instance.RemoveItem(path);

		ShowFeedback($"+{actual} HP");
		Refresh();
	}

	private void ShowFeedback(string text)
	{
		_feedbackLabel.Text = text;
		GetTree().CreateTimer(1.5f).Timeout += () =>
		{
			if (IsInstanceValid(_feedbackLabel))
				_feedbackLabel.Text = "";
		};
	}
}
