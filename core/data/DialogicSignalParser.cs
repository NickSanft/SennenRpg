namespace SennenRpg.Core.Data;

/// <summary>
/// Parses the string argument carried by Dialogic signal_event nodes.
///
/// Supported conventions (set in the Dialogic timeline editor):
///   [signal arg="flag:{name}"]       → set GameManager flag "{name}" to true
///   [signal arg="give_item:{path}"]  → add item at res:// "{path}" to inventory
///   [signal arg="{anything else}"]   → forwarded as a custom signal for game code to handle
///
/// Extracted as a pure static class so it can be unit-tested without the Godot runtime.
/// </summary>
public static class DialogicSignalParser
{
	public const string TypeFlag     = "flag";
	public const string TypeGiveItem = "give_item";
	public const string TypeCustom   = "custom";

	private const string FlagPrefix     = "flag:";
	private const string GiveItemPrefix = "give_item:";

	/// <summary>
	/// Parses a raw Dialogic signal string into a (type, argument) pair.
	/// The type will be one of the Type* constants defined on this class.
	/// </summary>
	public static (string Type, string Argument) Parse(string signal)
	{
		if (signal.StartsWith(FlagPrefix))
			return (TypeFlag, signal.Substring(FlagPrefix.Length));

		if (signal.StartsWith(GiveItemPrefix))
			return (TypeGiveItem, signal.Substring(GiveItemPrefix.Length));

		return (TypeCustom, signal);
	}
}
