using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

public partial class Npc : CharacterBody2D, IInteractable
{
	[Export] public string TimelinePath { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "???";

	public void Interact(Node player)
	{
		if (string.IsNullOrEmpty(TimelinePath)) return;
		if (DialogicBridge.Instance.IsRunning()) return;  // Prevent double-trigger

		GameManager.Instance.SetState(GameState.Dialog);

		// Pass any game state that the timeline's Condition events might need
		DialogicBridge.Instance.SetVariable("npc_name", DisplayName);
		DialogicBridge.Instance.SetVariable("kill_count", GameManager.Instance.TotalKills);

		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnTimelineEnded));

		DialogicBridge.Instance.StartTimeline(TimelinePath);
	}

	public string GetInteractPrompt() => $"Talk to {DisplayName}";

	private void OnTimelineEnded()
	{
		GameManager.Instance.SetState(GameState.Overworld);
	}
}
