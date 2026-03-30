using NUnit.Framework;
using System.Text.Json;
using SennenRpg.Core.Data;
using SennenRpg.Autoloads;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class SaveSlotLogicTests
{
    // ── SaveSlotLogic.IsValidSlot ──────────────────────────────────────────────

    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, true)]
    [TestCase(0, false)]
    [TestCase(4, false)]
    [TestCase(-1, false)]
    public void IsValidSlot_ReturnsExpected(int slot, bool expected)
        => Assert.That(SaveSlotLogic.IsValidSlot(slot), Is.EqualTo(expected));

    // ── SaveSlotLogic.GetSavePath ──────────────────────────────────────────────

    [TestCase(1, "user://save_1.json")]
    [TestCase(2, "user://save_2.json")]
    [TestCase(3, "user://save_3.json")]
    public void GetSavePath_ReturnsCorrectPath(int slot, string expected)
        => Assert.That(SaveSlotLogic.GetSavePath(slot), Is.EqualTo(expected));

    [Test]
    public void GetSavePath_DifferentSlotsReturnDifferentPaths()
    {
        var paths = new[] { SaveSlotLogic.GetSavePath(1), SaveSlotLogic.GetSavePath(2), SaveSlotLogic.GetSavePath(3) };
        Assert.That(paths, Is.Unique);
    }

    // ── SaveSlotLogic.FormatPlayTime ───────────────────────────────────────────

    [TestCase(0,    "0s")]
    [TestCase(30,   "30s")]
    [TestCase(59,   "59s")]
    [TestCase(60,   "1m 00s")]
    [TestCase(90,   "1m 30s")]
    [TestCase(3600, "1h 00m")]
    [TestCase(3661, "1h 01m")]
    [TestCase(7325, "2h 02m")]
    public void FormatPlayTime_ReturnsExpectedString(int totalSeconds, string expected)
        => Assert.That(SaveSlotLogic.FormatPlayTime(totalSeconds), Is.EqualTo(expected));

    [Test]
    public void FormatPlayTime_NegativeInput_TreatedAsZero()
        => Assert.That(SaveSlotLogic.FormatPlayTime(-10), Is.EqualTo("0s"));

    // ── MaxSlots constant ─────────────────────────────────────────────────────

    [Test]
    public void MaxSlots_IsThree()
        => Assert.That(SaveSlotLogic.MaxSlots, Is.EqualTo(3));

    [Test]
    public void MaxSlots_AllSlotsAreValid()
    {
        for (int s = 1; s <= SaveSlotLogic.MaxSlots; s++)
            Assert.That(SaveSlotLogic.IsValidSlot(s), Is.True, $"Slot {s} should be valid");
    }
}

[TestFixture]
public class SaveDataJsonTests
{
    // ── JSON round-trip ────────────────────────────────────────────────────────

    [Test]
    public void SaveData_JsonRoundTrip_PreservesIntFields()
    {
        var original = new SaveData
        {
            PlayerHp    = 18,
            PlayerMaxHp = 30,
            PlayerLevel = 5,
            Gold        = 250,
            Exp         = 1200,
        };

        string   json   = JsonSerializer.Serialize(original);
        SaveData loaded = JsonSerializer.Deserialize<SaveData>(json)!;

        Assert.That(loaded.PlayerHp,    Is.EqualTo(18));
        Assert.That(loaded.PlayerMaxHp, Is.EqualTo(30));
        Assert.That(loaded.PlayerLevel, Is.EqualTo(5));
        Assert.That(loaded.Gold,        Is.EqualTo(250));
        Assert.That(loaded.Exp,         Is.EqualTo(1200));
    }

    [Test]
    public void SaveData_JsonRoundTrip_PreservesSlotMetadata()
    {
        var original = new SaveData
        {
            PlayerName      = "Sen",
            PlayTimeSeconds = 3661,
            Timestamp       = "2026-03-30 14:00",
        };

        string   json   = JsonSerializer.Serialize(original);
        SaveData loaded = JsonSerializer.Deserialize<SaveData>(json)!;

        Assert.That(loaded.PlayerName,      Is.EqualTo("Sen"));
        Assert.That(loaded.PlayTimeSeconds, Is.EqualTo(3661));
        Assert.That(loaded.Timestamp,       Is.EqualTo("2026-03-30 14:00"));
    }

    [Test]
    public void SaveData_JsonRoundTrip_PreservesPaletteColors()
    {
        var original = new SaveData
        {
            PaletteSourceColors = ["ff0000", "00ff00"],
            PaletteTargetColors = ["0000ff", "ffff00"],
        };

        string   json   = JsonSerializer.Serialize(original);
        SaveData loaded = JsonSerializer.Deserialize<SaveData>(json)!;

        Assert.That(loaded.PaletteSourceColors, Is.EqualTo(new[] { "ff0000", "00ff00" }));
        Assert.That(loaded.PaletteTargetColors, Is.EqualTo(new[] { "0000ff", "ffff00" }));
    }

    [Test]
    public void SaveData_JsonRoundTrip_PreservesFlags()
    {
        var original = new SaveData
        {
            Flags = new System.Collections.Generic.Dictionary<string, bool>
            {
                ["intro_seen"]  = true,
                ["boss_killed"] = false,
            },
        };

        string   json   = JsonSerializer.Serialize(original);
        SaveData loaded = JsonSerializer.Deserialize<SaveData>(json)!;

        Assert.That(loaded.Flags["intro_seen"],  Is.True);
        Assert.That(loaded.Flags["boss_killed"], Is.False);
    }

    [Test]
    public void SaveData_DefaultValues_AreCorrect()
    {
        var data = new SaveData();
        Assert.That(data.PlayerLevel,   Is.EqualTo(1));
        Assert.That(data.PlayerName,    Is.EqualTo("Sen"));
        Assert.That(data.PlayTimeSeconds, Is.EqualTo(0));
        Assert.That(data.Timestamp,     Is.EqualTo(""));
        Assert.That(data.LastMapPath,   Is.EqualTo(""));
    }

    [Test]
    public void SaveData_UnknownJsonField_IsIgnoredGracefully()
    {
        // Simulates loading a save from an older/newer version that has extra fields.
        const string json = """
            {
                "PlayerHp": 10,
                "PlayerMaxHp": 20,
                "FutureField": "ignored",
                "PlayerLevel": 2
            }
            """;

        var opts   = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loaded = JsonSerializer.Deserialize<SaveData>(json, opts);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.PlayerHp,    Is.EqualTo(10));
        Assert.That(loaded.PlayerLevel,  Is.EqualTo(2));
    }
}
