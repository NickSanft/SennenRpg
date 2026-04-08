using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Overworld HUD shown in the bottom-left corner. Built entirely in code so the
/// .tscn is a bare CanvasLayer shell.
///
/// Lays out one "party card" per recruited member. Each card has an animated
/// portrait, a name + level row, an HP bar, and (when the member has MP) an MP
/// bar. The leader is highlighted with a gold border. Updates live via the
/// <see cref="GameManager.PlayerStatsChanged"/> signal.
///
/// Instantiated by both <see cref="SennenRpg.Scenes.Overworld.OverworldBase"/>
/// (interior maps + dungeons) and <see cref="SennenRpg.Scenes.Overworld.WorldMap"/>
/// (the world map). Sits at canvas layer 2 — above the world but below menus.
/// </summary>
public partial class GameHud : CanvasLayer
{
	private const float CardWidth     = 200f;
	private const float CardMinHeight = 56f;

	private static readonly Color CardBg            = new(0.08f, 0.08f, 0.14f, 0.85f);
	private static readonly Color CardBorder        = new(0.55f, 0.40f, 0.85f, 1f);
	private static readonly Color CardBorderActive  = new(1f, 0.85f, 0.1f, 1f);
	private static readonly Color BarBgColor        = new(0.15f, 0.15f, 0.20f, 1f);

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
		Control           MpRoot);

	private VBoxContainer? _cardsColumn;
	private Label? _goldLabel;
	private readonly System.Collections.Generic.Dictionary<string, CardWidgets> _cards = new();

	public override void _Ready()
	{
		BuildSkeleton();
		GameManager.Instance.PlayerStatsChanged += OnStatsChanged;
		Callable.From(RebuildCards).CallDeferred();
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PlayerStatsChanged -= OnStatsChanged;
	}

	private void OnStatsChanged() => UpdateAllCards();

	// ── Skeleton ──────────────────────────────────────────────────────

	private void BuildSkeleton()
	{
		// Anchored to the bottom-left of the viewport. The cards stack vertically
		// upward from above the gold/hint bar.
		var anchor = new Control
		{
			AnchorTop    = 1f, AnchorBottom = 1f,
			AnchorLeft   = 0f, AnchorRight  = 0f,
			OffsetLeft   = 8f,
			OffsetTop    = -260f,
			OffsetBottom = -8f,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		AddChild(anchor);

		var outerVbox = new VBoxContainer
		{
			AnchorLeft   = 0f, AnchorRight  = 1f,
			AnchorTop    = 0f, AnchorBottom = 1f,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		outerVbox.AddThemeConstantOverride("separation", 4);
		anchor.AddChild(outerVbox);

		_cardsColumn = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			MouseFilter         = Control.MouseFilterEnum.Ignore,
		};
		_cardsColumn.AddThemeConstantOverride("separation", 4);
		outerVbox.AddChild(_cardsColumn);

		_goldLabel = new Label();
		_goldLabel.AddThemeFontSizeOverride("font_size", 10);
		_goldLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
		outerVbox.AddChild(_goldLabel);

		var hintLabel = new Label { Text = "[Tab] Log  [Esc] Menu" };
		hintLabel.AddThemeFontSizeOverride("font_size", 7);
		hintLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
		outerVbox.AddChild(hintLabel);

		UiTheme.ApplyPixelFontToAll(this);
	}

	// ── Card construction ─────────────────────────────────────────────

	private void RebuildCards()
	{
		if (_cardsColumn == null) return;

		foreach (var child in _cardsColumn.GetChildren())
			if (child is Node n) n.QueueFree();
		_cards.Clear();

		var party = GameManager.Instance?.Party;
		if (party == null || party.IsEmpty)
		{
			UpdateAllCards();
			return;
		}

		foreach (var m in party.Members)
		{
			var card = BuildCard(m);
			_cardsColumn.AddChild(card.Root);
			_cards[m.MemberId] = card;
		}

		UpdateAllCards();
		UiTheme.ApplyPixelFontToAll(_cardsColumn);
	}

	private CardWidgets BuildCard(PartyMember m)
	{
		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(CardWidth, CardMinHeight),
		};
		ApplyCardStyle(panel, active: false);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left",   6);
		margin.AddThemeConstantOverride("margin_right",  6);
		margin.AddThemeConstantOverride("margin_top",    3);
		margin.AddThemeConstantOverride("margin_bottom", 3);
		panel.AddChild(margin);

		var rootRow = new HBoxContainer();
		rootRow.AddThemeConstantOverride("separation", 6);
		margin.AddChild(rootRow);

		// Animated portrait — same widget the BattleHUD uses.
		var portrait = new AnimatedPortrait
		{
			PortraitSize      = new Vector2(32f, 32f),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
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
		vbox.AddThemeConstantOverride("separation", 1);
		rootRow.AddChild(vbox);

		// Name + level row
		var topRow = new HBoxContainer();
		topRow.AddThemeConstantOverride("separation", 4);
		var nameLabel = new Label
		{
			Text                = m.DisplayName,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Modulate            = Colors.White,
		};
		nameLabel.AddThemeFontSizeOverride("font_size", 10);
		var lvLabel = new Label { Text = "LV 1" };
		lvLabel.AddThemeFontSizeOverride("font_size", 9);
		lvLabel.Modulate = new Color(0.85f, 0.85f, 0.95f);
		topRow.AddChild(nameLabel);
		topRow.AddChild(lvLabel);
		vbox.AddChild(topRow);

		// HP row
		var hpLabel = new Label { Text = "HP 0/0" };
		hpLabel.AddThemeFontSizeOverride("font_size", 9);
		vbox.AddChild(hpLabel);
		var hpBarBg = new ColorRect
		{
			Color             = BarBgColor,
			CustomMinimumSize = new Vector2(120f, 6f),
		};
		var hpBar = new ColorRect
		{
			Color        = new Color(0.95f, 0.85f, 0.10f),
			AnchorLeft   = 0f, AnchorRight  = 1f,
			AnchorTop    = 0f, AnchorBottom = 1f,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		hpBarBg.AddChild(hpBar);
		vbox.AddChild(hpBarBg);

		// MP row — wrapped in its own VBox so we can hide it for 0-MP classes.
		var mpRoot = new VBoxContainer();
		mpRoot.AddThemeConstantOverride("separation", 1);
		var mpLabel = new Label { Text = "MP 0/0" };
		mpLabel.AddThemeFontSizeOverride("font_size", 9);
		mpRoot.AddChild(mpLabel);
		var mpBarBg = new ColorRect
		{
			Color             = BarBgColor,
			CustomMinimumSize = new Vector2(120f, 5f),
		};
		var mpBar = new ColorRect
		{
			Color        = new Color(0.30f, 0.55f, 1f),
			AnchorLeft   = 0f, AnchorRight  = 1f,
			AnchorTop    = 0f, AnchorBottom = 1f,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		mpBarBg.AddChild(mpBar);
		mpRoot.AddChild(mpBarBg);
		vbox.AddChild(mpRoot);

		return new CardWidgets(
			panel, portrait, nameLabel, lvLabel,
			hpLabel, hpBarBg, hpBar,
			mpLabel, mpBarBg, mpBar, mpRoot);
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
			ContentMarginLeft      = 4, ContentMarginRight  = 4,
			ContentMarginTop       = 2, ContentMarginBottom = 2,
		};
		panel.AddThemeStyleboxOverride("panel", style);
	}

	// ── Refresh ───────────────────────────────────────────────────────

	private void UpdateAllCards()
	{
		if (GameManager.Instance == null) return;
		var party = GameManager.Instance.Party;

		// Rebuild on shape change (recruit / remove).
		if (_cards.Count != party.Count)
			RebuildCards();

		foreach (var m in party.Members)
		{
			if (!_cards.TryGetValue(m.MemberId, out var card)) { RebuildCards(); break; }

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

			bool ko = curHp <= 0;
			card.Name.Text     = m.DisplayName + (ko ? " (KO)" : "");
			card.Name.Modulate = ko ? new Color(0.6f, 0.6f, 0.6f) : Colors.White;
			card.Lv.Text       = $"LV {level}";
			card.Hp.Text       = $"HP  {curHp} / {maxHp}";
			card.Mp.Text       = $"MP  {curMp} / {maxMp}";

			float hpRatio = maxHp > 0 ? (float)curHp / maxHp : 0f;
			card.HpBar.AnchorRight = Mathf.Clamp(hpRatio, 0f, 1f);

			bool hasMp = maxMp > 0;
			card.MpRoot.Visible = hasMp;
			if (hasMp)
			{
				float mpRatio = (float)curMp / maxMp;
				card.MpBar.AnchorRight = Mathf.Clamp(mpRatio, 0f, 1f);
			}

			// Highlight the leader's card with a gold border.
			bool isLeader = m.MemberId == party.Leader?.MemberId;
			ApplyCardStyle(card.Root, active: isLeader);
		}

		if (_goldLabel != null)
			_goldLabel.Text = $"G   {GameManager.Instance.Gold}";
	}

	/// <summary>
	/// Briefly flashes Sen's HP bar to <paramref name="flashColor"/> then fades back.
	/// Kept for the existing damage-flash callers; non-Sen members aren't flashed
	/// from gameplay code today so this is intentionally Sen-only.
	/// </summary>
	public void FlashHpBar(Color flashColor, float holdDuration = 0.4f)
	{
		if (!_cards.TryGetValue("sen", out var card)) return;
		var origColor = card.HpBar.Color;
		var t = CreateTween();
		t.TweenProperty(card.HpBar, "color", flashColor, 0.08f);
		t.TweenProperty(card.HpBar, "color", origColor,  0.35f).SetDelay(holdDuration);
	}
}
