using Godot;
using SennenRpg.Core.Data;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Area2D-based trigger. When the player walks in it starts a battle encounter.
/// Set OneShot = true for scripted fights (trigger removes itself after firing).
/// </summary>
public partial class EncounterTrigger : Area2D
{
	[Export] public EncounterData? EncounterResource { get; set; }
	[Export] public bool OneShot { get; set; } = false;

	private bool _triggered = false;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private async void OnBodyEntered(Node2D body)
	{
		if (_triggered) return;
		if (!body.IsInGroup("player")) return;

		if (EncounterResource == null)
		{
			GD.PushWarning("[EncounterTrigger] No EncounterResource assigned — nothing to battle.");
			return;
		}

		int enemyCount = EncounterResource.Enemies?.Count ?? 0;
		GD.Print($"[EncounterTrigger] Triggered. Resource: '{EncounterResource.ResourcePath}', Enemies: {enemyCount}");

		// Set flag before await so re-entry is blocked while transitioning
		_triggered = true;

		try
		{
			if (OneShot)
				QueueFree();

			await SceneTransition.Instance.ToBattleAsync(EncounterResource);
		}
		catch (System.Exception e)
		{
			// If the transition fails, reset the flag so the trigger remains usable
			GD.PushError($"[EncounterTrigger] Transition failed: {e.Message}");
			_triggered = false;
		}
	}
}
