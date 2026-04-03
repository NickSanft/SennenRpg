namespace SennenRpg.Core.Data;

/// <summary>
/// Metadata for a music track: artist, album, track number, and display title.
/// </summary>
public record MusicTrackInfo(
    string Artist,
    string Album,
    int    TrackNumber,
    string Title,
    string ResourcePath);
