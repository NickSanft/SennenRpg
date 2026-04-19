using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// Pins the wire format for <see cref="SaveData.SeenTutorialIds"/> so an accidental
/// rename/removal is caught in CI before it breaks existing player saves.
/// </summary>
[TestFixture]
public class TutorialSaveRoundtripTests
{
    [Test]
    public void SeenTutorialIds_SurvivesJsonRoundtrip()
    {
        var original = new SaveData
        {
            SeenTutorialIds = new HashSet<string>
            {
                TutorialIds.OverworldMovement,
                TutorialIds.BattleFight,
                TutorialIds.Cooking,
            },
        };

        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<SaveData>(json);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored!.SeenTutorialIds, Is.EquivalentTo(original.SeenTutorialIds));
    }

    [Test]
    public void SeenTutorialIds_DefaultsToEmpty_ForLegacySaves()
    {
        // Minimal JSON (no SeenTutorialIds field) = legacy save before the
        // tutorial system existed. Should deserialize with an empty set, not null.
        const string legacyJson = "{\"PlayerLevel\":1,\"Gold\":50}";
        var data = JsonSerializer.Deserialize<SaveData>(legacyJson);

        Assert.That(data, Is.Not.Null);
        Assert.That(data!.SeenTutorialIds, Is.Not.Null);
        Assert.That(data.SeenTutorialIds, Is.Empty);
    }

    [Test]
    public void SeenTutorialIds_EmptySet_RoundtripsCleanly()
    {
        var original = new SaveData { SeenTutorialIds = new HashSet<string>() };
        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<SaveData>(json);

        Assert.That(restored!.SeenTutorialIds, Is.Empty);
    }

    [Test]
    public void SkipTutorialsSetting_DefaultsToFalse()
    {
        // New-player experience: tutorials ON by default.
        var defaults = new SettingsData();
        Assert.That(defaults.SkipTutorials, Is.False);
    }

    [Test]
    public void SkipTutorialsSetting_SurvivesJsonRoundtrip()
    {
        var original = new SettingsData { SkipTutorials = true };
        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<SettingsData>(json);

        Assert.That(restored!.SkipTutorials, Is.True);
    }
}
