using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SennenRpg.Tests.BeatAnalyzer;

/// <summary>
/// Serialises analyzer results to <c>assets/music/beat_data.json</c>.
/// Schema is intentionally tiny so the runtime loader can stay simple.
/// </summary>
public static class BeatDataJsonWriter
{
    public static string Serialize(IDictionary<string, BeatAnalysisResult> entries)
    {
        var dict = new Dictionary<string, BeatEntryDto>();
        foreach (var kv in entries)
        {
            dict[kv.Key] = new BeatEntryDto(
                kv.Value.Bpm,
                kv.Value.FirstBeatSec,
                kv.Value.Confidence,
                kv.Value.BeatCount);
        }

        var opts = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(dict, opts);
    }

    public static void Write(IDictionary<string, BeatAnalysisResult> entries, string outPath)
    {
        File.WriteAllText(outPath, Serialize(entries));
    }

    public sealed record BeatEntryDto(
        float bpm,
        float first_beat_sec,
        float confidence,
        int   beat_count);
}
