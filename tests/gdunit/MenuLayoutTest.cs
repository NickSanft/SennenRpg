using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace SennenRpg.Tests.GdUnit;

/// <summary>
/// GdUnit4 layout tests that instantiate menus inside the Godot runtime,
/// let the layout engine resolve sizes, then verify that no Label overflows
/// its parent container. These catch text-overflow bugs that pure NUnit
/// tests cannot detect because they use the real font metrics and layout.
///
/// Run from the GdUnit4 panel in the Godot editor, or headless via:
///   godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd
/// </summary>
[TestSuite]
public sealed class MenuLayoutTest
{
    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Instantiate a scene, add it to the tree, and wait for layout.
    /// Returns the root node (caller must QueueFree).
    /// </summary>
    private static T? AddToTree<T>(string scenePath) where T : Node
    {
        if (!ResourceLoader.Exists(scenePath)) return null;
        var packed = GD.Load<PackedScene>(scenePath);
        var node = packed?.Instantiate<T>();
        if (node == null) return null;

        var tree = Engine.GetMainLoop() as SceneTree;
        tree!.Root.AddChild(node);
        return node;
    }

    /// <summary>
    /// Recursively find all Label nodes in the subtree.
    /// </summary>
    private static System.Collections.Generic.List<Label> FindAllLabels(Node root)
    {
        var labels = new System.Collections.Generic.List<Label>();
        CollectLabels(root, labels);
        return labels;
    }

    private static void CollectLabels(Node node, System.Collections.Generic.List<Label> list)
    {
        if (node is Label label)
            list.Add(label);
        foreach (var child in node.GetChildren())
            CollectLabels(child, list);
    }

    /// <summary>
    /// Recursively find all Button nodes in the subtree.
    /// </summary>
    private static System.Collections.Generic.List<Button> FindAllButtons(Node root)
    {
        var buttons = new System.Collections.Generic.List<Button>();
        CollectButtons(root, buttons);
        return buttons;
    }

    private static void CollectButtons(Node node, System.Collections.Generic.List<Button> list)
    {
        if (node is Button btn)
            list.Add(btn);
        foreach (var child in node.GetChildren())
            CollectButtons(child, list);
    }

    /// <summary>
    /// Check that a Label's minimum width doesn't exceed its parent container width.
    /// Uses GetMinimumSize() which accounts for the actual font and text content.
    /// Returns a list of overflow descriptions (empty = all OK).
    /// </summary>
    private static System.Collections.Generic.List<string> CheckLabelOverflows(Node root)
    {
        var overflows = new System.Collections.Generic.List<string>();
        var labels = FindAllLabels(root);

        foreach (var label in labels)
        {
            // Skip labels with autowrap (they handle overflow by wrapping)
            if (label.AutowrapMode != TextServer.AutowrapMode.Off) continue;

            // Skip empty labels (dynamic content filled later)
            if (string.IsNullOrEmpty(label.Text) || label.Text == "—" || label.Text == "…") continue;

            // Skip labels with ClipText enabled (intentionally truncated)
            if (label.ClipText) continue;

            var minSize = label.GetMinimumSize();
            var parentControl = label.GetParent() as Control;
            if (parentControl == null) continue;

            float availableWidth = parentControl.Size.X;

            // If parent hasn't resolved size yet, use its CustomMinimumSize
            if (availableWidth <= 0f)
                availableWidth = parentControl.CustomMinimumSize.X;

            // If we still don't have a width, skip (can't measure)
            if (availableWidth <= 0f) continue;

            // Check for overflow
            if (minSize.X > availableWidth)
            {
                string path = label.GetPath().ToString();
                overflows.Add(
                    $"Label overflow: \"{label.Text}\" needs {minSize.X:F0}px " +
                    $"but parent has {availableWidth:F0}px (at {path})");
            }
        }

        return overflows;
    }

