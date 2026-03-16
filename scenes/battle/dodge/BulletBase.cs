using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Base class for all enemy attack bullets. Each bullet is an Area2D.
/// Subclasses set Direction and Speed in their _Ready().
/// Auto-frees when it travels more than 300 units from its spawn position
/// (well outside the DodgeBox bounds).
/// Collision: Layer 3, Mask 2 (monitors the soul on layer 2).
/// </summary>
public abstract partial class BulletBase : Area2D
{
	[Export] public int   Damage { get; set; } = 1;
	[Export] public float Speed  { get; set; } = 100f;

	protected Vector2 Direction = Vector2.Down;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		Position += Direction * Speed * (float)delta;

		// Destroy once it has travelled well outside the DodgeBox
		if (Position.Length() > 300f)
			QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Soul soul)
		{
			soul.TakeDamage(Damage);
			QueueFree();
		}
	}
}
