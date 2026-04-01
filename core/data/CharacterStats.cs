using Godot;

namespace SennenRpg.Core.Data;

[GlobalClass]
public partial class CharacterStats : Resource
{
	[Export] public int MaxHp { get; set; } = 20;
	[Export] public int CurrentHp { get; set; } = 20;
	[Export] public int Attack { get; set; } = 10;
	[Export] public int Defense { get; set; } = 0;
	/// <summary>Battle turn order — higher acts first. 0–99 scale.</summary>
	[Export] public int Speed { get; set; } = 10;
	/// <summary>Spell attack power. Used by magic skills to calculate damage.</summary>
	[Export] public int Magic { get; set; } = 0;
	/// <summary>Magic defence — reduces incoming spell damage like Defense does for physical.</summary>
	[Export] public int Resistance { get; set; } = 0;
	/// <summary>Critical hit chance. Crit probability = Luck / 200f (max 99 → ~49.5%).</summary>
	[Export] public int Luck { get; set; } = 0;
	/// <summary>Overworld movement speed in pixels per second.</summary>
	[Export] public float MoveSpeed { get; set; } = 80f;
	[Export] public float InvincibilityDuration { get; set; } = 1.5f;
	/// <summary>Maximum mana points. 0 for characters that do not cast spells.</summary>
	[Export] public int MaxMp { get; set; } = 0;
	/// <summary>Current mana points.</summary>
	[Export] public int CurrentMp { get; set; } = 0;
	/// <summary>Class archetype selected during character creation.</summary>
	[Export] public PlayerClass Class     { get; set; } = PlayerClass.Bard;
	[Export] public string      ClassName { get; set; } = "Bard";
}
