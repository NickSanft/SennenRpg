using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// The four-button action menu shown during the player's turn.
/// Emits a signal for each button press. BattleScene connects to these.
/// </summary>
public partial class ActionMenu : Control
{
	[Signal] public delegate void FightSelectedEventHandler();
	[Signal] public delegate void ActSelectedEventHandler();
	[Signal] public delegate void ItemSelectedEventHandler();
	[Signal] public delegate void MercySelectedEventHandler();

	private Button _fightButton = null!;
	private Button _actButton   = null!;
	private Button _itemButton  = null!;
	private Button _mercyButton = null!;

	public override void _Ready()
	{
		_fightButton = GetNode<Button>("GridContainer/FightButton");
		_actButton   = GetNode<Button>("GridContainer/ActButton");
		_itemButton  = GetNode<Button>("GridContainer/ItemButton");
		_mercyButton = GetNode<Button>("GridContainer/MercyButton");

		_fightButton.Pressed += () => EmitSignal(SignalName.FightSelected);
		_actButton.Pressed   += () => EmitSignal(SignalName.ActSelected);
		_itemButton.Pressed  += () => EmitSignal(SignalName.ItemSelected);
		_mercyButton.Pressed += () => EmitSignal(SignalName.MercySelected);
	}

	/// <summary>Grab keyboard focus on the first button when the menu becomes active.</summary>
	public void FocusFirst() => _fightButton.GrabFocus();
}
