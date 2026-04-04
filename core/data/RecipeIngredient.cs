namespace SennenRpg.Core.Data;

/// <summary>One ingredient requirement: item resource path + count needed.</summary>
public readonly record struct RecipeIngredient(string ItemPath, int Count);
