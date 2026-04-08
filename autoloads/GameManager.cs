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

	/// <summary>Debug toggle: when true, random encounters are suppressed. Toggle with L key.</summary>
	public bool DebugNoEncounters { get; set; }

	[Signal] public delegate void GameStateChangedEventHandler(GameState newState);
	[Signal] public delegate void PlayerStatsChangedEventHandler();
	/// <summary>Fired after level-up stat rolls are applied. Read PendingLevelUps for results.</summary>
	[Signal] public delegate void PlayerLeveledUpEventHandler();
	/// <summary>Fired after the player switches to a different class.</summary>
	[Signal] public delegate void ClassChangedEventHandler();
	/// <summary>Fired the very first time an enemy is defeated. Bestiary toast hook.</summary>
	[Signal] public delegate void BestiaryDiscoveredEventHandler(string enemyId);
	/// <summary>Fired when a new party member is recruited (e.g. Lily, Rain).</summary>
	[Signal] public delegate void PartyMemberRecruitedEventHandler(string memberId);
	/// <summary>Fired when the party order or leader changes (Party Menu reorder / promote).</summary>
	[Signal] public delegate void PartyOrderChangedEventHandler();

	// ── Internal domains ──────────────────────────────────────────────────────

	private readonly PlayerProgressionData _progression = new();
	private readonly PlayerCombatData      _combat      = new();
	private readonly InventoryData         _inventory   = new();
	private readonly WorldStateData        _world       = new();
	private readonly MellyrRewardData      _mellyr      = new();
	private readonly MultiClassData        _multiClass  = new();
	private readonly ForageCodexData       _forageCodex = new();
	private readonly BestiaryData          _bestiary    = new();
	private readonly PartyData             _party       = new();

	// ── State ─────────────────────────────────────────────────────────────────

	public GameState CurrentState { get; private set; } = GameState.Boot;

	public string PlayerName { get; private set; } = "Sen";

	// Story flags: keyed by string, all booleans
	public Godot.Collections.Dictionary<string, bool> Flags { get; private set; } = new();

	// Kill tracking: keyed by EnemyData.EnemyId
	public System.Collections.Generic.Dictionary<string, int> KillCounts { get; } = new();

	/// <summary>Per-enemy rhythm performance history for the Rhythm Memory adaptation system.</summary>
	public System.Collections.Generic.Dictionary<string, EnemyRhythmHistory> RhythmMemory { get; } = new();

	/// <summary>
	/// Current Perfect-streak counter for the foraging minigame. Increments on a Perfect grade,
	/// resets to zero on anything else. Persisted in <see cref="SaveData.ForageStreak"/>.
	/// </summary>
	public int ForageStreak { get; set; }

	/// <summary>Foragery codex container — populated by the foraging minigame on every find.</summary>
	public ForageCodexData ForageCodex => _forageCodex;

	/// <summary>Bestiary container — records first-defeated timestamp + total kills per enemy.</summary>
	public BestiaryData Bestiary => _bestiary;

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

	// ── Multi-class pass-through ──────────────────────────────────────────────

	public PlayerClass ActiveClass                                              => _multiClass.ActiveClass;
	public System.Collections.Generic.Dictionary<PlayerClass, ClassProgressionEntry> ClassEntries => _multiClass.ClassEntries;

	// ── Party pass-through ────────────────────────────────────────────────────

	/// <summary>Active party (Sen + any recruited members). Always contains at least Sen post-init.</summary>
	public PartyData Party => _party;
	/// <summary>Member id of whichever member is currently selected for inspection in menus.</summary>
	public string SelectedMemberId { get; set; } = "sen";

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
	public Vector2? BattleReturnPosition         { get => _world.BattleReturnPosition;  set => _world.BattleReturnPosition = value; }

	/// <summary>Set true before teleport transition; player checks this on arrival to play dissolve-in.</summary>
	public bool TeleportArriving { get; set; }

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
		UiTheme.ApplyGlobalTheme();
	}

	public override void _Input(InputEvent @event)
	{
		Core.Extensions.InputMapExtensions.TrackInputDevice(@event);
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
			var result = _combat.RollGrowth(PlayerLevel);
			result.MemberName = PlayerName;
			result.ClassName  = _multiClass.ActiveClass.ToString();
			PendingLevelUps.Add(result);
		}

		_multiClass.UpdateActiveClassProgression(PlayerLevel, Exp);

		EmitSignal(SignalName.PlayerLeveledUp);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	// ── Class switching ───────────────────────────────────────────────────────

	/// <summary>
	/// Switch the player to a different class. Snapshots current state,
	/// loads the target class's stats/growth, and restores HP/MP to full.
	/// </summary>
	public void SwitchClass(PlayerClass newClass)
	{
		if (newClass == _multiClass.ActiveClass) return;

		// Snapshot current class state
		var s = _combat.PlayerStats;
		_multiClass.SaveActiveClassState(
			PlayerLevel, Exp,
			s.MaxHp, s.Attack, s.Defense, s.Speed,
			s.Magic, s.Resistance, s.Luck, s.MaxMp);

		// Switch to target class (creates defaults from .tres if first time)
		var entry = _multiClass.SwitchTo(newClass, cls =>
		{
			string path = $"res://resources/characters/class_{cls.ToString().ToLower()}.tres";
			if (ResourceLoader.Exists(path))
			{
				var template = GD.Load<CharacterStats>(path);
				return MultiClassLogic.SnapshotToEntry(
					cls, level: 1, exp: 0,
					template.MaxHp, template.Attack, template.Defense, template.Speed,
					template.Magic, template.Resistance, template.Luck, template.MaxMp);
			}
			return new ClassProgressionEntry { Class = cls };
		});

		// Apply target class stats and progression
		_combat.ApplyFromClassEntry(entry);
		_combat.LoadGrowthRatesForClass(newClass);
		_progression.ApplyFromSave(Gold, entry.Exp, entry.Level, PlayTimeSeconds);

		// Mirror the new class's stats onto Sen's PartyMember so menus reading
		// from the party get the up-to-date numbers.
		SyncSenToParty();

		EmitSignal(SignalName.ClassChanged);
		EmitSignal(SignalName.PlayerStatsChanged);
	}

	// ── Inventory & items ─────────────────────────────────────────────────────

	public void AddItem(string resourcePath)    => _inventory.AddItem(resourcePath);
	public bool RemoveItem(string resourcePath) => _inventory.RemoveItem(resourcePath);
	public void AddSpell(string resourcePath)   => _inventory.AddSpell(resourcePath);

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
			_inventory.DynamicEquipmentInventory,
			MultiClassLogic.SumCrossClassBonuses(_multiClass.GetAllClassLevels()));

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

		// Bestiary: record kill with deterministic UTC timestamp; signal new discoveries.
		bool isNewDiscovery = _bestiary.Record(enemyId, System.DateTime.UtcNow);
		if (isNewDiscovery)
			EmitSignal(SignalName.BestiaryDiscovered, enemyId);

		QuestManager.Instance?.NotifyKill(enemyId);
	}

	/// <summary>Records post-battle rhythm performance for the Rhythm Memory adaptation system.</summary>
	public void RecordRhythmPerformance(string enemyId, PerformanceScore score)
	{
		RhythmMemory.TryGetValue(enemyId, out var existing);
		var updated = RhythmMemoryLogic.RecordEncounter(existing, score);
		RhythmMemory[enemyId] = updated;
		GD.Print($"[GameManager] Rhythm Memory recorded: {enemyId} → " +
			$"{updated.TotalEncounters} encounters (P:{updated.TotalPerfects} G:{updated.TotalGoods} M:{updated.TotalMisses}, best streak:{updated.BestMaxStreak})");
	}

	// ── Character customization ───────────────────────────────────────────────

	public void ApplyCharacterCustomization(CharacterStats stats, Color[] sourceColors, Color[] targetColors)
	{
		_combat.ApplyCustomization(stats);
		PaletteSourceColors = sourceColors;
		PaletteTargetColors = targetColors;

		// Initialize multi-class starting entry from the chosen class
		var startEntry = MultiClassLogic.SnapshotToEntry(
			stats.Class, level: 1, exp: 0,
			stats.MaxHp, stats.Attack, stats.Defense, stats.Speed,
			stats.Magic, stats.Resistance, stats.Luck, stats.MaxMp);
		_multiClass.InitializeStartingClass(stats.Class, startEntry);
		_combat.LoadGrowthRatesForClass(stats.Class);

		// Make sure Sen's party entry mirrors the freshly chosen stats so the rest
		// of the party API can read from it immediately.
		SyncSenToParty();

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
		_multiClass.Reset();

		Flags.Clear();
		KillCounts.Clear();
		RhythmMemory.Clear();
		_forageCodex.Reset();
		_bestiary.Reset();
		ForageStreak = 0;
		WeatherManager.Instance?.Reset();
		PaletteSourceColors = [];
		PaletteTargetColors = [];

		// Seed the party with Sen as the sole leader. Stats are mirrored from
		// _combat at this point — ApplyCharacterCustomization or class switches
		// will keep the mirror in sync.
		_party.Clear();
		_party.Add(BuildSenMemberFromCurrentState());
		SelectedMemberId = "sen";

		GD.Print("[GameManager] Reset for new game.");
	}

	// ── Party / Sen mirroring ─────────────────────────────────────────────────

	/// <summary>
	/// Build a fresh PartyMember for Sen from the current authoritative state held in
	/// <see cref="_combat"/>, <see cref="_progression"/>, and <see cref="_inventory"/>.
	/// Used by ResetForNewGame and the legacy save migration path.
	/// </summary>
	private PartyMember BuildSenMemberFromCurrentState()
	{
		var s = _combat.PlayerStats;
		var sen = PartyMember.CreateSen(
			displayName: PlayerName,
			cls: _multiClass.ActiveClass,
			level: PlayerLevel, exp: Exp,
			currentHp: s.CurrentHp, maxHp: s.MaxHp,
			currentMp: s.CurrentMp, maxMp: s.MaxMp,
			attack: s.Attack, defense: s.Defense, speed: s.Speed,
			magic: s.Magic, resistance: s.Resistance, luck: s.Luck);
		// Mirror Sen's equipment dicts so the party member is fully self-describing
		// (Phase 6 will route equipment writes through the member directly).
		foreach (var kv in _inventory.EquippedItemPaths)
			sen.EquippedItemPaths[kv.Key.ToString()] = kv.Value;
		foreach (var kv in _inventory.EquippedDynamicItemIds)
			sen.EquippedDynamicItemIds[kv.Key.ToString()] = kv.Value;
		return sen;
	}

	/// <summary>
	/// Promote a member to leader. Returns true on success and emits
	/// <see cref="PartyOrderChanged"/>.
	/// </summary>
	public bool SetPartyLeader(string memberId)
	{
		if (!_party.SetLeader(memberId)) return false;
		EmitSignal(SignalName.PartyOrderChanged);
		return true;
	}

	/// <summary>
	/// Swap two members in the marching order. Out-of-range or self-swap is a no-op.
	/// Emits <see cref="PartyOrderChanged"/> when the swap actually changes the list.
	/// </summary>
	public void SwapPartyMembers(int i, int j)
	{
		if (i == j) return;
		_party.Swap(i, j);
		EmitSignal(SignalName.PartyOrderChanged);
	}

	/// <summary>
	/// Add a recruited member to the active party. No-op if a member with the same id
	/// already exists or if the party is full. Emits <see cref="PartyMemberRecruited"/>
	/// on success.
	/// </summary>
	public bool RecruitPartyMember(PartyMember member)
	{
		if (member == null) return false;
		if (_party.Contains(member.MemberId)) return false;
		if (!_party.Add(member)) return false;
		GD.Print($"[GameManager] Recruited party member: {member.MemberId} ({member.DisplayName}, {member.Class})");
		EmitSignal(SignalName.PartyMemberRecruited, member.MemberId);
		EmitSignal(SignalName.PlayerStatsChanged);
		return true;
	}

	/// <summary>
	/// Push Sen's authoritative state into his PartyMember. Called by SaveManager just
	/// before serialization so the saved Party list reflects the current numbers.
	/// </summary>
	public void SyncSenToParty()
	{
		var sen = _party.GetById("sen");
		if (sen == null)
		{
			_party.Add(BuildSenMemberFromCurrentState());
			return;
		}
		var s = _combat.PlayerStats;
		sen.DisplayName            = PlayerName;
		sen.Class                  = _multiClass.ActiveClass.ToString();
		sen.CanChangeClass         = true;
		sen.OverworldSpritePath    = "res://assets/sprites/player/Sen_Overworld.png";
		sen.Level                  = PlayerLevel;
		sen.Exp                    = Exp;
		sen.CurrentHp              = s.CurrentHp;
		sen.MaxHp                  = s.MaxHp;
		sen.CurrentMp              = s.CurrentMp;
		sen.MaxMp                  = s.MaxMp;
		sen.Attack                 = s.Attack;
		sen.Defense                = s.Defense;
		sen.Speed                  = s.Speed;
		sen.Magic                  = s.Magic;
		sen.Resistance             = s.Resistance;
		sen.Luck                   = s.Luck;
		sen.EquippedItemPaths.Clear();
		foreach (var kv in _inventory.EquippedItemPaths)
			sen.EquippedItemPaths[kv.Key.ToString()] = kv.Value;
		sen.EquippedDynamicItemIds.Clear();
		foreach (var kv in _inventory.EquippedDynamicItemIds)
			sen.EquippedDynamicItemIds[kv.Key.ToString()] = kv.Value;
	}

	public void ApplySaveData(SaveData data)
	{
		_progression.ApplyFromSave(data.Gold, data.Exp, data.PlayerLevel, data.PlayTimeSeconds);
		_combat.ApplyFromSave(data);
		_inventory.ApplyFromSave(data);
		_world.ApplyFromSave(data);
		_mellyr.ApplyFromSave(data);

		// Ensure Teleport Home spell exists (migration for saves created before it existed)
		_inventory.AddSpell("res://resources/spells/teleport_home.tres");

		// Multi-class: restore from save or migrate legacy single-class save
		if (data.ClassProgressionEntries.Count > 0)
		{
			_multiClass.ApplyFromSave(
				data.ClassProgressionEntries,
				data.ActiveClassName ?? data.PlayerClassName ?? "Bard");
			_combat.LoadGrowthRatesForClass(_multiClass.ActiveClass);
		}
		else
		{
			// Legacy save migration: create a single entry from the saved stats
			var legacyClass = PlayerClass.Bard;
			if (data.PlayerClassName != null)
				System.Enum.TryParse(data.PlayerClassName, out legacyClass);

			var legacyEntry = MultiClassLogic.SnapshotToEntry(
				legacyClass, data.PlayerLevel, data.Exp,
				data.PlayerMaxHp, data.PlayerAttack, data.PlayerDefense, data.PlayerSpeed,
				data.PlayerMagic, data.PlayerResistance, data.PlayerLuck, data.PlayerMaxMp);
			_multiClass.InitializeStartingClass(legacyClass, legacyEntry);
			_combat.LoadGrowthRatesForClass(legacyClass);
		}

		Flags = new Godot.Collections.Dictionary<string, bool>(data.Flags);
		KillCounts.Clear();
		foreach (var kv in data.KillCounts) KillCounts[kv.Key] = kv.Value;

		RhythmMemory.Clear();
		foreach (var kv in data.RhythmMemory) RhythmMemory[kv.Key] = kv.Value;

		ForageStreak = data.ForageStreak;
		_forageCodex.ReplaceAll(data.ForageCodex);
		_bestiary.ReplaceAll(data.Bestiary);

		WeatherManager.Instance?.LoadFromSave(data.Weather, data.WeatherStepCounter);

		PaletteSourceColors = DeserialiseColors(data.PaletteSourceColors);
		PaletteTargetColors = DeserialiseColors(data.PaletteTargetColors);

		// Party (Phase 2): legacy saves have an empty Party list — synthesise Sen
		// from the existing _combat / _progression state. Newer saves carry a Party
		// list directly; we replace the runtime party with what was on disk.
		PlayerName = string.IsNullOrEmpty(data.PlayerName) ? PlayerName : data.PlayerName;
		_party.Clear();
		if (data.Party != null && data.Party.Count > 0)
		{
			_party.ReplaceAll(data.Party, data.PartyLeaderIndex);
		}
		else
		{
			_party.Add(BuildSenMemberFromCurrentState());
		}
		// Sen's authoritative state already lives in _combat — make sure his PartyMember
		// reflects that exactly. (For Lily/Rain in Phase 3+, their PartyMember IS the
		// authoritative state, so this only touches the Sen entry.)
		SyncSenToParty();
		SelectedMemberId = "sen";

		// Legacy migration: a save written before Phase 3 may already have
		// npc_lily_purchased / npc_rain_purchased set without the corresponding
		// PartyMember on disk. Synthesise the missing members so the player keeps
		// who they recruited across the upgrade.
		MigrateLegacyRecruitFlag(SennenRpg.Core.Data.Flags.NpcLilyPurchased,
			"lily", "Lily", PlayerClass.Alchemist,
			"res://resources/characters/lily_stats.tres",
			"res://assets/sprites/player/Lily_Overworld.png");
		MigrateLegacyRecruitFlag(SennenRpg.Core.Data.Flags.NpcRainPurchased,
			"rain", "Rain", PlayerClass.Rogue,
			"res://resources/characters/rain_stats.tres",
			"res://assets/sprites/player/Rain_Overworld.png");
	}

	private void MigrateLegacyRecruitFlag(
		string flagKey, string memberId, string displayName,
		PlayerClass cls, string statsPath, string overworldSpritePath)
	{
		if (!GetFlag(flagKey)) return;
		if (_party.Contains(memberId)) return;

		CharacterStats? stats = null;
		if (ResourceLoader.Exists(statsPath))
			stats = GD.Load<CharacterStats>(statsPath);

		var member = new PartyMember
		{
			MemberId            = memberId,
			DisplayName         = displayName,
			Class               = cls.ToString(),
			CanChangeClass      = false,
			Row                 = FormationRow.Front,
			OverworldSpritePath = overworldSpritePath,
			Level               = 1,
			Exp                 = 0,
			MaxHp      = stats?.MaxHp      ?? 18,
			CurrentHp  = stats?.MaxHp      ?? 18,
			MaxMp      = stats?.MaxMp      ?? 10,
			CurrentMp  = stats?.MaxMp      ?? 10,
			Attack     = stats?.Attack     ?? 8,
			Defense    = stats?.Defense    ?? 3,
			Speed      = stats?.Speed      ?? 10,
			Magic      = stats?.Magic      ?? 6,
			Resistance = stats?.Resistance ?? 3,
			Luck       = stats?.Luck       ?? 8,
		};
		_party.Add(member);
		GD.Print($"[GameManager] Legacy migration: synthesised {memberId} party member from existing flag.");
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
