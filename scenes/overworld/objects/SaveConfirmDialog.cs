using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Lightweight native save-confirmation popup — no Dialogic dependency.
/// Instantiated by SavePoint.Interact(); frees itself when dismissed.
/// Emits Confirmed or Cancelled.
/// </summary>
public partial class SaveConfirmDialog : CanvasLayer
{
	[Signal] public delegate void ConfirmedEventHandler();
	[Signal] public delegate void CancelledEventHandler();

	public override void _Ready()
	{
		Layer = 60; // Above PauseMenu (50), below SceneTransition (100)

		// ── Dim overlay ──────────────────────────────────────────────
		var overlay = new ColorRect
		{
			Color          = new Color(0, 0, 0, 0.55f),
			AnchorRight    = 1f,
			AnchorBottom   = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical   = Control.GrowDirection.Both,
		};
		AddChild(overlay);

		// ── Panel centered on screen ─────────────────────────────────
		var panel = new PanelContainer
		{
			AnchorLeft     = 0.5f, AnchorRight  = 0.5f,
			AnchorTop      = 0.5f, AnchorBottom = 0.5f,
			OffsetLeft     = -90f, OffsetRight  = 90f,
			OffsetTop      = -44f, OffsetBottom = 44f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical   = Control.GrowDirection.Both,
		};
		overlay.AddChild(panel);

		var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		panel.AddChild(vbox);

		var label = new Label
		{
			Text                = "Save your progress?",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		vbox.AddChild(label);

		var spacer = new Control { CustomMinimumSize = new Vector2(0, 8) };
		vbox.AddChild(spacer);

		var hbox = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		vbox.AddChild(hbox);

		var yesBtn = new Button { Text = "YES" };
		var noBtn  = new Button { Text = "NO" };
		hbox.AddChild(yesBtn);
		hbox.AddChild(noBtn);

		yesBtn.Pressed += OnYes;
		noBtn.Pressed  += OnNo;

		// Default focus on YES so keyboard/gamepad works immediately
		yesBtn.CallDeferred(Control.MethodName.GrabFocus);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// ESC / cancel dismisses without saving
		if (@event.IsActionPressed("cancel") || @event.IsActionPressed("menu"))
		{
			GetViewport().SetInputAsHandled();
			OnNo();
		}
	}

	private void OnYes()
	{
		EmitSignal(SignalName.Confirmed);
		QueueFree();
	}

	private void OnNo()
	{
		EmitSignal(SignalName.Cancelled);
		QueueFree();
	}
}
