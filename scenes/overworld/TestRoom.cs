using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// The starting room. Registers spawn points so MapExits from other rooms
/// land in the right place. DefaultSpawnPosition is used on fresh game start.
/// </summary>
public partial class TestRoom : OverworldBase
{
	public override void _Ready()
	{
		MapId = "test_room";

		// Player arrives here after walking back from Room2
		SpawnPoints["from_room2"] = new Vector2(240, 120);

		// Save point spawn — player loads here after saving in this room
		SpawnPoints["test_room"] = new Vector2(200, 100);

		DefaultSpawnPosition = new Vector2(100, 100);

		base._Ready();
	}
}
