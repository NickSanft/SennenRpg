using Godot;

namespace SennenRpg.Core.Data;

[GlobalClass]
public partial class CharacterStats : Resource
{
    [Export] public int MaxHp { get; set; } = 20;
    [Export] public int CurrentHp { get; set; } = 20;
    [Export] public int Attack { get; set; } = 10;
    [Export] public int Defense { get; set; } = 0;
    [Export] public float Speed { get; set; } = 80f;
    [Export] public float InvincibilityDuration { get; set; } = 1.5f;
}
