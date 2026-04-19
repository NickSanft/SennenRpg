using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;
using SennenRpg.Scenes.Menus;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Interactable jukebox prop for MAPP Tavern. Opens <see cref="JukeboxMenu"/>
/// to let the player replay any BGM they have heard during their adventure.
/// </summary>
[Tool]
public partial class JukeboxProp : Area2D, IInteractable
{
    private InteractPromptBubble? _prompt;
    private bool _open;

    public override void _Ready()
    {
        if (GetChildCount() > 1) return; // CollisionShape2D is pre-baked in .tscn

        if (!Engine.IsEditorHint())
            AddToGroup("interactable");

        // Simple visual: a small colored rectangle representing the jukebox
        var body = new Polygon2D { Color = new Color(0.3f, 0.2f, 0.5f) };
        body.Polygon = [
            new Vector2(-8, -12), new Vector2(8, -12),
            new Vector2(8, 8),    new Vector2(-8, 8),
        ];
        AddChild(body);

        // Accent strip
        var accent = new Polygon2D { Color = new Color(1f, 0.85f, 0.1f) };
        accent.Polygon = [
            new Vector2(-6, -10), new Vector2(6, -10),
            new Vector2(6, -7),   new Vector2(-6, -7),
        ];
        AddChild(accent);

        _prompt = new InteractPromptBubble("[Z] Jukebox");
        _prompt.Position = new Vector2(0, -24);
        AddChild(_prompt);
    }

    public void ShowPrompt() => _prompt?.ShowBubble();
    public void HidePrompt() => _prompt?.HideBubble();
    public string GetInteractPrompt() => "Jukebox";

    public void Interact(Node player)
    {
        if (_open) return;
        _open = true;
        HidePrompt();
        GameManager.Instance.SetState(GameState.Dialog);

        var menu = new JukeboxMenu();
        menu.Closed += OnMenuClosed;
        GetTree().CurrentScene.AddChild(menu);
        menu.Open();
    }

    private void OnMenuClosed()
    {
        _open = false;
        GameManager.Instance.SetState(GameState.Overworld);
        ShowPrompt();
    }
}
