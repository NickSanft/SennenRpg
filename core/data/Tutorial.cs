namespace SennenRpg.Core.Data;

/// <summary>
/// A single tutorial entry — shown as a modal popup the first time
/// the player encounters the associated mechanic.
/// </summary>
/// <param name="Id">Stable lookup key, e.g. "battle_basics". Never rename — saves reference it.</param>
/// <param name="Title">Popup title text.</param>
/// <param name="Body">Popup body text. Supports <c>\n</c> for line breaks.</param>
/// <param name="Category">Grouping category (display only).</param>
public readonly record struct Tutorial(
    string Id,
    string Title,
    string Body,
    TutorialCategory Category);
