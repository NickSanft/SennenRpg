using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Core.Data;

/// <summary>
/// Holds map navigation and world-map state.
/// Plain C# class — owned by GameManager as an internal domain.
/// </summary>
public class WorldStateData
{
	public string   LastMapPath         { get; set; } = "";
	public string   LastSavePointId     { get; set; } = "";
	public string   LastSpawnId         { get; set; } = "";
	public Vector2I WorldMapSpawnTile   { get; set; } = new Vector2I(10, 8);
	public Vector2I WorldMapReturnTile  { get; set; } = Vector2I.Zero;
	public bool     IsNight             { get; set; } = false;
	public int      TilesWalkedOnWorldMap { get; set; } = 0;
	public int      RepelStepsRemaining { get; set; } = 0;

	/// <summary>Player position saved before a battle, restored on return. Reset to null after use.</summary>
	public Vector2? BattleReturnPosition { get; set; } = null;

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	public void Reset()
	{
		LastMapPath           = "";
		LastSavePointId       = "";
		LastSpawnId           = "";
		WorldMapSpawnTile     = new Vector2I(10, 8);
		WorldMapReturnTile    = Vector2I.Zero;
		IsNight               = false;
		TilesWalkedOnWorldMap = 0;
		RepelStepsRemaining   = 0;
	}

	public void ApplyFromSave(SaveData data)
	{
		LastMapPath           = data.LastMapPath;
		LastSavePointId       = data.LastSavePointId;
		LastSpawnId           = data.LastSpawnId;
		WorldMapSpawnTile     = new Vector2I(data.WorldMapSpawnTileX, data.WorldMapSpawnTileY);
		IsNight               = data.IsNight;
		TilesWalkedOnWorldMap = data.TilesWalkedOnWorldMap;
	}
}
