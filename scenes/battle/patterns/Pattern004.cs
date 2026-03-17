using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Dustmote attack — chaotic bouncing sparks.
/// Every 0.5 s spawns 1–2 BouncingBullets from random box edges travelling inward at
/// random angles. Fast and unpredictable but individually weak.
/// </summary>
public partial class Pattern004 : Node
{
	[Export] public PackedScene? BulletScene { get; set; }

	private Godot.Timer _timer = null!;

	private const float BoxHalfX = 72f;
	private const float BoxHalfY = 52f;

	public override void _Ready()
	{
		_timer = GetNode<Godot.Timer>("SpawnTimer");
		_timer.Timeout += OnSpawnTimer;
	}

	private void OnSpawnTimer()
	{
		if (BulletScene == null)
		{
			GD.PushWarning("[Pattern004] BulletScene not assigned.");
			return;
		}

		int count = (int)GD.RandRange(1, 3);
		for (int i = 0; i < count; i++)
		{
			SpawnOne();
		}
	}

	private void SpawnOne()
	{
		var bullet = BulletScene.Instantiate<BouncingBullet>();

		// Pick a random edge to spawn from
		int edge = (int)GD.RandRange(0, 4);
		bullet.Position = edge switch
		{
			0 => new Vector2((float)GD.RandRange(-BoxHalfX, BoxHalfX), -BoxHalfY), // top
			1 => new Vector2((float)GD.RandRange(-BoxHalfX, BoxHalfX),  BoxHalfY), // bottom
			2 => new Vector2(-BoxHalfX, (float)GD.RandRange(-BoxHalfY, BoxHalfY)), // left
			_ => new Vector2( BoxHalfX, (float)GD.RandRange(-BoxHalfY, BoxHalfY)), // right
		};

		float angle = (float)GD.RandRange(0.0, Mathf.Tau);
		bullet.SetDirection(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
		bullet.Speed = (float)GD.RandRange(85.0, 135.0);

		GetParent().AddChild(bullet);
	}
}
