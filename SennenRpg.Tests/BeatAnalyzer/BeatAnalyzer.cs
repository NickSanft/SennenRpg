using System;
using System.IO;
using NWaves.Audio;
using NWaves.Signals;
using NWaves.Transforms;

namespace SennenRpg.Tests.BeatAnalyzer;

/// <summary>
/// Offline tempo + first-downbeat estimator. Pure logic — fully testable
/// from synthetic signals, no Godot runtime required.
///
/// Algorithm:
///   1. Read WAV (16-bit PCM expected) and mix to mono.
///   2. Build a spectrogram via NWaves Stft (frame=2048, hop=512).
///   3. Spectral flux envelope (sum of positive bin deltas frame-to-frame).
///   4. Smooth and rectify.
///   5. Autocorrelate the envelope; pick the strongest lag in the
///      <see cref="MinBpm"/>..<see cref="MaxBpm"/> range — that's the BPM.
///   6. When two strong peaks exist at lag and lag/2 or lag*2 (within
///      <see cref="HalfDoubleTolerance"/>), prefer the FASTER tempo
///      (closer to <see cref="PreferBpm"/>) per project preference.
///   7. With BPM in hand, slide a beat-period comb across the first
///      ~8 seconds of envelope; offset that maximises the comb sum is
///      <c>FirstBeatSec</c>.
///   8. Confidence = peak height / mean autocorrelation in search range.
/// </summary>
public static class BeatAnalyzer
{
    public const int    FrameSize           = 2048;
    public const int    HopSize             = 512;
    public const float  MinBpm              = 60f;
    public const float  MaxBpm              = 200f;
    public const float  PreferBpm           = 140f;   // tiebreaker target — favour faster
    public const float  HalfDoubleTolerance = 0.15f;  // peaks within 15% are "comparable"
    public const float  ConfidenceFloor     = 0.4f;   // below this, treat as "no clear pulse"

    /// <summary>Run the analyzer over a WAV file on disk.</summary>
    public static BeatAnalysisResult AnalyzeFile(string wavPath)
    {
        DiscreteSignal mono;
        using (var fs = File.OpenRead(wavPath))
        {
            var wav = new WaveFile(fs);
            mono = wav[Channels.Average];
        }
        return Analyze(mono.Samples, mono.SamplingRate);
    }

