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

	/// <summary>Number of frames in the sprite sheet (0 = static sprite).</summary>
	[Export] public int SpriteFrameCount { get; set; } = 0;
	/// <summary>Width/height of each frame in the sprite sheet.</summary>
	[Export] public int SpriteFrameSize { get; set; } = 32;
	/// <summary>Animation FPS for sprite sheet playback.</summary>
	[Export] public float SpriteAnimFps { get; set; } = 6f;

	// ── Rhythm fields ──────────────────────────────────────────────────
	/// <summary>BPM of this enemy's battle track. 0 = use encounter/default BPM.</summary>
    [Export] public float BattleBpm { get; set; } = 0f;
	/// <summary>BGM to play during this enemy's battle. Leave empty for the default battle track.</summary>
	[Export] public string BattleBgmPath { get; set; } = "";
	/// <summary>
	/// Seconds from the start of the audio file to the first beat.
	/// Set this if the track has silence or a musical intro before beat 1.
	/// </summary>
	[Export] public float BattleBeatOffsetSec { get; set; } = 0f;
	/// <summary>Which Bard skills this enemy responds to.</summary>
	[Export] public string[] BardicActOptions { get; set; } = [];

	// ── Legacy Act fields (still used for Check/flavor text) ───────────
	[Export] public string[] ActOptions { get; set; } = [];
	[Export] public string[] ActResultTexts { get; set; } = [];

	[Export] public string[] BattleDialogLines { get; set; } = [];
	[Export] public string BattleTimelinePath { get; set; } = "";
	[Export] public int GoldDrop { get; set; } = 0;
	[Export] public int ExpDrop { get; set; } = 0;
	[Export] public PackedScene? AttackPatternScene { get; set; }

	/// <summary>Item awarded as bonus loot when the Rhythm Memory adaptation roll succeeds. Leave empty for no bonus loot.</summary>
	[Export] public string BonusLootItemPath { get; set; } = "";

	/// <summary>
	/// Per-enemy loot table. One entry is rolled per kill via <see cref="LootLogic.RollLoot"/>.
	/// Each element is a <see cref="LootEntry"/> resource. Empty = no item drop on kill.
	///
	/// Per CLAUDE.md sub-resource rule: this MUST be typed as <c>Resource[]</c> (not
	/// <c>LootEntry[]</c>) so the C++ serializer can handle it. Cast at runtime via
	/// <c>.OfType&lt;LootEntry&gt;()</c>.
	/// </summary>
	[Export] public Resource[] LootTable { get; set; } = [];
}
