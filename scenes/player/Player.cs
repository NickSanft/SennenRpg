using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Player;

public partial class Player : CharacterBody2D
{
	[Export] public float MoveSpeed      { get; set; } = 80f;
	[Export] public float RunSpeed       { get; set; } = 140f;
	[Export] public float InteractRadius { get; set; } = 32f;

	private AnimatedSprite2D _sprite = null!;
	private Area2D _interactRange = null!;
	private readonly HashSet<IInteractable> _candidates = new();
	private IInteractable? _nearbyInteractable;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		AddToGroup("player");

		_interactRange = GetNode<Area2D>("InteractRange");
		var collShape = _interactRange.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collShape?.Shape is CircleShape2D circle)
			circle.Radius = InteractRadius;

		_interactRange.BodyEntered += body => { if (body is IInteractable i) _candidates.Add(i); };
		_interactRange.BodyExited  += body => { if (body is IInteractable i) _candidates.Remove(i); };
		_interactRange.AreaEntered += area => { if (area is IInteractable i) _candidates.Add(i); };
		_interactRange.AreaExited  += area => { if (area is IInteractable i) _candidates.Remove(i); };

		// Placeholder visual — visible until real sprites are assigned
		if (_sprite.SpriteFrames == null)
		{
			var placeholder = new Polygon2D();
			placeholder.Polygon = [
				new Vector2(-6, -10), new Vector2(6, -10),
				new Vector2(6, 8),    new Vector2(-6, 8)
			];
			placeholder.Color = new Color(0.2f, 0.8f, 1f);
			AddChild(placeholder);
		}
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
		bool running  = Input.IsActionPressed("run") && direction != Vector2.Zero;
		Velocity = direction * (running ? RunSpeed : MoveSpeed);

		if (direction != Vector2.Zero)
		{
			UpdateAnimation(direction);
			if (_sprite.SpriteFrames != null)
				_sprite.SpeedScale = running ? 1.5f : 1.0f;
		}
		else
		{
			PlayIdleAnimation();
			if (_sprite.SpriteFrames != null)
				_sprite.SpeedScale = 1.0f;
		}

		MoveAndSlide();

		// Pick the closest candidate within the InteractRange Area2D
		IInteractable? previous = _nearbyInteractable;
		_nearbyInteractable = null;
		float closest = float.MaxValue;
		foreach (var candidate in _candidates)
		{
			if (candidate is Node2D n)
			{
				float dist = GlobalPosition.DistanceTo(n.GlobalPosition);
				if (dist < closest)
				{
					closest = dist;
					_nearbyInteractable = candidate;
				}
			}
		}

		if (_nearbyInteractable != previous)
		{
			previous?.HidePrompt();
			_nearbyInteractable?.ShowPrompt();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("interact"))
		{
			GD.Print($"[Player] Interact pressed. Nearby: {_nearbyInteractable?.GetType().Name ?? "none"}");
			_nearbyInteractable?.Interact(this);
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
}
