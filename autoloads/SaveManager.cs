using Godot;
using System.Text.Json;

namespace SennenRpg.Autoloads;

public record SaveData
{
	public int PlayerHp         { get; init; }
	public int PlayerMaxHp      { get; init; }
	public int PlayerAttack     { get; init; }
	public int PlayerDefense    { get; init; }
	public int PlayerSpeed      { get; init; }
	public int PlayerMagic      { get; init; }
	public int PlayerResistance { get; init; }
	public int PlayerLuck       { get; init; }
	public int PlayerMaxMp      { get; init; }
	public int PlayerMp         { get; init; }
	public int PlayerLevel      { get; init; } = 1;
	public int Gold { get; init; }
	public int Exp  { get; init; }
	public string LastMapPath { get; init; } = "";
	public string LastSavePointId { get; init; } = "";
	public string LastSpawnId { get; init; } = "";
	public Dictionary<string, bool> Flags { get; init; } = new();
	public List<string>              InventoryItemPaths  { get; init; } = new();
	public List<string>              KnownSpellPaths     { get; init; } = new();
	public List<string>              OwnedEquipmentPaths { get; init; } = new();
	/// <summary>Slot enum name → resource path. Empty string means nothing equipped in that slot.</summary>
	public Dictionary<string, string> EquippedItemPaths  { get; init; } = new();

	// World map / progression state
	public int    WorldMapSpawnTileX    { get; init; } = 10;
	public int    WorldMapSpawnTileY    { get; init; } = 8;
	public bool   IsNight               { get; init; } = false;
	public int    TilesWalkedOnWorldMap { get; init; } = 0;
	public Dictionary<string, int>  KillCounts        { get; init; } = new();
	public List<string>             ActiveQuestIds    { get; init; } = new();
	public List<string>             CompletedQuestIds { get; init; } = new();
}

public partial class SaveManager : Node
{
	public static SaveManager Instance { get; private set; } = null!;

	private const string SavePath = "user://save.json";

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
	}

	public bool HasSave() => Godot.FileAccess.FileExists(SavePath);

	public void SaveGame()
	{
		var gm = GameManager.Instance;
		var data = new SaveData
		{
			PlayerHp         = gm.PlayerStats.CurrentHp,
			PlayerMaxHp      = gm.PlayerStats.MaxHp,
			PlayerAttack     = gm.PlayerStats.Attack,
			PlayerDefense    = gm.PlayerStats.Defense,
			PlayerSpeed      = gm.PlayerStats.Speed,
			PlayerMagic      = gm.PlayerStats.Magic,
			PlayerResistance = gm.PlayerStats.Resistance,
			PlayerLuck       = gm.PlayerStats.Luck,
			PlayerMaxMp      = gm.PlayerStats.MaxMp,
			PlayerMp         = gm.PlayerStats.CurrentMp,
			PlayerLevel      = gm.PlayerLevel,
			Gold = gm.Gold,
			Exp  = gm.Exp,
			LastMapPath = gm.LastMapPath,
			LastSavePointId = gm.LastSavePointId,
			LastSpawnId = gm.LastSavePointId, // spawn at save point on load
			Flags = new Dictionary<string, bool>(gm.Flags),
				InventoryItemPaths  = new List<string>(gm.InventoryItemPaths),
			KnownSpellPaths     = new List<string>(gm.KnownSpellPaths),
			OwnedEquipmentPaths = new List<string>(gm.OwnedEquipmentPaths),
			EquippedItemPaths   = SerialiseEquipped(gm.EquippedItemPaths),
			WorldMapSpawnTileX    = gm.WorldMapSpawnTile.X,
			WorldMapSpawnTileY    = gm.WorldMapSpawnTile.Y,
			IsNight               = gm.IsNight,
			TilesWalkedOnWorldMap = gm.TilesWalkedOnWorldMap,
			KillCounts            = new Dictionary<string, int>(gm.KillCounts),
		};

		string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
		using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
		file.StoreString(json);

		GD.Print($"[SaveManager] Game saved to {SavePath}");
	}

	public SaveData? LoadGame()
	{
		if (!HasSave()) return null;

		using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
		string json = file.GetAsText();
		return JsonSerializer.Deserialize<SaveData>(json);
	}

	public void ApplyLoadedData(SaveData data)
	{
		GameManager.Instance.ApplySaveData(data);
	}

	private static Dictionary<string, string> SerialiseEquipped(
		System.Collections.Generic.Dictionary<SennenRpg.Core.Data.EquipmentSlot, string> equipped)
	{
		var result = new Dictionary<string, string>();
		foreach (var kv in equipped)
			result[kv.Key.ToString()] = kv.Value;
		return result;
	}
}
