using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Pokédex-style Bestiary menu opened from the PauseMenu.
///
/// Two-panel layout: left side = scrollable entry list (showing entry numbers,
/// names, and mastery stars), right side = detail panel with the selected enemy's
/// animated battle sprite, flavor text, stats, drops (when Studied), and a
/// rhythm-BPM heartbeat visualizer.
///
/// Tier rules:
///   Locked     → silhouette + ??? (zero kills)
///   Discovered → name, sprite, flavor, stats, "X more kills until drops" hint
///   Studied    → everything above + drop table with rates, Bardic skills, BPM
///
/// The BPM visualizer uses a local Time-based phase (NOT RhythmClock) so the
/// overworld BGM clock attachment is left undisturbed.
/// </summary>
public partial class BestiaryMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static Color Gold       => UiTheme.Gold;
    private static Color HaveGreen  => UiTheme.HaveGreen;
    private static Color SubtleGrey => UiTheme.SubtleGrey;
    private static Color NeedRed    => UiTheme.NeedRed;

    // ── State ────────────────────────────────────────────────────────────
    private System.Collections.Generic.IReadOnlyList<EnemyData> _allEnemies = System.Array.Empty<EnemyData>();
    private int                _selectedIndex;
    private VBoxContainer      _entryListVbox = null!;
    private VBoxContainer      _detailVbox    = null!;
    private TextureRect        _spriteView    = null!;
    private AtlasTexture?      _spriteAtlas;
    private BpmPulse?          _bpmPulse;
    private float              _spriteAccum;
    private int                _spriteFrameIdx;
    private readonly System.Collections.Generic.List<Button> _entryButtons = new();

    public override void _Ready()
    {
        Layer       = 51;
        Visible     = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void Open()
    {
        _allEnemies     = BattleRegistry.Instance.AllEnemies();
        _selectedIndex  = 0;
        _spriteFrameIdx = 0;
        _spriteAccum    = 0f;
        BuildUi();
        UiTheme.ApplyPixelFontToAll(this);
        UiTheme.ApplyToAllButtons(this);
        Visible = true;
        if (_entryButtons.Count > 0)
            _entryButtons[0].CallDeferred(Control.MethodName.GrabFocus);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;
        if (!e.IsActionPressed("ui_cancel")) return;
        GetViewport().SetInputAsHandled();
        Close();
    }

    private void Close()
    {
        AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    // ── Per-frame: animate sprite + BPM pulse ────────────────────────────

    public override void _Process(double delta)
    {
        if (!Visible) return;

        var enemy = CurrentEnemy();
        if (enemy?.BattleSprite != null && enemy.SpriteFrameCount > 0 && _spriteAtlas != null)
        {
            _spriteAccum += (float)delta;
            float frameDur = 1f / Mathf.Max(0.1f, enemy.SpriteAnimFps);
            while (_spriteAccum >= frameDur)
            {
                _spriteAccum   -= frameDur;
                _spriteFrameIdx = (_spriteFrameIdx + 1) % enemy.SpriteFrameCount;
            }
            int sz = enemy.SpriteFrameSize;
            _spriteAtlas.Region = new Rect2(_spriteFrameIdx * sz, 0, sz, sz);
        }

        // Guard against scene-tear-down: when the dungeon unloads, the
        // BpmPulse Control may already be disposed while this _Process runs
        // one more frame on its parent CanvasLayer.
        if (_bpmPulse != null)
        {
            if (IsInstanceValid(_bpmPulse))
                _bpmPulse.QueueRedraw();
            else
                _bpmPulse = null;
        }
    }

    // ── UI build ─────────────────────────────────────────────────────────

    private void BuildUi()
    {
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();

        _entryButtons.Clear();
        _bpmPulse = null;

        var overlay = new ColorRect
        {
            Color        = UiTheme.OverlayDim,
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(overlay);

        var centerer = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(centerer);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(720f, 480f) };
        UiTheme.ApplyPanelTheme(panel);
        centerer.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   16);
        margin.AddThemeConstantOverride("margin_right",  16);
        margin.AddThemeConstantOverride("margin_top",    12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var rootVbox = new VBoxContainer();
        rootVbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(rootVbox);

        // Title with completion ticker
        var (discovered, total) = BestiaryLogic.Completion(
            EnumerateEnemyIds(),
            ToReadOnlyDict(GameManager.Instance.KillCounts));
        var title = new Label
        {
            Text                = $"BESTIARY    ({discovered}/{total})",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        rootVbox.AddChild(title);
        rootVbox.AddChild(new HSeparator());

        // Two-panel body: entry list (left) + detail (right)
        var body = new HBoxContainer();
        body.AddThemeConstantOverride("separation", 12);
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.SizeFlagsVertical   = Control.SizeFlags.ExpandFill;
        rootVbox.AddChild(body);

        // ── Left: scrollable entry list ──
        var listScroll = new ScrollContainer
        {
            CustomMinimumSize    = new Vector2(260f, 360f),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        body.AddChild(listScroll);

        _entryListVbox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _entryListVbox.AddThemeConstantOverride("separation", 2);
        listScroll.AddChild(_entryListVbox);

        BuildEntryList();

        // ── Right: detail panel ──
        _detailVbox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
        };
        _detailVbox.AddThemeConstantOverride("separation", 6);
        body.AddChild(_detailVbox);

        BuildDetailPanel();

        rootVbox.AddChild(new HSeparator());
        var hint = new Label
        {
            Text                = "[↑↓] Browse  [Esc] Back",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 12);
        rootVbox.AddChild(hint);
    }

    private void BuildEntryList()
    {
        foreach (var child in _entryListVbox.GetChildren())
            child.QueueFree();
        _entryButtons.Clear();

        var killCounts = GameManager.Instance.KillCounts;

        for (int i = 0; i < _allEnemies.Count; i++)
        {
            var enemy = _allEnemies[i];
            int kills = killCounts.GetValueOrDefault(enemy.EnemyId, 0);
            var tier  = BestiaryLogic.TierFor(kills);
            var rank  = BestiaryLogic.MasteryFor(kills);

            string number = $"№{i + 1:D3}";
            string label  = tier == BestiaryLogic.EntryTier.Locked
                ? $"{number}  ???"
                : $"{number}  {enemy.DisplayName}";

            // Append mastery stars to studied entries.
            if (rank != BestiaryLogic.MasteryRank.None)
                label += "  " + new string('★', (int)rank);

            int capturedIndex = i;
            var btn = new Button
            {
                Text                = label,
                Alignment           = HorizontalAlignment.Left,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            btn.AddThemeColorOverride("font_color",
                tier == BestiaryLogic.EntryTier.Locked ? SubtleGrey : Gold);
            btn.AddThemeFontSizeOverride("font_size", 12);
            btn.Pressed       += () => SelectEntry(capturedIndex);
            btn.FocusEntered  += () => { AudioManager.Instance?.PlaySfx(UiSfx.Cursor); SelectEntry(capturedIndex); };
            _entryListVbox.AddChild(btn);
            _entryButtons.Add(btn);
        }
    }

    /// <summary>
    /// Rebuild the right-hand detail panel from <see cref="CurrentEnemy"/>.
    /// Called once on Open and again every time the player selects a new entry.
    /// </summary>
    private void BuildDetailPanel()
    {
        foreach (var child in _detailVbox.GetChildren())
            child.QueueFree();

        var enemy = CurrentEnemy();
        if (enemy == null) return;

        int kills = GameManager.Instance.KillCounts.GetValueOrDefault(enemy.EnemyId, 0);
        var tier  = BestiaryLogic.TierFor(kills);

        // Header: number + name (or ??? when locked)
        string headerText = tier == BestiaryLogic.EntryTier.Locked
            ? $"№{(_selectedIndex + 1):D3} — ???"
            : $"№{(_selectedIndex + 1):D3} — {enemy.DisplayName}";
        var header = new Label
        {
            Text     = headerText,
            Modulate = Gold,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        header.AddThemeFontSizeOverride("font_size", 16);
        _detailVbox.AddChild(header);

        // Sprite + BPM pulse row
        var spriteRow = new HBoxContainer();
        spriteRow.AddThemeConstantOverride("separation", 8);
        spriteRow.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        _detailVbox.AddChild(spriteRow);

        _spriteView = BuildSpriteView(enemy, tier);
        spriteRow.AddChild(_spriteView);
        _spriteFrameIdx = 0;
        _spriteAccum    = 0f;

        // BPM heartbeat — shown for both Discovered and Studied tiers when BPM > 0
        if (tier != BestiaryLogic.EntryTier.Locked && enemy.BattleBpm > 0f)
        {
            var bpmContainer = new VBoxContainer
            {
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            };
            bpmContainer.AddThemeConstantOverride("separation", 2);

            _bpmPulse = new BpmPulse(() => CurrentEnemy()?.BattleBpm ?? 0f)
            {
                CustomMinimumSize = new Vector2(36f, 36f),
            };
            bpmContainer.AddChild(_bpmPulse);

            var bpmLabel = new Label
            {
                Text                = $"{enemy.BattleBpm:F0} BPM",
                Modulate            = SubtleGrey,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            bpmLabel.AddThemeFontSizeOverride("font_size", 10);
            bpmContainer.AddChild(bpmLabel);

            spriteRow.AddChild(bpmContainer);
        }

        // Body content varies by tier.
        switch (tier)
        {
            case BestiaryLogic.EntryTier.Locked:
                AddLine("Defeat this enemy to add it to the bestiary.", SubtleGrey);
                break;

            case BestiaryLogic.EntryTier.Discovered:
                BuildDiscoveredBody(enemy, kills);
                break;

            case BestiaryLogic.EntryTier.Studied:
                BuildStudiedBody(enemy, kills);
                break;
        }
    }

    private TextureRect BuildSpriteView(EnemyData enemy, BestiaryLogic.EntryTier tier)
    {
        int sz   = enemy.SpriteFrameSize;
        int side = sz > 0 ? sz : 32;
        var view = new TextureRect
        {
            CustomMinimumSize = new Vector2(side * 4, side * 4),
            ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode       = TextureRect.StretchModeEnum.KeepAspectCovered,
            TextureFilter     = CanvasItem.TextureFilterEnum.Nearest,
            Texture           = enemy.BattleSprite,
        };

        // Sprite-sheets need an AtlasTexture so we can step through frames each
        // _Process tick by mutating Region. Static (non-sheet) sprites just use
        // the raw texture and skip the per-frame update.
        if (enemy.BattleSprite != null && enemy.SpriteFrameCount > 0)
        {
            _spriteAtlas = new AtlasTexture
            {
                Atlas  = enemy.BattleSprite,
                Region = new Rect2(0, 0, sz, sz),
            };
            view.Texture = _spriteAtlas;
        }
        else
        {
            _spriteAtlas = null;
        }

        // Locked silhouette: tint to black; the underlying sprite still animates.
        if (tier == BestiaryLogic.EntryTier.Locked)
            view.Modulate = Colors.Black;

        return view;
    }

    private void BuildDiscoveredBody(EnemyData enemy, int kills)
    {
        if (!string.IsNullOrEmpty(enemy.FlavorText))
            AddLine(enemy.FlavorText, Colors.White);

        AddSeparator();
        AddStatRow(enemy);
        AddLine($"Defeated: {kills} time(s)", HaveGreen);

        var entry = GameManager.Instance.Bestiary.Entries.GetValueOrDefault(enemy.EnemyId);
        if (entry != null && entry.FirstDefeatedUtc != default)
        {
            string ts = entry.FirstDefeatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            AddLine($"First defeated: {ts}", SubtleGrey);
        }

        AddSeparator();
        int needed = BestiaryLogic.KillsUntilStudied(kills);
        AddLine($"Defeat {needed} more time(s) to reveal drops.", NeedRed);
    }

    private void BuildStudiedBody(EnemyData enemy, int kills)
    {
        if (!string.IsNullOrEmpty(enemy.FlavorText))
            AddLine(enemy.FlavorText, Colors.White);

        AddSeparator();
        AddStatRow(enemy);

        var rank = BestiaryLogic.MasteryFor(kills);
        AddLine($"Defeated: {kills} time(s)    Mastery: {rank}", HaveGreen);

        var entry = GameManager.Instance.Bestiary.Entries.GetValueOrDefault(enemy.EnemyId);
        if (entry != null && entry.FirstDefeatedUtc != default)
        {
            string ts = entry.FirstDefeatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            AddLine($"First defeated: {ts}", SubtleGrey);
        }

        AddSeparator();
        BuildDropsSection(enemy);

        AddLine($"Gold: {enemy.GoldDrop}    Exp: {enemy.ExpDrop}", SubtleGrey);

        if (enemy.BardicActOptions != null && enemy.BardicActOptions.Length > 0)
            AddLine("Bardic skills: " + string.Join(", ", enemy.BardicActOptions), SubtleGrey);

        // Personal best rhythm streak from Rhythm Memory.
        if (GameManager.Instance.RhythmMemory.TryGetValue(enemy.EnemyId, out var mem))
            AddLine($"Best streak: {mem.BestMaxStreak}", SubtleGrey);
    }

    private void BuildDropsSection(EnemyData enemy)
    {
        var entries = new System.Collections.Generic.List<LootEntry>();
        if (enemy.LootTable != null)
        {
            foreach (var res in enemy.LootTable)
                if (res is LootEntry le) entries.Add(le);
        }

        if (entries.Count == 0)
        {
            AddLine("Drops: (none in table)", SubtleGrey);
        }
        else
        {
            AddLine("─── DROPS (rates) ───", Gold);

            var paths      = entries.ConvertAll(e => e.ItemPath).ToArray();
            var weights    = entries.ConvertAll(e => e.Weight).ToArray();
            var guaranteed = entries.ConvertAll(e => e.Guaranteed).ToArray();
            var probs      = LootLogic.ComputeProbabilities(weights, guaranteed);

            for (int i = 0; i < entries.Count; i++)
            {
                string display = ItemDisplayName(paths[i]);
                string pct     = guaranteed[i]
                    ? "100% (guaranteed)"
                    : $"{probs[i] * 100f:F0}%";
                AddLine($"  • {display,-22} {pct}", Colors.White);
            }
        }

        // Bonus loot is the Rhythm Memory adaptive drop — labeled clearly.
        if (!string.IsNullOrEmpty(enemy.BonusLootItemPath))
        {
            string bonusName = ItemDisplayName(enemy.BonusLootItemPath);
            AddLine($"Bonus loot: {bonusName} (Rhythm Memory)", new Color(0.7f, 0.85f, 1f));
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void SelectEntry(int index)
    {
        if (index < 0 || index >= _allEnemies.Count) return;
        if (index == _selectedIndex && _detailVbox != null && _detailVbox.GetChildCount() > 0) return;
        _selectedIndex = index;
        BuildDetailPanel();
    }

    private EnemyData? CurrentEnemy()
        => (_selectedIndex >= 0 && _selectedIndex < _allEnemies.Count)
            ? _allEnemies[_selectedIndex]
            : null;

    private System.Collections.Generic.IEnumerable<string> EnumerateEnemyIds()
    {
        foreach (var e in _allEnemies)
            yield return e.EnemyId;
    }

    private static System.Collections.Generic.IReadOnlyDictionary<string, int> ToReadOnlyDict(
        System.Collections.Generic.Dictionary<string, int> src) => src;

    private void AddLine(string text, Color color)
    {
        var label = new Label
        {
            Text         = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate     = color,
        };
        label.AddThemeFontSizeOverride("font_size", 11);
        _detailVbox.AddChild(label);
    }

    private void AddSeparator()
    {
        var sep = new HSeparator();
        _detailVbox.AddChild(sep);
    }

    private void AddStatRow(EnemyData enemy)
    {
        var stats = enemy.Stats;
        if (stats == null) return;
        AddLine($"HP {stats.MaxHp}  ATK {stats.Attack}  DEF {stats.Defense}  SPD {stats.Speed}",
                Colors.White);
    }

    private static string ItemDisplayName(string path)
    {
        if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path))
            return path;
        var item = GD.Load<ItemData>(path);
        return item != null && !string.IsNullOrEmpty(item.DisplayName)
            ? item.DisplayName
            : path.GetFile().Replace(".tres", "");
    }

    // ── BPM heartbeat sub-control ───────────────────────────────────────

    /// <summary>
    /// Tiny self-contained Control that draws a pulsing dot synced to a per-frame
    /// computed phase derived from <see cref="Time.GetTicksMsec"/> and the supplied
    /// BPM. Does NOT touch <see cref="RhythmClock"/> — that stays attached to the
    /// live overworld BGM.
    /// </summary>
    private sealed partial class BpmPulse : Control
    {
        private readonly System.Func<float> _getBpm;
        public BpmPulse(System.Func<float> getBpm) { _getBpm = getBpm; }

        public override void _Draw()
        {
            float bpm = _getBpm();
            if (bpm <= 0f) return;

            double secondsPerBeat = 60.0 / bpm;
            double now            = Time.GetTicksMsec() / 1000.0;
            double phase          = (now / secondsPerBeat) % 1.0;
            float  pulse          = Mathf.Max(0f, 1f - (float)phase * 2f);

            var center = new Vector2(Size.X * 0.5f, Size.Y * 0.5f);
            float radius = 6f + 6f * pulse;
            var color = new Color(1f, 0.85f, 0.2f, 0.45f + 0.55f * pulse);
            DrawCircle(center, radius, color);
            // Faint outer ring for shape
            DrawArc(center, radius + 2f, 0f, Mathf.Tau, 24,
                    Colors.White with { A = 0.25f * pulse }, 1.5f);
        }
    }
}
