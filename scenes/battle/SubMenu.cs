using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// A dynamic vertical list of options used for Act choices and Item lists.
/// Call Populate() to fill it, then make it visible.
/// Emits OptionSelected(int index) or Cancelled on ui_cancel.
/// </summary>
public partial class SubMenu : Control
{
	[Signal] public delegate void OptionSelectedEventHandler(int index);
	[Signal] public delegate void CancelledEventHandler();

	private VBoxContainer _list = null!;

	public override void _Ready()
	{
		_list = GetNode<VBoxContainer>("VBoxContainer");
	}

	public void Populate(string[] options)
	{
		// Clear old buttons immediately (not deferred) to avoid stale child issues
		foreach (Node child in _list.GetChildren())
		{
			_list.RemoveChild(child);
			child.QueueFree();
		}

		var buttons = new System.Collections.Generic.List<Button>();
		for (int i = 0; i < options.Length; i++)
		{
			int captured = i;
			var btn = new Button();
			btn.Text = options[i];
			btn.Pressed += () => EmitSignal(SignalName.OptionSelected, captured);
			// Ensure all focus modes are enabled for gamepad navigation
			btn.FocusMode = Control.FocusModeEnum.All;
			_list.AddChild(btn);
			buttons.Add(btn);
		}

		// Set explicit focus neighbors for circular navigation
		for (int i = 0; i < buttons.Count; i++)
		{
			var prev = buttons[(i - 1 + buttons.Count) % buttons.Count];
			var next = buttons[(i + 1) % buttons.Count];
			buttons[i].FocusNeighborTop    = prev.GetPath();
			buttons[i].FocusNeighborBottom = next.GetPath();
		}

		// Auto-focus first item (deferred so layout is ready)
		if (buttons.Count > 0)
			buttons[0].CallDeferred(Control.MethodName.GrabFocus);
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;
		if (@event.IsActionPressed("ui_cancel"))
		{
			EmitSignal(SignalName.Cancelled);
			GetViewport().SetInputAsHandled();
		}
	}
}
