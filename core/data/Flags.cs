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

    // ── Helper ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the flag key that Npc.cs sets automatically after every conversation
    /// with the given NPC (format: "talked_to_{npcId}").
    /// </summary>
    public static string TalkedTo(string npcId) => $"talked_to_{npcId}";
}
