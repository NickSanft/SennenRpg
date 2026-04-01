namespace SennenRpg.Core.Data;

/// <summary>
/// Plain-data DTO for a Lily-generated piece of equipment.
/// Stored in <c>SaveData.DynamicEquipmentInventory</c> and serialised to JSON.
/// Not a Godot Resource — safe to instantiate in NUnit tests.
/// </summary>
public class DynamicEquipmentSave
{
    public string       Id          { get; set; } = "";
    public string       DisplayName { get; set; } = "";
    public string       Description { get; set; } = "";
    public EquipmentSlot Slot       { get; set; } = EquipmentSlot.Accessory;
    public int          BonusMaxHp  { get; set; }
    public int          BonusAttack { get; set; }
    public int          BonusDefense{ get; set; }
    public int          BonusMagic  { get; set; }
    public int          BonusSpeed  { get; set; }
    public int          BonusLuck   { get; set; }
}
