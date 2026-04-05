using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Full-screen shop overlay opened by VendorNpc when the player interacts with a vendor.
/// Displays each ShopItemEntry with its price; buying deducts gold and adds the item to inventory.
/// Emits Closed when the player backs out so VendorNpc can restore game state.
/// </summary>
public partial class ShopMenu : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	private Label         _goldLabel  = null!;
	private VBoxContainer _itemRows   = null!;
	private Label         _emptyLabel = null!;
	private Label         _feedbackLabel = null!;
	private Label         _descLabel  = null!;
	private Button        _backButton = null!;

	private ShopItemEntry[] _stock = [];

	public override void _Ready()
	{
		Layer   = 51; // above PauseMenu (50), below SceneTransition (100)
		Visible = false;

		_goldLabel     = GetNode<Label>("Overlay/Panel/VBox/GoldLabel");
		_itemRows      = GetNode<VBoxContainer>("Overlay/Panel/VBox/ItemRows");
		_emptyLabel    = GetNode<Label>("Overlay/Panel/VBox/EmptyLabel");
		_feedbackLabel = GetNode<Label>("Overlay/Panel/VBox/FeedbackLabel");
		_backButton    = GetNode<Button>("Overlay/Panel/VBox/BackButton");

		_backButton.Pressed += Close;

		// Description area — shows item info on focus
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

		// Apply SNES theme
		var overlay = GetNodeOrNull<ColorRect>("Overlay");
		if (overlay != null) overlay.Color = UiTheme.OverlayDim;
		var panel = GetNodeOrNull<PanelContainer>("Overlay/Panel");
		if (panel != null) UiTheme.ApplyPanelTheme(panel);
		UiTheme.ApplyToAllButtons(this);
		UiTheme.ApplyPixelFontToAll(this);
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

	/// <summary>Populates the shop with the given stock and makes the menu visible.</summary>
	public void Open(ShopItemEntry[] stock)
	{
		_stock = stock;
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
		_goldLabel.Text = $"Gold: {GameManager.Instance.Gold}";
		_feedbackLabel.Text = "";
		_descLabel.Text     = "";

		foreach (var child in _itemRows.GetChildren())
			child.QueueFree();

		int validCount = 0;
		bool focusGrabbed = false;

		foreach (var entry in _stock)
		{
			if (string.IsNullOrEmpty(entry.ItemDataPath)) continue;
			if (!ResourceLoader.Exists(entry.ItemDataPath)) continue;

			var item = GD.Load<ItemData>(entry.ItemDataPath);
			if (item == null) continue;

			validCount++;
			var row = BuildItemRow(item, entry);
			_itemRows.AddChild(row);

			if (!focusGrabbed)
			{
				row.GetNodeOrNull<Button>("BuyButton")?.GrabFocus();
				focusGrabbed = true;
			}
		}

		_emptyLabel.Visible = validCount == 0;

		if (!focusGrabbed)
			_backButton.GrabFocus();

		// Re-apply font to dynamically created rows
		UiTheme.ApplyPixelFontToAll(_itemRows);
	}

	private HBoxContainer BuildItemRow(ItemData item, ShopItemEntry entry)
	{
		var row = new HBoxContainer();

		var nameLabel = new Label { Text = item.DisplayName };
		nameLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		nameLabel.AddThemeFontSizeOverride("font_size", 10);

		var priceLabel = new Label { Text = $"{entry.Price}G" };
		priceLabel.AddThemeFontSizeOverride("font_size", 10);

		var buyButton = new Button { Text = "BUY", Name = "BuyButton" };
		buyButton.Disabled = !ShopLogic.CanAfford(GameManager.Instance.Gold, entry.Price);
		buyButton.Pressed += () => OnBuy(item, entry);
		buyButton.FocusEntered += () =>
		{
			ShowItemDesc(item);
			AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
		};

		row.AddChild(nameLabel);
		row.AddChild(priceLabel);
		row.AddChild(buyButton);
		return row;
	}

	private void OnBuy(ItemData item, ShopItemEntry entry)
	{
		if (!ShopLogic.CanAfford(GameManager.Instance.Gold, entry.Price))
		{
			AudioManager.Instance?.PlaySfx(UiSfx.Error);
			return;
		}

		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		GameManager.Instance.AddGold(-entry.Price);
		GameManager.Instance.AddItem(entry.ItemDataPath);

		ShowFeedback($"Bought {item.DisplayName}!");
		Refresh();
	}

	private void ShowItemDesc(ItemData item)
	{
		var parts = new System.Collections.Generic.List<string>();
		if (!string.IsNullOrEmpty(item.Description)) parts.Add(item.Description);
		if (item.HealAmount > 0) parts.Add($"Restores {item.HealAmount} HP");
		if (item.RepelSteps > 0) parts.Add($"Repels enemies for {item.RepelSteps} steps");
		_descLabel.Text = parts.Count > 0 ? string.Join("  ·  ", parts) : item.DisplayName;
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
