using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// A bullet that weaves side-to-side as it travels in its main direction.
/// Position is computed analytically (main travel + perpendicular sine wave)
/// so the oscillation stays smooth regardless of frame rate.
/// Auto-frees after MaxLifetime seconds.
/// </summary>
public partial class ZigZagBullet : BulletBase
{
	/// <summary>Peak side-to-side displacement in pixels.</summary>
	[Export] public float OscAmplitude { get; set; } = 20f;
	/// <summary>Oscillation cycles per second (radians/s = this × 2π).</summary>
	[Export] public float OscFrequency { get; set; } = 4f;
	[Export] public float MaxLifetime   { get; set; } = 5f;

	private Vector2 _origin;
	private float   _time;

	public void SetDirection(Vector2 dir) => Direction = dir.Normalized();

	public override void _Ready()
	{
		base._Ready();
		_origin = Position;
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;

		// Main travel along direction + sine-wave perpendicular oscillation
		var perp = new Vector2(-Direction.Y, Direction.X);
		Position = _origin
			+ Direction * Speed * _time
			+ perp * Mathf.Sin(_time * OscFrequency) * OscAmplitude;

		if (_time >= MaxLifetime)
			QueueFree();
	}
}
