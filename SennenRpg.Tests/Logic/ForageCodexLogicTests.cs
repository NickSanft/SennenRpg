using System;
using System.Collections.Generic;
using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class ForageCodexLogicTests
{
    private static readonly DateTime T0 = new DateTime(2026, 4, 6, 14, 32, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = T0.AddMinutes(5);
    private static readonly DateTime T2 = T0.AddHours(1);

    [Test]
    public void RecordFind_FirstTime_SetsTimestampAndCount()
    {
        var entry = ForageCodexLogic.RecordFind(null, ForageLogic.ForageGrade.Good, T0);

        Assert.That(entry.FirstFoundUtc, Is.EqualTo(T0));
        Assert.That(entry.TimesFound,    Is.EqualTo(1));
        Assert.That(entry.BestGradeRaw,  Is.EqualTo((int)ForageLogic.ForageGrade.Good));
    }

    [Test]
    public void RecordFind_SecondTime_PreservesFirstFoundUtc()
    {
        var first  = ForageCodexLogic.RecordFind(null,  ForageLogic.ForageGrade.Good, T0);
        var second = ForageCodexLogic.RecordFind(first, ForageLogic.ForageGrade.Good, T1);

        // Critical: re-finding must NOT overwrite the original discovery timestamp.
        Assert.That(second.FirstFoundUtc, Is.EqualTo(T0));
        Assert.That(second.TimesFound,    Is.EqualTo(2));
    }

    [Test]
    public void RecordFind_BetterGrade_UpgradesBestGrade()
    {
        var first  = ForageCodexLogic.RecordFind(null,  ForageLogic.ForageGrade.Miss,    T0);
        var second = ForageCodexLogic.RecordFind(first, ForageLogic.ForageGrade.Perfect, T1);

        Assert.That(second.BestGradeRaw, Is.EqualTo((int)ForageLogic.ForageGrade.Perfect));
    }

    [Test]
    public void RecordFind_WorseGrade_NeverDowngradesBest()
    {
        var first  = ForageCodexLogic.RecordFind(null,  ForageLogic.ForageGrade.Perfect, T0);
        var second = ForageCodexLogic.RecordFind(first, ForageLogic.ForageGrade.Miss,    T1);

        Assert.That(second.BestGradeRaw, Is.EqualTo((int)ForageLogic.ForageGrade.Perfect));
    }

    [Test]
    public void RecordFind_TimesFound_AccumulatesAcrossManyCalls()
    {
        var entry = ForageCodexLogic.RecordFind(null, ForageLogic.ForageGrade.Good, T0);
        for (int i = 0; i < 9; i++)
            entry = ForageCodexLogic.RecordFind(entry, ForageLogic.ForageGrade.Good, T0.AddMinutes(i + 1));

        Assert.That(entry.TimesFound, Is.EqualTo(10));
    }

    [Test]
    public void IsUnlocked_KnownPath_True()
    {
        var dict = new Dictionary<string, ForageCodexEntry>
        {
            ["res://test/foo.tres"] = ForageCodexLogic.RecordFind(null, ForageLogic.ForageGrade.Good, T0),
        };
        Assert.That(ForageCodexLogic.IsUnlocked(dict, "res://test/foo.tres"), Is.True);
    }

    [Test]
    public void IsUnlocked_UnknownPath_False()
    {
        var dict = new Dictionary<string, ForageCodexEntry>();
        Assert.That(ForageCodexLogic.IsUnlocked(dict, "res://test/missing.tres"), Is.False);
    }

    [Test]
    public void EntriesSorted_Alphabetical_StableByDisplayName()
    {
        var dict = new Dictionary<string, ForageCodexEntry>
        {
            ["res://b.tres"] = ForageCodexLogic.RecordFind(null, ForageLogic.ForageGrade.Good, T0),
            ["res://a.tres"] = ForageCodexLogic.RecordFind(null, ForageLogic.ForageGrade.Good, T1),
            ["res://c.tres"] = ForageCodexLogic.RecordFind(null, ForageLogic.ForageGrade.Good, T2),
        };

        // Display name is the path's basename so alphabetical = a, b, c.
        var sorted = ForageCodexLogic.EntriesSortedAlphabetical(
            dict, path => path.Replace("res://", "").Replace(".tres", ""));

        Assert.That(sorted, Has.Count.EqualTo(3));
        Assert.That(sorted[0].Key, Is.EqualTo("res://a.tres"));
        Assert.That(sorted[1].Key, Is.EqualTo("res://b.tres"));
        Assert.That(sorted[2].Key, Is.EqualTo("res://c.tres"));
    }

    [Test]
    public void TotalFinds_SumsAllEntryCounts()
    {
        var dict = new Dictionary<string, ForageCodexEntry>();
        var a = ForageCodexLogic.RecordFind(null, ForageLogic.ForageGrade.Good, T0);
        a = ForageCodexLogic.RecordFind(a, ForageLogic.ForageGrade.Good, T1);
        a = ForageCodexLogic.RecordFind(a, ForageLogic.ForageGrade.Good, T2);
        dict["res://a.tres"] = a;
        dict["res://b.tres"] = ForageCodexLogic.RecordFind(null, ForageLogic.ForageGrade.Good, T0);

        Assert.That(ForageCodexLogic.TotalFinds(dict), Is.EqualTo(4));
    }
}