    // ── LevelUpScreen layout ─────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task LevelUpScreen_NoLabelOverflows()
    {
        var scene = AddToTree<CanvasLayer>("res://scenes/menus/LevelUpScreen.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        // Wait two frames for layout to resolve
        await (Engine.GetMainLoop() as SceneTree)!.ToSignal(
            (Engine.GetMainLoop() as SceneTree)!.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        var overflows = CheckLabelOverflows(scene);
        scene.QueueFree();

        AssertThat(overflows.Count).IsEqual(0);
        if (overflows.Count > 0)
            GD.PrintErr(string.Join("\n", overflows));
    }

    // ── PauseMenu layout ─────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task PauseMenu_NoLabelOverflows()
    {
        var scene = AddToTree<CanvasLayer>("res://scenes/menus/PauseMenu.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        await (Engine.GetMainLoop() as SceneTree)!.ToSignal(
            (Engine.GetMainLoop() as SceneTree)!.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        var overflows = CheckLabelOverflows(scene);
        scene.QueueFree();

        AssertThat(overflows.Count).IsEqual(0);
        if (overflows.Count > 0)
            GD.PrintErr(string.Join("\n", overflows));
    }

    // ── SaveSlotMenu layout ──────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task SaveSlotMenu_NoLabelOverflows()
    {
        var scene = AddToTree<CanvasLayer>("res://scenes/menus/SaveSlotMenu.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        await (Engine.GetMainLoop() as SceneTree)!.ToSignal(
            (Engine.GetMainLoop() as SceneTree)!.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        var overflows = CheckLabelOverflows(scene);
        scene.QueueFree();

        AssertThat(overflows.Count).IsEqual(0);
        if (overflows.Count > 0)
            GD.PrintErr(string.Join("\n", overflows));
    }

    // ── SettingsMenu layout ──────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task SettingsMenu_NoLabelOverflows()
    {
        var scene = AddToTree<CanvasLayer>("res://scenes/menus/SettingsMenu.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        await (Engine.GetMainLoop() as SceneTree)!.ToSignal(
            (Engine.GetMainLoop() as SceneTree)!.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        var overflows = CheckLabelOverflows(scene);
        scene.QueueFree();

        AssertThat(overflows.Count).IsEqual(0);
        if (overflows.Count > 0)
            GD.PrintErr(string.Join("\n", overflows));
    }

    // ── GameOver layout ──────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task GameOver_NoLabelOverflows()
    {
        var scene = AddToTree<CanvasLayer>("res://scenes/menus/GameOver.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        await (Engine.GetMainLoop() as SceneTree)!.ToSignal(
            (Engine.GetMainLoop() as SceneTree)!.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        var overflows = CheckLabelOverflows(scene);
        scene.QueueFree();

        AssertThat(overflows.Count).IsEqual(0);
        if (overflows.Count > 0)
            GD.PrintErr(string.Join("\n", overflows));
    }

    // ── MainMenu layout ──────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task MainMenu_NoLabelOverflows()
    {
        var scene = AddToTree<Node>("res://scenes/menus/MainMenu.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        await (Engine.GetMainLoop() as SceneTree)!.ToSignal(
            (Engine.GetMainLoop() as SceneTree)!.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        var overflows = CheckLabelOverflows(scene);
        scene.QueueFree();

        AssertThat(overflows.Count).IsEqual(0);
        if (overflows.Count > 0)
            GD.PrintErr(string.Join("\n", overflows));
    }

    // ── CreditsMenu layout ───────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task CreditsMenu_NoLabelOverflows()
    {
        var scene = AddToTree<CanvasLayer>("res://scenes/menus/CreditsMenu.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        await (Engine.GetMainLoop() as SceneTree)!.ToSignal(
            (Engine.GetMainLoop() as SceneTree)!.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        var overflows = CheckLabelOverflows(scene);
        scene.QueueFree();

        AssertThat(overflows.Count).IsEqual(0);
        if (overflows.Count > 0)
            GD.PrintErr(string.Join("\n", overflows));
    }

    // ── Button text checks ───────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public async Task PauseMenu_AllButtons_HaveNonEmptyText()
    {
        var scene = AddToTree<CanvasLayer>("res://scenes/menus/PauseMenu.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        await (Engine.GetMainLoop() as SceneTree)!.ToSignal(
            (Engine.GetMainLoop() as SceneTree)!.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        var buttons = FindAllButtons(scene);
        scene.QueueFree();

        AssertThat(buttons.Count).IsGreaterThan(0);
        foreach (var btn in buttons)
        {
            AssertThat(btn.Text.Length).IsGreaterThan(0);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task MainMenu_AllButtons_HaveNonEmptyText()
    {
        var scene = AddToTree<Node>("res://scenes/menus/MainMenu.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        await (Engine.GetMainLoop() as SceneTree)!.ToSignal(
            (Engine.GetMainLoop() as SceneTree)!.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);

        var buttons = FindAllButtons(scene);
        scene.QueueFree();

        AssertThat(buttons.Count).IsGreaterThan(0);
        foreach (var btn in buttons)
        {
            AssertThat(btn.Text.Length).IsGreaterThan(0);
        }
    }

    // ── CanvasLayer ordering ─────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void LevelUpScreen_Layer_IsBelowSceneTransition()
    {
        var scene = AddToTree<CanvasLayer>("res://scenes/menus/LevelUpScreen.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        // LevelUpScreen should be at layer 70 — below SceneTransition (100)
        AssertThat(scene.Layer).IsLess(100);
        AssertThat(scene.Layer).IsGreaterEqual(50); // above pause menu

        scene.QueueFree();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PauseMenu_Layer_IsBelow_SubMenus()
    {
        var pause = AddToTree<CanvasLayer>("res://scenes/menus/PauseMenu.tscn");
        if (pause == null) { AssertThat(pause).IsNotNull(); return; }

        // PauseMenu should be at layer 50
        AssertThat(pause.Layer).IsEqual(50);

        pause.QueueFree();
    }
}
