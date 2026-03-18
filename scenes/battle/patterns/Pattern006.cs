using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Flickerfly attack — zigzag shower.
/// Every 0.55 s spawns 1–2 ZigZagBullets at random X positions above the box.
/// Each bullet has randomised amplitude and frequency so no two weave the same way.
/// </summary>
public partial class Pattern006 : Node
{
	[Export] public PackedScene? BulletScene { get; set; }

	private Godot.Timer _timer = null!;

	private const float SpawnY    = -55f;
	private const float SpawnHalfX = 62f;

	public override void _Ready()
	{
		_timer = GetNode<Godot.Timer>("SpawnTimer");
		_timer.Timeout += OnSpawnTimer;
	}

	private void OnSpawnTimer()
	{
		if (BulletScene == null)
		{
			GD.PushWarning("[Pattern006] BulletScene not assigned.");
			return;
		}

		int count = (int)GD.RandRange(1, 3);
		for (int i = 0; i < count; i++)
			SpawnOne(BulletScene);
	}

	private void SpawnOne(PackedScene bulletScene)
	{
		var bullet = bulletScene.Instantiate<ZigZagBullet>();
		bullet.Position      = new Vector2((float)GD.RandRange(-SpawnHalfX, SpawnHalfX), SpawnY);
		bullet.Speed         = (float)GD.RandRange(55.0, 88.0);
		bullet.OscAmplitude  = (float)GD.RandRange(12.0, 30.0);
		bullet.OscFrequency  = (float)GD.RandRange(3.0,   7.0);
		bullet.SetDirection(Vector2.Down);
		GetParent().AddChild(bullet);
	}
}
