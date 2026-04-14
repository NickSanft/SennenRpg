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

	// Tab filter entry. null Filter = ALL; "equipment" virtual filter uses IsEquipment flag.
	private readonly struct TabEntry
	{
		public readonly string Label;
		public readonly ItemType? Filter;
		public readonly bool IsEquipment;
		public TabEntry(string label, ItemType? filter, bool isEquipment = false)
		{
			Label = label; Filter = filter; IsEquipment = isEquipment;
		}
	}

	private static readonly TabEntry[] Tabs =
	{
		new("ALL",        null),
		new("CONSUMABLE", ItemType.Consumable),
		new("INGREDIENT", ItemType.Ingredient),
		new("EQUIPMENT",  null, isEquipment: true),
		new("KEY",        ItemType.KeyItem),
		new("REPEL",      ItemType.Repel),
	};

	private Label           _emptyLabel    = null!;
	private VBoxContainer   _itemRows      = null!;
	private Label           _feedbackLabel = null!;
	private Button          _backButton    = null!;
	private Label           _descLabel     = null!;
	private HBoxContainer   _tabRow        = null!;
	private int             _activeTabIndex;
	private readonly List<Button> _tabButtons = new();

	// Target selection sub-state for consumable items
	private bool            _targetSelectMode;
	private ItemData?       _pendingItem;
	private string?         _pendingItemPath;
	private int             _targetIndex;
	private PanelContainer? _targetPanel;
	private VBoxContainer?  _targetRows;
	private readonly List<Button> _targetButtons = new();

	public override void _Ready()
	{
		Layer   = 51;
		Visible = false;

		_emptyLabel    = GetNode<Label>("Overlay/Panel/VBox/EmptyLabel");
		_itemRows      = GetNode<VBoxContainer>("Overlay/Panel/VBox/ItemRows");
		_feedbackLabel = GetNode<Label>("Overlay/Panel/VBox/FeedbackLabel");
		_backButton    = GetNode<Button>("Overlay/Panel/VBox/BackButton");

		var vbox = GetNode<VBoxContainer>("Overlay/Panel/VBox");

		// Wrap ItemRows in a ScrollContainer so large inventories can't push the
		// panel off-screen (it's anchored to center and grows in both directions).
		int itemRowsIdx = _itemRows.GetIndex();
		vbox.RemoveChild(_itemRows);
		var scroll = new ScrollContainer
		{
			CustomMinimumSize    = new Vector2(240f, 260f),
			SizeFlagsHorizontal  = Control.SizeFlags.Expand | Control.SizeFlags.Fill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
		};
		_itemRows.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		scroll.AddChild(_itemRows);
		vbox.AddChild(scroll);
		vbox.MoveChild(scroll, itemRowsIdx);

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
		_tabButtons.Clear();
		for (int i = 0; i < Tabs.Length; i++)
		{
			int captured = i;
			var btn = new Button
			{
				Text = Tabs[i].Label,
				ToggleMode = true,
				ButtonPressed = i == _activeTabIndex,
				FocusMode = Control.FocusModeEnum.None,
			};
			btn.AddThemeFontSizeOverride("font_size", 9);
			btn.Modulate = i == _activeTabIndex ? UiTheme.Gold : UiTheme.SubtleGrey;
			btn.Pressed += () => SetActiveTab(captured);
			_tabRow.AddChild(btn);
			_tabButtons.Add(btn);
		}
	}

	private void SetActiveTab(int index)
	{
		if (index < 0) index = Tabs.Length - 1;
		if (index >= Tabs.Length) index = 0;
		if (index == _activeTabIndex) return;
		_activeTabIndex = index;
		AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
		UpdateTabVisuals();
		Refresh();
	}

	private void CycleTab(int delta)
	{
		SetActiveTab(_activeTabIndex + delta);
	}

	private void UpdateTabVisuals()
	{
		for (int i = 0; i < _tabButtons.Count; i++)
		{
			_tabButtons[i].ButtonPressed = i == _activeTabIndex;
			_tabButtons[i].Modulate      = i == _activeTabIndex ? UiTheme.Gold : UiTheme.SubtleGrey;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible) return;
		if (_targetSelectMode)
		{
			if (@event.IsActionPressed("ui_cancel"))
			{
				CloseTargetSelect();
				GetViewport().SetInputAsHandled();
				return;
			}
			if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
			{
				ConfirmTargetSelect();
				GetViewport().SetInputAsHandled();
				return;
			}
			return; // Let button focus handle up/down navigation
		}
		if (@event.IsActionPressed("menu") || @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
			return;
		}

		// Tab cycling: Left/Right arrows or Q/E
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.Q) { CycleTab(-1); GetViewport().SetInputAsHandled(); return; }
			if (key.Keycode == Key.E) { CycleTab(+1); GetViewport().SetInputAsHandled(); return; }
		}
		if (@event.IsActionPressed("ui_left"))
		{
			CycleTab(-1);
			GetViewport().SetInputAsHandled();
			return;
		}
		if (@event.IsActionPressed("ui_right"))
		{
			CycleTab(+1);
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

		var tab           = Tabs[_activeTabIndex];
		bool showItems    = !tab.IsEquipment;           // hide item list on EQUIPMENT tab
		bool showEquip    = tab.IsEquipment || tab.Filter == null; // show equip on ALL + EQUIPMENT

		// Stack items by path and count duplicates
		var stacked = CountItems(paths);

		if (showItems)
		{
			foreach (var (path, count) in stacked)
			{
				if (!ResourceLoader.Exists(path)) continue;
				var resource = GD.Load<Resource>(path);
				if (resource is not ItemData item) continue;

				// Apply category filter (null = ALL, show everything)
				if (tab.Filter.HasValue && item.Type != tab.Filter.Value) continue;

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
		}

		// Equipment section
		if (showEquip)
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
				foreach (var member in gm.Party.AllMembers)
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

		foreach (var m in gm.Party.AllMembers)
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
		// Enter target selection mode — let the player choose who to use it on.
		_pendingItem     = item;
		_pendingItemPath = path;
		OpenTargetSelect(item);
	}

	private void OpenTargetSelect(ItemData item)
	{
		_targetSelectMode = true;
		_targetButtons.Clear();

		// Build a small overlay panel for target selection
		_targetPanel?.QueueFree();
		_targetPanel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(300f, 0f),
		};
		UiTheme.ApplyPanelTheme(_targetPanel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left",   12);
		margin.AddThemeConstantOverride("margin_right",  12);
		margin.AddThemeConstantOverride("margin_top",    8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		_targetPanel.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 4);
		margin.AddChild(vbox);

		var header = new Label
		{
			Text = $"Use {item.DisplayName} on...",
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = UiTheme.Gold,
		};
		header.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(header);

		_targetRows = new VBoxContainer();
		_targetRows.AddThemeConstantOverride("separation", 2);
		vbox.AddChild(_targetRows);

		var gm = GameManager.Instance;
		var allMembers = gm.Party.AllMembers;
		int bestIdx = 0;

		// Find best default target
		var best = FindBestTarget(item);

		for (int i = 0; i < allMembers.Count; i++)
		{
			var m = allMembers[i];
			GetMemberHpMp(m, out int curHp, out int maxHp, out int curMp, out int maxMp);

			bool canUse = ItemLogic.CanUseItem(item.HealAmount, item.RestoreMp, curHp, maxHp, curMp, maxMp);

			string text = $"{m.DisplayName,-10}  HP {curHp,3}/{maxHp,-3}  MP {curMp,2}/{maxMp,-2}";
			var btn = new Button
			{
				Text = text,
				CustomMinimumSize = new Vector2(0f, 24f),
				Disabled = !canUse,
			};
			btn.AddThemeFontSizeOverride("font_size", 10);

			if (m.IsKO) btn.Modulate = new Color(0.6f, 0.4f, 0.4f);

			int captured = i;
			btn.FocusEntered += () =>
			{
				_targetIndex = captured;
				AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
			};

			_targetRows.AddChild(btn);
			_targetButtons.Add(btn);

			if (best != null && m.MemberId == best.MemberId)
				bestIdx = i;
		}

		// Position the panel centered on screen
		var centerer = new CenterContainer
		{
			AnchorRight  = 1f,
			AnchorBottom = 1f,
			Name         = "TargetCenterer",
		};
		centerer.AddChild(_targetPanel);
		AddChild(centerer);

		UiTheme.ApplyPixelFontToAll(centerer);
		UiTheme.ApplyToAllButtons(centerer);

		_targetIndex = bestIdx;
		if (_targetButtons.Count > bestIdx)
			_targetButtons[bestIdx].CallDeferred(Control.MethodName.GrabFocus);
	}

	private void ConfirmTargetSelect()
	{
		if (_pendingItem == null || _pendingItemPath == null) return;

		var gm = GameManager.Instance;
		var allMembers = gm.Party.AllMembers;
		if (_targetIndex < 0 || _targetIndex >= allMembers.Count) return;

		var target = allMembers[_targetIndex];
		GetMemberHpMp(target, out int curHp, out int maxHp, out int curMp, out int maxMp);

		if (!ItemLogic.CanUseItem(_pendingItem.HealAmount, _pendingItem.RestoreMp, curHp, maxHp, curMp, maxMp))
		{
			ShowFeedback("No effect.");
			CloseTargetSelect();
			return;
		}

		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);

		int actualHp = _pendingItem.HealAmount > 0
			? ItemLogic.ActualHeal(_pendingItem.HealAmount, curHp, maxHp)
			: 0;
		int actualMp = _pendingItem.RestoreMp > 0
			? ItemLogic.ActualMpRestore(_pendingItem.RestoreMp, curMp, maxMp)
			: 0;

		if (target.MemberId == "sen")
		{
			if (_pendingItem.HealAmount > 0) gm.HealPlayer(_pendingItem.HealAmount);
			if (_pendingItem.RestoreMp  > 0) gm.RestoreMp(_pendingItem.RestoreMp);
		}
		else
		{
			if (actualHp > 0) target.CurrentHp = System.Math.Min(target.MaxHp, target.CurrentHp + actualHp);
			if (actualMp > 0) target.CurrentMp = System.Math.Min(target.MaxMp, target.CurrentMp + actualMp);
		}
		gm.RemoveItem(_pendingItemPath);

		string who = target.DisplayName;
		string feedback = (actualHp, actualMp) switch
		{
			( > 0,  > 0) => $"{who} +{actualHp} HP, +{actualMp} MP",
			( > 0, _  )  => $"{who} +{actualHp} HP",
			(_,     > 0) => $"{who} +{actualMp} MP",
			_            => "No effect.",
		};
		ShowFeedback(feedback);
		CloseTargetSelect();
		Refresh();
	}

	private void CloseTargetSelect()
	{
		_targetSelectMode = false;
		_pendingItem      = null;
		_pendingItemPath  = null;
		_targetButtons.Clear();

		var centerer = GetNodeOrNull("TargetCenterer");
		centerer?.QueueFree();
		_targetPanel = null;
		_targetRows  = null;

		AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
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
