using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Journal table-of-contents overlay. Lists all entries as buttons;
/// selecting one opens a JournalEntryPopup. Cancel closes the journal.
/// Emits Closed and QueueFrees itself when the player exits.
/// </summary>
public partial class JournalMenuPopup : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	private readonly JournalData.JournalEntry[] _entries;
	private          PanelContainer             _panel   = null!;
	private          Button[]                   _buttons = [];

	public JournalMenuPopup(JournalData.JournalEntry[] entries)
	{
		_entries = entries;
	}

	public override void _Ready()
	{
		Layer = 55;
		BuildMenu();
	}

	private void BuildMenu()
	{
		_panel = new PanelContainer
		{
			AnchorLeft   = 0.10f, AnchorRight  = 0.90f,
			AnchorTop    = 0.10f, AnchorBottom = 0.90f,
			OffsetLeft   = 0f,    OffsetRight  = 0f,
			OffsetTop    = 0f,    OffsetBottom = 0f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical   = Control.GrowDirection.Both,
		};
		AddChild(_panel);

		var vbox = new VBoxContainer();
		_panel.AddChild(vbox);

		// ── Title ─────────────────────────────────────────────────────────────
		var title = new Label
		{
			Text                = "Aoife's Contes",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeColorOverride("font_color", Colors.Yellow);
		title.AddThemeFontSizeOverride("font_size", 16);
		vbox.AddChild(title);

		var divider = new ColorRect
		{
			Color             = new Color(0.5f, 0.45f, 0.2f, 0.6f),
			CustomMinimumSize = new Vector2(0, 1),
			SizeFlagsHorizontal = Control.SizeFlags.Fill,
		};
		vbox.AddChild(divider);

		// ── Entry buttons ─────────────────────────────────────────────────────
		var scroll = new ScrollContainer
		{
			SizeFlagsVertical    = Control.SizeFlags.Expand | Control.SizeFlags.Fill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
		};
		vbox.AddChild(scroll);

		var buttonList = new VBoxContainer();
		scroll.AddChild(buttonList);

		_buttons = new Button[_entries.Length];
		for (int i = 0; i < _entries.Length; i++)
		{
			int   idx    = i; // capture for lambda
			var   entry  = _entries[i];
			var   btn    = new Button
			{
				Text      = $"{entry.Date}  —  {entry.Title}",
				FocusMode = Control.FocusModeEnum.All,
				Flat      = true,
			};
			btn.AddThemeFontSizeOverride("font_size", 13);
			btn.Pressed += () => OnEntrySelected(idx);
			buttonList.AddChild(btn);
			_buttons[i] = btn;
		}

		// ── Close hint ────────────────────────────────────────────────────────
		var hint = new Label
		{
			Text                = "[X] Close",
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		hint.AddThemeColorOverride("font_color", Colors.Yellow);
		hint.AddThemeFontSizeOverride("font_size", 11);
		vbox.AddChild(hint);

		if (_buttons.Length > 0)
			_buttons[0].GrabFocus();
	}

	private void OnEntrySelected(int index)
	{
		_panel.Visible = false;
		var entry  = _entries[index];
		var popup  = new JournalEntryPopup(entry.Date, entry.Title, entry.Body);
		popup.Closed += OnEntryPopupClosed;
		AddChild(popup);
	}

	private void OnEntryPopupClosed()
	{
		_panel.Visible = true;
		if (_buttons.Length > 0) _buttons[0].GrabFocus();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_panel.Visible) return; // entry popup is open, don't intercept

		if (@event.IsActionPressed("cancel"))
		{
			GetViewport().SetInputAsHandled();
			EmitSignal(SignalName.Closed);
			QueueFree();
			return;
		}

		// Let interact confirm the focused button
		if (@event.IsActionPressed("interact"))
		{
			foreach (var btn in _buttons)
			{
				if (btn.HasFocus())
				{
					btn.EmitSignal(Button.SignalName.Pressed);
					GetViewport().SetInputAsHandled();
					return;
				}
			}
		}
	}
}
