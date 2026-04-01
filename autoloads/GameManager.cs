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

	public GameState CurrentState { get; private set; } = GameState.Boot;

	public string PlayerName { get; private set; } = "Sen";
	public int Gold        { get; private set; }
	public int Exp         { get; private set; }
	public int PlayerLevel { get; private set; } = 1;

	/// <summary>Results from the most recent level-up batch. Read and clear after showing the screen.</summary>
	public List<LevelUpResult> PendingLevelUps { get; } = new();

	private GrowthRates? _growthRates;
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

	// Known spells: list of SpellData resource paths
	public Array<string> KnownSpellPaths { get; private set; } = new();

	// Equipment: owned (picked up) and equipped (one per slot)
	public Array<string> OwnedEquipmentPaths { get; private set; } = new();
	public EquipDict EquippedItemPaths { get; private set; } = new();

	// Single cached CharacterStats for EffectiveStats — never recreated to avoid GodotObject GCHandle races
	private CharacterStats _effectiveStatsCache = new CharacterStats();

	// World map state
	public Vector2I WorldMapSpawnTile     { get; set; } = new Vector2I(10, 8);
	public Vector2I WorldMapReturnTile    { get; set; } = Vector2I.Zero;
	public bool     IsNight               { get; set; } = false;
	public int      TilesWalkedOnWorldMap { get; set; } = 0;

	// Kill tracking: keyed by EnemyData.EnemyId
	public System.Collections.Generic.Dictionary<string, int> KillCounts { get; } = new();

	/// <summary>World-map steps remaining before encounters can trigger again (Repel effect).</summary>
	public int RepelStepsRemaining { get; set; } = 0;

	// ── Mellyr Outpost passive rewards ────────────────────────────────────────

	/// <summary>Steps taken on 16×16 maps since the last passive reward tick (resets at 10).</summary>
	public int TownStepCounter { get; set; } = 0;

	/// <summary>Gold earned by Rain but not yet collected. Hard cap: 200 g.</summary>
	public int PendingRainGold { get; set; } = 0;

	/// <summary>Lily forge recipe strings pending collection. Hard cap: 5 items.</summary>
	public System.Collections.Generic.List<string> PendingLilyRecipes { get; } = new();

	/// <summary>Lily-generated equipment in the player's unequipped pool.</summary>
	public System.Collections.Generic.List<DynamicEquipmentSave> DynamicEquipmentInventory { get; } = new();

	/// <summary>Dynamic equipment slot → item ID (for Lily-generated items that are equipped).</summary>
	public System.Collections.Generic.Dictionary<EquipmentSlot, string> EquippedDynamicItemIds { get; } = new();

	/// <summary>Original palette colours extracted from the sprite at character creation.</summary>
	public Color[] PaletteSourceColors { get; set; } = [];
	/// <summary>Replacement colours chosen by the player, parallel to PaletteSourceColors.</summary>
	public Color[] PaletteTargetColors { get; set; } = [];

	/// <summary>Cumulative seconds the player has spent in Overworld or Battle states.</summary>
	public int PlayTimeSeconds { get; private set; } = 0;
	private double _playTimeAccumulator = 0.0;

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

		const string growthPath = "res://resources/characters/player_growth_rates.tres";
		if (ResourceLoader.Exists(growthPath))
			_growthRates = GD.Load<GrowthRates>(growthPath);
	}

	public override void _Process(double delta)
	{
		if (CurrentState is GameState.Overworld or GameState.Battle)
		{
			_playTimeAccumulator += delta;
			if (_playTimeAccumulator >= 1.0)
			{
				int whole = (int)_playTimeAccumulator;
				PlayTimeSeconds      += whole;
				_playTimeAccumulator -= whole;
			}
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
	public void AddExp(int amount)
	{
		Exp += amount;
		CheckAndApplyLevelUp();
	}

	private void CheckAndApplyLevelUp()
	{
		int gained = LevelData.CheckLevelUp(Exp, PlayerLevel);
		if (gained == 0) return;

		PendingLevelUps.Clear();
		for (int i = 0; i < gained; i++)
		{
			PlayerLevel++;
			PendingLevelUps.Add(RollGrowth(PlayerLevel));
		}

		EmitSignal(SignalName.PlayerLeveledUp);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	private LevelUpResult RollGrowth(int newLevel)
	{
		var s = PlayerStats;
		int oldHp  = s.MaxHp,      oldAtk = s.Attack,  oldDef = s.Defense,
			oldSpd = s.Speed,       oldMag = s.Magic,   oldRes = s.Resistance,
			oldLck = s.Luck;

		if (_growthRates != null)
		{
			if (GD.RandRange(0, 99) < _growthRates.MaxHp)
				{ s.MaxHp++; s.CurrentHp++; }                   // HP up heals too
			if (GD.RandRange(0, 99) < _growthRates.Attack)     s.Attack++;
			if (GD.RandRange(0, 99) < _growthRates.Defense)    s.Defense++;
			if (GD.RandRange(0, 99) < _growthRates.Speed)      s.Speed++;
			if (GD.RandRange(0, 99) < _growthRates.Magic)      s.Magic++;
			if (GD.RandRange(0, 99) < _growthRates.Resistance) s.Resistance++;
			if (GD.RandRange(0, 99) < _growthRates.Luck)       s.Luck++;
		}

		return new LevelUpResult
		{
			NewLevel      = newLevel,
			OldMaxHp      = oldHp,  NewMaxHp      = s.MaxHp,
			OldAttack     = oldAtk, NewAttack     = s.Attack,
			OldDefense    = oldDef, NewDefense    = s.Defense,
			OldSpeed      = oldSpd, NewSpeed      = s.Speed,
			OldMagic      = oldMag, NewMagic      = s.Magic,
			OldResistance = oldRes, NewResistance = s.Resistance,
			OldLuck       = oldLck, NewLuck       = s.Luck,
		};
	}

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

	/// <summary>
	/// Deducts <paramref name="cost"/> MP. Returns false without deducting if insufficient.
	/// </summary>
	public bool UseMp(int cost)
	{
		if (PlayerStats.CurrentMp < cost) return false;
		PlayerStats.CurrentMp -= cost;
		EmitSignal(SignalName.PlayerStatsChanged);
		return true;
	}

	/// <summary>Restores MP up to MaxMp and emits PlayerStatsChanged.</summary>
	public void RestoreMp(int amount)
	{
		PlayerStats.CurrentMp = Mathf.Min(PlayerStats.MaxMp, PlayerStats.CurrentMp + amount);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	// ── Equipment ─────────────────────────────────────────────────────────────

	/// <summary>Add a piece of equipment to the player's owned pool (e.g. from a chest).</summary>
	public void AddEquipment(string resourcePath) => OwnedEquipmentPaths.Add(resourcePath);

	/// <summary>Returns the equipped EquipmentData for a slot, or null if empty.</summary>
	public EquipmentData? GetEquipped(EquipmentSlot slot)
	{
		if (!EquippedItemPaths.TryGetValue(slot, out string? path) || string.IsNullOrEmpty(path))
			return null;
		return ResourceLoader.Exists(path) ? GD.Load<EquipmentData>(path) : null;
	}

	/// <summary>Equip an item from OwnedEquipmentPaths into the given slot.</summary>
	public void Equip(EquipmentSlot slot, string path)
	{
		// If something was already equipped in that slot, return it to the owned pool
		if (EquippedItemPaths.TryGetValue(slot, out string? current) && !string.IsNullOrEmpty(current))
			OwnedEquipmentPaths.Add(current);

		EquippedItemPaths[slot] = path;
		OwnedEquipmentPaths.Remove(path);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	/// <summary>Unequip the item in a slot, returning it to OwnedEquipmentPaths.</summary>
	public void Unequip(EquipmentSlot slot)
	{
		if (!EquippedItemPaths.TryGetValue(slot, out string? path) || string.IsNullOrEmpty(path))
			return;
		OwnedEquipmentPaths.Add(path);
		EquippedItemPaths.Remove(slot);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	/// <summary>
	/// Effective combat stats: PlayerStats + all equipped item bonuses.
	/// Mutates and returns a single cached CharacterStats held by GameManager to avoid
	/// creating throwaway GodotObject instances that race against the C# GC finalizer.
	/// </summary>
	public CharacterStats EffectiveStats
	{
		get
		{
			var bonusList = new List<EquipmentBonuses>();
			foreach (var kv in EquippedItemPaths)
			{
				if (!ResourceLoader.Exists(kv.Value)) continue;
				var equipment = GD.Load<EquipmentData>(kv.Value);
				if (equipment == null) continue;
				bonusList.Add(equipment.Bonuses);
			}
			// Also include equipped Lily-generated dynamic items.
			// DynamicEquipmentInventory holds ALL dynamic items; EquippedDynamicItemIds
			// maps slots to the IDs of those that are currently active.
			foreach (var equippedId in EquippedDynamicItemIds.Values)
			{
				var dynItem = DynamicEquipmentInventory.Find(e => e.Id == equippedId);
				if (dynItem != null)
					bonusList.Add(new EquipmentBonuses(dynItem.BonusMaxHp, dynItem.BonusAttack,
						dynItem.BonusDefense, dynItem.BonusMagic, 0, dynItem.BonusSpeed, dynItem.BonusLuck));
			}
			var bonus = EquipmentLogic.SumBonuses(bonusList);
			var s = PlayerStats;
			_effectiveStatsCache.MaxHp               = s.MaxHp      + bonus.MaxHp;
			_effectiveStatsCache.CurrentHp           = s.CurrentHp;
			_effectiveStatsCache.Attack              = s.Attack     + bonus.Attack;
			_effectiveStatsCache.Defense             = s.Defense    + bonus.Defense;
			_effectiveStatsCache.Speed               = s.Speed      + bonus.Speed;
			_effectiveStatsCache.Magic               = s.Magic      + bonus.Magic;
			_effectiveStatsCache.Resistance          = s.Resistance + bonus.Resistance;
			_effectiveStatsCache.Luck                = s.Luck       + bonus.Luck;
			_effectiveStatsCache.MoveSpeed           = s.MoveSpeed;
			_effectiveStatsCache.MaxMp               = s.MaxMp;
			_effectiveStatsCache.CurrentMp           = s.CurrentMp;
			_effectiveStatsCache.InvincibilityDuration = s.InvincibilityDuration;
			return _effectiveStatsCache;
		}
	}

	// ── Mellyr Outpost reward collection ─────────────────────────────────────

	/// <summary>
	/// Transfers all pending Rain gold to the player wallet.
	/// Returns the amount collected (0 if nothing was pending).
	/// </summary>
	public int CollectRainRewards()
	{
		int amount = PendingRainGold;
		PendingRainGold = 0;
		if (amount > 0) AddGold(amount);
		return amount;
	}

	/// <summary>
	/// Resolves all pending Lily recipes into equipment and moves them to
	/// <see cref="DynamicEquipmentInventory"/>. Returns the count added.
	/// </summary>
	public List<DynamicEquipmentSave> CollectLilyRewards()
	{
		var items = PendingLilyRecipes.Select(LilyForgeLogic.Resolve).ToList();
		foreach (var item in items)
			DynamicEquipmentInventory.Add(item);
		PendingLilyRecipes.Clear();
		return items;
	}

	/// <summary>Collects all pending Rain and Lily rewards at once.</summary>
	public (int gold, List<DynamicEquipmentSave> items) CollectAllTownRewards()
		=> (CollectRainRewards(), CollectLilyRewards());

	/// <summary>
	/// Equip a Lily-generated dynamic item into a slot.
	/// The item must exist in <see cref="DynamicEquipmentInventory"/> (it stays there;
	/// <see cref="EquippedDynamicItemIds"/> just records which one is active per slot).
	/// </summary>
	public void EquipDynamic(EquipmentSlot slot, string itemId)
	{
		EquippedDynamicItemIds[slot] = itemId;
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	/// <summary>Unequip the dynamic item from a slot (it remains in DynamicEquipmentInventory).</summary>
	public void UnequipDynamic(EquipmentSlot slot)
	{
		EquippedDynamicItemIds.Remove(slot);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public bool GetFlag(string key) => Flags.TryGetValue(key, out bool val) && val;

	public void SetFlag(string key, bool value)
	{
		Flags[key] = value;
		QuestManager.Instance?.NotifyFlagChanged(key);
	}

	/// <summary>Increments the kill counter for the given enemy ID and notifies QuestManager.</summary>
	public void RecordKill(string enemyId)
	{
		KillCounts[enemyId] = KillCounts.GetValueOrDefault(enemyId, 0) + 1;
		GD.Print($"[GameManager] Kill recorded: {enemyId} (total: {KillCounts[enemyId]})");
		QuestManager.Instance?.NotifyKill(enemyId);
	}

	/// <summary>Resets all runtime state for a fresh new game.</summary>
	public void ResetForNewGame()
	{
		Gold        = 500;
		Exp         = 0;
		PlayerLevel = 1;
		LastMapPath = "";
		LastSavePointId = "";
		LastSpawnId = "";
		Flags.Clear();
		InventoryItemPaths.Clear();
		InventoryItemPaths.Add("res://resources/items/item_001.tres"); // starting Bandage
		KnownSpellPaths.Clear();
		KnownSpellPaths.Add("res://resources/spells/shadow_bolt.tres");

		OwnedEquipmentPaths.Clear();
		OwnedEquipmentPaths.Add("res://resources/equipment/iron_sword.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/leather_cap.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/leather_body.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/leather_legs.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/leather_boots.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/wooden_shield.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/work_gloves.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/lucky_charm.tres");
		EquippedItemPaths.Clear();

		WorldMapSpawnTile     = new Vector2I(10, 8);
		WorldMapReturnTile    = Vector2I.Zero;
		IsNight               = false;
		TilesWalkedOnWorldMap = 0;
		KillCounts.Clear();
		PaletteSourceColors   = [];
		PaletteTargetColors   = [];
		PlayTimeSeconds        = 0;
		_playTimeAccumulator   = 0.0;
		TownStepCounter       = 0;
		PendingRainGold       = 0;
		PendingLilyRecipes.Clear();
		DynamicEquipmentInventory.Clear();
		EquippedDynamicItemIds.Clear();

		const string statsPath = "res://resources/characters/player_stats.tres";
		if (ResourceLoader.Exists(statsPath))
			PlayerStats = (CharacterStats)GD.Load<CharacterStats>(statsPath).Duplicate();
		GD.Print("[GameManager] Reset for new game.");
	}

	/// <summary>
	/// Applies the result of the character-customization screen.
	/// Copies all stat fields into the existing <see cref="PlayerStats"/> object rather than
	/// replacing it, to avoid creating a new GodotObject that could race the C# GC finaliser.
	/// Pass empty arrays for sourceColors/targetColors to use the default sprite appearance.
	/// </summary>
	public void ApplyCharacterCustomization(CharacterStats stats, Color[] sourceColors, Color[] targetColors)
	{
		PlayerStats.MaxHp      = stats.MaxHp;
		PlayerStats.CurrentHp  = stats.MaxHp;
		PlayerStats.Attack     = stats.Attack;
		PlayerStats.Defense    = stats.Defense;
		PlayerStats.Speed      = stats.Speed;
		PlayerStats.Magic      = stats.Magic;
		PlayerStats.Resistance = stats.Resistance;
		PlayerStats.Luck       = stats.Luck;
		PlayerStats.MaxMp      = stats.MaxMp;
		PlayerStats.CurrentMp  = stats.MaxMp;
		PlayerStats.Class      = stats.Class;
		PlayerStats.ClassName  = stats.ClassName;
		PaletteSourceColors    = sourceColors;
		PaletteTargetColors    = targetColors;
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	public void ApplySaveData(SaveData data)
	{
		Gold        = data.Gold;
		Exp         = data.Exp;
		PlayerLevel = data.PlayerLevel > 0 ? data.PlayerLevel : 1;
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
		PlayerStats.MaxMp       = data.PlayerMaxMp;
		PlayerStats.CurrentMp   = data.PlayerMp;

		InventoryItemPaths.Clear();
		foreach (var path in data.InventoryItemPaths)
			InventoryItemPaths.Add(path);

		KnownSpellPaths.Clear();
		foreach (var path in data.KnownSpellPaths)
			KnownSpellPaths.Add(path);

		OwnedEquipmentPaths.Clear();
		foreach (var path in data.OwnedEquipmentPaths)
			OwnedEquipmentPaths.Add(path);

		EquippedItemPaths.Clear();
		foreach (var kv in data.EquippedItemPaths)
		{
			if (System.Enum.TryParse<EquipmentSlot>(kv.Key, out var slot))
				EquippedItemPaths[slot] = kv.Value;
		}

		WorldMapSpawnTile     = new Vector2I(data.WorldMapSpawnTileX, data.WorldMapSpawnTileY);
		IsNight               = data.IsNight;
		TilesWalkedOnWorldMap = data.TilesWalkedOnWorldMap;
		KillCounts.Clear();
		foreach (var kv in data.KillCounts) KillCounts[kv.Key] = kv.Value;

		PaletteSourceColors  = DeserialiseColors(data.PaletteSourceColors);
		PaletteTargetColors  = DeserialiseColors(data.PaletteTargetColors);
		PlayTimeSeconds      = data.PlayTimeSeconds;
		_playTimeAccumulator = 0.0;

		TownStepCounter = data.TownStepCounter;
		PendingRainGold = data.PendingRainGold;
		PendingLilyRecipes.Clear();
		PendingLilyRecipes.AddRange(data.PendingLilyRecipes);
		DynamicEquipmentInventory.Clear();
		DynamicEquipmentInventory.AddRange(data.DynamicEquipmentInventory);
		EquippedDynamicItemIds.Clear();
		foreach (var kv in data.EquippedDynamicItemIds)
		{
			if (System.Enum.TryParse<EquipmentSlot>(kv.Key, out var slot))
				EquippedDynamicItemIds[slot] = kv.Value;
		}
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
