using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Plays a Dialogic timeline when the player walks into this Area2D.
/// Complements CutsceneTrigger (which fires on scene load) — use this for
/// story moments triggered mid-map, e.g. approaching a landmark for the first time.
///
/// OnceFlag: if non-empty, the trigger fires only once. The flag is set before the
///           timeline starts so a save-and-reload on the same map won't re-trigger it.
///
/// Tip: size the CollisionShape2D to match the area you want to activate the cutscene.
/// The default RectangleShape2D (32×32) can be resized freely in the Godot inspector.
/// </summary>
public partial class WalkInTrigger : Area2D
{
	[Export] public string TimelinePath { get; set; } = "";

	/// <summary>Flag that gates this trigger. Leave empty to fire on every map load.</summary>
	[Export] public string OnceFlag { get; set; } = "";

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("player")) return;
		if (string.IsNullOrEmpty(TimelinePath)) return;
		if (!string.IsNullOrEmpty(OnceFlag) && GameManager.Instance.GetFlag(OnceFlag)) return;
		if (DialogicBridge.Instance.IsRunning()) return;

		if (!string.IsNullOrEmpty(OnceFlag))
			GameManager.Instance.SetFlag(OnceFlag, true);

		GameManager.Instance.SetState(GameState.Dialog);

		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnTimelineEnded));
		DialogicBridge.Instance.StartTimelineWithFlags(TimelinePath);

		GD.Print($"[WalkInTrigger] Playing '{TimelinePath}' (once={!string.IsNullOrEmpty(OnceFlag)}).");
	}

	private void OnTimelineEnded()
	{
		GameManager.Instance.SetState(GameState.Overworld);
	}
}
