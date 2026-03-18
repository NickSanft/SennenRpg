using Godot;

namespace SennenRpg.Core.Data;

[GlobalClass]
public partial class EnemyData : Resource
{
	[Export] public string EnemyId { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	[Export] public string FlavorText { get; set; } = "";
	[Export] public CharacterStats? Stats { get; set; }
	[Export] public Texture2D? BattleSprite { get; set; }

	// ── Rhythm fields ──────────────────────────────────────────────────
	/// <summary>BPM of this enemy's battle track. 0 = use encounter/default BPM.</summary>
    [Export] public float BattleBpm { get; set; } = 0f;
	/// <summary>BGM to play during this enemy's battle. Leave empty for the default battle track.</summary>
	[Export] public string BattleBgmPath { get; set; } = "";
	/// <summary>Which Bard skills this enemy responds to. Parallel to SkillMercyValues.</summary>
	[Export] public string[] BardicActOptions { get; set; } = [];
	/// <summary>Mercy % granted by each successful Bard skill. Parallel to BardicActOptions.</summary>
	[Export] public int[] SkillMercyValues { get; set; } = [];

	// ── Legacy Act fields (still used for Check/flavor text) ───────────
	[Export] public string[] ActOptions { get; set; } = [];
	[Export] public int[] ActMercyValues { get; set; } = [];
	[Export] public string[] ActResultTexts { get; set; } = [];

	[Export] public string[] BattleDialogLines { get; set; } = [];
	[Export] public string BattleTimelinePath { get; set; } = "";
	[Export] public bool CanBeSpared { get; set; } = false;
	[Export] public int GoldDrop { get; set; } = 0;
	[Export] public int ExpDrop { get; set; } = 0;
	[Export] public PackedScene? AttackPatternScene { get; set; }
}
