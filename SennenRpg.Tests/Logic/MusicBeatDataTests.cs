using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// Pure-logic tests for the runtime JSON loader. Exercises the parser and the
/// override layering — neither of which touch Godot FileAccess. The full
/// EnsureLoaded() path requires a Godot runtime and is verified by hand.
/// </summary>
[TestFixture]
public sealed class MusicBeatDataTests
{
    [Test]
    public void ParseJson_ReadsBpmOffsetConfidence()
    {
        const string json = """
        {
          "res://assets/music/Foo.wav": {
            "bpm": 128.0,
            "first_beat_sec": 0.213,
            "confidence": 0.87,
            "beat_count": 412
          }
        }
        """;

        var result = MusicBeatData.ParseJson(json, isOverride: false);
        Assert.That(result.Count, Is.EqualTo(1));
        var entry = result["res://assets/music/Foo.wav"];
        Assert.That(entry.Bpm,          Is.EqualTo(128.0f).Within(0.001f));
        Assert.That(entry.FirstBeatSec, Is.EqualTo(0.213f).Within(0.001f));
        Assert.That(entry.Confidence,   Is.EqualTo(0.87f).Within(0.001f));
        Assert.That(entry.Override,     Is.False);
    }

    [Test]
    public void ParseJson_OverrideFlagPropagates()
    {
        const string json = """{ "res://x.wav": { "bpm": 100, "first_beat_sec": 0 } }""";
        var result = MusicBeatData.ParseJson(json, isOverride: true);
        Assert.That(result["res://x.wav"].Override, Is.True);
    }

    [Test]
    public void ParseJson_MissingConfidenceDefaultsToOne()
    {
        const string json = """{ "res://x.wav": { "bpm": 100, "first_beat_sec": 0 } }""";
        var result = MusicBeatData.ParseJson(json, isOverride: false);
        Assert.That(result["res://x.wav"].Confidence, Is.EqualTo(1f));
    }

    [Test]
    public void ParseJson_EmptyStringReturnsEmpty()
    {
        var result = MusicBeatData.ParseJson("", isOverride: false);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Layer_OverrideWinsOverBase()
    {
        var baseEntries = new System.Collections.Generic.Dictionary<string, MusicBeatData.BeatEntry>
        {
            ["res://a.wav"] = new(100f, 0f,    0.9f, false),
            ["res://b.wav"] = new(120f, 0.05f, 0.7f, false),
        };
        var overrides = new System.Collections.Generic.Dictionary<string, MusicBeatData.BeatEntry>
        {
            ["res://a.wav"] = new(110f, 0.10f, 1.0f, true),  // wins
        };

        var merged = MusicBeatData.Layer(baseEntries, overrides);
        Assert.That(merged.Count, Is.EqualTo(2));
        Assert.That(merged["res://a.wav"].Bpm,      Is.EqualTo(110f));
        Assert.That(merged["res://a.wav"].Override, Is.True);
        Assert.That(merged["res://b.wav"].Bpm,      Is.EqualTo(120f));
        Assert.That(merged["res://b.wav"].Override, Is.False);
    }

    [Test]
    public void Layer_NewKeysInOverrideAreAdded()
    {
        var baseEntries = new System.Collections.Generic.Dictionary<string, MusicBeatData.BeatEntry>
        {
            ["res://a.wav"] = new(100f, 0f, 0.9f, false),
        };
        var overrides = new System.Collections.Generic.Dictionary<string, MusicBeatData.BeatEntry>
        {
            ["res://b.wav"] = new(140f, 0f, 1.0f, true),
        };

        var merged = MusicBeatData.Layer(baseEntries, overrides);
        Assert.That(merged.Count, Is.EqualTo(2));
        Assert.That(merged.ContainsKey("res://b.wav"), Is.True);
    }
}
