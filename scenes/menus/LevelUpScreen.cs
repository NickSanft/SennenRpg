using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Fire Emblem-style level-up screen. Call ShowAll() from BattleScene after AddExp.
/// Animates each stat one-by-one, flashing gold if it increased and dimming grey if not.
/// Returns when the player presses ui_accept or a 5-second timeout elapses.
///
/// All child nodes are built in code — no .tscn children needed.
/// CanvasLayer 70 (above pause menu at 50, below SceneTransition at 100).
/// </summary>
public partial class LevelUpScreen : CanvasLayer
{
    // ── Node references (built in _Ready) ─────────────────────────────────────
    private ColorRect  _overlay    = null!;
    private Control    _panel      = null!;
    private Label      _titleLabel = null!;
    private Label      _levelLabel = null!;

    // One row per stat: (container, nameLabel, valueLabel, deltaLabel)
    private record StatRow(Control Root, Label Name, Label Value, Label Delta);
    private StatRow[] _rows = null!;

    private static readonly (string Label, System.Func<LevelUpResult, int> Old,
                                            System.Func<LevelUpResult, int> New)[] StatDefs =
    {
        ("MAX HP",     r => r.OldMaxHp,      r => r.NewMaxHp),
        ("ATTACK",     r => r.OldAttack,     r => r.NewAttack),
        ("DEFENSE",    r => r.OldDefense,    r => r.NewDefense),
        ("SPEED",      r => r.OldSpeed,      r => r.NewSpeed),
        ("MAGIC",      r => r.OldMagic,      r => r.NewMagic),
        ("RESISTANCE", r => r.OldResistance, r => r.NewResistance),
        ("LUCK",       r => r.OldLuck,       r => r.NewLuck),
    };

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _canDismiss;
    private bool _dismissed;
    private TaskCompletionSource<bool>? _dismissTcs;

    private static readonly Color ColourGrey    = new(0.45f, 0.45f, 0.45f);
    private static readonly Color ColourWhite   = Colors.White;
    private static readonly Color ColourGold    = new(1.0f, 0.85f, 0.1f);
    private static readonly Color ColourGreen   = new(0.3f, 1.0f, 0.4f);

