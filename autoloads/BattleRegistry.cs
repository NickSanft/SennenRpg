using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

public partial class BattleRegistry : Node
{
	public static BattleRegistry Instance { get; private set; } = null!;

	// The encounter set by the overworld trigger, read by BattleScene on load
	private EncounterData? _pendingEncounter;

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
}
