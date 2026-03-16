using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// A bullet that falls straight down at constant speed.
/// </summary>
public partial class StraightBullet : BulletBase
{
	public override void _Ready()
	{
		base._Ready();
		Direction = Vector2.Down;
	}
}
