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

	[Export] public float MoveTime { get; set; } = 0.18f;

	[Signal] public delegate void StepTakenEventHandler(Vector2I newTile);

	private AnimatedSprite2D? _sprite;
	private bool              _moving    = false;
	private Vector2I          _facingDir = Vector2I.Down;
	private WorldMap?         _worldMap;

	public Vector2I CurrentTile => WorldToTile(Position);

	public static Vector2I WorldToTile(Vector2 world)
		=> new((int)Mathf.Floor(world.X / TileSize), (int)Mathf.Floor(world.Y / TileSize));

	public static Vector2 TileToWorld(Vector2I tile)
		=> new(tile.X * TileSize + TileSize * 0.5f, tile.Y * TileSize + TileSize * 0.5f);

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;

		_sprite   = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		_worldMap = GetParent<WorldMap>();

		// Snap to nearest tile centre on load
		Position = TileToWorld(WorldToTile(Position));
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (Engine.IsEditorHint()) return;
		if (_moving) return;
		if (GameManager.Instance.CurrentState != GameState.Overworld) return;

		Vector2I dir = Vector2I.Zero;
		if      (e.IsActionPressed("move_up"))    { dir = Vector2I.Up;    _facingDir = Vector2I.Up; }
		else if (e.IsActionPressed("move_down"))  { dir = Vector2I.Down;  _facingDir = Vector2I.Down; }
		else if (e.IsActionPressed("move_left"))  { dir = Vector2I.Left;  _facingDir = Vector2I.Left; }
		else if (e.IsActionPressed("move_right")) { dir = Vector2I.Right; _facingDir = Vector2I.Right; }
		else return;

		UpdateFacing();
		Vector2I targetTile = CurrentTile + dir;

		if (_worldMap != null && !_worldMap.IsTilePassable(targetTile)) return;

		_ = DoMove(targetTile);
		GetViewport().SetInputAsHandled();
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
