using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-data constants describing UI layout constraints.
/// Used by NUnit data-validation tests to catch text overflow before runtime.
/// All values are derived from actual menu BuildUI code.
/// </summary>
public static class UiLayoutConstants
{
    // ── Panel widths (from CustomMinimumSize in menu BuildUI) ─────────

    /// <summary>Standard menu margin (left + right) inside PanelContainer.</summary>
    public const int PanelMarginHorizontal = 32; // 16 left + 16 right

    /// <summary>Panel border width (left + right).</summary>
    public const int PanelBorderHorizontal = 4; // 2 left + 2 right

    // ── Per-menu content widths (panel width - margins - borders) ─────

    /// <summary>LevelUpScreen panel: 400px total → 364px content.</summary>
    public const int LevelUpContentWidth = 400 - PanelMarginHorizontal - PanelBorderHorizontal;

    /// <summary>StatsMenu panel: 420px total → 384px content.</summary>
    public const int StatsContentWidth = 420 - PanelMarginHorizontal - PanelBorderHorizontal;

    /// <summary>SpellsMenu panel: 450px total → 414px content.</summary>
    public const int SpellsContentWidth = 450 - PanelMarginHorizontal - PanelBorderHorizontal;

    /// <summary>CookingMenu panel: 500px total → 464px content.</summary>
    public const int CookingContentWidth = 500 - PanelMarginHorizontal - PanelBorderHorizontal;

    /// <summary>PartyMenu panel: 560px total → 524px content.</summary>
    public const int PartyContentWidth = 560 - PanelMarginHorizontal - PanelBorderHorizontal;

    /// <summary>EquipmentMenu panel: 560px total → 524px content.</summary>
    public const int EquipmentContentWidth = 560 - PanelMarginHorizontal - PanelBorderHorizontal;

    // ── Font metrics (PressStart2P is a monospaced pixel font) ────────

    /// <summary>
    /// Approximate character width in pixels for PressStart2P at a given font size.
    /// PressStart2P is monospaced with an 8×8 native grid. Godot's font renderer
    /// at higher font_size values scales proportionally but the effective width per
    /// glyph is approximately 80% of the font_size setting (empirically measured).
    /// This estimate is conservative enough to catch real overflow while avoiding
    /// false positives on text that renders correctly in-game.
    /// </summary>
    public static int ApproxCharWidth(int fontSize)
        => Math.Max(1, (int)(fontSize * 0.8f));

    /// <summary>
    /// Maximum characters that fit in a given pixel width at a given font size.
    /// Uses the conservative character width estimate.
    /// </summary>
    public static int MaxCharsForWidth(int widthPx, int fontSize)
        => fontSize > 0 ? widthPx / ApproxCharWidth(fontSize) : 0;

    // ── Common font sizes used in menus ───────────────────────────────

    public const int FontSizeTitle = 18;
    public const int FontSizeHeader = 16;
    public const int FontSizeBody = 12;
    public const int FontSizeSmall = 10;
    public const int FontSizeTiny = 8;

    // ── Stat label column widths (from LevelUpScreen/StatsMenu) ──────

    /// <summary>LevelUpScreen stat name column: 110px.</summary>
    public const int LevelUpStatNameWidth = 110;

    /// <summary>LevelUpScreen stat value column: 100px.</summary>
    public const int LevelUpStatValueWidth = 100;

    /// <summary>LevelUpScreen stat delta column: 40px.</summary>
    public const int LevelUpStatDeltaWidth = 40;

    // ── Viewport ─────────────────────────────────────────────────────

    /// <summary>Base viewport width (project setting).</summary>
    public const int ViewportWidth = 1280;

    /// <summary>Base viewport height (project setting).</summary>
    public const int ViewportHeight = 720;
}