    /// <summary>Run the analyzer over an in-memory mono PCM buffer.</summary>
    public static BeatAnalysisResult Analyze(float[] samples, int sampleRate)
    {
        if (samples == null || samples.Length < FrameSize * 2)
            return new BeatAnalysisResult(0f, 0f, 0f, 0);

        // ── 1) Spectrogram ────────────────────────────────────────────────────
        var stft = new Stft(FrameSize, HopSize);
        var spectrogram = stft.Spectrogram(samples);   // List<float[]> magnitudes per frame
        int frameCount  = spectrogram.Count;
        if (frameCount < 8) return new BeatAnalysisResult(0f, 0f, 0f, 0);

        float hopSeconds = (float)HopSize / sampleRate;

        // ── 2) Spectral flux envelope ────────────────────────────────────────
        var flux = new float[frameCount];
        for (int f = 1; f < frameCount; f++)
        {
            var prev = spectrogram[f - 1];
            var cur  = spectrogram[f];
            int bins = Math.Min(prev.Length, cur.Length);
            float sum = 0f;
            for (int b = 0; b < bins; b++)
            {
                float diff = cur[b] - prev[b];
                if (diff > 0f) sum += diff;
            }
            flux[f] = sum;
        }

        // ── 3) Smooth (3-frame moving average) and remove DC ─────────────────
        var smoothed = new float[frameCount];
        for (int f = 0; f < frameCount; f++)
        {
            float a = f > 0           ? flux[f - 1] : 0f;
            float b = flux[f];
            float c = f < frameCount - 1 ? flux[f + 1] : 0f;
            smoothed[f] = (a + b + c) / 3f;
        }
        float mean = 0f;
        for (int i = 0; i < smoothed.Length; i++) mean += smoothed[i];
        mean /= smoothed.Length;
        for (int i = 0; i < smoothed.Length; i++)
        {
            float v = smoothed[i] - mean;
            smoothed[i] = v > 0f ? v : 0f;
        }

        // ── 4) Autocorrelation in BPM search range ───────────────────────────
        int minLag = (int)Math.Floor(60f / (MaxBpm * hopSeconds));
        int maxLag = (int)Math.Ceiling(60f / (MinBpm * hopSeconds));
        if (maxLag >= frameCount) maxLag = frameCount - 1;
        if (minLag < 1)            minLag = 1;
        if (maxLag <= minLag)
            return new BeatAnalysisResult(0f, 0f, 0f, 0);

        var ac = new float[maxLag + 1];
        for (int lag = minLag; lag <= maxLag; lag++)
        {
            float s = 0f;
            int n = frameCount - lag;
            for (int i = 0; i < n; i++) s += smoothed[i] * smoothed[i + lag];
            ac[lag] = s;
        }

        // Pick the highest peak in the search range
        int bestLag = minLag;
        float bestVal = ac[minLag];
        for (int lag = minLag + 1; lag <= maxLag; lag++)
        {
            if (ac[lag] > bestVal)
            {
                bestVal = ac[lag];
                bestLag = lag;
            }
        }

        // ── 5) Half / double tiebreaker (favour FASTER per user pref) ────────
        int chosenLag = bestLag;
        int halfLag   = bestLag / 2;
        int doubleLag = bestLag * 2;
        if (halfLag >= minLag && halfLag <= maxLag &&
            ac[halfLag] >= bestVal * (1f - HalfDoubleTolerance))
        {
            // Half-lag means DOUBLE the BPM — faster. Prefer if closer to PreferBpm.
            float fastBpm = 60f / (halfLag * hopSeconds);
            float slowBpm = 60f / (bestLag * hopSeconds);
            if (Math.Abs(fastBpm - PreferBpm) <= Math.Abs(slowBpm - PreferBpm))
                chosenLag = halfLag;
        }
        if (doubleLag >= minLag && doubleLag <= maxLag &&
            ac[doubleLag] >= bestVal * (1f - HalfDoubleTolerance))
        {
            // Double-lag means HALF the BPM — slower. Only prefer if MUCH closer to PreferBpm.
            float slowBpm = 60f / (doubleLag * hopSeconds);
            float curBpm  = 60f / (chosenLag * hopSeconds);
            if (Math.Abs(slowBpm - PreferBpm) + 10f < Math.Abs(curBpm - PreferBpm))
                chosenLag = doubleLag;
        }

        // Parabolic interpolation around the chosen integer lag for sub-bin
        // accuracy. With hop=512 @ 22050 Hz one lag step is ≈3 BPM near 120,
        // which is too coarse without this refinement.
        float refinedLag = chosenLag;
        if (chosenLag > minLag && chosenLag < maxLag)
        {
            float yLeft  = ac[chosenLag - 1];
            float yMid   = ac[chosenLag];
            float yRight = ac[chosenLag + 1];
            float denom  = yLeft - 2f * yMid + yRight;
            if (Math.Abs(denom) > 1e-9f)
            {
                float delta = 0.5f * (yLeft - yRight) / denom;
                if (delta > -1f && delta < 1f) refinedLag = chosenLag + delta;
            }
        }

        float bpm = 60f / (refinedLag * hopSeconds);

        // ── 6) Confidence = peak / mean(autocorrelation in search range) ────
        float acMean = 0f;
        int acCount = 0;
        for (int lag = minLag; lag <= maxLag; lag++) { acMean += ac[lag]; acCount++; }
        acMean = acCount > 0 ? acMean / acCount : 1f;
        float confidence = acMean > 0f ? bestVal / acMean / 4f : 0f;
        // Empirically the peak/mean ratio sits around 4 for clean tracks; divide so
        // ≈1.0 = clean, ≈0.4 = noisy. Clamp to [0,1].
        if (confidence > 1f) confidence = 1f;
        if (confidence < 0f) confidence = 0f;

        // ── 7) First-downbeat offset via comb correlation ────────────────────
        // Use FLOAT-precision beat period from the refined BPM so the comb taps
        // don't drift off the click train after a handful of beats.
        float beatPeriodFrames = (float)(60.0 / bpm / hopSeconds);
        float firstBeatSec = 0f;
        if (beatPeriodFrames >= 1f)
        {
            int searchFrames = Math.Min(frameCount, (int)Math.Ceiling(beatPeriodFrames));
            int combTaps     = Math.Min(8, (int)(frameCount / beatPeriodFrames));
            float bestComb   = -1f;
            int   bestOffset = 0;
            for (int o = 0; o < searchFrames; o++)
            {
                float s = 0f;
                for (int t = 0; t < combTaps; t++)
                {
                    int idx = (int)Math.Round(o + t * beatPeriodFrames);
                    if (idx >= frameCount) break;
                    s += smoothed[idx];
                }
                if (s > bestComb)
                {
                    bestComb   = s;
                    bestOffset = o;
                }
            }
            firstBeatSec = bestOffset * hopSeconds;
        }

        // ── 8) Result ─────────────────────────────────────────────────────────
        float durationSec = (float)samples.Length / sampleRate;
        int   beatCount   = (int)Math.Floor(durationSec * bpm / 60f);

        return new BeatAnalysisResult(bpm, firstBeatSec, confidence, beatCount);
    }
}
