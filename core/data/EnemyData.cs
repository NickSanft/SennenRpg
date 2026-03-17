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
	[Export] public string[] ActOptions { get; set; } = [];
	/// <summary>How much mercy % each Act option adds. Parallel array with ActOptions.</summary>
	[Export] public int[] ActMercyValues { get; set; } = [];
	/// <summary>Flavor text shown in the dialog box after each Act. Parallel array with ActOptions.</summary>
	[Export] public string[] ActResultTexts { get; set; } = [];
	/// <summary>Lines randomly picked for enemy dialog before its attack (plain text fallback).</summary>
	[Export] public string[] BattleDialogLines { get; set; } = [];
	/// <summary>
	/// Optional Dialogic timeline to play at the start of the enemy's turn instead of
	/// a random BattleDialogLine. Leave empty to use the plain-text fallback.
	/// Convention: "res://dialog/timelines/battle_{enemyId}_turn.dtl"
	/// </summary>
	[Export] public string BattleTimelinePath { get; set; } = "";
	[Export] public bool CanBeSpared { get; set; } = false;
	[Export] public int GoldDrop { get; set; } = 0;
	[Export] public int ExpDrop { get; set; } = 0;
	[Export] public PackedScene? AttackPatternScene { get; set; }
}
