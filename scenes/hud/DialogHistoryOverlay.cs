using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Scrollable overlay showing the last dialog lines spoken.
/// Toggled with the dialog_log action (Tab). Persists within each map scene.
/// </summary>
public partial class DialogHistoryOverlay : CanvasLayer
{
	private Control        _panel      = null!;
	private VBoxContainer  _lines      = null!;
	private ScrollContainer _scroll    = null!;

	public override void _Ready()
	{
		Layer   = 58; // above HUD, minimap; below save dialog (60)
		Visible = false;

		// Dim backdrop
		var overlay = new ColorRect
		{
			Color          = new Color(0, 0, 0, 0.75f),
			AnchorRight    = 1f,
			AnchorBottom   = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical   = Control.GrowDirection.Both,
		};
		AddChild(overlay);

		// Centred panel
		_panel = new PanelContainer
		{
			AnchorLeft     = 0.1f, AnchorRight  = 0.9f,
			AnchorTop      = 0.1f, AnchorBottom = 0.9f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical   = Control.GrowDirection.Both,
		};
		overlay.AddChild(_panel);

		var vbox = new VBoxContainer();
		_panel.AddChild(vbox);

		var title = new Label
		{
			Text                = "Dialog History",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 10);
		title.AddThemeColorOverride("font_color", Colors.Yellow);
		vbox.AddChild(title);

		vbox.AddChild(new HSeparator());

		_scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.Expand | Control.SizeFlags.Fill };
		vbox.AddChild(_scroll);

		_lines = new VBoxContainer();
		_lines.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		_scroll.AddChild(_lines);

		vbox.AddChild(new HSeparator());

		var hint = new Label
		{
			Text                = "[Tab] Close",
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		hint.AddThemeColorOverride("font_color", Colors.Yellow);
		hint.AddThemeFontSizeOverride("font_size", 7);
		vbox.AddChild(hint);

		DialogicBridge.Instance.HistoryUpdated += OnHistoryUpdated;
	}

	public override void _ExitTree()
	{
		if (DialogicBridge.Instance != null)
			DialogicBridge.Instance.HistoryUpdated -= OnHistoryUpdated;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("dialog_log"))
		{
			GetViewport().SetInputAsHandled();
			Toggle();
		}
	}

	private void Toggle()
	{
		if (!Visible)
		{
			Refresh();
			Visible = true;
			// Scroll to bottom after layout
			Callable.From(ScrollToBottom).CallDeferred();
		}
		else
		{
			Visible = false;
		}
	}

	private void Refresh()
	{
		foreach (var child in _lines.GetChildren())
			child.QueueFree();

		foreach (var entry in DialogicBridge.Instance.DialogHistory)
		{
			if (!string.IsNullOrEmpty(entry.Speaker))
			{
				var speakerLabel = new Label { Text = entry.Speaker };
				speakerLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.9f, 1f));
				speakerLabel.AddThemeFontSizeOverride("font_size", 8);
				_lines.AddChild(speakerLabel);
			}

			var textLabel = new Label
			{
				Text         = entry.Text,
				AutowrapMode = TextServer.AutowrapMode.Word,
			};
			textLabel.AddThemeFontSizeOverride("font_size", 8);
			textLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
			_lines.AddChild(textLabel);

			_lines.AddChild(new HSeparator());
		}

		if (_lines.GetChildCount() == 0)
		{
			var empty = new Label
			{
				Text                = "No dialog yet.",
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			empty.AddThemeFontSizeOverride("font_size", 8);
			_lines.AddChild(empty);
		}
	}

	private void ScrollToBottom()
	{
		_scroll.ScrollVertical = (int)_scroll.GetVScrollBar().MaxValue;
	}

	private void OnHistoryUpdated()
	{
		if (Visible) Refresh();
	}
}
