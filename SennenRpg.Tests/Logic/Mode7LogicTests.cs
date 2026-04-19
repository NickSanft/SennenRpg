using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class Mode7LogicTests
{
    // ── WorldToShaderOffset ──────────────────────────────────────────────

    [Test]
    public void WorldToShaderOffset_CenterOfMap_ReturnsZero()
    {
        var (x, y) = Mode7Logic.WorldToShaderOffset(500f, 300f, 1000f, 600f);
        Assert.That(x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(y, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void WorldToShaderOffset_TopLeftCorner_ReturnsNegativeHalf()
    {
        var (x, y) = Mode7Logic.WorldToShaderOffset(0f, 0f, 1000f, 600f);
        Assert.That(x, Is.EqualTo(-0.5f).Within(0.001f));
        Assert.That(y, Is.EqualTo(-0.5f).Within(0.001f));
    }

    [Test]
    public void WorldToShaderOffset_BottomRightCorner_ReturnsPositiveHalf()
    {
        var (x, y) = Mode7Logic.WorldToShaderOffset(1000f, 600f, 1000f, 600f);
        Assert.That(x, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(y, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void WorldToShaderOffset_BeyondBounds_ClampsToHalf()
    {
        var (x, y) = Mode7Logic.WorldToShaderOffset(2000f, -500f, 1000f, 600f);
        Assert.That(x, Is.EqualTo(0.5f));
        Assert.That(y, Is.EqualTo(-0.5f));
    }

    [Test]
    public void WorldToShaderOffset_ZeroDimensions_ReturnsZero()
    {
        var (x, y) = Mode7Logic.WorldToShaderOffset(100f, 200f, 0f, 0f);
        Assert.That(x, Is.EqualTo(0f));
        Assert.That(y, Is.EqualTo(0f));
    }

    [Test]
    public void WorldToShaderOffset_ZeroWidth_XIsZero()
    {
        var (x, y) = Mode7Logic.WorldToShaderOffset(100f, 300f, 0f, 600f);
        Assert.That(x, Is.EqualTo(0f));
        Assert.That(y, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void WorldToShaderOffset_ZeroHeight_YIsZero()
    {
        var (x, y) = Mode7Logic.WorldToShaderOffset(500f, 200f, 1000f, 0f);
        Assert.That(x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(y, Is.EqualTo(0f));
    }

    // ── ClampParams ─────────────────────────────────────────────────────

    [Test]
    public void ClampParams_WithinRange_PassThrough()
    {
        var (h, ht, f) = Mode7Logic.ClampParams(0.35f, 1.2f, 1.0f);
        Assert.That(h, Is.EqualTo(0.35f).Within(0.001f));
        Assert.That(ht, Is.EqualTo(1.2f).Within(0.001f));
        Assert.That(f, Is.EqualTo(1.0f).Within(0.001f));
    }

    [Test]
    public void ClampParams_BelowMin_ClampsUp()
    {
        var (h, ht, f) = Mode7Logic.ClampParams(0.0f, 0.1f, 0.2f);
        Assert.That(h, Is.EqualTo(0.1f));
        Assert.That(ht, Is.EqualTo(0.5f));
        Assert.That(f, Is.EqualTo(0.5f));
    }

    [Test]
    public void ClampParams_AboveMax_ClampsDown()
    {
        var (h, ht, f) = Mode7Logic.ClampParams(1.0f, 5.0f, 3.0f);
        Assert.That(h, Is.EqualTo(0.6f));
        Assert.That(ht, Is.EqualTo(3.0f));
        Assert.That(f, Is.EqualTo(2.0f));
    }

    [Test]
    public void ClampParams_AtBoundaries_PassThrough()
    {
        var (h, ht, f) = Mode7Logic.ClampParams(0.1f, 0.5f, 0.5f);
        Assert.That(h, Is.EqualTo(0.1f));
        Assert.That(ht, Is.EqualTo(0.5f));
        Assert.That(f, Is.EqualTo(0.5f));

        var (h2, ht2, f2) = Mode7Logic.ClampParams(0.6f, 3.0f, 2.0f);
        Assert.That(h2, Is.EqualTo(0.6f));
        Assert.That(ht2, Is.EqualTo(3.0f));
        Assert.That(f2, Is.EqualTo(2.0f));
    }
}
