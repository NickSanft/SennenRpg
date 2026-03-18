using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

/// <summary>
/// Authoritative beat clock driven by AudioStreamPlayer.GetPlaybackPosition().
/// Never uses delta accumulation — audio position is always the ground truth.
///
/// Register as autoload "RhythmClock" in project.godot.
/// Call AttachPlayer() after starting BGM playback to begin beat tracking.
/// </summary>
public partial class RhythmClock : Node
{
    public static RhythmClock Instance { get; private set; } = null!;

    [Signal] public delegate void BeatEventHandler(int beatIndex);
    [Signal] public delegate void MeasureEventHandler(int measureIndex);

    /// <summary>Current BPM. Set via SetBpm() or through AttachPlayer().</summary>
    public float Bpm           { get; private set; } = RhythmConstants.DefaultBpm;
    /// <summary>Seconds per beat at the current BPM.</summary>
    public float BeatInterval  { get; private set; } = RhythmConstants.BeatInterval(RhythmConstants.DefaultBpm);
    /// <summary>0.0–1.0 position within the current beat.</summary>
    public float BeatPhase     { get; private set; }
    /// <summary>Cumulative beat count since the clock was attached.</summary>
    public int   BeatIndex     { get; private set; }
    /// <summary>Cumulative measure count (one measure = BeatsPerMeasure beats).</summary>
    public int   MeasureIndex  { get; private set; }
    /// <summary>Beat position within the current measure (0 to BeatsPerMeasure-1).</summary>
    public int   BeatInMeasure { get; private set; }
    /// <summary>Seconds remaining until the next beat fires.</summary>
    public float TimeToNextBeat => BeatInterval * (1f - BeatPhase);

    /// <summary>
    /// Seconds from the start of the audio file to beat 0.
    /// Set this if the track has silence or a pickup before the first beat.
    /// </summary>
    public float BeatOffsetSec { get; private set; }

    private AudioStreamPlayer? _bgmPlayer;
    private int   _lastBeatIndex = -1;
    private bool  _running;
    private float _freeRunPos;   // accumulated seconds when running without a player

    public override void _Ready()
    {
        Instance    = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>Change the active BPM. Recomputes BeatInterval immediately.</summary>
    public void SetBpm(float bpm)
    {
        Bpm          = bpm;
        BeatInterval = RhythmConstants.BeatInterval(bpm);
    }

    /// <summary>
    /// Bind the clock to a playing AudioStreamPlayer and (optionally) set a new BPM.
    /// Call this immediately after AudioStreamPlayer.Play() so beat 0 aligns with the
    /// first frame of audio.
    /// </summary>
    /// <param name="beatOffsetSec">
    /// Seconds from the start of the audio stream to the first beat.
    /// Leave at 0 if the track starts right on beat 1.
    /// </param>
    public void AttachPlayer(AudioStreamPlayer player, float bpm = 0f, float beatOffsetSec = 0f)
    {
        _bgmPlayer     = player;
        BeatOffsetSec  = beatOffsetSec;
        _lastBeatIndex = -1;
        BeatIndex      = 0;
        MeasureIndex   = 0;
        BeatInMeasure  = 0;
        BeatPhase      = 0f;
        _running       = true;

        if (bpm > 0f)
            SetBpm(bpm);
    }

    /// <summary>
    /// Start the clock without an audio player, using delta accumulation.
    /// Useful when no BGM file is configured — guarantees Beat signals fire
    /// so all rhythm minigames work even without audio.
    /// </summary>
    public void StartFreeRunning(float bpm = 0f)
    {
        if (bpm > 0f) SetBpm(bpm);
        _bgmPlayer     = null;
        _freeRunPos    = 0f;
        _lastBeatIndex = -1;
        BeatIndex      = 0;
        MeasureIndex   = 0;
        BeatInMeasure  = 0;
        BeatPhase      = 0f;
        _running       = true;
    }

    /// <summary>Stop tracking beats. Call when leaving the battle scene.</summary>
    public void Stop()
    {
        _running   = false;
        _bgmPlayer = null;
    }

    public override void _Process(double delta)
    {
        if (!_running) return;

        float pos;
        if (_bgmPlayer != null && _bgmPlayer.Playing)
        {
            // Compensate for Godot's output latency so the clock reflects
            // what the player actually hears, not what's in the mix buffer.
            float rawPos = _bgmPlayer.GetPlaybackPosition()
                         + (float)AudioServer.GetTimeSinceLastMix()
                         - (float)AudioServer.GetOutputLatency();
            pos = Mathf.Max(0f, rawPos - BeatOffsetSec);
        }
        else if (_bgmPlayer == null)
        {
            // Free-running: accumulate delta so beats fire without audio
            _freeRunPos += (float)delta;
            pos          = _freeRunPos;
        }
        else
        {
            return; // player attached but not playing yet
        }

        int newBeat = (int)(pos / BeatInterval);
        BeatPhase   = (pos % BeatInterval) / BeatInterval;

        if (newBeat == _lastBeatIndex) return;

        _lastBeatIndex = newBeat;
        BeatIndex      = newBeat;
        BeatInMeasure  = newBeat % RhythmConstants.BeatsPerMeasure;
        MeasureIndex   = newBeat / RhythmConstants.BeatsPerMeasure;

        EmitSignal(SignalName.Beat, newBeat);
        if (BeatInMeasure == 0)
            EmitSignal(SignalName.Measure, MeasureIndex);
    }
}
