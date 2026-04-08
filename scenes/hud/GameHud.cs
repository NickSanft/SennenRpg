using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Overworld HP display shown in the bottom-left corner.
/// Instantiated by OverworldBase._Ready() so every map gets it automatically.
/// </summary>
public partial class GameHud : CanvasLayer
{
	private const float BarMaxWidth = 160f;
	private const float BarTweenDuration = 0.3f;

	private Label     _nameLabel = null!;
	private Label     _hpLabel   = null!;
	private ColorRect _hpBar     = null!;
	private Label     _mpLabel   = null!;
	private Control   _mpBarBg   = null!;
	private ColorRect _mpBar     = null!;
	private Label     _goldLabel = null!;
	// Multiline label that lists every recruited party member's HP/MP under Sen's
	// row. Hidden when the party only contains Sen.
	private Label?    _partyExtrasLabel;
	private Tween?    _hpTween;
	private Tween?    _lowHpPulseTween;
	private bool      _lowHpPulsing;
	private Tween?    _mpTween;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("Panel/VBox/NameRow/NameLabel");
		_hpLabel   = GetNode<Label>("Panel/VBox/HpRow/HpLabel");
		_hpBar     = GetNode<ColorRect>("Panel/VBox/HpBarBg/HpBar");

		var vbox = GetNode<VBoxContainer>("Panel/VBox");

		// MP row — added in code, only visible when MaxMp > 0
		_mpLabel = new Label { Text = "" };
		_mpLabel.AddThemeFontSizeOverride("font_size", 10);
		vbox.AddChild(_mpLabel);

		_mpBarBg = new Control { CustomMinimumSize = new Vector2(BarMaxWidth, 8f) };
		vbox.AddChild(_mpBarBg);

		_mpBar = new ColorRect { OffsetRight = 0f, OffsetBottom = 8f };
		_mpBarBg.AddChild(_mpBar);

		// Gold label added in code so GameHud.tscn doesn't need changing
		_goldLabel = new Label();
		_goldLabel.AddThemeFontSizeOverride("font_size", 10);
		_goldLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
		vbox.AddChild(_goldLabel);

		// Multi-member party rows. Hidden until the player recruits Lily or Rain.
		_partyExtrasLabel = new Label
		{
			Text     = "",
			Visible  = false,
			Modulate = new Color(0.95f, 0.95f, 1f, 0.95f),
		};
		_partyExtrasLabel.AddThemeFontSizeOverride("font_size", 9);
		vbox.AddChild(_partyExtrasLabel);

		// Key hints
		var hintLabel = new Label { Text = "[Tab] Log  [Esc] Menu" };
		hintLabel.AddThemeFontSizeOverride("font_size", 7);
		hintLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
		vbox.AddChild(hintLabel);

		// Apply pixel font to all labels
		UiTheme.ApplyPixelFontToAll(this);

		// Apply colorblind palette on load — SettingsManager.ApplyAll() runs before this HUD loads.
		var mode = SettingsManager.Instance?.Current.ColorblindMode ?? ColorblindMode.Normal;
		_hpBar.Color = SettingsLogic.HpBarColor(mode);
		_mpBar.Color = SettingsLogic.MpBarColor(mode);

