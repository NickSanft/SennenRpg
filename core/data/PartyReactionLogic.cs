using System;
using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-logic helper for the overworld party reaction system.
/// No Godot dependencies — fully NUnit-testable.
/// </summary>
public static class PartyReactionLogic
{
    /// <summary>
    /// Returns the highest-priority unshown reaction for the current map and active party,
    /// or <c>null</c> if nothing is available.
    /// </summary>
    public static PartyReaction? GetNextReaction(
        string mapId,
        IReadOnlyList<string> activeMemberIds,
        IReadOnlySet<string> shownKeys)
    {
        PartyReaction? best = null;
        int bestPriority = -1;

        foreach (var r in PartyReactionRegistry.All)
        {
            if (!string.Equals(r.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                continue;

            bool memberActive = false;
            for (int i = 0; i < activeMemberIds.Count; i++)
            {
                if (string.Equals(activeMemberIds[i], r.MemberId, StringComparison.OrdinalIgnoreCase))
                {
                    memberActive = true;
                    break;
                }
            }
            if (!memberActive) continue;

            string key = ReactionKey(r);
            if (shownKeys.Contains(key)) continue;

            if (r.Priority > bestPriority)
            {
                best = r;
                bestPriority = r.Priority;
            }
        }

        return best;
    }

    /// <summary>
    /// Deterministic unique key for a reaction: <c>"{MemberId}:{MapId}:{TextHash}"</c>.
    /// </summary>
    public static string ReactionKey(PartyReaction r)
        => $"{r.MemberId}:{r.MapId}:{r.Text.GetHashCode()}";

    /// <summary>
    /// Returns <c>true</c> when enough steps have elapsed since the last reaction check.
    /// </summary>
    public static bool ShouldCheckReactions(int stepsSinceLastCheck, int interval = 15)
        => stepsSinceLastCheck >= interval;
}
