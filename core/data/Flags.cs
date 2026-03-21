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
