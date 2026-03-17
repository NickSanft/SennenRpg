using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// A bullet that bounces off the DodgeBox walls until its lifetime expires.
/// Call SetDirection() after Instantiate() but before AddChild().
/// Overrides BulletBase._Process — does NOT auto-free at 300 units.
/// </summary>
public partial class BouncingBullet : BulletBase
{
	/// <summary>Half-extents of the bounce area. Should match DodgeBox.BoxSize / 2 minus a small margin.</summary>
	[Export] public Vector2 BoxHalfSize { get; set; } = new Vector2(72f, 52f);

	private float _lifetime;
	private const float MaxLifetime = 12f;

	public void SetDirection(Vector2 dir) => Direction = dir.Normalized();

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		Position += Direction * Speed * dt;

		// Bounce off left/right walls
		if (Position.X <= -BoxHalfSize.X || Position.X >= BoxHalfSize.X)
		{
			Direction = new Vector2(-Direction.X, Direction.Y);
			Position = new Vector2(
				Mathf.Clamp(Position.X, -BoxHalfSize.X, BoxHalfSize.X),
				Position.Y);
		}

		// Bounce off top/bottom walls
		if (Position.Y <= -BoxHalfSize.Y || Position.Y >= BoxHalfSize.Y)
		{
			Direction = new Vector2(Direction.X, -Direction.Y);
			Position = new Vector2(
				Position.X,
				Mathf.Clamp(Position.Y, -BoxHalfSize.Y, BoxHalfSize.Y));
		}

		_lifetime += dt;
		if (_lifetime >= MaxLifetime)
			QueueFree();
	}
}
