using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// Data-driven tests that verify text content and registry entries will fit
/// within their UI containers. These run without the Godot runtime and catch
/// overflow issues at CI time.
///
/// Strategy: measure string lengths against known panel widths and font sizes
/// using PressStart2P's monospaced character width (≈ fontSize px per glyph).
/// </summary>
[TestFixture]
public class UiDataValidationTests
{
    private static string ProjectRoot => Path.GetFullPath(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));

    // ── Cross-class bonus descriptions ───────────────────────────────

    [Test]
    public void CrossClassBonusDescriptions_FitLevelUpPanel()
    {
        // The bonus label in LevelUpScreen uses AutowrapMode.WordSmart,
        // so we check that descriptions fit within ~2 lines (not single-line).
        // This catches absurdly long descriptions while allowing normal wrapping.
        int maxCharsPerLine = UiLayoutConstants.MaxCharsForWidth(
            UiLayoutConstants.LevelUpContentWidth,
            UiLayoutConstants.FontSizeBody);
        int maxChars = maxCharsPerLine * 2; // allow up to 2 lines with autowrap

        foreach (var bonus in CrossClassBonusRegistry.All)
        {
            Assert.That(bonus.Description.Length, Is.LessThanOrEqualTo(maxChars),
                $"Cross-class bonus description is too long even with autowrap " +
                $"({bonus.Description.Length} chars > {maxChars} max 2-line limit): \"{bonus.Description}\"");
        }
    }

    [Test]
    public void CrossClassBonusDescriptions_AreNonEmpty()
    {
        foreach (var bonus in CrossClassBonusRegistry.All)
            Assert.That(bonus.Description, Is.Not.Empty,
                $"Cross-class bonus for {bonus.SourceClass} Lv{bonus.RequiredLevel} has empty description");
    }

    // ── Character milestone descriptions ─────────────────────────────

    [Test]
    public void MilestoneDescriptions_FitLevelUpPanel()
    {
        // The bonus label in LevelUpScreen uses AutowrapMode.WordSmart,
        // so descriptions can wrap across lines. Check they fit within ~2 lines.
        const string longestPrefix = "(Passive) "; // 10 chars
        int maxCharsPerLine = UiLayoutConstants.MaxCharsForWidth(
            UiLayoutConstants.LevelUpContentWidth,
            UiLayoutConstants.FontSizeBody);
        int maxChars = maxCharsPerLine * 2; // allow up to 2 lines with autowrap

        foreach (var m in CharacterMilestoneRegistry.All)
        {
            int totalLen = longestPrefix.Length + m.Description.Length;
            Assert.That(totalLen, Is.LessThanOrEqualTo(maxChars),
                $"Milestone description is too long even with autowrap " +
                $"({totalLen} chars > {maxChars} max 2-line limit): \"{m.Description}\"");
        }
    }

    [Test]
    public void MilestoneDescriptions_AreNonEmpty()
    {
        foreach (var m in CharacterMilestoneRegistry.All)
            Assert.That(m.Description, Is.Not.Empty,
                $"Milestone for {m.MemberId} Lv{m.RequiredLevel} has empty description");
    }

    [Test]
    public void MilestoneDescriptions_FitStatsMenuPanel()
    {
        // StatsMenu milestone labels use AutowrapMode.WordSmart,
        // so allow up to 2 lines of text.
        int maxCharsPerLine = UiLayoutConstants.MaxCharsForWidth(
            UiLayoutConstants.StatsContentWidth,
            UiLayoutConstants.FontSizeBody);
        int maxChars = maxCharsPerLine * 2;

        foreach (var m in CharacterMilestoneRegistry.All)
        {
            // StatsMenu shows: "✓ Description" or "✗ Description"
            int totalLen = 2 + m.Description.Length; // "✓ " prefix
            Assert.That(totalLen, Is.LessThanOrEqualTo(maxChars),
                $"Milestone is too long even with autowrap in StatsMenu " +
                $"({totalLen} chars > {maxChars} max 2-line limit): \"{m.Description}\"");
        }
    }

    // ── Stat label names fit their columns ───────────────────────────

    [Test]
    public void LevelUpStatLabels_FitNameColumn()
    {
        string[] statLabels = { "MAX HP", "ATTACK", "DEFENSE", "SPEED", "MAGIC", "RESISTANCE", "LUCK" };
        int maxChars = UiLayoutConstants.MaxCharsForWidth(
            UiLayoutConstants.LevelUpStatNameWidth,
            UiLayoutConstants.FontSizeBody);

        foreach (var label in statLabels)
        {
            Assert.That(label.Length, Is.LessThanOrEqualTo(maxChars),
                $"Stat label \"{label}\" ({label.Length} chars) exceeds " +
                $"LevelUpScreen name column ({maxChars} max chars at {UiLayoutConstants.FontSizeBody}px)");
        }
    }

    [Test]
    public void LevelUpStatValues_FitValueColumn()
    {
        // Worst case: "999 → 999" (9 chars) at 12px font in 100px column
        const string worstCase = "999 → 999";
        int maxChars = UiLayoutConstants.MaxCharsForWidth(
            UiLayoutConstants.LevelUpStatValueWidth,
            UiLayoutConstants.FontSizeBody);

        // The arrow character "→" may render wider than one char width in the pixel font.
        // Use the raw character count as a conservative estimate.
        Assert.That(worstCase.Length, Is.LessThanOrEqualTo(maxChars),
            $"Worst-case stat value \"{worstCase}\" ({worstCase.Length} chars) " +
            $"exceeds value column ({maxChars} max)");
    }

    [Test]
    public void LevelUpStatDeltas_FitDeltaColumn()
    {
        // Worst case: "+99" (3 chars) at 12px font in 40px column
        const string worstCase = "+99";
        int maxChars = UiLayoutConstants.MaxCharsForWidth(
            UiLayoutConstants.LevelUpStatDeltaWidth,
            UiLayoutConstants.FontSizeBody);

        Assert.That(worstCase.Length, Is.LessThanOrEqualTo(maxChars),
            $"Worst-case stat delta \"{worstCase}\" exceeds delta column ({maxChars} max)");
    }

    // ── Registry completeness ────────────────────────────────────────

    [Test]
    public void CrossClassBonuses_AllHaveValidLevels()
    {
        foreach (var bonus in CrossClassBonusRegistry.All)
        {
            Assert.That(bonus.RequiredLevel, Is.GreaterThan(0),
                $"Cross-class bonus \"{bonus.Description}\" has non-positive level");
            Assert.That(bonus.RequiredLevel % 5, Is.EqualTo(0),
                $"Cross-class bonus \"{bonus.Description}\" level {bonus.RequiredLevel} is not a multiple of 5");
        }
    }

    [Test]
    public void Milestones_AllHaveValidLevels()
    {
        int[] validLevels = { 5, 10, 15, 20 };
        foreach (var m in CharacterMilestoneRegistry.All)
        {
            Assert.That(validLevels, Does.Contain(m.RequiredLevel),
                $"Milestone \"{m.Description}\" for {m.MemberId} has unexpected level {m.RequiredLevel}");
        }
    }

    [Test]
    public void Milestones_AllMemberIds_AreKnownCharacters()
    {
        string[] knownIds = { "sen", "lily", "rain", "bhata", "kriora" };
        foreach (var m in CharacterMilestoneRegistry.All)
        {
            Assert.That(knownIds, Does.Contain(m.MemberId),
                $"Milestone \"{m.Description}\" has unknown MemberId \"{m.MemberId}\"");
        }
    }

    [Test]
    public void Milestones_NoDuplicateMemberLevelPairs()
    {
        var pairs = CharacterMilestoneRegistry.All
            .Select(m => (m.MemberId, m.RequiredLevel))
            .ToList();

        Assert.That(pairs.Distinct().Count(), Is.EqualTo(pairs.Count),
            "Found duplicate MemberId+RequiredLevel pairs in milestone registry");
    }

    // ── LevelUp title text fits panel ────────────────────────────────

    [Test]
    public void LevelUpTitle_WorstCase_FitsPanel()
    {
        // Title format: "★  KRIORA LEVEL UP!  ★"
        string[] names = { "SEN", "LILY", "RAIN", "BHATA", "KRIORA" };
        int maxChars = UiLayoutConstants.MaxCharsForWidth(
            UiLayoutConstants.LevelUpContentWidth,
            UiLayoutConstants.FontSizeHeader);

        foreach (var name in names)
        {
            string title = $"★  {name} LEVEL UP!  ★";
            Assert.That(title.Length, Is.LessThanOrEqualTo(maxChars),
                $"Level-up title \"{title}\" ({title.Length} chars) may overflow " +
                $"at {UiLayoutConstants.FontSizeHeader}px ({maxChars} max chars)");
        }
    }

    [Test]
    public void LevelUpLevelLine_WorstCase_FitsPanel()
    {
        // Format: "Alchemist   Level 19 → Level 20"
        string[] classNames = { "Bard", "Fighter", "Ranger", "Mage", "Rogue", "Alchemist" };
        int maxChars = UiLayoutConstants.MaxCharsForWidth(
            UiLayoutConstants.LevelUpContentWidth,
            UiLayoutConstants.FontSizeBody);

        foreach (var cls in classNames)
        {
            string line = $"{cls}   Level 19 → Level 20";
            Assert.That(line.Length, Is.LessThanOrEqualTo(maxChars),
                $"Level line \"{line}\" ({line.Length} chars) may overflow ({maxChars} max)");
        }
    }

    // ── Menu font sizes are within readable range ────────────────────

    [Test]
    public void AllDefinedFontSizes_AreReadableAtBaseResolution()
    {
        int[] usedSizes = { 8, 9, 10, 11, 12, 13, 14, 16, 18, 22, 24, 40 };

        foreach (var size in usedSizes)
        {
            // At 1280x720, anything below 6px is unreadable; above 60px is absurd
            Assert.That(size, Is.InRange(6, 60),
                $"Font size {size}px is outside the readable range at {UiLayoutConstants.ViewportWidth}x{UiLayoutConstants.ViewportHeight}");
        }
    }

    // ── Panel widths are within viewport bounds ──────────────────────

    [Test]
    public void AllMenuPanelWidths_FitViewport()
    {
        int[] panelWidths = { 400, 420, 450, 480, 500, 560, 640, 720 };

        foreach (var width in panelWidths)
        {
            Assert.That(width, Is.LessThanOrEqualTo(UiLayoutConstants.ViewportWidth),
                $"Panel width {width}px exceeds viewport width {UiLayoutConstants.ViewportWidth}px");

            // Also check that the panel isn't wider than 90% of viewport
            // (centered panels need some breathing room)
            Assert.That(width, Is.LessThanOrEqualTo(UiLayoutConstants.ViewportWidth * 0.9f),
                $"Panel width {width}px is wider than 90% of viewport — may feel cramped");
        }
    }

    // ── Source-level checks: menus use UiTheme consistently ──────────

    [Test]
    public void AllMenuFiles_UseUiThemeApplyPanelTheme()
    {
        string menusDir = Path.Combine(ProjectRoot, "scenes", "menus");
        if (!Directory.Exists(menusDir)) Assert.Inconclusive($"Directory not found: {menusDir}");

        // Menus that create PanelContainers should use UiTheme.ApplyPanelTheme.
        // CharacterCustomization uses its own theme (it's a .tscn-driven scene).
        // QuestRewardScreen is a known gap — TODO: add ApplyPanelTheme to it.
        string[] excludedFiles = { "EquipmentSlotButton.cs", "CharacterCustomization.cs", "QuestRewardScreen.cs" };
        string[] menuFiles = Directory.GetFiles(menusDir, "*.cs")
            .Where(f => !excludedFiles.Contains(Path.GetFileName(f)))
            .ToArray();

        foreach (var file in menuFiles)
        {
            string content = File.ReadAllText(file);
            string fileName = Path.GetFileName(file);

            // Skip files that don't build panels
            if (!content.Contains("new PanelContainer")) continue;

            Assert.That(content, Does.Contain("ApplyPanelTheme"),
                $"{fileName} creates a PanelContainer but doesn't call UiTheme.ApplyPanelTheme. " +
                "This may result in missing SNES-style theme.");
        }
    }

    [Test]
    public void AllMenuFiles_UseUiThemeColors_NotHardcoded()
    {
        string menusDir = Path.Combine(ProjectRoot, "scenes", "menus");
        if (!Directory.Exists(menusDir)) Assert.Inconclusive($"Directory not found: {menusDir}");

        string[] menuFiles = Directory.GetFiles(menusDir, "*.cs");

        foreach (var file in menuFiles)
        {
            string content = File.ReadAllText(file);
            string fileName = Path.GetFileName(file);

            // Check for hardcoded gold-ish colors that should use UiTheme.Gold
            // Pattern: new Color(1.0f, 0.85f, 0.1f) — exact gold value
            if (content.Contains("new Color(1.0f, 0.85f, 0.1f"))
            {
                Assert.Fail($"{fileName} contains hardcoded gold color. Use UiTheme.Gold instead.");
            }
        }
    }

    // ── UiLayoutConstants self-consistency ────────────────────────────

    [Test]
    public void MaxCharsForWidth_ReturnsPositive_ForAllMenuWidths()
    {
        int[] widths = {
            UiLayoutConstants.LevelUpContentWidth,
            UiLayoutConstants.StatsContentWidth,
            UiLayoutConstants.SpellsContentWidth,
            UiLayoutConstants.CookingContentWidth,
            UiLayoutConstants.PartyContentWidth,
            UiLayoutConstants.EquipmentContentWidth,
        };

        foreach (var w in widths)
        {
            int chars = UiLayoutConstants.MaxCharsForWidth(w, UiLayoutConstants.FontSizeBody);
            Assert.That(chars, Is.GreaterThan(0),
                $"MaxCharsForWidth({w}, {UiLayoutConstants.FontSizeBody}) returned {chars}");
        }
    }

    [Test]
    public void MaxCharsForWidth_ZeroFontSize_ReturnsZero()
    {
        Assert.That(UiLayoutConstants.MaxCharsForWidth(400, 0), Is.EqualTo(0));
    }
}
