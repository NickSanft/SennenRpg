using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// A bullet that travels in a caller-specified direction.
/// Call SetDirection() after Instantiate() but before AddChild().
/// Uses BulletBase._Process auto-free at 300 units from origin.
/// </summary>
public partial class DirectedBullet : BulletBase
{
	/// <summary>Set the travel direction (normalised automatically).</summary>
	public void SetDirection(Vector2 dir) => Direction = dir.Normalized();
}
