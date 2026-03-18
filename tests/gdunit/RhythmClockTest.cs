using GdUnit4;
using static GdUnit4.Assertions;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.GdUnit;

/// <summary>
/// GdUnit4 tests for RhythmClock autoload.
/// Beat-signal tests require the engine loop to be running so _Process fires.
/// </summary>
[TestSuite]
public sealed class RhythmClockTest
{
    // Ensure the clock is stopped between tests so state is clean.
    [After]
    [RequireGodotRuntime]
    public void TearDown() => RhythmClock.Instance?.Stop();

    // ── Presence ─────────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void Instance_IsNotNull()
        => AssertThat(RhythmClock.Instance).IsNotNull();

    // ── BPM / interval ───────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void StartFreeRunning_SetsBeatInterval()
    {
        RhythmClock.Instance.StartFreeRunning(180f);
        float expected = RhythmConstants.BeatInterval(180f);
        AssertFloat(RhythmClock.Instance.BeatInterval).IsEqualApprox(expected, 0.0001f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SetBpm_UpdatesBeatInterval()
    {
        RhythmClock.Instance.SetBpm(120f);
        AssertFloat(RhythmClock.Instance.BeatInterval).IsEqualApprox(0.5f, 0.0001f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Stop_DoesNotThrow()
    {
        RhythmClock.Instance.StartFreeRunning(180f);
        RhythmClock.Instance.Stop(); // no exception
        AssertThat(true).IsTrue();   // if we reached here it passed
    }

    // ── Beat signal ───────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task StartFreeRunning_EmitsBeatSignal_Within2Beats()
    {
        // At 180 BPM the first beat fires after ~333ms.
        // We wait up to 2 seconds to be safe against frame-rate variance.
        RhythmClock.Instance.StartFreeRunning(180f);

        await AssertSignal(RhythmClock.Instance)
            .IsEmitted("Beat")
            .WithTimeout(2000);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task StartFreeRunning_EmitsMultipleBeats()
    {
        // At 60 BPM a beat fires every second; wait 3 s → should see ≥ 2 beats.
        RhythmClock.Instance.StartFreeRunning(60f);

        // Await first beat
        await AssertSignal(RhythmClock.Instance)
            .IsEmitted("Beat")
            .WithTimeout(2000);

        // Await second beat
        await AssertSignal(RhythmClock.Instance)
            .IsEmitted("Beat")
            .WithTimeout(2000);

        AssertThat(RhythmClock.Instance.BeatIndex).IsGreaterEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Stop_PreventsSubsequentBeatSignals()
    {
        RhythmClock.Instance.StartFreeRunning(180f);
        // Let one beat fire to confirm it's running
        await AssertSignal(RhythmClock.Instance)
            .IsEmitted("Beat")
            .WithTimeout(2000);

        RhythmClock.Instance.Stop();

        // After stop, no further Beat signal should fire in 1 second
        await AssertSignal(RhythmClock.Instance)
            .IsNotEmitted("Beat")
            .WithTimeout(1000);
    }

    // ── BeatPhase ────────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task BeatPhase_IsInRange_WhileRunning()
    {
        RhythmClock.Instance.StartFreeRunning(180f);
        // Wait one frame for _Process to run
        await AssertSignal(RhythmClock.Instance)
            .IsEmitted("Beat")
            .WithTimeout(2000);

        float phase = RhythmClock.Instance.BeatPhase;
        AssertFloat(phase).IsBetween(0f, 1f);
    }
}
