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
		_backButton.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);

		// Apply SNES theme
		var overlay = GetNodeOrNull<ColorRect>("Overlay");
		if (overlay != null) overlay.Color = UiTheme.OverlayDim;
		var panel = GetNodeOrNull<PanelContainer>("Overlay/Panel");
		if (panel != null) UiTheme.ApplyPanelTheme(panel);
		UiTheme.ApplyToAllButtons(this);
		UiTheme.ApplyPixelFontToAll(this);
	}

	private void BuildCategoryTabs()
	{
		AddTab("ALL", null);
		AddTab("CONSUMABLE", ItemType.Consumable);
		AddTab("INGREDIENT", ItemType.Ingredient);
		AddTab("KEY ITEM", ItemType.KeyItem);
		AddTab("JUNK", ItemType.Junk);
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
		btn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
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
		ItemType?[] filters = [null, ItemType.Consumable, ItemType.Ingredient, ItemType.KeyItem, ItemType.Junk];
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
		AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
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

				// Sen — read static equipment from InventoryData (canonical for him).
				foreach (var kv in equippedPaths)
				{
					if (string.IsNullOrEmpty(kv.Value) || !ResourceLoader.Exists(kv.Value)) continue;
					var eq = GD.Load<EquipmentData>(kv.Value);
					if (eq == null) continue;
					_itemRows.AddChild(BuildStaticEquipRow(eq, kv.Key.ToString(),
						equipped: true, ownerName: gm.PlayerName));
					itemCount++;
				}

				// Lily / Rain / future recruits — read static equipment from each PartyMember.
				foreach (var member in gm.Party.Members)
				{
					if (member.MemberId == "sen") continue; // Sen handled above
					foreach (var kv in member.EquippedItemPaths)
					{
						if (string.IsNullOrEmpty(kv.Value) || !ResourceLoader.Exists(kv.Value)) continue;
						var eq = GD.Load<EquipmentData>(kv.Value);
						if (eq == null) continue;
						_itemRows.AddChild(BuildStaticEquipRow(eq, kv.Key,
							equipped: true, ownerName: member.DisplayName));
						itemCount++;
					}
				}

				foreach (var eqPath in ownedEquip)
				{
					if (!ResourceLoader.Exists(eqPath)) continue;
					var eq = GD.Load<EquipmentData>(eqPath);
					if (eq == null) continue;
					_itemRows.AddChild(BuildStaticEquipRow(eq, eq.Slot.ToString(),
						equipped: false, ownerName: ""));
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

		// Re-apply font to dynamically created rows
		UiTheme.ApplyPixelFontToAll(_itemRows);
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
			ItemType.Junk       => new Color(0.7f, 0.7f, 0.5f),
			_                   => Colors.White,
		};
		nameLabel.AddThemeColorOverride("font_color", typeColor);

		var hpLabel = new Label();
		if (item.Type == ItemType.Junk && item.SellValue > 0)
		{
			hpLabel.Text = $"{item.SellValue}G";
			hpLabel.AddThemeColorOverride("font_color", UiTheme.Gold);
		}
		else
		{
			hpLabel.Text = item.HealAmount > 0 ? $"+{item.HealAmount} HP" : "";
			hpLabel.AddThemeColorOverride("font_color", new Color(0.5f, 1f, 0.5f));
		}
		hpLabel.AddThemeFontSizeOverride("font_size", 10);

		row.AddChild(nameLabel);
		row.AddChild(hpLabel);

		if (item.Type == ItemType.Consumable || item.Type == ItemType.Repel)
		{
			var useButton = new Button { Text = "USE", Name = "UseButton" };
			useButton.Disabled = FindBestTarget(item) == null;
			useButton.Pressed      += () => OnUseItem(item, path);
			useButton.FocusEntered += () => { ShowDesc(item); AudioManager.Instance?.PlaySfx(UiSfx.Cursor); };
			row.AddChild(useButton);
		}
		else
		{
			// Ingredient / KeyItem — info-only button for focus/description
			var infoBtn = new Button { Text = "INFO", Name = "InfoButton", Disabled = false };
			infoBtn.AddThemeFontSizeOverride("font_size", 9);
			infoBtn.FocusEntered += () => { ShowDesc(item); AudioManager.Instance?.PlaySfx(UiSfx.Cursor); };
			infoBtn.Pressed     += () => ShowDesc(item);
			row.AddChild(infoBtn);
		}

		return row;
	}

	private HBoxContainer BuildStaticEquipRow(EquipmentData eq, string slotText, bool equipped, string ownerName)
	{
		var row = new HBoxContainer();

		var nameLabel = new Label { Text = eq.DisplayName };
		nameLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		nameLabel.AddThemeFontSizeOverride("font_size", 10);

		var slotLabel = new Label { Text = slotText };
		slotLabel.AddThemeFontSizeOverride("font_size", 9);
		slotLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));

		// Owner column — only meaningful when equipped. Empty for unworn items.
		string ownerText = equipped && !string.IsNullOrEmpty(ownerName) ? ownerName : "";
		var ownerLabel = new Label { Text = ownerText };
		ownerLabel.AddThemeFontSizeOverride("font_size", 9);
		ownerLabel.AddThemeColorOverride("font_color", UiTheme.Gold);
		ownerLabel.CustomMinimumSize = new Vector2(60f, 0f);

		var hint = new Button
		{
			Text     = equipped ? "EQUIPPED" : "—",
			Disabled = true,
		};
		hint.AddThemeFontSizeOverride("font_size", 9);

		row.AddChild(nameLabel);
		row.AddChild(slotLabel);
		row.AddChild(ownerLabel);
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

	/// <summary>
	/// Returns the party member who would benefit most from this item, or null
	/// if nobody can use it. Picks the member with the largest combined HP+MP
	/// deficit relative to the item's effects.
	/// </summary>
	private PartyMember? FindBestTarget(ItemData item)
	{
		var gm = GameManager.Instance;
		PartyMember? best = null;
		int bestDeficit = 0;

		foreach (var m in gm.Party.Members)
		{
			GetMemberHpMp(m, out int curHp, out int maxHp, out int curMp, out int maxMp);

			bool canHeal    = item.HealAmount > 0 && curHp < maxHp;
			bool canRestore = item.RestoreMp  > 0 && curMp < maxMp;
			if (!canHeal && !canRestore) continue;

			int deficit = (canHeal ? maxHp - curHp : 0) + (canRestore ? maxMp - curMp : 0);
			if (best == null || deficit > bestDeficit)
			{
				best = m;
				bestDeficit = deficit;
			}
		}
		return best;
	}

	private static void GetMemberHpMp(PartyMember m, out int curHp, out int maxHp, out int curMp, out int maxMp)
	{
		if (m.MemberId == "sen")
		{
			var s = GameManager.Instance.PlayerStats;
			curHp = s.CurrentHp; maxHp = s.MaxHp;
			curMp = s.CurrentMp; maxMp = s.MaxMp;
		}
		else
		{
			curHp = m.CurrentHp; maxHp = m.MaxHp;
			curMp = m.CurrentMp; maxMp = m.MaxMp;
		}
	}

	private void OnUseItem(ItemData item, string path)
	{
		var target = FindBestTarget(item);
		if (target == null)
		{
			ShowFeedback("No effect.");
			return;
		}

		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		GetMemberHpMp(target, out int curHp, out int maxHp, out int curMp, out int maxMp);

		int actualHp = item.HealAmount > 0
			? ItemLogic.ActualHeal(item.HealAmount, curHp, maxHp)
			: 0;
		int actualMp = item.RestoreMp > 0
			? ItemLogic.ActualMpRestore(item.RestoreMp, curMp, maxMp)
			: 0;

		if (target.MemberId == "sen")
		{
			if (item.HealAmount > 0) GameManager.Instance.HealPlayer(item.HealAmount);
			if (item.RestoreMp  > 0) GameManager.Instance.RestoreMp(item.RestoreMp);
		}
		else
		{
			if (actualHp > 0) target.CurrentHp = System.Math.Min(target.MaxHp, target.CurrentHp + actualHp);
			if (actualMp > 0) target.CurrentMp = System.Math.Min(target.MaxMp, target.CurrentMp + actualMp);
		}
		GameManager.Instance.RemoveItem(path);

		string who = target.DisplayName;
		string feedback = (actualHp, actualMp) switch
		{
			( > 0,  > 0) => $"{who} +{actualHp} HP, +{actualMp} MP",
			( > 0, _  )  => $"{who} +{actualHp} HP",
			(_,     > 0) => $"{who} +{actualMp} MP",
			_            => "No effect.",
		};
		ShowFeedback(feedback);
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
