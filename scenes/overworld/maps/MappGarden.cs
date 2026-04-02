using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// The backyard garden behind the MAPP tavern.
/// A small enclosed stone-walled yard at perpetual twilight — overgrown but
/// lovingly tended, with fireflies, wildflowers, and a few unusual residents.
///
/// All static visuals are built procedurally. NPCs are placed in MappGarden.tscn.
/// </summary>
[Tool]
public partial class MappGarden : OverworldBase
{
	private const string GardenBgmPath      = "res://assets/audio/bgm/garden_theme.ogg";
	private const string GardenAmbiencePath = "res://assets/audio/sfx/garden_ambience.ogg";

	// Garden world bounds (used by multiple helpers)
	private const float WallX    = 112f;  // east/west wall inner face
	private const float WallYN   = -80f;  // north wall inner face
	private const float WallYS   =  80f;  // south wall inner face
	private const float DoorHalf =  16f;  // half-width of south door opening

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			BuildEditorVisuals();
			return;
		}

		MapId   = "mapp_garden";
		BgmPath = ResourceLoader.Exists(GardenBgmPath) ? GardenBgmPath : "";

		// Spawn point when arriving from the MAPP back door
		SpawnPoints["from_mapp_backyard"] = new Vector2(0f, 60f);
		SpawnPoints["default"]            = new Vector2(0f, 60f);
		DefaultSpawnPosition              = new Vector2(0f, 60f);

		base._Ready();

		SpawnGround();
		SpawnPerimeterWalls();
		SpawnGardenWallColliders();
		SpawnGrassPatches();
		SpawnFlagstonePathway();
		SpawnSouthDoor();
		SpawnMoonflowerTrellis();
		SpawnBirdbath();
		SpawnLanterns();
		SpawnFireflies();
		SpawnWell();
		SpawnBench();
		SpawnHive();
		SpawnStatue();
		SpawnHerbGarden();
		SpawnCompostHeap();
		SpawnEntryFade();

		if (ResourceLoader.Exists(GardenAmbiencePath))
			AudioManager.Instance.PlayAmbience(GardenAmbiencePath, fadeTime: 1.5f);
	}

	// ── Editor preview ─────────────────────────────────────────────────────────

	private void BuildEditorVisuals()
	{
		if (GetNodeOrNull("GardenGround") != null) return;

		SpawnGround();
		SpawnPerimeterWalls();
		SpawnGrassPatches();
		SpawnFlagstonePathway();
		SpawnMoonflowerTrellis();
		SpawnBirdbath();
		SpawnLanterns();
	}

}
