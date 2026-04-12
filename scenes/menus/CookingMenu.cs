using Godot;
using System.Collections.Generic;
using System.Linq;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Cooking menu opened from the PauseMenu. Lists available recipes,
/// shows ingredient requirements, launches the cooking minigame,
/// and produces food items based on performance quality.
/// </summary>
public partial class CookingMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static Color Gold       => UiTheme.Gold;
    private static Color HaveGreen  => UiTheme.HaveGreen;
    private static Color NeedRed    => UiTheme.NeedRed;
    private static Color SubtleGrey => UiTheme.SubtleGrey;

    private static readonly string[] RecipePaths =
    [
        "res://resources/recipes/recipe_mystery_meat_sandwich.tres",
        "res://resources/recipes/recipe_ecto_cooler.tres",
    ];

    private RecipeData[]      _recipes = [];
    private RecipeData?       _activeRecipe;
    private CookingMinigame?  _minigame;
    private VBoxContainer     _recipeList = null!;
    private Label             _feedbackLabel = null!;
    private Control           _recipePanel = null!;

    public override void _Ready()
    {
        Layer   = 51;
        Visible = false;
    }

    public void Open()
    {
        LoadRecipes();
        BuildUI();
        UiTheme.ApplyPixelFontToAll(this);
        UiTheme.ApplyToAllButtons(this);
        Visible = true;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;
        if (_minigame is { Visible: true }) return; // don't close during minigame
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

    // ── Data loading ──────────────────────────────────────────────────

    private void LoadRecipes()
    {
        var list = new List<RecipeData>();
        foreach (var path in RecipePaths)
        {
            if (!ResourceLoader.Exists(path)) continue;
            var res = GD.Load<RecipeData>(path);
            if (res != null) list.Add(res);
        }
        _recipes = list.ToArray();
    }

    // ── UI building ───────────────────────────────────────────────────

    private void BuildUI()
    {
        // Clear previous UI
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();

        // Overlay
        var overlay = new ColorRect
        {
            Color = UiTheme.OverlayDim,
            AnchorRight = 1f, AnchorBottom = 1f,
        };
        AddChild(overlay);

        var centerer = new CenterContainer
        {
            AnchorRight = 1f, AnchorBottom = 1f,
        };
        AddChild(centerer);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(500f, 0f) };
        UiTheme.ApplyPanelTheme(panel);
        centerer.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        _recipePanel = new VBoxContainer();
        ((VBoxContainer)_recipePanel).AddThemeConstantOverride("separation", 6);
        margin.AddChild(_recipePanel);

        // Title
        var title = new Label
        {
            Text = "COOKING",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        _recipePanel.AddChild(title);
        _recipePanel.AddChild(new HSeparator());

        // Recipe rows
        _recipeList = new VBoxContainer();
        _recipeList.AddThemeConstantOverride("separation", 8);
        _recipePanel.AddChild(_recipeList);
        RefreshRecipeList();

        _recipePanel.AddChild(new HSeparator());

        // Feedback label
        _feedbackLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _feedbackLabel.AddThemeFontSizeOverride("font_size", 12);
        _recipePanel.AddChild(_feedbackLabel);

        // Minigame area (added here but hidden)
        _minigame = new CookingMinigame { Visible = false };
        _minigame.CookingCompleted += OnCookingCompleted;
        _recipePanel.AddChild(_minigame);

        // Hint
        var hint = new Label
        {
            Text = "[Esc] Back",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 18);
        _recipePanel.AddChild(hint);
    }

    private void RefreshRecipeList()
    {
        foreach (var child in _recipeList.GetChildren())
            child.QueueFree();

        var inv = GameManager.Instance.InventoryItemPaths;
        Button? firstCookable = null;

        foreach (var recipe in _recipes)
        {
            var row = new VBoxContainer();
            row.AddThemeConstantOverride("separation", 2);

            // Recipe name
            var nameLabel = new Label { Text = recipe.DisplayName };
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            nameLabel.AddThemeColorOverride("font_color", Gold);
            row.AddChild(nameLabel);

            // Ingredient list
            var ingredients = recipe.GetIngredients();
            var ingRow = new HBoxContainer();
            ingRow.AddThemeConstantOverride("separation", 12);
            bool canCook = true;

            foreach (var ing in ingredients)
            {
                int have = inv.Count(p => p == ing.ItemPath);
                bool enough = have >= ing.Count;
                if (!enough) canCook = false;

                string ingName = ing.ItemPath.GetFile().Replace(".tres", "")
                    .Replace("ingredient_", "").Replace("_", " ");
                // Capitalize first letter of each word
                ingName = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(ingName);

                var ingLabel = new Label
                {
                    Text = $"{ingName} {have}/{ing.Count}",
                    Modulate = enough ? HaveGreen : NeedRed,
                };
                ingLabel.AddThemeFontSizeOverride("font_size", 16);
                ingRow.AddChild(ingLabel);
            }
            row.AddChild(ingRow);

            // Cook button
            var btn = new Button
            {
                Text = "COOK",
                Disabled = !canCook,
                CustomMinimumSize = new Vector2(80f, 0f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            };
            var capturedRecipe = recipe;
            btn.Pressed += () => OnCookPressed(capturedRecipe);
            btn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
            row.AddChild(btn);

            if (canCook && firstCookable == null)
                firstCookable = btn;

            _recipeList.AddChild(row);
        }

        firstCookable?.CallDeferred(Control.MethodName.GrabFocus);
    }

    // ── Cooking flow ──────────────────────────────────────────────────

    private void OnCookPressed(RecipeData recipe)
    {
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);

        // Consume ingredients
        var ingredients = recipe.GetIngredients();
        foreach (var ing in ingredients)
        {
            for (int i = 0; i < ing.Count; i++)
                GameManager.Instance.RemoveItem(ing.ItemPath);
        }

        _activeRecipe = recipe;
        _recipeList.Visible = false;
        _feedbackLabel.Text = "Cooking...";

        _minigame!.Activate(recipe.Difficulty);
    }

    private void OnCookingCompleted(int perfects, int goods, int misses, int totalNotes)
    {
        bool hasBrewMaster = CharacterMilestoneLogic.HasTag(
            GameManager.Instance.Party.AllMembers, CharacterMilestone.LilyBrewMaster);
        var quality = CookingLogic.DetermineQuality(perfects, goods, totalNotes, hasBrewMaster);
        string outputPath = CookingLogic.QualityItemPath(_activeRecipe!.OutputItemPath, quality);

        // Fall back to normal if quality variant doesn't exist
        if (!ResourceLoader.Exists(outputPath))
            outputPath = _activeRecipe.OutputItemPath;

        GameManager.Instance.AddItem(outputPath);

        string qualityLabel = CookingLogic.QualityLabel(quality);
        Color qualityColor = quality switch
        {
            CookingQuality.Perfect => Gold,
            CookingQuality.Burnt   => NeedRed,
            _                      => Colors.White,
        };

        _feedbackLabel.Text = $"Cooked {_activeRecipe.DisplayName}! ({qualityLabel})";
        _feedbackLabel.Modulate = qualityColor;

        GD.Print($"[CookingMenu] Cooked {_activeRecipe.DisplayName}: {qualityLabel} " +
            $"(P:{perfects} G:{goods} M:{misses}/{totalNotes}) → {outputPath}");

        // Reset after a moment
        GetTree().CreateTimer(2.0f).Timeout += () =>
        {
            _feedbackLabel.Text = "";
            _feedbackLabel.Modulate = Colors.White;
            _recipeList.Visible = true;
            _activeRecipe = null;
            RefreshRecipeList();
        };
    }
}
