using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// "Perfect timing" — must press interact/ui_accept exactly on the beat (within 60ms)
/// for 3 consecutive opportunities over 4 beats.
/// </summary>
public partial class SerenadeMinigame : BardMinigameBase
{
    public override string SkillName        => "Serenade";
    public override string SkillDescription => "Press exactly on the beat, 3 times.";

    private const float TightWindowSec = 0.060f;
    private const int   TotalOpportunities = 3;

    private int  _beatsElapsed;
    private int  _successCount;
    private bool _pressedThisBeat;
    private bool _finished;

    protected override void OnActivate()
    {
        _beatsElapsed     = 0;
        _successCount     = 0;
        _pressedThisBeat  = false;
        _finished         = false;
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

        _beatsElapsed++;
        _pressedThisBeat = false;
        QueueRedraw();

        // 4 beats total (3 opportunities + 1 extra for last beat press)
        if (_beatsElapsed >= 4)
            Finish();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_finished || !Visible) return;
        if (!e.IsActionPressed("interact") && !e.IsActionPressed("ui_accept")) return;
        if (_pressedThisBeat) return; // one press per beat only

        _pressedThisBeat = true;

        // Check timing: deviation from nearest beat boundary
        float phase     = RhythmClock.Instance.BeatPhase;
        float deviation = Mathf.Min(phase, 1f - phase) * RhythmClock.Instance.BeatInterval;

        if (deviation <= TightWindowSec)
            _successCount++;

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
        DrawRect(new Rect2(0f, 0f, Size.X, Size.Y), new Color(0.06f, 0.06f, 0.10f, 1f));

        float phase  = RhythmClock.Instance.BeatPhase;
        float pulse  = Mathf.Max(0f, 1f - phase * 3f);
        float startX = Size.X * 0.15f;
        float spacing = Size.X * 0.7f / (TotalOpportunities - 1);
        float ringY  = Size.Y * 0.45f;

        // 3 ring indicators showing upcoming beat slots
        for (int i = 0; i < TotalOpportunities; i++)
        {
            float cx       = startX + i * spacing;
            bool  isActive = i == Mathf.Clamp(_beatsElapsed, 0, TotalOpportunities - 1);
            bool  isDone   = i < _beatsElapsed;

            if (isDone)
            {
                // Completed slot — small dim ring
                DrawArc(new Vector2(cx, ringY), 10f, 0f, Mathf.Tau, 24,
                        Colors.Green with { A = 0.5f }, 2f);
            }
            else if (isActive)
            {
                // Current slot — convergence ring like RhythmStrike
                float outerR = Mathf.Lerp(28f, 12f, phase);
                DrawArc(new Vector2(cx, ringY), outerR, 0f, Mathf.Tau, 48,
                        Colors.White with { A = 0.6f }, 2f);
                DrawArc(new Vector2(cx, ringY), 12f, 0f, Mathf.Tau, 48,
                        Colors.Yellow with { A = 0.4f + pulse * 0.6f }, 3f);
            }
            else
            {
                // Future slot — dim
                DrawArc(new Vector2(cx, ringY), 16f, 0f, Mathf.Tau, 24,
                        Colors.White with { A = 0.2f }, 1f);
            }
        }

        // Success indicator
        DrawString(ThemeDB.FallbackFont, new Vector2(Size.X * 0.5f - 30f, ringY + 30f),
                   $"{_successCount}/{TotalOpportunities} on beat", HorizontalAlignment.Left, -1, 11,
                   Colors.White with { A = 0.8f });

        // Instruction
        DrawString(ThemeDB.FallbackFont, new Vector2(Size.X * 0.5f - 65f, Size.Y - 12f),
                   "Press EXACTLY on the beat!", HorizontalAlignment.Left, -1, 11,
                   Colors.White with { A = 0.8f });
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        RhythmClock.Instance.Beat -= OnBeat;

        HitGrade grade = _successCount >= 3 ? HitGrade.Perfect
                       : _successCount >= 2 ? HitGrade.Good
                       : HitGrade.Miss;

        GD.Print($"[Serenade] successes={_successCount}/3, grade={grade}");
        Complete(grade);
    }
}
