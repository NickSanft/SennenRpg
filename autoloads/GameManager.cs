using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using SennenRpg.Core.Data;
using SennenRpg.Core.Extensions;
using EquipDict = System.Collections.Generic.Dictionary<SennenRpg.Core.Data.EquipmentSlot, string>;

namespace SennenRpg.Autoloads;

public enum GameState { Boot, MainMenu, Overworld, Battle, Dialog, Paused }

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; } = null!;

	[Signal] public delegate void GameStateChangedEventHandler(GameState newState);
	[Signal] public delegate void PlayerStatsChangedEventHandler();
	/// <summary>Fired after level-up stat rolls are applied. Read PendingLevelUps for results.</summary>
	[Signal] public delegate void PlayerLeveledUpEventHandler();

	// ── Internal domains ──────────────────────────────────────────────────────

	private readonly PlayerProgressionData _progression = new();
	private readonly PlayerCombatData      _combat      = new();
	private readonly InventoryData         _inventory   = new();
	private readonly WorldStateData        _world       = new();
	private readonly MellyrRewardData      _mellyr      = new();

	// ── State ─────────────────────────────────────────────────────────────────

	public GameState CurrentState { get; private set; } = GameState.Boot;

	public string PlayerName { get; private set; } = "Sen";

	// Story flags: keyed by string, all booleans
	public Godot.Collections.Dictionary<string, bool> Flags { get; private set; } = new();

	// Kill tracking: keyed by EnemyData.EnemyId
	public System.Collections.Generic.Dictionary<string, int> KillCounts { get; } = new();

	/// <summary>Original palette colours extracted from the sprite at character creation.</summary>
	public Color[] PaletteSourceColors { get; set; } = [];
	/// <summary>Replacement colours chosen by the player, parallel to PaletteSourceColors.</summary>
	public Color[] PaletteTargetColors { get; set; } = [];

	// ── Progression pass-through ──────────────────────────────────────────────

	public int Gold                              => _progression.Gold;
	public int Exp                               => _progression.Exp;
	public int PlayerLevel                       => _progression.PlayerLevel;
	public int PlayTimeSeconds                   => _progression.PlayTimeSeconds;
	public List<LevelUpResult> PendingLevelUps   => _progression.PendingLevelUps;

	// ── Combat pass-through ───────────────────────────────────────────────────

	public CharacterStats PlayerStats            => _combat.PlayerStats;

	// ── Inventory pass-through ────────────────────────────────────────────────

	public Array<string> InventoryItemPaths      => _inventory.InventoryItemPaths;
	public Array<string> KnownSpellPaths         => _inventory.KnownSpellPaths;
	public Array<string> OwnedEquipmentPaths     => _inventory.OwnedEquipmentPaths;
	public EquipDict     EquippedItemPaths       => _inventory.EquippedItemPaths;
	public List<DynamicEquipmentSave>                                   DynamicEquipmentInventory => _inventory.DynamicEquipmentInventory;
	public System.Collections.Generic.Dictionary<EquipmentSlot, string> EquippedDynamicItemIds    => _inventory.EquippedDynamicItemIds;

	// ── World state pass-through ──────────────────────────────────────────────

	public string   LastMapPath                  { get => _world.LastMapPath;           private set => _world.LastMapPath = value; }
	public string   LastSavePointId              { get => _world.LastSavePointId;       private set => _world.LastSavePointId = value; }
	public string   LastSpawnId                  { get => _world.LastSpawnId;           private set => _world.LastSpawnId = value; }
	public Vector2I WorldMapSpawnTile            { get => _world.WorldMapSpawnTile;     set => _world.WorldMapSpawnTile = value; }
	public Vector2I WorldMapReturnTile           { get => _world.WorldMapReturnTile;    set => _world.WorldMapReturnTile = value; }
	public bool     IsNight                      { get => _world.IsNight;               set => _world.IsNight = value; }
	public int      TilesWalkedOnWorldMap        { get => _world.TilesWalkedOnWorldMap; set => _world.TilesWalkedOnWorldMap = value; }
	public int      RepelStepsRemaining          { get => _world.RepelStepsRemaining;   set => _world.RepelStepsRemaining = value; }

	// ── Mellyr reward pass-through ────────────────────────────────────────────

	public int          TownStepCounter          { get => _mellyr.TownStepCounter;      set => _mellyr.TownStepCounter = value; }
	public int          PendingRainGold          { get => _mellyr.PendingRainGold;      set => _mellyr.PendingRainGold = value; }
	public List<string> PendingLilyRecipes       => _mellyr.PendingLilyRecipes;

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		_combat.LoadDefaults();
	}

	public override void _Process(double delta)
	{
		if (CurrentState is GameState.Overworld or GameState.Battle)
			_progression.Tick(delta);
	}

	// ── State management ──────────────────────────────────────────────────────

	public void SetState(GameState newState)
	{
		CurrentState = newState;
		EmitSignal(SignalName.GameStateChanged, (int)newState);
	}

	public void SetLastMap(string scenePath)       => _world.LastMapPath     = scenePath;
	public void SetLastSavePoint(string savePointId) => _world.LastSavePointId = savePointId;
	public void SetLastSpawn(string spawnId)       => _world.LastSpawnId     = spawnId;

	// ── Economy ───────────────────────────────────────────────────────────────

	public void AddGold(int amount)
	{
		_progression.AddGold(amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public void RemoveGold(int amount)
	{
		_progression.RemoveGold(amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public void AddExp(int amount)
	{
		_progression.AddExp(amount);
		CheckAndApplyLevelUp();
	}

	private void CheckAndApplyLevelUp()
	{
		int gained = LevelData.CheckLevelUp(Exp, PlayerLevel);
		if (gained == 0) return;

		PendingLevelUps.Clear();
		for (int i = 0; i < gained; i++)
		{
			_progression.IncrementLevel();
			PendingLevelUps.Add(_combat.RollGrowth(PlayerLevel));
		}

		EmitSignal(SignalName.PlayerLeveledUp);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	// ── Inventory & items ─────────────────────────────────────────────────────

	public void AddItem(string resourcePath)    => _inventory.AddItem(resourcePath);
	public bool RemoveItem(string resourcePath) => _inventory.RemoveItem(resourcePath);

	// ── Combat ────────────────────────────────────────────────────────────────

	public void HurtPlayer(int amount)
	{
		_combat.HurtPlayer(amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public void HealPlayer(int amount)
	{
		_combat.HealPlayer(amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public bool UseMp(int cost)
	{
		if (!_combat.UseMp(cost)) return false;
		EmitSignal(SignalName.PlayerStatsChanged);
		return true;
	}

	public void RestoreMp(int amount)
	{
		_combat.RestoreMp(amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	// ── Equipment ─────────────────────────────────────────────────────────────

	public void AddEquipment(string resourcePath) => _inventory.AddEquipment(resourcePath);

	public EquipmentData? GetEquipped(EquipmentSlot slot) => _inventory.GetEquipped(slot);

	public void Equip(EquipmentSlot slot, string path)
	{
		_inventory.Equip(slot, path);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public void Unequip(EquipmentSlot slot)
	{
		if (_inventory.Unequip(slot))
			EmitSignal(SignalName.PlayerStatsChanged);
	}

	public CharacterStats EffectiveStats
		=> _combat.ComputeEffectiveStats(
			_inventory.EquippedItemPaths,
			_inventory.EquippedDynamicItemIds,
			_inventory.DynamicEquipmentInventory);

	// ── Dynamic equipment ─────────────────────────────────────────────────────

	public void EquipDynamic(EquipmentSlot slot, string itemId)
	{
		_inventory.EquipDynamic(slot, itemId);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public void UnequipDynamic(EquipmentSlot slot)
	{
		_inventory.UnequipDynamic(slot);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	// ── Mellyr Outpost reward collection ─────────────────────────────────────

	public int CollectRainRewards()
	{
		int amount = _mellyr.CollectRainRewards();
		if (amount > 0) AddGold(amount);
		return amount;
	}

	public List<DynamicEquipmentSave> CollectLilyRewards()
		=> _inventory.CollectLilyRewards(_mellyr.PendingLilyRecipes);

	public (int gold, List<DynamicEquipmentSave> items) CollectAllTownRewards()
		=> (CollectRainRewards(), CollectLilyRewards());

	// ── Flags & kill tracking ─────────────────────────────────────────────────

	public bool GetFlag(string key) => Flags.TryGetValue(key, out bool val) && val;

	public void SetFlag(string key, bool value)
	{
		Flags[key] = value;
		QuestManager.Instance?.NotifyFlagChanged(key);
	}

	public void RecordKill(string enemyId)
	{
		KillCounts[enemyId] = KillCounts.GetValueOrDefault(enemyId, 0) + 1;
		GD.Print($"[GameManager] Kill recorded: {enemyId} (total: {KillCounts[enemyId]})");
		QuestManager.Instance?.NotifyKill(enemyId);
	}

	// ── Character customization ───────────────────────────────────────────────

	public void ApplyCharacterCustomization(CharacterStats stats, Color[] sourceColors, Color[] targetColors)
	{
		_combat.ApplyCustomization(stats);
		PaletteSourceColors = sourceColors;
		PaletteTargetColors = targetColors;
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	// ── New-game / save-load ──────────────────────────────────────────────────

	public void ResetForNewGame()
	{
		_progression.Reset();
		_combat.Reset();
		_inventory.Reset();
		_world.Reset();
		_mellyr.Reset();

		Flags.Clear();
		KillCounts.Clear();
		PaletteSourceColors = [];
		PaletteTargetColors = [];

		GD.Print("[GameManager] Reset for new game.");
	}

	public void ApplySaveData(SaveData data)
	{
		_progression.ApplyFromSave(data.Gold, data.Exp, data.PlayerLevel, data.PlayTimeSeconds);
		_combat.ApplyFromSave(data);
		_inventory.ApplyFromSave(data);
		_world.ApplyFromSave(data);
		_mellyr.ApplyFromSave(data);

		Flags = new Godot.Collections.Dictionary<string, bool>(data.Flags);
		KillCounts.Clear();
		foreach (var kv in data.KillCounts) KillCounts[kv.Key] = kv.Value;

		PaletteSourceColors = DeserialiseColors(data.PaletteSourceColors);
		PaletteTargetColors = DeserialiseColors(data.PaletteTargetColors);
	}

	private static Color[] DeserialiseColors(string[]? hexArray)
	{
		if (hexArray == null || hexArray.Length == 0) return [];
		var result = new Color[hexArray.Length];
		for (int i = 0; i < hexArray.Length; i++)
			result[i] = new Color(hexArray[i]);
		return result;
	}
}
