using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Shadow Bolt charge-up ring.
/// A cursor sweeps around a circle; press Z/Enter when it aligns with the highlighted arc.
///   Gold arc = Perfect zone.
///   Blue arc = Good zone (wider, surrounding the gold arc).
///   Outside both arcs = Miss.
/// If the timer (TotalBeats beats) expires without a press, the cast misses automatically.
/// </summary>
public partial class ShadowBoltMinigame : BardMinigameBase
{
    public override string SkillName        => "Shadow Bolt";
    public override string SkillDescription => "Time your cast — strike the arc!";

    // ── Tuning constants ──────────────────────────────────────────────────────
    private const float RingRadius      = 60f;
    private const float PerfectHalfArc  = 0.18f; // radians (~10°)
    private const float GoodHalfArc     = 0.40f; // radians (~23°)
    private const int   TotalBeats      = 6;      // time window before auto-miss

    // ── State ─────────────────────────────────────────────────────────────────
    private float _cursorAngle;    // current cursor position, radians
    private float _targetAngle;    // sweet-spot centre, radians
    private float _rotationSpeed;  // radians per second
    private int   _beatsRemaining;
    private bool  _finished;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnActivate()
    {
        // One full rotation every 4 beats
        _rotationSpeed  = Mathf.Tau / (RhythmClock.Instance.BeatInterval * 4f);
        _cursorAngle    = -Mathf.Pi * 0.5f;          // start at 12 o'clock
        _targetAngle    = (float)GD.RandRange(0.0, Mathf.Tau);
        _beatsRemaining = TotalBeats;
        _finished       = false;

        RhythmClock.Instance.Beat += OnBeat;
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        RhythmClock.Instance.Beat -= OnBeat;
    }

    // ── Tick / input ──────────────────────────────────────────────────────────

    private void OnBeat(int _)
    {
        if (_finished) return;
        _beatsRemaining--;
        if (_beatsRemaining <= 0) Resolve(HitGrade.Miss);
    }

    public override void _Process(double delta)
    {
        if (!Visible || _finished) return;
        _cursorAngle = (_cursorAngle + _rotationSpeed * (float)delta) % Mathf.Tau;
        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_finished || !Visible) return;
        if (!e.IsActionPressed("interact") && !e.IsActionPressed("ui_accept")) return;

        GetViewport().SetInputAsHandled();

        float diff  = AngleDiff(_cursorAngle, _targetAngle);
        float absDiff = Mathf.Abs(diff);
        HitGrade grade = absDiff <= PerfectHalfArc ? HitGrade.Perfect
                       : absDiff <= GoodHalfArc    ? HitGrade.Good
                       :                             HitGrade.Miss;
        Resolve(grade);
    }

    private void Resolve(HitGrade grade)
    {
        if (_finished) return;
        _finished = true;
        RhythmClock.Instance.Beat -= OnBeat;
        GD.Print($"[ShadowBolt] grade={grade}  cursor={Mathf.RadToDeg(_cursorAngle):F1}°  " +
                 $"target={Mathf.RadToDeg(_targetAngle):F1}°");
        Complete(grade);
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        var center = VP * 0.5f;

        // Background
        DrawRect(new Rect2(Vector2.Zero, VP), new Color(0.04f, 0.04f, 0.10f, 1f));

        // Dim track ring
        DrawArc(center, RingRadius, 0f, Mathf.Tau, 64, new Color(1f, 1f, 1f, 0.15f), 3f);

        // Good arc — blue/purple glow
        DrawArc(center, RingRadius,
                _targetAngle - GoodHalfArc, _targetAngle + GoodHalfArc,
                32, new Color(0.4f, 0.4f, 1.0f, 0.75f), 10f);

        // Perfect arc — bright gold, thicker
        DrawArc(center, RingRadius,
                _targetAngle - PerfectHalfArc, _targetAngle + PerfectHalfArc,
                16, new Color(1.0f, 0.85f, 0.0f, 0.95f), 14f);

        // Rotating cursor line + dot
        float beatPulse = Mathf.Max(0f, 1f - RhythmClock.Instance.BeatPhase * 5f);
        float tipLen    = RingRadius + 12f + beatPulse * 5f;
        var   dir       = new Vector2(Mathf.Cos(_cursorAngle), Mathf.Sin(_cursorAngle));
        DrawLine(center, center + dir * tipLen, Colors.White, 3f);
        DrawCircle(center + dir * tipLen, 5f + beatPulse * 2f, Colors.White);

        // Centre orb
        DrawCircle(center, 10f, new Color(0.45f, 0.25f, 0.9f, 0.7f));
        DrawCircle(center,  5f, new Color(0.75f, 0.55f, 1.0f, 0.9f));

        // Title
        DrawString(ThemeDB.FallbackFont,
                   new Vector2(center.X - 48f, center.Y - RingRadius - 22f),
                   "SHADOW BOLT", HorizontalAlignment.Left, -1, 14,
                   new Color(0.7f, 0.5f, 1.0f));

        // Countdown bar
        const float barW = 120f;
        const float barH = 8f;
        float ratio = Mathf.Clamp(_beatsRemaining / (float)TotalBeats, 0f, 1f);
        DrawRect(new Rect2(center.X - barW * 0.5f, VP.Y - 32f, barW, barH),
                 new Color(0.15f, 0.15f, 0.15f));
        DrawRect(new Rect2(center.X - barW * 0.5f, VP.Y - 32f, barW * ratio, barH),
                 new Color(0.5f, 0.3f, 1.0f));

        // Instruction
        DrawString(ThemeDB.FallbackFont,
                   new Vector2(center.X - 65f, VP.Y - 14f),
                   $"Press {Core.Extensions.InputMapExtensions.GetInputHint("interact", "Z")} to cast!", HorizontalAlignment.Left, -1, 10,
                   Colors.White with { A = 0.8f });
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Shortest signed angle difference (result in [-π, π]).</summary>
    private static float AngleDiff(float from, float to)
    {
        float diff = (to - from) % Mathf.Tau;
        if (diff >  Mathf.Pi) diff -= Mathf.Tau;
        if (diff < -Mathf.Pi) diff += Mathf.Tau;
        return diff;
    }
}
