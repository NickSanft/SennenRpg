using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class LevelDataTests
{
	// ── ExpThreshold ──────────────────────────────────────────────────────────

	[Test]
	public void ExpThreshold_Level1_Is20()
		=> Assert.That(LevelData.ExpThreshold(1), Is.EqualTo(20));

	[Test]
	public void ExpThreshold_Level2_Is80()
		=> Assert.That(LevelData.ExpThreshold(2), Is.EqualTo(80));

	[Test]
	public void ExpThreshold_Level3_Is180()
		=> Assert.That(LevelData.ExpThreshold(3), Is.EqualTo(180));

	[Test]
	public void ExpThreshold_IsStrictlyIncreasing()
	{
		for (int lv = 1; lv < 10; lv++)
			Assert.That(LevelData.ExpThreshold(lv + 1), Is.GreaterThan(LevelData.ExpThreshold(lv)));
	}

	// ── CheckLevelUp ──────────────────────────────────────────────────────────

	[Test]
	public void CheckLevelUp_BelowThreshold_ReturnsZero()
		=> Assert.That(LevelData.CheckLevelUp(currentExp: 79, currentLevel: 1), Is.EqualTo(0));

	[Test]
	public void CheckLevelUp_ExactThreshold_ReturnsOne()
		=> Assert.That(LevelData.CheckLevelUp(currentExp: 80, currentLevel: 1), Is.EqualTo(1));

	[Test]
	public void CheckLevelUp_AboveThreshold_ReturnsOne()
		=> Assert.That(LevelData.CheckLevelUp(currentExp: 150, currentLevel: 1), Is.EqualTo(1));

	[Test]
	public void CheckLevelUp_MultiLevel_ReturnsCorrectCount()
	{
		// 200 EXP from level 1: passes thresholds for lv2 (80) and lv3 (180)
		Assert.That(LevelData.CheckLevelUp(currentExp: 200, currentLevel: 1), Is.EqualTo(2));
	}

	[Test]
	public void CheckLevelUp_AtMaxLevel_ReturnsZero()
		=> Assert.That(LevelData.CheckLevelUp(currentExp: 999_999, currentLevel: LevelData.MaxLevel), Is.EqualTo(0));

	[Test]
	public void CheckLevelUp_StartsAtLevelTwo_UsesCorrectThreshold()
	{
		// Lv 2 → Lv 3 threshold = ExpThreshold(3) = 180
		Assert.That(LevelData.CheckLevelUp(currentExp: 179, currentLevel: 2), Is.EqualTo(0));
		Assert.That(LevelData.CheckLevelUp(currentExp: 180, currentLevel: 2), Is.EqualTo(1));
	}

	[Test]
	public void CheckLevelUp_ZeroExp_ReturnsZero()
		=> Assert.That(LevelData.CheckLevelUp(currentExp: 0, currentLevel: 1), Is.EqualTo(0));
}
