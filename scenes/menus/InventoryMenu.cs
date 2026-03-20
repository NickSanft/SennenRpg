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

	public override void _Ready()
	{
		Layer   = 51; // above PauseMenu (50), below SceneTransition (100)
		Visible = false;

		_emptyLabel    = GetNode<Label>("Overlay/Panel/VBox/EmptyLabel");
		_itemRows      = GetNode<VBoxContainer>("Overlay/Panel/VBox/ItemRows");
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

		var paths = GameManager.Instance.InventoryItemPaths;
		_emptyLabel.Visible = paths.Count == 0;

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
				// Focus the first USE button so keyboard/controller works immediately
				row.GetNodeOrNull<Button>("UseButton")?.GrabFocus();
				focusGrabbed = true;
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

		string descText = item.HealAmount > 0 ? $"+{item.HealAmount} HP" : item.Description;
		var descLabel = new Label { Text = descText };
		descLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		descLabel.AddThemeFontSizeOverride("font_size", 10);

		var useButton = new Button { Text = "USE", Name = "UseButton" };
		useButton.Disabled = !ItemLogic.CanUseItem(
			item.HealAmount,
			GameManager.Instance.PlayerStats.CurrentHp,
			GameManager.Instance.PlayerStats.MaxHp);
		useButton.Pressed += () => OnUseItem(item, path);

		row.AddChild(nameLabel);
		row.AddChild(descLabel);
		row.AddChild(useButton);
		return row;
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
