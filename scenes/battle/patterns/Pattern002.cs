using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Thornling attack — horizontal thorn sweep.
/// Fires a row of 3 DirectedBullets from alternating sides every 0.7 s.
/// The row stagger means the player must dodge left or right as a wall sweeps across.
/// </summary>
public partial class Pattern002 : Node
{
	[Export] public PackedScene? BulletScene { get; set; }

	private Godot.Timer _timer = null!;
	private bool  _fromLeft = true;

	private const float BoxHalfX    = 72f;
	private const float BulletSpeed = 115f;
	// Three evenly-spaced row heights
	private static readonly float[] RowHeights = { -30f, 0f, 30f };

	public override void _Ready()
	{
		_timer = GetNode<Godot.Timer>("SpawnTimer");
		_timer.Timeout += OnSpawnTimer;
	}

	private void OnSpawnTimer()
	{
		if (BulletScene == null)
		{
			GD.PushWarning("[Pattern002] BulletScene not assigned.");
			return;
		}

		float startX = _fromLeft ? -(BoxHalfX + 12f) : BoxHalfX + 12f;
		Vector2 dir  = _fromLeft ? Vector2.Right : Vector2.Left;

		foreach (float y in RowHeights)
		{
			var bullet = BulletScene.Instantiate<DirectedBullet>();
			bullet.Position = new Vector2(startX, y);
			bullet.Speed    = BulletSpeed;
			bullet.SetDirection(dir);
			GetParent().AddChild(bullet);
		}

		_fromLeft = !_fromLeft;
	}
}
