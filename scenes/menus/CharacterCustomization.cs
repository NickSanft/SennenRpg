using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Character customization screen — class, stat allocation, and colour scheme.
/// Stub: immediately continues to MAPP until steps 13–14 are implemented.
/// </summary>
public partial class CharacterCustomization : Node2D
{
    public override void _Ready()
    {
        var btn = GetNodeOrNull<Button>("UI/Center/VBox/ContinueButton");
        if (btn != null)
            btn.Pressed += OnContinuePressed;
    }

    private void OnContinuePressed()
        => _ = SceneTransition.Instance.GoToAsync("res://scenes/overworld/MAPP.tscn");
}
