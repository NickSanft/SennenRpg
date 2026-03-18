using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// "Rapid tap" — player taps interact/ui_accept as fast as possible in 2 beats.
/// </summary>
public partial class WarCryMinigame : BardMinigameBase
{
    public override string SkillName        => "War Cry";
    public override string SkillDescription => "Tap as fast as you can!";

    private int  _tapCount;
    private int  _beatsElapsed;
    private bool _ready;
    private bool _finished;

    protected override void OnActivate()
    {
        _tapCount     = 0;
        _beatsElapsed = 0;
        _ready        = false;
        _finished     = false;
        RhythmClock.Instance.Beat += OnBeat;
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        RhythmClock.Instance.Beat -= OnBeat;
    }

    private void OnBeat(int _beatIndex)
    {
        if (_finished) return;
        if (!_ready) { _ready = true; QueueRedraw(); return; } // countdown beat
        _beatsElapsed++;
        QueueRedraw();

        if (_beatsElapsed >= 4)
            Finish();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_finished || !Visible) return;
        if (!e.IsActionPressed("interact") && !e.IsActionPressed("ui_accept")) return;

        _tapCount++;
        GetViewport().SetInputAsHandled();
        QueueRedraw();
    }

    public override void _Process(double _delta)
    {
        if (Visible && !_finished)
            QueueRedraw();
    }

    public override void _Draw()
    {
        // Dark background
        DrawRect(new Rect2(0f, 0f, VP.X, VP.Y), new Color(0.06f, 0.06f, 0.10f, 1f));

        float phase  = RhythmClock.Instance.BeatPhase;
        float pulse  = Mathf.Max(0f, 1f - phase * 4f);
        var   center = VP * 0.5f;

        // Progress circle around beat indicator
        float radius = 30f;
        float arc    = Mathf.Tau * Mathf.Clamp(_tapCount / 8f, 0f, 1f);
        DrawArc(center, radius, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + arc, 48,
                new Color(1f, 0.6f, 0.1f, 0.8f), 5f);
        DrawArc(center, radius, 0f, Mathf.Tau, 48, Colors.White with { A = 0.2f }, 2f);

        // Beat pulse indicator
        float pulseR = 10f + pulse * 6f;
        DrawCircle(center, pulseR, new Color(1f, 0.5f + pulse * 0.5f, 0.1f, 0.7f + pulse * 0.3f));

        // Large tap counter
        DrawString(ThemeDB.FallbackFont, new Vector2(center.X - 14f, center.Y + 6f),
                   $"{_tapCount}", HorizontalAlignment.Left, -1, 20, Colors.White);

        // "HITS" label
        DrawString(ThemeDB.FallbackFont, new Vector2(center.X - 12f, center.Y + 22f),
                   "HITS", HorizontalAlignment.Left, -1, 11, Colors.White with { A = 0.7f });

        // Instruction label
        DrawString(ThemeDB.FallbackFont, new Vector2(center.X - 55f, VP.Y - 12f),
                   "TAP FAST! Z / Enter", HorizontalAlignment.Left, -1, 11,
                   Colors.White with { A = 0.8f });
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        RhythmClock.Instance.Beat -= OnBeat;

        HitGrade grade = _tapCount >= 6 ? HitGrade.Perfect
                       : _tapCount >= 3 ? HitGrade.Good
                       : HitGrade.Miss;

        GD.Print($"[WarCry] taps={_tapCount}, grade={grade}");
        Complete(grade);
    }
}
