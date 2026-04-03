using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Menus;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Child node added to any Npc that offers a quest.
/// Add as a child named "QuestGiver" in the Npc scene.
/// </summary>
[Tool]
public partial class QuestGiver : Node
{
	/// <summary>
	/// Typed as Resource so Godot's C++ serialiser doesn't cast-fail on external .tres files;
	/// use the <see cref="Quest"/> accessor for typed access at runtime.
	/// </summary>
	[Export] public Resource?  QuestResource                 { get; set; }
	[Export] public string     OfferTimelinePath             { get; set; } = "";
	[Export] public string     ActiveReminderTimelinePath    { get; set; } = "";
	[Export] public string     TurnInTimelinePath            { get; set; } = "";
	[Export] public string     RewardedTimelinePath          { get; set; } = "";

	private QuestData? Quest => QuestResource as QuestData;

	/// <summary>Quest state that was active when the player started talking.</summary>
	public QuestState StateAtTalkStart { get; private set; } = QuestState.Inactive;

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;
		if (Quest != null)
			QuestManager.Instance.RegisterQuest(Quest);
	}

	/// <summary>True when this quest is active or ready to turn in.</summary>
	public bool HasActiveQuest()
	{
		if (Quest == null) return false;
		var state = QuestManager.Instance.GetState(Quest.QuestId);
		return state is QuestState.Active or QuestState.ReadyToComplete;
	}

	/// <summary>
	/// Returns the timeline path appropriate for the current quest state,
	/// and records <see cref="StateAtTalkStart"/> for post-dialog handling.
	/// Returns empty string if no quest override is needed.
	/// </summary>
	public string GetQuestTimelineOverride()
	{
		if (Quest == null) return "";
		StateAtTalkStart = QuestManager.Instance.GetState(Quest.QuestId);
		return StateAtTalkStart switch
		{
			QuestState.Inactive          => OfferTimelinePath,
			QuestState.Active            => ActiveReminderTimelinePath,
			QuestState.ReadyToComplete   => TurnInTimelinePath,
			QuestState.Rewarded          => RewardedTimelinePath,
			_                            => "",
		};
	}

	/// <summary>
	/// Called by Npc after the quest dialog timeline ends.
	/// Handles quest activation (offer → active) and turn-in (readyToComplete → rewarded).
	/// </summary>
	public async Task HandlePostDialog(Node sceneRoot)
	{
		if (Quest == null) return;
		string id = Quest.QuestId;

		switch (StateAtTalkStart)
		{
			case QuestState.Inactive:
				QuestManager.Instance.ActivateQuest(id);
				break;

			case QuestState.ReadyToComplete:
				QuestManager.Instance.CompleteQuest(id);
				var screen = new QuestRewardScreen();
				sceneRoot.AddChild(screen);
				var rewards = Quest.GetRewards();
				int chosen  = await screen.ShowRewards(Quest.Title, Quest.BaseExpReward, rewards);
				ApplyReward(chosen, rewards);
				QuestManager.Instance.MarkRewarded(id);
				screen.QueueFree();
				break;
		}
	}

	private void ApplyReward(int index, IReadOnlyList<QuestRewardOption> rewards)
	{
		var gm = GameManager.Instance;
		gm.AddExp(Quest!.BaseExpReward);
		if (index < 0 || index >= rewards.Count) return;
		var r = rewards[index];
		if (r.ExpBonus  > 0) gm.AddExp(r.ExpBonus);
		if (r.GoldBonus > 0) gm.AddGold(r.GoldBonus);
		if (!string.IsNullOrEmpty(r.ItemPath) && ResourceLoader.Exists(r.ItemPath))
			gm.AddItem(r.ItemPath);
	}
}
