using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Code-driven popup shown when the player interacts with an NPC that has a
/// CharacterDescription set. Offers TALK / EXAMINE / CANCEL options.
/// EXAMINE reveals the description inline; CLOSE returns to the main menu.
/// Call Open() from Npc.Interact(), then connect TalkSelected and Cancelled.
/// </summary>
public partial class NpcInteractMenu : CanvasLayer
{
	[Signal] public delegate void TalkSelectedEventHandler();
	[Signal] public delegate void ShopSelectedEventHandler();
	[Signal] public delegate void RestSelectedEventHandler();
	[Signal] public delegate void ChangeClassSelectedEventHandler();
	[Signal] public delegate void SellJunkSelectedEventHandler();
	[Signal] public delegate void CancelledEventHandler();

	private PanelContainer _panel              = null!;
	private VBoxContainer _vbox               = null!;
	private Button        _talkButton         = null!;
	private Button        _shopButton         = null!;
	private Button        _restButton         = null!;
	private Button        _changeClassButton  = null!;
	private Button        _sellJunkButton     = null!;
	private Button        _examineButton      = null!;
	private Button        _cancelButton       = null!;
	private Label         _descLabel          = null!;
	private Button        _closeButton        = null!;

	private string _description = "";
	private bool   _showingDesc;
	private bool   _showShop;
	private bool   _showRest;
	private bool   _showChangeClass;
	private bool   _showSellJunk;

	public override void _Ready()
	{
		Layer = 52; // above pause/inventory (50–51), below scene transition (100)

		// Dark overlay behind the panel
		var overlay = new ColorRect
		{
			Color          = UiTheme.OverlayDim,
			AnchorLeft     = 0f, AnchorRight  = 1f,
			AnchorTop      = 0f, AnchorBottom = 1f,
			MouseFilter    = Control.MouseFilterEnum.Ignore,
		};
		AddChild(overlay);

		// Centred auto-sizing panel with SNES theme
		var centerer = new CenterContainer
		{
			AnchorRight  = 1f,
			AnchorBottom = 1f,
		};
		AddChild(centerer);

		var panelContainer = new PanelContainer
		{
			CustomMinimumSize = new Vector2(160f, 0f),
		};
		UiTheme.ApplyPanelTheme(panelContainer);
		centerer.AddChild(panelContainer);

		// Store as Panel for desc-view resizing
		_panel = panelContainer;

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left",   12);
		margin.AddThemeConstantOverride("margin_right",  12);
		margin.AddThemeConstantOverride("margin_top",    8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		panelContainer.AddChild(margin);

		_vbox = new VBoxContainer();
		_vbox.AddThemeConstantOverride("separation", 4);
		margin.AddChild(_vbox);

		_talkButton = new Button { Text = "Talk" };
		UiTheme.ApplyButtonTheme(_talkButton);
		_talkButton.Pressed += OnTalk;
		_vbox.AddChild(_talkButton);

		_shopButton = new Button { Text = "Shop", Visible = false };
		UiTheme.ApplyButtonTheme(_shopButton);
		_shopButton.Pressed += OnShop;
		_vbox.AddChild(_shopButton);

		_restButton = new Button { Text = "Rest (10G)", Visible = false };
		UiTheme.ApplyButtonTheme(_restButton);
		_restButton.Pressed += OnRest;
		_vbox.AddChild(_restButton);

		_changeClassButton = new Button { Text = "Change Class", Visible = false };
		UiTheme.ApplyButtonTheme(_changeClassButton);
		_changeClassButton.Pressed += OnChangeClass;
		_vbox.AddChild(_changeClassButton);

		_sellJunkButton = new Button { Text = "Sell Junk", Visible = false };
		UiTheme.ApplyButtonTheme(_sellJunkButton);
		_sellJunkButton.Pressed += OnSellJunk;
		_vbox.AddChild(_sellJunkButton);

		_examineButton = new Button { Text = "Examine" };
		UiTheme.ApplyButtonTheme(_examineButton);
		_examineButton.Pressed += OnExamine;
		_vbox.AddChild(_examineButton);

		_cancelButton = new Button { Text = "Cancel" };
		UiTheme.ApplyButtonTheme(_cancelButton);
		_cancelButton.Pressed += OnCancel;
		_vbox.AddChild(_cancelButton);

		_descLabel = new Label
		{
			Text                = "",
			AutowrapMode        = TextServer.AutowrapMode.Word,
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsVertical   = Control.SizeFlags.Expand | Control.SizeFlags.Fill,
			Visible             = false,
		};
		_descLabel.AddThemeFontSizeOverride("font_size", 9);
		_vbox.AddChild(_descLabel);

		_closeButton = new Button { Text = "Close", Visible = false };
		_closeButton.Pressed += OnCloseDesc;
		_vbox.AddChild(_closeButton);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("cancel") || @event.IsActionPressed("ui_cancel"))
		{
			if (_showingDesc)
				OnCloseDesc();
			else
				OnCancel();
			GetViewport().SetInputAsHandled();
		}
	}

