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

		float hpRatio = stats.MaxHp > 0 ? (float)stats.CurrentHp / stats.MaxHp : 0f;
		_hpBar.Size = new Vector2(_hpBarBg.Size.X * hpRatio, _hpBarBg.Size.Y);

		float mpRatio = stats.MaxMp > 0 ? (float)stats.CurrentMp / stats.MaxMp : 0f;
		_mpBar.Size = new Vector2(_mpBarBg.Size.X * mpRatio, _mpBarBg.Size.Y);

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

	/// <summary>Updates the status icon row beneath the stat line.</summary>
	public void UpdateStatuses(Dictionary<StatusEffect, int> statuses)
	{
		if (statuses.Count == 0)
		{
			_statusLabel.Text = "";
			return;
		}
		var parts = new System.Collections.Generic.List<string>();
		foreach (var (effect, turns) in statuses)
			parts.Add($"{StatusLogic.IconText(effect)}({turns})");
		_statusLabel.Text = string.Join(" ", parts);
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
