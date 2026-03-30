using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Three-tab character customization screen shown after the intro cutscene.
///   Tab 1 — Class:      choose Bard / Fighter / Ranger / Mage
///   Tab 2 — Stats:      allocate 5 bonus points across 6 stats
///   Tab 3 — Appearance: choose 1 of 8 colour-scheme presets
///
/// Confirm applies the result to GameManager via ApplyCharacterCustomization(),
/// then transitions to MAPP.tscn.
/// Skip applies Bard defaults and transitions immediately.
/// </summary>
public partial class CharacterCustomization : Node2D
{
    private const string NextScene   = "res://scenes/overworld/MAPP.tscn";
    private const int    BonusPoints = 5;
    private const int    PerStatCap  = 5;

    private static readonly (string Path, string Desc)[] ClassDefs =
    [
        ("res://resources/characters/class_bard.tres",    "Song-based magic.\nHigh speed and MP."),
        ("res://resources/characters/class_fighter.tres", "Front-line warrior.\nHigh HP and attack."),
        ("res://resources/characters/class_ranger.tres",  "Swift and precise.\nHigh speed and luck."),
        ("res://resources/characters/class_mage.tres",    "Arcane power.\nHigh magic and MP, fragile."),
    ];

    private static readonly string[] SchemePaths =
    [
        "res://resources/color_schemes/scheme_default.tres",
        "res://resources/color_schemes/scheme_shadow.tres",
        "res://resources/color_schemes/scheme_ember.tres",
        "res://resources/color_schemes/scheme_frost.tres",
        "res://resources/color_schemes/scheme_forest.tres",
        "res://resources/color_schemes/scheme_dusk.tres",
        "res://resources/color_schemes/scheme_rose.tres",
        "res://resources/color_schemes/scheme_gold.tres",
    ];

    // (save key, display label, hp-multiplier for one point spend)
    private static readonly (string Key, string Label, int Mult)[] StatDefs =
    [
        ("hp",      "HP",      4),
        ("attack",  "Attack",  1),
        ("defense", "Defense", 1),
        ("magic",   "Magic",   1),
        ("speed",   "Speed",   1),
        ("luck",    "Luck",    1),
    ];

    // Loaded resources
    private CharacterStats[] _presets = null!;
    private ColorScheme[]    _schemes = null!;

    // Selection state
    private int _selectedClassIdx  = 0;
    private int _selectedSchemeIdx = 0;
    private int _remainingPoints   = BonusPoints;
    private readonly Dictionary<string, int> _deltas = new();

    // UI refs built in BuildXxxTab()
    private Button[]     _classSelectBtns = null!;
    private Label        _pointsLabel     = null!;
    private readonly Dictionary<string, Label> _statCurrentLabels = new();
    private readonly Dictionary<string, Label> _statBaseLabels    = new();
    private StyleBoxFlat[] _swatchStyles  = null!;
    private Button[]       _swatchBtns    = null!;
    private TextureRect    _previewSprite = null!;

    private bool _transitioning = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _presets = new CharacterStats[ClassDefs.Length];
        for (int i = 0; i < ClassDefs.Length; i++)
            _presets[i] = GD.Load<CharacterStats>(ClassDefs[i].Path);

        _schemes = new ColorScheme[SchemePaths.Length];
        for (int i = 0; i < SchemePaths.Length; i++)
            _schemes[i] = GD.Load<ColorScheme>(SchemePaths[i]);

        GetNode<Button>("UI/Margin/VBox/BottomBar/ConfirmButton").Pressed += OnConfirmPressed;
        GetNode<Button>("UI/Margin/VBox/BottomBar/SkipButton").Pressed    += OnSkipPressed;

        BuildClassTab();
        BuildStatsTab();
        BuildAppearanceTab();

