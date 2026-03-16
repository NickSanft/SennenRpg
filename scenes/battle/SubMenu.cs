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
		// Clear old buttons
		foreach (Node child in _list.GetChildren())
			child.QueueFree();

		for (int i = 0; i < options.Length; i++)
		{
			int captured = i;
			var btn = new Button();
			btn.Text = options[i];
			btn.Pressed += () => EmitSignal(SignalName.OptionSelected, captured);
			_list.AddChild(btn);
		}

		// Auto-focus first item
		if (_list.GetChildCount() > 0)
			(_list.GetChild(0) as Button)?.GrabFocus();
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
