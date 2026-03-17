using Godot;
using Godot.Collections;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

public enum GameState { Boot, MainMenu, Overworld, Battle, Dialog, Paused }
public enum RouteType { Neutral, Pacifist, Genocide }

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; } = null!;

	[Signal] public delegate void GameStateChangedEventHandler(GameState newState);
	[Signal] public delegate void RouteChangedEventHandler(RouteType route);
	[Signal] public delegate void PlayerStatsChangedEventHandler();

	public GameState CurrentState { get; private set; } = GameState.Boot;
	public RouteType CurrentRoute { get; private set; } = RouteType.Neutral;

	public string PlayerName { get; private set; } = "Foran";
	public int TotalKills { get; private set; }
	public int Gold { get; private set; }
	public int Love { get; private set; } = 1;          // LV (Level of Violence) starts at 1
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

	public void RegisterKill()
	{
		TotalKills++;
		RecalculateRoute();
	}

	public void RegisterSpare()
	{
		// Sparing does not affect kill count but may affect flags
		RecalculateRoute();
	}

	public void AddGold(int amount) => Gold += amount;

	/// <summary>Reduce player HP and emit PlayerStatsChanged so the HUD updates.</summary>
	public void HurtPlayer(int amount)
	{
		PlayerStats.CurrentHp = Mathf.Max(0, PlayerStats.CurrentHp - amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public bool GetFlag(string key) => Flags.TryGetValue(key, out bool val) && val;
	public void SetFlag(string key, bool value) => Flags[key] = value;

	private void RecalculateRoute()
	{
		// Love (LV) scales with kills — expand thresholds as areas are built
		Love = TotalKills switch
		{
			0    => 1,
			< 10 => 2,
			< 20 => 3,
			_    => 4
		};
		EmitSignal(SignalName.PlayerStatsChanged); // HUD shows LV

		var newRoute = TotalKills switch
		{
			0     => RouteType.Pacifist,
			>= 20 => RouteType.Genocide,
			_     => RouteType.Neutral
		};

		if (newRoute != CurrentRoute)
		{
			CurrentRoute = newRoute;
			EmitSignal(SignalName.RouteChanged, (int)newRoute);
		}
	}
	
	/// <summary>Resets all runtime state for a fresh new game.</summary>
	public void ResetForNewGame()
	{
		TotalKills = 0;
		Gold = 0;
		Love = 1;
		LastMapPath = "";
		LastSavePointId = "";
		LastSpawnId = "";
		Flags.Clear();
		InventoryItemPaths.Clear();
		CurrentRoute = RouteType.Neutral;
		const string statsPath = "res://resources/characters/player_stats.tres";
		if (ResourceLoader.Exists(statsPath))
			PlayerStats = (CharacterStats)GD.Load<CharacterStats>(statsPath).Duplicate();
		GD.Print("[GameManager] Reset for new game.");
	}

	public void ApplySaveData(SaveData data)
	{
		Gold = data.Gold;
		Love = data.Love;
		TotalKills = data.TotalKills;
		CurrentRoute = (RouteType)data.Route;
		LastMapPath = data.LastMapPath;
		LastSavePointId = data.LastSavePointId;
		LastSpawnId = data.LastSpawnId;
		Flags = new Godot.Collections.Dictionary<string, bool>(data.Flags);
		PlayerStats.CurrentHp = data.PlayerHp;
		PlayerStats.MaxHp = data.PlayerMaxHp;
	}
}
