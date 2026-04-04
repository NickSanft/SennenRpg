using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// Static analysis tests to prevent regressions where world-space UI elements
/// use CanvasLayer (which doesn't scale with canvas_items stretch mode).
/// </summary>
[TestFixture]
public class WorldSpaceUiTests
{
    private static string ProjectRoot => Path.GetFullPath(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));

    /// <summary>
    /// NPC name labels and interact prompts must NOT create CanvasLayer at Layer 0
    /// because it renders at window resolution, not viewport resolution.
    /// </summary>
    [Test]
    public void Npc_DoesNotCreateCanvasLayerForNameLabel()
    {
        string path = Path.Combine(ProjectRoot, "scenes", "overworld", "objects", "Npc.cs");
        if (!File.Exists(path)) Assert.Inconclusive($"File not found: {path}");

        string content = File.ReadAllText(path);
        Assert.That(content, Does.Not.Contain("new CanvasLayer"),
            "Npc.cs should not create a CanvasLayer for name labels. " +
            "Use AddChild() instead so labels scale with canvas_items stretch.");
    }

    /// <summary>
    /// InteractPromptBubble must NOT create CanvasLayer.
    /// </summary>
    [Test]
    public void InteractPromptBubble_DoesNotCreateCanvasLayer()
    {
        string path = Path.Combine(ProjectRoot, "scenes", "overworld", "objects", "InteractPromptBubble.cs");
        if (!File.Exists(path)) Assert.Inconclusive($"File not found: {path}");

        string content = File.ReadAllText(path);
        // The field _canvas is kept for compatibility but should not be instantiated
        int canvasLayerCreations = content.Split("new CanvasLayer").Length - 1;
        Assert.That(canvasLayerCreations, Is.EqualTo(0),
            "InteractPromptBubble should not create a CanvasLayer. " +
            "Use AddChild() to the parent Node2D instead.");
    }

    /// <summary>
    /// WorldMap entrance labels must NOT use CanvasLayer.
    /// </summary>
    [Test]
    public void WorldMap_EntranceLabels_NoCanvasLayer()
    {
        string path = Path.Combine(ProjectRoot, "scenes", "overworld", "WorldMap.cs");
        if (!File.Exists(path)) Assert.Inconclusive($"File not found: {path}");

        string content = File.ReadAllText(path);
        // The SpawnEntranceLabels method should not contain CanvasLayer
        int labelSectionStart = content.IndexOf("SpawnEntranceLabels");
        if (labelSectionStart < 0) Assert.Inconclusive("SpawnEntranceLabels not found");

        string labelSection = content[labelSectionStart..];
        // Take until the next method
        int nextMethod = labelSection.IndexOf("\n\t//", 10);
        if (nextMethod > 0) labelSection = labelSection[..nextMethod];

        Assert.That(labelSection, Does.Not.Contain("CanvasLayer"),
            "WorldMap.SpawnEntranceLabels should not use CanvasLayer. " +
            "World-space labels scale correctly with canvas_items stretch.");
    }
}
