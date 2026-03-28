namespace SennenRpg.Core.Data;

/// <summary>
/// Pure battle math with no Godot runtime dependency — fully unit-testable via NUnit.
/// All methods are stateless helpers; randomness is injected so tests stay deterministic.
/// </summary>
public static class BattleFormulas
{
    /// <summary>
    /// Physical damage: grade multiplier applied to attack, minus defense. Minimum 1.
    /// </summary>
    public static int PhysicalDamage(int attack, int defense, float gradeMult)
        => Math.Max(1, (int)Math.Round(attack * gradeMult) - defense);

    /// <summary>
    /// Magic damage: spell base power scaled by caster magic, minus target resistance. Minimum 1.
    /// </summary>
    public static int MagicDamage(int spellBasePower, int magic, int resistance, float gradeMult)
        => Math.Max(1, (int)Math.Round(spellBasePower * (magic / 10f) * gradeMult) - resistance);

    /// <summary>
    /// Returns true if a critical hit triggers.
    /// A Perfect grade always crits. Other grades crit when <paramref name="randomValue"/>
    /// is below <c>luck / 200f</c> (Luck 0 = never, Luck 200 = always).
    /// Pass <see cref="Godot.GD.Randf()"/> for <paramref name="randomValue"/> in production;
    /// pass a fixed value in tests.
    /// </summary>
    public static bool IsCrit(bool isPerfectGrade, int luck, float randomValue)
        => isPerfectGrade || randomValue < luck / 200f;

    /// <summary>
    /// Returns true if the player acts before the enemy.
    /// Equal Speed is a tie in the player's favour.
    /// </summary>
    public static bool PlayerGoesFirst(int playerSpeed, int enemySpeed)
        => playerSpeed >= enemySpeed;
}
