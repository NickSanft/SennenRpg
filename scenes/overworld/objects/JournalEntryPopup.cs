using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Full-screen panel showing one journal entry with a scrollable body.
/// Dismissed with interact or cancel; emits Closed and QueueFrees itself.
/// </summary>
public partial class JournalEntryPopup : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	private readonly string _date;
	private readonly string _title;
	private readonly string _body;

	public JournalEntryPopup(string date, string title, string body)
	{
		_date  = date;
		_title = title;
		_body  = body;
	}

	public override void _Ready()
	{
		Layer = 56;

		var panel = new PanelContainer
		{
			AnchorLeft   = 0.05f, AnchorRight  = 0.95f,
			AnchorTop    = 0.05f, AnchorBottom = 0.95f,
			OffsetLeft   = 0f,    OffsetRight  = 0f,
			OffsetTop    = 0f,    OffsetBottom = 0f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical   = Control.GrowDirection.Both,
		};
		AddChild(panel);

		var vbox = new VBoxContainer();
		panel.AddChild(vbox);

		// ── Header ────────────────────────────────────────────────────────────
		var dateLabel = new Label { Text = _date };
		dateLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.6f, 0.3f));
		dateLabel.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(dateLabel);

		var titleLabel = new Label { Text = _title };
		titleLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		titleLabel.AddThemeFontSizeOverride("font_size", 15);
		vbox.AddChild(titleLabel);

		var divider = new ColorRect
		{
			Color               = new Color(0.5f, 0.45f, 0.2f, 0.6f),
			CustomMinimumSize   = new Vector2(0, 1),
			SizeFlagsHorizontal = Control.SizeFlags.Fill,
		};
		vbox.AddChild(divider);

		// ── Scrollable body ───────────────────────────────────────────────────
		// RichTextLabel with ScrollActive fills the remaining vertical space and
		// handles its own scrollbar — avoids the Godot 4 AutowrapMode width bug
		// that occurs when Label is inside a ScrollContainer.
		var bodyLabel = new RichTextLabel
		{
			Text                = _body,
			BbcodeEnabled       = false,
			FitContent          = false,
			ScrollActive        = true,
			SizeFlagsVertical   = Control.SizeFlags.Expand | Control.SizeFlags.Fill,
			SizeFlagsHorizontal = Control.SizeFlags.Fill,
		};
		bodyLabel.AddThemeFontSizeOverride("normal_font_size", 13);
		bodyLabel.GrabFocus();
		vbox.AddChild(bodyLabel);

		// ── Footer ────────────────────────────────────────────────────────────
		var hint = new Label
		{
			Text                = "[Z] Back",
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		hint.AddThemeColorOverride("font_color", Colors.Yellow);
		hint.AddThemeFontSizeOverride("font_size", 11);
		vbox.AddChild(hint);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("interact") || @event.IsActionPressed("cancel"))
		{
			GetViewport().SetInputAsHandled();
			EmitSignal(SignalName.Closed);
			QueueFree();
		}
	}
}
