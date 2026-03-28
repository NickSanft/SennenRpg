namespace SennenRpg.Core.Data;

public sealed class PerformanceScore
{
    public int Perfects      { get; private set; }
    public int Goods         { get; private set; }
    public int Misses        { get; private set; }
    public int CurrentStreak { get; private set; }
    public int MaxStreak     { get; private set; }
    public int Total         => Perfects + Goods + Misses;

    /// <summary>Damage multiplier — reduces damage taken when on a streak.
    /// 0 streak = 1.0×, 10+ streak = 0.5× (minimum), linear 5% per hit.</summary>
    public float ComboMultiplier =>
        (float)System.Math.Clamp(1.0 - CurrentStreak * 0.05, 0.5, 1.0);

    public void Record(HitGrade grade)
    {
        switch (grade)
        {
            case HitGrade.Perfect:
                Perfects++;
                CurrentStreak++;
                break;
            case HitGrade.Good:
                Goods++;
                CurrentStreak++;
                break;
            default:
                Misses++;
                CurrentStreak = 0;
                break;
        }
        if (CurrentStreak > MaxStreak) MaxStreak = CurrentStreak;
    }

    public string GetRating()
    {
        if (Total == 0) return "—";
        float ratio = (Perfects * 2f + Goods) / (Total * 2f);
        return ratio >= 0.95f ? "S"
             : ratio >= 0.75f ? "A"
             : ratio >= 0.50f ? "B"
             : ratio >= 0.25f ? "C"
             : "D";
    }

    public string GetSummaryText() =>
        $"Rating: {GetRating()}  ♦ {Perfects} Perfect / {Goods} Good / {Misses} Miss  ♦ Max Combo: {MaxStreak}";
}
