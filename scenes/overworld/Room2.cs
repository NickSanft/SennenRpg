using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Second room. Inherits all overworld logic from OverworldBase.
/// Register named spawn points here before calling base._Ready() so
/// MapExit transitions land in the right place.
/// </summary>
public partial class Room2 : OverworldBase
{
	public override void _Ready()
	{
		MapId = "room2";

		// Register the spawn point that TestRoom's MapExit will target.
		// The player arrives here after walking through the exit in TestRoom.
		SpawnPoints["from_test_room"] = new Vector2(120, 80);
		DefaultSpawnPosition = new Vector2(120, 80);

		base._Ready();
	}
}
