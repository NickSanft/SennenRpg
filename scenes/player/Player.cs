using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Extensions;
using SennenRpg.Core.Interfaces;
using SennenRpg.Scenes.Fx;

namespace SennenRpg.Scenes.Player;

public partial class Player : CharacterBody2D
{
	/// <summary>Emitted after each physics frame in which the player actually moved.</summary>
	[Signal] public delegate void MovedEventHandler(float distance);

	[Export] public float MoveSpeed      { get; set; } = 80f;
	[Export] public float RunSpeed       { get; set; } = 140f;
	[Export] public float InteractRadius { get; set; } = 32f;

	private AnimatedSprite2D _sprite = null!;
	private BeatSyncTrigger? _beatSync;
	private Area2D _interactRange = null!;
	private readonly HashSet<IInteractable> _candidates = new();
	private IInteractable? _nearbyInteractable;
	private Vector2 _lastPosition;
	private string _lastFacingDir = "down";

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

		var gm = GameManager.Instance;
		if (gm.PaletteSourceColors.Length > 0)
			PaletteSwapHelper.ApplyPalette(_sprite, gm.PaletteSourceColors, gm.PaletteTargetColors);

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

		_lastPosition = GlobalPosition;

		// Beat-sync the walk cycle to whatever BGM is playing. Scaled mode
		// (default) keeps frame transitions smooth — the running multiplier
		// is layered on top via SetUserMultiplier in _PhysicsProcess.
		_beatSync = new BeatSyncTrigger
		{
			Mode          = BeatSyncMode.Scaled,
			BaselineBpm   = 120f,
			FramesPerBeat = 0.5f, // 4-frame walk cycle = 1 frame per 2 beats at 120 BPM
		};
		_sprite.AddChild(_beatSync);

		// Teleport arrival: play dissolve-in (reform) animation
		if (gm.TeleportArriving)
		{
			gm.TeleportArriving = false;
			PlayTeleportReform();
		}
	}

	private void PlayTeleportReform()
	{
		const string shaderPath = "res://assets/shaders/dissolve_vertical.gdshader";
		if (!ResourceLoader.Exists(shaderPath)) return;

		var mat = new ShaderMaterial { Shader = GD.Load<Shader>(shaderPath) };
		mat.SetShaderParameter("progress", 1.0f);
		_sprite.Material = mat;

		var tween = CreateTween();
		tween.TweenMethod(
			Callable.From<float>(v => mat.SetShaderParameter("progress", v)),
			1.0f, 0.0f, 1.0f)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.TweenCallback(Callable.From(() => _sprite.Material = null));
	}

	public override void _PhysicsProcess(double delta)
	{
		// Block movement during dialog or non-overworld states
		if (GameManager.Instance.CurrentState is GameState.Dialog or GameState.Battle or GameState.Paused)
		{
			Velocity = Vector2.Zero;
			PlayIdleAnimation();
			return;
		}

		var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		bool running  = Input.IsActionPressed("run") && direction != Vector2.Zero;
		Velocity = direction * (running ? RunSpeed : MoveSpeed);

		if (direction != Vector2.Zero)
		{
			UpdateAnimation(direction);
			// Layer the running multiplier on top of the beat-derived scale.
			// The trigger combines this with the BPM/baseline ratio.
			_beatSync?.SetUserMultiplier(running ? 1.5f : 1.0f);
		}
		else
		{
			PlayIdleAnimation();
		}

		MoveAndSlide();

		float moved = GlobalPosition.DistanceTo(_lastPosition);
		if (moved > 0.5f)
		{
			EmitSignal(SignalName.Moved, moved);
			_lastPosition = GlobalPosition;

		}

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
			_lastFacingDir = "side";
			PlayIfExists("walk_side");
		}
		else if (direction.Y < 0)
		{
			_lastFacingDir = "up";
			PlayIfExists("walk_up");
		}
		else
		{
			_lastFacingDir = "down";
			PlayIfExists("walk_down");
		}
	}

	private void PlayIdleAnimation()
	{
		if (_sprite.SpriteFrames == null) return;

		// Use the walk animation at a slow speed for a visible idle sway effect,
		// since idle_* and walk_* share the same sprite frames.
		string target = $"walk_{_lastFacingDir}";
		if (_sprite.Animation != target)
			PlayIfExists(target);
		// Idle sway runs slower than walking. Layer the 0.6× user multiplier
		// on top of the beat-sync scale (so the sway still feels in time).
		_beatSync?.SetUserMultiplier(0.6f);
	}

	private void PlayIfExists(string animationName)
	{
		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation(animationName))
			_sprite.Play(animationName);
	}
}
