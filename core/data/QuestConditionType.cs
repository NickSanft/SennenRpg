namespace SennenRpg.Core.Data;

/// <summary>The kinds of conditions that can gate quest progress.</summary>
public enum QuestConditionType
{
    /// <summary>Kill a specific enemy type N times.</summary>
    KillCount,

    /// <summary>A named story flag is set to true.</summary>
    Flag,

    /// <summary>The player has talked to a specific NPC (flag "talked_to_&lt;npcId&gt;" is set).</summary>
    TalkTo,
}
