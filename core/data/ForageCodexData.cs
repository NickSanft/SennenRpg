using System;
using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Mutable container for the Foragery codex.
/// Owned by <c>GameManager</c>; serialised through <c>SaveData.ForageCodex</c>.
///
/// All mutation funnels through <see cref="Record"/> so the dictionary stays consistent
/// with the pure logic in <see cref="ForageCodexLogic"/>.
/// </summary>
public class ForageCodexData
{
    /// <summary>Item resource path → codex entry.</summary>
    public Dictionary<string, ForageCodexEntry> Entries { get; private set; } = new();

    /// <summary>True when this is the first time the player has ever foraged the given item.</summary>
    public bool IsNewDiscovery(string itemPath) => !Entries.ContainsKey(itemPath);

    /// <summary>
    /// Record a forage of <paramref name="itemPath"/> at the given grade and timestamp.
    /// Returns true when this call unlocked a new entry (used to fire the discovery chime).
    /// </summary>
    public bool Record(string itemPath, ForageLogic.ForageGrade grade, DateTime nowUtc)
    {
        bool isNew = !Entries.TryGetValue(itemPath, out var existing);
        Entries[itemPath] = ForageCodexLogic.RecordFind(existing, grade, nowUtc);
        return isNew;
    }

    /// <summary>Replace all entries (used by save-load).</summary>
    public void ReplaceAll(IDictionary<string, ForageCodexEntry> entries)
    {
        Entries = new Dictionary<string, ForageCodexEntry>(entries);
    }

    /// <summary>Discard all codex progress (used on new game).</summary>
    public void Reset() => Entries = new Dictionary<string, ForageCodexEntry>();
}
