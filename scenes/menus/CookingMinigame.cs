using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Single-lane rhythm minigame for cooking. Notes travel left→right;
/// player presses interact/ui_accept to hit them at the hit zone.
/// Tracks Perfect/Good/Miss separately for quality calculation.
/// Follows the CharmMinigame pattern. Logical size: 320×80 px.
/// </summary>
public partial class CookingMinigame : Control
{
    [Signal] public delegate void CookingCompletedEventHandler(
        int perfects, int goods, int misses, int totalNotes);

    // ── Geometry ──────────────────────────────────────────────────────
    private const float LaneY          = 40f;
    private const float HitZoneX       = 250f;
    private const float SpawnX         = 20f;
    private const float NoteRadius     = 12f;
    private const float PerfectWindowPx = 10f;
    private const float GoodWindowPx    = 22f;
    private const float CookingBpm      = 120f;

    // ── Note labels for visual flair ──────────────────────────────────
    private static readonly string[] NoteLabels = ["Stir", "Flip", "Chop", "Season", "Mix", "Taste", "Sear", "Fold"];

    // ── State ─────────────────────────────────────────────────────────
    private readonly System.Collections.Generic.List<CookNote> _notes = new();
    private int  _noteCount;
    private int  _spawnedCount;
    private int  _beatsElapsed;
    private int  _perfects;
    private int  _goods;
    private int  _misses;
    private bool _finished;

    private sealed class CookNote
    {
        public float  X;
        public float  Speed;
        public bool   Resolved;
        public int    Grade; // 0=unresolved, 1=perfect, 2=good, 3=miss
        public string Label = "";
    }

    // ── Public API ────────────────────────────────────────────────────

    public void Activate(int noteCount = 6)
    {
        _noteCount    = noteCount;
        _spawnedCount = 0;
        _beatsElapsed = 0;
        _perfects     = 0;
        _goods        = 0;
        _misses       = 0;
        _finished     = false;
        _notes.Clear();

        Visible = true;
        RhythmClock.Instance.StartFreeRunning(CookingBpm);
        RhythmClock.Instance.Beat += OnBeat;
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        RhythmClock.Instance.Beat -= OnBeat;
    }

    // ── Beat-driven note spawning ─────────────────────────────────────

    private void OnBeat(int _beatIndex)
    {
        if (_finished) return;

        if (_spawnedCount < _noteCount)
        {
            float travelTime = 2f * RhythmClock.Instance.BeatInterval;
            float speed = (HitZoneX - SpawnX) / travelTime;
            string label = NoteLabels[_spawnedCount % NoteLabels.Length];
            _notes.Add(new CookNote { X = SpawnX, Speed = speed, Label = label });
            _spawnedCount++;
        }

        _beatsElapsed++;

        if (_beatsElapsed >= _noteCount + 2)
            Finish();
    }

    // ── Per-frame update ──────────────────────────────────────────────

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

    // ── Input ─────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent e)
    {
        if (_finished || !Visible) return;
        if (!e.IsActionPressed("interact") && !e.IsActionPressed("ui_accept")) return;

        CookNote? nearest = null;
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
            if (bestDist <= PerfectWindowPx)
            {
                nearest.Grade = 1;
                _perfects++;
            }
            else
            {
                nearest.Grade = 2;
                _goods++;
            }

            AudioManager.Instance?.PlaySfx(Core.Data.UiSfx.Confirm);
        }

        GetViewport().SetInputAsHandled();
        QueueRedraw();
    }

    // ── Drawing ───────────────────────────────────────────────────────

    public override void _Draw()
    {
        // Dark lane background
        DrawRect(new Rect2(0f, LaneY - NoteRadius - 8f, 320f, (NoteRadius + 8f) * 2f),
                 new Color(0.06f, 0.06f, 0.10f, 1f));

        // Hit zone circle (pulsing with BeatPhase)
        float phase = RhythmClock.Instance.BeatPhase;
        float pulse = Mathf.Max(0f, 1f - phase * 3f);
        DrawArc(new Vector2(HitZoneX, LaneY), NoteRadius + 4f, 0f, Mathf.Tau, 32,
                Colors.White with { A = 0.4f + pulse * 0.5f }, 2f);
        DrawArc(new Vector2(HitZoneX, LaneY), NoteRadius, 0f, Mathf.Tau, 32,
                new Color(1f, 0.6f, 0.2f) with { A = 0.3f + pulse * 0.4f }, 3f);

        // Notes
        foreach (var note in _notes)
        {
            if (note.Resolved) continue;
            float dist = Mathf.Abs(note.X - HitZoneX);
            Color color = dist <= PerfectWindowPx  ? new Color(1f, 0.85f, 0.1f)  // gold = perfect zone
                        : dist <= GoodWindowPx     ? Colors.Cyan                   // cyan = good zone
                        : Colors.White;
            DrawCircle(new Vector2(note.X, LaneY), NoteRadius, color);

            // Note label
            DrawString(ThemeDB.FallbackFont, new Vector2(note.X - 12f, LaneY - NoteRadius - 4f),
                       note.Label, HorizontalAlignment.Left, -1, 8, Colors.White with { A = 0.7f });
        }

        // Hit zone label
        DrawString(ThemeDB.FallbackFont, new Vector2(HitZoneX - 12f, LaneY + NoteRadius + 16f),
                   "HIT", HorizontalAlignment.Left, -1, 10, Colors.White with { A = 0.6f });

        // Score display
        DrawString(ThemeDB.FallbackFont, new Vector2(10f, 14f),
                   $"Perfect: {_perfects}  Good: {_goods}  Miss: {_misses}",
                   HorizontalAlignment.Left, -1, 10, Colors.White with { A = 0.8f });
    }

    // ── Finish ────────────────────────────────────────────────────────

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        Visible   = false;
        RhythmClock.Instance.Beat -= OnBeat;
        RhythmClock.Instance.Stop();
        EmitSignal(SignalName.CookingCompleted, _perfects, _goods, _misses, _noteCount);
    }
}
