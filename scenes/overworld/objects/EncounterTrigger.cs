using Godot;
using SennenRpg.Core.Data;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Area2D-based trigger. When the player walks in (or interacts with a door-type),
/// it starts a battle encounter. Set OneShot = true for scripted fights.
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

		GD.Print($"[EncounterTrigger] Encounter triggered. Enemy count: {EncounterResource.Enemies.Count}");
		_triggered = true;

		if (OneShot)
			QueueFree();

		await SceneTransition.Instance.ToBattleAsync(EncounterResource);
	}
}
