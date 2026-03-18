using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Horizontal note-highway arena that replaces DodgeBox.
/// Four lanes (rows) scroll left → right. The player presses lane_0–lane_3
/// when notes reach the HitZone on the right side.
///
/// Arena is centred at its Node2D origin.
///   Width  = 224 px  (ArenaHalfW × 2)
///   Height = 144 px  (4 lanes × 36 px)
///   HitZone at X = +90 (relative to centre)
///   Spawn   at X = -96 (off-screen left)
///   Travel distance = 186 px
///   At BeatsUntilArrival=3, BPM 180 → 1.0 s → speed 186 px/s
/// </summary>
public partial class RhythmArena : Node2D
{
    [Signal] public delegate void PhaseEndedEventHandler();
    [Signal] public delegate void PlayerHurtEventHandler(int damage);
    [Signal] public delegate void NoteHitEventHandler(int grade);

    // ── Arena geometry constants ──────────────────────────────────────
    public const float ArenaHalfW  = 112f;
    public const float ArenaHalfH  = 18f;
    public const float HitZoneX    = 90f;
    public const float SpawnX      = -96f;
    public const int   BeatsUntilArrival = 3;
    public const float LaneHeight  = 36f;

    /// <summary>Y-centre of each lane relative to arena centre.</summary>
    public static readonly float[] LaneCenterY = { 0f };

    // ── Hit-window constants (pixels) ─────────────────────────────────
    /// <summary>Pixel distance from HitZone that corresponds to GoodWindowSec at base speed.</summary>
    public const float GoodWindowPx  = 22f;
    /// <summary>Pixel distance that corresponds to PerfectWindowSec at base speed.</summary>
    public const float PerfectWindowPx = 9f;
    /// <summary>Extra pixels past GoodWindowPx before a note is irrecoverable.</summary>
    private const float MissGracePx  = 10f;

    // ── Node references ───────────────────────────────────────────────
    public Node2D ObstacleContainer { get; private set; } = null!;

    // ── State ─────────────────────────────────────────────────────────
    private bool              _running;
    private RhythmPatternBase? _activePattern;
    private Label?             _feedbackLabel;

    private PackedScene _standardObstacleScene = null!;

    public override void _Ready()
    {
        _standardObstacleScene = GD.Load<PackedScene>("res://scenes/battle/rhythm/StandardObstacle.tscn");

        ObstacleContainer = new Node2D();
        ObstacleContainer.Name = "ObstacleContainer";
        AddChild(ObstacleContainer);

        // Feedback label ("PERFECT", "GOOD", "MISS")
        _feedbackLabel = new Label();
        _feedbackLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _feedbackLabel.AddThemeFontSizeOverride("font_size", 14);
        _feedbackLabel.Position = new Vector2(-40f, -ArenaHalfH - 24f);
        _feedbackLabel.Visible  = false;
        AddChild(_feedbackLabel);

        Visible = false;
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>Start the rhythm phase with the given pattern scene for totalMeasures measures.</summary>
    public void StartPhase(PackedScene? patternScene, int totalMeasures = 2)
    {
        _running = true;
        Visible  = true;
        QueueRedraw();

        if (patternScene != null)
        {
            _activePattern = patternScene.Instantiate<RhythmPatternBase>();
            _activePattern.Initialize(this, totalMeasures);
            _activePattern.PatternFinished += OnPatternFinished;
            ObstacleContainer.AddChild(_activePattern);
        }
        else
        {
            // No pattern — end after the specified duration
            float dur = totalMeasures * RhythmConstants.BeatsPerMeasure
                        * RhythmClock.Instance.BeatInterval;
            GetTree().CreateTimer(dur).Timeout += EndPhase;
        }
    }

    /// <summary>Spawn a StandardObstacle in the given lane, arriving in beatsUntilArrival beats.</summary>
    public void CreateObstacle(int lane, int beatsUntilArrival, int damage)
    {
        if (lane < 0 || lane >= LaneCenterY.Length) return;

        var obs = _standardObstacleScene.Instantiate<StandardObstacle>();
        obs.Lane = lane;
        obs.Damage = damage;

        // Compute speed: the note must travel (HitZoneX - SpawnX) in exactly
        // beatsUntilArrival beats, so speed = distance / time.
        float travelTime = RhythmClock.Instance.BeatInterval * beatsUntilArrival;
        obs.TravelSpeed  = (HitZoneX - SpawnX) / travelTime;

        obs.Position = new Vector2(SpawnX, LaneCenterY[lane]);
        ObstacleContainer.AddChild(obs);
    }

    // ── Per-frame processing ──────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (!_running) return;
        QueueRedraw(); // update beat-pulse visual each frame

        float dt      = (float)delta;
        float missX   = HitZoneX + GoodWindowPx + MissGracePx;

        foreach (Node child in ObstacleContainer.GetChildren())
        {
            if (child is not StandardObstacle obs || obs.IsResolved) continue;

            // Move rightward
            obs.Position += new Vector2(obs.TravelSpeed * dt, 0f);

            // Past the miss threshold → unrecoverable, apply damage
            if (obs.Position.X > missX)
            {
                EmitSignal(SignalName.PlayerHurt, obs.Damage);
                obs.Resolve(HitGrade.Miss);
            }
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!_running) return;

        for (int lane = 0; lane < LaneCenterY.Length; lane++)
        {
            if (!e.IsActionPressed($"lane_{lane}")) continue;

            // Find the closest unresolved obstacle in this lane near the HitZone
            ObstacleBase? best     = null;
            float         bestDist = float.MaxValue;

            foreach (Node child in ObstacleContainer.GetChildren())
            {
                if (child is not StandardObstacle obs) continue;
                if (obs.IsResolved || obs.Lane != lane) continue;

                float dist = Mathf.Abs(obs.Position.X - HitZoneX);
                if (dist <= GoodWindowPx + MissGracePx && dist < bestDist)
                {
                    best     = obs;
                    bestDist = dist;
                }
            }

            if (best != null)
            {
                // Convert pixel deviation → seconds for grade calculation
                float deviationSec = bestDist / best.TravelSpeed;
                var   grade        = RhythmConstants.GradeDeviation(deviationSec);
                best.Resolve(grade);
                EmitSignal(SignalName.NoteHit, (int)grade);
                ShowFeedback(grade, lane);
            }
            // No obstacle nearby → false press, no effect (good UX)

            GetViewport().SetInputAsHandled();
        }
    }

