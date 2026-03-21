using Godot;

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
	[Signal] public delegate void CancelledEventHandler();

	private Panel         _panel         = null!;
	private VBoxContainer _vbox          = null!;
	private Button        _talkButton    = null!;
	private Button        _examineButton = null!;
	private Button        _cancelButton  = null!;
	private Label         _descLabel     = null!;
	private Button        _closeButton   = null!;

	private string _description = "";
	private bool   _showingDesc;

	public override void _Ready()
	{
		Layer = 52; // above pause/inventory (50–51), below scene transition (100)

		// Dark overlay behind the panel
		var overlay = new ColorRect
		{
			Color          = new Color(0f, 0f, 0f, 0.5f),
			AnchorLeft     = 0f, AnchorRight  = 1f,
			AnchorTop      = 0f, AnchorBottom = 1f,
			MouseFilter    = Control.MouseFilterEnum.Ignore,
		};
		AddChild(overlay);

		// Centred panel
		_panel = new Panel
		{
			AnchorLeft   = 0.5f, AnchorRight  = 0.5f,
			AnchorTop    = 0.5f, AnchorBottom = 0.5f,
			OffsetLeft   = -80f, OffsetRight  =  80f,
			OffsetTop    = -60f, OffsetBottom =  60f,
		};
		AddChild(_panel);

		_vbox = new VBoxContainer
		{
			AnchorLeft   = 0f, AnchorRight  = 1f,
			AnchorTop    = 0f, AnchorBottom = 1f,
			OffsetLeft   =  8f, OffsetRight  = -8f,
			OffsetTop    =  8f, OffsetBottom = -8f,
		};
		_panel.AddChild(_vbox);

		_talkButton = new Button { Text = "Talk" };
		_talkButton.Pressed += OnTalk;
		_vbox.AddChild(_talkButton);

		_examineButton = new Button { Text = "Examine" };
		_examineButton.Pressed += OnExamine;
		_vbox.AddChild(_examineButton);

		_cancelButton = new Button { Text = "Cancel" };
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
	public void Open(string description)
	{
		_description = description;
		ShowMainButtons();
	}

	// ── Private ───────────────────────────────────────────────────────────────

	private void ShowMainButtons()
	{
		_showingDesc = false;
		_talkButton.Visible    = true;
		_examineButton.Visible = !string.IsNullOrEmpty(_description);
		_cancelButton.Visible  = true;
		_descLabel.Visible     = false;
		_closeButton.Visible   = false;
		_panel.OffsetLeft   = -80f; _panel.OffsetRight  =  80f;
		_panel.OffsetTop    = -60f; _panel.OffsetBottom =  60f;
		_talkButton.GrabFocus();
	}

	private void ShowDescView()
	{
		_showingDesc = true;
		_talkButton.Visible    = false;
		_examineButton.Visible = false;
		_cancelButton.Visible  = false;
		_descLabel.Text        = _description;
		_descLabel.Visible     = true;
		_closeButton.Visible   = true;
		// Widen panel to fit description text
		_panel.OffsetLeft   = -110f; _panel.OffsetRight  =  110f;
		_panel.OffsetTop    =  -80f; _panel.OffsetBottom =   80f;
		_closeButton.GrabFocus();
	}

	private void OnTalk()
	{
		QueueFree();
		EmitSignal(SignalName.TalkSelected);
	}

	private void OnExamine() => ShowDescView();

	private void OnCloseDesc()
	{
		_panel.OffsetLeft   = -80f; _panel.OffsetRight  =  80f;
		ShowMainButtons();
	}

	private void OnCancel()
	{
		QueueFree();
		EmitSignal(SignalName.Cancelled);
	}
}
