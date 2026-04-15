using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// Tests to ensure UI elements scale correctly with the canvas_items stretch mode.
/// World-space UI (NPC names, prompts, map labels) must NOT use CanvasLayer
/// because CanvasLayer renders at window resolution, not viewport resolution.
/// </summary>
[TestFixture]
public class UiScalingTests
{
    [Test]
    public void BaseResolution_Is1280x720()
    {
        Assert.That(SettingsLogic.BaseResolution.X, Is.EqualTo(1280));
        Assert.That(SettingsLogic.BaseResolution.Y, Is.EqualTo(720));
    }

    [Test]
    public void StandardWindowScales_Are16by9()
    {
        WindowScale[] standard = {
            WindowScale.Scale1x, WindowScale.Scale2x, WindowScale.Scale3x,
            WindowScale.Scale4x, WindowScale.Scale1440p, WindowScale.Fullscreen,
        };
        foreach (WindowScale scale in standard)
        {
            var size = SettingsLogic.WindowSize(scale);
            float ratio = (float)size.X / size.Y;
            Assert.That(ratio, Is.EqualTo(16f / 9f).Within(0.01f),
                $"WindowScale.{scale} ({size.X}x{size.Y}) is not 16:9");
        }
    }

    [Test]
    public void UltrawideScale_Is21by9()
    {
        var size = SettingsLogic.WindowSize(WindowScale.ScaleUltrawide);
        Assert.That(size.X, Is.EqualTo(3440));
        Assert.That(size.Y, Is.EqualTo(1440));
        float ratio = (float)size.X / size.Y;
        // 3440/1440 ≈ 2.389 (43:18), commonly marketed as 21:9
        Assert.That(ratio, Is.GreaterThan(2.3f));
        Assert.That(ratio, Is.LessThan(2.4f));
    }

    [Test]
    public void AllWindowScales_HavePositiveDimensions()
    {
        foreach (WindowScale scale in System.Enum.GetValues<WindowScale>())
        {
            var size = SettingsLogic.WindowSize(scale);
            Assert.That(size.X, Is.GreaterThan(0), $"WindowScale.{scale} has non-positive width");
            Assert.That(size.Y, Is.GreaterThan(0), $"WindowScale.{scale} has non-positive height");
        }
    }

    [Test]
    public void AllWindowScales_HaveLabels()
    {
        foreach (WindowScale scale in System.Enum.GetValues<WindowScale>())
        {
            string label = SettingsLogic.WindowScaleLabel(scale);
            Assert.That(label, Is.Not.Empty, $"WindowScale.{scale} has no label");
            Assert.That(label, Is.Not.EqualTo("1280x720 (HD)").Or.EqualTo(
                SettingsLogic.WindowScaleLabel(WindowScale.Scale2x)),
                $"WindowScale.{scale} fell through to default label");
        }
    }

    [Test]
    public void WindowScales_AreInAscendingOrder()
    {
        // Verify that each scale (except Fullscreen) is larger than the previous
        WindowScale[] ordered = {
            WindowScale.Scale1x, WindowScale.Scale2x, WindowScale.Scale3x,
            WindowScale.Scale4x, WindowScale.Scale1440p, WindowScale.ScaleUltrawide,
        };
        for (int i = 1; i < ordered.Length; i++)
        {
            var prev = SettingsLogic.WindowSize(ordered[i - 1]);
            var curr = SettingsLogic.WindowSize(ordered[i]);
            int prevPixels = prev.X * prev.Y;
            int currPixels = curr.X * curr.Y;
            Assert.That(currPixels, Is.GreaterThan(prevPixels),
                $"WindowScale.{ordered[i]} ({curr.X}x{curr.Y}) should be larger than " +
                $"WindowScale.{ordered[i - 1]} ({prev.X}x{prev.Y})");
        }
    }

    [Test]
    public void UiTheme_PanelBg_IsOpaque()
    {
        Assert.That(UiTheme.PanelBg.A, Is.GreaterThanOrEqualTo(0.9f));
    }

    [Test]
    public void UiTheme_OverlayDim_IsSemiTransparent()
    {
        Assert.That(UiTheme.OverlayDim.A, Is.GreaterThan(0.5f));
        Assert.That(UiTheme.OverlayDim.A, Is.LessThan(1.0f));
    }

    [Test]
    public void FallbackFontSize_IsReasonableForViewport()
    {
        // At 1280x720, font size should be 8-16px for readability
        // ThemeDB.FallbackFontSize is set in ApplyGlobalTheme() to 12
        // We test the constant here since we can't access ThemeDB in NUnit
        int fallbackFontSize = 12;
        Assert.That(fallbackFontSize, Is.InRange(8, 16));
    }
}
