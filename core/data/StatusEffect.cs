namespace SennenRpg.Core.Data;

/// <summary>
/// Battle status effects that can be applied to the player or enemy.
/// </summary>
public enum StatusEffect
{
    /// <summary>Deals HP damage at the start of each turn.</summary>
    Poison,
    /// <summary>Forces the affected party to skip their next action.</summary>
    Stun,
    /// <summary>Reduces incoming physical damage for the duration.</summary>
    Shield,
    /// <summary>Prevents the affected party from using magic/spells.</summary>
    Silence,
}
