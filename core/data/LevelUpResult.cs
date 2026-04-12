namespace SennenRpg.Core.Data;

/// <summary>
/// Snapshot of a single level-up: the level gained and before/after values for every stat.
/// Produced by GameManager.CheckAndApplyLevelUp; consumed by LevelUpScreen for animation.
/// Plain class — not a Godot Resource.
/// </summary>
public sealed class LevelUpResult
{
    public int NewLevel      { get; init; }

    /// <summary>Display name of the party member who levelled up. Empty = legacy/Sen.</summary>
    public string MemberName { get; set; } = "";
    /// <summary>Stable member ID for milestone/bonus lookup. Empty = legacy (defaults to "sen").</summary>
    public string MemberId   { get; set; } = "";
    /// <summary>Class name shown alongside the member's name in the level-up title.</summary>
    public string ClassName  { get; set; } = "";

    public int OldMaxHp      { get; init; }  public int NewMaxHp      { get; init; }
    public int OldAttack     { get; init; }  public int NewAttack     { get; init; }
    public int OldDefense    { get; init; }  public int NewDefense    { get; init; }
    public int OldSpeed      { get; init; }  public int NewSpeed      { get; init; }
    public int OldMagic      { get; init; }  public int NewMagic      { get; init; }
    public int OldResistance { get; init; }  public int NewResistance { get; init; }
    public int OldLuck       { get; init; }  public int NewLuck       { get; init; }
}
