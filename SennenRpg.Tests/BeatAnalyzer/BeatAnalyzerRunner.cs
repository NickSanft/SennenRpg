using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace SennenRpg.Tests.BeatAnalyzer;

/// <summary>
/// On-demand runners that walk the real <c>assets/music/</c> folder. Marked
/// <see cref="ExplicitAttribute"/> so CI never invokes them — run with:
/// <code>
/// dotnet test --filter FullyQualifiedName~BeatAnalyzerRunner
/// </code>
/// or pick a single fixture in the IDE.
/// </summary>
[TestFixture]
[Explicit("Runs over real asset files. Manual trigger only.")]
public sealed class BeatAnalyzerRunner
{
    private static string FindRepoRoot()
    {
        // The test DLL lives at <repo>/SennenRpg.Tests/bin/<cfg>/<tfm>/.
        // Walk up until we find the assets/music folder.
        var dir = TestContext.CurrentContext.TestDirectory;
        for (int i = 0; i < 8 && dir != null; i++)
        {
            string candidate = Path.Combine(dir, "assets", "music");
            if (Directory.Exists(candidate))
                return dir;
            dir = Directory.GetParent(dir)?.FullName ?? "";
        }
        Assert.Fail("Could not locate repo root containing assets/music/.");
        return "";
    }

    [Test]
    public void Analyze_AllTracks()
    {
        string repo  = FindRepoRoot();
        string music = Path.Combine(repo, "assets", "music");
        var wavFiles = Directory.GetFiles(music, "*.wav", SearchOption.TopDirectoryOnly);

        var entries = new Dictionary<string, BeatAnalysisResult>();
        foreach (var path in wavFiles)
        {
            string fileName = Path.GetFileName(path);
            string resPath  = $"res://assets/music/{fileName}";
            BeatAnalysisResult result;
            try
            {
                result = BeatAnalyzer.AnalyzeFile(path);
            }
            catch (System.Exception ex)
            {
                TestContext.Out.WriteLine($"  SKIP  {fileName}  ({ex.GetType().Name}: {ex.Message})");
                continue;
            }

            // Skip low-confidence tracks — they fall back to free-running at runtime
            if (result.Confidence < BeatAnalyzer.ConfidenceFloor)
            {
                TestContext.Out.WriteLine(
                    $"  LOW   {fileName,-50}  bpm={result.Bpm,6:F1}  off={result.FirstBeatSec,5:F3}s  conf={result.Confidence:F2}  (omitted)");
                continue;
            }

            entries[resPath] = result;
            TestContext.Out.WriteLine(
                $"  OK    {fileName,-50}  bpm={result.Bpm,6:F1}  off={result.FirstBeatSec,5:F3}s  conf={result.Confidence:F2}");
        }

        string outPath = Path.Combine(music, "beat_data.json");
        BeatDataJsonWriter.Write(entries, outPath);
        TestContext.Out.WriteLine($"\nWrote {entries.Count} entries to {outPath}");
    }

    [Test]
    public void Verify_AgainstMusicMetadata()
    {
        // Hard-coded BPM values from MusicMetadata.cs (snapshot — keep in sync manually).
        var manual = new Dictionary<string, float>
        {
            ["Carillion Forest.wav"]                       = 108f,
            ["Mellyr Outpost.wav"]                         = 72f,
            ["Drifting in the Astral Paring (Ambient).wav"] = 148f,
            ["Drifting in the Astral Paring.wav"]          = 148f,
            ["Outpacing.wav"]                              = 128f,
            ["Corruption Can Be Fun.wav"]                  = 180f,
            ["Origins Of The Gyre.wav"]                    = 148f,
            ["Sozitek.wav"]                                = 108f,
            ["Slow Broil.wav"]                             = 128f,
            ["Melancholy Conspectus.wav"]                  = 140f,
            ["Foggy Morning in Flas.wav"]                  = 78f,
            ["A Calm Rain in Argyre.wav"]                  = 70f,
            ["Glimmersong Forest.wav"]                     = 88f,
        };

        string repo  = FindRepoRoot();
        string music = Path.Combine(repo, "assets", "music");

        TestContext.Out.WriteLine($"\n  {"track",-50}  {"manual",-7}  {"detected",-9}  {"offset",-7}  {"conf"}");
        TestContext.Out.WriteLine(new string('-', 90));

        foreach (var kv in manual)
        {
            string path = Path.Combine(music, kv.Key);
            if (!File.Exists(path))
            {
                TestContext.Out.WriteLine($"  {kv.Key,-50}  {kv.Value,-7:F0}  MISSING");
                continue;
            }

            BeatAnalysisResult r;
            try { r = BeatAnalyzer.AnalyzeFile(path); }
            catch (System.Exception ex)
            {
                TestContext.Out.WriteLine($"  {kv.Key,-50}  {kv.Value,-7:F0}  ERROR ({ex.GetType().Name})");
                continue;
            }

            string flag = System.Math.Abs(r.Bpm - kv.Value) < 2f ? " " : "*";
            TestContext.Out.WriteLine(
                $"{flag} {kv.Key,-50}  {kv.Value,-7:F0}  {r.Bpm,-9:F1}  {r.FirstBeatSec,-7:F3}  {r.Confidence:F2}");
        }
    }
}
