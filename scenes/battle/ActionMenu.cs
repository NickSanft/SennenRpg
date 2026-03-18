using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Four-button action menu shown during the player's turn.
/// ACT has been renamed to PERFORM to reflect the Bard/musician theme.
/// </summary>
public partial class ActionMenu : Control
{
    [Signal] public delegate void FightSelectedEventHandler();
    [Signal] public delegate void PerformSelectedEventHandler();
    [Signal] public delegate void ItemSelectedEventHandler();
    [Signal] public delegate void MercySelectedEventHandler();

    private Button _fightButton   = null!;
    private Button _performButton = null!;
    private Button _itemButton    = null!;
    private Button _mercyButton   = null!;

    public override void _Ready()
    {
        _fightButton   = GetNode<Button>("GridContainer/FightButton");
        _performButton = GetNode<Button>("GridContainer/ActButton");
        _itemButton    = GetNode<Button>("GridContainer/ItemButton");
        _mercyButton   = GetNode<Button>("GridContainer/MercyButton");

        _fightButton.Pressed   += () => EmitSignal(SignalName.FightSelected);
        _performButton.Pressed += () => EmitSignal(SignalName.PerformSelected);
        _itemButton.Pressed    += () => EmitSignal(SignalName.ItemSelected);
        _mercyButton.Pressed   += () => EmitSignal(SignalName.MercySelected);
    }

    public void FocusFirst() => _fightButton.GrabFocus();
}
