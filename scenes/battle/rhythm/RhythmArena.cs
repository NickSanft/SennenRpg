using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using static SennenRpg.Core.Data.RhythmTimingWindow;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Horizontal note-highway arena.
/// Four lanes scroll left → right. The player presses lane_0–lane_3
/// when notes reach the HitZone on the right side.
///
/// Arena centred at its Node2D origin.
///   Width  = 224 px  (ArenaHalfW × 2)
///   Height = 144 px  (4 lanes × 36 px)
///   HitZone at X = +90   Spawn at X = -96
/// </summary>
public partial class RhythmArena : Node2D
{
    [Signal] public delegate void PhaseEndedEventHandler();
    [Signal] public delegate void PlayerHurtEventHandler(int damage);
    [Signal] public delegate void NoteHitEventHandler(int grade);

    // ── Arena geometry constants ──────────────────────────────────────
    public const float ArenaHalfW  = 112f;
    public const float ArenaHalfH  = 72f;
    public const float HitZoneX    = 90f;
    public const float SpawnX      = -96f;
    public const int   BeatsUntilArrival = 3;
    public const float LaneHeight  = 36f;

    /// <summary>Y-centre of each lane relative to arena centre.</summary>
    public static readonly float[] LaneCenterY = { -54f, -18f, 18f, 54f };

    // ── Hit-window constants (pixels) ─────────────────────────────────
    // Base values used when SettingsManager is unavailable (editor, tests).
    private const float BaseGoodWindowPx    = 22f;
    private const float BasePerfectWindowPx = 9f;
    private const float MissGracePx         = 10f;

    private RhythmTimingWindow TimingWindow
        => SettingsManager.Instance?.Current.RhythmTimingWindow ?? Normal;

    private float GoodWindowPx
        => SettingsLogic.RhythmGoodWindowPx(TimingWindow);

    private float PerfectWindowPx
        => SettingsLogic.RhythmPerfectWindowPx(TimingWindow);

    private bool IsAutoHit => TimingWindow == AutoHit;

    // ── Node references ───────────────────────────────────────────────
    public Node2D ObstacleContainer { get; private set; } = null!;

    // ── State ─────────────────────────────────────────────────────────
    private bool               _running;
    private RhythmPatternBase? _activePattern;
    private Label?             _feedbackLabel;
    private Label?             _comboLabel;
    private Label?             _breakLabel;

    private PackedScene  _standardObstacleScene = null!;
    private PackedScene? _holdObstacleScene;

    // Hold-note tracking (one slot per lane)
    private readonly HoldObstacle?[] _laneHeldObstacles = new HoldObstacle?[4];
    private readonly float[]         _laneHoldElapsed   = new float[4];

    // Combo / streak tracking
    private int _currentStreak;
    private int _maxStreak;

    /// <summary>Maximum consecutive hit streak this phase — read by BattleScene after phase ends.</summary>
    public int MaxStreak => _maxStreak;

    // ── Setup ─────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _standardObstacleScene = GD.Load<PackedScene>("res://scenes/battle/rhythm/StandardObstacle.tscn");
        const string holdPath  = "res://scenes/battle/rhythm/HoldObstacle.tscn";
        if (ResourceLoader.Exists(holdPath))
            _holdObstacleScene = GD.Load<PackedScene>(holdPath);

        ObstacleContainer      = new Node2D { Name = "ObstacleContainer" };
        AddChild(ObstacleContainer);

