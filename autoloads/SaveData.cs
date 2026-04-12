using System.Collections.Generic;

namespace SennenRpg.Autoloads;

/// <summary>
/// Plain-data record that is serialised to / deserialised from JSON by SaveManager.
/// Contains no Godot runtime types so it can be compiled and tested in NUnit.
/// </summary>
public record SaveData
{
	// ── Combat stats ──────────────────────────────────────────────────────────
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

	// ── Economy / progression ─────────────────────────────────────────────────
	public int Gold { get; init; }
	public int Exp  { get; init; }

	// ── Navigation ────────────────────────────────────────────────────────────
	public string LastMapPath    { get; init; } = "";
	public string LastSavePointId{ get; init; } = "";
	public string LastSpawnId    { get; init; } = "";

	// ── Story ─────────────────────────────────────────────────────────────────
	public Dictionary<string, bool> Flags { get; init; } = new();

	// ── Inventory / equipment ─────────────────────────────────────────────────
	public List<string>              InventoryItemPaths  { get; init; } = new();
	public List<string>              KnownSpellPaths     { get; init; } = new();
	public List<string>              OwnedEquipmentPaths { get; init; } = new();
	/// <summary>Slot enum name → resource path. Empty string means nothing equipped.</summary>
	public Dictionary<string, string> EquippedItemPaths  { get; init; } = new();

	// ── World map ─────────────────────────────────────────────────────────────
	public int  WorldMapSpawnTileX    { get; init; } = 10;
	public int  WorldMapSpawnTileY    { get; init; } = 8;
	public bool IsNight               { get; init; } = false;
	public int  TilesWalkedOnWorldMap { get; init; } = 0;

	// ── Kill / quest tracking ────────────────────────────────────────────────
	public Dictionary<string, int> KillCounts        { get; init; } = new();
	public List<string>            ActiveQuestIds    { get; init; } = new();
	public List<string>            CompletedQuestIds { get; init; } = new();

	// ── Character customization ───────────────────────────────────────────────
	/// <summary>Enum name of the class chosen at character creation (e.g. "Bard").</summary>
	public string?   PlayerClassName     { get; init; }
	/// <summary>Original sprite colours extracted at character creation (hex strings, no #).</summary>
	public string[]? PaletteSourceColors { get; init; }
	/// <summary>Replacement colours chosen by the player, parallel to PaletteSourceColors.</summary>
	public string[]? PaletteTargetColors { get; init; }

	// ── Mellyr Outpost passive rewards ────────────────────────────────────────
	/// <summary>Steps taken on 16×16 maps since the last passive reward tick.</summary>
	public int          TownStepCounter         { get; init; } = 0;
	/// <summary>Gold earned by Rain but not yet collected. Hard cap: 200.</summary>
	public int          PendingRainGold         { get; init; } = 0;
	/// <summary>Lily forge recipe strings pending collection. Hard cap: 5.</summary>
	public List<string> PendingLilyRecipes      { get; init; } = new();
	/// <summary>Bhata-brewed ales pending collection. Hard cap: 5.</summary>
	public int          PendingBhataAles        { get; init; } = 0;
	/// <summary>Kriora crystal weapon recipe strings pending collection. Hard cap: 3.</summary>
	public List<string> PendingKrioraRecipes    { get; init; } = new();
	/// <summary>Lily-generated equipment in the player's unequipped pool.</summary>
	public List<SennenRpg.Core.Data.DynamicEquipmentSave> DynamicEquipmentInventory { get; init; } = new();
	/// <summary>Slot name → dynamic equipment ID for currently equipped Lily items.</summary>
	public Dictionary<string, string> EquippedDynamicItemIds { get; init; } = new();

	// ── Rhythm Memory — per-enemy adaptation history ─────────────────────────
	/// <summary>Per-enemy performance history for the Rhythm Memory system.</summary>
	public Dictionary<string, SennenRpg.Core.Data.EnemyRhythmHistory> RhythmMemory { get; init; } = new();

	// ── Multi-class progression ─────────────────────────────────────────���─────
	/// <summary>Per-class progression entries. Empty for legacy saves (auto-migrated on load).</summary>
	public List<SennenRpg.Core.Data.ClassProgressionEntry> ClassProgressionEntries { get; init; } = new();
	/// <summary>Active class name. Null for legacy saves (falls back to PlayerClassName).</summary>
	public string? ActiveClassName { get; init; }

	// ── Foraging ──────────────────────────────────────────────────────────────
	/// <summary>Current Perfect-streak counter for the foraging rhythm minigame.</summary>
	public int                                                              ForageStreak { get; init; } = 0;
	/// <summary>Foragery codex entries — item path → discovery record.</summary>
	public Dictionary<string, SennenRpg.Core.Data.ForageCodexEntry>         ForageCodex  { get; init; } = new();

	// ── Weather ───────────────────────────────────────────────────────────────
	/// <summary>Current weather state. Defaults to Sunny for legacy saves.</summary>
	public SennenRpg.Core.Data.WeatherType                                  Weather            { get; init; } = SennenRpg.Core.Data.WeatherType.Sunny;
	/// <summary>Steps accumulated toward the next weather roll. Defaults to 0 for legacy saves.</summary>
	public int                                                              WeatherStepCounter { get; init; } = 0;

	// ── Bestiary ──────────────────────────────────────────────────────────────
	/// <summary>Bestiary entries — enemy id → kill record with first-defeated timestamp.</summary>
	public Dictionary<string, SennenRpg.Core.Data.BestiaryEntry>            Bestiary           { get; init; } = new();

	// ── Party (Phase 2) ──────────────────────────────────────────────────────
	/// <summary>
	/// All party members in order. Empty for legacy saves — auto-migrated on load
	/// (Sen is reconstructed from the legacy single-character fields above).
	/// </summary>
	public List<SennenRpg.Core.Data.PartyMember> Party { get; init; } = new();
	/// <summary>Index of the current party leader within <see cref="Party"/>. Defaults to 0.</summary>
	public int PartyLeaderIndex { get; init; } = 0;
	/// <summary>
	/// Number of active party members (first N in <see cref="Party"/> are active,
	/// rest are reserve). Defaults to -1 for legacy saves (all active, capped to 4).
	/// </summary>
	public int ActivePartyCount { get; init; } = -1;

	// ── Slot metadata ─────────────────────────────────────────────────────────
	/// <summary>Player's chosen name (displayed on the slot card).</summary>
	public string PlayerName      { get; init; } = "Sen";
	/// <summary>Cumulative seconds spent playing on this slot.</summary>
	public int    PlayTimeSeconds { get; init; } = 0;
	/// <summary>ISO-ish timestamp of when this file was last written.</summary>
	public string Timestamp       { get; init; } = "";
}
