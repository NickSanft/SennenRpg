using Godot;
using System.Collections.Generic;

namespace SennenRpg.Autoloads;

/// <summary>
/// Central registry mapping short timeline keys to their full resource paths.
/// Register entries in code (e.g. your GameManager or map _Ready) or populate via
/// the editor by editing the ShortNames / Paths export arrays.
///
/// Usage:
///   DialogRegistry.Instance.Register("intro", "res://dialog/timelines/intro.dtl");
///   DialogRegistry.Instance.StartTimeline("intro");
///
/// Convention for keys:
///   npc_{id}          → primary NPC timeline
///   npc_{id}_after    → post-flag alternate
///   act_{id}_{option} → in-battle Act dialog
///   battle_{id}_turn  → enemy battle-turn dialog
///   cutscene_{name}   → scripted cutscene
/// </summary>
public partial class DialogRegistry : Node
{
	public static DialogRegistry Instance { get; private set; } = null!;

	/// <summary>Short keys — parallel array with Paths.</summary>
	[Export] public string[] ShortNames { get; set; } = [];
	/// <summary>Full res:// paths — parallel array with ShortNames.</summary>
	[Export] public string[] Paths { get; set; } = [];

	private readonly Dictionary<string, string> _map = new();

	public override void _Ready()
	{
		Instance = this;

		// Load any entries pre-configured via the inspector export arrays.
		for (int i = 0; i < ShortNames.Length && i < Paths.Length; i++)
		{
			if (!string.IsNullOrEmpty(ShortNames[i]) && !string.IsNullOrEmpty(Paths[i]))
				_map[ShortNames[i]] = Paths[i];
		}

		GD.Print($"[DialogRegistry] Ready — {_map.Count} pre-registered timeline(s).");
	}

	/// <summary>Register (or overwrite) a short key → full path mapping at runtime.</summary>
	public void Register(string key, string path) => _map[key] = path;

	/// <summary>
	/// Resolve a short key to its full path. Returns null if the key is unknown.
	/// If the input already looks like a full path (starts with "res://") it is returned as-is.
	/// </summary>
	public string? Resolve(string keyOrPath)
	{
		if (keyOrPath.StartsWith("res://")) return keyOrPath;
		return _map.TryGetValue(keyOrPath, out var path) ? path : null;
	}

	/// <summary>
	/// Start a timeline by short key or full path.
	/// Syncs GameManager flags into Dialogic before starting (via StartTimelineWithFlags).
	/// </summary>
	public void StartTimeline(string keyOrPath)
	{
		string? path = Resolve(keyOrPath);
		if (path == null)
		{
			GD.PushWarning($"[DialogRegistry] Unknown key: '{keyOrPath}'. Register it first or use a full res:// path.");
			return;
		}
		if (!ResourceLoader.Exists(path))
		{
			GD.PushWarning($"[DialogRegistry] Timeline file not found: '{path}'");
			return;
		}
		DialogicBridge.Instance.StartTimelineWithFlags(path);
	}
}