        _feedbackLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Position            = new Vector2(-40f, -ArenaHalfH - 24f),
            Visible             = false,
        };
        _feedbackLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_feedbackLabel);

        _comboLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Position            = new Vector2(ArenaHalfW - 4f, -ArenaHalfH - 20f),
            Modulate            = new Color(1f, 0.85f, 0.1f),
            Visible             = false,
        };
        _comboLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_comboLabel);

        _breakLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text                = "BREAK!",
            Modulate            = Colors.Red,
            Position            = new Vector2(-30f, -ArenaHalfH - 20f),
            Visible             = false,
        };
        _breakLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_breakLabel);

        Visible = false;
    }

    // ── Public API ────────────────────────────────────────────────────

    public void StartPhase(PackedScene? patternScene, int totalMeasures = 2)
    {
        _running       = true;
        _currentStreak = 0;
        _maxStreak     = 0;
        Visible        = true;
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
            float dur = totalMeasures * RhythmConstants.BeatsPerMeasure
                        * RhythmClock.Instance.BeatInterval;
            GetTree().CreateTimer(dur).Timeout += EndPhase;
        }
    }

    /// <summary>Spawn a StandardObstacle in the given lane.</summary>
    public void CreateObstacle(int lane, int beatsUntilArrival, int damage)
    {
        if (lane < 0 || lane >= LaneCenterY.Length) return;

        var obs        = _standardObstacleScene.Instantiate<StandardObstacle>();
        obs.Lane       = lane;
        obs.Damage     = damage;
        obs.TravelSpeed = (HitZoneX - SpawnX) / (RhythmClock.Instance.BeatInterval * beatsUntilArrival);
        obs.Position   = new Vector2(SpawnX, LaneCenterY[lane]);
        ObstacleContainer.AddChild(obs);
    }

    /// <summary>Spawn a HoldObstacle in the given lane.</summary>
    public void CreateHoldObstacle(int lane, int holdBeats, int beatsUntilArrival, int damage)
    {
        if (_holdObstacleScene == null || lane < 0 || lane >= LaneCenterY.Length) return;

        var obs        = _holdObstacleScene.Instantiate<HoldObstacle>();
        obs.Lane       = lane;
        obs.Damage     = damage;
        obs.HoldBeats  = holdBeats;
        obs.TravelSpeed = (HitZoneX - SpawnX) / (RhythmClock.Instance.BeatInterval * beatsUntilArrival);
        obs.Position   = new Vector2(SpawnX, LaneCenterY[lane]);
        ObstacleContainer.AddChild(obs);
    }

    // ── Per-frame processing ──────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (!_running) return;
        QueueRedraw();

        float dt    = (float)delta;
        float safeWindow = GoodWindowPx >= float.MaxValue / 2f ? float.MaxValue / 4f : GoodWindowPx;
        float missX = HitZoneX + safeWindow + MissGracePx;

        // Advance held-note timers
        for (int lane = 0; lane < LaneCenterY.Length; lane++)
        {
            if (_laneHeldObstacles[lane] is not { } hObs) continue;

            _laneHoldElapsed[lane] += dt;
            float ratio = _laneHoldElapsed[lane] / hObs.FullHoldTime;
            hObs.HoldProgress = (float)System.Math.Min(ratio, 1.0);

            if (_laneHoldElapsed[lane] >= hObs.FullHoldTime)
            {
                // Full hold — auto-complete as Perfect
                _laneHeldObstacles[lane] = null;
                hObs.Resolve(HitGrade.Perfect);
                EmitSignal(SignalName.NoteHit, (int)HitGrade.Perfect);
                ShowFeedback(HitGrade.Perfect, lane);
                RecordHit(HitGrade.Perfect);
            }
        }

        bool autoHit = IsAutoHit;

        // Move and miss-check obstacles
        foreach (Node child in ObstacleContainer.GetChildren())
        {
            if (child is not ObstacleBase obs || obs.IsResolved) continue;
            if (obs is HoldObstacle heldObs && heldObs.IsBeingHeld) continue;

            obs.Position += new Vector2(obs.TravelSpeed * dt, 0f);

            // AutoHit: resolve as Perfect when note reaches the hit zone
            if (autoHit && obs.Position.X >= HitZoneX - 5f)
            {
                obs.Resolve(HitGrade.Perfect);
                EmitSignal(SignalName.NoteHit, (int)HitGrade.Perfect);
                ShowFeedback(HitGrade.Perfect, obs.Lane);
                RecordHit(HitGrade.Perfect);
                continue;
            }

            if (obs.Position.X > missX)
            {
                // Apply combo-reduced damage
                float mult       = (float)System.Math.Clamp(1.0 - _currentStreak * 0.05, 0.5, 1.0);
                int   dmg        = System.Math.Max(1, (int)(obs.Damage * mult));
                bool  hadStreak  = _currentStreak >= 3;
                _currentStreak   = 0;
                UpdateComboDisplay(hadStreak);
                EmitSignal(SignalName.PlayerHurt, dmg);
                obs.Resolve(HitGrade.Miss);
            }
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!_running) return;

        for (int lane = 0; lane < LaneCenterY.Length; lane++)
        {
            // Hold-note release
            if (e.IsActionReleased($"lane_{lane}") && _laneHeldObstacles[lane] is { } hObs)
            {
                float ratio = _laneHoldElapsed[lane] / hObs.FullHoldTime;
                var   grade = ratio >= 0.85f ? HitGrade.Perfect
                            : ratio >= 0.45f ? HitGrade.Good
                            : HitGrade.Miss;
                _laneHeldObstacles[lane] = null;
                hObs.Resolve(grade);
                EmitSignal(SignalName.NoteHit, (int)grade);
                ShowFeedback(grade, lane);
                RecordHit(grade);
                GetViewport().SetInputAsHandled();
            }

            if (!e.IsActionPressed($"lane_{lane}")) continue;

            // Find closest unresolved obstacle in this lane
            ObstacleBase? best     = null;
            float         bestDist = float.MaxValue;

            foreach (Node child in ObstacleContainer.GetChildren())
            {
                if (child is not ObstacleBase obs || obs.IsResolved || obs.Lane != lane) continue;
                if (obs is HoldObstacle h && h.IsBeingHeld) continue;

                float safeGoodWindow = GoodWindowPx >= float.MaxValue / 2f ? float.MaxValue / 4f : GoodWindowPx;
                float dist = Mathf.Abs(obs.Position.X - HitZoneX);
                if (dist <= safeGoodWindow + MissGracePx && dist < bestDist)
                {
                    best     = obs;
                    bestDist = dist;
                }
            }

            if (best != null)
            {
                if (best is HoldObstacle holdObs)
                {
                    holdObs.StartHold();
                    _laneHeldObstacles[lane] = holdObs;
                    _laneHoldElapsed[lane]   = 0f;
                }
                else
                {
                    float deviationSec  = bestDist / best.TravelSpeed;
                    float timingScale   = SettingsLogic.RhythmTimingScale(TimingWindow);
                    var   grade         = RhythmConstants.GradeDeviationScaled(deviationSec, timingScale);
                    best.Resolve(grade);
                    EmitSignal(SignalName.NoteHit, (int)grade);
                    ShowFeedback(grade, lane);
                    RecordHit(grade);
                }
                GetViewport().SetInputAsHandled();
            }
        }
    }

    // ── Visuals ───────────────────────────────────────────────────────

    public override void _Draw()
    {
        var halfV = new Vector2(ArenaHalfW, ArenaHalfH);
        var bg    = new Rect2(-halfV, halfV * 2f);

        DrawRect(bg, new Color(0.06f, 0.06f, 0.10f, 1f));
        DrawRect(bg, Colors.White, filled: false, width: 2f);

        // Lane dividers
        for (int i = 1; i < LaneCenterY.Length; i++)
        {
            float y = LaneCenterY[i] - LaneHeight * 0.5f;
            DrawLine(new Vector2(-ArenaHalfW, y), new Vector2(ArenaHalfW, y),
                     new Color(1, 1, 1, 0.15f), 1f);
        }

        // Lane colour strips on left edge
        for (int i = 0; i < LaneCenterY.Length; i++)
        {
            var col = ObstacleBase.LaneColors[i] with { A = 0.25f };
            DrawRect(new Rect2(-ArenaHalfW, LaneCenterY[i] - LaneHeight * 0.5f, 20f, LaneHeight), col);
        }

        // Hit zone line
        DrawLine(new Vector2(HitZoneX, -ArenaHalfH),
                 new Vector2(HitZoneX,  ArenaHalfH),
                 Colors.White with { A = 0.8f }, 2f);

        // Per-lane crosshair markers at the hit zone (gold)
        var crosshairColor = new Color(1f, 0.85f, 0.1f, 0.65f);
        const float tickLen = 10f;
        foreach (float cy in LaneCenterY)
            DrawLine(new Vector2(HitZoneX - tickLen, cy), new Vector2(HitZoneX + tickLen, cy),
                     crosshairColor, 2f);

        // Beat pulse
        float pulse = Mathf.Max(0f, 1f - RhythmClock.Instance.BeatPhase * 4f);
        if (pulse > 0f)
        {
            // Hit zone glow
            DrawLine(new Vector2(HitZoneX, -ArenaHalfH),
                     new Vector2(HitZoneX,  ArenaHalfH),
                     Colors.White with { A = pulse * 0.6f }, 6f);

            // Lane colour pulse
            for (int i = 0; i < LaneCenterY.Length; i++)
            {
                var laneCol = ObstacleBase.LaneColors[i] with { A = pulse * 0.12f };
                DrawRect(new Rect2(-ArenaHalfW, LaneCenterY[i] - LaneHeight * 0.5f,
                                   ArenaHalfW * 2f, LaneHeight), laneCol);
            }
        }
    }

    // ── Internal helpers ──────────────────────────────────────────────

    private void RecordHit(HitGrade grade)
    {
        if (grade != HitGrade.Miss)
        {
            _currentStreak++;
            if (_currentStreak > _maxStreak) _maxStreak = _currentStreak;
            UpdateComboDisplay(false);
        }
        else
        {
            bool hadStreak = _currentStreak >= 3;
            _currentStreak = 0;
            UpdateComboDisplay(hadStreak);
        }
    }

    private void UpdateComboDisplay(bool streakBroken)
    {
        if (_comboLabel == null || _breakLabel == null) return;

        if (streakBroken)
        {
            _comboLabel.Visible = false;
            _breakLabel.Visible = true;
            GetTree().CreateTimer(0.5f).Timeout +=
                () => { if (_breakLabel != null) _breakLabel.Visible = false; };
            return;
        }

        if (_currentStreak >= 3)
        {
            _comboLabel.Text    = $"COMBO ×{_currentStreak}";
            _comboLabel.Visible = true;
        }
        else
        {
            _comboLabel.Visible = false;
        }
    }

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

        GetTree().CreateTimer(0.4f).Timeout +=
            () => { if (_feedbackLabel != null) _feedbackLabel.Visible = false; };
    }

    private void OnPatternFinished()
    {
        float grace = (BeatsUntilArrival + 1) * RhythmClock.Instance.BeatInterval;
        GetTree().CreateTimer(grace).Timeout += EndPhase;
    }

    private void EndPhase()
    {
        _running = false;

        // Release any still-held notes as misses
        for (int lane = 0; lane < LaneCenterY.Length; lane++)
        {
            if (_laneHeldObstacles[lane] is { } hObs && !hObs.IsResolved)
            {
                _laneHeldObstacles[lane] = null;
                hObs.Resolve(HitGrade.Miss);
            }
        }

        foreach (Node child in ObstacleContainer.GetChildren())
            child.QueueFree();

        GD.Print($"[RhythmArena] Phase ended. Max combo streak: {_maxStreak}");
        Visible = false;
        EmitSignal(SignalName.PhaseEnded);
    }
}
