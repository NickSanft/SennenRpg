using System;
using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure logic for the Jukebox feature — filtering and sorting unlocked BGM tracks.
/// No Godot dependencies.
/// </summary>
public static class JukeboxLogic
{
    /// <summary>
    /// Returns only the tracks whose <see cref="MusicTrackInfo.ResourcePath"/>
    /// appears in <paramref name="unlockedPaths"/>.
    /// </summary>
    public static List<MusicTrackInfo> GetUnlockedTracks(
        IReadOnlyList<MusicTrackInfo> allTracks,
        IReadOnlyCollection<string> unlockedPaths)
    {
        var result = new List<MusicTrackInfo>();
        foreach (var track in allTracks)
        {
            if (unlockedPaths.Contains(track.ResourcePath))
                result.Add(track);
        }
        return result;
    }

    /// <summary>
    /// Returns true if <paramref name="bgmPath"/> is a valid, non-empty path
    /// that should be recorded as "heard" by the player.
    /// </summary>
    public static bool ShouldRecord(string? bgmPath)
        => !string.IsNullOrEmpty(bgmPath);

    /// <summary>
    /// Returns a new list sorted by Album, then TrackNumber, then Title.
    /// </summary>
    public static List<MusicTrackInfo> SortByAlbum(List<MusicTrackInfo> tracks)
    {
        var sorted = new List<MusicTrackInfo>(tracks);
        sorted.Sort((a, b) =>
        {
            int cmp = string.Compare(a.Album, b.Album, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0) return cmp;

            cmp = a.TrackNumber.CompareTo(b.TrackNumber);
            if (cmp != 0) return cmp;

            return string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        });
        return sorted;
    }
}
