using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Four-button action menu shown during the player's turn.
/// </summary>
public partial class ActionMenu : Control
{
    [Signal] public delegate void FightSelectedEventHandler();
    [Signal] public delegate void PerformSelectedEventHandler();
    [Signal] public delegate void ItemSelectedEventHandler();
    [Signal] public delegate void FleeSelectedEventHandler();

    private Button _fightButton   = null!;
    private Button _performButton = null!;
    private Button _itemButton    = null!;
    private Button _fleeButton    = null!;

    public override void _Ready()
    {
        _fightButton   = GetNode<Button>("GridContainer/FightButton");
        _performButton = GetNode<Button>("GridContainer/ActButton");
        _itemButton    = GetNode<Button>("GridContainer/ItemButton");
        _fleeButton    = GetNode<Button>("GridContainer/FleeButton");

        _fightButton.Pressed   += () => EmitSignal(SignalName.FightSelected);
        _performButton.Pressed += () => EmitSignal(SignalName.PerformSelected);
        _itemButton.Pressed    += () => EmitSignal(SignalName.ItemSelected);
        _fleeButton.Pressed    += () => EmitSignal(SignalName.FleeSelected);
    }

    public void FocusFirst() => _fightButton.GrabFocus();

    /// <summary>Overrides the Flee button label text (e.g. "Flee (72%)").</summary>
    public void SetFleeLabel(string text) => _fleeButton.Text = text;
}
