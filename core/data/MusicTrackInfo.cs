namespace SennenRpg.Core.Data;

/// <summary>
/// Metadata for a music track: artist, album, track number, and display title.
/// <see cref="Bpm"/> drives rhythm minigames that sync to live BGM playback.
/// A value of 0 means "unknown" — callers should fall back to free-running.
///
/// <see cref="BeatOffsetSec"/> is the time from the start of the audio file to
/// beat 0. Tracks with intro silence or a pickup before beat 1 set this; it's
/// passed to <see cref="Autoloads.RhythmClock.AttachPlayer"/> on playback.
///
/// <see cref="BeatConfidence"/> is the analyzer's certainty that the BPM is
/// real (0..1). Tracks below the floor fall back to free-running mode.
/// </summary>
public record MusicTrackInfo(
    string Artist,
    string Album,
    int    TrackNumber,
    string Title,
    string ResourcePath,
    float  Bpm           = 0f,
    float  BeatOffsetSec = 0f,
    float  BeatConfidence = 1f);
