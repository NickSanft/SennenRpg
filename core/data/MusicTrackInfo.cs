namespace SennenRpg.Core.Data;

/// <summary>
/// Metadata for a music track: artist, album, track number, and display title.
/// <see cref="Bpm"/> drives rhythm minigames that sync to live BGM playback.
/// A value of 0 means "unknown" — callers should fall back to <see cref="RhythmConstants.DefaultBpm"/>.
/// </summary>
public record MusicTrackInfo(
    string Artist,
    string Album,
    int    TrackNumber,
    string Title,
    string ResourcePath,
    float  Bpm = 0f);
