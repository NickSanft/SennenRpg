using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

public partial class BattleRegistry : Node
{
	public static BattleRegistry Instance { get; private set; } = null!;

	// The encounter set by the overworld trigger, read by BattleScene on load
	private EncounterData? _pendingEncounter;

	/// <summary>Background tint color sampled from the overworld tile at encounter location.</summary>
	public Color PendingBackgroundColor { get; set; } = new(0.55f, 0.3f, 0.85f);

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
	}

	public void SetPendingEncounter(EncounterData encounter)
	{
		_pendingEncounter = encounter;
	}

	public EncounterData? GetPendingEncounter()
	{
		var enc = _pendingEncounter;
		_pendingEncounter = null; // Consume once — BattleScene owns it after this
		return enc;
	}

	/// <summary>Convenience loader — loads an EnemyData resource by its file path.</summary>
	public EnemyData? LoadEnemy(string resourcePath)
	{
		if (!ResourceLoader.Exists(resourcePath))
		{
			GD.PushError($"[BattleRegistry] Enemy resource not found: {resourcePath}");
			return null;
		}
		return GD.Load<EnemyData>(resourcePath);
	}

	// ── Bestiary support ─────────────────────────────────────────────────────

	private System.Collections.Generic.List<EnemyData>? _allEnemiesCache;

	/// <summary>
	/// Returns every <see cref="EnemyData"/> resource discovered under
	/// <c>res://resources/enemies/</c>, sorted by <see cref="EnemyData.EnemyId"/>.
	/// Lazily populated on first access — used by the Bestiary menu to show locked
	/// silhouettes for enemies the player has not yet defeated.
	/// </summary>
	public System.Collections.Generic.IReadOnlyList<EnemyData> AllEnemies()
	{
		if (_allEnemiesCache != null) return _allEnemiesCache;
		_allEnemiesCache = LoadAllEnemiesFromDisk();
		return _allEnemiesCache;
	}

	private static System.Collections.Generic.List<EnemyData> LoadAllEnemiesFromDisk()
	{
		var list = new System.Collections.Generic.List<EnemyData>();
		using var dir = DirAccess.Open("res://resources/enemies");
		if (dir == null) return list;

		dir.ListDirBegin();
		string file;
		while ((file = dir.GetNext()) != "")
		{
			if (dir.CurrentIsDir() || !file.EndsWith(".tres")) continue;
			var enemy = GD.Load<EnemyData>($"res://resources/enemies/{file}");
			if (enemy != null) list.Add(enemy);
		}
		dir.ListDirEnd();

		list.Sort((a, b) => string.CompareOrdinal(a.EnemyId, b.EnemyId));
		return list;
	}
}
