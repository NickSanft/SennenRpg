using System;
using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Mutable container for the Bestiary record set. Owned by <c>GameManager</c>;
/// serialised through <c>SaveData.Bestiary</c>. Mirrors <see cref="ForageCodexData"/>
/// in structure so the same patterns apply.
/// </summary>
public class BestiaryData
{
    /// <summary>EnemyId → kill record.</summary>
    public Dictionary<string, BestiaryEntry> Entries { get; private set; } = new();

    /// <summary>True when this enemy has never been defeated before.</summary>
    public bool IsNewDiscovery(string enemyId) => !Entries.ContainsKey(enemyId);

    /// <summary>
    /// Records a kill of <paramref name="enemyId"/> at <paramref name="nowUtc"/>.
    /// Returns true when this call unlocked a brand-new entry — used to fire the
    /// "New entry!" toast and to play the discovery chime.
    /// </summary>
    public bool Record(string enemyId, DateTime nowUtc)
    {
        bool isNew = !Entries.TryGetValue(enemyId, out var existing);
        Entries[enemyId] = BestiaryRecordLogic.RecordKill(existing, nowUtc);
        return isNew;
    }

    /// <summary>Replace all entries (used on save load).</summary>
    public void ReplaceAll(IDictionary<string, BestiaryEntry> entries)
    {
        Entries = new Dictionary<string, BestiaryEntry>(entries);
    }

    /// <summary>Discard all bestiary progress (used on new game).</summary>
    public void Reset() => Entries = new Dictionary<string, BestiaryEntry>();
}
