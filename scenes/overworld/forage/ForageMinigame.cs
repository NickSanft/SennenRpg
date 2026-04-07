using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld.Forage;

/// <summary>
/// Single-lane rhythm minigame for foraging. Locks to the live overworld BGM via
/// <see cref="AudioManager.AttachRhythmClockToCurrentBgm"/> so notes scroll in time
/// with whatever song is currently playing.
///
/// Notes spawn on integer beats, with the very first note scheduled
/// <see cref="FirstNoteLookaheadBeats"/> beats ahead of <see cref="RhythmClock.BeatIndex"/>.
/// This guarantees a consistent intro window even if the forage roll lands late
/// in a beat phase.
///
/// On completion emits <see cref="ForageCompletedEventHandler"/> with raw counts.
/// The owner is responsible for converting (perfects, hits, total) into a
/// <see cref="ForageLogic.ForageGrade"/> and granting items.
/// </summary>
public partial class ForageMinigame : Control
{
    [Signal] public delegate void ForageCompletedEventHandler(int perfects, int hits, int totalNotes);
    [Signal] public delegate void ForageCancelledEventHandler();

    // ── Geometry (logical 320×80) ────────────────────────────────────────
    private const float LaneY           = 40f;
    private const float HitZoneX        = 250f;
    private const float SpawnX          = 20f;
    private const float NoteRadius      = 12f;
    private const float PerfectWindowPx = 9f;
    private const float GoodWindowPx    = 22f;

    // ── Rhythm scheduling ────────────────────────────────────────────────
    /// <summary>Beats of "Get Ready" countdown before the first note spawns.</summary>
    private const int CountdownBeats          = 4;
    /// <summary>Travel time, in beats, from spawn to the hit zone.</summary>
    private const int NoteTravelBeats         = 2;
    /// <summary>Beats between successive note spawns — one note per beat.</summary>
    private const int BeatsBetweenNotes       = 1;
    /// <summary>Tail beats after the last note arrives so trailing misses can resolve.</summary>
    private const int TailBeats               = 3;

    // ── Note labels ──────────────────────────────────────────────────────
    private static readonly string[] NoteLabels =
        ["Pluck", "Dig", "Sift", "Twist", "Pry", "Reach"];

    // ── State ────────────────────────────────────────────────────────────
    private readonly System.Collections.Generic.List<ForageNote> _notes = new();
    private int  _noteCount;
    private int  _spawnedCount;
    /// <summary>
    /// RhythmClock.BeatIndex captured on the first OnBeat after Activate.
    /// Captured lazily (not at Activate time) because RhythmClock.AttachPlayer
    /// jumps the BeatIndex to match the live audio playback position one frame
    /// after attaching — capturing eagerly would make all subsequent
    /// beatsSinceStart values race ahead of real time.
    /// </summary>
    private int  _baseBeat = -1;
    private int  _hits;
    private int  _perfects;
    private int  _misses;
    private bool _finished;
    private int  _countdownDisplay; // remaining countdown beats shown in HUD (4..1, then 0)

    private sealed class ForageNote
    {
        public float  X;
        public float  Speed;
        public bool   Resolved;
        public int    Grade; // 0=unresolved, 1=perfect, 2=good, 3=miss
        public string Label = "";
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Begin a new round. Locks the rhythm clock to the live BGM and schedules
    /// <paramref name="noteCount"/> notes one beat apart starting two beats from now.
    /// </summary>
    public void Activate(int noteCount = 4)
    {
        _noteCount        = Mathf.Max(1, noteCount);
        _spawnedCount     = 0;
        _hits             = 0;
        _perfects         = 0;
        _misses           = 0;
        _finished         = false;
        _baseBeat         = -1; // captured lazily on first OnBeat
        _countdownDisplay = CountdownBeats;
        _notes.Clear();

        Visible = true;

        // Lock notes to whatever BGM is playing right now. RhythmClock will
        // sync its BeatIndex to the live playback position on the next frame,
        // which is why we wait for the first OnBeat to capture _baseBeat.
        AudioManager.Instance?.AttachRhythmClockToCurrentBgm();

        RhythmClock.Instance.Beat += OnBeat;
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        // Defensive — Finish() also detaches.
        RhythmClock.Instance.Beat -= OnBeat;
    }

    // ── Beat-driven note spawning ────────────────────────────────────────

    private void OnBeat(int beatIndex)
    {
        if (_finished) return;

        // Lazy base capture — see _baseBeat comment for why this is deferred.
        if (_baseBeat < 0)
        {
            _baseBeat = beatIndex;
            return;          // beatsSinceStart == 0 — start countdown on the NEXT beat
        }

        int beatsSinceStart = beatIndex - _baseBeat;

        // ── Countdown phase ───────────────────────────────────────────────
        // Beats 0..(CountdownBeats-1) tick down "Get Ready" without spawning notes.
        if (beatsSinceStart < CountdownBeats)
        {
            _countdownDisplay = CountdownBeats - beatsSinceStart;
            return;
        }

        // Hide the countdown the moment notes start flowing.
        _countdownDisplay = 0;

        // ── Note spawning ─────────────────────────────────────────────────
        // Note i spawns at beat (CountdownBeats + i * BeatsBetweenNotes) and
        // travels NoteTravelBeats beats to reach the hit zone, so the i-th
        // note arrives at beat (CountdownBeats + i + NoteTravelBeats).
        int beatsSinceCountdown = beatsSinceStart - CountdownBeats;
        int targetSpawnBeat     = _spawnedCount * BeatsBetweenNotes;
        if (_spawnedCount < _noteCount && beatsSinceCountdown >= targetSpawnBeat)
        {
            float travelTime = NoteTravelBeats * RhythmClock.Instance.BeatInterval;
            float speed      = (HitZoneX - SpawnX) / travelTime;
            string label     = NoteLabels[_spawnedCount % NoteLabels.Length];
            _notes.Add(new ForageNote { X = SpawnX, Speed = speed, Label = label });
            _spawnedCount++;
        }

        // ── End check ─────────────────────────────────────────────────────
        // Last note (index noteCount-1) arrives at beat
        // (CountdownBeats + (noteCount-1)*BeatsBetweenNotes + NoteTravelBeats).
        // Add TailBeats so trailing misses can resolve before we close out.
        int lastArrivalBeat =
            CountdownBeats
            + (_noteCount - 1) * BeatsBetweenNotes
            + NoteTravelBeats;
        int endBeat = lastArrivalBeat + TailBeats;

        if (beatsSinceStart >= endBeat)
            Finish();
    }