    // ── Setup ─────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        Layer   = 70;
        Visible = false;
        BuildUI();
    }

    private void BuildUI()
    {
        // Full-screen dark overlay
        _overlay = new ColorRect
        {
            Color          = new Color(0f, 0f, 0f, 0.80f),
            AnchorRight    = 1f,
            AnchorBottom   = 1f,
        };
        AddChild(_overlay);

        // Centred panel
        _panel = new Control();
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.CustomMinimumSize = new Vector2(340f, 0f);
        AddChild(_panel);

        // Background panel
        var bg = new ColorRect
        {
            Color          = new Color(0.07f, 0.07f, 0.12f, 1f),
            AnchorRight    = 1f,
            AnchorBottom   = 1f,
        };
        _panel.AddChild(bg);

        // Border (drawn by a second rect slightly inset)
        var border = new NinePatchRect
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        _panel.AddChild(border);

        // Content VBox
        var vbox = new VBoxContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
            OffsetLeft   = 16f,
            OffsetRight  = -16f,
            OffsetTop    = 12f,
            OffsetBottom = -12f,
        };
        _panel.AddChild(vbox);

        // Title
        _titleLabel = new Label
        {
            Text                = "★  LEVEL UP!  ★",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = ColourGold,
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(_titleLabel);

        // Level line
        _levelLabel = new Label
        {
            Text                = "Level 1 → Level 2",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = ColourWhite,
        };
        _levelLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(_levelLabel);

        vbox.AddChild(new HSeparator());

        // Stat rows
        _rows = new StatRow[StatDefs.Length];
        for (int i = 0; i < StatDefs.Length; i++)
        {
            var hbox = new HBoxContainer();

            var nameL = new Label
            {
                Text              = StatDefs[i].Label,
                CustomMinimumSize = new Vector2(110f, 0f),
                Modulate          = ColourGrey,
            };
            nameL.AddThemeFontSizeOverride("font_size", 12);

            var valL = new Label
            {
                Text                = "—",
                HorizontalAlignment = HorizontalAlignment.Right,
                CustomMinimumSize   = new Vector2(100f, 0f),
                Modulate            = ColourGrey,
            };
            valL.AddThemeFontSizeOverride("font_size", 12);

            var deltaL = new Label
            {
                Text                = "",
                HorizontalAlignment = HorizontalAlignment.Right,
                CustomMinimumSize   = new Vector2(40f, 0f),
                Modulate            = ColourGrey,
            };
            deltaL.AddThemeFontSizeOverride("font_size", 12);

            hbox.AddChild(nameL);
            hbox.AddChild(valL);
            hbox.AddChild(deltaL);
            vbox.AddChild(hbox);

            _rows[i] = new StatRow(hbox, nameL, valL, deltaL);
        }

        vbox.AddChild(new HSeparator());

        // Dismiss hint
        var hint = new Label
        {
            Name                = "DismissHint",
            Text                = "Press Z / Enter to continue",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = new Color(0.6f, 0.6f, 0.6f),
        };
        hint.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(hint);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Show the level-up animation for each result in sequence, then await player dismiss.
    /// Call from BattleScene after AddExp.
    /// </summary>
    public async Task ShowAll(List<LevelUpResult> levelUps)
    {
        _dismissed  = false;
        _canDismiss = false;
        _dismissTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        Visible = true;
        GetViewport().GuiReleaseFocus();

        foreach (var result in levelUps)
            await AnimateLevelUp(result);

        _canDismiss = true;
        UpdateHint("Press Z / Enter to continue");

        // Auto-dismiss after 5 s
        GetTree().CreateTimer(5.0).Timeout += () => _dismissTcs.TrySetResult(true);

        await _dismissTcs.Task;
        Visible = false;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent e)
    {
        if (!_canDismiss || !Visible) return;
        if (!e.IsActionPressed("ui_accept") && !e.IsActionPressed("interact")) return;
        GetViewport().SetInputAsHandled();
        Dismiss();
    }

    private void Dismiss()
    {
        if (_dismissed) return;
        _dismissed = true;
        _dismissTcs?.TrySetResult(true);
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private async Task AnimateLevelUp(LevelUpResult result)
    {
        // Update header
        _titleLabel.Text = "★  LEVEL UP!  ★";
        _levelLabel.Text = $"Level {result.NewLevel - 1} → Level {result.NewLevel}";
        _titleLabel.Modulate = ColourGold;

        // Reset all rows to dim grey (values filled in, just not yet revealed)
        for (int i = 0; i < StatDefs.Length; i++)
        {
            int oldVal = StatDefs[i].Old(result);
            _rows[i].Name.Modulate  = ColourGrey;
            _rows[i].Value.Text     = oldVal.ToString();
            _rows[i].Value.Modulate = ColourGrey;
            _rows[i].Delta.Text     = "";
        }

        UpdateHint("…");

        // Brief pause before the stat cascade
        await Wait(0.4f);

        // Animate each stat sequentially
        for (int i = 0; i < StatDefs.Length; i++)
        {
            int oldVal = StatDefs[i].Old(result);
            int newVal = StatDefs[i].New(result);
            bool gained = newVal > oldVal;

            // Flash the row
            FlashRow(i, gained);

            if (gained)
            {
                _rows[i].Value.Text     = $"{oldVal} → {newVal}";
                _rows[i].Delta.Text     = $"+{newVal - oldVal}";
                _rows[i].Delta.Modulate = ColourGreen;
                PlayStatSfx();
            }
            else
            {
                _rows[i].Value.Text  = oldVal.ToString();
                _rows[i].Delta.Text  = "—";
            }

            await Wait(0.28f);
        }

        // Hold on the final screen briefly before allowing dismiss
        PlayFanfareSfx();
        await Wait(0.4f);
    }

    private void FlashRow(int i, bool increased)
    {
        var row     = _rows[i];
        var endCol  = increased ? ColourWhite : ColourGrey;

        row.Name.Modulate  = ColourGold;
        row.Value.Modulate = ColourGold;
        row.Delta.Modulate = ColourGold;

        // Tween from gold → final colour
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(row.Name,  "modulate", endCol, 0.25f).SetDelay(0.05f);
        tween.TweenProperty(row.Value, "modulate", endCol, 0.25f).SetDelay(0.05f);
        if (increased)
            tween.TweenProperty(row.Delta, "modulate", ColourGreen, 0.25f).SetDelay(0.05f);
        else
            tween.TweenProperty(row.Delta, "modulate", ColourGrey,  0.25f).SetDelay(0.05f);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task Wait(float seconds)
        => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private void UpdateHint(string text)
    {
        var hint = _panel.FindChild("DismissHint", recursive: true) as Label;
        if (hint != null) hint.Text = text;
    }

    private static void PlayStatSfx()
    {
        const string path = "res://assets/audio/sfx/level_up_stat.ogg";
        if (ResourceLoader.Exists(path))
            AudioManager.Instance.PlaySfx(path);
    }

    private static void PlayFanfareSfx()
    {
        const string path = "res://assets/audio/sfx/level_up_fanfare.ogg";
        if (ResourceLoader.Exists(path))
            AudioManager.Instance.PlaySfx(path);
    }
}
