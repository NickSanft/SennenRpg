using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Stonecrawler attack — column volley with a gap.
/// Every 1.8 s fires a row of 5 slow DirectedBullets across the box width,
/// leaving one column randomly empty. The player must spot and hold the safe column.
/// </summary>
public partial class Pattern005 : Node
{
	[Export] public PackedScene? BulletScene { get; set; }

	private Godot.Timer _timer = null!;

	private static readonly float[] Columns = { -56f, -28f, 0f, 28f, 56f };
	private const float SpawnY      = -55f;
	private const float BulletSpeed = 48f;

	public override void _Ready()
	{
		_timer = GetNode<Godot.Timer>("SpawnTimer");
		_timer.Timeout += OnSpawnTimer;
	}

	private void OnSpawnTimer()
	{
		if (BulletScene == null)
		{
			GD.PushWarning("[Pattern005] BulletScene not assigned.");
			return;
		}

		int gap = (int)GD.RandRange(0, Columns.Length);

		for (int i = 0; i < Columns.Length; i++)
		{
			if (i == gap) continue; // leave this column open

			var bullet = BulletScene.Instantiate<DirectedBullet>();
			bullet.Position = new Vector2(Columns[i], SpawnY);
			bullet.Speed    = BulletSpeed;
			bullet.SetDirection(Vector2.Down);
			GetParent().AddChild(bullet);
		}
	}
}