	/// <summary>Populate and display the menu. Call after adding to the scene tree.</summary>
	/// <param name="description">Character description shown by Examine, or empty to hide Examine.</param>
	/// <param name="showShop">When true, shows a Shop button that emits ShopSelected.</param>
	/// <param name="showRest">When true, shows a Rest button that emits RestSelected.</param>
	/// <param name="showChangeClass">When true, shows a Change Class button that emits ChangeClassSelected.</param>
	public void Open(string description, bool showShop = false, bool showRest = false,
		bool showChangeClass = false, bool showSellJunk = false)
	{
		_description     = description;
		_showShop        = showShop;
		_showRest        = showRest;
		_showChangeClass = showChangeClass;
		_showSellJunk    = showSellJunk;
		ShowMainButtons();
	}

	// ── Private ───────────────────────────────────────────────────────────────

	private void ShowMainButtons()
	{
		_showingDesc = false;
		_talkButton.Visible         = true;
		_shopButton.Visible         = _showShop;
		_restButton.Visible         = _showRest;
		_changeClassButton.Visible  = _showChangeClass;
		_sellJunkButton.Visible     = _showSellJunk;
		_examineButton.Visible      = !string.IsNullOrEmpty(_description);
		_cancelButton.Visible       = true;
		_descLabel.Visible          = false;
		_closeButton.Visible        = false;
		_panel.CustomMinimumSize = new Vector2(160f, 0f);
		_talkButton.GrabFocus();
	}

	private void ShowDescView()
	{
		_showingDesc = true;
		_talkButton.Visible         = false;
		_shopButton.Visible         = false;
		_restButton.Visible         = false;
		_changeClassButton.Visible  = false;
		_sellJunkButton.Visible     = false;
		_examineButton.Visible      = false;
		_cancelButton.Visible       = false;
		_descLabel.Text        = _description;
		_descLabel.Visible     = true;
		_closeButton.Visible   = true;
		// Widen panel to fit description text
		_panel.CustomMinimumSize = new Vector2(220f, 0f);
		_closeButton.GrabFocus();
	}

	private void OnTalk()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		QueueFree();
		EmitSignal(SignalName.TalkSelected);
	}

	private void OnShop()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		QueueFree();
		EmitSignal(SignalName.ShopSelected);
	}

	private void OnRest()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		QueueFree();
		EmitSignal(SignalName.RestSelected);
	}

	private void OnChangeClass()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		QueueFree();
		EmitSignal(SignalName.ChangeClassSelected);
	}

	private void OnSellJunk()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		QueueFree();
		EmitSignal(SignalName.SellJunkSelected);
	}

	private void OnExamine()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		ShowDescView();
	}

	private void OnCloseDesc()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
		_panel.CustomMinimumSize = new Vector2(160f, 0f);
		ShowMainButtons();
	}

	private void OnCancel()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
		QueueFree();
		EmitSignal(SignalName.Cancelled);
	}
}
