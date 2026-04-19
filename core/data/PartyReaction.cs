namespace SennenRpg.Core.Data;

public readonly record struct PartyReaction(
    string MemberId, string MapId, string Text, int Priority);
