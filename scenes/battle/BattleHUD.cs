using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Extensions;
using SennenRpg.Scenes.Hud;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Phase 7c — centered party-aware battle HUD.
///
/// Built entirely in code (the .tscn is a bare CanvasLayer shell). Lays out one
/// "party card" per active member at the bottom of the screen, centered horizontally.
/// Each card shows name, level, HP / MP bars, status badges. The currently-active
/// actor is highlighted with a gold border.
///
/// Lives on a low CanvasLayer (below Dialogic's default layer 1) so the in-battle
/// dialog box renders ON TOP of the HUD background — fixing the previous regression
/// where the bottom-of-screen Dialogic textbox was hidden behind the HUD.
/// </summary>
public partial class BattleHUD : CanvasLayer
{
	private const int HudCanvasLayer = 0; // below Dialogic (layer 1) so dialog overlaps the HUD bg

	private record CardWidgets(
		PanelContainer    Root,
		AnimatedPortrait  Portrait,
		Label             Name,
		Label             Lv,
		Label             Hp,
		ColorRect         HpBarBg,
		ColorRect         HpBar,
		Label             Mp,
		ColorRect         MpBarBg,
		ColorRect         MpBar,
		HBoxContainer     StatusRow);

	private CenterContainer? _centerer;
	private HBoxContainer?   _cardsRow;
	private Label?           _hintLabel;
	private Label?           _summaryLabel;
	private readonly Dictionary<string, CardWidgets> _cards = new();
	private int _activeMemberIdx = -1;

	private static readonly Color CardBg          = new(0.08f, 0.08f, 0.14f, 0.85f);
	private static readonly Color CardBorder      = new(0.55f, 0.40f, 0.85f, 1f);
	private static readonly Color CardBorderActive = new(1f, 0.85f, 0.1f, 1f);
	private static readonly Color BarBgColor      = new(0.15f, 0.15f, 0.2f, 1f);

	private const float CardWidth     = 240f;
	private const float CardMinHeight = 80f;

	public override void _Ready()
	{
		Layer = HudCanvasLayer;

		BuildSkeleton();

		// Apply colorblind palette tweaks if any.
		var mode = SettingsManager.Instance?.Current.ColorblindMode ?? ColorblindMode.Normal;
		// Bars get re-coloured per member when cards are built.

		GameManager.Instance.PlayerStatsChanged += OnStatsChanged;
		Callable.From(RebuildCards).CallDeferred();
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PlayerStatsChanged -= OnStatsChanged;
	}

	private void OnStatsChanged() => UpdateAllCards();

	// ── Skeleton (centerer + cards row + hint) ────────────────────────

