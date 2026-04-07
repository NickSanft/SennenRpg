using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class MusicMetadataTests
{
    [Test]
    public void Lookup_KnownTrack_ReturnsMetadata()
    {
        var info = MusicMetadata.Lookup("res://assets/music/Carillion Forest.wav");

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Artist, Is.EqualTo("Divora"));
        Assert.That(info.Album, Is.EqualTo("New Beginnings - DND 4"));
        Assert.That(info.TrackNumber, Is.EqualTo(2));
        Assert.That(info.Title, Is.EqualTo("Carillion Forest"));
    }

    [Test]
    public void Lookup_UnknownPath_ReturnsNull()
    {
        Assert.That(MusicMetadata.Lookup("res://assets/music/nonexistent.wav"), Is.Null);
    }

    [Test]
    public void Lookup_SlowBroil_ReturnsCorrectAlbum()
    {
        var info = MusicMetadata.Lookup("res://assets/music/Slow Broil.wav");

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Album, Does.Contain("Gravity"));
        Assert.That(info.TrackNumber, Is.EqualTo(10));
    }

    [Test]
    public void Lookup_CorruptionCanBeFun_ReturnsCorrectData()
    {
        var info = MusicMetadata.Lookup("res://assets/music/Corruption Can Be Fun.wav");

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Title, Is.EqualTo("Corruption Can Be Fun"));
        Assert.That(info.Album, Does.Contain("Ominous Augury"));
    }

    [Test]
    public void All_ContainsAllRegisteredTracks()
    {
        // 10 original + 3 weather tracks (foggy, rain, glimmersong).
        Assert.That(MusicMetadata.All, Has.Count.EqualTo(13));
    }

    [Test]
    public void All_AllTracksHaveArtistDivora()
    {
        foreach (var (_, info) in MusicMetadata.All)
            Assert.That(info.Artist, Is.EqualTo("Divora"));
    }

    [Test]
    public void All_AllTrackNumbersPositive()
    {
        foreach (var (_, info) in MusicMetadata.All)
            Assert.That(info.TrackNumber, Is.GreaterThan(0));
    }

    [Test]
    public void All_AllTitlesNonEmpty()
    {
        foreach (var (_, info) in MusicMetadata.All)
            Assert.That(info.Title, Is.Not.Empty);
    }

    [Test]
    public void Lookup_AmbientVariant_StripsParentheticalFromTitle()
    {
        var info = MusicMetadata.Lookup("res://assets/music/Drifting in the Astral Paring (Ambient).wav");

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Title, Is.EqualTo("Drifting in the Astral Paring"));
    }

    [Test]
    public void All_AllTracksHavePositiveBpm()
    {
        // Locked invariant: every registered track must have a known BPM so the
        // forage minigame and any future rhythm-sync features can rely on it.
        foreach (var (path, info) in MusicMetadata.All)
            Assert.That(info.Bpm, Is.GreaterThan(0f), $"Track {path} has BPM <= 0");
    }

    [TestCase("res://assets/music/Carillion Forest.wav", 108f)]
    [TestCase("res://assets/music/Mellyr Outpost.wav", 72f)]
    [TestCase("res://assets/music/Origins Of The Gyre.wav", 148f)]
    [TestCase("res://assets/music/Outpacing.wav", 128f)]
    [TestCase("res://assets/music/Slow Broil.wav", 128f)]
    [TestCase("res://assets/music/Melancholy Conspectus.wav", 140f)]
    [TestCase("res://assets/music/Corruption Can Be Fun.wav", 180f)]
    [TestCase("res://assets/music/Sozitek.wav", 108f)]
    [TestCase("res://assets/music/Drifting in the Astral Paring.wav", 148f)]
    [TestCase("res://assets/music/Drifting in the Astral Paring (Ambient).wav", 148f)]
    [TestCase("res://assets/music/Foggy Morning in Flas.wav", 78f)]
    [TestCase("res://assets/music/A Calm Rain in Argyre.wav", 70f)]
    [TestCase("res://assets/music/Glimmersong Forest.wav", 88f)]
    public void Lookup_TrackBpm_MatchesLockedValue(string path, float expectedBpm)
    {
        var info = MusicMetadata.Lookup(path);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Bpm, Is.EqualTo(expectedBpm));
    }
}
