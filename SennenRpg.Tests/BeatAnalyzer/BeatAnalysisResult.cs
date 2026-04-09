namespace SennenRpg.Tests.BeatAnalyzer;

/// <summary>
/// Result of running the offline beat analyzer over a single audio file.
/// </summary>
public sealed record BeatAnalysisResult(
    float Bpm,
    float FirstBeatSec,
    float Confidence,
    int   BeatCount);
