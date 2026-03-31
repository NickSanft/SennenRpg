using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-logic helpers for NPC behaviour, extracted from Npc.cs for unit-testability.
/// No Godot runtime required.
/// </summary>
public static class NpcLogic
{
	/// <summary>
	/// Selects the correct dialog timeline path from a default + parallel flag/path arrays.
	/// Iterates <paramref name="altRequiredFlags"/> in order; returns the matching
	/// <paramref name="altTimelinePaths"/> entry for the first flag that
	/// <paramref name="flagChecker"/> returns <c>true</c> for.
	/// Falls back to <paramref name="defaultPath"/> when nothing matches.
	/// Array length mismatches are handled safely — only the shorter length is checked.
	/// Empty flag strings are skipped.
	/// </summary>
	public static string SelectTimeline(
		string            defaultPath,
		string[]          altRequiredFlags,
		string[]          altTimelinePaths,
		Func<string, bool> flagChecker)
	{
		int count = Math.Min(altRequiredFlags.Length, altTimelinePaths.Length);
		for (int i = 0; i < count; i++)
		{
			if (!string.IsNullOrEmpty(altRequiredFlags[i]) && flagChecker(altRequiredFlags[i]))
				return altTimelinePaths[i];
		}
		return defaultPath;
	}

	/// <summary>
	/// Derives the "revisit" timeline path by inserting "_again" before the ".dtl" extension.
	/// Returns <paramref name="basePath"/> unchanged when <paramref name="talkFlag"/> is false
	/// or when the path does not contain ".dtl".
	/// </summary>
	/// <example>"res://…/npc_gus.dtl" → "res://…/npc_gus_again.dtl"</example>
	public static string GetRevisitPath(string basePath, bool talkFlag)
	{
		if (!talkFlag) return basePath;
		int ext = basePath.LastIndexOf(".dtl", StringComparison.OrdinalIgnoreCase);
		if (ext < 0) return basePath;
		return basePath.Substring(0, ext) + "_again.dtl";
	}
}
