using NUnit.Framework;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class StatusLogicTests
{
    // ── PoisonDamage ─────────────────────────────────────────────────────────

    [TestCase(100, 10)]
    [TestCase(50,  5)]
    [TestCase(30,  3)]
    [TestCase(1,   1)]  // minimum 1
    [TestCase(0,   1)]  // 0 maxHp still deals 1
    public void PoisonDamage_ReturnsExpected(int maxHp, int expectedDamage)
        => Assert.That(StatusLogic.PoisonDamage(maxHp), Is.EqualTo(expectedDamage));

    // ── Tick ─────────────────────────────────────────────────────────────────

    [TestCase(3, 2)]
    [TestCase(1, 0)]
    [TestCase(0, 0)]  // cannot go negative
    public void Tick_DecrementsOrClampsAtZero(int input, int expected)
        => Assert.That(StatusLogic.Tick(input), Is.EqualTo(expected));

    // ── HasStatus ────────────────────────────────────────────────────────────

    [Test]
    public void HasStatus_ActiveEffect_ReturnsTrue()
    {
        var statuses = new Dictionary<StatusEffect, int> { [StatusEffect.Poison] = 3 };
        Assert.That(StatusLogic.HasStatus(statuses, StatusEffect.Poison), Is.True);
    }

    [Test]
    public void HasStatus_MissingEffect_ReturnsFalse()
    {
        var statuses = new Dictionary<StatusEffect, int> { [StatusEffect.Poison] = 3 };
        Assert.That(StatusLogic.HasStatus(statuses, StatusEffect.Stun), Is.False);
    }

    [Test]
    public void HasStatus_ZeroTurns_ReturnsFalse()
    {
        var statuses = new Dictionary<StatusEffect, int> { [StatusEffect.Poison] = 0 };
        Assert.That(StatusLogic.HasStatus(statuses, StatusEffect.Poison), Is.False);
    }

    // ── TickAll ──────────────────────────────────────────────────────────────

    [Test]
    public void TickAll_DecrementsAllActiveStatuses()
    {
        var statuses = new Dictionary<StatusEffect, int>
        {
            [StatusEffect.Poison] = 3,
            [StatusEffect.Stun]   = 1,
        };
        StatusLogic.TickAll(statuses);
        Assert.That(statuses[StatusEffect.Poison], Is.EqualTo(2));
        Assert.That(statuses.ContainsKey(StatusEffect.Stun), Is.False);  // expired, removed
    }

    [Test]
    public void TickAll_RemovesExpiredEffects()
    {
        var statuses = new Dictionary<StatusEffect, int> { [StatusEffect.Silence] = 1 };
        StatusLogic.TickAll(statuses);
        Assert.That(statuses, Is.Empty);
    }

    [Test]
    public void TickAll_EmptyDictionary_DoesNotThrow()
    {
        var statuses = new Dictionary<StatusEffect, int>();
        Assert.DoesNotThrow(() => StatusLogic.TickAll(statuses));
    }

    // ── Apply ────────────────────────────────────────────────────────────────

    [Test]
    public void Apply_AddsNewEffect()
    {
        var statuses = new Dictionary<StatusEffect, int>();
        StatusLogic.Apply(statuses, StatusEffect.Shield, 2);
        Assert.That(statuses[StatusEffect.Shield], Is.EqualTo(2));
    }

    [Test]
    public void Apply_OverwritesExistingEffect()
    {
        var statuses = new Dictionary<StatusEffect, int> { [StatusEffect.Poison] = 1 };
        StatusLogic.Apply(statuses, StatusEffect.Poison, 5);
        Assert.That(statuses[StatusEffect.Poison], Is.EqualTo(5));
    }

    [Test]
    public void Apply_ZeroTurns_DoesNotAdd()
    {
        var statuses = new Dictionary<StatusEffect, int>();
        StatusLogic.Apply(statuses, StatusEffect.Stun, 0);
        Assert.That(statuses, Is.Empty);
    }

    // ── IconText ─────────────────────────────────────────────────────────────

    [TestCase(StatusEffect.Poison,  "PSN")]
    [TestCase(StatusEffect.Stun,    "STN")]
    [TestCase(StatusEffect.Shield,  "SHD")]
    [TestCase(StatusEffect.Silence, "SIL")]
    public void IconText_ReturnsExpectedAbbreviation(StatusEffect effect, string expected)
        => Assert.That(StatusLogic.IconText(effect), Is.EqualTo(expected));

    // ── TryParseStatusSignal ─────────────────────────────────────────────────

    [Test]
    public void TryParseStatusSignal_ValidInput_ReturnsTrue()
    {
        bool result = StatusLogic.TryParseStatusSignal("poison:3", out StatusEffect effect, out int turns);
        Assert.That(result,  Is.True);
        Assert.That(effect,  Is.EqualTo(StatusEffect.Poison));
        Assert.That(turns,   Is.EqualTo(3));
    }

    [Test]
    public void TryParseStatusSignal_CaseInsensitive()
    {
        bool result = StatusLogic.TryParseStatusSignal("STUN:2", out StatusEffect effect, out int turns);
        Assert.That(result, Is.True);
        Assert.That(effect, Is.EqualTo(StatusEffect.Stun));
        Assert.That(turns,  Is.EqualTo(2));
    }

    [Test]
    public void TryParseStatusSignal_UnknownEffect_ReturnsFalse()
    {
        bool result = StatusLogic.TryParseStatusSignal("unknown:3", out _, out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParseStatusSignal_MissingColon_ReturnsFalse()
    {
        bool result = StatusLogic.TryParseStatusSignal("poison", out _, out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParseStatusSignal_ZeroTurns_ReturnsFalse()
    {
        bool result = StatusLogic.TryParseStatusSignal("poison:0", out _, out _);
        Assert.That(result, Is.False);
    }
}
