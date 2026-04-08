using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Extensions;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Three-tab character customization screen shown after the intro cutscene.
///   Tab 1 — Class:      choose Bard / Fighter / Ranger / Mage / Rogue / Alchemist
///   Tab 2 — Stats:      allocate 5 bonus points across 6 stats
///   Tab 3 — Appearance: per-colour palette swap of the player sprite
///
/// Confirm applies the result to GameManager via ApplyCharacterCustomization(),
/// then transitions to MAPP.tscn.
/// Skip applies Bard defaults and transitions immediately with no colour changes.
/// </summary>
public partial class CharacterCustomization : Node2D
{
    private const string NextScene   = "res://scenes/overworld/WorldMap.tscn";
    private const int    BonusPoints = 5;
    private const int    PerStatCap  = 5;
    private const int    ClassGridColumns = 3;

    private static readonly (string Path, string Desc)[] ClassDefs =
    [
        ("res://resources/characters/class_bard.tres",      "Song-based magic.\nHigh speed and MP."),
        ("res://resources/characters/class_fighter.tres",   "Front-line warrior.\nHigh HP and attack."),
        ("res://resources/characters/class_ranger.tres",    "Swift and precise.\nHigh speed and luck."),
        ("res://resources/characters/class_mage.tres",      "Arcane power.\nHigh magic and MP, fragile."),
        ("res://resources/characters/class_rogue.tres",     "Quick blades.\nMaximum speed, can steal."),
        ("res://resources/characters/class_alchemist.tres", "Brewer of fortune.\nHigh luck and MP, frail."),
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

    // Colour preset definitions: (name, hue rotation in degrees, saturation multiplier, value multiplier)
    private static readonly (string Name, float HueDeg, float SatMult, float ValMult)[] PresetDefs =
    [
        ("Default",  0f,   1.0f, 1.0f),
        ("Shadow",   260f, 0.6f, 0.7f),
        ("Ember",    330f, 1.3f, 1.0f),
        ("Frost",    190f, 0.9f, 1.1f),
        ("Forest",   110f, 1.1f, 0.9f),
        ("Dusk",     250f, 0.7f, 0.8f),
        ("Rose",     320f, 1.1f, 1.0f),
        ("Gold",      50f, 1.2f, 1.1f),
    ];

    // Loaded class resources
    private CharacterStats[] _presets = null!;

    // Selection state
    private int _selectedClassIdx = 0;
    private int _remainingPoints  = BonusPoints;
    private readonly Dictionary<string, int> _deltas = new();

    // UI refs — class tab
    private Button[] _classSelectBtns = null!;

    // UI refs — stats tab
    private Label _pointsLabel = null!;
    private readonly Dictionary<string, Label> _statCurrentLabels = new();
    private readonly Dictionary<string, Label> _statBaseLabels    = new();

    // UI refs — appearance tab
    private TextureRect         _previewSprite = null!;
    private Color[]             _sourceColors  = [];
    private Color[]             _targetColors  = [];
    private ColorPickerButton[] _pickers       = [];

    private bool _transitioning = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _presets = new CharacterStats[ClassDefs.Length];
        for (int i = 0; i < ClassDefs.Length; i++)
            _presets[i] = GD.Load<CharacterStats>(ClassDefs[i].Path);

        GetNode<Button>("UI/Margin/VBox/BottomBar/ConfirmButton").Pressed += OnConfirmPressed;
        GetNode<Button>("UI/Margin/VBox/BottomBar/SkipButton").Pressed    += OnSkipPressed;

        BuildClassTab();
        BuildStatsTab();
        BuildAppearanceTab();

        SelectClass(0);

        // Apply SNES theme
        UiTheme.ApplyToAllButtons(this);
		UiTheme.ApplyPixelFontToAll(this);
    }

    // ── Tab 1 — Class ─────────────────────────────────────────────────────────

    private void BuildClassTab()
    {
        var tab  = GetNode<VBoxContainer>("UI/Margin/VBox/Tabs/Class");

        // Use a 3-column GridContainer so 4 / 6 / 9 classes all lay out cleanly
        // without overflowing the viewport horizontally. Each card is constrained
        // by SizeFlagsHorizontal = ExpandFill which divides the available width
        // evenly across the columns.
        var grid = new GridContainer
        {
            Columns                = ClassGridColumns,
            SizeFlagsHorizontal    = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical      = Control.SizeFlags.ExpandFill,
        };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        tab.AddChild(grid);

        _classSelectBtns = new Button[ClassDefs.Length];

        for (int i = 0; i < ClassDefs.Length; i++)
        {
            int            idx   = i;
            CharacterStats stats = _presets[i];

            var card = new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
                ClipContents        = true,
            };

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 6);
            vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            var nameLabel = new Label
            {
                Text                = stats.ClassName,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 12);

            var descLabel = new Label
            {
                Text                 = ClassDefs[i].Desc,
                AutowrapMode         = TextServer.AutowrapMode.Word,
                HorizontalAlignment  = HorizontalAlignment.Center,
                SizeFlagsHorizontal  = Control.SizeFlags.ExpandFill,
            };
            descLabel.AddThemeFontSizeOverride("font_size", 10);

