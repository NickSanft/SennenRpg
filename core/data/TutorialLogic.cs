using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-logic helpers for the tutorial system. No Godot dependencies —
/// fully NUnit-testable.
/// </summary>
public static class TutorialLogic
{
    /// <summary>
    /// Returns <c>true</c> when the tutorial identified by <paramref name="tutorialId"/>
    /// should be presented to the player right now.
    /// <para>
    /// Returns <c>false</c> when: skip is enabled, the ID is already seen,
    /// or the ID is unknown (defensive — silently no-op on typos).
    /// </para>
    /// </summary>
    public static bool ShouldShow(
        string tutorialId,
        bool skipEnabled,
        IReadOnlySet<string> seenIds)
    {
        if (string.IsNullOrEmpty(tutorialId)) return false;
        if (skipEnabled) return false;
        if (seenIds.Contains(tutorialId)) return false;
        if (Find(tutorialId) == null) return false;
        return true;
    }

    /// <summary>
    /// Returns the <see cref="Tutorial"/> with the given ID, or <c>null</c> if
    /// no such entry is registered. Safe from typos.
    /// </summary>
    public static Tutorial? Find(string tutorialId)
    {
        if (string.IsNullOrEmpty(tutorialId)) return null;
        foreach (var t in TutorialRegistry.All)
        {
            if (string.Equals(t.Id, tutorialId, System.StringComparison.Ordinal))
                return t;
        }
        return null;
    }

    /// <summary>
    /// Validates the registry for shipping-quality invariants: no duplicate IDs,
    /// no empty fields. Returns <c>true</c> when valid, <c>false</c> when errors
    /// were collected into <paramref name="errors"/>.
    /// </summary>
    public static bool ValidateRegistry(IEnumerable<Tutorial> all, out List<string> errors)
    {
        errors = new List<string>();
        var seen = new HashSet<string>();

        foreach (var t in all)
        {
            if (string.IsNullOrWhiteSpace(t.Id))
                errors.Add("Tutorial has empty Id.");
            else if (!seen.Add(t.Id))
                errors.Add($"Duplicate Tutorial Id: '{t.Id}'.");

            if (string.IsNullOrWhiteSpace(t.Title))
                errors.Add($"Tutorial '{t.Id}' has empty Title.");
            if (string.IsNullOrWhiteSpace(t.Body))
                errors.Add($"Tutorial '{t.Id}' has empty Body.");
        }

        return errors.Count == 0;
    }
}
