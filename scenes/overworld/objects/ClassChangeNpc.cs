using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Menus;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// NPC that lets the player change their class. Plays a dialog timeline first,
/// then opens the ClassChangeMenu. Follows the RorkTownNpc/VendorNpc pattern.
/// </summary>
[Tool]
public partial class ClassChangeNpc : Npc
{
    private ClassChangeMenu? _menu;

    protected override string PromptText => "[Z] Change Class";

    public override void Interact(Node player)
    {
        if (Engine.IsEditorHint()) return;
        if (DialogicBridge.Instance.IsRunning()) return;

        if (player is Node2D p2d)
            FaceToward(p2d.GlobalPosition);

        GameManager.Instance.SetState(GameState.Dialog);

        string timeline = Npc.SelectTimeline(TimelinePath, AltRequiredFlags, AltTimelinePaths,
            GameManager.Instance.GetFlag);

        // AutoRevisit: route to _again variant after first talk
        if (timeline == TimelinePath && !string.IsNullOrEmpty(NpcId))
        {
            bool talked = GameManager.Instance.GetFlag(Flags.TalkedTo(NpcId));
            string revisit = NpcLogic.GetRevisitPath(TimelinePath, talked);
            if (revisit != TimelinePath && ResourceLoader.Exists(revisit))
                timeline = revisit;
        }

        if (!string.IsNullOrEmpty(timeline))
        {
            DialogicBridge.Instance.ConnectTimelineEnded(Callable.From(OnTalkEnded));
            DialogicBridge.Instance.StartTimelineWithFlags(timeline);
        }
        else
        {
            OpenMenu();
        }
    }

    private void OnTalkEnded()
    {
        if (!string.IsNullOrEmpty(NpcId))
            GameManager.Instance.SetFlag(Flags.TalkedTo(NpcId), true);
        OpenMenu();
    }

    private void OpenMenu()
    {
        var scene = GD.Load<PackedScene>("res://scenes/menus/ClassChangeMenu.tscn");
        _menu = scene.Instantiate<ClassChangeMenu>();
        GetTree().Root.AddChild(_menu);
        _menu.Closed += OnMenuClosed;
        _menu.Open();

        GD.Print("[ClassChangeNpc] Class change menu opened.");
    }

    private void OnMenuClosed()
    {
        _menu?.QueueFree();
        _menu = null;
        PlayFacingIdle(DefaultFacing);
        GameManager.Instance.SetState(GameState.Overworld);
    }
}