    // ── Per-frame note motion ────────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (_finished || !Visible) return;

        float dt = (float)delta;
        foreach (var note in _notes)
        {
            if (note.Resolved) continue;
            note.X += note.Speed * dt;

            if (note.X > HitZoneX + 30f)
            {
                note.Resolved = true;
                note.Grade    = 3;
                _misses++;
            }
        }
        QueueRedraw();
    }

    // ── Input ────────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent e)
    {
        if (_finished || !Visible) return;

        // Allow ESC to bail — counts as Miss for everything unhit.
        if (e.IsActionPressed("ui_cancel"))
        {
            EmitSignal(SignalName.ForageCancelled);
            Finish();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!e.IsActionPressed("interact") && !e.IsActionPressed("ui_accept")) return;

        ForageNote? nearest = null;
        float bestDist = float.MaxValue;
        foreach (var note in _notes)
        {
            if (note.Resolved) continue;
            float dist = Mathf.Abs(note.X - HitZoneX);
            if (dist <= GoodWindowPx && dist < bestDist)
            {
                nearest  = note;
                bestDist = dist;
            }
        }

        if (nearest != null)
        {
            nearest.Resolved = true;
            _hits++;
            if (bestDist <= PerfectWindowPx)
            {
                nearest.Grade = 1;
                _perfects++;
            }
            else
            {
                nearest.Grade = 2;
            }
            AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        }

        GetViewport().SetInputAsHandled();
        QueueRedraw();
    }

    // ── Drawing ──────────────────────────────────────────────────────────

    public override void _Draw()
    {
        // Lane background
        DrawRect(new Rect2(0f, LaneY - NoteRadius - 8f, 320f, (NoteRadius + 8f) * 2f),
                 new Color(0.06f, 0.10f, 0.06f, 1f));

        // Hit zone, pulsing on beat phase
        float phase = RhythmClock.Instance.BeatPhase;
        float pulse = Mathf.Max(0f, 1f - phase * 3f);
        DrawArc(new Vector2(HitZoneX, LaneY), NoteRadius + 4f, 0f, Mathf.Tau, 32,
                Colors.White with { A = 0.4f + pulse * 0.5f }, 2f);
        DrawArc(new Vector2(HitZoneX, LaneY), NoteRadius, 0f, Mathf.Tau, 32,
                new Color(0.4f, 1f, 0.4f) with { A = 0.3f + pulse * 0.4f }, 3f);

        // Notes
        foreach (var note in _notes)
        {
            if (note.Resolved) continue;
            float dist = Mathf.Abs(note.X - HitZoneX);
            Color color = dist <= PerfectWindowPx ? new Color(1f, 0.85f, 0.1f)
                        : dist <= GoodWindowPx    ? Colors.Cyan
                        : Colors.White;
            DrawCircle(new Vector2(note.X, LaneY), NoteRadius, color);
            DrawString(ThemeDB.FallbackFont, new Vector2(note.X - 14f, LaneY - NoteRadius - 4f),
                       note.Label, HorizontalAlignment.Left, -1, 8, Colors.White with { A = 0.7f });
        }

        // Header
        DrawString(ThemeDB.FallbackFont, new Vector2(10f, 12f),
                   "FORAGE!", HorizontalAlignment.Left, -1, 10, new Color(1f, 0.95f, 0.4f));
        DrawString(ThemeDB.FallbackFont, new Vector2(70f, 12f),
                   $"Perfect: {_perfects}  Hits: {_hits}  Miss: {_misses}",
                   HorizontalAlignment.Left, -1, 9, Colors.White with { A = 0.8f });

        // Countdown overlay (drawn over the lane while we're still in the get-ready phase)
        if (_countdownDisplay > 0)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(120f, LaneY + 6f),
                       "GET READY", HorizontalAlignment.Left, -1, 11,
                       Colors.White with { A = 0.85f });
            DrawString(ThemeDB.FallbackFont, new Vector2(160f, LaneY - 16f),
                       _countdownDisplay.ToString(), HorizontalAlignment.Left, -1, 22,
                       new Color(1f, 0.95f, 0.4f));
        }
    }

    // ── Finish ───────────────────────────────────────────────────────────

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        Visible   = false;
        RhythmClock.Instance.Beat -= OnBeat;
        // Don't Stop() the clock — overworld BGM should keep ticking it.
        EmitSignal(SignalName.ForageCompleted, _perfects, _hits, _noteCount);
    }
}
