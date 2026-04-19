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

    // ── Constants ────────────────────────────────────────────────────────

    [Test]
    public void DefaultPitch_MatchesShader()
    {
        Assert.That(Mode7Logic.DefaultPitch, Is.EqualTo(0.35f));
    }

    [Test]
    public void CameraOffsetY_IsPositive()
    {
        // Positive offset pushes player to upper screen for bottom-receding view.
        Assert.That(Mode7Logic.CameraOffsetY, Is.GreaterThan(0f));
    }

    // ── PerspectiveWarpY ────────────────────────────────────────────────
    // Mirrors the shader's ground plane: top of screen (screenY=1) is close,
    // bottom (screenY=0) is far/horizon. Ground covers the ENTIRE screen.

    [Test]
    public void PerspectiveWarpY_TopOfScreen_MapsToTopOfTexture()
    {
        // screenY=1 (close, top) → source_y should be near 0 (top of original)
        float sourceY = Mode7Logic.PerspectiveWarpY(1f, 0.35f, 1f);
        Assert.That(sourceY, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void PerspectiveWarpY_BottomOfScreen_MapsToBottomOfTexture()
    {
        // screenY=0 (far, bottom) → source_y should be 1 (bottom of original)
        float sourceY = Mode7Logic.PerspectiveWarpY(0f, 0.35f, 1f);
        Assert.That(sourceY, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void PerspectiveWarpY_NoTransition_IsIdentity()
    {
        // t=0 means no warp — source_y should equal (1 - screenY) i.e. identity flip
        float sourceY = Mode7Logic.PerspectiveWarpY(0.7f, 0.35f, 0f);
        Assert.That(sourceY, Is.EqualTo(1f - 0.7f).Within(0.001f));
    }

    [Test]
    public void PerspectiveWarpY_FullTransition_CompressesNearHorizon()
    {
        // Mid-screen should map further from center than linear due to pow < 1
        float linearSourceY = 1f - 0.5f;  // 0.5
        float warpedSourceY = Mode7Logic.PerspectiveWarpY(0.5f, 0.35f, 1f);
        // pow(0.5, 0.35) > 0.5, so 1 - pow > 0.5 is false; warped < linear
        Assert.That(warpedSourceY, Is.LessThan(linearSourceY));
    }

    [Test]
    public void PerspectiveWarpY_GroundCoversEntireScreen()
    {
        // Verify the full range [0,1] of screenY maps to full range [0,1] of source
        float top = Mode7Logic.PerspectiveWarpY(1f, 0.35f, 1f);
        float bot = Mode7Logic.PerspectiveWarpY(0f, 0.35f, 1f);
        Assert.That(top, Is.EqualTo(0f).Within(0.001f), "Top of screen → top of texture");
        Assert.That(bot, Is.EqualTo(1f).Within(0.001f), "Bottom of screen → bottom of texture");
    }

    [Test]
    public void PerspectiveWarpY_MonotonicallyIncreasing_TopToBottom()
    {
        // As screenY decreases (top→bottom), source_y should increase
        float prev = Mode7Logic.PerspectiveWarpY(1f, 0.35f, 1f);
        for (int i = 9; i >= 0; i--)
        {
            float sy = i / 10f;
            float curr = Mode7Logic.PerspectiveWarpY(sy, 0.35f, 1f);
            Assert.That(curr, Is.GreaterThanOrEqualTo(prev),
                $"source_y should increase as screenY decreases (screenY={sy})");
            prev = curr;
        }
    }

    [Test]
    public void PerspectiveWarpY_NoSkyBand_AllScreenYValuesProduceGroundUV()
    {
        // Every scanline from top to bottom should produce a valid ground UV,
        // NOT a sky color. This ensures the entire screen is ground plane.
        for (int i = 0; i <= 20; i++)
        {
            float screenY = i / 20f;
            float sourceY = Mode7Logic.PerspectiveWarpY(screenY, 0.35f, 1f);
            Assert.That(sourceY, Is.InRange(0f, 1f),
                $"screenY={screenY} must map to a valid texture UV, not sky");
        }
    }

    // ── HorizontalScale ─────────────────────────────────────────────────

    [Test]
    public void HorizontalScale_TopOfScreen_IsOne()
    {
        // screenY=1 (close) → no stretch
        float scale = Mode7Logic.HorizontalScale(1f, 1f);
        Assert.That(scale, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void HorizontalScale_BottomOfScreen_IsMaxStretch()
    {
        // screenY=0 (far) → maximum horizontal stretch = 1 + 1*1*2 = 3
        float scale = Mode7Logic.HorizontalScale(0f, 1f);
        Assert.That(scale, Is.EqualTo(3f).Within(0.001f));
    }

    [Test]
    public void HorizontalScale_NoTransition_IsOne()
    {
        float scale = Mode7Logic.HorizontalScale(0f, 0f);
        Assert.That(scale, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void HorizontalScale_QuadraticGrowth_TopHalfBarelySpreads()
    {
        // At screenY=0.5 (mid-screen), dist=0.5, dist²=0.25, scale=1.5
        // At screenY=0.0 (bottom), dist=1.0, dist²=1.0, scale=3.0
        // The top half has much less stretch than bottom half.
        float mid = Mode7Logic.HorizontalScale(0.5f, 1f);
        float bot = Mode7Logic.HorizontalScale(0f, 1f);
        Assert.That(mid, Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(bot, Is.EqualTo(3f).Within(0.001f));
        Assert.That(bot - 1f, Is.GreaterThan((mid - 1f) * 3f),
            "Bottom half stretch should be much greater than top half (quadratic)");
    }

    [Test]
    public void HorizontalScale_MonotonicallyIncreasing_TopToBottom()
    {
        float prev = Mode7Logic.HorizontalScale(1f, 1f);
        for (int i = 9; i >= 0; i--)
        {
            float sy = i / 10f;
            float curr = Mode7Logic.HorizontalScale(sy, 1f);
            Assert.That(curr, Is.GreaterThanOrEqualTo(prev),
                $"h_scale should increase as screenY decreases (screenY={sy})");
            prev = curr;
        }
    }

    // ── SourceX ─────────────────────────────────────────────────────────

    [Test]
    public void SourceX_CenterColumn_AlwaysCenter()
    {
        // Center pixel (uvX=0.5) should stay at 0.5 regardless of row/transition
        Assert.That(Mode7Logic.SourceX(0.5f, 0f, 1f), Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(Mode7Logic.SourceX(0.5f, 1f, 1f), Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(Mode7Logic.SourceX(0.5f, 0.5f, 0.5f), Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void SourceX_TopOfScreen_NoShift()
    {
        // screenY=1, h_scale=1, so source_x = uvX
        Assert.That(Mode7Logic.SourceX(0.2f, 1f, 1f), Is.EqualTo(0.2f).Within(0.001f));
        Assert.That(Mode7Logic.SourceX(0.8f, 1f, 1f), Is.EqualTo(0.8f).Within(0.001f));
    }

    [Test]
    public void SourceX_BottomOfScreen_CompressedTowardCenter()
    {
        // At bottom, h_scale=3.0, so offset from center is divided by 3
        float left  = Mode7Logic.SourceX(0.2f, 0f, 1f);
        float right = Mode7Logic.SourceX(0.8f, 0f, 1f);
        Assert.That(left, Is.EqualTo(0.5f + (0.2f - 0.5f) / 3f).Within(0.001f));
        Assert.That(right, Is.EqualTo(0.5f + (0.8f - 0.5f) / 3f).Within(0.001f));
    }

    // ── FogFactor ───────────────────────────────────────────────────────

    [Test]
    public void FogFactor_AtBottom_IsZero()
    {
        // screenY=0 (horizon) → full fog
        Assert.That(Mode7Logic.FogFactor(0f), Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void FogFactor_AtTop_IsOne()
    {
        // screenY=1 (close) → no fog
        Assert.That(Mode7Logic.FogFactor(1f), Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void FogFactor_AboveThreshold_IsOne()
    {
        // Fog only applies in the bottom 25% of screen (screenY < 0.25)
        Assert.That(Mode7Logic.FogFactor(0.3f), Is.EqualTo(1f).Within(0.001f));
        Assert.That(Mode7Logic.FogFactor(0.5f), Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void FogFactor_InFogBand_Interpolates()
    {
        // screenY=0.125 is halfway through the fog band [0, 0.25]
        float fog = Mode7Logic.FogFactor(0.125f);
        Assert.That(fog, Is.GreaterThan(0f));
        Assert.That(fog, Is.LessThan(1f));
    }

    [Test]
    public void FogFactor_MonotonicallyIncreasing()
    {
        float prev = Mode7Logic.FogFactor(0f);
        for (int i = 1; i <= 20; i++)
        {
            float sy = i / 20f;
            float curr = Mode7Logic.FogFactor(sy);
            Assert.That(curr, Is.GreaterThanOrEqualTo(prev),
                $"Fog factor should increase as screenY increases (screenY={sy})");
            prev = curr;
        }
    }
}
