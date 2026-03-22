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

    /// <summary>Set when Foran gives the player a Potion during their first conversation.</summary>
    public const string GotItemFromForan = "got_item_from_foran";

    /// <summary>Set when Brix's horse magically materialises next to him.</summary>
    public const string BrixHorseAppeared = "brix_horse_appeared";

    /// <summary>Set when Bhata's ferret materialises floating in purple energy.</summary>
    public const string BhataFerretAppeared = "bhata_ferret_appeared";

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

    // ── Helper ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the flag key that Npc.cs sets automatically after every conversation
    /// with the given NPC (format: "talked_to_{npcId}").
    /// </summary>
    public static string TalkedTo(string npcId) => $"talked_to_{npcId}";
}
