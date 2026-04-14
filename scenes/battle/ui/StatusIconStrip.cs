using Godot;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// A compact row of colored status badges. Each badge is an 18x18 panel with a
/// one-letter label (first letter of the status name) centered, and a small
/// turns-remaining number in the bottom-right corner.
///
/// Pass a <c>Dictionary&lt;StatusEffect, int&gt;</c> to <see cref="SetStatuses"/>
/// (or a <c>Dictionary&lt;string, int&gt;</c> via <see cref="SetStatusesByName"/>)
/// and the row rebuilds itself.
/// </summary>
public partial class StatusIconStrip : HBoxContainer
{
    private const int IconSize        = 18;
    private const int LetterFontSize  = 11;
    private const int TurnsFontSize   = 9;
    private const int IconSeparation  = 2;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", IconSeparation);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>Rebuilds the row from a typed status dictionary.</summary>
    public void SetStatuses(Dictionary<StatusEffect, int> statuses)
    {
        ClearChildren();
        if (statuses == null) return;
        foreach (var (effect, turns) in statuses)
        {
            if (turns <= 0) continue;
            AddBadge(StatusName(effect), turns);
        }
    }

    /// <summary>
    /// Rebuilds the row from a name-keyed dictionary (for callers that don't use
    /// the typed enum).
    /// </summary>
    public void SetStatusesByName(Dictionary<string, int> statuses)
    {
        ClearChildren();
        if (statuses == null) return;
        foreach (var (name, turns) in statuses)
        {
            if (turns <= 0) continue;
            AddBadge(name, turns);
        }
    }

    /// <summary>Removes all current badges.</summary>
    public void ClearChildren()
    {
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();
    }

    private void AddBadge(string statusName, int turns)
    {
        var color = ColorFor(statusName);
        char letter = string.IsNullOrEmpty(statusName) ? '?' : char.ToUpperInvariant(statusName[0]);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            MouseFilter       = MouseFilterEnum.Ignore,
            TooltipText       = $"{statusName} ({turns})",
        };

        var style = new StyleBoxFlat
        {
            BgColor                = color,
            BorderColor            = new Color(0f, 0f, 0f, 0.6f),
            BorderWidthTop         = 1, BorderWidthBottom = 1,
            BorderWidthLeft        = 1, BorderWidthRight  = 1,
            CornerRadiusTopLeft    = 3, CornerRadiusTopRight    = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
            ContentMarginLeft      = 0, ContentMarginRight  = 0,
            ContentMarginTop       = 0, ContentMarginBottom = 0,
        };
        panel.AddThemeStyleboxOverride("panel", style);

        // Layered children: centered letter + bottom-right turns number.
        var layer = new Control
        {
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            MouseFilter       = MouseFilterEnum.Ignore,
        };
        panel.AddChild(layer);

        var letterLabel = new Label
        {
            Text                = letter.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            AnchorLeft = 0f, AnchorRight  = 1f,
            AnchorTop  = 0f, AnchorBottom = 1f,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate    = Colors.White,
        };
        letterLabel.AddThemeFontSizeOverride("font_size", LetterFontSize);
        letterLabel.AddThemeColorOverride("font_color", Colors.White);
        letterLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        letterLabel.AddThemeConstantOverride("outline_size", 2);
        layer.AddChild(letterLabel);

        var turnsLabel = new Label
        {
            Text                = turns.ToString(),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment   = VerticalAlignment.Bottom,
            AnchorLeft = 0f, AnchorRight  = 1f,
            AnchorTop  = 0f, AnchorBottom = 1f,
            OffsetRight  = -1f,
            OffsetBottom = 0f,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate    = Colors.White,
        };
        turnsLabel.AddThemeFontSizeOverride("font_size", TurnsFontSize);
        turnsLabel.AddThemeColorOverride("font_color", Colors.White);
        turnsLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        turnsLabel.AddThemeConstantOverride("outline_size", 2);
        layer.AddChild(turnsLabel);

        AddChild(panel);
    }

    private static string StatusName(StatusEffect effect) => effect switch
    {
        StatusEffect.Poison  => "Poison",
        StatusEffect.Stun    => "Stun",
        StatusEffect.Shield  => "Shield",
        StatusEffect.Silence => "Silence",
        _                    => effect.ToString(),
    };

    /// <summary>Matches the badge color to the status name (case-insensitive).</summary>
    public static Color ColorFor(string statusName)
    {
        if (string.IsNullOrEmpty(statusName)) return DefaultGrey;
        return statusName.ToLowerInvariant() switch
        {
            "poison"  => new Color(0.3f, 0.8f, 0.3f),
            "burn"    => new Color(1.0f, 0.5f, 0.1f),
            "freeze"  => new Color(0.5f, 0.8f, 1.0f),
            "shock"   => new Color(1.0f, 0.9f, 0.2f),
            "sleep"   => new Color(0.7f, 0.4f, 0.9f),
            "bleed"   => new Color(0.9f, 0.2f, 0.2f),
            // Existing statuses in the enum that aren't in the elemental palette.
            "stun"    => new Color(1.0f, 0.85f, 0.1f),
            "shield"  => new Color(0.3f, 0.6f, 1.0f),
            "silence" => new Color(0.7f, 0.3f, 0.9f),
            _         => DefaultGrey,
        };
    }

    private static readonly Color DefaultGrey = new(0.55f, 0.55f, 0.6f);
}
