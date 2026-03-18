using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Single-beat strike minigame that replaces FightBar.
/// The player presses ui_accept / interact on the beat to deal damage.
/// Grades against RhythmClock.BeatPhase (0.0 = perfect beat, wrap around at 1.0).
/// The minigame stays active for ActivationBeats beats; missing all = Miss.
/// </summary>
public partial class RhythmStrike : Control
{
    [Signal] public delegate void StrikeResolvedEventHandler(int grade);

    private const int   ActivationBeats = 2;
    private const float RingRadiusMin   = 12f;
    private const float RingRadiusMax   = 50f;

    private bool _active;
    private int  _beatsRemaining;
    private bool _hasResult;

    public override void _Ready()
    {
        Visible = false;
        RhythmClock.Instance.Beat += OnBeat;
    }

    public override void _ExitTree()
    {
        RhythmClock.Instance.Beat -= OnBeat;
    }

    /// <summary>Activate the strike window for ActivationBeats beats.</summary>
    public void Activate()
    {
        _active        = true;
        _hasResult     = false;
        _beatsRemaining = ActivationBeats;
        Visible        = true;
        QueueRedraw();
        GD.Print("[RhythmStrike] Activated.");
    }

    private void OnBeat(int _beatIndex)
    {
        if (!_active) return;
        _beatsRemaining--;
        QueueRedraw();

        if (_beatsRemaining <= 0 && !_hasResult)
        {
            // Ran out of beats without a press → Miss
            _active = false;
            Visible = false;
            EmitSignal(SignalName.StrikeResolved, (int)HitGrade.Miss);
        }
    }

    public override void _Process(double _delta)
    {
        if (_active) QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!_active || _hasResult) return;
        if (!e.IsActionPressed("interact") && !e.IsActionPressed("ui_accept")) return;

        _hasResult = true;
        _active    = false;
        Visible    = false;

        // Deviation from nearest beat boundary
        float phase     = RhythmClock.Instance.BeatPhase;
        float deviation = Mathf.Min(phase, 1f - phase) * RhythmClock.Instance.BeatInterval;
        var   grade     = RhythmConstants.GradeDeviation(deviation);

        GD.Print($"[RhythmStrike] Resolved. Phase={phase:F3}, deviation={deviation*1000:F1}ms, grade={grade}");
        EmitSignal(SignalName.StrikeResolved, (int)grade);
        GetViewport().SetInputAsHandled();
    }

    public override void _Draw()
    {
        if (!_active) return;

        float phase  = RhythmClock.Instance.BeatPhase;
        var   center = Size * 0.5f;

        // Outer convergence ring — shrinks from max to min over the beat
        float outerR = Mathf.Lerp(RingRadiusMax, RingRadiusMin, phase);
        DrawArc(center, outerR, 0, Mathf.Tau, 48, Colors.White with { A = 0.6f }, 2f);

        // Inner target ring — pulses bright at beat 0
        float pulse = Mathf.Max(0f, 1f - phase * 3f);
        DrawArc(center, RingRadiusMin, 0, Mathf.Tau, 48,
                Colors.Yellow with { A = 0.4f + pulse * 0.6f }, 3f);

        // Hit prompt
        DrawString(ThemeDB.FallbackFont, center + new Vector2(-18f, 22f),
                   "Press on beat!", HorizontalAlignment.Left, -1, 11, Colors.White with { A = 0.8f });
    }
}
