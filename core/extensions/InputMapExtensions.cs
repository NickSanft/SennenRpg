using Godot;

namespace SennenRpg.Core.Extensions;

/// <summary>
/// Helpers for reading action bindings from Godot's <see cref="InputMap"/>
/// so hint labels stay accurate when the player remaps keys in Settings.
/// </summary>
public static class InputMapExtensions
{
    /// <summary>
    /// Returns a human-readable name for the first keyboard key bound to <paramref name="action"/>.
    /// Falls back to <paramref name="fallback"/> if no key event is found.
    /// </summary>
    public static string GetFirstKeyName(string action, string fallback = "?")
    {
        if (!InputMap.HasAction(action)) return fallback;

        foreach (var evt in InputMap.ActionGetEvents(action))
        {
            if (evt is InputEventKey key)
                return key.AsText();
        }
        return fallback;
    }

    /// <summary>
    /// Builds a compact hint string, e.g. "[Z] Confirm" from action "interact".
    /// </summary>
    public static string HintFor(string action, string label, string fallback = "?")
        => $"[{GetFirstKeyName(action, fallback)}] {label}";
}
