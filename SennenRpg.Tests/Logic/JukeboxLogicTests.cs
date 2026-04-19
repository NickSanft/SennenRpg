using NUnit.Framework;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class JukeboxLogicTests
{
    private static MusicTrackInfo Track(string path, string album = "A", int num = 1, string title = "T")
        => new("Artist", album, num, title, path);

    // ── GetUnlockedTracks ────────────────────────────────────────────────

    [Test]
    public void GetUnlockedTracks_EmptyUnlocked_ReturnsEmpty()
    {
        var all = new List<MusicTrackInfo> { Track("a.wav"), Track("b.wav") };
        var result = JukeboxLogic.GetUnlockedTracks(all, new HashSet<string>());
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetUnlockedTracks_FiltersCorrectly()
    {
        var all = new List<MusicTrackInfo>
        {
            Track("a.wav"), Track("b.wav"), Track("c.wav"),
        };
        var unlocked = new HashSet<string> { "a.wav", "c.wav" };

        var result = JukeboxLogic.GetUnlockedTracks(all, unlocked);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].ResourcePath, Is.EqualTo("a.wav"));
        Assert.That(result[1].ResourcePath, Is.EqualTo("c.wav"));
    }

    [Test]
    public void GetUnlockedTracks_PreservesTrackInfo()
    {
        var track = new MusicTrackInfo("Divora", "Album X", 3, "Cool Song", "x.wav", 120f, 0.5f, 0.9f);
        var result = JukeboxLogic.GetUnlockedTracks(
            new List<MusicTrackInfo> { track },
            new HashSet<string> { "x.wav" });

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Artist, Is.EqualTo("Divora"));
        Assert.That(result[0].Album, Is.EqualTo("Album X"));
        Assert.That(result[0].TrackNumber, Is.EqualTo(3));
        Assert.That(result[0].Bpm, Is.EqualTo(120f));
    }

    // ── ShouldRecord ─────────────────────────────────────────────────────

    [Test]
    public void ShouldRecord_Null_ReturnsFalse()
    {
        Assert.That(JukeboxLogic.ShouldRecord(null), Is.False);
    }

    [Test]
    public void ShouldRecord_Empty_ReturnsFalse()
    {
        Assert.That(JukeboxLogic.ShouldRecord(""), Is.False);
    }

    [Test]
    public void ShouldRecord_ValidPath_ReturnsTrue()
    {
        Assert.That(JukeboxLogic.ShouldRecord("res://assets/music/track.wav"), Is.True);
    }

    // ── SortByAlbum ──────────────────────────────────────────────────────

    [Test]
    public void SortByAlbum_SortsByAlbumThenTrackNumberThenTitle()
    {
        var tracks = new List<MusicTrackInfo>
        {
            Track("c.wav", "Bravo",  2, "Zeta"),
            Track("a.wav", "Alpha",  1, "Delta"),
            Track("b.wav", "Bravo",  1, "Echo"),
            Track("d.wav", "Alpha",  2, "Alpha"),
            Track("e.wav", "Bravo",  2, "Alpha"),
        };

        var sorted = JukeboxLogic.SortByAlbum(tracks);

        Assert.That(sorted[0].ResourcePath, Is.EqualTo("a.wav")); // Alpha, #1
        Assert.That(sorted[1].ResourcePath, Is.EqualTo("d.wav")); // Alpha, #2
        Assert.That(sorted[2].ResourcePath, Is.EqualTo("b.wav")); // Bravo, #1
        Assert.That(sorted[3].ResourcePath, Is.EqualTo("e.wav")); // Bravo, #2, Alpha
        Assert.That(sorted[4].ResourcePath, Is.EqualTo("c.wav")); // Bravo, #2, Zeta
    }

    [Test]
    public void SortByAlbum_DoesNotMutateOriginal()
    {
        var tracks = new List<MusicTrackInfo>
        {
            Track("b.wav", "B", 1, "X"),
            Track("a.wav", "A", 1, "Y"),
        };

        var sorted = JukeboxLogic.SortByAlbum(tracks);

        Assert.That(tracks[0].ResourcePath, Is.EqualTo("b.wav")); // original unchanged
        Assert.That(sorted[0].ResourcePath, Is.EqualTo("a.wav")); // sorted copy
    }

    [Test]
    public void SortByAlbum_EmptyList_ReturnsEmpty()
    {
        var result = JukeboxLogic.SortByAlbum(new List<MusicTrackInfo>());
        Assert.That(result, Is.Empty);
    }
}
