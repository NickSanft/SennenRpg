using Godot;
using System.Threading.Tasks;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Grid-locked overworld player. Moves exactly one 16×16 tile per input press,
/// tweening smoothly between tile centres. Emits StepTaken after each completed move.
/// </summary>
[Tool]
public partial class WorldMapPlayer : CharacterBody2D
{
	public const int TileSize = 16;

	[Export] public float MoveTime  { get; set; } = 0.18f;
	[Export] public float HoldDelay { get; set; } = 0.35f;

	[Signal] public delegate void StepTakenEventHandler(Vector2I newTile);

	private AnimatedSprite2D? _sprite;
	private bool              _moving    = false;
	private Vector2I          _facingDir = Vector2I.Down;
	private WorldMap?         _worldMap;
	private Vector2I          _heldDir   = Vector2I.Zero;
	private float             _holdTimer = 0f;

	public Vector2I CurrentTile => WorldToTile(Position);

	public static Vector2I WorldToTile(Vector2 world)
		=> new((int)Mathf.Floor(world.X / TileSize), (int)Mathf.Floor(world.Y / TileSize));

	public static Vector2 TileToWorld(Vector2I tile)
		=> new(tile.X * TileSize + TileSize * 0.5f, tile.Y * TileSize + TileSize * 0.5f);

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;

		_sprite   = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		var scheme = GameManager.Instance.PlayerColorScheme;
		if (scheme != null && _sprite != null)
			_sprite.Modulate = scheme.Tint;
		_worldMap = GetParent<WorldMap>();

		// Snap to nearest tile centre on load
		Position = TileToWorld(WorldToTile(Position));
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;
		if (GameManager.Instance.CurrentState != GameState.Overworld) return;

		Vector2I dir = ReadDirection();

		if (dir == Vector2I.Zero)
		{
			_heldDir   = Vector2I.Zero;
			_holdTimer = 0f;
			return;
		}

		if (dir != _heldDir)
		{
			// New direction pressed — move immediately
			_heldDir   = dir;
			_holdTimer = 0f;
			if (!_moving) TryMoveDir(dir);
			return;
		}

		// Same direction held — wait for HoldDelay then repeat each MoveTime
		if (_moving) return;

		_holdTimer += (float)delta;
		if (_holdTimer >= HoldDelay)
			TryMoveDir(dir);
	}

	private void TryMoveDir(Vector2I dir)
	{
		_facingDir = dir;
		UpdateFacing();
		Vector2I targetTile = CurrentTile + dir;
		if (_worldMap != null && !_worldMap.IsTilePassable(targetTile)) return;
		_ = DoMove(targetTile);
	}

	private static Vector2I ReadDirection()
	{
		if (Input.IsActionPressed("move_up"))    return Vector2I.Up;
		if (Input.IsActionPressed("move_down"))  return Vector2I.Down;
		if (Input.IsActionPressed("move_left"))  return Vector2I.Left;
		if (Input.IsActionPressed("move_right")) return Vector2I.Right;
		return Vector2I.Zero;
	}

	private async Task DoMove(Vector2I targetTile)
	{
		_moving = true;
		PlayAnim(WalkAnim());

		var tween = CreateTween()
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(this, "position", TileToWorld(targetTile), MoveTime);
		await ToSignal(tween, Tween.SignalName.Finished);

		PlayAnim(IdleAnim());
		_moving = false;
		EmitSignal(SignalName.StepTaken, targetTile);
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private void UpdateFacing()
	{
		if (_sprite != null)
			_sprite.FlipH = _facingDir == Vector2I.Left;
	}

	private void PlayAnim(string anim)
	{
		if (_sprite?.SpriteFrames?.HasAnimation(anim) == true)
			_sprite.Play(anim);
	}

	private string WalkAnim() => _facingDir switch
	{
		{ Y: < 0 } => "walk_up",
		{ Y: > 0 } => "walk_down",
		_           => "walk_side",
	};

	private string IdleAnim() => _facingDir switch
	{
		{ Y: < 0 } => "idle_up",
		{ Y: > 0 } => "idle_down",
		_           => "idle_side",
	};
}
