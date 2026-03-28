using Godot;
using Godot.Collections;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

public enum GameState { Boot, MainMenu, Overworld, Battle, Dialog, Paused }

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; } = null!;

	[Signal] public delegate void GameStateChangedEventHandler(GameState newState);
	[Signal] public delegate void PlayerStatsChangedEventHandler();

	public GameState CurrentState { get; private set; } = GameState.Boot;

	public string PlayerName { get; private set; } = "Sen";
	public int Gold { get; private set; }
	public int Exp  { get; private set; }
	public string LastMapPath { get; private set; } = "";
	public string LastSavePointId { get; private set; } = "";
	/// <summary>Spawn point ID to use when the next map loads. Set by MapExit before transitioning.</summary>
	public string LastSpawnId { get; private set; } = "";

	// Player stats — loaded from resources/characters/player_stats.tres on boot
	public CharacterStats PlayerStats { get; private set; } = new CharacterStats();

	// Story flags: keyed by string, all booleans
	public Godot.Collections.Dictionary<string, bool> Flags { get; private set; } = new();

	// Inventory: list of item resource paths (stored as strings for save/load simplicity)
	public Array<string> InventoryItemPaths { get; private set; } = new();

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always; // Survive scene changes

		// Load default player stats if the resource exists
		const string statsPath = "res://resources/characters/player_stats.tres";
		if (ResourceLoader.Exists(statsPath))
		{
			var loaded = GD.Load<CharacterStats>(statsPath);
			PlayerStats = (CharacterStats)loaded.Duplicate(); // Local copy so we can modify HP
		}
	}

	public void SetState(GameState newState)
	{
		CurrentState = newState;
		EmitSignal(SignalName.GameStateChanged, (int)newState);
	}

	public void SetLastMap(string scenePath) => LastMapPath = scenePath;
	public void SetLastSavePoint(string savePointId) => LastSavePointId = savePointId;
	public void SetLastSpawn(string spawnId) => LastSpawnId = spawnId;

	public void AddGold(int amount)
	{
		Gold += amount;
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public void RemoveGold(int amount)
	{
		Gold = Mathf.Max(0, Gold - amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}
	public void AddExp(int amount)  => Exp  += amount;

	public void AddItem(string resourcePath)    => InventoryItemPaths.Add(resourcePath);
	public bool RemoveItem(string resourcePath)
	{
		int idx = InventoryItemPaths.IndexOf(resourcePath);
		if (idx < 0) return false;
		InventoryItemPaths.RemoveAt(idx);
		return true;
	}

	/// <summary>Reduce player HP and emit PlayerStatsChanged so the HUD updates.</summary>
	public void HurtPlayer(int amount)
	{
		PlayerStats.CurrentHp = Mathf.Max(0, PlayerStats.CurrentHp - amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	/// <summary>Restore player HP (capped at MaxHp) and emit PlayerStatsChanged.</summary>
	public void HealPlayer(int amount)
	{
		PlayerStats.CurrentHp = Mathf.Min(PlayerStats.MaxHp, PlayerStats.CurrentHp + amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public bool GetFlag(string key) => Flags.TryGetValue(key, out bool val) && val;
	public void SetFlag(string key, bool value) => Flags[key] = value;

	/// <summary>Resets all runtime state for a fresh new game.</summary>
	public void ResetForNewGame()
	{
		Gold = 100;
		Exp  = 0;
		LastMapPath = "";
		LastSavePointId = "";
		LastSpawnId = "";
		Flags.Clear();
		InventoryItemPaths.Clear();
		InventoryItemPaths.Add("res://resources/items/item_001.tres"); // starting Bandage
		const string statsPath = "res://resources/characters/player_stats.tres";
		if (ResourceLoader.Exists(statsPath))
			PlayerStats = (CharacterStats)GD.Load<CharacterStats>(statsPath).Duplicate();
		GD.Print("[GameManager] Reset for new game.");
	}

	public void ApplySaveData(SaveData data)
	{
		Gold = data.Gold;
		Exp  = data.Exp;
		LastMapPath = data.LastMapPath;
		LastSavePointId = data.LastSavePointId;
		LastSpawnId = data.LastSpawnId;
		Flags = new Godot.Collections.Dictionary<string, bool>(data.Flags);

		PlayerStats.CurrentHp   = data.PlayerHp;
		PlayerStats.MaxHp       = data.PlayerMaxHp;
		PlayerStats.Attack      = data.PlayerAttack;
		PlayerStats.Defense     = data.PlayerDefense;
		PlayerStats.Speed       = data.PlayerSpeed;
		PlayerStats.Magic       = data.PlayerMagic;
		PlayerStats.Resistance  = data.PlayerResistance;
		PlayerStats.Luck        = data.PlayerLuck;

		InventoryItemPaths.Clear();
		foreach (var path in data.InventoryItemPaths)
			InventoryItemPaths.Add(path);
	}
}
