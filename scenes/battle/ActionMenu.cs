using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

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

        // Ensure all focus modes for gamepad navigation
        _fightButton.FocusMode   = Control.FocusModeEnum.All;
        _performButton.FocusMode = Control.FocusModeEnum.All;
        _itemButton.FocusMode    = Control.FocusModeEnum.All;
        _fleeButton.FocusMode    = Control.FocusModeEnum.All;

        _fightButton.Pressed   += () => { AudioManager.Instance?.PlaySfx(UiSfx.Confirm); EmitSignal(SignalName.FightSelected); };
        _performButton.Pressed += () => { AudioManager.Instance?.PlaySfx(UiSfx.Confirm); EmitSignal(SignalName.PerformSelected); };
        _itemButton.Pressed    += () => { AudioManager.Instance?.PlaySfx(UiSfx.Confirm); EmitSignal(SignalName.ItemSelected); };
        _fleeButton.Pressed    += () => { AudioManager.Instance?.PlaySfx(UiSfx.Confirm); EmitSignal(SignalName.FleeSelected); };

        // Cursor SFX on focus
        _fightButton.FocusEntered   += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
        _performButton.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
        _itemButton.FocusEntered    += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
        _fleeButton.FocusEntered    += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
    }

    public void FocusFirst() => _fightButton.GrabFocus();

    /// <summary>Overrides the Flee button label text (e.g. "Flee (72%)").</summary>
    public void SetFleeLabel(string text) => _fleeButton.Text = text;

    /// <summary>Slide in from the bottom. Call before FocusFirst.</summary>
    public void SlideIn()
    {
        Visible = true;
        float restY = Position.Y;
        Position = new Vector2(Position.X, restY + 60f);
        Modulate = Colors.Transparent;
        var t = CreateTween().SetParallel();
        t.TweenProperty(this, "position:y", restY, 0.2f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        t.TweenProperty(this, "modulate:a", 1f, 0.15f);
    }

    /// <summary>Slide out downward and hide.</summary>
    public async void SlideOut()
    {
        var t = CreateTween().SetParallel();
        t.TweenProperty(this, "position:y", Position.Y + 60f, 0.15f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
        t.TweenProperty(this, "modulate:a", 0f, 0.12f);
        await ToSignal(t, Tween.SignalName.Finished);
        Visible = false;
        // Reset position for next slide-in
        Position = new Vector2(Position.X, Position.Y - 60f);
        Modulate = Colors.White;
    }
}
