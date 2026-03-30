using System;
using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure battle status-effect logic. No Godot runtime dependency — fully unit-testable via NUnit.
/// All methods are stateless helpers that accept the status dictionary directly.
/// </summary>
public static class StatusLogic
{
    /// <summary>Poison damage per turn: 10% of max HP, minimum 1.</summary>
    public static int PoisonDamage(int maxHp) => Math.Max(1, maxHp / 10);

    /// <summary>Decrements a turn counter, returning the new value (minimum 0).</summary>
    public static int Tick(int turnsRemaining) => Math.Max(0, turnsRemaining - 1);

    /// <summary>Returns true when <paramref name="effect"/> is active (turns remaining > 0).</summary>
    public static bool HasStatus(Dictionary<StatusEffect, int> statuses, StatusEffect effect)
        => statuses.TryGetValue(effect, out int t) && t > 0;

    /// <summary>
    /// Decrements all active statuses by one turn, removing any that have expired.
    /// </summary>
    public static void TickAll(Dictionary<StatusEffect, int> statuses)
    {
        var keys = new List<StatusEffect>(statuses.Keys);
        foreach (var k in keys)
        {
            statuses[k] = Tick(statuses[k]);
            if (statuses[k] <= 0)
                statuses.Remove(k);
        }
    }

    /// <summary>
    /// Applies a status effect, overwriting any existing duration with the new one.
    /// </summary>
    public static void Apply(Dictionary<StatusEffect, int> statuses, StatusEffect effect, int turns)
    {
        if (turns > 0)
            statuses[effect] = turns;
    }

    /// <summary>Short display text for status icons shown in the HUD.</summary>
    public static string IconText(StatusEffect effect) => effect switch
    {
        StatusEffect.Poison  => "PSN",
        StatusEffect.Stun    => "STN",
        StatusEffect.Shield  => "SHD",
        StatusEffect.Silence => "SIL",
        _                    => "???"
    };

    /// <summary>
    /// Parses a status signal argument of the form "{effect}:{turns}" (e.g. "poison:3").
    /// Returns false when the argument is malformed or turns is not a positive integer.
    /// </summary>
    public static bool TryParseStatusSignal(string arg, out StatusEffect effect, out int turns)
    {
        effect = StatusEffect.Poison;
        turns  = 0;

        int colon = arg.IndexOf(':');
        if (colon < 0) return false;

        string name = arg.Substring(0, colon);
        if (!Enum.TryParse(name, ignoreCase: true, out effect)) return false;
        if (!int.TryParse(arg.Substring(colon + 1), out turns)) return false;
        return turns > 0;
    }
}