            var statsLabel = new Label
            {
                Text = $"HP {stats.MaxHp}  ATK {stats.Attack}  DEF {stats.Defense}\n" +
                       $"MAG {stats.Magic}  SPD {stats.Speed}  LCK {stats.Luck}\n" +
                       $"MP {stats.MaxMp}",
                HorizontalAlignment  = HorizontalAlignment.Center,
                SizeFlagsHorizontal  = Control.SizeFlags.ExpandFill,
            };
            statsLabel.AddThemeFontSizeOverride("font_size", 9);

            var btn = new Button
            {
                Text                = "Select",
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                CustomMinimumSize   = new Vector2(120f, 32f),
            };
            btn.AddThemeFontSizeOverride("font_size", 14);
            btn.Pressed += () => SelectClass(idx);
            _classSelectBtns[i] = btn;

            vbox.AddChild(nameLabel);
            vbox.AddChild(descLabel);
            vbox.AddChild(statsLabel);
            vbox.AddChild(btn);
            card.AddChild(vbox);
            grid.AddChild(card);
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

        // Extract unique colours from the player sprite (sorted by frequency)
        _sourceColors = SpriteColorExtractor.ExtractUniqueColors(
            "res://assets/sprites/player/Sen_Overworld.png");
        _targetColors = (Color[])_sourceColors.Clone();

        var header = new Label { Text = "Customise Colours", HorizontalAlignment = HorizontalAlignment.Center };
        header.AddThemeFontSizeOverride("font_size", 14);
        tab.AddChild(header);

        // Live preview
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

        // Preset buttons
        var presetLabel = new Label { Text = "Presets:", HorizontalAlignment = HorizontalAlignment.Center };
        tab.AddChild(presetLabel);

        var presetFlow = new HFlowContainer();
        presetFlow.AddThemeConstantOverride("h_separation", 6);
        presetFlow.AddThemeConstantOverride("v_separation", 4);
        tab.AddChild(presetFlow);

        foreach (var (name, hue, sat, val) in PresetDefs)
        {
            float h = hue, s = sat, v = val;
            var btn = new Button { Text = name, CustomMinimumSize = new Vector2(68, 0) };
            btn.Pressed += () => ApplyPreset(h, s, v);
            presetFlow.AddChild(btn);
        }

        if (_sourceColors.Length == 0)
        {
            tab.AddChild(new Label { Text = "(No sprite colours found)", HorizontalAlignment = HorizontalAlignment.Center });
            _pickers = [];
            return;
        }

        // Scroll container for per-colour rows
        var pickerLabel = new Label { Text = "Per-colour adjustments:", HorizontalAlignment = HorizontalAlignment.Center };
        tab.AddChild(pickerLabel);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        var colorList = new VBoxContainer();
        colorList.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(colorList);
        tab.AddChild(scroll);

        _pickers = new ColorPickerButton[_sourceColors.Length];
        for (int i = 0; i < _sourceColors.Length; i++)
        {
            int idx = i;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);

            var origSwatch = new ColorRect
            {
                Color             = _sourceColors[i],
                CustomMinimumSize = new Vector2(28, 22),
            };

            var arrow = new Label { Text = "→", VerticalAlignment = VerticalAlignment.Center };

            var picker = new ColorPickerButton
            {
                Color             = _targetColors[i],
                CustomMinimumSize = new Vector2(60, 22),
            };
            picker.ColorChanged += color =>
            {
                _targetColors[idx] = color;
                RefreshPreview();
            };
            _pickers[i] = picker;

            row.AddChild(origSwatch);
            row.AddChild(arrow);
            row.AddChild(picker);
            colorList.AddChild(row);
        }

        // Reset
        var resetBtn = new Button { Text = "Reset to Original", SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        resetBtn.Pressed += ResetToOriginal;
        tab.AddChild(resetBtn);

        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (_sourceColors.Length == 0) return;
        PaletteSwapHelper.ApplyPalette(_previewSprite, _sourceColors, _targetColors);
    }

    private void ApplyPreset(float hueDeg, float satMult, float valMult)
    {
        for (int i = 0; i < _sourceColors.Length; i++)
        {
            _targetColors[i] = ShiftColor(_sourceColors[i], hueDeg, satMult, valMult);
            if (i < _pickers.Length) _pickers[i].Color = _targetColors[i];
        }
        RefreshPreview();
    }

    private void ResetToOriginal()
    {
        for (int i = 0; i < _sourceColors.Length; i++)
        {
            _targetColors[i] = _sourceColors[i];
            if (i < _pickers.Length) _pickers[i].Color = _targetColors[i];
        }
        RefreshPreview();
    }

    private static Color ShiftColor(Color src, float hueDeg, float satMult, float valMult)
    {
        src.ToHsv(out float h, out float s, out float v);
        h = ((h + hueDeg / 360f) % 1f + 1f) % 1f;
        s = Mathf.Clamp(s * satMult, 0f, 1f);
        v = Mathf.Clamp(v * valMult, 0f, 1f);
        return Color.FromHsv(h, s, v);
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
        _selectedClassIdx = 0;
        _deltas.Clear();
        ApplyToGameManager();
        _ = SceneTransition.Instance.GoToAsync(NextScene);
    }

    private void ApplyToGameManager()
    {
        var preset = _presets[_selectedClassIdx];

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

        GameManager.Instance.ApplyCharacterCustomization(stats, _sourceColors, _targetColors);
    }
}
