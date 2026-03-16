using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

public partial class OverworldBase : Node2D
{
	[Export] public string MapId { get; set; } = "";
	[Export] public string BgmPath { get; set; } = "";

	protected Node2D YSort = null!;

	public override void _Ready()
	{
		YSort = GetNode<Node2D>("YSort");
		GameManager.Instance.SetLastMap(SceneFilePath);
		GameManager.Instance.SetState(GameState.Overworld);

		// Spawn player into YSort so it Y-sorts with NPCs
		var playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
		var player = playerScene.Instantiate<CharacterBody2D>();
		YSort.AddChild(player);
		player.Position = GetSpawnPosition();

		if (!string.IsNullOrEmpty(BgmPath))
			AudioManager.Instance.PlayBgm(BgmPath);
	}

	/// <summary>Override in map subclasses to set the player spawn point.</summary>
	protected virtual Vector2 GetSpawnPosition() => Vector2.Zero;
}
