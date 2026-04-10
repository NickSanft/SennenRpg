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
///   3. BASS-emphasis spectral flux (only sum bins below ~250 Hz). Real music
///      has tempo information dominated by kick/snare onsets — restricting
///      to the low end suppresses melodic onsets that confuse the picker.
///   4. Smooth, rectify, and de-mean.
///   5. Autocorrelate the envelope across an extended lag range so we can
///      look up harmonics (k * lag) of every candidate beat period.
///   6. Score each candidate lag with HARMONIC SUM × tempo prior:
///        score(L) = (sum_k ac[k*L] / k) × prior(60 / (L*hop))
///      where prior is a log-Gaussian centred on <see cref="PreferBpm"/>.
///      Harmonic sum disambiguates 3:2 / dotted-note errors that the old
///      single-peak picker fell into; the prior keeps the answer in the
///      musically plausible 60-200 BPM range.
///   7. Parabolic interpolation around the chosen lag for sub-frame accuracy.
///   8. With BPM in hand, slide a beat-period comb across the first beat
///      period of envelope; offset that maximises the comb sum is
///      <c>FirstBeatSec</c>.
///   9. Confidence = peak score / mean score across the search range.
/// </summary>
public static class BeatAnalyzer
{
    public const int    FrameSize       = 2048;
    public const int    HopSize         = 512;
    public const float  MinBpm          = 60f;
    public const float  MaxBpm          = 200f;

    /// <summary>Centre of the log-Gaussian tempo prior.</summary>
    public const float  PreferBpm       = 120f;

    /// <summary>Width of the prior in log2-octaves. 0.8 keeps 60-240 BPM
    /// reasonably weighted while crushing implausible extremes.</summary>
    public const float  PriorSigma      = 0.8f;

    /// <summary>Number of harmonics summed when scoring a candidate lag.
    /// 6 covers ~1.5 bars of 4/4 — gives fast-tempo candidates enough
    /// "votes" to outscore their dotted-quarter (2/3) impostors when the
    /// underlying song has clean periodicity at every beat.</summary>
    public const int    HarmonicsK      = 6;

    /// <summary>Upper cutoff for spectral flux. Includes kick (≤250 Hz),
    /// snare body (≤500 Hz), and the snare/clap "snap" up to ~1 kHz. Going
    /// wider than this starts pulling in melodic onsets that fight the beat.</summary>
    public const float  BassCutoffHz    = 1000f;

    public const float  ConfidenceFloor = 0.4f;

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

        // ── 2) Log-magnitude bass-emphasis spectral flux envelope ────────────
        // - Restrict to bins below BassCutoffHz so kick/snare drives the picker.
        // - Take log(1+|X|) before differencing: this is the standard onset
        //   detection trick (Klapuri / Bello). It compresses the dynamic range
        //   so a loud bridge doesn't drown out a quieter intro and weak hits
        //   become visible. Without this, fast tracks with soft snare layers
        //   (e.g. "Corruption Can Be Fun") get their 180-BPM grid washed out
        //   and the picker locks onto a slower 120-BPM "feel" tempo instead.
        // Bin width = sampleRate / FrameSize.
        int bassBinHi = Math.Max(4, (int)Math.Round(BassCutoffHz * FrameSize / sampleRate));
        var flux = new float[frameCount];
        for (int f = 1; f < frameCount; f++)
        {
            var prev = spectrogram[f - 1];
            var cur  = spectrogram[f];
            int bins = Math.Min(prev.Length, cur.Length);
            int hi   = Math.Min(bassBinHi, bins);
            float sum = 0f;
            for (int b = 1; b < hi; b++) // skip DC
            {
                float curLog  = MathF.Log(1f + cur[b]);
                float prevLog = MathF.Log(1f + prev[b]);
                float diff    = curLog - prevLog;
                if (diff > 0f) sum += diff;
            }
            flux[f] = sum;
        }

