using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Single-lane note highway that plays for 4 beats.
/// Notes travel left→right; player presses interact/ui_accept to hit them at the hit zone.
/// Logical size: 320×80 px.
/// </summary>
public partial class CharmMinigame : Control
{
    [Signal] public delegate void CharmCompletedEventHandler(int successCount, int totalNotes);

    // ── Geometry constants ────────────────────────────────────────────
    private const float LaneY        = 40f;
    private const float HitZoneX     = 250f;
    private const float SpawnX       = 20f;
    private const float NoteRadius   = 12f;
    private const float GoodWindowPx = 22f;

    // ── State ─────────────────────────────────────────────────────────
    private readonly System.Collections.Generic.List<CharmNote> _notes = new();
    private int  _noteCount;
    private int  _spawnedCount;
    private int  _beatsElapsed;
    private int  _successCount;
    private bool _finished;

    // ── Inner note class ──────────────────────────────────────────────
    private sealed class CharmNote
    {
        public float X;
        public float Speed;
        public bool  Resolved;
        public bool  Hit;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────

    public void Activate(int noteCount = 4)
    {
        _noteCount    = noteCount;
        _spawnedCount = 0;
        _beatsElapsed = 0;
        _successCount = 0;
        _finished     = false;
        _notes.Clear();

        Visible = true;
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

        // Spawn one note per beat for the first noteCount beats
        if (_spawnedCount < _noteCount)
        {
            float travelTime = 2f * RhythmClock.Instance.BeatInterval;
            float speed = (HitZoneX - SpawnX) / travelTime;
            _notes.Add(new CharmNote { X = SpawnX, Speed = speed });
            _spawnedCount++;
        }

        _beatsElapsed++;

        // After noteCount + 2 beats, finish
        if (_beatsElapsed >= _noteCount + 2)
            Finish();
    }

    public override void _Process(double delta)
    {
        if (_finished || !Visible) return;

        float dt = (float)delta;

        foreach (var note in _notes)
        {
            if (note.Resolved) continue;
            note.X += note.Speed * dt;

            // Auto-miss notes that travel too far past the hit zone
            if (note.X > HitZoneX + 30f)
                note.Resolved = true;
        }

        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_finished || !Visible) return;
        if (!e.IsActionPressed("interact") && !e.IsActionPressed("ui_accept")) return;

        // Find nearest unresolved note within GoodWindowPx of HitZoneX
        CharmNote? nearest = null;
        float      bestDist = float.MaxValue;

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
            nearest.Hit      = true;
            _successCount++;
        }

        GetViewport().SetInputAsHandled();
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Dark lane background
        DrawRect(new Rect2(0f, LaneY - NoteRadius - 4f, 320f, (NoteRadius + 4f) * 2f),
                 new Color(0.06f, 0.06f, 0.10f, 1f));

        // Hit zone circle (pulsing with BeatPhase)
        float phase = RhythmClock.Instance.BeatPhase;
        float pulse = Mathf.Max(0f, 1f - phase * 3f);
        DrawArc(new Vector2(HitZoneX, LaneY), NoteRadius + 4f, 0f, Mathf.Tau, 32,
                Colors.White with { A = 0.4f + pulse * 0.5f }, 2f);
        DrawArc(new Vector2(HitZoneX, LaneY), NoteRadius, 0f, Mathf.Tau, 32,
                Colors.Yellow with { A = 0.3f + pulse * 0.4f }, 3f);

        // Notes
        foreach (var note in _notes)
        {
            if (note.Resolved) continue;
            float dist  = Mathf.Abs(note.X - HitZoneX);
            Color color = dist <= NoteRadius * 1.0f    ? Colors.Yellow    // perfect zone
                        : dist <= GoodWindowPx         ? Colors.Cyan      // good zone
                        : Colors.White;
            DrawCircle(new Vector2(note.X, LaneY), NoteRadius, color);
        }

        // Hit zone label
        DrawString(ThemeDB.FallbackFont, new Vector2(HitZoneX - 12f, LaneY + NoteRadius + 16f),
                   "HIT", HorizontalAlignment.Left, -1, 10, Colors.White with { A = 0.6f });
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        Visible   = false;
        RhythmClock.Instance.Beat -= OnBeat;
        EmitSignal(SignalName.CharmCompleted, _successCount, _noteCount);
    }
}
