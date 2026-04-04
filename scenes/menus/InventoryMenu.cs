using System.Collections.Generic;
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

	private static readonly Color TabActiveColor  = new(1.0f, 0.85f, 0.1f);
	private static readonly Color TabInactiveColor = new(0.55f, 0.55f, 0.55f);

	private Label           _emptyLabel    = null!;
	private VBoxContainer   _itemRows      = null!;
	private Label           _feedbackLabel = null!;
	private Button          _backButton    = null!;
	private Label           _descLabel     = null!;
	private HBoxContainer   _tabRow        = null!;
	private ItemType?       _activeFilter;

	public override void _Ready()
	{
		Layer   = 51;
		Visible = false;

		_emptyLabel    = GetNode<Label>("Overlay/Panel/VBox/EmptyLabel");
		_itemRows      = GetNode<VBoxContainer>("Overlay/Panel/VBox/ItemRows");
		_feedbackLabel = GetNode<Label>("Overlay/Panel/VBox/FeedbackLabel");
		_backButton    = GetNode<Button>("Overlay/Panel/VBox/BackButton");

		var vbox = GetNode<VBoxContainer>("Overlay/Panel/VBox");

		// Category tabs inserted at the top (after any existing title)
		_tabRow = new HBoxContainer();
		_tabRow.AddThemeConstantOverride("separation", 4);
		vbox.AddChild(_tabRow);
		vbox.MoveChild(_tabRow, 0);
		BuildCategoryTabs();

		// Description area inserted before FeedbackLabel
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

	private void BuildCategoryTabs()
	{
		AddTab("ALL", null);
		AddTab("CONSUMABLE", ItemType.Consumable);
		AddTab("INGREDIENT", ItemType.Ingredient);
		AddTab("KEY ITEM", ItemType.KeyItem);
	}

	private void AddTab(string label, ItemType? filter)
	{
		var btn = new Button
		{
			Text = label,
			ToggleMode = true,
			ButtonPressed = filter == _activeFilter,
		};
		btn.AddThemeFontSizeOverride("font_size", 9);
		btn.Modulate = filter == _activeFilter ? TabActiveColor : TabInactiveColor;
		btn.Pressed += () =>
		{
			_activeFilter = filter;
			UpdateTabVisuals();
			Refresh();
		};
		_tabRow.AddChild(btn);
	}

	private void UpdateTabVisuals()
	{
		int idx = 0;
		ItemType?[] filters = [null, ItemType.Consumable, ItemType.Ingredient, ItemType.KeyItem];
		foreach (var child in _tabRow.GetChildren())
		{
			if (child is Button btn && idx < filters.Length)
			{
				btn.ButtonPressed = filters[idx] == _activeFilter;
				btn.Modulate = filters[idx] == _activeFilter ? TabActiveColor : TabInactiveColor;
				idx++;
			}
		}
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

		var gm       = GameManager.Instance;
		var paths    = gm.InventoryItemPaths;
		var dynItems = gm.DynamicEquipmentInventory;

		bool focusGrabbed = false;
		int  itemCount    = 0;

		// Stack items by path and count duplicates
		var stacked = CountItems(paths);

		foreach (var (path, count) in stacked)
		{
			if (!ResourceLoader.Exists(path)) continue;
			var resource = GD.Load<Resource>(path);
			if (resource is not ItemData item) continue;

			// Apply category filter
			if (_activeFilter.HasValue && item.Type != _activeFilter.Value) continue;

			var row = BuildItemRow(item, path, count);
			_itemRows.AddChild(row);
			itemCount++;

			if (!focusGrabbed)
			{
				var btn = row.GetNodeOrNull<Button>("UseButton") ?? row.GetNodeOrNull<Button>("InfoButton");
				btn?.CallDeferred(Control.MethodName.GrabFocus);
				focusGrabbed = true;
			}
		}

		// Equipment section (only in ALL tab)
		if (!_activeFilter.HasValue)
		{
			var ownedEquip    = gm.OwnedEquipmentPaths;
			var equippedPaths = gm.EquippedItemPaths;
			bool hasAnyEquip  = ownedEquip.Count > 0 || equippedPaths.Count > 0 || dynItems.Count > 0;

			if (hasAnyEquip)
			{
				if (itemCount > 0)
					_itemRows.AddChild(new HSeparator());

				var eqHeader = new Label { Text = "— EQUIPMENT —" };
				eqHeader.HorizontalAlignment = HorizontalAlignment.Center;
				eqHeader.AddThemeFontSizeOverride("font_size", 9);
				eqHeader.AddThemeColorOverride("font_color", new Color(0.6f, 0.7f, 1f));
				_itemRows.AddChild(eqHeader);

				foreach (var kv in equippedPaths)
				{
					if (string.IsNullOrEmpty(kv.Value) || !ResourceLoader.Exists(kv.Value)) continue;
					var eq = GD.Load<EquipmentData>(kv.Value);
					if (eq == null) continue;
					_itemRows.AddChild(BuildStaticEquipRow(eq, kv.Key.ToString(), equipped: true));
					itemCount++;
				}

				foreach (var eqPath in ownedEquip)
				{
					if (!ResourceLoader.Exists(eqPath)) continue;
					var eq = GD.Load<EquipmentData>(eqPath);
					if (eq == null) continue;
					_itemRows.AddChild(BuildStaticEquipRow(eq, eq.Slot.ToString(), equipped: false));
					itemCount++;
				}

				foreach (var dynItem in dynItems)
				{
					bool equipped = gm.EquippedDynamicItemIds.ContainsValue(dynItem.Id);
					var row = BuildDynEquipRow(dynItem, equipped);
					_itemRows.AddChild(row);
					itemCount++;

					if (!focusGrabbed)
					{
						row.GetNodeOrNull<Button>("EquipHint")?.CallDeferred(Control.MethodName.GrabFocus);
						focusGrabbed = true;
					}
				}
			}
		}

		_emptyLabel.Visible = itemCount == 0;
		if (!focusGrabbed)
			_backButton.GrabFocus();
	}

	private static Dictionary<string, int> CountItems(Godot.Collections.Array<string> paths)
	{
		var counts = new Dictionary<string, int>();
		foreach (var p in paths)
			counts[p] = counts.GetValueOrDefault(p) + 1;
		return counts;
	}

	private HBoxContainer BuildItemRow(ItemData item, string path, int count)
	{
		var row = new HBoxContainer();

		string nameText = count > 1 ? $"{item.DisplayName} x{count}" : item.DisplayName;
		var nameLabel = new Label { Text = nameText };
		nameLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		nameLabel.AddThemeFontSizeOverride("font_size", 10);

		// Type-specific label color
		Color typeColor = item.Type switch
		{
			ItemType.Ingredient => new Color(0.6f, 0.85f, 1f),
			ItemType.KeyItem    => new Color(1f, 0.85f, 0.1f),
			ItemType.Repel      => new Color(0.7f, 0.9f, 0.5f),
			_                   => Colors.White,
		};
		nameLabel.AddThemeColorOverride("font_color", typeColor);

		var hpLabel = new Label();
		hpLabel.Text = item.HealAmount > 0 ? $"+{item.HealAmount} HP" : "";
		hpLabel.AddThemeFontSizeOverride("font_size", 10);
		hpLabel.AddThemeColorOverride("font_color", new Color(0.5f, 1f, 0.5f));

		row.AddChild(nameLabel);
		row.AddChild(hpLabel);

		if (item.Type == ItemType.Consumable || item.Type == ItemType.Repel)
		{
			var useButton = new Button { Text = "USE", Name = "UseButton" };
			useButton.Disabled = !ItemLogic.CanUseItem(
				item.HealAmount,
				GameManager.Instance.PlayerStats.CurrentHp,
				GameManager.Instance.PlayerStats.MaxHp);
			useButton.Pressed      += () => OnUseItem(item, path);
			useButton.FocusEntered += () => ShowDesc(item);
			row.AddChild(useButton);
		}
		else
		{
			// Ingredient / KeyItem — info-only button for focus/description
			var infoBtn = new Button { Text = "INFO", Name = "InfoButton", Disabled = false };
			infoBtn.AddThemeFontSizeOverride("font_size", 9);
			infoBtn.FocusEntered += () => ShowDesc(item);
			infoBtn.Pressed     += () => ShowDesc(item);
			row.AddChild(infoBtn);
		}

		return row;
	}

	private HBoxContainer BuildStaticEquipRow(EquipmentData eq, string slotText, bool equipped)
	{
		var row = new HBoxContainer();

		var nameLabel = new Label { Text = eq.DisplayName };
		nameLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		nameLabel.AddThemeFontSizeOverride("font_size", 10);

		var slotLabel = new Label { Text = slotText };
		slotLabel.AddThemeFontSizeOverride("font_size", 9);
		slotLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));

		var hint = new Button
		{
			Text     = equipped ? "EQUIPPED" : "—",
			Disabled = true,
		};
		hint.AddThemeFontSizeOverride("font_size", 9);

		row.AddChild(nameLabel);
		row.AddChild(slotLabel);
		row.AddChild(hint);
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