		GameManager.Instance.PlayerStatsChanged += UpdateDisplay;
		UpdateDisplay();
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PlayerStatsChanged -= UpdateDisplay;
	}

	/// <summary>Briefly flashes the HP bar to <paramref name="flashColor"/> then fades back.</summary>
	public void FlashHpBar(Color flashColor, float holdDuration = 0.4f)
	{
		var origColor = _hpBar.Color;
		var t = CreateTween();
		t.TweenProperty(_hpBar, "color", flashColor,  0.08f);
		t.TweenProperty(_hpBar, "color", origColor,   0.35f).SetDelay(holdDuration);
	}

	private void UpdateDisplay()
	{
		var stats = GameManager.Instance.PlayerStats;
		_nameLabel.Text = " " + GameManager.Instance.PlayerName;
		_hpLabel.Text   = $"HP  {stats.CurrentHp} / {stats.MaxHp}";
		_goldLabel.Text = $"G   {GameManager.Instance.Gold}";
		RefreshPartyExtras();

		float hpRatio  = stats.MaxHp > 0 ? (float)stats.CurrentHp / stats.MaxHp : 0f;
		float hpTarget = Mathf.Max(0f, BarMaxWidth * hpRatio);
		_hpTween?.Kill();
		_hpTween = CreateTween();
		_hpTween.TweenProperty(_hpBar, "size:x", hpTarget, BarTweenDuration)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

		// MP — hide entirely for classes with 0 MaxMp
		bool hasMp = stats.MaxMp > 0;
		_mpLabel.Visible = hasMp;
		_mpBarBg.Visible = hasMp;
		if (hasMp)
		{
			_mpLabel.Text = $"MP  {stats.CurrentMp} / {stats.MaxMp}";
			float mpRatio  = (float)stats.CurrentMp / stats.MaxMp;
			float mpTarget = Mathf.Max(0f, BarMaxWidth * mpRatio);
			_mpTween?.Kill();
			_mpTween = CreateTween();
			_mpTween.TweenProperty(_mpBar, "size:x", mpTarget, BarTweenDuration)
				.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		}

		// (party extras refreshed at the top of UpdateDisplay)

		// Low HP pulse warning
		bool isLowHp = stats.MaxHp > 0 && (float)stats.CurrentHp / stats.MaxHp < 0.25f && stats.CurrentHp > 0;
		if (isLowHp && !_lowHpPulsing)
		{
			_lowHpPulsing = true;
			_lowHpPulseTween?.Kill();
			_lowHpPulseTween = CreateTween().SetLoops();
			_lowHpPulseTween.TweenProperty(_hpBar, "color", new Color(0.9f, 0.15f, 0.15f), 0.4f);
			_lowHpPulseTween.TweenProperty(_hpBar, "color",
				SettingsLogic.HpBarColor(SettingsManager.Instance?.Current.ColorblindMode ?? ColorblindMode.Normal), 0.4f);
		}
		else if (!isLowHp && _lowHpPulsing)
		{
			_lowHpPulsing = false;
			_lowHpPulseTween?.Kill();
			_lowHpPulseTween = null;
			_hpBar.Color = SettingsLogic.HpBarColor(
				SettingsManager.Instance?.Current.ColorblindMode ?? ColorblindMode.Normal);
		}
	}

	/// <summary>
	/// Build a multiline label of every recruited party member's HP/MP. Hidden when
	/// only Sen is in the party so the HUD looks identical pre-recruitment.
	/// </summary>
	private void RefreshPartyExtras()
	{
		if (_partyExtrasLabel == null) return;
		if (GameManager.Instance == null) return;

		var party = GameManager.Instance.Party;
		if (party.Count <= 1)
		{
			_partyExtrasLabel.Visible = false;
			_partyExtrasLabel.Text    = "";
			return;
		}

		var lines = new System.Collections.Generic.List<string>();
		foreach (var m in party.Members)
		{
			if (m.MemberId == "sen") continue; // Sen is already shown above by the main HUD
			string ko = m.IsKO ? " (KO)" : "";
			lines.Add($"{m.DisplayName,-7} HP {m.CurrentHp,3}/{m.MaxHp,-3}  MP {m.CurrentMp,2}/{m.MaxMp,-2}{ko}");
		}

		if (lines.Count == 0)
		{
			_partyExtrasLabel.Visible = false;
			_partyExtrasLabel.Text    = "";
		}
		else
		{
			_partyExtrasLabel.Visible = true;
			_partyExtrasLabel.Text    = string.Join("\n", lines);
		}
	}
}
