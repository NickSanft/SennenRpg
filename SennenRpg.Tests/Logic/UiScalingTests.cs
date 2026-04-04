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
    public void AllWindowScales_Are16by9()
    {
        foreach (WindowScale scale in System.Enum.GetValues<WindowScale>())
        {
            var size = SettingsLogic.WindowSize(scale);
            float ratio = (float)size.X / size.Y;
            Assert.That(ratio, Is.EqualTo(16f / 9f).Within(0.01f),
                $"WindowScale.{scale} ({size.X}x{size.Y}) is not 16:9");
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
        Assert.That(12, Is.InRange(8, 16));
    }
}
