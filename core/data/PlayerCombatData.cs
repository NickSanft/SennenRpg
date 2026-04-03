using System.Collections.Generic;
using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Core.Data;

/// <summary>
/// Holds player combat state: HP, MP, stats, growth rates, and effective stat computation.
/// Plain C# class — owned by GameManager as an internal domain.
/// </summary>
public class PlayerCombatData
{
	public CharacterStats PlayerStats { get; private set; } = new CharacterStats();

	// Single cached CharacterStats for EffectiveStats — never recreated to avoid GodotObject GCHandle races
	private CharacterStats _effectiveStatsCache = new CharacterStats();
	private GrowthRates? _growthRates;

	// ── Initialisation ────────────────────────────────────────────────────────

	public void LoadDefaults()
	{
		const string statsPath = "res://resources/characters/player_stats.tres";
		if (ResourceLoader.Exists(statsPath))
		{
			var loaded = GD.Load<CharacterStats>(statsPath);
			PlayerStats = (CharacterStats)loaded.Duplicate();
		}

		const string growthPath = "res://resources/characters/player_growth_rates.tres";
		if (ResourceLoader.Exists(growthPath))
			_growthRates = GD.Load<GrowthRates>(growthPath);
	}

	/// <summary>
	/// Load class-specific growth rates. Falls back to default if not found.
	/// </summary>
	public void LoadGrowthRatesForClass(PlayerClass cls)
	{
		string path = $"res://resources/characters/growth_rates_{cls.ToString().ToLower()}.tres";
		if (ResourceLoader.Exists(path))
			_growthRates = GD.Load<GrowthRates>(path);
	}

	/// <summary>
	/// Apply base stats from a class progression entry (used when switching classes).
	/// </summary>
	public void ApplyFromClassEntry(ClassProgressionEntry entry)
	{
		PlayerStats.MaxHp      = entry.MaxHp;
		PlayerStats.CurrentHp  = entry.MaxHp;
		PlayerStats.Attack     = entry.Attack;
		PlayerStats.Defense    = entry.Defense;
		PlayerStats.Speed      = entry.Speed;
		PlayerStats.Magic      = entry.Magic;
		PlayerStats.Resistance = entry.Resistance;
		PlayerStats.Luck       = entry.Luck;
		PlayerStats.MaxMp      = entry.MaxMp;
		PlayerStats.CurrentMp  = entry.MaxMp;
		PlayerStats.Class      = entry.Class;
		PlayerStats.ClassName  = entry.Class.ToString();
	}

	// ── HP / MP ───────────────────────────────────────────────────────────────

	public void HurtPlayer(int amount)
		=> PlayerStats.CurrentHp = Mathf.Max(0, PlayerStats.CurrentHp - amount);

	public void HealPlayer(int amount)
		=> PlayerStats.CurrentHp = Mathf.Min(PlayerStats.MaxHp, PlayerStats.CurrentHp + amount);

	public bool UseMp(int cost)
	{
		if (PlayerStats.CurrentMp < cost) return false;
		PlayerStats.CurrentMp -= cost;
		return true;
	}

	public void RestoreMp(int amount)
		=> PlayerStats.CurrentMp = Mathf.Min(PlayerStats.MaxMp, PlayerStats.CurrentMp + amount);

	// ── Level-up growth ───────────────────────────────────────────────────────

	public LevelUpResult RollGrowth(int newLevel)
	{
		var s = PlayerStats;
		int oldHp  = s.MaxHp,      oldAtk = s.Attack,  oldDef = s.Defense,
		    oldSpd = s.Speed,       oldMag = s.Magic,   oldRes = s.Resistance,
		    oldLck = s.Luck;

		if (_growthRates != null)
		{
			if (GD.RandRange(0, 99) < _growthRates.MaxHp)
				{ s.MaxHp++; s.CurrentHp++; }
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

	// ── Effective stats (base + equipment bonuses) ────────────────────────────

	public CharacterStats ComputeEffectiveStats(
		Dictionary<EquipmentSlot, string> equippedItemPaths,
		Dictionary<EquipmentSlot, string> equippedDynamicItemIds,
		List<DynamicEquipmentSave> dynamicEquipmentInventory,
		EquipmentBonuses crossClassBonuses = default)
	{
		var bonusList = new List<EquipmentBonuses>();
		foreach (var kv in equippedItemPaths)
		{
			if (!ResourceLoader.Exists(kv.Value)) continue;
			var equipment = GD.Load<EquipmentData>(kv.Value);
			if (equipment == null) continue;
			bonusList.Add(equipment.Bonuses);
		}
		foreach (var equippedId in equippedDynamicItemIds.Values)
		{
			var dynItem = dynamicEquipmentInventory.Find(e => e.Id == equippedId);
			if (dynItem != null)
				bonusList.Add(new EquipmentBonuses(dynItem.BonusMaxHp, dynItem.BonusAttack,
					dynItem.BonusDefense, dynItem.BonusMagic, 0, dynItem.BonusSpeed, dynItem.BonusLuck));
		}
		if (crossClassBonuses != default)
			bonusList.Add(crossClassBonuses);
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

	// ── Character customization ───────────────────────────────────────────────

	public void ApplyCustomization(CharacterStats stats)
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
	}

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	public void Reset()
	{
		const string statsPath = "res://resources/characters/player_stats.tres";
		if (ResourceLoader.Exists(statsPath))
			PlayerStats = (CharacterStats)GD.Load<CharacterStats>(statsPath).Duplicate();
	}

	public void ApplyFromSave(SaveData data)
	{
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

		// Restore class identity from save
		if (!string.IsNullOrEmpty(data.PlayerClassName)
			&& System.Enum.TryParse<PlayerClass>(data.PlayerClassName, out var cls))
		{
			PlayerStats.Class     = cls;
			PlayerStats.ClassName = cls.ToString();
		}
	}
}
