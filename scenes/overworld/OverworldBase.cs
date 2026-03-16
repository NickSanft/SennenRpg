using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

public partial class OverworldBase : Node2D
{
	[Export] public string MapId { get; set; } = "";
	[Export] public string BgmPath { get; set; } = "";

	protected Node2D YSort = null!;
	private CharacterBody2D? _player;
	private Camera2D? _camera;

	public override void _Ready()
	{
		YSort = GetNode<Node2D>("YSort");
		GameManager.Instance.SetLastMap(SceneFilePath);
		GameManager.Instance.SetState(GameState.Overworld);

		// Spawn player into YSort so it Y-sorts with NPCs
		var playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
		_player = playerScene.Instantiate<CharacterBody2D>();
		YSort.AddChild(_player);
		_player.GlobalPosition = GetSpawnPosition();

		// Configure camera — set zoom here; position is updated every frame in _Process
		_camera = GetNode<Camera2D>("Camera");
		_camera.Zoom = new Vector2(3, 3);
		_camera.GlobalPosition = _player.GlobalPosition;

		if (!string.IsNullOrEmpty(BgmPath))
			AudioManager.Instance.PlayBgm(BgmPath);

		GD.Print($"[OverworldBase] Ready. Map: {MapId}, Player spawned at {_player.GlobalPosition}");
	}

	public override void _Process(double delta)
	{
		// Smoothly follow the player each frame
		if (_player != null && _camera != null)
			_camera.GlobalPosition = _player.GlobalPosition;
	}

	/// <summary>Override in map subclasses to set the player spawn point.</summary>
	protected virtual Vector2 GetSpawnPosition() => Vector2.Zero;
}
