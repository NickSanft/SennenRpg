using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// "Off-beat" — player must press interact/ui_accept BETWEEN beats
/// (when BeatPhase is between 0.30 and 0.70). 3 beat windows over 4 beats.
/// </summary>
public partial class DissonanceMinigame : BardMinigameBase
{
    public override string SkillName        => "Dissonance";
    public override string SkillDescription => "Press between the beats.";

    private const float SafeZoneLow  = 0.30f;
    private const float SafeZoneHigh = 0.70f;

    private int  _beatsElapsed;
    private int  _successfulHits;
    private int  _pressedThisBeat;  // count of presses this beat window
    private bool _finished;

    protected override void OnActivate()
    {
        _beatsElapsed    = 0;
        _successfulHits  = 0;
        _pressedThisBeat = 0;
        _finished        = false;
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
        _pressedThisBeat = 0;
        QueueRedraw();

        if (_beatsElapsed >= 4)
            Finish();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_finished || !Visible) return;
        if (!e.IsActionPressed("interact") && !e.IsActionPressed("ui_accept")) return;
        if (_pressedThisBeat > 0) return; // one press per beat window

        _pressedThisBeat++;
        float phase = RhythmClock.Instance.BeatPhase;

        if (phase >= SafeZoneLow && phase <= SafeZoneHigh)
            _successfulHits++;
        // On-beat press (phase < 0.30 or > 0.70) counts as a miss attempt — no increment

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

        float phase   = RhythmClock.Instance.BeatPhase;
        float timelineW = Size.X * 0.8f;
        float timelineH = 16f;
        float timelineX = (Size.X - timelineW) * 0.5f;
        float timelineY = Size.Y * 0.45f - timelineH * 0.5f;

        // Timeline background
        DrawRect(new Rect2(timelineX, timelineY, timelineW, timelineH),
                 new Color(0.15f, 0.15f, 0.20f, 1f));

        // SAFE zone highlight (0.30–0.70 of the timeline)
        float safeX = timelineX + timelineW * SafeZoneLow;
        float safeW = timelineW * (SafeZoneHigh - SafeZoneLow);
        DrawRect(new Rect2(safeX, timelineY, safeW, timelineH),
                 new Color(0.1f, 0.8f, 0.3f, 0.35f));

        // "SAFE" label above the zone
        DrawString(ThemeDB.FallbackFont, new Vector2(safeX + safeW * 0.5f - 12f, timelineY - 12f),
                   "SAFE", HorizontalAlignment.Left, -1, 10, new Color(0.2f, 1f, 0.5f, 0.9f));

        // Timeline border
        DrawRect(new Rect2(timelineX, timelineY, timelineW, timelineH),
                 Colors.White with { A = 0.4f }, filled: false, width: 1f);

        // Cursor showing current BeatPhase
        float cursorX = timelineX + timelineW * phase;
        DrawLine(new Vector2(cursorX, timelineY - 4f),
                 new Vector2(cursorX, timelineY + timelineH + 4f),
                 Colors.White, 3f);

        // Beat tick marks
        DrawLine(new Vector2(timelineX, timelineY - 2f),
                 new Vector2(timelineX, timelineY + timelineH + 2f),
                 Colors.White with { A = 0.6f }, 2f);
        DrawLine(new Vector2(timelineX + timelineW, timelineY - 2f),
                 new Vector2(timelineX + timelineW, timelineY + timelineH + 2f),
                 Colors.White with { A = 0.6f }, 2f);

        // Success count
        DrawString(ThemeDB.FallbackFont, new Vector2(Size.X * 0.5f - 25f, timelineY + timelineH + 20f),
                   $"{_successfulHits}/3 hits", HorizontalAlignment.Left, -1, 11,
                   Colors.White with { A = 0.8f });

        // Instruction
        DrawString(ThemeDB.FallbackFont, new Vector2(Size.X * 0.5f - 80f, Size.Y - 12f),
                   "Press BETWEEN the beats", HorizontalAlignment.Left, -1, 11,
                   Colors.White with { A = 0.8f });
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        RhythmClock.Instance.Beat -= OnBeat;

        HitGrade grade = _successfulHits >= 3 ? HitGrade.Perfect
                       : _successfulHits >= 2 ? HitGrade.Good
                       : HitGrade.Miss;

        GD.Print($"[Dissonance] successfulHits={_successfulHits}/3, grade={grade}");
        Complete(grade);
    }
}