	private void BuildSkeleton()
	{
		_centerer = new CenterContainer
		{
			AnchorLeft   = 0f,
			AnchorRight  = 1f,
			AnchorTop    = 1f,
			AnchorBottom = 1f,
			OffsetTop    = -120f,
			OffsetBottom = -8f,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		AddChild(_centerer);

		var outerVbox = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		outerVbox.AddThemeConstantOverride("separation", 4);
		_centerer.AddChild(outerVbox);

		_cardsRow = new HBoxContainer
		{
			Alignment   = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_cardsRow.AddThemeConstantOverride("separation", 6);
		outerVbox.AddChild(_cardsRow);

		_hintLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate            = new Color(1f, 1f, 1f, 0.55f),
		};
		_hintLabel.AddThemeFontSizeOverride("font_size", 10);
		outerVbox.AddChild(_hintLabel);
		SetHints(BattleHints.PlayerTurn);
	}

	// ── Card construction ─────────────────────────────────────────────

	private void RebuildCards()
	{
		if (_cardsRow == null) return;

		// Clear previous cards.
		foreach (var child in _cardsRow.GetChildren())
			if (child is Node n) n.QueueFree();
		_cards.Clear();

		var party = GameManager.Instance.Party;
		if (party.IsEmpty)
		{
			UpdateAllCards();
			return;
		}

		foreach (var m in party.Members)
		{
			var card = BuildCard(m);
			_cardsRow.AddChild(card.Root);
			_cards[m.MemberId] = card;
		}

		UpdateAllCards();
		UiTheme.ApplyPixelFontToAll(_cardsRow);
	}

	private CardWidgets BuildCard(PartyMember m)
	{
		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(CardWidth, CardMinHeight),
		};
		ApplyCardStyle(panel, active: false);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left",   8);
		margin.AddThemeConstantOverride("margin_right",  8);
		margin.AddThemeConstantOverride("margin_top",    4);
		margin.AddThemeConstantOverride("margin_bottom", 4);
		panel.AddChild(margin);

		// Outer HBox: portrait on the left, content (name/HP/MP/status) on the right.
		var rootRow = new HBoxContainer();
		rootRow.AddThemeConstantOverride("separation", 6);
		margin.AddChild(rootRow);

		// Animated portrait — loops the 2-frame overworld walk animation so the
		// face on each card actually moves instead of being frozen on frame 0.
		var portrait = new AnimatedPortrait
		{
			PortraitSize        = new Vector2(40f, 40f),
			SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter,
		};
		rootRow.AddChild(portrait);
		string spritePath = string.IsNullOrEmpty(m.OverworldSpritePath)
			? "res://assets/sprites/player/Sen_Overworld.png"
			: m.OverworldSpritePath;
		portrait.SetSpriteSheet(spritePath);

		var vbox = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		vbox.AddThemeConstantOverride("separation", 2);
		rootRow.AddChild(vbox);

		// Name + level row
		var topRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
		topRow.AddThemeConstantOverride("separation", 4);
		var nameLabel = new Label
		{
			Text                = m.DisplayName,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Modulate            = Colors.White,
		};
		nameLabel.AddThemeFontSizeOverride("font_size", 11);
		var lvLabel = new Label { Text = "LV 1" };
		lvLabel.AddThemeFontSizeOverride("font_size", 10);
		lvLabel.Modulate = new Color(0.85f, 0.85f, 0.95f);
		topRow.AddChild(nameLabel);
		topRow.AddChild(lvLabel);
		vbox.AddChild(topRow);

		// HP row
		var hpRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
		hpRow.AddThemeConstantOverride("separation", 4);
		var hpLabel = new Label
		{
			Text              = "HP 0/0",
			CustomMinimumSize = new Vector2(58f, 0f),
		};
		hpLabel.AddThemeFontSizeOverride("font_size", 9);
		var hpBarBg = new ColorRect
		{
			Color               = BarBgColor,
			CustomMinimumSize   = new Vector2(110f, 8f),
			SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter,
		};
		var hpBar = new ColorRect
		{
			Color        = new Color(0.95f, 0.85f, 0.10f),
			AnchorLeft   = 0f, AnchorRight  = 1f,
			AnchorTop    = 0f, AnchorBottom = 1f,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		hpBarBg.AddChild(hpBar);
		hpRow.AddChild(hpLabel);
		hpRow.AddChild(hpBarBg);
		vbox.AddChild(hpRow);

		// MP row
		var mpRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
		mpRow.AddThemeConstantOverride("separation", 4);
		var mpLabel = new Label
		{
			Text              = "MP 0/0",
			CustomMinimumSize = new Vector2(58f, 0f),
		};
		mpLabel.AddThemeFontSizeOverride("font_size", 9);
		var mpBarBg = new ColorRect
		{
			Color             = BarBgColor,
			CustomMinimumSize = new Vector2(110f, 6f),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		var mpBar = new ColorRect
		{
			Color        = new Color(0.30f, 0.55f, 1f),
			AnchorLeft   = 0f, AnchorRight  = 1f,
			AnchorTop    = 0f, AnchorBottom = 1f,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		mpBarBg.AddChild(mpBar);
		mpRow.AddChild(mpLabel);
		mpRow.AddChild(mpBarBg);
		vbox.AddChild(mpRow);

		// Status badge row
		var statusRow = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Alignment           = BoxContainer.AlignmentMode.Begin,
		};
		statusRow.AddThemeConstantOverride("separation", 3);
		vbox.AddChild(statusRow);

		return new CardWidgets(panel, portrait, nameLabel, lvLabel, hpLabel, hpBarBg, hpBar,
			mpLabel, mpBarBg, mpBar, statusRow);
	}

	private static void ApplyCardStyle(PanelContainer panel, bool active)
	{
		var style = new StyleBoxFlat
		{
			BgColor                = CardBg,
			BorderColor            = active ? CardBorderActive : CardBorder,
			BorderWidthTop         = 2, BorderWidthBottom = 2,
			BorderWidthLeft        = 2, BorderWidthRight  = 2,
			CornerRadiusTopLeft    = 4, CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
			ContentMarginLeft      = 6, ContentMarginRight  = 6,
			ContentMarginTop       = 4, ContentMarginBottom = 4,
		};
		panel.AddThemeStyleboxOverride("panel", style);
	}

	// ── Refresh ───────────────────────────────────────────────────────

	private void UpdateAllCards()
	{
		var party = GameManager.Instance.Party;

		// If the party shape changed (recruited/removed) since the last build, rebuild.
		if (_cards.Count != party.Count)
		{
			RebuildCards();
			return;
		}

		foreach (var m in party.Members)
		{
			if (!_cards.TryGetValue(m.MemberId, out var card)) { RebuildCards(); return; }

			int curHp, maxHp, curMp, maxMp, level;
			if (m.MemberId == "sen")
			{
				var s = GameManager.Instance.PlayerStats;
				curHp = s.CurrentHp; maxHp = s.MaxHp;
				curMp = s.CurrentMp; maxMp = s.MaxMp;
				level = GameManager.Instance.PlayerLevel;
			}
			else
			{
				curHp = m.CurrentHp; maxHp = m.MaxHp;
				curMp = m.CurrentMp; maxMp = m.MaxMp;
				level = m.Level;
			}

			card.Name.Text = m.DisplayName + (curHp <= 0 ? " (KO)" : "");
			card.Name.Modulate = curHp <= 0 ? new Color(0.6f, 0.6f, 0.6f) : Colors.White;
			card.Lv.Text   = $"Lv {level}";
			card.Hp.Text   = $"HP {curHp}/{maxHp}";
			card.Mp.Text   = $"MP {curMp}/{maxMp}";

			float hpRatio = maxHp > 0 ? (float)curHp / maxHp : 0f;
			float mpRatio = maxMp > 0 ? (float)curMp / maxMp : 0f;
			card.HpBar.AnchorRight = Mathf.Clamp(hpRatio, 0f, 1f);
			card.MpBar.AnchorRight = Mathf.Clamp(mpRatio, 0f, 1f);
		}
	}

	// ── Public API used by BattleScene ────────────────────────────────

	/// <summary>Highlights the active actor's card with a gold border.</summary>
	public void HighlightActor(int activeMemberIdx)
	{
		_activeMemberIdx = activeMemberIdx;
		var party = GameManager.Instance.Party;
		for (int i = 0; i < party.Members.Count; i++)
		{
			var m = party.Members[i];
			if (!_cards.TryGetValue(m.MemberId, out var card)) continue;
			ApplyCardStyle(card.Root, active: i == activeMemberIdx);
		}
	}

	private static readonly Color CardBorderTarget = new(0.3f, 1f, 0.4f, 1f);

	/// <summary>Highlights a party member's card with a green target border for item targeting.</summary>
	public void SetTargetHighlight(int memberIdx, bool on)
	{
		var party = GameManager.Instance.Party;
		if (memberIdx < 0 || memberIdx >= party.Members.Count) return;
		var m = party.Members[memberIdx];
		if (!_cards.TryGetValue(m.MemberId, out var card)) return;

		var style = new StyleBoxFlat
		{
			BgColor                = CardBg,
			BorderColor            = on ? CardBorderTarget : CardBorder,
			BorderWidthTop         = 2, BorderWidthBottom = 2,
			BorderWidthLeft        = 2, BorderWidthRight  = 2,
			CornerRadiusTopLeft    = 4, CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
			ContentMarginLeft      = 6, ContentMarginRight  = 6,
			ContentMarginTop       = 4, ContentMarginBottom = 4,
		};
		card.Root.AddThemeStyleboxOverride("panel", style);
	}

	/// <summary>Clear all target highlights and restore normal styling.</summary>
	public void ClearTargetHighlights()
	{
		var party = GameManager.Instance.Party;
		for (int i = 0; i < party.Members.Count; i++)
		{
			var m = party.Members[i];
			if (!_cards.TryGetValue(m.MemberId, out var card)) continue;
			ApplyCardStyle(card.Root, active: i == _activeMemberIdx);
		}
	}

	/// <summary>Updates the contextual hint bar at the bottom of the HUD.</summary>
	public void SetHints(string text)
	{
		if (_hintLabel != null) _hintLabel.Text = text;
	}

	/// <summary>
	/// Writes status badges onto Sen's card specifically. For non-Sen members use
	/// <see cref="UpdateStatusesFor"/>.
	/// </summary>
	public void UpdateStatuses(Dictionary<StatusEffect, int> statuses)
	{
		if (!_cards.TryGetValue("sen", out var card)) return;
		ApplyStatusBadges(card.StatusRow, statuses);
	}

	/// <summary>Writes status badges onto a specific party member's card.</summary>
	public void UpdateStatusesFor(string memberId, Dictionary<StatusEffect, int> statuses)
	{
		if (string.IsNullOrEmpty(memberId)) return;
		if (!_cards.TryGetValue(memberId, out var card)) return;
		ApplyStatusBadges(card.StatusRow, statuses);
	}

	private void ApplyStatusBadges(HBoxContainer row, Dictionary<StatusEffect, int> statuses)
	{
		foreach (var child in row.GetChildren())
			if (child is Node n) n.QueueFree();

		foreach (var (effect, turns) in statuses)
		{
			Color badgeColor = effect switch
			{
				StatusEffect.Poison  => new Color(0.2f, 0.85f, 0.3f),
				StatusEffect.Stun    => new Color(1.0f, 0.85f, 0.1f),
				StatusEffect.Shield  => new Color(0.3f, 0.6f, 1.0f),
				StatusEffect.Silence => new Color(0.7f, 0.3f, 0.9f),
				_                    => Colors.Gray,
			};

			var badge = new PanelContainer();
			var style = new StyleBoxFlat
			{
				BgColor                = badgeColor,
				CornerRadiusTopLeft    = 2, CornerRadiusTopRight = 2,
				CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
				ContentMarginLeft      = 3, ContentMarginRight = 3,
				ContentMarginTop       = 1, ContentMarginBottom = 1,
			};
			badge.AddThemeStyleboxOverride("panel", style);

			var lbl = new Label
			{
				Text     = $"{StatusLogic.IconText(effect)} {turns}",
				Modulate = Colors.White,
			};
			lbl.AddThemeFontSizeOverride("font_size", 8);
			badge.AddChild(lbl);
			row.AddChild(badge);
		}
	}

	/// <summary>Brief performance summary toast that fades in then out above the HUD.</summary>
	public void ShowPerformanceSummary(PerformanceScore score)
	{
		if (_summaryLabel == null)
		{
			_summaryLabel = new Label
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				Modulate = Colors.Transparent,
				AnchorLeft   = 0f, AnchorRight  = 1f,
				AnchorTop    = 1f, AnchorBottom = 1f,
				OffsetTop    = -160f, OffsetBottom = -130f,
			};
			_summaryLabel.AddThemeFontSizeOverride("font_size", 14);
			AddChild(_summaryLabel);
		}

		_summaryLabel.Text = score.GetSummaryText();

		var tween = CreateTween().SetParallel(true);
		tween.TweenProperty(_summaryLabel, "modulate:a", 1f, 0.3f);
		tween.Chain().TweenInterval(2.5f);
		tween.Chain().TweenProperty(_summaryLabel, "modulate:a", 0f, 0.5f);
	}
}

/// <summary>
/// Pre-built hint strings for each battle phase.
/// Uses <see cref="InputMapExtensions.HintFor"/> so hints reflect player key remaps.
/// </summary>
public static class BattleHints
{
	public static string PlayerTurn =>
		$"{InputMapExtensions.HintFor("interact",   "Confirm")}  " +
		$"{InputMapExtensions.HintFor("ui_cancel",  "Back")}  " +
		$"{InputMapExtensions.HintFor("ui_up",      "↑")} " +
		$"{InputMapExtensions.HintFor("ui_down",    "↓")} Navigate  " +
		$"{InputMapExtensions.HintFor("ui_left",    "←")} " +
		$"{InputMapExtensions.HintFor("ui_right",   "→")} Target";

