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

		// Bottom panel — spans full width, ~120px tall
		var panel = new PanelContainer
		{
			AnchorLeft     = 0f,     AnchorRight  = 1f,
			AnchorTop      = 1f,     AnchorBottom = 1f,
			OffsetLeft     = 8f,     OffsetRight  = -8f,
			OffsetTop      = -120f,  OffsetBottom = -8f,
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
				LabelSettings       = new LabelSettings
				{
					FontSize     = 12,
					FontColor    = Colors.Yellow,
					OutlineSize  = 2,
					OutlineColor = new Color(0f, 0f, 0f, 0.9f),
				},
			};
			vbox.AddChild(titleLabel);
		}

		var textLabel = new Label
		{
			Text          = string.Join("\n", _lines),
			AutowrapMode  = TextServer.AutowrapMode.Word,
			LabelSettings = new LabelSettings
			{
				FontSize     = 12,
				FontColor    = Colors.White,
				OutlineSize  = 2,
				OutlineColor = new Color(0f, 0f, 0f, 0.9f),
			},
		};
		vbox.AddChild(textLabel);

		var hint = new Label
		{
			Text                = "[Z] Close",
			HorizontalAlignment = HorizontalAlignment.Right,
			LabelSettings       = new LabelSettings
			{
				FontSize     = 11,
				FontColor    = Colors.Yellow,
				OutlineSize  = 2,
				OutlineColor = new Color(0f, 0f, 0f, 0.9f),
			},
		};
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
