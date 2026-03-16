using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Player;

public partial class Player : CharacterBody2D
{
	[Export] public float MoveSpeed { get; set; } = 80f;

	private AnimatedSprite2D _sprite = null!;
	private Area2D _interactRange = null!;
	private IInteractable? _nearbyInteractable;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_interactRange = GetNode<Area2D>("InteractRange");

		_interactRange.BodyEntered += OnInteractRangeBodyEntered;
		_interactRange.BodyExited += OnInteractRangeBodyExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Block movement during dialog or non-overworld states
		if (GameManager.Instance.CurrentState is GameState.Dialog or GameState.Battle or GameState.Paused)
		{
			Velocity = Vector2.Zero;
			return;
		}

		var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = direction * MoveSpeed;

		if (direction != Vector2.Zero)
		{
			UpdateAnimation(direction);
		}
		else
		{
			PlayIdleAnimation();
		}

		MoveAndSlide();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("interact") && _nearbyInteractable != null)
		{
			_nearbyInteractable.Interact(this);
		}
	}

	private void UpdateAnimation(Vector2 direction)
	{
		if (_sprite.SpriteFrames == null) return;

		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			_sprite.FlipH = direction.X < 0;
			PlayIfExists("walk_side");
		}
		else if (direction.Y < 0)
		{
			PlayIfExists("walk_up");
		}
		else
		{
			PlayIfExists("walk_down");
		}
	}

	private void PlayIdleAnimation()
	{
		if (_sprite.SpriteFrames == null) return;

		string current = _sprite.Animation;
		if (current.StartsWith("walk_"))
			PlayIfExists(current.Replace("walk_", "idle_"));
	}

	private void PlayIfExists(string animationName)
	{
		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation(animationName))
			_sprite.Play(animationName);
	}

	private void OnInteractRangeBodyEntered(Node2D body)
	{
		if (body is IInteractable interactable)
			_nearbyInteractable = interactable;
	}

	private void OnInteractRangeBodyExited(Node2D body)
	{
		if (body is IInteractable && _nearbyInteractable == body)
			_nearbyInteractable = null;
	}
}
