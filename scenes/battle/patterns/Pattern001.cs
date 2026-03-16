using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Simple falling-rain attack pattern.
/// Spawns StraightBullets at random X positions above the DodgeBox every 0.4 seconds.
/// Bullets are added to the parent node (BulletContainer inside DodgeBox).
/// </summary>
public partial class Pattern001 : Node
{
	[Export] public PackedScene? BulletScene { get; set; }

	private Godot.Timer _timer = null!;

	// Horizontal spawn range relative to DodgeBox center (matches a 160-wide box)
	private const float SpawnRangeX = 65f;
	private const float SpawnY      = -80f; // just above the box

	public override void _Ready()
	{
		_timer = GetNode<Godot.Timer>("SpawnTimer");
		_timer.Timeout += OnSpawnTimerTimeout;
	}

	private void OnSpawnTimerTimeout()
	{
		if (BulletScene == null)
		{
			GD.PushWarning("[Pattern001] BulletScene not assigned.");
			return;
		}

		var bullet = BulletScene.Instantiate<StraightBullet>();
		bullet.Position = new Vector2((float)GD.RandRange(-SpawnRangeX, SpawnRangeX), SpawnY);
		bullet.Speed    = 90f;
		GetParent().AddChild(bullet);
	}
}
