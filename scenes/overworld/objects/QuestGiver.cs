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
	[Export] public QuestData? QuestResource                 { get; set; }
	[Export] public string     OfferTimelinePath             { get; set; } = "";
	[Export] public string     ActiveReminderTimelinePath    { get; set; } = "";
	[Export] public string     TurnInTimelinePath            { get; set; } = "";
	[Export] public string     RewardedTimelinePath          { get; set; } = "";

	/// <summary>Quest state that was active when the player started talking.</summary>
	public QuestState StateAtTalkStart { get; private set; } = QuestState.Inactive;

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;
		if (QuestResource != null)
			QuestManager.Instance.RegisterQuest(QuestResource);
	}

	/// <summary>
	/// Returns the timeline path appropriate for the current quest state,
	/// and records <see cref="StateAtTalkStart"/> for post-dialog handling.
	/// Returns empty string if no quest override is needed.
	/// </summary>
	public string GetQuestTimelineOverride()
	{
		if (QuestResource == null) return "";
		StateAtTalkStart = QuestManager.Instance.GetState(QuestResource.QuestId);
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
		if (QuestResource == null) return;
		string id = QuestResource.QuestId;

		switch (StateAtTalkStart)
		{
			case QuestState.Inactive:
				QuestManager.Instance.ActivateQuest(id);
				break;

			case QuestState.ReadyToComplete:
				QuestManager.Instance.CompleteQuest(id);
				var screen = new QuestRewardScreen();
				sceneRoot.AddChild(screen);
				var rewards = QuestResource.GetRewards();
				int chosen  = await screen.ShowRewards(QuestResource.Title, QuestResource.BaseExpReward, rewards);
				ApplyReward(chosen, rewards);
				QuestManager.Instance.MarkRewarded(id);
				screen.QueueFree();
				break;
		}
	}

	private void ApplyReward(int index, IReadOnlyList<QuestRewardOption> rewards)
	{
		var gm = GameManager.Instance;
		gm.AddExp(QuestResource!.BaseExpReward);
		if (index < 0 || index >= rewards.Count) return;
		var r = rewards[index];
		if (r.ExpBonus  > 0) gm.AddExp(r.ExpBonus);
		if (r.GoldBonus > 0) gm.AddGold(r.GoldBonus);
		if (!string.IsNullOrEmpty(r.ItemPath) && ResourceLoader.Exists(r.ItemPath))
			gm.AddItem(r.ItemPath);
	}
}
