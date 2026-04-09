using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// Pure-logic tests for the sprite beat-sync helpers — no Godot runtime.
/// </summary>
[TestFixture]
public sealed class BeatSyncLogicTests
{
    // ── SnappedFrame ──────────────────────────────────────────────────────────

    [Test]
    public void SnappedFrame_OneFramePerBeat_TwoFrameCycle()
    {
        Assert.That(BeatSyncLogic.SnappedFrame(0, 1.0f, 2), Is.EqualTo(0));
        Assert.That(BeatSyncLogic.SnappedFrame(1, 1.0f, 2), Is.EqualTo(1));
        Assert.That(BeatSyncLogic.SnappedFrame(2, 1.0f, 2), Is.EqualTo(0));
        Assert.That(BeatSyncLogic.SnappedFrame(3, 1.0f, 2), Is.EqualTo(1));
    }

    [Test]
    public void SnappedFrame_HalfFramePerBeat_AdvancesEverySecondBeat()
    {
        // framesPerBeat = 0.5 → frame changes every 2 beats
        Assert.That(BeatSyncLogic.SnappedFrame(0, 0.5f, 4), Is.EqualTo(0));
        Assert.That(BeatSyncLogic.SnappedFrame(1, 0.5f, 4), Is.EqualTo(0));
        Assert.That(BeatSyncLogic.SnappedFrame(2, 0.5f, 4), Is.EqualTo(1));
        Assert.That(BeatSyncLogic.SnappedFrame(3, 0.5f, 4), Is.EqualTo(1));
        Assert.That(BeatSyncLogic.SnappedFrame(4, 0.5f, 4), Is.EqualTo(2));
        Assert.That(BeatSyncLogic.SnappedFrame(8, 0.5f, 4), Is.EqualTo(0)); // wraps
    }

    [Test]
    public void SnappedFrame_TwoFramesPerBeat_StrobeMode()
    {
        // framesPerBeat = 2.0 → two frames per beat
        Assert.That(BeatSyncLogic.SnappedFrame(0, 2.0f, 4), Is.EqualTo(0));
        Assert.That(BeatSyncLogic.SnappedFrame(1, 2.0f, 4), Is.EqualTo(2));
        Assert.That(BeatSyncLogic.SnappedFrame(2, 2.0f, 4), Is.EqualTo(0)); // wraps after 2 beats
    }

    [Test]
    public void SnappedFrame_ZeroFramesReturnsZero()
        => Assert.That(BeatSyncLogic.SnappedFrame(5, 1.0f, 0), Is.EqualTo(0));

    [Test]
    public void SnappedFrame_NegativeFramesPerBeatReturnsZero()
        => Assert.That(BeatSyncLogic.SnappedFrame(5, -1.0f, 4), Is.EqualTo(0));

    [Test]
    public void SnappedFrame_NegativeBeatIndexWrapsPositively()
    {
        // -1 % 2 = -1 in C#; we want it to wrap to 1.
        Assert.That(BeatSyncLogic.SnappedFrame(-1, 1.0f, 2), Is.EqualTo(1));
        Assert.That(BeatSyncLogic.SnappedFrame(-2, 1.0f, 2), Is.EqualTo(0));
    }

    [Test]
    public void SnappedFrame_HighBeatIndexWraps()
    {
        // 1000 % 4 = 0
        Assert.That(BeatSyncLogic.SnappedFrame(1000, 1.0f, 4), Is.EqualTo(0));
        // 1001 % 4 = 1
        Assert.That(BeatSyncLogic.SnappedFrame(1001, 1.0f, 4), Is.EqualTo(1));
    }

    // ── ScaleFactor ───────────────────────────────────────────────────────────

    [Test]
    public void ScaleFactor_Identity_BpmEqualsBaseline()
        => Assert.That(BeatSyncLogic.ScaleFactor(120f, 120f, 1.0f), Is.EqualTo(1.0f).Within(0.001f));

    [Test]
    public void ScaleFactor_DoubleTempoIsTwo()
        => Assert.That(BeatSyncLogic.ScaleFactor(240f, 120f, 1.0f), Is.EqualTo(2.0f).Within(0.001f));

    [Test]
    public void ScaleFactor_HalfTempoIsHalf()
        => Assert.That(BeatSyncLogic.ScaleFactor(60f, 120f, 1.0f), Is.EqualTo(0.5f).Within(0.001f));

    [Test]
    public void ScaleFactor_FramesPerBeatHalvesAtIdentity()
        => Assert.That(BeatSyncLogic.ScaleFactor(120f, 120f, 0.5f), Is.EqualTo(0.5f).Within(0.001f));

    [Test]
    public void ScaleFactor_ZeroBpmReturnsOne()
    {
        Assert.That(BeatSyncLogic.ScaleFactor(0f,   120f, 1.0f), Is.EqualTo(1f));
        Assert.That(BeatSyncLogic.ScaleFactor(120f, 0f,   1.0f), Is.EqualTo(1f));
    }

    [Test]
    public void ScaleFactor_NegativeInputsReturnOne()
    {
        Assert.That(BeatSyncLogic.ScaleFactor(-1f,  120f, 1.0f), Is.EqualTo(1f));
        Assert.That(BeatSyncLogic.ScaleFactor(120f, -1f,  1.0f), Is.EqualTo(1f));
        Assert.That(BeatSyncLogic.ScaleFactor(120f, 120f, -1f),  Is.EqualTo(1f));
    }

    // ── CombineScales ─────────────────────────────────────────────────────────

    [Test]
    public void CombineScales_MultipliesBoth()
        => Assert.That(BeatSyncLogic.CombineScales(2.0f, 1.5f), Is.EqualTo(3.0f).Within(0.001f));

    [Test]
    public void CombineScales_NegativeBeatScaleIsClampedToOne()
        => Assert.That(BeatSyncLogic.CombineScales(-1f, 1.5f), Is.EqualTo(1.5f));

    [Test]
    public void CombineScales_NaNUserMultiplierIsClampedToOne()
        => Assert.That(BeatSyncLogic.CombineScales(2f, float.NaN), Is.EqualTo(2f));
}
