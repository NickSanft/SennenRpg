namespace SennenRpg.Core.Data;

public static class EncoreLogic
{
    public const float EncoreDamageMultiplier = 1.5f;

    /// <summary>
    /// Check if a rhythm phase was flawless (all perfect, >0 notes).
    /// </summary>
    public static bool IsFlawless(int perfects, int goods, int misses)
        => perfects > 0 && goods == 0 && misses == 0;

    /// <summary>
    /// Check if an encore can be granted (flawless + not already used this round).
    /// </summary>
    public static bool CanGrantEncore(bool isFlawless, bool encoreAlreadyUsedThisRound)
        => isFlawless && !encoreAlreadyUsedThisRound;

    /// <summary>
    /// Apply encore bonus to damage.
    /// </summary>
    public static int ApplyEncoreBonus(int baseDamage)
        => (int)(baseDamage * EncoreDamageMultiplier);
}
