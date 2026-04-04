using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Extensions;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// CanvasLayer (Layer 10) showing player name, LV, HP bar, MP bar, and battle stats.
/// Updates live via GameManager.PlayerStatsChanged.
/// </summary>
public partial class BattleHUD : CanvasLayer
{
	private Label     _nameLabel    = null!;
	private Label     _lvLabel      = null!;
	private Label     _hpLabel      = null!;
	private Label     _mpLabel      = null!;
	private Label     _statsLabel   = null!;
	private Label     _statusLabel  = null!;
	private Label?    _summaryLabel;
	private Label?    _hintLabel;
	private ColorRect _hpBarBg     = null!;
	private ColorRect _hpBar       = null!;
	private ColorRect _mpBarBg     = null!;
	private ColorRect _mpBar       = null!;
	private Tween?    _hpTween;
	private Tween?    _mpTween;
	private const float BarTweenDuration = 0.3f;

	public override void _Ready()
	{
		Layer = 10;

		const string row = "HudPanel/VBoxContainer/HBoxContainer/";
		_nameLabel  = GetNode<Label>(row + "NameLabel");
		_lvLabel    = GetNode<Label>(row + "LvLabel");
		_hpLabel    = GetNode<Label>(row + "HpLabel");
		_hpBarBg    = GetNode<ColorRect>(row + "HpBarBg");
		_hpBar      = GetNode<ColorRect>(row + "HpBarBg/HpBar");
		_mpLabel    = GetNode<Label>(row + "MpLabel");
		_mpBarBg    = GetNode<ColorRect>(row + "MpBarBg");
		_mpBar      = GetNode<ColorRect>(row + "MpBarBg/MpBar");
		_statsLabel = GetNode<Label>("HudPanel/VBoxContainer/StatsLabel");

		// Status label is created dynamically so the .tscn doesn't need editing.
		_statusLabel = new Label { Text = "" };
		GetNode<VBoxContainer>("HudPanel/VBoxContainer").AddChild(_statusLabel);

		// Hint bar — thin label at the bottom of the panel, pre-populated with common bindings.
		_hintLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(1f, 1f, 1f, 0.55f),
		};
		_hintLabel.AddThemeFontSizeOverride("font_size", 10);
		GetNode<VBoxContainer>("HudPanel/VBoxContainer").AddChild(_hintLabel);
		SetHints(BattleHints.PlayerTurn);

		// Apply colorblind palette — HUD loads after SettingsManager.ApplyAll(), so we apply again here.
		var mode = SettingsManager.Instance?.Current.ColorblindMode ?? ColorblindMode.Normal;
		_hpBar.Color = SettingsLogic.HpBarColor(mode);
		_mpBar.Color = SettingsLogic.MpBarColor(mode);

		GameManager.Instance.PlayerStatsChanged += UpdateHud;
		Callable.From(UpdateHud).CallDeferred();

		UiTheme.ApplyPixelFontToAll(this);
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PlayerStatsChanged -= UpdateHud;
	}

	private void UpdateHud()
	{
		var stats = GameManager.Instance.PlayerStats;

		_nameLabel.Text = "* " + GameManager.Instance.PlayerName;
		_lvLabel.Text   = $"LV {GameManager.Instance.PlayerLevel}";
		_hpLabel.Text   = $"{stats.CurrentHp} / {stats.MaxHp} HP";
		_mpLabel.Text   = $"{stats.CurrentMp} / {stats.MaxMp} MP";

		float hpTarget = stats.MaxHp > 0
			? _hpBarBg.Size.X * ((float)stats.CurrentHp / stats.MaxHp) : 0f;
		_hpTween?.Kill();
		_hpTween = CreateTween();
		_hpTween.TweenProperty(_hpBar, "size:x", hpTarget, BarTweenDuration)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

		float mpTarget = stats.MaxMp > 0
			? _mpBarBg.Size.X * ((float)stats.CurrentMp / stats.MaxMp) : 0f;
		_mpTween?.Kill();
		_mpTween = CreateTween();
		_mpTween.TweenProperty(_mpBar, "size:x", mpTarget, BarTweenDuration)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

		_statsLabel.Text =
			$"ATK:{stats.Attack}  DEF:{stats.Defense}  SPD:{stats.Speed}" +
			$"  MAG:{stats.Magic}  RES:{stats.Resistance}  LCK:{stats.Luck}";
	}

	/// <summary>
	/// Shows a brief performance summary ("PERFECT×5 GOOD×3 …") that auto-hides after 3 s.
	/// </summary>
	public void ShowPerformanceSummary(PerformanceScore score)
	{
		if (_summaryLabel == null)
		{
			_summaryLabel = new Label
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				Modulate = Colors.Transparent,
			};
			_summaryLabel.AddThemeFontSizeOverride("font_size", 11);
			GetNode<VBoxContainer>("HudPanel/VBoxContainer").AddChild(_summaryLabel);
		}

		_summaryLabel.Text = score.GetSummaryText();

		var tween = CreateTween().SetParallel(true);
		tween.TweenProperty(_summaryLabel, "modulate:a", 1f, 0.3f);
		tween.Chain().TweenInterval(2.5f);
		tween.Chain().TweenProperty(_summaryLabel, "modulate:a", 0f, 0.5f);
	}

	/// <summary>Updates the contextual hint bar at the bottom of the HUD panel.</summary>
	public void SetHints(string text)
	{
		if (_hintLabel != null)
			_hintLabel.Text = text;
	}

	/// <summary>Updates the status icon row beneath the stat line with colored badges.</summary>
	public void UpdateStatuses(Dictionary<StatusEffect, int> statuses)
	{
		// Clear previous badges
		foreach (var child in _statusLabel.GetParent().GetChildren())
		{
			if (child is HBoxContainer hb && hb.Name == "StatusBadges")
				hb.QueueFree();
		}

		_statusLabel.Text = ""; // clear text fallback

		if (statuses.Count == 0) return;

		var row = new HBoxContainer { Name = "StatusBadges" };
		row.AddThemeConstantOverride("separation", 6);

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
				BgColor = badgeColor,
				CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
				CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
				ContentMarginLeft = 4, ContentMarginRight = 4,
				ContentMarginTop = 1, ContentMarginBottom = 1,
			};
			badge.AddThemeStyleboxOverride("panel", style);

			var lbl = new Label
			{
				Text = $"{StatusLogic.IconText(effect)} {turns}",
				Modulate = Colors.White,
			};
			lbl.AddThemeFontSizeOverride("font_size", 9);
			badge.AddChild(lbl);
			row.AddChild(badge);
		}

		_statusLabel.GetParent().AddChild(row);
		UiTheme.ApplyPixelFontToAll(row);
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
        $"{InputMapExtensions.HintFor("ui_down",    "↓")} Navigate";

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

    public static string Flee(int pct) =>
		$"{InputMapExtensions.HintFor("interact", "Flee")}  — {pct}% escape chance";
}
