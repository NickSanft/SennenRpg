using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Cooking Journal menu — lists all recipes with discovery status and best quality achieved.
/// CanvasLayer 52. Opened from PauseMenu or CookingMenu.
/// </summary>
public partial class CookingJournalMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static Color Gold       => UiTheme.Gold;
    private static Color SubtleGrey => UiTheme.SubtleGrey;
    private static Color HaveGreen  => UiTheme.HaveGreen;
    private static Color NeedRed    => UiTheme.NeedRed;

    /// <summary>All known recipe resource paths — must match CookingMenu.RecipePaths.</summary>
    private static readonly string[] RecipePaths =
    [
        "res://resources/recipes/recipe_mystery_meat_sandwich.tres",
        "res://resources/recipes/recipe_ecto_cooler.tres",
    ];

    private Dictionary<string, string> _journal = new();
    private readonly List<Button> _entryButtons = new();

    public override void _Ready()
    {
        Layer       = 52;
        Visible     = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// Open the journal with the current cooking journal data.
    /// </summary>
    public void Open(Dictionary<string, string> journal)
    {
        _journal = journal ?? new Dictionary<string, string>();
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

    private void BuildUi()
    {
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();
        _entryButtons.Clear();

        // Overlay dim
        var overlay = new ColorRect
        {
            Color        = UiTheme.OverlayDim,
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(overlay);

        // Centerer
        var centerer = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(centerer);

        // Panel
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(520f, 400f) };
        UiTheme.ApplyPanelTheme(panel);
        centerer.AddChild(panel);

        // Margin
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   16);
        margin.AddThemeConstantOverride("margin_right",  16);
        margin.AddThemeConstantOverride("margin_top",    12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        // Root VBox
        var rootVbox = new VBoxContainer();
        rootVbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(rootVbox);

        // Title
        var title = new Label
        {
            Text                = "RECIPES",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        rootVbox.AddChild(title);
        rootVbox.AddChild(new HSeparator());

        // Scrollable recipe list
        var scroll = new ScrollContainer
        {
            CustomMinimumSize    = new Vector2(0f, 260f),
            SizeFlagsVertical    = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        rootVbox.AddChild(scroll);

        var listVbox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        listVbox.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(listVbox);

        // Load and list recipes
        int totalRecipes = RecipePaths.Length;
        int discoveredCount = 0;
        int perfectCount = 0;

        foreach (var path in RecipePaths)
        {
            RecipeData? recipe = null;
            if (ResourceLoader.Exists(path))
                recipe = GD.Load<RecipeData>(path);

            string recipeId = recipe?.RecipeId ?? path;
            string displayName = recipe?.DisplayName ?? "";

            var (name, badge, discovered) = CookingJournalLogic.GetDisplayInfo(
                recipeId, displayName, _journal);

            if (discovered) discoveredCount++;
            if (badge == "[Perfect]") perfectCount++;

            // Row container
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            if (discovered)
            {
                // Recipe name
                var nameLabel = new Label
                {
                    Text                = name,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                };
                nameLabel.AddThemeFontSizeOverride("font_size", 12);
                row.AddChild(nameLabel);

                // Quality badge with color
                Color badgeColor = badge switch
                {
                    "[Perfect]" => Gold,
                    "[Normal]"  => Colors.White,
                    "[Burnt]"   => NeedRed,
                    _           => SubtleGrey,
                };
                var badgeLabel = new Label
                {
                    Text     = badge,
                    Modulate = badgeColor,
                };
                badgeLabel.AddThemeFontSizeOverride("font_size", 12);
                row.AddChild(badgeLabel);

                // Ingredient list
                if (recipe != null)
                {
                    string ingredients = BuildIngredientText(recipe);
                    var ingLabel = new Label
                    {
                        Text     = ingredients,
                        Modulate = SubtleGrey,
                    };
                    ingLabel.AddThemeFontSizeOverride("font_size", 10);
                    row.AddChild(ingLabel);
                }
            }
            else
            {
                // Undiscovered: ??? in grey
                var unknownLabel = new Label
                {
                    Text                = "???",
                    Modulate            = SubtleGrey,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                };
                unknownLabel.AddThemeFontSizeOverride("font_size", 12);
                row.AddChild(unknownLabel);

                // Ingredient count hint
                int ingCount = recipe?.IngredientPaths?.Length ?? 0;
                if (ingCount > 0)
                {
                    var hintLabel = new Label
                    {
                        Text     = $"{ingCount} ingredient{(ingCount == 1 ? "" : "s")}",
                        Modulate = SubtleGrey,
                    };
                    hintLabel.AddThemeFontSizeOverride("font_size", 10);
                    row.AddChild(hintLabel);
                }
            }

            // Wrap in a focusable button for keyboard nav
            var btn = new Button
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            btn.AddThemeFontSizeOverride("font_size", 12);
            // Use a panel-style button: remove text, add the row as a child
            btn.Text = "";
            btn.AddChild(row);
            btn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
            listVbox.AddChild(btn);
            _entryButtons.Add(btn);
        }

        rootVbox.AddChild(new HSeparator());

        // Stats footer
        var (burnt, normal, perfect) = CookingJournalLogic.CountByQuality(_journal);
        var statsLabel = new Label
        {
            Text                = $"{discoveredCount}/{totalRecipes} Discovered | {perfectCount} Perfect",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        statsLabel.AddThemeFontSizeOverride("font_size", 11);
        rootVbox.AddChild(statsLabel);

        // Hint
        var hint = new Label
        {
            Text                = "[Esc] Back",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 12);
        rootVbox.AddChild(hint);
    }

    private static string BuildIngredientText(RecipeData recipe)
    {
        var parts = new List<string>();
        int len = System.Math.Min(
            recipe.IngredientPaths?.Length ?? 0,
            recipe.IngredientCounts?.Length ?? 0);

        for (int i = 0; i < len; i++)
        {
            string path = recipe.IngredientPaths![i];
            int count = recipe.IngredientCounts![i];
            string itemName = ItemDisplayName(path);
            parts.Add(count > 1 ? $"{itemName} x{count}" : itemName);
        }
        return string.Join(", ", parts);
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
}
