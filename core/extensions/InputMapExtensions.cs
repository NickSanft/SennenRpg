using Godot;

namespace SennenRpg.Core.Extensions;

/// <summary>
/// Helpers for reading action bindings from Godot's <see cref="InputMap"/>
/// so hint labels stay accurate when the player remaps keys in Settings.
/// Automatically detects the last used input device (keyboard vs gamepad).
/// </summary>
public static class InputMapExtensions
{
    /// <summary>True when the last input event was from a gamepad.</summary>
    public static bool IsUsingGamepad { get; set; }

    /// <summary>Call from a top-level _Input or _UnhandledInput to track input device.</summary>
    public static void TrackInputDevice(InputEvent evt)
    {
        if (evt is InputEventJoypadButton or InputEventJoypadMotion)
            IsUsingGamepad = true;
        else if (evt is InputEventKey or InputEventMouseButton or InputEventMouseMotion)
            IsUsingGamepad = false;
    }

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
    /// Returns the gamepad button name for the first joypad event bound to <paramref name="action"/>.
    /// Uses Xbox-style labels (A, B, X, Y, LB, RB, etc.).
    /// </summary>
    public static string GetFirstGamepadButton(string action, string fallback = "?")
    {
        if (!InputMap.HasAction(action)) return fallback;

        foreach (var evt in InputMap.ActionGetEvents(action))
        {
            if (evt is InputEventJoypadButton joyBtn)
            {
                return joyBtn.ButtonIndex switch
                {
                    JoyButton.A           => "A",
                    JoyButton.B           => "B",
                    JoyButton.X           => "X",
                    JoyButton.Y           => "Y",
                    JoyButton.LeftShoulder  => "LB",
                    JoyButton.RightShoulder => "RB",
                    JoyButton.Start       => "Start",
                    JoyButton.Back        => "Back",
                    JoyButton.DpadUp      => "D-Up",
                    JoyButton.DpadDown    => "D-Down",
                    JoyButton.DpadLeft    => "D-Left",
                    JoyButton.DpadRight   => "D-Right",
                    _ => joyBtn.ButtonIndex.ToString(),
                };
            }
        }
        return fallback;
    }

    /// <summary>
    /// Returns the appropriate button name based on the last used input device.
    /// Keyboard: returns key name. Gamepad: returns Xbox-style button name.
    /// </summary>
    public static string GetInputHint(string action, string fallback = "?")
    {
        return IsUsingGamepad
            ? GetFirstGamepadButton(action, fallback)
            : GetFirstKeyName(action, fallback);
    }

    /// <summary>
    /// Builds a compact hint string, e.g. "[Z] Confirm" or "[A] Confirm" depending on device.
    /// </summary>
    public static string HintFor(string action, string label, string fallback = "?")
        => $"[{GetInputHint(action, fallback)}] {label}";
}
