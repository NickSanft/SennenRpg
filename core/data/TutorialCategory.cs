namespace SennenRpg.Core.Data;

/// <summary>
/// Groups tutorials for display/filtering. Currently display-only —
/// trigger order is determined by runtime events, not category.
/// </summary>
public enum TutorialCategory
{
    Overworld,
    Battle,
    Rhythm,
    Menus,
    Cooking,
    Foraging,
    Party,
    Advanced,
}
