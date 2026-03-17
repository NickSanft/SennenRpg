using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Gloomfish attack — radial burst spread.
/// Every 1.5 s fires 6 DirectedBullets evenly spread in a ring from the centre,
/// with a random angular offset each burst so the safe gaps rotate.
/// </summary>
public partial class Pattern003 : Node
{
	[Export] public PackedScene? BulletScene { get; set; }

	private Godot.Timer _timer = null!;

	private const int   BurstCount   = 6;
	private const float BulletSpeed  = 80f;

	public override void _Ready()
	{
		_timer = GetNode<Godot.Timer>("SpawnTimer");
		_timer.Timeout += OnSpawnTimer;
	}

	private void OnSpawnTimer()
	{
		if (BulletScene == null)
		{
			GD.PushWarning("[Pattern003] BulletScene not assigned.");
			return;
		}

		// Rotate the burst each time so the "safe" angle changes
		float offset = (float)GD.RandRange(0.0, Mathf.Tau / BurstCount);

		for (int i = 0; i < BurstCount; i++)
		{
			float angle = i * (Mathf.Tau / BurstCount) + offset;
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

			var bullet = BulletScene.Instantiate<DirectedBullet>();
			bullet.Position = Vector2.Zero; // spawn at box centre
			bullet.Speed    = BulletSpeed;
			bullet.SetDirection(dir);
			GetParent().AddChild(bullet);
		}
	}
}
