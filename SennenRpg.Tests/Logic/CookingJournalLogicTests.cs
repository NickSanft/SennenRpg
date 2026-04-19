using System.Collections.Generic;
using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class CookingJournalLogicTests
{
    // ── RecordAttempt ──────────────────────────────────────────────────────────

    [Test]
    public void RecordAttempt_NewEntry_Created()
    {
        var journal = new Dictionary<string, string>();
        var (updated, improved) = CookingJournalLogic.RecordAttempt(journal, "stew", CookingQuality.Normal);
        Assert.That(improved, Is.True);
        Assert.That(updated["stew"], Is.EqualTo("Normal"));
    }

    [Test]
    public void RecordAttempt_UpgradeBurntToNormal()
    {
        var journal = new Dictionary<string, string> { ["stew"] = "Burnt" };
        var (updated, improved) = CookingJournalLogic.RecordAttempt(journal, "stew", CookingQuality.Normal);
        Assert.That(improved, Is.True);
        Assert.That(updated["stew"], Is.EqualTo("Normal"));
    }

    [Test]
    public void RecordAttempt_UpgradeNormalToPerfect()
    {
        var journal = new Dictionary<string, string> { ["stew"] = "Normal" };
        var (updated, improved) = CookingJournalLogic.RecordAttempt(journal, "stew", CookingQuality.Perfect);
        Assert.That(improved, Is.True);
        Assert.That(updated["stew"], Is.EqualTo("Perfect"));
    }

    [Test]
    public void RecordAttempt_NoDowngrade_PerfectToBurnt()
    {
        var journal = new Dictionary<string, string> { ["stew"] = "Perfect" };
        var (updated, improved) = CookingJournalLogic.RecordAttempt(journal, "stew", CookingQuality.Burnt);
        Assert.That(improved, Is.False);
        Assert.That(updated["stew"], Is.EqualTo("Perfect"));
    }

    [Test]
    public void RecordAttempt_NoDowngrade_NormalToBurnt()
    {
        var journal = new Dictionary<string, string> { ["stew"] = "Normal" };
        var (updated, improved) = CookingJournalLogic.RecordAttempt(journal, "stew", CookingQuality.Burnt);
        Assert.That(improved, Is.False);
        Assert.That(updated["stew"], Is.EqualTo("Normal"));
    }

    // ── IsBetterQuality ───────────────────────────────────────────────────────

    [Test]
    public void IsBetterQuality_NullStored_ReturnsTrue()
    {
        Assert.That(CookingJournalLogic.IsBetterQuality(CookingQuality.Burnt, null), Is.True);
    }

    [Test]
    public void IsBetterQuality_SameQuality_ReturnsFalse()
    {
        Assert.That(CookingJournalLogic.IsBetterQuality(CookingQuality.Normal, CookingQuality.Normal), Is.False);
    }

    [Test]
    public void IsBetterQuality_Upgrade_ReturnsTrue()
    {
        Assert.That(CookingJournalLogic.IsBetterQuality(CookingQuality.Perfect, CookingQuality.Normal), Is.True);
    }

    [Test]
    public void IsBetterQuality_Downgrade_ReturnsFalse()
    {
        Assert.That(CookingJournalLogic.IsBetterQuality(CookingQuality.Burnt, CookingQuality.Perfect), Is.False);
    }

    // ── CountByQuality ────────────────────────────────────────────────────────

    [Test]
    public void CountByQuality_Empty_ReturnsZeros()
    {
        var journal = new Dictionary<string, string>();
        var (burnt, normal, perfect) = CookingJournalLogic.CountByQuality(journal);
        Assert.That(burnt, Is.EqualTo(0));
        Assert.That(normal, Is.EqualTo(0));
        Assert.That(perfect, Is.EqualTo(0));
    }

    [Test]
    public void CountByQuality_MixedEntries_CountedCorrectly()
    {
        var journal = new Dictionary<string, string>
        {
            ["stew"] = "Burnt",
            ["pie"] = "Normal",
            ["cake"] = "Perfect",
            ["soup"] = "Normal",
        };
        var (burnt, normal, perfect) = CookingJournalLogic.CountByQuality(journal);
        Assert.That(burnt, Is.EqualTo(1));
        Assert.That(normal, Is.EqualTo(2));
        Assert.That(perfect, Is.EqualTo(1));
    }

    // ── GetDisplayInfo ────────────────────────────────────────────────────────

    [Test]
    public void GetDisplayInfo_Discovered_ShowsRealNameAndBadge()
    {
        var journal = new Dictionary<string, string> { ["stew"] = "Perfect" };
        var (name, badge, discovered) = CookingJournalLogic.GetDisplayInfo("stew", "Hearty Stew", journal);
        Assert.That(discovered, Is.True);
        Assert.That(name, Is.EqualTo("Hearty Stew"));
        Assert.That(badge, Is.EqualTo("[Perfect]"));
    }

    [Test]
    public void GetDisplayInfo_Undiscovered_ShowsQuestionMarks()
    {
        var journal = new Dictionary<string, string>();
        var (name, badge, discovered) = CookingJournalLogic.GetDisplayInfo("stew", "Hearty Stew", journal);
        Assert.That(discovered, Is.False);
        Assert.That(name, Is.EqualTo("???"));
        Assert.That(badge, Is.EqualTo(""));
    }

    [Test]
    public void GetDisplayInfo_NullJournal_ShowsQuestionMarks()
    {
        var (name, badge, discovered) = CookingJournalLogic.GetDisplayInfo("stew", "Hearty Stew", null!);
        Assert.That(discovered, Is.False);
        Assert.That(name, Is.EqualTo("???"));
    }

    [Test]
    public void GetDisplayInfo_BurntBadge()
    {
        var journal = new Dictionary<string, string> { ["stew"] = "Burnt" };
        var (_, badge, _) = CookingJournalLogic.GetDisplayInfo("stew", "Stew", journal);
        Assert.That(badge, Is.EqualTo("[Burnt]"));
    }

    [Test]
    public void GetDisplayInfo_NormalBadge()
    {
        var journal = new Dictionary<string, string> { ["stew"] = "Normal" };
        var (_, badge, _) = CookingJournalLogic.GetDisplayInfo("stew", "Stew", journal);
        Assert.That(badge, Is.EqualTo("[Normal]"));
    }
}
