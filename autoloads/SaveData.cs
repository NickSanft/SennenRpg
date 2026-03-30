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

    // ── Slot metadata ─────────────────────────────────────────────────────────
    /// <summary>Player's chosen name (displayed on the slot card).</summary>
    public string PlayerName      { get; init; } = "Sen";
    /// <summary>Cumulative seconds spent playing on this slot.</summary>
    public int    PlayTimeSeconds { get; init; } = 0;
    /// <summary>ISO-ish timestamp of when this file was last written.</summary>
    public string Timestamp       { get; init; } = "";
}
