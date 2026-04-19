namespace SennenRpg.Core.Data;

public readonly record struct Trophy(
    string Id, string DisplayName, string Description,
    string IconLetter, bool IsHidden, TrophyCategory Category);
