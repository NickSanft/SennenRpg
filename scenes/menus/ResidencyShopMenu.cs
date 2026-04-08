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

	/// <summary>
	/// Queue of join-dialog timeline paths to play after the menu closes. Each successful
	/// party-recruit hire appends to this list, so hiring Lily AND Rain in the same
	/// session plays both join cutscenes back-to-back instead of dropping the first one.
	/// Cleared on every <see cref="Open"/>.
	/// </summary>
	public System.Collections.Generic.List<string> PendingJoinTimelines { get; }
		= new System.Collections.Generic.List<string>();

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
		_backButton.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);

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

	/// <summary>Populates the menu with the given resident entries and makes it visible.</summary>
	public void Open(NpcResidencyEntry[] stock)
	{
		_stock = stock;
		PendingJoinTimelines.Clear();
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

		// Re-apply font to dynamically created rows
		UiTheme.ApplyPixelFontToAll(_itemRows);
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
			hireButton.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);

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

		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		GameManager.Instance.AddGold(-entry.Price);
		GameManager.Instance.SetFlag(entry.FlagKey, true);

		// Phase 3: residency entries with a PartyMemberId also recruit a party member.
		if (!string.IsNullOrEmpty(entry.PartyMemberId))
		{
			var member = BuildPartyMemberFromEntry(entry);
			if (GameManager.Instance.RecruitPartyMember(member)
				&& !string.IsNullOrEmpty(entry.JoinTimelinePath))
			{
				// Queue the join cutscene; multiple hires in one session play back-to-back.
				PendingJoinTimelines.Add(entry.JoinTimelinePath);
			}
			ShowFeedback($"{entry.DisplayName} joined the party!");
		}
		else
		{
			ShowFeedback($"{entry.DisplayName} has moved into Mellyr Outpost!");
		}

		Refresh();
	}

	private static PartyMember BuildPartyMemberFromEntry(NpcResidencyEntry entry)
	{
		var stats = entry.StartingStats as CharacterStats;
		return new PartyMember
		{
			MemberId            = entry.PartyMemberId,
			DisplayName         = entry.DisplayName,
			Class               = entry.JoinClass.ToString(),
			CanChangeClass      = false,
			Row                 = FormationRow.Front,
			OverworldSpritePath = entry.OverworldSpritePath,
			Level               = 1,
			Exp                 = 0,
			MaxHp               = stats?.MaxHp      ?? 18,
			CurrentHp           = stats?.MaxHp      ?? 18,
			MaxMp               = stats?.MaxMp      ?? 10,
			CurrentMp           = stats?.MaxMp      ?? 10,
			Attack              = stats?.Attack     ?? 8,
			Defense             = stats?.Defense    ?? 3,
			Speed               = stats?.Speed      ?? 10,
			Magic               = stats?.Magic      ?? 6,
			Resistance          = stats?.Resistance ?? 3,
			Luck                = stats?.Luck       ?? 8,
		};
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
