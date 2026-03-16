using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// The red heart the player controls during the dodge phase.
/// Reads move_left/right/up/down input (same actions as the overworld player).
/// DodgeBox clamps the position to the arena bounds after physics.
/// </summary>
public partial class Soul : CharacterBody2D
{
	[Signal] public delegate void DiedEventHandler();

	[Export] public float MoveSpeed { get; set; } = 120f;

	private bool  _invincible = false;
	private float _invincibilityTimer = 0f;
	private Node2D _sprite = null!;

	public override void _Ready()
	{
		_sprite = GetNode<Node2D>("Visual");
		AddToGroup("soul");
	}

	public override void _PhysicsProcess(double delta)
	{
		var dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = dir * MoveSpeed;
		MoveAndSlide();

		// Tick invincibility — flash sprite while active
		if (_invincible)
		{
			_invincibilityTimer -= (float)delta;
			_sprite.Visible = (int)(_invincibilityTimer * 12) % 2 == 0;
			if (_invincibilityTimer <= 0f)
			{
				_invincible = false;
				_sprite.Visible = true;
			}
		}
	}

	public void TakeDamage(int amount)
	{
		if (_invincible) return;

		GameManager.Instance.HurtPlayer(amount);
		GD.Print($"[Soul] Hit for {amount}. HP now {GameManager.Instance.PlayerStats.CurrentHp}");

		_invincible = true;
		_invincibilityTimer = GameManager.Instance.PlayerStats.InvincibilityDuration;

		if (GameManager.Instance.PlayerStats.CurrentHp <= 0)
		{
			GD.Print("[Soul] HP = 0 — emitting Died.");
			EmitSignal(SignalName.Died);
		}
	}
}
