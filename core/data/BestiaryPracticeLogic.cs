namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static logic for Bestiary practice mode — grading, eligibility.
/// No Godot dependency, fully NUnit-testable.
/// </summary>
public static class BestiaryPracticeLogic
{
    /// <summary>Practice run grade.</summary>
    public enum PracticeRank { S, A, B, C, D }

    /// <summary>
    /// Returns true if the player can practice against this enemy
    /// (must have defeated it at least once).
    /// </summary>
    public static bool CanPractice(int killCount) => killCount >= 1;

    /// <summary>
    /// Grade a practice run based on hit counts.
    /// S = 95%+ Perfect rate, A = 80%+, B = 60%+, C = 40%+, D = below.
    /// Perfect rate = perfects / total. If total is 0, returns S (vacuous truth).
    /// </summary>
    public static PracticeRank GradeRun(int perfects, int goods, int misses)
    {
        int total = perfects + goods + misses;
        if (total == 0) return PracticeRank.S;

        float perfectRate = (float)perfects / total;
        if (perfectRate >= 0.95f) return PracticeRank.S;
        if (perfectRate >= 0.80f) return PracticeRank.A;
        if (perfectRate >= 0.60f) return PracticeRank.B;
        if (perfectRate >= 0.40f) return PracticeRank.C;
        return PracticeRank.D;
    }

    /// <summary>
    /// Returns the accuracy percentage (0-100) based on non-miss hits / total.
    /// </summary>
    public static float Accuracy(int perfects, int goods, int misses)
    {
        int total = perfects + goods + misses;
        if (total == 0) return 100f;
        return (float)(perfects + goods) / total * 100f;
    }

    /// <summary>
    /// Returns the perfect percentage (0-100).
    /// </summary>
    public static float PerfectRate(int perfects, int goods, int misses)
    {
        int total = perfects + goods + misses;
        if (total == 0) return 100f;
        return (float)perfects / total * 100f;
    }
}
