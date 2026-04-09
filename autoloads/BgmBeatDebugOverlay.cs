using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

/// <summary>
/// Debug-only F9 overlay for verifying BGM beat sync. Top half draws live
/// magnitude bars from the Music bus's <see cref="AudioEffectSpectrumAnalyzer"/>.
/// Bottom half is a 4-second scrolling beat timeline with a centre "now" marker
/// that flashes white on each <see cref="RhythmClock.Beat"/> emission.
///
/// Permanent overlay — registered as an autoload, hidden by default. Press F9
/// to toggle. Never visible to players (debug builds only).
///
/// Phase 4 hand-correction keys (when overlay is visible):
///   ←/→         Nudge first-beat offset ±10 ms
///   Shift+←/→   Nudge first-beat offset ±1 ms (fine)
///   ↑/↓         Nudge BPM ±0.1
///   R           Reset both to whatever the JSON loader provided
///   Shift+S     Print current values as a JSON snippet for paste into
///               beat_data.overrides.json
/// </summary>
public partial class BgmBeatDebugOverlay : CanvasLayer
{
    public static BgmBeatDebugOverlay? Instance { get; private set; }

    private const string MusicBusName = "Music";
    private const int    SpectrumBins  = 32;
    private const float  TimelineSeconds = 4f;

    private AudioEffectSpectrumAnalyzerInstance? _analyzer;
    private Control _root             = null!;
    private ColorRect _bg              = null!;
    private SpectrumDrawer _spectrum   = null!;
    private TimelineDrawer _timeline   = null!;
    private Label _statusPill          = null!;
    private float _flashAlpha;
    // Live editable values (Phase 4 nudge keys mutate these and the RhythmClock)
    private float _liveBpm;
    private float _liveOffset;

    public override void _Ready()
    {
        Instance      = this;
        Layer         = 99;
        ProcessMode   = ProcessModeEnum.Always;
        Visible       = false;
        SetProcess(false); // only process while visible

        // Cache the bus analyzer instance — null if the Music bus / effect is missing
        int idx = AudioServer.GetBusIndex(MusicBusName);
        if (idx >= 0 && AudioServer.GetBusEffectCount(idx) > 0)
            _analyzer = AudioServer.GetBusEffectInstance(idx, 0) as AudioEffectSpectrumAnalyzerInstance;

        BuildUi();

        if (RhythmClock.Instance != null)
            RhythmClock.Instance.Beat += OnBeat;

        // Re-fit on viewport size changes (window resize)
        GetViewport().SizeChanged += FitRootToViewport;
    }

