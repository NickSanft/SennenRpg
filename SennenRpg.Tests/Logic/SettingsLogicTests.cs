using Godot;
using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class SettingsLogicTests
{
    // ── LinearToDb ────────────────────────────────────────────────────────────

    [Test]
    public void LinearToDb_Zero_ReturnsSilence()
        => Assert.That(SettingsLogic.LinearToDb(0f), Is.EqualTo(-80f));

    [Test]
    public void LinearToDb_Negative_ReturnsSilence()
        => Assert.That(SettingsLogic.LinearToDb(-1f), Is.EqualTo(-80f));

    [Test]
    public void LinearToDb_One_ReturnsZeroDb()
        => Assert.That(SettingsLogic.LinearToDb(1f), Is.EqualTo(0f).Within(0.001f));

    [Test]
    public void LinearToDb_Half_ReturnsApproxMinus6Db()
        => Assert.That(SettingsLogic.LinearToDb(0.5f), Is.EqualTo(-6.021f).Within(0.01f));

    // ── EnemyDifficultyMultiplier ─────────────────────────────────────────────

    [Test]
    public void EnemyDifficultyMultiplier_Easy_Returns60Percent()
        => Assert.That(SettingsLogic.EnemyDifficultyMultiplier(BattleDifficulty.Easy), Is.EqualTo(0.60f));

    [Test]
    public void EnemyDifficultyMultiplier_Normal_Returns100Percent()
        => Assert.That(SettingsLogic.EnemyDifficultyMultiplier(BattleDifficulty.Normal), Is.EqualTo(1.00f));

    [Test]
    public void EnemyDifficultyMultiplier_Hard_Returns150Percent()
        => Assert.That(SettingsLogic.EnemyDifficultyMultiplier(BattleDifficulty.Hard), Is.EqualTo(1.50f));

    // ── EncounterRateMultiplier ───────────────────────────────────────────────

    [Test]
    public void EncounterRateMultiplier_Normal_Returns100Percent()
        => Assert.That(SettingsLogic.EncounterRateMultiplier(EncounterRateMode.Normal), Is.EqualTo(1.00f));

    [Test]
    public void EncounterRateMultiplier_Low_Returns30Percent()
        => Assert.That(SettingsLogic.EncounterRateMultiplier(EncounterRateMode.Low), Is.EqualTo(0.30f));

    [Test]
    public void EncounterRateMultiplier_Off_ReturnsZero()
        => Assert.That(SettingsLogic.EncounterRateMultiplier(EncounterRateMode.Off), Is.EqualTo(0.00f));

    // ── RhythmGoodWindowPx ───────────────────────────────────────────────────

    [Test]
    public void RhythmGoodWindowPx_Normal_Returns22()
        => Assert.That(SettingsLogic.RhythmGoodWindowPx(RhythmTimingWindow.Normal), Is.EqualTo(22f));

    [Test]
    public void RhythmGoodWindowPx_Tight_Returns12()
        => Assert.That(SettingsLogic.RhythmGoodWindowPx(RhythmTimingWindow.Tight), Is.EqualTo(12f));

    [Test]
    public void RhythmGoodWindowPx_Forgiving_Returns40()
        => Assert.That(SettingsLogic.RhythmGoodWindowPx(RhythmTimingWindow.Forgiving), Is.EqualTo(40f));

    [Test]
    public void RhythmGoodWindowPx_AutoHit_ReturnsMaxValue()
        => Assert.That(SettingsLogic.RhythmGoodWindowPx(RhythmTimingWindow.AutoHit), Is.EqualTo(float.MaxValue));

    // ── RhythmPerfectWindowPx ────────────────────────────────────────────────

    [Test]
    public void RhythmPerfectWindowPx_Normal_Returns9()
        => Assert.That(SettingsLogic.RhythmPerfectWindowPx(RhythmTimingWindow.Normal), Is.EqualTo(9f));

    [Test]
    public void RhythmPerfectWindowPx_Tight_Returns5()
        => Assert.That(SettingsLogic.RhythmPerfectWindowPx(RhythmTimingWindow.Tight), Is.EqualTo(5f));

    [Test]
    public void RhythmPerfectWindowPx_Forgiving_Returns18()
        => Assert.That(SettingsLogic.RhythmPerfectWindowPx(RhythmTimingWindow.Forgiving), Is.EqualTo(18f));

    [Test]
    public void RhythmPerfectWindowPx_AutoHit_ReturnsMaxValue()
        => Assert.That(SettingsLogic.RhythmPerfectWindowPx(RhythmTimingWindow.AutoHit), Is.EqualTo(float.MaxValue));

    // ── RhythmTimingScale ────────────────────────────────────────────────────

    [Test]
    public void RhythmTimingScale_Normal_Returns1()
        => Assert.That(SettingsLogic.RhythmTimingScale(RhythmTimingWindow.Normal), Is.EqualTo(1.00f));

    [Test]
    public void RhythmTimingScale_Tight_IsLessThan1()
        => Assert.That(SettingsLogic.RhythmTimingScale(RhythmTimingWindow.Tight), Is.LessThan(1f));

    [Test]
    public void RhythmTimingScale_Forgiving_IsGreaterThan1()
        => Assert.That(SettingsLogic.RhythmTimingScale(RhythmTimingWindow.Forgiving), Is.GreaterThan(1f));

    // ── FontSizePx ───────────────────────────────────────────────────────────

    [Test]
    public void FontSizePx_Small_Returns12()
        => Assert.That(SettingsLogic.FontSizePx(TextSize.Small), Is.EqualTo(12));

    [Test]
    public void FontSizePx_Medium_Returns16()
        => Assert.That(SettingsLogic.FontSizePx(TextSize.Medium), Is.EqualTo(16));

    [Test]
    public void FontSizePx_Large_Returns22()
        => Assert.That(SettingsLogic.FontSizePx(TextSize.Large), Is.EqualTo(22));

    // ── DialogTextSpeed ──────────────────────────────────────────────────────

    [Test]
    public void DialogTextSpeed_Normal_Returns40()
        => Assert.That(SettingsLogic.DialogTextSpeed(BattleTextSpeed.Normal), Is.EqualTo(40f));

    [Test]
    public void DialogTextSpeed_Slow_Returns20()
        => Assert.That(SettingsLogic.DialogTextSpeed(BattleTextSpeed.Slow), Is.EqualTo(20f));

    [Test]
    public void DialogTextSpeed_Fast_Returns80()
        => Assert.That(SettingsLogic.DialogTextSpeed(BattleTextSpeed.Fast), Is.EqualTo(80f));

    [Test]
    public void DialogTextSpeed_Instant_ReturnsZero()
        => Assert.That(SettingsLogic.DialogTextSpeed(BattleTextSpeed.Instant), Is.EqualTo(0f));

    // ── GradeDeviationScaled (via RhythmConstants) ───────────────────────────

    [Test]
    public void GradeDeviationScaled_AutoHit_AlwaysPerfect()
        => Assert.That(RhythmConstants.GradeDeviationScaled(10f, float.MaxValue), Is.EqualTo(HitGrade.Perfect));

    [Test]
    public void GradeDeviationScaled_WithinPerfectWindow_ReturnsPerfect()
        => Assert.That(RhythmConstants.GradeDeviationScaled(0.03f, 1.0f), Is.EqualTo(HitGrade.Perfect));

    [Test]
    public void GradeDeviationScaled_BeyondGoodWindow_ReturnsMiss()
        => Assert.That(RhythmConstants.GradeDeviationScaled(0.15f, 1.0f), Is.EqualTo(HitGrade.Miss));

    [Test]
    public void GradeDeviationScaled_ForgivingScale_WidensWindow()
    {
        // 0.10s is a Miss at normal scale (> GoodWindowSec 0.120 is actually borderline — use 0.14)
        Assert.That(RhythmConstants.GradeDeviationScaled(0.14f, 1.0f),  Is.EqualTo(HitGrade.Miss));
        // At Forgiving scale (1.82) the Good window widens to 0.218s — same deviation is Good
        Assert.That(RhythmConstants.GradeDeviationScaled(0.14f, 1.82f), Is.EqualTo(HitGrade.Good));
    }

    [Test]
    public void GradeDeviationScaled_TightScale_NarrowsWindow()
    {
        // 0.06s is Good at normal scale (> Perfect 0.050s, <= Good 0.120s)
        Assert.That(RhythmConstants.GradeDeviationScaled(0.06f, 1.0f),  Is.EqualTo(HitGrade.Good));
        // At Tight scale (0.55) the Good window shrinks to 0.066s — same deviation is still Good but Perfect window is 0.0275s
        // Use 0.09s which is Good at normal but Miss at tight (0.120 * 0.55 = 0.066)
        Assert.That(RhythmConstants.GradeDeviationScaled(0.09f, 0.55f), Is.EqualTo(HitGrade.Miss));
    }

    // ── HighContrastOutlineSize ───────────────────────────────────────────

    [Test]
    public void HighContrastOutlineSize_Enabled_Returns2()
        => Assert.That(SettingsLogic.HighContrastOutlineSize(true), Is.EqualTo(2));

    [Test]
    public void HighContrastOutlineSize_Disabled_Returns0()
        => Assert.That(SettingsLogic.HighContrastOutlineSize(false), Is.EqualTo(0));

    // ── HpBarColor ────────────────────────────────────────────────────────

    [Test]
    public void HpBarColor_Normal_ReturnsYellow()
    {
        var c = SettingsLogic.HpBarColor(ColorblindMode.Normal);
        Assert.That(c.R, Is.EqualTo(1.00f).Within(0.01f));
        Assert.That(c.G, Is.EqualTo(1.00f).Within(0.01f));
        Assert.That(c.B, Is.EqualTo(0.00f).Within(0.01f));
    }

    [Test]
    public void HpBarColor_Protanopia_ReturnsCyan()
    {
        var c = SettingsLogic.HpBarColor(ColorblindMode.Protanopia);
        Assert.That(c.R, Is.EqualTo(0.00f).Within(0.01f));
        Assert.That(c.G, Is.GreaterThan(0.5f));
        Assert.That(c.B, Is.GreaterThan(0.5f)); // cyan: high G and B
    }

    [Test]
    public void HpBarColor_Deuteranopia_ReturnsOrange()
    {
        var c = SettingsLogic.HpBarColor(ColorblindMode.Deuteranopia);
        Assert.That(c.R, Is.GreaterThan(0.5f));
        Assert.That(c.G, Is.GreaterThan(0.3f));
        Assert.That(c.B, Is.EqualTo(0.00f).Within(0.01f)); // orange: high R, mid G, zero B
    }

    [Test]
    public void HpBarColor_Tritanopia_ReturnsRed()
    {
        var c = SettingsLogic.HpBarColor(ColorblindMode.Tritanopia);
        Assert.That(c.R, Is.GreaterThan(0.5f));
        Assert.That(c.G, Is.EqualTo(0.00f).Within(0.01f));
        Assert.That(c.B, Is.EqualTo(0.00f).Within(0.01f));
    }

    [Test]
    public void HpBarColor_AllModesDistinct()
    {
        var colors = new[]
        {
            SettingsLogic.HpBarColor(ColorblindMode.Normal),
            SettingsLogic.HpBarColor(ColorblindMode.Protanopia),
            SettingsLogic.HpBarColor(ColorblindMode.Deuteranopia),
            SettingsLogic.HpBarColor(ColorblindMode.Tritanopia),
        };
        for (int i = 0; i < colors.Length; i++)
        for (int j = i + 1; j < colors.Length; j++)
            Assert.That(colors[i], Is.Not.EqualTo(colors[j]),
                $"Modes {i} and {j} returned the same color");
    }

    // ── MpBarColor ────────────────────────────────────────────────────────

    [Test]
    public void MpBarColor_Normal_ReturnsBlue()
    {
        var c = SettingsLogic.MpBarColor(ColorblindMode.Normal);
        Assert.That(c.B, Is.GreaterThan(c.R)); // more blue than red
        Assert.That(c.B, Is.GreaterThan(c.G)); // more blue than green
    }

    [Test]
    public void MpBarColor_Tritanopia_ReturnsGreen()
    {
        var c = SettingsLogic.MpBarColor(ColorblindMode.Tritanopia);
        Assert.That(c.G, Is.GreaterThan(c.R));
        Assert.That(c.G, Is.GreaterThan(c.B));
        Assert.That(c.R, Is.EqualTo(0.00f).Within(0.01f));
    }

    [Test]
    public void MpBarColor_NonTritanopia_AllReturnSameBlue()
    {
        // Protanopia and Deuteranopia both fall through to the default blue
        var blue = SettingsLogic.MpBarColor(ColorblindMode.Normal);
        Assert.That(SettingsLogic.MpBarColor(ColorblindMode.Protanopia),   Is.EqualTo(blue));
        Assert.That(SettingsLogic.MpBarColor(ColorblindMode.Deuteranopia), Is.EqualTo(blue));
    }

    // ── EffectiveKey ──────────────────────────────────────────────────────

    [Test]
    public void EffectiveKey_KeycodeSet_ReturnsKeycode()
        => Assert.That(SettingsLogic.EffectiveKey(Key.W, Key.None), Is.EqualTo(Key.W));

    [Test]
    public void EffectiveKey_KeycodeNone_ReturnsPhysical()
        => Assert.That(SettingsLogic.EffectiveKey(Key.None, Key.W), Is.EqualTo(Key.W));

    [Test]
    public void EffectiveKey_BothNone_ReturnsNone()
        => Assert.That(SettingsLogic.EffectiveKey(Key.None, Key.None), Is.EqualTo(Key.None));

    [Test]
    public void EffectiveKey_BothSet_PrefersKeycode()
        => Assert.That(SettingsLogic.EffectiveKey(Key.A, Key.B), Is.EqualTo(Key.A));
}
