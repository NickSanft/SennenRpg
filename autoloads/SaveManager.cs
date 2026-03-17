using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace SennenRpg.Autoloads;

public record SaveData
{
	public int PlayerHp { get; init; }
	public int PlayerMaxHp { get; init; }
	public int Gold { get; init; }
	public int Exp  { get; init; }
	public int Love { get; init; }
	public int TotalKills { get; init; }
	public int Route { get; init; }
	public string LastMapPath { get; init; } = "";
	public string LastSavePointId { get; init; } = "";
	public string LastSpawnId { get; init; } = "";
	public Dictionary<string, bool> Flags { get; init; } = new();
	public List<string> InventoryItemPaths { get; init; } = new();
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
			PlayerHp = gm.PlayerStats.CurrentHp,
			PlayerMaxHp = gm.PlayerStats.MaxHp,
			Gold = gm.Gold,
			Exp  = gm.Exp,
			Love = gm.Love,
			TotalKills = gm.TotalKills,
			Route = (int)gm.CurrentRoute,
			LastMapPath = gm.LastMapPath,
			LastSavePointId = gm.LastSavePointId,
			LastSpawnId = gm.LastSavePointId, // spawn at save point on load
			Flags = new Dictionary<string, bool>(gm.Flags),
			InventoryItemPaths = new List<string>(gm.InventoryItemPaths),
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
		var gm = GameManager.Instance;
		gm.PlayerStats.CurrentHp = data.PlayerHp;
		gm.PlayerStats.MaxHp = data.PlayerMaxHp;

		// Use reflection-safe setters — add public setters or internal methods to GameManager
		// for Gold, Love, TotalKills, Route, LastMapPath, Flags as needed.
		// For now, GameManager will expose Apply(SaveData) method:
		gm.ApplySaveData(data);
	}
}
