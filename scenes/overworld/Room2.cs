using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Second room. Contains two roaming enemy pawns spawned programmatically:
///   - A Stonecrawler (slow, high-DEF, column-volley pattern) in the centre.
///   - A Flickerfly  (fast, fragile, zigzag pattern)          near the far wall.
///
/// Spawn points are registered before base._Ready() so MapExit transitions
/// land in the right place.
/// </summary>
public partial class Room2 : OverworldBase
{
	private const string PawnScene    = "res://scenes/overworld/objects/EnemyPawn.tscn";
	private const string Encounter005 = "res://resources/encounters/encounter_005.tres";
	private const string Encounter006 = "res://resources/encounters/encounter_006.tres";

	public override void _Ready()
	{
		MapId                         = "room2";
		SpawnPoints["from_test_room"] = new Vector2(120, 80);
		DefaultSpawnPosition          = new Vector2(120, 80);

		base._Ready();

		SpawnEnemyPawns();
	}

	private void SpawnEnemyPawns()
	{
		var pawnScene = GD.Load<PackedScene>(PawnScene);

		// ── Stonecrawler ──────────────────────────────────────────────
		// Slow patrol speed, wide detection so the player has plenty of
		// warning. The column-volley pattern rewards reading the gaps.
		var stonecrawler = pawnScene.Instantiate<EnemyPawn>();
		stonecrawler.EncounterResource = GD.Load<EncounterData>(Encounter005);
		stonecrawler.DetectionRadius   = 100f;
		stonecrawler.ChaseSpeed        = 42f;
		stonecrawler.PersistenceFlag   = "defeated_room2_stonecrawler";
		YSort.AddChild(stonecrawler);
		stonecrawler.GlobalPosition    = new Vector2(300, 190);

		// ── Flickerfly ────────────────────────────────────────────────
		// Fast and jumpy — narrow detection radius but very high chase
		// speed, so if the player wanders too close they really need to
		// run. The zigzag pattern is tricky to dodge at low HP.
		var flickerfly = pawnScene.Instantiate<EnemyPawn>();
		flickerfly.EncounterResource   = GD.Load<EncounterData>(Encounter006);
		flickerfly.DetectionRadius     = 64f;
		flickerfly.ChaseSpeed          = 95f;
		flickerfly.PersistenceFlag     = "defeated_room2_flickerfly";
		YSort.AddChild(flickerfly);
		flickerfly.GlobalPosition      = new Vector2(460, 140);
	}
}
