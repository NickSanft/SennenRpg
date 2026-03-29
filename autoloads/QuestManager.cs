using Godot;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

/// <summary>
/// Tracks active and completed quests; evaluates completion conditions
/// each time a kill or flag changes.
/// </summary>
public partial class QuestManager : Node
{
	public static QuestManager Instance { get; private set; } = null!;

	[Signal] public delegate void QuestAcceptedEventHandler(string questId);
	[Signal] public delegate void QuestCompletedEventHandler(string questId);

	/// <summary>Quests the player has accepted but not yet completed.</summary>
	public List<string> ActiveQuestIds    { get; } = new();
	/// <summary>Quests the player has already finished.</summary>
	public List<string> CompletedQuestIds { get; } = new();

	/// <summary>Loaded QuestData resources, keyed by QuestId.</summary>
	private readonly Dictionary<string, QuestData> _registry = new();

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
	}

	// ── Registry ──────────────────────────────────────────────────────────────

	/// <summary>Register a QuestData resource so the manager can track it.</summary>
	public void RegisterQuest(QuestData quest) => _registry[quest.QuestId] = quest;

	public QuestData? GetQuest(string questId) =>
		_registry.TryGetValue(questId, out var q) ? q : null;

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	/// <summary>Accept a quest and begin tracking its conditions.</summary>
	public void AcceptQuest(string questId)
	{
		if (ActiveQuestIds.Contains(questId) || CompletedQuestIds.Contains(questId)) return;
		ActiveQuestIds.Add(questId);
		EmitSignal(SignalName.QuestAccepted, questId);
		GD.Print($"[QuestManager] Accepted quest: {questId}");
	}

	/// <summary>
	/// Call after every kill or flag change to check if any active quest
	/// has just become completable. Does NOT auto-complete — completion
	/// requires the player to turn in via QuestGiver.
	/// </summary>
	public void EvaluateAll()
	{
		var gm = GameManager.Instance;
		// Convert Godot dictionary to System one for QuestLogic
		var flags      = new Dictionary<string, bool>(gm.Flags);
		var killCounts = gm.KillCounts;

		foreach (var id in ActiveQuestIds)
		{
			if (!_registry.TryGetValue(id, out var quest)) continue;
			var conditions = quest.GetConditions();
			bool met = QuestLogic.AreAllConditionsMet(conditions, flags, killCounts);
			GD.Print($"[QuestManager] Quest '{id}' conditions met: {met}");
		}
	}

	/// <summary>Returns true if the quest exists, is active, and all conditions are met.</summary>
	public bool IsReadyToComplete(string questId)
	{
		if (!ActiveQuestIds.Contains(questId)) return false;
		if (!_registry.TryGetValue(questId, out var quest)) return false;
		var gm         = GameManager.Instance;
		var flags      = new Dictionary<string, bool>(gm.Flags);
		var killCounts = gm.KillCounts;
		return QuestLogic.AreAllConditionsMet(quest.GetConditions(), flags, killCounts);
	}

	/// <summary>Marks the quest complete and fires QuestCompleted signal.</summary>
	public void CompleteQuest(string questId)
	{
		if (!ActiveQuestIds.Remove(questId)) return;
		CompletedQuestIds.Add(questId);
		EmitSignal(SignalName.QuestCompleted, questId);
		GD.Print($"[QuestManager] Completed quest: {questId}");
	}

	public bool IsActive(string questId)    => ActiveQuestIds.Contains(questId);
	public bool IsCompleted(string questId) => CompletedQuestIds.Contains(questId);

	// ── Save / Load support ───────────────────────────────────────────────────

	public void ApplySaveData(List<string> activeIds, List<string> completedIds)
	{
		ActiveQuestIds.Clear();
		ActiveQuestIds.AddRange(activeIds);
		CompletedQuestIds.Clear();
		CompletedQuestIds.AddRange(completedIds);
	}
}
