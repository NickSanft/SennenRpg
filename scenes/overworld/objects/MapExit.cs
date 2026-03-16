using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Transitions the player to another map.
/// AutoTrigger = true  → fires on body_entered (walk-in exits, like Undertale room edges).
/// AutoTrigger = false → waits for the player to press interact (doors).
/// </summary>
public partial class MapExit : Area2D, IInteractable
{
	[Export] public string TargetMapPath { get; set; } = "";
	[Export] public string TargetSpawnId { get; set; } = "default";
	[Export] public bool AutoTrigger { get; set; } = false;

	private bool _triggered = false;

	public override void _Ready()
	{
		if (!AutoTrigger)
			AddToGroup("interactable");

		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!AutoTrigger) return;
		if (body.IsInGroup("player") || body is CharacterBody2D)
			TriggerExit();
	}

	public void Interact(Node player) => TriggerExit();

	public string GetInteractPrompt() => "Enter";

	private void TriggerExit()
	{
		if (_triggered || string.IsNullOrEmpty(TargetMapPath)) return;
		_triggered = true;

		GameManager.Instance.SetLastSpawn(TargetSpawnId);
		_ = SceneTransition.Instance.GoToAsync(TargetMapPath);
	}
}
