using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// Shared UI theme constants for Chrono Trigger-inspired SNES styling.
/// All code-built menus should use these instead of hardcoded colors.
/// </summary>
public static class UiTheme
{
    // ── Color palette ─────────────────────────────────────────────────

    /// <summary>Dark blue panel background.</summary>
    public static readonly Color PanelBg       = new(0.06f, 0.09f, 0.22f, 1f);

    /// <summary>Light blue border for panels and separators.</summary>
    public static readonly Color PanelBorder   = new(0.45f, 0.55f, 0.85f);

    /// <summary>Full-screen dim overlay behind menus.</summary>
    public static readonly Color OverlayDim    = new(0f, 0f, 0f, 0.75f);

    /// <summary>Gold accent for titles and highlights.</summary>
    public static readonly Color Gold          = new(1.0f, 0.85f, 0.1f);

    /// <summary>Subtle grey for hints and disabled text.</summary>
    public static readonly Color SubtleGrey    = new(0.55f, 0.55f, 0.55f);

    /// <summary>Green for positive values (HP healed, items available).</summary>
    public static readonly Color HaveGreen     = new(0.3f, 0.9f, 0.4f);

    /// <summary>Red for negative values (missing items, errors).</summary>
    public static readonly Color NeedRed       = new(0.9f, 0.3f, 0.3f);

    /// <summary>Blue for MP-related values.</summary>
    public static readonly Color MpBlue        = new(0.4f, 0.6f, 1.0f);

    /// <summary>Link/URL color.</summary>
    public static readonly Color LinkBlue      = new(0.4f, 0.7f, 1.0f);

    /// <summary>Active tab/selection highlight.</summary>
    public static readonly Color ActiveHighlight = new(0.5f, 0.7f, 1.0f);

    // ── Panel styling ─────────────────────────────────────────────────

    /// <summary>Corner radius for all panels.</summary>
    public const int CornerRadius = 6;

    /// <summary>Border width for all panels.</summary>
    public const int BorderWidth = 2;

    /// <summary>
    /// Creates a Chrono Trigger-style blue gradient panel StyleBoxFlat.
    /// </summary>
    public static StyleBoxFlat CreatePanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor               = PanelBg,
            BorderWidthLeft       = BorderWidth,
            BorderWidthRight      = BorderWidth,
            BorderWidthTop        = BorderWidth,
            BorderWidthBottom     = BorderWidth,
            BorderColor           = PanelBorder,
            CornerRadiusTopLeft    = CornerRadius,
            CornerRadiusTopRight   = CornerRadius,
            CornerRadiusBottomLeft = CornerRadius,
            CornerRadiusBottomRight = CornerRadius,
        };
    }

    /// <summary>
    /// Creates a button StyleBoxFlat for normal state.
    /// </summary>
    public static StyleBoxFlat CreateButtonStyle()
    {
        return new StyleBoxFlat
        {
            BgColor               = new Color(0.10f, 0.14f, 0.32f),
            BorderWidthLeft       = 1,
            BorderWidthRight      = 1,
            BorderWidthTop        = 1,
            BorderWidthBottom     = 1,
            BorderColor           = PanelBorder,
            CornerRadiusTopLeft    = 4,
            CornerRadiusTopRight   = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft     = 8,
            ContentMarginRight    = 8,
            ContentMarginTop      = 4,
            ContentMarginBottom   = 4,
        };
    }

    /// <summary>
    /// Creates a button StyleBoxFlat for hover/focus state.
    /// </summary>
    public static StyleBoxFlat CreateButtonHoverStyle()
    {
        return new StyleBoxFlat
        {
            BgColor               = new Color(0.15f, 0.20f, 0.42f),
            BorderWidthLeft       = 1,
            BorderWidthRight      = 1,
            BorderWidthTop        = 1,
            BorderWidthBottom     = 1,
            BorderColor           = new Color(0.6f, 0.7f, 1.0f),
            CornerRadiusTopLeft    = 4,
            CornerRadiusTopRight   = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft     = 8,
            ContentMarginRight    = 8,
            ContentMarginTop      = 4,
            ContentMarginBottom   = 4,
        };
    }

    // ── Font paths ────────────────────────────────────────────────────

    public const string PixelFontPath = "res://assets/fonts/PressStart2P-Regular.ttf";

    /// <summary>Load the SNES pixel font. Returns null if not found.</summary>
    public static Font? LoadPixelFont()
    {
        return ResourceLoader.Exists(PixelFontPath)
            ? GD.Load<Font>(PixelFontPath) : null;
    }

    /// <summary>
    /// Recursively apply the pixel font to all Label and RichTextLabel nodes in the subtree.
    /// </summary>
    public static void ApplyPixelFontToAll(Node root)
    {
        var font = LoadPixelFont();
        if (font == null) return;
        ApplyPixelFontRecursive(root, font);
    }

    private static void ApplyPixelFontRecursive(Node node, Font font)
    {
        if (node is Label lbl)
            lbl.AddThemeFontOverride("font", font);
        foreach (var child in node.GetChildren())
            ApplyPixelFontRecursive(child, font);
    }

    /// <summary>
    /// Apply SNES theme overrides to a PanelContainer.
    /// </summary>
    public static void ApplyPanelTheme(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
    }

    /// <summary>
    /// Recursively apply SNES button theme to all Button nodes in the subtree.
    /// Call from _Ready() to theme all buttons in a scene.
    /// </summary>
    public static void ApplyToAllButtons(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Button btn)
                ApplyButtonTheme(btn);
            ApplyToAllButtons(child);
        }
    }

    /// <summary>
    /// Apply SNES theme overrides to a Button (normal + hover + focus states).
    /// </summary>
    public static void ApplyButtonTheme(Button button)
    {
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle());
        button.AddThemeStyleboxOverride("hover", CreateButtonHoverStyle());
        button.AddThemeStyleboxOverride("focus", CreateButtonHoverStyle());
        button.AddThemeStyleboxOverride("pressed", CreateButtonHoverStyle());
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Gold);
        button.AddThemeColorOverride("font_focus_color", Gold);

        var font = LoadPixelFont();
        if (font != null)
            button.AddThemeFontOverride("font", font);
    }
}
