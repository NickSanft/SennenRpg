using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Class-change menu opened by the ClassChangeNpc.
/// Shows all four classes with level, stat preview, and earned cross-class bonuses.
/// Code-built UI following the StatsMenu pattern. Layer 52.
/// </summary>
public partial class ClassChangeMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static readonly Color Gold       = new(1.0f, 0.85f, 0.1f);
    private static readonly Color SubtleGrey = new(0.55f, 0.55f, 0.55f);
    private static readonly Color ActiveGreen = new(0.3f, 0.9f, 0.4f);
    private static readonly Color BgColour   = new(0.07f, 0.07f, 0.12f, 1f);

    private readonly List<Button> _classButtons = new();
    private Label _infoLabel = null!;
    private Label _bonusLabel = null!;
    private VBoxContainer _outer = null!;

    public override void _Ready()
    {
        Layer   = 52;
        Visible = false;
    }

    public void Open()
    {
        BuildUI();
        Visible = true;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;
        if (e.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
    }

    private void Close()
    {
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    private void BuildUI()
    {
        // Clear previous UI if reopened
        foreach (var child in GetChildren())
        {
            if (child is Node n) n.QueueFree();
        }
        _classButtons.Clear();

        // Full-screen dim overlay
        var overlay = new ColorRect
        {
            Color        = new Color(0f, 0f, 0f, 0.75f),
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(overlay);

        var centerer = new CenterContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(centerer);

        var panelContainer = new PanelContainer
        {
            CustomMinimumSize = new Vector2(400f, 0f),
        };
        var style = new StyleBoxFlat
        {
            BgColor          = BgColour,
            BorderWidthLeft   = 1, BorderWidthRight  = 1,
            BorderWidthTop    = 1, BorderWidthBottom = 1,
            BorderColor       = new Color(0.25f, 0.25f, 0.35f),
            CornerRadiusTopLeft    = 4, CornerRadiusTopRight    = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
        };
        panelContainer.AddThemeStyleboxOverride("panel", style);
        centerer.AddChild(panelContainer);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   16);
        margin.AddThemeConstantOverride("margin_right",  16);
        margin.AddThemeConstantOverride("margin_top",    12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panelContainer.AddChild(margin);

        _outer = new VBoxContainer();
        _outer.AddThemeConstantOverride("separation", 6);
        margin.AddChild(_outer);

        // Title
        var title = new Label
        {
            Text                = "CHANGE CLASS",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        _outer.AddChild(title);

        _outer.AddChild(new HSeparator());

        var gm = GameManager.Instance;
        var activeClass = gm.ActiveClass;
        var entries = gm.ClassEntries;

        // Class buttons
        Button? firstFocusable = null;
        foreach (PlayerClass cls in System.Enum.GetValues<PlayerClass>())
        {
            int level = entries.TryGetValue(cls, out var entry) ? entry.Level : 0;
            bool isActive = cls == activeClass;

            string labelText = level > 0
                ? $"{cls}   Lv {level}"
                : $"{cls}   (New)";

            var btn = new Button
            {
                Text     = isActive ? $"> {labelText} <" : $"  {labelText}",
                Disabled = isActive,
                CustomMinimumSize = new Vector2(0f, 32f),
            };

            if (isActive)
                btn.Modulate = ActiveGreen;

            btn.AddThemeFontSizeOverride("font_size", 13);

            var capturedClass = cls;
            btn.Pressed += () => OnClassSelected(capturedClass);

            _outer.AddChild(btn);
            _classButtons.Add(btn);

            if (!isActive && firstFocusable == null)
                firstFocusable = btn;
        }

        _outer.AddChild(new HSeparator());

        // Info label — shows stat preview for focused class
        _infoLabel = new Label
        {
            Text = "Select a class to preview stats.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _infoLabel.AddThemeFontSizeOverride("font_size", 10);
        _outer.AddChild(_infoLabel);

        _outer.AddChild(new HSeparator());

        // Cross-class bonuses
        var bonusTitle = new Label
        {
            Text                = "Cross-Class Bonuses",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        bonusTitle.AddThemeFontSizeOverride("font_size", 12);
        _outer.AddChild(bonusTitle);

        _bonusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _bonusLabel.AddThemeFontSizeOverride("font_size", 10);
        _outer.AddChild(_bonusLabel);
        RefreshBonusList();

        _outer.AddChild(new HSeparator());

        var hint = new Label
        {
            Text                = "[Esc] Cancel",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 9);
        _outer.AddChild(hint);

        // Connect focus signals for stat preview
        for (int i = 0; i < _classButtons.Count; i++)
        {
            var capturedIdx = i;
            _classButtons[i].FocusEntered += () => OnClassFocused(capturedIdx);
        }

        // Grab focus on first available button
        if (firstFocusable != null)
            firstFocusable.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void OnClassFocused(int idx)
    {
        var cls = System.Enum.GetValues<PlayerClass>()[idx];
        var gm = GameManager.Instance;

        if (gm.ClassEntries.TryGetValue(cls, out var entry))
        {
            _infoLabel.Text =
                $"{cls} — Level {entry.Level}\n" +
                $"HP:{entry.MaxHp}  ATK:{entry.Attack}  DEF:{entry.Defense}\n" +
                $"SPD:{entry.Speed}  MAG:{entry.Magic}  RES:{entry.Resistance}\n" +
                $"LCK:{entry.Luck}  MP:{entry.MaxMp}";
        }
        else
        {
            // First time — load starting stats from class template
            string path = $"res://resources/characters/class_{cls.ToString().ToLower()}.tres";
            if (ResourceLoader.Exists(path))
            {
                var template = GD.Load<CharacterStats>(path);
                _infoLabel.Text =
                    $"{cls} — New (Level 1)\n" +
                    $"HP:{template.MaxHp}  ATK:{template.Attack}  DEF:{template.Defense}\n" +
                    $"SPD:{template.Speed}  MAG:{template.Magic}  RES:{template.Resistance}\n" +
                    $"LCK:{template.Luck}  MP:{template.MaxMp}";
            }
            else
            {
                _infoLabel.Text = $"{cls} — New (Level 1)";
            }
        }
    }

    private void OnClassSelected(PlayerClass cls)
    {
        GameManager.Instance.SwitchClass(cls);
        GD.Print($"[ClassChangeMenu] Switched to {cls}.");
        Close();
    }

    private void RefreshBonusList()
    {
        var gm = GameManager.Instance;
        var classLevels = gm.ClassEntries.ToDictionary(kv => kv.Key, kv => kv.Value.Level);

        var lines = new List<string>();
        foreach (var bonus in CrossClassBonusRegistry.All)
        {
            bool earned = classLevels.TryGetValue(bonus.SourceClass, out int lv) && lv >= bonus.RequiredLevel;
            string mark = earned ? "[Y]" : "[ ]";
            lines.Add($"{mark} {bonus.Description}");
        }

        _bonusLabel.Text = lines.Count > 0 ? string.Join("\n", lines) : "None yet.";
    }
}
