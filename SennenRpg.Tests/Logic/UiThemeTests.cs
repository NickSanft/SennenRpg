using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class UiThemeTests
{
    [Test]
    public void Gold_IsNonTransparent()
    {
        Assert.That(UiTheme.Gold.A, Is.GreaterThan(0f));
    }

    [Test]
    public void PanelBg_IsNonTransparent()
    {
        Assert.That(UiTheme.PanelBg.A, Is.GreaterThan(0f));
    }

    [Test]
    public void PanelBorder_IsNonTransparent()
    {
        Assert.That(UiTheme.PanelBorder.A, Is.GreaterThan(0f));
    }

    [Test]
    public void AllNamedColors_AreDistinct()
    {
        var colors = new[]
        {
            UiTheme.Gold, UiTheme.SubtleGrey, UiTheme.HaveGreen,
            UiTheme.NeedRed, UiTheme.MpBlue, UiTheme.LinkBlue,
        };

        for (int i = 0; i < colors.Length; i++)
            for (int j = i + 1; j < colors.Length; j++)
                Assert.That(colors[i], Is.Not.EqualTo(colors[j]),
                    $"Color at index {i} is the same as color at index {j}");
    }

    [Test]
    public void CornerRadius_IsPositive()
    {
        Assert.That(UiTheme.CornerRadius, Is.GreaterThan(0));
    }

    [Test]
    public void BorderWidth_IsPositive()
    {
        Assert.That(UiTheme.BorderWidth, Is.GreaterThan(0));
    }

    [Test]
    public void PixelFontPath_IsNonEmpty()
    {
        Assert.That(UiTheme.PixelFontPath, Is.Not.Empty);
    }
}
