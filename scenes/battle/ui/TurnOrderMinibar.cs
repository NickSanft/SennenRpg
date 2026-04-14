using Godot;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Thin horizontal strip anchored to the top-center of the battle screen that
/// previews the next few actors in turn order. Shows a "NEXT:" label followed by
/// up to 4 portrait icons (24×24) with tiny name labels. The currently-acting
/// actor (queue[currentIdx]) is highlighted with a gold border + ★ glyph.
///
/// Built code-only and mounted on a CanvasLayer (layer 10, same plane as BattleHUD).
/// Call <see cref="Refresh"/> whenever the queue is rebuilt or the index advances.
/// </summary>
public partial class TurnOrderMinibar : CanvasLayer
{
    private const int IconSize       = 24;
    private const int MaxIcons       = 4;
    private const int IconGap        = 6;

    private HBoxContainer _row = null!;
    private Label         _nextLabel = null!;

    public TurnOrderMinibar()
    {
        Layer = 10;
    }

    public override void _Ready()
    {
        // Wrapper control so we can center the HBox near the top of the viewport.
        var wrapper = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        wrapper.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        wrapper.OffsetTop    = 6f;
        wrapper.OffsetLeft   = 0f;
        wrapper.OffsetRight  = 0f;
        wrapper.OffsetBottom = (float)(IconSize + 18);
        AddChild(wrapper);

        var center = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        wrapper.AddChild(center);

        _row = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _row.AddThemeConstantOverride("separation", IconGap);
        center.AddChild(_row);

        var pixelFont = UiTheme.LoadPixelFont();

        _nextLabel = new Label
        {
            Text                = "NEXT:",
            VerticalAlignment   = VerticalAlignment.Center,
            MouseFilter         = Control.MouseFilterEnum.Ignore,
        };
        _nextLabel.AddThemeFontSizeOverride("font_size", 10);
        if (pixelFont != null)
            _nextLabel.AddThemeFontOverride("font", pixelFont);
        _nextLabel.AddThemeColorOverride("font_color", UiTheme.SubtleGrey);
        _row.AddChild(_nextLabel);
    }

    /// <summary>
    /// Rebuild the icon strip from the current turn queue. Starts from the NEXT
    /// entry (currentIdx) and walks forward up to <see cref="MaxIcons"/>. KO'd
    /// actors are skipped — they'll never act again this round.
    /// </summary>
    public void Refresh(
        IReadOnlyList<TurnQueueEntry> queue,
        int currentIdx,
        IReadOnlyList<PartyMember> party,
        IReadOnlyList<EnemyInstance> enemies)
    {
        if (_row == null) return;

        // Clear out everything except the "NEXT:" label (index 0).
        for (int i = _row.GetChildCount() - 1; i >= 1; i--)
        {
            var child = _row.GetChild(i);
            _row.RemoveChild(child);
            child.QueueFree();
        }

        if (queue == null || queue.Count == 0 || currentIdx >= queue.Count)
        {
            Visible = false;
            return;
        }

        Visible = true;

        int added   = 0;
        int scanIdx = currentIdx;
        bool isFirst = true; // the first shown entry is the CURRENT actor

        while (added < MaxIcons && scanIdx < queue.Count)
        {
            var entry = queue[scanIdx];
            scanIdx++;

            string? display  = null;
            bool    isParty  = entry.IsParty;
            bool    alive    = true;

            if (entry.IsParty)
            {
                if (party != null && entry.Index >= 0 && entry.Index < party.Count)
                {
                    var m = party[entry.Index];
                    alive = !m.IsKO;
                    display = string.IsNullOrEmpty(m.DisplayName) ? m.MemberId : m.DisplayName;
                }
            }
            else
            {
                if (enemies != null && entry.Index >= 0 && entry.Index < enemies.Count)
                {
                    var e = enemies[entry.Index];
                    alive = !e.IsKO;
                    display = e.DisplayName;
                }
            }

            if (!alive) continue;        // dead since queue was built; skip
            if (display == null) continue;

            _row.AddChild(BuildIcon(display, isParty, isCurrent: isFirst));
            isFirst = false;
            added++;
        }

        // If we somehow ended up with only the "NEXT:" label visible (every
        // upcoming actor dead), hide the bar entirely rather than showing a
        // dangling label.
        if (added == 0) Visible = false;
    }

    /// <summary>Build one icon tile (portrait panel + 4-char name label).</summary>
    private Control BuildIcon(string displayName, bool isParty, bool isCurrent)
    {
        var box = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        box.AddThemeConstantOverride("separation", 1);

        // Portrait panel — a StyleBoxFlat with a single letter centered inside.
        var panel = new PanelContainer
        {
            MouseFilter     = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(IconSize, IconSize),
        };
        var style = new StyleBoxFlat();
        if (isParty)
        {
            style.BgColor = UiTheme.PanelBg;
            style.BorderColor = isCurrent ? UiTheme.Gold : UiTheme.PanelBorder;
        }
        else
        {
            style.BgColor = new Color(0.25f, 0.08f, 0.08f, 1f); // dark red for enemies
            style.BorderColor = isCurrent ? UiTheme.Gold : new Color(0.7f, 0.25f, 0.25f);
        }
        style.BorderWidthTop = style.BorderWidthBottom =
            style.BorderWidthLeft = style.BorderWidthRight = isCurrent ? 2 : 1;
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
            style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 3;
        panel.AddThemeStyleboxOverride("panel", style);

        string letter = string.IsNullOrEmpty(displayName)
            ? (isParty ? "?" : "E")
            : displayName.Substring(0, 1).ToUpperInvariant();

        var letterLabel = new Label
        {
            Text                = letter,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            MouseFilter         = Control.MouseFilterEnum.Ignore,
        };
        letterLabel.AddThemeFontSizeOverride("font_size", 12);
        var pixelFont = UiTheme.LoadPixelFont();
        if (pixelFont != null)
            letterLabel.AddThemeFontOverride("font", pixelFont);
        letterLabel.AddThemeColorOverride(
            "font_color",
            isParty ? UiTheme.Gold : new Color(1f, 0.85f, 0.85f));
        panel.AddChild(letterLabel);

        box.AddChild(panel);

        // Tiny name label under the icon — truncated to 4 chars.
        string shortName = displayName.Length > 4
            ? displayName.Substring(0, 4)
            : displayName;
        if (isCurrent) shortName = "\u2605" + shortName; // ★ prefix on current actor

        var nameLabel = new Label
        {
            Text                = shortName,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter         = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize   = new Vector2(IconSize + 8, 0),
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 8);
        if (pixelFont != null)
            nameLabel.AddThemeFontOverride("font", pixelFont);
        nameLabel.AddThemeColorOverride(
            "font_color",
            isCurrent ? UiTheme.Gold : UiTheme.SubtleGrey);
        box.AddChild(nameLabel);

        // Brighten the whole tile for the current actor so it reads even at a glance.
        box.Modulate = isCurrent ? Colors.White : new Color(1f, 1f, 1f, 0.75f);

        return box;
    }
}
