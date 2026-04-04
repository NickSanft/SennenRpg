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
}
