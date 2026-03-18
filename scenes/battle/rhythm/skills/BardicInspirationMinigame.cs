using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// "Hold the note" — player must hold interact/ui_accept for 2 full beats.
/// </summary>
public partial class BardicInspirationMinigame : BardMinigameBase
{
    public override string SkillName        => "Bardic Inspiration";
    public override string SkillDescription => "Hold the note for 2 beats.";

    private int  _beatsElapsed;
    private int  _beatsHeld;
    private bool _holding;
    private bool _ready;
    private bool _finished;

    protected override void OnActivate()
    {
        _beatsElapsed = 0;
        _beatsHeld    = 0;
        _holding      = false;
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

        // Check if key was held since last beat
        if (_holding)
            _beatsHeld++;

        _beatsElapsed++;
        QueueRedraw();

        if (_beatsElapsed >= 4)
            Finish();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_finished || !Visible) return;

        if (e.IsActionPressed("interact") || e.IsActionPressed("ui_accept"))
        {
            _holding = true;
            QueueRedraw();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionReleased("interact") || e.IsActionReleased("ui_accept"))
        {
            _holding = false;
            QueueRedraw();
            GetViewport().SetInputAsHandled();
        }
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

        // Breath bar — fills while holding, uses BeatPhase for pulse glow
        float barW  = VP.X * 0.7f;
        float barH  = 20f;
        float barX  = (VP.X - barW) * 0.5f;
        float barY  = VP.Y * 0.5f - barH * 0.5f;

        DrawRect(new Rect2(barX, barY, barW, barH), new Color(0.2f, 0.2f, 0.3f, 1f));

        if (_holding)
        {
            float phase     = RhythmClock.Instance.BeatPhase;
            float fillRatio = Mathf.Clamp(_beatsElapsed / 2f + phase * 0.5f, 0f, 1f);
            float pulse     = Mathf.Max(0f, 1f - phase * 3f);
            DrawRect(new Rect2(barX, barY, barW * fillRatio, barH),
                     new Color(0.4f + pulse * 0.4f, 0.8f, 0.4f + pulse * 0.3f, 1f));
        }

        // Bar border
        DrawRect(new Rect2(barX, barY, barW, barH), Colors.White with { A = 0.6f }, filled: false, width: 2f);

        // Label
        DrawString(ThemeDB.FallbackFont, new Vector2(VP.X * 0.5f - 50f, barY - 12f),
                   "Hold Z / Enter", HorizontalAlignment.Left, -1, 13, Colors.White);

        // Beat counter
        DrawString(ThemeDB.FallbackFont, new Vector2(VP.X * 0.5f - 20f, barY + barH + 18f),
                   $"Beat {_beatsElapsed}/4", HorizontalAlignment.Left, -1, 11,
                   Colors.White with { A = 0.7f });
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        RhythmClock.Instance.Beat -= OnBeat;

        HitGrade grade = _beatsHeld >= 4 ? HitGrade.Perfect
                       : _beatsHeld >= 2 ? HitGrade.Good
                       : HitGrade.Miss;

        GD.Print($"[BardicInspiration] beatsHeld={_beatsHeld}/4, grade={grade}");
        Complete(grade);
    }
}
