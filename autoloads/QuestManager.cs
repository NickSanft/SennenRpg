using Godot;
using System.Collections.Generic;
using System.Linq;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

public enum QuestState { Inactive, Active, ReadyToComplete, Completed, Rewarded }

/// <summary>
/// Autoload that tracks quest states via a state machine and evaluates
/// conditions whenever kills or flags change.
/// </summary>
public partial class QuestManager : Node
{
	public static QuestManager Instance { get; private set; } = null!;

	[Signal] public delegate void QuestActivatedEventHandler(string questId);
	[Signal] public delegate void QuestReadyToCompleteEventHandler(string questId);
	[Signal] public delegate void QuestCompletedEventHandler(string questId);

	private readonly Dictionary<string, QuestData>    _registry    = new();
	private readonly Dictionary<string, QuestState>   _questStates = new();

	public override void _Ready()
	{
		Instance    = this;
		ProcessMode = ProcessModeEnum.Always;
	}

	// ── Registry ──────────────────────────────────────────────────────────────

	/// <summary>Register a QuestData resource (call from QuestGiver._Ready).</summary>
	public void RegisterQuest(QuestData quest)
	{
		_registry[quest.QuestId] = quest;
		_questStates.TryAdd(quest.QuestId, QuestState.Inactive);
	}

	public QuestData? GetQuest(string questId) =>
		_registry.TryGetValue(questId, out var q) ? q : null;

	// ── State accessors ───────────────────────────────────────────────────────

	public QuestState GetState(string questId) =>
		_questStates.GetValueOrDefault(questId, QuestState.Inactive);

	public bool IsActive(string questId)          => GetState(questId) == QuestState.Active;
	public bool IsReadyToComplete(string questId) => GetState(questId) == QuestState.ReadyToComplete;
	public bool IsCompleted(string questId)       => GetState(questId) >= QuestState.Completed;
	public bool IsRewarded(string questId)        => GetState(questId) == QuestState.Rewarded;

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	public void ActivateQuest(string questId)
	{
		if (_questStates.GetValueOrDefault(questId) != QuestState.Inactive) return;
		_questStates[questId] = QuestState.Active;
		EmitSignal(SignalName.QuestActivated, questId);
		GD.Print($"[QuestManager] Quest activated: {questId}");
	}

	public void CompleteQuest(string questId)
	{
		var state = _questStates.GetValueOrDefault(questId);
		if (state != QuestState.ReadyToComplete && state != QuestState.Active) return;
		_questStates[questId] = QuestState.Completed;
		EmitSignal(SignalName.QuestCompleted, questId);
		GD.Print($"[QuestManager] Quest completed: {questId}");
	}

	public void MarkRewarded(string questId) => _questStates[questId] = QuestState.Rewarded;

	// ── Condition evaluation ──────────────────────────────────────────────────

	/// <summary>Called from GameManager.RecordKill after each enemy defeat.</summary>
	public void NotifyKill(string enemyId)
	{
		foreach (var id in ActiveQuestIds())
		{
			if (_registry.TryGetValue(id, out var quest))
				CheckConditions(id, quest);
		}
	}

	/// <summary>Called from GameManager.SetFlag after any flag changes.</summary>
	public void NotifyFlagChanged(string flagKey)
	{
		foreach (var id in ActiveQuestIds())
		{
			if (_registry.TryGetValue(id, out var quest))
				CheckConditions(id, quest);
		}
	}

	private void CheckConditions(string questId, QuestData data)
	{
		var gm    = GameManager.Instance;
		var flags = new System.Collections.Generic.Dictionary<string, bool>(gm.Flags);
		bool met  = QuestLogic.AreAllConditionsMet(data.GetConditions(), flags, gm.KillCounts);
		if (met && _questStates[questId] == QuestState.Active)
		{
			_questStates[questId] = QuestState.ReadyToComplete;
			EmitSignal(SignalName.QuestReadyToComplete, questId);
			GD.Print($"[QuestManager] Quest ready to complete: {questId}");
		}
	}

	// ── Save / load ───────────────────────────────────────────────────────────

	public List<string> GetActiveQuestIds() =>
		_questStates
			.Where(kv => kv.Value is QuestState.Active or QuestState.ReadyToComplete)
			.Select(kv => kv.Key).ToList();

	public List<string> GetCompletedQuestIds() =>
		_questStates
			.Where(kv => kv.Value >= QuestState.Completed)
			.Select(kv => kv.Key).ToList();

	/// <summary>Restore quest states from a loaded save file.</summary>
	public void ApplySaveData(List<string> activeIds, List<string> completedIds)
	{
		foreach (var id in activeIds)
			_questStates[id] = QuestState.Active;
		foreach (var id in completedIds)
			_questStates[id] = QuestState.Rewarded;
	}

	// helper — returns snapshot to avoid mutation during iteration
	private List<string> ActiveQuestIds() =>
		_questStates.Where(kv => kv.Value == QuestState.Active).Select(kv => kv.Key).ToList();
}
