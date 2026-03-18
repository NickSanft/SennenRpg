using Godot;

namespace SennenRpg.Core.Data;

public enum HitGrade { Perfect, Good, Miss }

/// <summary>Shared timing constants used by all rhythm systems.</summary>
public static class RhythmConstants
{
    public const float DefaultBpm      = 180f;
    public const int   BeatsPerMeasure = 4;

    /// <summary>Maximum deviation (seconds) for a Perfect grade.</summary>
    public const float PerfectWindowSec = 0.050f;
    /// <summary>Maximum deviation (seconds) for a Good grade.</summary>
    public const float GoodWindowSec    = 0.120f;

    public static float BeatInterval(float bpm) => 60f / bpm;

    /// <summary>Grade a timing deviation in seconds against the Perfect/Good windows.</summary>
    public static HitGrade GradeDeviation(float deviationSec)
    {
        float abs = Mathf.Abs(deviationSec);
        if (abs <= PerfectWindowSec) return HitGrade.Perfect;
        if (abs <= GoodWindowSec)    return HitGrade.Good;
        return HitGrade.Miss;
    }

    /// <summary>Grade multiplier to apply to attack damage based on strike grade.</summary>
    public static float GradeMultiplier(HitGrade grade) => grade switch
    {
        HitGrade.Perfect => 1.5f,
        HitGrade.Good    => 1.0f,
        _                => 0.5f,
    };
}
