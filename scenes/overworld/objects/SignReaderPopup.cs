using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Bottom-of-screen text panel shown when the player reads a sign.
/// Dismissed with the interact or cancel action.
/// Emits Closed and QueueFrees itself.
/// </summary>
public partial class SignReaderPopup : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	private readonly string   _title;
	private readonly string[] _lines;

	public SignReaderPopup(string title, string[] lines)
	{
		_title = title;
		_lines = lines;
	}

	public override void _Ready()
	{
		Layer = 55;

		// Bottom panel — spans full width, ~90px tall
		var panel = new PanelContainer
		{
			AnchorLeft     = 0f,     AnchorRight  = 1f,
			AnchorTop      = 1f,     AnchorBottom = 1f,
			OffsetLeft     = 8f,     OffsetRight  = -8f,
			OffsetTop      = -92f,   OffsetBottom = -8f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical   = Control.GrowDirection.Begin,
		};
		AddChild(panel);

		var vbox = new VBoxContainer();
		panel.AddChild(vbox);

		if (!string.IsNullOrEmpty(_title))
		{
			var titleLabel = new Label
			{
				Text                = _title,
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			titleLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			titleLabel.AddThemeFontSizeOverride("font_size", 9);
			vbox.AddChild(titleLabel);
		}

		var textLabel = new Label
		{
			Text         = string.Join("\n", _lines),
			AutowrapMode = TextServer.AutowrapMode.Word,
		};
		textLabel.AddThemeFontSizeOverride("font_size", 8);
		vbox.AddChild(textLabel);

		var hint = new Label
		{
			Text                = "[Z] Close",
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		hint.AddThemeColorOverride("font_color", Colors.Yellow);
		hint.AddThemeFontSizeOverride("font_size", 7);
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