        // ── 3) Smooth (3-frame moving average) and remove DC ─────────────────
        var smoothed = new float[frameCount];
        for (int f = 0; f < frameCount; f++)
        {
            float a = f > 0               ? flux[f - 1] : 0f;
            float b = flux[f];
            float c = f < frameCount - 1  ? flux[f + 1] : 0f;
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

        // ── 4) Autocorrelation across extended lag range ─────────────────────
        // We need ac[k*lag] for k = 1..HarmonicsK for every candidate lag in
        // [minLag..maxLag], so the AC array must reach HarmonicsK * maxLag.
        int minLag = (int)Math.Floor(60f / (MaxBpm * hopSeconds));
        int maxLag = (int)Math.Ceiling(60f / (MinBpm * hopSeconds));
        if (minLag < 1) minLag = 1;
        if (maxLag >= frameCount) maxLag = frameCount - 1;
        if (maxLag <= minLag)
            return new BeatAnalysisResult(0f, 0f, 0f, 0);

        int maxLagAc = Math.Min(frameCount - 1, maxLag * HarmonicsK);

        var ac = new float[maxLagAc + 1];
        for (int lag = minLag; lag <= maxLagAc; lag++)
        {
            float s = 0f;
            int n = frameCount - lag;
            for (int i = 0; i < n; i++) s += smoothed[i] * smoothed[i + lag];
            ac[lag] = s;
        }

        // ── 5) Harmonic-sum × tempo-prior scoring ────────────────────────────
        // We track BOTH the raw harmonic sum and the prior-weighted score so
        // the dotted-error post-check can compare candidates on raw evidence
        // (without the prior penalising the faster reading just because it
        // lives further from PreferBpm).
        var score   = new double[maxLag + 1];
        var rawHarm = new double[maxLag + 1];
        double bestScore = -1.0;
        int bestLag = minLag;
        for (int lag = minLag; lag <= maxLag; lag++)
        {
            float bpmLag  = 60f / (lag * hopSeconds);
            double octaves = Math.Log2(bpmLag / PreferBpm);
            double prior   = Math.Exp(-0.5 * (octaves * octaves) / (PriorSigma * PriorSigma));

            double harmSum = 0.0;
            for (int k = 1; k <= HarmonicsK; k++)
            {
                int idx = k * lag;
                if (idx > maxLagAc) break;
                harmSum += ac[idx] / k;   // 1/k weighting (Klapuri-style)
            }

            rawHarm[lag] = harmSum;
            double s = harmSum * prior;
            score[lag] = s;
            if (s > bestScore)
            {
                bestScore = s;
                bestLag   = lag;
            }
        }

        // ── 5b) Dotted-quarter error correction ──────────────────────────────
        // Common failure mode: tracks with a syncopated layer (chord stabs,
        // pad accents) every dotted quarter produce a strong autocorrelation
        // peak at 1.5× the true beat period. The harmonic-sum picker can
        // prefer the slower 2/3-tempo reading because the prior pulls it
        // toward the 120-BPM region. Diagnostic case: "Corruption Can Be Fun"
        // — true 180 BPM, picker landed on 120 BPM with raw harmonic 90004
        // vs the true tempo's 75021 (83% of winner) — the prior penalty at
        // 178 BPM (0.775) was the deciding factor.
        //
        // Strategy: explicitly check the 2/3-lag candidate (= 1.5× BPM)
        // and compare RAW harmonic sums (not prior-adjusted). If the faster
        // candidate's raw evidence is at least 80% as strong AND it lands
        // in 90-200 BPM, switch. We compare on raw evidence so a faster
        // tempo isn't disqualified just for being further from PreferBpm.
        //
        // Carillion Forest (true 108) is unaffected because its 108-BPM
        // harmonic stack dominates the 162-BPM candidate by a wide margin
        // (Carillion's 162 raw harmonic is well below 80% of the 108 raw).
        int fastLag = (int)Math.Round(bestLag * 2.0 / 3.0);
        if (fastLag >= minLag && fastLag <= maxLag && fastLag != bestLag)
        {
            double bestRaw  = rawHarm[bestLag];
            double fastRaw  = rawHarm[fastLag];
            float  fastBpm  = 60f / (fastLag * hopSeconds);
            if (fastRaw >= bestRaw * 0.80 && fastBpm >= 90f && fastBpm <= 200f)
            {
                bestLag   = fastLag;
                bestScore = score[fastLag];
            }
        }

        // ── 6) Parabolic interpolation on the SCORE function ─────────────────
        // Refine sub-bin position around the chosen lag. Using the score
        // function (not raw ac) means the refinement respects the harmonic
        // structure that picked the lag in the first place.
        float refinedLag = bestLag;
        if (bestLag > minLag && bestLag < maxLag)
        {
            double yLeft  = score[bestLag - 1];
            double yMid   = score[bestLag];
            double yRight = score[bestLag + 1];
            double denom  = yLeft - 2.0 * yMid + yRight;
            if (Math.Abs(denom) > 1e-9)
            {
                double delta = 0.5 * (yLeft - yRight) / denom;
                if (delta > -1.0 && delta < 1.0) refinedLag = (float)(bestLag + delta);
            }
        }

        float bpm = 60f / (refinedLag * hopSeconds);

        // ── 7) Confidence = peak score / mean score across search range ──────
        double scoreMean = 0.0;
        int scoreCount = 0;
        for (int lag = minLag; lag <= maxLag; lag++) { scoreMean += score[lag]; scoreCount++; }
        scoreMean = scoreCount > 0 ? scoreMean / scoreCount : 1.0;
        float confidence = scoreMean > 0.0 ? (float)(bestScore / scoreMean / 4.0) : 0f;
        // Empirically the peak/mean ratio sits around 4 for clean tracks; divide so
        // ≈1.0 = clean, ≈0.4 = noisy. Clamp to [0,1].
        if (confidence > 1f) confidence = 1f;
        if (confidence < 0f) confidence = 0f;

        // ── 8) First-downbeat offset via comb correlation ────────────────────
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

        // ── 9) Result ─────────────────────────────────────────────────────────
        // Round BPM to the nearest integer — the project's source tracks are
        // all authored at whole-number tempos, and rounding suppresses the
        // sub-bin parabolic refinement noise (e.g. 147.8 → 148, 86.6 → 87).
        float bpmRounded  = (float)Math.Round(bpm);
        float durationSec = (float)samples.Length / sampleRate;
        int   beatCount   = (int)Math.Floor(durationSec * bpmRounded / 60f);

        return new BeatAnalysisResult(bpmRounded, firstBeatSec, confidence, beatCount);
    }
}
