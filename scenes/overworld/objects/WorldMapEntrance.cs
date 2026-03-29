using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Placed on the world map at an entrance tile.
/// WorldMap detects it by calling MatchesTile() after each player step —
/// no physics collision is used.
/// </summary>
[Tool]
public partial class WorldMapEntrance : Area2D
{
	/// <summary>Scene to load when the player steps onto this tile.</summary>
	[Export] public string   TargetScenePath { get; set; } = "";
	/// <summary>World-map tile to return the player to when they leave the target scene.</summary>
	[Export] public Vector2I ReturnTile      { get; set; } = Vector2I.Zero;
	[Export] public string   EntranceName    { get; set; } = "";

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;

		// Collision shape for editor visibility only — layer/mask are both 0
		var shape  = new CollisionShape2D();
		shape.Shape = new RectangleShape2D { Size = new Vector2(16f, 16f) };
		AddChild(shape);
		CollisionLayer = 0;
		CollisionMask  = 0;
	}

	/// <summary>Returns true when the player's current tile overlaps this entrance.</summary>
	public bool MatchesTile(Vector2I tile)
		=> WorldMapPlayer.WorldToTile(GlobalPosition) == tile;
}
