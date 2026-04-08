using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Collections.Generic;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class MultiClassDataTests
{
    private MultiClassData _data = null!;

    [SetUp]
    public void SetUp() => _data = new MultiClassData();

    [Test]
    public void InitializeStartingClass_SetsActiveClassAndEntry()
    {
        var entry = new ClassProgressionEntry
        {
            Class = PlayerClass.Fighter, Level = 1, MaxHp = 25, Attack = 12,
        };

        _data.InitializeStartingClass(PlayerClass.Fighter, entry);

        Assert.That(_data.ActiveClass, Is.EqualTo(PlayerClass.Fighter));
        Assert.That(_data.ClassEntries, Has.Count.EqualTo(1));
        Assert.That(_data.ClassEntries[PlayerClass.Fighter].MaxHp, Is.EqualTo(25));
    }

    [Test]
    public void SwitchTo_ExistingClass_ReturnsStoredEntry()
    {
        var bard = new ClassProgressionEntry
        {
            Class = PlayerClass.Bard, Level = 3, Attack = 8,
        };
        var fighter = new ClassProgressionEntry
        {
            Class = PlayerClass.Fighter, Level = 5, Attack = 15,
        };
        _data.InitializeStartingClass(PlayerClass.Bard, bard);
        _data.ClassEntries[PlayerClass.Fighter] = fighter;

        var result = _data.SwitchTo(PlayerClass.Fighter);

        Assert.That(_data.ActiveClass, Is.EqualTo(PlayerClass.Fighter));
        Assert.That(result.Level, Is.EqualTo(5));
        Assert.That(result.Attack, Is.EqualTo(15));
    }

    [Test]
    public void SwitchTo_NewClass_UsesFactory()
    {
        _data.InitializeStartingClass(PlayerClass.Bard,
            new ClassProgressionEntry { Class = PlayerClass.Bard });

        var result = _data.SwitchTo(PlayerClass.Mage, cls => new ClassProgressionEntry
        {
            Class = cls, Level = 1, Magic = 12, MaxMp = 10,
        });

        Assert.That(_data.ActiveClass, Is.EqualTo(PlayerClass.Mage));
        Assert.That(result.Magic, Is.EqualTo(12));
        Assert.That(result.MaxMp, Is.EqualTo(10));
        Assert.That(_data.ClassEntries, Has.Count.EqualTo(2));
    }

    [Test]
    public void SwitchTo_NewClass_NoFactory_CreatesDefault()
    {
        _data.InitializeStartingClass(PlayerClass.Bard,
            new ClassProgressionEntry { Class = PlayerClass.Bard });

        var result = _data.SwitchTo(PlayerClass.Ranger);

        Assert.That(result.Class, Is.EqualTo(PlayerClass.Ranger));
        Assert.That(result.Level, Is.EqualTo(1));
    }

    [Test]
    public void SaveActiveClassState_UpdatesEntry()
    {
        _data.InitializeStartingClass(PlayerClass.Bard,
            new ClassProgressionEntry { Class = PlayerClass.Bard, Level = 1, Attack = 5 });

        _data.SaveActiveClassState(
            level: 3, exp: 180,
            maxHp: 24, attack: 12, defense: 3, speed: 15,
            magic: 8, resistance: 4, luck: 7, maxMp: 5);

        var entry = _data.ClassEntries[PlayerClass.Bard];
        Assert.That(entry.Level, Is.EqualTo(3));
        Assert.That(entry.Exp, Is.EqualTo(180));
        Assert.That(entry.Attack, Is.EqualTo(12));
        Assert.That(entry.Speed, Is.EqualTo(15));
    }

    [Test]
    public void GetAllClassLevels_ReturnsAllEntries()
    {
        _data.InitializeStartingClass(PlayerClass.Bard,
            new ClassProgressionEntry { Class = PlayerClass.Bard, Level = 3 });
        _data.ClassEntries[PlayerClass.Fighter] =
            new ClassProgressionEntry { Class = PlayerClass.Fighter, Level = 7 };

        var levels = _data.GetAllClassLevels();

        Assert.That(levels, Has.Count.EqualTo(2));
        Assert.That(levels[PlayerClass.Bard], Is.EqualTo(3));
        Assert.That(levels[PlayerClass.Fighter], Is.EqualTo(7));
    }

    [Test]
    public void UpdateActiveClassProgression_SyncsLevelAndExp()
    {
        _data.InitializeStartingClass(PlayerClass.Bard,
            new ClassProgressionEntry { Class = PlayerClass.Bard, Level = 1, Exp = 0 });

        _data.UpdateActiveClassProgression(level: 4, exp: 320);

        Assert.That(_data.ClassEntries[PlayerClass.Bard].Level, Is.EqualTo(4));
        Assert.That(_data.ClassEntries[PlayerClass.Bard].Exp, Is.EqualTo(320));
    }

    [Test]
    public void Reset_ClearsEverything()
    {
        _data.InitializeStartingClass(PlayerClass.Fighter,
            new ClassProgressionEntry { Class = PlayerClass.Fighter });

        _data.Reset();

        Assert.That(_data.ClassEntries, Is.Empty);
        Assert.That(_data.ActiveClass, Is.EqualTo(PlayerClass.Bard));
    }

    [Test]
    public void SwitchTo_Rogue_FromBard_CreatesNewEntryAndActivates()
    {
        _data.InitializeStartingClass(PlayerClass.Bard,
            new ClassProgressionEntry { Class = PlayerClass.Bard, Level = 5 });

        var result = _data.SwitchTo(PlayerClass.Rogue, cls => new ClassProgressionEntry
        {
            Class = cls, Level = 1, MaxHp = 18, Speed = 14, Luck = 11
        });

        Assert.That(_data.ActiveClass, Is.EqualTo(PlayerClass.Rogue));
        Assert.That(result.Speed, Is.EqualTo(14));
        Assert.That(result.Luck, Is.EqualTo(11));
        Assert.That(_data.ClassEntries, Has.Count.EqualTo(2));
    }

    [Test]
    public void SwitchTo_Alchemist_FromBard_CreatesNewEntryAndActivates()
    {
        _data.InitializeStartingClass(PlayerClass.Bard,
            new ClassProgressionEntry { Class = PlayerClass.Bard, Level = 5 });

        var result = _data.SwitchTo(PlayerClass.Alchemist, cls => new ClassProgressionEntry
        {
            Class = cls, Level = 1, MaxHp = 15, Magic = 12, Luck = 13, MaxMp = 24
        });

        Assert.That(_data.ActiveClass, Is.EqualTo(PlayerClass.Alchemist));
        Assert.That(result.Magic, Is.EqualTo(12));
        Assert.That(result.Luck, Is.EqualTo(13));
        Assert.That(result.MaxMp, Is.EqualTo(24));
    }

    [Test]
    public void ApplyFromSave_RestoresState()
    {
        var entries = new List<ClassProgressionEntry>
        {
            new() { Class = PlayerClass.Bard, Level = 5, Attack = 10 },
            new() { Class = PlayerClass.Mage, Level = 3, Magic = 15 },
        };

        _data.ApplyFromSave(entries, "Mage");

        Assert.That(_data.ActiveClass, Is.EqualTo(PlayerClass.Mage));
        Assert.That(_data.ClassEntries, Has.Count.EqualTo(2));
        Assert.That(_data.ClassEntries[PlayerClass.Bard].Level, Is.EqualTo(5));
        Assert.That(_data.ClassEntries[PlayerClass.Mage].Magic, Is.EqualTo(15));
    }

    [Test]
    public void ApplyFromSave_InvalidClassName_DefaultsToBard()
    {
        _data.ApplyFromSave(new List<ClassProgressionEntry>(), "InvalidClass");

        Assert.That(_data.ActiveClass, Is.EqualTo(PlayerClass.Bard));
    }
}
