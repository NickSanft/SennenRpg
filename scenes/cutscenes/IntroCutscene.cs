using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Cutscenes;

/// <summary>
/// Plays the intro cutscene via a Dialogic timeline, then routes to CharacterCustomization.
///
/// The timeline uses [signal arg="bg:black|stars|world|title"] events to drive the background.
/// Each signal name maps to a preloaded placeholder texture in assets/sprites/ui/cutscene/.
///
/// Replace bg_*.png with final artwork; no code changes needed.
/// </summary>
public partial class IntroCutscene : Node2D
{
    private const string TimelinePath = "res://dialog/timelines/intro_cutscene.dtl";
    private const string NextScene    = "res://scenes/menus/CharacterCustomization.tscn";

    private static readonly string[] BgNames = ["black", "stars", "world", "title"];

    private TextureRect _bg = null!;

    public override void _Ready()
    {
        _bg = GetNode<TextureRect>("Bg");

        // Explicitly fill the viewport — Control anchors inside a Node2D are not reliable
        // across all Godot versions, so we use SetDeferred to avoid the
        // "non-equal opposite anchors" warning during _Ready.
        var vpSize = GetViewportRect().Size;
        GetNode<ColorRect>("BgColor").SetDeferred(Control.PropertyName.Size, vpSize);
        _bg.SetDeferred(Control.PropertyName.Size, vpSize);

        // Pre-load all placeholder backgrounds so swaps are instant.
        // Each [signal arg="bg:name"] event in the timeline calls SwitchBackground("name").
        DialogicBridge.Instance.DialogicSignalReceived += OnDialogicSignal;
        DialogicBridge.Instance.ConnectTimelineEnded(new Callable(this, MethodName.OnCutsceneEnded));
        DialogicBridge.Instance.StartTimeline(TimelinePath);
    }

    public override void _ExitTree()
    {
        // Unsubscribe in case the scene is freed before the timeline ends.
        if (DialogicBridge.Instance != null)
            DialogicBridge.Instance.DialogicSignalReceived -= OnDialogicSignal;
    }

    private void OnDialogicSignal(Variant arg)
    {
        string sig = arg.AsString();
        if (!sig.StartsWith("bg:")) return;

        string name = sig.Substring(3);
        string path = $"res://assets/sprites/ui/cutscene/bg_{name}.png";
        if (ResourceLoader.Exists(path))
            _bg.Texture = GD.Load<Texture2D>(path);
    }

    private async void OnCutsceneEnded()
    {
        GameManager.Instance.SetFlag(Flags.IntroCutsceneSeen, true);
        DialogicBridge.Instance.DialogicSignalReceived -= OnDialogicSignal;
        await SceneTransition.Instance.GoToAsync(NextScene);
    }
}
