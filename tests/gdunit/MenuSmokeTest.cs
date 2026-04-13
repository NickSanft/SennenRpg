using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace SennenRpg.Tests.GdUnit;

/// <summary>
/// GdUnit4 smoke tests for all code-built menus. Each test instantiates
/// a menu scene, verifies it doesn't crash, and checks that key child
/// nodes exist. These require the Godot runtime.
///
/// Run from the GdUnit4 panel in the Godot editor, or headless via:
///   godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd
/// </summary>
[TestSuite]
public sealed class MenuSmokeTest
{
    // ── Helper ────────────────────────────────────────────────────────

    private static T? InstantiateScene<T>(string scenePath) where T : Node
    {
        if (!ResourceLoader.Exists(scenePath))
            return null;
        var packed = GD.Load<PackedScene>(scenePath);
        return packed?.Instantiate<T>();
    }

    // ── LevelUpScreen ────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void LevelUpScreen_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/LevelUpScreen.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void LevelUpScreen_HasDismissHint()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/LevelUpScreen.tscn");
        if (scene == null) { AssertThat(scene).IsNotNull(); return; }

        // LevelUpScreen builds its UI in _Ready, so we need to add it to the tree
        var tree = Engine.GetMainLoop() as SceneTree;
        tree!.Root.AddChild(scene);

        // Allow _Ready to fire
        var hint = scene.FindChild("DismissHint", recursive: true);
        AssertThat(hint).IsNotNull();

        scene.QueueFree();
    }

    // ── PauseMenu ────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void PauseMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/PauseMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── PartyMenu ────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void PartyMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/PartyMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── StatsMenu ────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void StatsMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/StatsMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── EquipmentMenu ────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void EquipmentMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/EquipmentMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── InventoryMenu ────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void InventoryMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/InventoryMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── SpellsMenu ───────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void SpellsMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/SpellsMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── CookingMenu ──────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void CookingMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/CookingMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── ShopMenu ─────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void ShopMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/ShopMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── ClassChangeMenu ──────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void ClassChangeMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/ClassChangeMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── BestiaryMenu ─────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void BestiaryMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/BestiaryMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── SaveSlotMenu ─────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void SaveSlotMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/SaveSlotMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── SettingsMenu ─────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void SettingsMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/SettingsMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── GameOver ─────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void GameOver_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/GameOver.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── MainMenu ─────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void MainMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<Node>("res://scenes/menus/MainMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── QuestRewardScreen ────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void QuestRewardScreen_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/QuestRewardScreen.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── ForageryMenu ─────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void ForageryMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/ForageryMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── CreditsMenu ──────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void CreditsMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/CreditsMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }

    // ── ResidencyShopMenu ────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void ResidencyShopMenu_Instantiates_WithoutCrash()
    {
        var scene = InstantiateScene<CanvasLayer>("res://scenes/menus/ResidencyShopMenu.tscn");
        AssertThat(scene).IsNotNull();
        scene?.QueueFree();
    }
}