    // ── Visuals ───────────────────────────────────────────────────────

    public override void _Draw()
    {
        var halfV = new Vector2(ArenaHalfW, ArenaHalfH);
        var bg    = new Rect2(-halfV, halfV * 2f);

        // Dark background
        DrawRect(bg, new Color(0.06f, 0.06f, 0.10f, 1f));
        // White border
        DrawRect(bg, Colors.White, filled: false, width: 2f);

        // Lane dividers
        for (int i = 1; i < LaneCenterY.Length; i++)
        {
            float y = LaneCenterY[i] - LaneHeight * 0.5f;
            DrawLine(new Vector2(-ArenaHalfW, y), new Vector2(ArenaHalfW, y),
                     new Color(1, 1, 1, 0.15f), 1f);
        }

        // Lane colour strips on the left edge
        for (int i = 0; i < LaneCenterY.Length; i++)
        {
            var col  = ObstacleBase.LaneColors[i] with { A = 0.25f };
            DrawRect(new Rect2(-ArenaHalfW, LaneCenterY[i] - LaneHeight * 0.5f, 20f, LaneHeight), col);
        }

        // Hit zone vertical line
        DrawLine(new Vector2(HitZoneX, -ArenaHalfH),
                 new Vector2(HitZoneX,  ArenaHalfH),
                 Colors.White with { A = 0.8f }, 2f);

        // Beat pulse: brighter hit zone glow on the beat (BeatPhase near 0)
        float pulse = Mathf.Max(0f, 1f - RhythmClock.Instance.BeatPhase * 4f);
        if (pulse > 0f)
        {
            DrawLine(new Vector2(HitZoneX, -ArenaHalfH),
                     new Vector2(HitZoneX,  ArenaHalfH),
                     Colors.White with { A = pulse * 0.6f }, 6f);
        }
    }

    // ── Internal helpers ──────────────────────────────────────────────

    private void ShowFeedback(HitGrade grade, int lane)
    {
        if (_feedbackLabel == null) return;

        (_feedbackLabel.Text, _feedbackLabel.Modulate) = grade switch
        {
            HitGrade.Perfect => ("PERFECT!", Colors.Yellow),
            HitGrade.Good    => ("GOOD",     Colors.White),
            _                => ("MISS",     Colors.Red),
        };
        _feedbackLabel.Position = new Vector2(-30f, LaneCenterY[lane] - 20f);
        _feedbackLabel.Visible  = true;

        GetTree().CreateTimer(0.4f).Timeout += () => { if (_feedbackLabel != null) _feedbackLabel.Visible = false; };
    }

    private void OnPatternFinished()
    {
        // Give 2 extra beats of grace for in-flight obstacles then end
        float grace = 2f * RhythmClock.Instance.BeatInterval;
        GetTree().CreateTimer(grace).Timeout += EndPhase;
    }

    private void EndPhase()
    {
        _running = false;

        // Free all remaining obstacles
        foreach (Node child in ObstacleContainer.GetChildren())
            child.QueueFree();

        GD.Print("[RhythmArena] Phase ended.");
        Visible = false;
        EmitSignal(SignalName.PhaseEnded);
    }
}
