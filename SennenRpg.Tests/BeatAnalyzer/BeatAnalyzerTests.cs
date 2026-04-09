using System;
using NUnit.Framework;

namespace SennenRpg.Tests.BeatAnalyzer;

/// <summary>
/// Synthetic-signal tests for the offline beat analyzer. These run in CI on
/// every push and assert the algorithm holds within tolerance bands. They do
/// NOT touch any real WAV files in the project.
/// </summary>
[TestFixture]
public sealed class BeatAnalyzerTests
{
    private const int SampleRate = 22050;

    /// <summary>
    /// Generate a percussive click track at the given BPM with optional lead-in
    /// silence. Each "click" is a 100 ms exponentially-decaying broadband burst —
    /// long enough to survive the STFT Hann window and span multiple analysis
    /// frames the way a real kick drum does.
    /// </summary>
    private static float[] MakeClickTrack(float bpm, float durationSec, float leadInSec = 0f)
    {
        int totalSamples = (int)(durationSec * SampleRate);
        var buf = new float[totalSamples];
        float beatPeriod = 60f / bpm;

        int clickLen = (int)(0.100f * SampleRate); // 100 ms
        var rng = new Random(12345);

        for (float t = leadInSec; t < durationSec; t += beatPeriod)
        {
            int start = (int)(t * SampleRate);
            for (int i = 0; i < clickLen && start + i < totalSamples; i++)
            {
                // Exponential decay × broadband noise — looks like a percussion hit
                float decay = (float)Math.Exp(-5.0 * i / clickLen);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                buf[start + i] += decay * noise * 0.8f;
            }
        }
        return buf;
    }

    /// <summary>
    /// Returns how far the detected offset is from the expected offset, MODULO
    /// one beat period. RhythmClock uses (audioPos - offset) % period internally,
    /// so an offset that's off by an integer number of periods is functionally
    /// identical. The metric we actually care about is phase error within one
    /// period, i.e., min(err mod period, period - (err mod period)).
    /// </summary>
    private static float PhaseError(float detected, float expected, float bpm)
    {
        float period = 60f / bpm;
        float diff   = detected - expected;
        float mod    = ((diff % period) + period) % period;
        return Math.Min(mod, period - mod);
    }

    [Test]
    public void Detects120BpmCleanClickTrack()
    {
        // Real game music never has a transient at sample 0 — STFT framing
        // can't detect it (frame 0 has no predecessor for spectral flux), so
        // we use a 50 ms intro silence to mirror realistic conditions.
        var samples = MakeClickTrack(120f, durationSec: 10f, leadInSec: 0.05f);
        var result  = BeatAnalyzer.Analyze(samples, SampleRate);

        Assert.That(result.Bpm,        Is.EqualTo(120f).Within(1.0f), "BPM");
        Assert.That(PhaseError(result.FirstBeatSec, 0.05f, result.Bpm),
            Is.LessThan(0.080f), "first-beat phase alignment (modulo period)");
        Assert.That(result.Confidence, Is.GreaterThan(BeatAnalyzer.ConfidenceFloor), "confidence");
    }

    [Test]
    public void Detects140BpmWithLeadInSilence()
    {
        var samples = MakeClickTrack(140f, durationSec: 10f, leadInSec: 0.30f);
        var result  = BeatAnalyzer.Analyze(samples, SampleRate);

        Assert.That(result.Bpm, Is.EqualTo(140f).Within(1.0f), "BPM");
        Assert.That(PhaseError(result.FirstBeatSec, 0.30f, result.Bpm),
            Is.LessThan(0.080f), "first-beat phase alignment (modulo period)");
    }

    [Test]
    public void PureSineHasLowConfidence()
    {
        // 5 seconds of 440 Hz sine — no transients, no beat
        int n = SampleRate * 5;
        var samples = new float[n];
        for (int i = 0; i < n; i++)
            samples[i] = (float)Math.Sin(2 * Math.PI * 440 * i / SampleRate) * 0.5f;

        var result = BeatAnalyzer.Analyze(samples, SampleRate);
        // Either confidence is low or BPM is junk — we just assert it doesn't crash and
        // confidence is not "clean track" levels.
        Assert.That(result.Confidence, Is.LessThan(0.95f),
            "Pure sine should not register as a high-confidence beat track.");
    }

    [Test]
    public void EmptyOrTinyBufferReturnsZero()
    {
        var result = BeatAnalyzer.Analyze(new float[100], SampleRate);
        Assert.That(result.Bpm, Is.EqualTo(0f));
        Assert.That(result.Confidence, Is.EqualTo(0f));
    }

    [Test]
    public void NullBufferReturnsZero()
    {
        var result = BeatAnalyzer.Analyze(null!, SampleRate);
        Assert.That(result.Bpm, Is.EqualTo(0f));
    }

    [Test]
    public void FasterTempoIsPreferredOnAmbiguousTrack()
    {
        // 160 BPM clicks. Without the faster-bias tiebreaker the analyzer can latch
        // onto 80 BPM (every other click). Assert we get the faster value.
        var samples = MakeClickTrack(160f, durationSec: 10f, leadInSec: 0f);
        var result  = BeatAnalyzer.Analyze(samples, SampleRate);

        // Should be 160 ± 2 (allow tiny autocorrelation drift), NOT 80.
        Assert.That(result.Bpm, Is.EqualTo(160f).Within(2.0f),
            "Faster-bias tiebreaker should resolve to 160, not 80.");
    }
}
