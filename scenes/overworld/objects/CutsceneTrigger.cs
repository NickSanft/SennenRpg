using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Place this node anywhere in a map scene to play a Dialogic timeline on scene load.
///
/// OnceFlag: if non-empty, the cutscene only plays if that GameManager flag is NOT set.
///           The flag is raised immediately before the timeline starts, so it won't
///           re-trigger if the player saves and loads while on the same map.
///
/// Example: opening monologue when the player first enters a new area.
/// </summary>
public partial class CutsceneTrigger : Node2D
{
	[Export] public string TimelinePath { get; set; } = "";

	/// <summary>Flag name that gates this cutscene. Leave empty to play every load.</summary>
	[Export] public string OnceFlag { get; set; } = "";

	public override void _Ready()
	{
		if (string.IsNullOrEmpty(TimelinePath)) return;
		if (!string.IsNullOrEmpty(OnceFlag) && GameManager.Instance.GetFlag(OnceFlag)) return;

		// Defer one frame so the map scene and player are fully spawned first
		CallDeferred(MethodName.PlayCutscene);
	}

	private void PlayCutscene()
	{
		if (DialogicBridge.Instance.IsRunning()) return;

		if (!string.IsNullOrEmpty(OnceFlag))
			GameManager.Instance.SetFlag(OnceFlag, true);

		GameManager.Instance.SetState(GameState.Dialog);
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnTimelineEnded));
		DialogicBridge.Instance.StartTimelineWithFlags(TimelinePath);

		GD.Print($"[CutsceneTrigger] Playing '{TimelinePath}' (once={!string.IsNullOrEmpty(OnceFlag)}).");
	}

	private void OnTimelineEnded()
	{
		GameManager.Instance.SetState(GameState.Overworld);
	}
}
