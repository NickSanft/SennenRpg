namespace SennenRpg.Core.Data;

/// <summary>
/// Plain C# DTO storing one class's independent progression state.
/// JSON-serializable via System.Text.Json for SaveData persistence.
/// </summary>
public class ClassProgressionEntry
{
    public PlayerClass Class { get; set; } = PlayerClass.Bard;
    public int Level { get; set; } = 1;
    public int Exp   { get; set; } = 0;

    // Per-class base stats (accumulated via probabilistic growth rolls)
    public int MaxHp      { get; set; } = 20;
    public int Attack     { get; set; } = 10;
    public int Defense    { get; set; } = 0;
    public int Speed      { get; set; } = 10;
    public int Magic      { get; set; } = 0;
    public int Resistance { get; set; } = 0;
    public int Luck       { get; set; } = 0;
    public int MaxMp      { get; set; } = 0;
}
