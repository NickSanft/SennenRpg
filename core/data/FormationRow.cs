namespace SennenRpg.Core.Data;

/// <summary>
/// Battle formation row for a party member.
/// Front-row members are exposed to physical attacks; back-row members are safer.
/// (Effects are applied in Phase 7 when the multi-actor battle ships;
/// Phase 2 introduces the field so the data can be stored on save.)
/// </summary>
public enum FormationRow
{
    Front = 0,
    Back  = 1,
}
