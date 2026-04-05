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
    public void All_ContainsNineTracks()
    {
        Assert.That(MusicMetadata.All, Has.Count.EqualTo(10));
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
}
