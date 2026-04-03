namespace SennenRpg.Core.Data;

/// <summary>
/// Compile-time constants for all named story and game-state flags.
/// Use these everywhere instead of raw strings so typos become compile errors.
///
/// Naming convention:
///   Story/encounter flags — lowercase_snake_case constant (e.g. MetShizu = "met_shizu")
///   "talked_to" flags     — generated automatically by Npc.cs; use TalkedTo(npcId) to check.
///
/// To set a flag from a .dtl timeline use the Signal event:
///   [signal arg="flag:met_shizu"]
/// The string after "flag:" must match the constant value exactly.
/// </summary>
public static class Flags
{
    // ── NPC story flags ──────────────────────────────────────────────────────

    /// <summary>Set the first time the player speaks to Shizukana Ito.</summary>
    public const string MetShizu = "met_shizu";

    /// <summary>Set when Brix's horse magically materialises next to him.</summary>
    public const string BrixHorseAppeared = "brix_horse_appeared";

    /// <summary>Set when Bhata's falafel materialises floating in purple energy.</summary>
    public const string BhataFalafelAppeared = "bhata_falafel_appeared";

    /// <summary>Set when light-blue crystals appear at Kriora's feet.</summary>
    public const string KrioraCrystalsAppeared = "kriora_crystals_appeared";

    /// <summary>Set when Gus transforms into a giant frog.</summary>
    public const string GusTransformedToFrog = "gus_transformed_to_frog";

    /// <summary>Set when Shizu's music note aura activates and BGM changes.</summary>
    public const string ShizuMusicAuraActive = "shizu_music_aura_active";

    /// <summary>Set once Lily's alt dialog has been seen at least once.</summary>
    public const string LilyAltDone = "lily_alt_done";

    /// <summary>Set once Rain's alt dialog has been seen at least once.</summary>
    public const string RainAltDone = "rain_alt_done";

    /// <summary>Set when all seven MAPP NPCs have completed their alt dialogs.</summary>
    public const string AllAltDialogsDone = "all_alt_dialogs_done";

    // ── Environment flags ─────────────────────────────────────────────────────

    /// <summary>Set when the player walks close to the northern exit in the test room.</summary>
    public const string SeenNorthExitHint = "seen_north_exit_hint";

    // ── Dungeon flags ─────────────────────────────────────────────────────

    /// <summary>Set the first time the player enters the dungeon from the world map.</summary>
    public const string DungeonDiscovered = "dungeon_discovered";

    /// <summary>Set when the player first exits dungeon floor 1 (reached floor 2).</summary>
    public const string DungeonFloor1Cleared = "dungeon_floor1_cleared";

    /// <summary>Set when the player first exits dungeon floor 2 (reached floor 3).</summary>
    public const string DungeonFloor2Cleared = "dungeon_floor2_cleared";

    /// <summary>Set when the dungeon boss on floor 3 is defeated.</summary>
    public const string DungeonBossDefeated = "dungeon_boss_defeated";

    // ── Mellyr Outpost ───────────────────────────────────────────────────

    /// <summary>Set when Rain's residency slot is purchased from Rork in Mellyr Outpost.</summary>
    public const string NpcRainPurchased = "npc_rain_purchased";

    /// <summary>Set when Lily's residency slot is purchased from Rork in Mellyr Outpost.</summary>
    public const string NpcLilyPurchased = "npc_lily_purchased";

    // ── Meta / flow flags ─────────────────────────────────────────────────

    /// <summary>Set after the introductory cutscene plays on a new game. Prevents it replaying.</summary>
    public const string IntroCutsceneSeen = "intro_cutscene_seen";

    // ── Helper ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the flag key that Npc.cs sets automatically after every conversation
    /// with the given NPC (format: "talked_to_{npcId}").
    /// </summary>
    public static string TalkedTo(string npcId) => $"talked_to_{npcId}";
}
