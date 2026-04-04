using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Overworld spells menu opened from the PauseMenu.
/// Shows known spells with MP cost and description.
/// Overworld-usable spells (like Teleport Home) can be cast directly.
/// </summary>
public partial class SpellsMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static Color Gold       => UiTheme.Gold;
    private static Color MpBlue     => UiTheme.MpBlue;
    private static Color SubtleGrey => UiTheme.SubtleGrey;

    private VBoxContainer _spellList = null!;
    private Label         _descLabel = null!;
    private Label         _feedbackLabel = null!;

    public override void _Ready()
    {
        Layer   = 51;
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
        if (!e.IsActionPressed("ui_cancel")) return;
        GetViewport().SetInputAsHandled();
        AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    private void BuildUI()
    {
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();

        var overlay = new ColorRect
        {
            Color = UiTheme.OverlayDim,
            AnchorRight = 1f, AnchorBottom = 1f,
        };
        AddChild(overlay);

        var centerer = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(centerer);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(380f, 0f) };
        UiTheme.ApplyPanelTheme(panel);
        centerer.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 6);
        margin.AddChild(outer);

        var title = new Label
        {
            Text = "SPELLS",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        outer.AddChild(title);
        outer.AddChild(new HSeparator());

        _spellList = new VBoxContainer();
        _spellList.AddThemeConstantOverride("separation", 6);
        outer.AddChild(_spellList);

        outer.AddChild(new HSeparator());

        _descLabel = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _descLabel.AddThemeFontSizeOverride("font_size", 10);
        outer.AddChild(_descLabel);

        _feedbackLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _feedbackLabel.AddThemeFontSizeOverride("font_size", 11);
        outer.AddChild(_feedbackLabel);

        var hint = new Label
        {
            Text = "[Esc] Back",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 9);
        outer.AddChild(hint);

        RefreshSpellList();
    }

    private void RefreshSpellList()
    {
        foreach (var child in _spellList.GetChildren())
            child.QueueFree();

        var gm = GameManager.Instance;
        var paths = gm.KnownSpellPaths;
        Button? firstBtn = null;

        foreach (var path in paths)
        {
            if (!ResourceLoader.Exists(path)) continue;
            var spell = GD.Load<SpellData>(path);
            if (spell == null) continue;

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var nameLabel = new Label { Text = spell.DisplayName };
            nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            row.AddChild(nameLabel);

            var mpLabel = new Label
            {
                Text = $"{spell.MpCost} MP",
                Modulate = MpBlue,
            };
            mpLabel.AddThemeFontSizeOverride("font_size", 10);
            row.AddChild(mpLabel);

            if (spell.OverworldUsable)
            {
                bool canAfford = gm.PlayerStats.CurrentMp >= spell.MpCost;
                var castBtn = new Button
                {
                    Text = "CAST",
                    Disabled = !canAfford,
                };
                var capturedSpell = spell;
                var capturedPath = path;
                castBtn.Pressed += () => OnCastSpell(capturedSpell);
                castBtn.FocusEntered += () =>
                {
                    _descLabel.Text = spell.Description;
                    AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
                };
                row.AddChild(castBtn);
                if (canAfford && firstBtn == null) firstBtn = castBtn;
            }
            else
            {
                var infoLabel = new Label
                {
                    Text = "Battle only",
                    Modulate = SubtleGrey,
                };
                infoLabel.AddThemeFontSizeOverride("font_size", 9);
                row.AddChild(infoLabel);
            }

            _spellList.AddChild(row);
        }

        if (paths.Count == 0)
        {
            var empty = new Label
            {
                Text = "No spells known.",
                Modulate = SubtleGrey,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            empty.AddThemeFontSizeOverride("font_size", 11);
            _spellList.AddChild(empty);
        }

        firstBtn?.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void OnCastSpell(SpellData spell)
    {
        var gm = GameManager.Instance;
        if (!gm.UseMp(spell.MpCost))
        {
            _feedbackLabel.Text = "Not enough MP!";
            _feedbackLabel.Modulate = new Color(1f, 0.3f, 0.3f);
            AudioManager.Instance?.PlaySfx(UiSfx.Error);
            return;
        }

        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);

        if (!string.IsNullOrEmpty(spell.OverworldTargetScene))
        {
            // Hide ALL menus immediately so the dissolve is visible
            Visible = false;
            EmitSignal(SignalName.Closed);
            // Also hide the PauseMenu that will reappear from our Closed signal
            GetTree().CallGroup("pause_menus", "hide");
            foreach (var node in GetTree().GetNodesInGroup("pause_menus"))
                if (node is CanvasLayer cl) cl.Visible = false;
            // Brute-force: hide any sibling PauseMenu
            if (GetParent() != null)
                foreach (var sibling in GetParent().GetChildren())
                    if (sibling is CanvasLayer cl && cl.Name.ToString().Contains("Pause"))
                        cl.Visible = false;

            gm.SetState(GameState.Overworld);
            gm.TeleportArriving = true;
            _ = PlayTeleportDissolveAndTransition(spell.OverworldTargetScene);
        }
    }

    private async System.Threading.Tasks.Task PlayTeleportDissolveAndTransition(string targetScene)
    {
        // Find the player sprite and apply dissolve shader
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        var sprite = player?.GetNodeOrNull<AnimatedSprite2D>("Sprite");

        if (sprite != null)
        {
            const string shaderPath = "res://assets/shaders/dissolve_vertical.gdshader";
            if (ResourceLoader.Exists(shaderPath))
            {
                var mat = new ShaderMaterial { Shader = GD.Load<Shader>(shaderPath) };
                mat.SetShaderParameter("progress", 0.0f);
                sprite.Material = mat;

                // Dissolve out: bottom to top over 1.2s (slow and dramatic)
                var tween = CreateTween();
                tween.TweenMethod(
                    Callable.From<float>(v => mat.SetShaderParameter("progress", v)),
                    0.0f, 1.0f, 1.2f)
                    .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
                await ToSignal(tween, Tween.SignalName.Finished);

                // Brief hold after dissolve completes
                await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);
            }
        }

        await SceneTransition.Instance.GoToAsync(targetScene, TransitionType.PixelMosaic);
    }
}