        SelectClass(0);
        SelectScheme(0);
    }

    // ── Tab 1 — Class ─────────────────────────────────────────────────────────

    private void BuildClassTab()
    {
        var tab  = GetNode<VBoxContainer>("UI/Margin/VBox/Tabs/Class");
        var hbox = new HBoxContainer();
        hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.SizeFlagsVertical   = Control.SizeFlags.ExpandFill;
        hbox.AddThemeConstantOverride("separation", 8);
        tab.AddChild(hbox);

        _classSelectBtns = new Button[ClassDefs.Length];

        for (int i = 0; i < ClassDefs.Length; i++)
        {
            int            idx   = i;
            CharacterStats stats = _presets[i];

            var card = new PanelContainer();
            card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            card.SizeFlagsVertical   = Control.SizeFlags.ExpandFill;

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 6);

            var nameLabel = new Label { Text = stats.ClassName, HorizontalAlignment = HorizontalAlignment.Center };
            nameLabel.AddThemeFontSizeOverride("font_size", 16);

            var descLabel = new Label
            {
                Text                = ClassDefs[i].Desc,
                AutowrapMode        = TextServer.AutowrapMode.Word,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var statsLabel = new Label
            {
                Text = $"HP {stats.MaxHp}  ATK {stats.Attack}  DEF {stats.Defense}\n" +
                       $"MAG {stats.Magic}  SPD {stats.Speed}  LCK {stats.Luck}  MP {stats.MaxMp}",
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var btn = new Button { Text = "Select", SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
            btn.Pressed += () => SelectClass(idx);
            _classSelectBtns[i] = btn;

            vbox.AddChild(nameLabel);
            vbox.AddChild(descLabel);
            vbox.AddChild(statsLabel);
            vbox.AddChild(btn);
            card.AddChild(vbox);
            hbox.AddChild(card);
        }
    }

    private void SelectClass(int idx)
    {
        _selectedClassIdx = idx;
        _deltas.Clear();
        _remainingPoints = BonusPoints;

        for (int i = 0; i < _classSelectBtns.Length; i++)
            _classSelectBtns[i].Text = i == idx ? "✓ Selected" : "Select";

        RefreshStatRows();
    }

    // ── Tab 2 — Stats ─────────────────────────────────────────────────────────

    private void BuildStatsTab()
    {
        var tab = GetNode<VBoxContainer>("UI/Margin/VBox/Tabs/Stats");

        _pointsLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _pointsLabel.AddThemeFontSizeOverride("font_size", 15);
        tab.AddChild(_pointsLabel);
        tab.AddChild(new HSeparator());

        foreach (var (key, label, mult) in StatDefs)
        {
            string k = key;
            int    m = mult;

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var nameLabel = new Label
            {
                Text              = label,
                CustomMinimumSize = new Vector2(80, 0),
            };

            var currentLabel = new Label
            {
                CustomMinimumSize   = new Vector2(40, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            _statCurrentLabels[key] = currentLabel;

            var minusBtn = new Button { Text = "−", CustomMinimumSize = new Vector2(30, 0) };
            var plusBtn  = new Button { Text = "+", CustomMinimumSize = new Vector2(30, 0) };
            minusBtn.Pressed += () => AdjustStat(k, -1, m);
            plusBtn.Pressed  += () => AdjustStat(k,  1, m);

            var baseLabel = new Label
            {
                CustomMinimumSize   = new Vector2(70, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate            = new Color(0.55f, 0.55f, 0.55f),
            };
            _statBaseLabels[key] = baseLabel;

            row.AddChild(nameLabel);
            row.AddChild(currentLabel);
            row.AddChild(minusBtn);
            row.AddChild(plusBtn);
            row.AddChild(baseLabel);
            tab.AddChild(row);
        }

        var resetBtn = new Button { Text = "Reset", SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        resetBtn.Pressed += () => SelectClass(_selectedClassIdx);
        tab.AddChild(resetBtn);
    }

    private void AdjustStat(string key, int direction, int mult)
    {
        int delta = _deltas.GetValueOrDefault(key);
        if (direction > 0)
        {
            if (_remainingPoints < 1 || delta >= PerStatCap) return;
            _deltas[key] = delta + 1;
            _remainingPoints--;
        }
        else
        {
            if (delta <= 0) return;
            _deltas[key] = delta - 1;
            _remainingPoints++;
        }
        RefreshStatRows();
    }

    private void RefreshStatRows()
    {
        var preset = _presets[_selectedClassIdx];
        _pointsLabel.Text = $"Bonus points remaining: {_remainingPoints}";

        void Update(string key, int baseVal)
        {
            int current = baseVal + _deltas.GetValueOrDefault(key) * (key == "hp" ? 4 : 1);
            if (_statCurrentLabels.TryGetValue(key, out var cur)) cur.Text = current.ToString();
            if (_statBaseLabels.TryGetValue(key,    out var bas)) bas.Text = $"(base {baseVal})";
        }

        Update("hp",      preset.MaxHp);
        Update("attack",  preset.Attack);
        Update("defense", preset.Defense);
        Update("magic",   preset.Magic);
        Update("speed",   preset.Speed);
        Update("luck",    preset.Luck);
    }

    // ── Tab 3 — Appearance ────────────────────────────────────────────────────

    private void BuildAppearanceTab()
    {
        var tab = GetNode<VBoxContainer>("UI/Margin/VBox/Tabs/Appearance");

        var header = new Label { Text = "Choose a colour scheme", HorizontalAlignment = HorizontalAlignment.Center };
        tab.AddChild(header);

        // Preview sprite (centred)
        var previewRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        _previewSprite = new TextureRect
        {
            Texture           = GD.Load<Texture2D>("res://assets/sprites/player/Sen_Overworld.png"),
            CustomMinimumSize = new Vector2(80, 80),
            ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        previewRow.AddChild(_previewSprite);
        tab.AddChild(previewRow);

        // Swatch grid
        var flow = new HFlowContainer();
        flow.AddThemeConstantOverride("h_separation", 10);
        flow.AddThemeConstantOverride("v_separation", 8);
        tab.AddChild(flow);

        _swatchBtns   = new Button[_schemes.Length];
        _swatchStyles = new StyleBoxFlat[_schemes.Length];

        for (int i = 0; i < _schemes.Length; i++)
        {
            int        idx    = i;
            ColorScheme scheme = _schemes[i];

            var swatchBox = new VBoxContainer();
            swatchBox.AddThemeConstantOverride("separation", 2);

            var style = new StyleBoxFlat
            {
                BgColor          = scheme.Tint,
                BorderColor      = Colors.DimGray,
                BorderWidthLeft  = 2, BorderWidthRight  = 2,
                BorderWidthTop   = 2, BorderWidthBottom = 2,
            };
            _swatchStyles[i] = style;

            var hoverStyle   = new StyleBoxFlat { BgColor = scheme.Tint.Lightened(0.15f) };
            var pressedStyle = new StyleBoxFlat { BgColor = scheme.Tint.Darkened(0.15f) };

            var btn = new Button { CustomMinimumSize = new Vector2(48, 48), FocusMode = Control.FocusModeEnum.Click };
            btn.AddThemeStyleboxOverride("normal",  style);
            btn.AddThemeStyleboxOverride("hover",   hoverStyle);
            btn.AddThemeStyleboxOverride("pressed", pressedStyle);
            btn.AddThemeStyleboxOverride("focus",   style);
            btn.Pressed += () => SelectScheme(idx);
            _swatchBtns[i] = btn;

            var nameLabel = new Label
            {
                Text                = scheme.SchemeName,
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize   = new Vector2(48, 0),
            };

            swatchBox.AddChild(btn);
            swatchBox.AddChild(nameLabel);
            flow.AddChild(swatchBox);
        }
    }

    private void SelectScheme(int idx)
    {
        _selectedSchemeIdx = idx;
        _previewSprite.Modulate = _schemes[idx].Tint;

        for (int i = 0; i < _swatchStyles.Length; i++)
        {
            bool selected = i == idx;
            _swatchStyles[i].BorderColor      = selected ? Colors.White : Colors.DimGray;
            _swatchStyles[i].BorderWidthLeft  = selected ? 3 : 2;
            _swatchStyles[i].BorderWidthRight = selected ? 3 : 2;
            _swatchStyles[i].BorderWidthTop   = selected ? 3 : 2;
            _swatchStyles[i].BorderWidthBottom = selected ? 3 : 2;
        }
    }

    // ── Confirm / Skip ────────────────────────────────────────────────────────

    private void OnConfirmPressed()
    {
        if (_transitioning) return;
        _transitioning = true;
        ApplyToGameManager();
        _ = SceneTransition.Instance.GoToAsync(NextScene);
    }

    private void OnSkipPressed()
    {
        if (_transitioning) return;
        _transitioning = true;
        _selectedClassIdx  = 0;
        _selectedSchemeIdx = 0;
        _deltas.Clear();
        ApplyToGameManager();
        _ = SceneTransition.Instance.GoToAsync(NextScene);
    }

    private void ApplyToGameManager()
    {
        var preset = _presets[_selectedClassIdx];

        // Build final stats: class preset + point deltas
        var stats = new CharacterStats
        {
            MaxHp      = preset.MaxHp    + _deltas.GetValueOrDefault("hp")      * 4,
            Attack     = preset.Attack   + _deltas.GetValueOrDefault("attack"),
            Defense    = preset.Defense  + _deltas.GetValueOrDefault("defense"),
            Magic      = preset.Magic    + _deltas.GetValueOrDefault("magic"),
            Speed      = preset.Speed    + _deltas.GetValueOrDefault("speed"),
            Luck       = preset.Luck     + _deltas.GetValueOrDefault("luck"),
            Resistance = preset.Resistance,
            MaxMp      = preset.MaxMp,
            MoveSpeed  = preset.MoveSpeed,
            InvincibilityDuration = preset.InvincibilityDuration,
            Class      = preset.Class,
            ClassName  = preset.ClassName,
        };
        stats.CurrentHp = stats.MaxHp;
        stats.CurrentMp = stats.MaxMp;

        GameManager.Instance.ApplyCharacterCustomization(stats, _schemes[_selectedSchemeIdx]);
    }
}
