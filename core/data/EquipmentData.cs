using Godot;

namespace SennenRpg.Core.Data;

[GlobalClass]
public partial class EquipmentData : Resource
{
    [Export] public string        EquipmentId   { get; set; } = "";
    [Export] public string        DisplayName   { get; set; } = "";
    [Export] public string        Description   { get; set; } = "";
    [Export] public Texture2D?    Icon          { get; set; }
    [Export] public EquipmentSlot Slot          { get; set; } = EquipmentSlot.Weapon;

    // Individual stat bonuses (avoids pulling in CharacterStats for testability)
    [Export] public int BonusMaxHp      { get; set; }
    [Export] public int BonusAttack     { get; set; }
    [Export] public int BonusDefense    { get; set; }
    [Export] public int BonusMagic      { get; set; }
    [Export] public int BonusResistance { get; set; }
    [Export] public int BonusSpeed      { get; set; }
    [Export] public int BonusLuck       { get; set; }

    /// <summary>Returns bonuses as a pure-C# struct for use in EquipmentLogic.</summary>
    public EquipmentBonuses Bonuses =>
        new(BonusMaxHp, BonusAttack, BonusDefense, BonusMagic, BonusResistance, BonusSpeed, BonusLuck);
}
