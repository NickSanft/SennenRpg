using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// "Follow the melody" — player presses lane keys (lane_0–lane_3) in the preset order.
/// Preset sequence: { 0, 2, 1, 3 }.
/// </summary>
public partial class LullabyMinigame : BardMinigameBase
{
    public override string SkillName        => "Lullaby";
    public override string SkillDescription => "Press the lane keys in sequence.";

    private static readonly int[] LaneSequence = { 0, 2, 1, 3 };

    private int  _currentNote;
    private int  _successCount;
    private int  _beatsElapsed;
    private bool _finished;
    private bool _lastWasWrong;

    protected override void OnActivate()
    {
        _currentNote  = 0;
        _successCount = 0;
        _beatsElapsed = 0;
        _finished     = false;
        _lastWasWrong = false;
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
        QueueRedraw();

        // Auto-finish after 6 beats with no completion
        if (_beatsElapsed >= 6)
            Finish();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_finished || !Visible) return;

        for (int lane = 0; lane < 4; lane++)
        {
            if (!e.IsActionPressed($"lane_{lane}")) continue;

            bool correct = lane == LaneSequence[_currentNote];
            if (correct)
                _successCount++;
            else
                _lastWasWrong = true;

            _currentNote++;
            GetViewport().SetInputAsHandled();
            QueueRedraw();

            if (_currentNote >= LaneSequence.Length)
                Finish();

            return;
        }
    }

    public override void _Draw()
    {
        // Dark background
        DrawRect(new Rect2(0f, 0f, Size.X, Size.Y), new Color(0.06f, 0.06f, 0.10f, 1f));

        float boxSize  = 40f;
        float spacing  = 12f;
        float totalW   = 4 * boxSize + 3 * spacing;
        float startX   = (Size.X - totalW) * 0.5f;
        float centerY  = Size.Y * 0.5f - boxSize * 0.5f;

        float phase = RhythmClock.Instance.BeatPhase;
        float pulse = Mathf.Max(0f, 1f - phase * 3f);

        for (int i = 0; i < 4; i++)
        {
            float x    = startX + i * (boxSize + spacing);
            var   rect = new Rect2(x, centerY, boxSize, boxSize);
            int   lane = LaneSequence[i];
            var   baseColor = ObstacleBase.LaneColors[lane];

            if (i < _currentNote)
            {
                // Completed note — dim
                DrawRect(rect, baseColor with { A = 0.3f });
                DrawRect(rect, baseColor with { A = 0.5f }, filled: false, width: 2f);
            }
            else if (i == _currentNote)
            {
                // Current expected note — bright pulsing
                float bright = 0.6f + pulse * 0.4f;
                DrawRect(rect, baseColor with { A = bright });
                DrawRect(rect, Colors.White with { A = 0.8f + pulse * 0.2f }, filled: false, width: 3f);
            }
            else
            {
                // Future note — medium
                DrawRect(rect, baseColor with { A = 0.5f });
                DrawRect(rect, baseColor with { A = 0.7f }, filled: false, width: 1f);
            }

            // Lane number label
            DrawString(ThemeDB.FallbackFont, new Vector2(x + boxSize * 0.5f - 5f, centerY + boxSize * 0.5f + 6f),
                       $"{lane}", HorizontalAlignment.Left, -1, 12, Colors.White);
        }

        // Wrong flash
        if (_lastWasWrong)
        {
            DrawRect(new Rect2(0f, 0f, Size.X, Size.Y), new Color(1f, 0f, 0f, 0.15f));
            _lastWasWrong = false;
        }

        // Instruction label
        DrawString(ThemeDB.FallbackFont, new Vector2(Size.X * 0.5f - 60f, centerY - 18f),
                   "Press lanes in order", HorizontalAlignment.Left, -1, 11, Colors.White with { A = 0.8f });
    }

    public override void _Process(double _delta)
    {
        if (Visible && !_finished)
            QueueRedraw();
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        RhythmClock.Instance.Beat -= OnBeat;

        HitGrade grade = _successCount >= 4 ? HitGrade.Perfect
                       : _successCount >= 2 ? HitGrade.Good
                       : HitGrade.Miss;

        GD.Print($"[Lullaby] successCount={_successCount}/4, grade={grade}");
        Complete(grade);
    }
}
