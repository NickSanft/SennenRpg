using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// Godot Resource defining a cooking recipe: required ingredients, output item, and minigame difficulty.
/// </summary>
[GlobalClass]
public partial class RecipeData : Resource
{
    [Export] public string RecipeId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public Texture2D? Icon { get; set; }

    /// <summary>Resource paths of required ingredient ItemData .tres files.</summary>
    [Export] public string[] IngredientPaths { get; set; } = [];
    /// <summary>Count needed per ingredient, parallel to IngredientPaths.</summary>
    [Export] public int[] IngredientCounts { get; set; } = [];

    /// <summary>Resource path of the Normal-quality output ItemData .tres.</summary>
    [Export] public string OutputItemPath { get; set; } = "";

    /// <summary>Base heal amount of the output (before quality modifier).</summary>
    [Export] public int BaseHealAmount { get; set; } = 0;

    /// <summary>Number of rhythm notes in the cooking minigame (6-8 recommended).</summary>
    [Export] public int Difficulty { get; set; } = 6;

    /// <summary>Build the ingredient list as RecipeIngredient array for CookingLogic.</summary>
    public RecipeIngredient[] GetIngredients()
    {
        int len = Math.Min(IngredientPaths.Length, IngredientCounts.Length);
        var result = new RecipeIngredient[len];
        for (int i = 0; i < len; i++)
            result[i] = new RecipeIngredient(IngredientPaths[i], IngredientCounts[i]);
        return result;
    }
}
