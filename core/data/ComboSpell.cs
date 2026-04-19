namespace SennenRpg.Core.Data;

public readonly record struct ComboSpell(
    string Id,
    string DisplayName,
    string MemberA,
    string MemberB,
    int MpCostA,
    int MpCostB,
    ComboSpellType DamageType);
