using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Place this node in a map scene to define a named player spawn position.
/// OverworldBase discovers all SpawnPoint nodes automatically in _Ready() —
/// no code changes needed in the map subclass.
///
/// Set SpawnId = "default" to replace the map's fallback spawn position.
/// SpawnPoint nodes take precedence over positions registered in code.
/// </summary>
public partial class SpawnPoint : Node2D
{
	/// <summary>Matches MapExit.TargetSpawnId on the trigger that sends the player here.</summary>
	[Export] public string SpawnId { get; set; } = "default";

	public override void _Ready()
	{
		AddToGroup("spawn_points");
	}
}
