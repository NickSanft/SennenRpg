namespace SennenRpg.Core.Data;

public sealed class PerformanceScore
{
    public int Perfects { get; private set; }
    public int Goods    { get; private set; }
    public int Misses   { get; private set; }
    public int Total    => Perfects + Goods + Misses;

    public void Record(HitGrade grade)
    {
        switch (grade)
        {
            case HitGrade.Perfect: Perfects++; break;
            case HitGrade.Good:    Goods++;    break;
            default:               Misses++;   break;
        }
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
        $"Rating: {GetRating()}  ♦ {Perfects} Perfect / {Goods} Good / {Misses} Miss";
}