	public static string RhythmPhase =>
		$"{InputMapExtensions.HintFor("lane_0", "Lane 1", "1")}  " +
		$"{InputMapExtensions.HintFor("lane_1", "Lane 2", "2")}  " +
		$"{InputMapExtensions.HintFor("lane_2", "Lane 3", "3")}  " +
		$"{InputMapExtensions.HintFor("lane_3", "Lane 4", "4")}";

	public static string FighterTiming =>
		$"{InputMapExtensions.HintFor("interact", "Lock In")}  — hit the sweet spot for max damage";

	public static string RangerAim =>
		$"{InputMapExtensions.HintFor("interact", "Fire")}  — aim for the bull's-eye to crit";

	public static string MageRunes =>
		$"{InputMapExtensions.HintFor("ui_up",    "↑")} " +
		$"{InputMapExtensions.HintFor("ui_down",  "↓")} " +
		$"{InputMapExtensions.HintFor("ui_left",  "←")} " +
		$"{InputMapExtensions.HintFor("ui_right", "→")}  — trace the rune sequence";

	public static string RogueCombo =>
		$"{InputMapExtensions.HintFor("interact", "Strike")}  — three rapid windows; all-perfect steals";

	public static string AlchemistBrew =>
		$"{InputMapExtensions.HintFor("interact", "Brew")}  — stop on the sweet spot; Luck widens it";

	public static string Flee(int pct) =>
		$"{InputMapExtensions.HintFor("interact", "Flee")}  — {pct}% escape chance";
}
