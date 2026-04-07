using System;
using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class BestiaryRecordLogicTests
{
    private static readonly DateTime T0 = new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = T0.AddMinutes(5);
    private static readonly DateTime T2 = T0.AddDays(1);

    [Test]
    public void RecordKill_FirstTime_SetsTimestampAndCountOne()
    {
        var entry = BestiaryRecordLogic.RecordKill(null, T0);

        Assert.That(entry.FirstDefeatedUtc, Is.EqualTo(T0));
        Assert.That(entry.TotalKills,       Is.EqualTo(1));
    }

    [Test]
    public void RecordKill_SecondTime_PreservesFirstDefeatedUtc()
    {
        var first  = BestiaryRecordLogic.RecordKill(null,  T0);
        var second = BestiaryRecordLogic.RecordKill(first, T1);

        // Critical: subsequent kills must NOT overwrite the original timestamp.
        Assert.That(second.FirstDefeatedUtc, Is.EqualTo(T0));
        Assert.That(second.TotalKills,       Is.EqualTo(2));
    }

    [Test]
    public void RecordKill_TenKills_AccumulatesCount()
    {
        var entry = BestiaryRecordLogic.RecordKill(null, T0);
        for (int i = 0; i < 9; i++)
            entry = BestiaryRecordLogic.RecordKill(entry, T0.AddMinutes(i + 1));

        Assert.That(entry.TotalKills,       Is.EqualTo(10));
        Assert.That(entry.FirstDefeatedUtc, Is.EqualTo(T0));
    }

    [Test]
    public void RecordKill_NowUtcInjectable_DeterministicTests()
    {
        // Whole point of injectable timestamps: tests should not depend on
        // DateTime.UtcNow. T2 is "tomorrow" relative to T0; we just verify
        // the helper passes it through unchanged.
        var entry = BestiaryRecordLogic.RecordKill(null, T2);
        Assert.That(entry.FirstDefeatedUtc, Is.EqualTo(T2));
    }
}
