using System.Linq;
using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Menu that lets the player hire NPC residents for Mellyr Outpost.
/// Opened by RorkTownNpc. Each row shows a resident's name, description, and price.
/// Already-purchased residents are shown as inactive "RESIDENT" entries.
/// </summary>
public partial class ResidencyShopMenu : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	private Label         _goldLabel     = null!;
	private VBoxContainer _itemRows      = null!;
	private Label         _emptyLabel    = null!;
	private Label         _feedbackLabel = null!;
	private Button        _backButton    = null!;

	private NpcResidencyEntry[] _stock = [];

	public override void _Ready()
	{
		Layer   = 51;
		Visible = false;

		_goldLabel     = GetNode<Label>("Overlay/Panel/VBox/GoldLabel");
		_itemRows      = GetNode<VBoxContainer>("Overlay/Panel/VBox/ItemRows");
		_emptyLabel    = GetNode<Label>("Overlay/Panel/VBox/EmptyLabel");
		_feedbackLabel = GetNode<Label>("Overlay/Panel/VBox/FeedbackLabel");
		_backButton    = GetNode<Button>("Overlay/Panel/VBox/BackButton");

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

	/// <summary>Populates the menu with the given resident entries and makes it visible.</summary>
	public void Open(NpcResidencyEntry[] stock)
	{
		_stock = stock;
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
		_goldLabel.Text     = $"Gold: {GameManager.Instance.Gold}";
		_feedbackLabel.Text = "";

		foreach (var child in _itemRows.GetChildren())
			child.QueueFree();

		bool focusGrabbed = false;

		foreach (var entry in _stock)
		{
			if (string.IsNullOrEmpty(entry.FlagKey)) continue;

			bool purchased = GameManager.Instance.GetFlag(entry.FlagKey);
			var row = BuildRow(entry, purchased);
			_itemRows.AddChild(row);

			if (!focusGrabbed)
			{
				var btn = row.GetNodeOrNull<Button>("HireButton") ?? row.GetNodeOrNull<Button>("OwnedLabel");
				btn?.GrabFocus();
				focusGrabbed = true;
			}
		}

		_emptyLabel.Visible = _stock.Length == 0;

		if (!focusGrabbed)
			_backButton.GrabFocus();
	}

	private HBoxContainer BuildRow(NpcResidencyEntry entry, bool purchased)
	{
		var row = new HBoxContainer();

		var nameLabel = new Label { Text = entry.DisplayName };
		nameLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		nameLabel.AddThemeFontSizeOverride("font_size", 10);

		var descLabel = new Label { Text = entry.Description };
		descLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		descLabel.AddThemeFontSizeOverride("font_size", 10);

		if (purchased)
		{
			var ownedBtn = new Button { Text = "RESIDENT", Name = "OwnedLabel", Disabled = true };
			row.AddChild(nameLabel);
			row.AddChild(descLabel);
			row.AddChild(ownedBtn);
		}
		else
		{
			var priceLabel = new Label { Text = $"{entry.Price}G" };
			priceLabel.AddThemeFontSizeOverride("font_size", 10);

			var hireButton = new Button { Text = "HIRE", Name = "HireButton" };
			hireButton.Disabled = GameManager.Instance.Gold < entry.Price;
			hireButton.Pressed += () => OnHire(entry);

			row.AddChild(nameLabel);
			row.AddChild(descLabel);
			row.AddChild(priceLabel);
			row.AddChild(hireButton);
		}

		return row;
	}

	private void OnHire(NpcResidencyEntry entry)
	{
		if (GameManager.Instance.Gold < entry.Price) return;

		GameManager.Instance.AddGold(-entry.Price);
		GameManager.Instance.SetFlag(entry.FlagKey, true);

		ShowFeedback($"{entry.DisplayName} has moved into Mellyr Outpost!");
		Refresh();
	}

	private void ShowFeedback(string text)
	{
		_feedbackLabel.Text = text;
		GetTree().CreateTimer(2.0f).Timeout += () =>
		{
			if (IsInstanceValid(_feedbackLabel))
				_feedbackLabel.Text = "";
		};
	}
}