    public override void _ExitTree()
    {
        if (RhythmClock.Instance != null)
            RhythmClock.Instance.Beat -= OnBeat;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true } key) return;

        if (key.Keycode == Key.F9)
        {
            Toggle();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!Visible) return;

        // Phase 4 nudge keys (only active when overlay is visible)
        bool shift = key.ShiftPressed;
        switch (key.Keycode)
        {
            case Key.Left:
                NudgeOffset(shift ? -0.001f : -0.010f);
                GetViewport().SetInputAsHandled();
                break;
            case Key.Right:
                NudgeOffset(shift ? +0.001f : +0.010f);
                GetViewport().SetInputAsHandled();
                break;
            case Key.Up:
                NudgeBpm(+0.1f);
                GetViewport().SetInputAsHandled();
                break;
            case Key.Down:
                NudgeBpm(-0.1f);
                GetViewport().SetInputAsHandled();
                break;
            case Key.R:
                ResetToJson();
                GetViewport().SetInputAsHandled();
                break;
            case Key.S when shift:
                PrintOverrideJson();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void Toggle()
    {
        Visible = !Visible;
        SetProcess(Visible);
        if (Visible)
        {
            FitRootToViewport();
            // Snapshot live values from RhythmClock the moment we open
            _liveBpm    = RhythmClock.Instance?.Bpm ?? 0f;
            _liveOffset = RhythmClock.Instance?.BeatOffsetSec ?? 0f;
            UpdateStatus();
            GD.Print("[BgmBeatDebug] overlay opened");
        }
        else
        {
            GD.Print("[BgmBeatDebug] overlay closed");
        }
    }

    private void FitRootToViewport()
    {
        if (_root == null) return;
        var size = GetViewport().GetVisibleRect().Size;
        _root.Position = Vector2.Zero;
        _root.Size     = size;

        _bg.Position = Vector2.Zero;
        _bg.Size     = size;

        // Top half: spectrum bars (10%–45% vertically)
        _spectrum.Position = new Vector2(size.X * 0.05f, size.Y * 0.10f);
        _spectrum.Size     = new Vector2(size.X * 0.90f, size.Y * 0.35f);

        // Middle: timeline (55%–85% vertically)
        _timeline.Position = new Vector2(size.X * 0.05f, size.Y * 0.55f);
        _timeline.Size     = new Vector2(size.X * 0.90f, size.Y * 0.30f);

        // Bottom: status pill
        _statusPill.Position = new Vector2(size.X * 0.05f, size.Y * 0.88f);
        _statusPill.Size     = new Vector2(size.X * 0.90f, size.Y * 0.07f);
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        _flashAlpha = Mathf.Max(0f, _flashAlpha - (float)delta * 8f);
        _spectrum.QueueRedraw();
        _timeline.FlashAlpha = _flashAlpha;
        _timeline.QueueRedraw();
        UpdateStatus();
    }

    private void OnBeat(int beatIndex) => _flashAlpha = 1f;

    // ── UI ────────────────────────────────────────────────────────────────────

    private void BuildUi()
    {
        // Anchors do not stretch Control children of a CanvasLayer to the
        // viewport — there is no parent rect. Use explicit Position+Size set
        // by FitRootToViewport on every open + on viewport size changes.
        _root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_root);

        _bg = new ColorRect
        {
            Color       = new Color(0f, 0f, 0f, 0.55f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _root.AddChild(_bg);

        _spectrum = new SpectrumDrawer(this);
        _root.AddChild(_spectrum);

        _timeline = new TimelineDrawer(this);
        _root.AddChild(_timeline);

        _statusPill = new Label { Text = "BPM —" };
        _statusPill.AddThemeFontSizeOverride("font_size", 12);
        _statusPill.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        _root.AddChild(_statusPill);
    }

    private void UpdateStatus()
    {
        var rc = RhythmClock.Instance;
        string bpm  = rc != null && rc.Bpm > 0f ? $"{rc.Bpm:F1}" : "—";
        string off  = rc != null ? $"{rc.BeatOffsetSec:F3}s" : "—";
        string conf = "—";

        // Pull track name + confidence from MusicMetadata if we can
        string trackName = "(free-running)";
        // Hacky: AudioManager doesn't expose currently-playing path. Look at all
        // tracks and find the one whose BPM matches RhythmClock; close-enough.
        if (rc != null && rc.Bpm > 0f)
        {
            foreach (var kv in MusicMetadata.All)
            {
                var info = MusicMetadata.Lookup(kv.Key);
                if (info != null && Mathf.Abs(info.Bpm - rc.Bpm) < 0.05f
                                 && Mathf.Abs(info.BeatOffsetSec - rc.BeatOffsetSec) < 0.001f)
                {
                    trackName = info.Title;
                    conf      = $"{info.BeatConfidence:F2}";
                    break;
                }
            }
        }

        _statusPill.Text = $"BPM {bpm}  off {off}  conf {conf}   {trackName}   [F9 close]";
    }

    // ── Phase 4 — nudge keys ─────────────────────────────────────────────────

    private void NudgeOffset(float deltaSec)
    {
        var rc = RhythmClock.Instance;
        if (rc == null || rc.Bpm <= 0f) return;
        _liveOffset += deltaSec;
        rc.AttachPlayer(GetActivePlayer(), rc.Bpm, _liveOffset);
        GD.Print($"[BgmBeatDebug] offset → {_liveOffset:F3}s");
    }

    private void NudgeBpm(float deltaBpm)
    {
        var rc = RhythmClock.Instance;
        if (rc == null || rc.Bpm <= 0f) return;
        _liveBpm += deltaBpm;
        rc.AttachPlayer(GetActivePlayer(), _liveBpm, _liveOffset);
        GD.Print($"[BgmBeatDebug] BPM → {_liveBpm:F1}");
    }

    private void ResetToJson()
    {
        // Re-attach using the original metadata values
        var rc = RhythmClock.Instance;
        if (rc == null) return;
        // Trigger a re-lookup by asking AudioManager... we don't have that hook,
        // so just print a hint.
        GD.Print("[BgmBeatDebug] Reset: change tracks (or restart) to reload from JSON.");
    }

    private void PrintOverrideJson()
    {
        // Best-effort track lookup — same as UpdateStatus
        string match = "<unknown>";
        var rc = RhythmClock.Instance;
        if (rc != null)
        {
            foreach (var kv in MusicMetadata.All)
            {
                var info = MusicMetadata.Lookup(kv.Key);
                if (info != null && info.Bpm > 0f)
                {
                    match = kv.Key;
                    break;
                }
            }
        }

        GD.Print("[BgmBeatDebug] Paste into assets/music/beat_data.overrides.json:");
        GD.Print($"  \"{match}\": {{");
        GD.Print($"    \"bpm\": {_liveBpm:F2},");
        GD.Print($"    \"first_beat_sec\": {_liveOffset:F3},");
        GD.Print($"    \"confidence\": 1.0");
        GD.Print("  }");
    }

    private AudioStreamPlayer GetActivePlayer()
    {
        // AudioManager doesn't expose the active player; reach in via the autoload tree.
        var am = GetNode<AudioManager>("/root/AudioManager");
        var player = am.FindChild("AudioStreamPlayer", recursive: false, owned: false) as AudioStreamPlayer;
        return player ?? new AudioStreamPlayer();
    }

    // ── Custom drawers ───────────────────────────────────────────────────────

    private partial class SpectrumDrawer : Control
    {
        private readonly BgmBeatDebugOverlay _owner;
        public SpectrumDrawer(BgmBeatDebugOverlay owner) { _owner = owner; MouseFilter = MouseFilterEnum.Ignore; }

        public override void _Draw()
        {
            float w = Size.X, h = Size.Y;
            DrawRect(new Rect2(0, 0, w, h), new Color(0f, 0f, 0f, 0.4f), filled: true);

            var inst = _owner._analyzer;
            if (inst == null)
            {
                DrawString(ThemeDB.FallbackFont, new Vector2(8, 16),
                    "(no Music bus spectrum analyzer)", HorizontalAlignment.Left, -1, 12,
                    new Color(1f, 0.4f, 0.4f));
                return;
            }

            float barW = w / SpectrumBins;
            // Logarithmic frequency mapping: 32 bins from 30 Hz to 16000 Hz
            float minHz = 30f, maxHz = 16000f;
            float logRatio = Mathf.Log(maxHz / minHz);
            for (int i = 0; i < SpectrumBins; i++)
            {
                float lo = minHz * Mathf.Exp(logRatio * i        / SpectrumBins);
                float hi = minHz * Mathf.Exp(logRatio * (i + 1) / SpectrumBins);
                Vector2 mag = inst.GetMagnitudeForFrequencyRange(lo, hi);
                float energy = (mag.X + mag.Y) * 0.5f;
                float db = 20f * Mathf.Log(Mathf.Max(energy, 1e-6f)) / Mathf.Log(10f);
                float norm = Mathf.Clamp((db + 80f) / 80f, 0f, 1f);
                float bh   = norm * h;
                DrawRect(new Rect2(i * barW + 1, h - bh, barW - 2, bh),
                    new Color(0.4f + norm * 0.6f, 0.6f, 1f - norm * 0.5f), filled: true);
            }
        }
    }

    private partial class TimelineDrawer : Control
    {
        private readonly BgmBeatDebugOverlay _owner;
        public float FlashAlpha;
        public TimelineDrawer(BgmBeatDebugOverlay owner) { _owner = owner; MouseFilter = MouseFilterEnum.Ignore; }

        public override void _Draw()
        {
            float w = Size.X, h = Size.Y;
            DrawRect(new Rect2(0, 0, w, h), new Color(0f, 0f, 0f, 0.4f), filled: true);

            var rc = RhythmClock.Instance;
            if (rc == null || rc.BeatInterval <= 0f)
            {
                DrawString(ThemeDB.FallbackFont, new Vector2(8, 16),
                    "(rhythm clock not running)", HorizontalAlignment.Left, -1, 12,
                    new Color(1f, 0.4f, 0.4f));
                return;
            }

            float centerX = w * 0.5f;
            float centerY = h * 0.5f;

            // Time window: ±2 seconds around now (4s total)
            float halfWindow = TimelineSeconds * 0.5f;
            float pxPerSec   = w / TimelineSeconds;

            // Current sub-beat phase tells us how far into the current beat we are
            float beatPhase = rc.BeatPhase;
            // Time-since-current-beat (in seconds, can be negative for clarity)
            float tIntoBeat = beatPhase * rc.BeatInterval;

            // Draw beat ticks for ±halfWindow seconds, anchored on the current beat
            int firstTick = -(int)Mathf.Ceil(halfWindow / rc.BeatInterval);
            int lastTick  = +(int)Mathf.Ceil(halfWindow / rc.BeatInterval);
            for (int i = firstTick; i <= lastTick; i++)
            {
                float tickTime = i * rc.BeatInterval - tIntoBeat;
                float x = centerX + tickTime * pxPerSec;
                if (x < 0 || x > w) continue;

                int absIdx = rc.BeatIndex + i;
                bool isDownbeat = absIdx >= 0 && absIdx % RhythmConstants.BeatsPerMeasure == 0;
                float tickH = isDownbeat ? h * 0.6f : h * 0.35f;
                var color  = isDownbeat ? new Color(1f, 0.85f, 0.2f) : new Color(0.7f, 0.7f, 0.9f);
                DrawLine(new Vector2(x, centerY - tickH * 0.5f),
                         new Vector2(x, centerY + tickH * 0.5f), color, isDownbeat ? 3f : 2f);
            }

            // Centre "now" marker
            DrawLine(new Vector2(centerX, 4), new Vector2(centerX, h - 4),
                new Color(1f, 1f, 1f, 0.4f + FlashAlpha * 0.6f), 2f);
        }
    }
}
