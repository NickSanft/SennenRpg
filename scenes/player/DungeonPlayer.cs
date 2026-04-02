using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Extensions;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Player;

/// <summary>
/// Grid-locked dungeon player. Moves exactly one 16 px tile per input press,
/// tweening smoothly between centres. Collision is tested via TestMove() so it
/// works with any TileMapLayer that has a physics layer configured.
///
/// Uses the 16×16 Sen_Overworld sprite. Includes the full interaction system
/// (InteractRange + nearest-candidate selection) so NPC/sign/chest prompts
/// work exactly as in the regular overworld.
///
/// Spawned by OverworldBase when UseSmallPlayer = true.
/// </summary>
public partial class DungeonPlayer : CharacterBody2D
{
    /// <summary>Emitted after each successful grid step.</summary>
    [Signal] public delegate void MovedEventHandler(float distance);

    public const int TileSize = 16;

    [Export] public float MoveTime     { get; set; } = 0.15f;
    [Export] public float HoldDelay    { get; set; } = 0.25f;
    [Export] public float InteractRadius { get; set; } = 20f;

    private AnimatedSprite2D?             _sprite;
    private Area2D                         _interactRange = null!;
    private readonly HashSet<IInteractable> _candidates    = new();
    private IInteractable?                 _nearbyInteractable;

    private bool     _moving       = false;
    private bool     _startSnapped = false; // deferred first-frame snap after OverworldBase sets position
    private Vector2I _facingDir = Vector2I.Down;
    private Vector2I _heldDir   = Vector2I.Zero;
    private float    _holdTimer = 0f;

    public override void _Ready()
    {
        AddToGroup("player");
        _sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");

        var gm = GameManager.Instance;
        if (_sprite != null && gm.PaletteSourceColors.Length > 0)
            PaletteSwapHelper.ApplyPalette(_sprite, gm.PaletteSourceColors, gm.PaletteTargetColors);

        _interactRange = GetNode<Area2D>("InteractRange");
        var collShape = _interactRange.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collShape?.Shape is CircleShape2D circle)
            circle.Radius = InteractRadius;

        _interactRange.BodyEntered += body => { if (body is IInteractable i) _candidates.Add(i); };
        _interactRange.BodyExited  += body => { if (body is IInteractable i) _candidates.Remove(i); };
        _interactRange.AreaEntered += area => { if (area is IInteractable i) _candidates.Add(i); };
        _interactRange.AreaExited  += area => { if (area is IInteractable i) _candidates.Remove(i); };

        // Snap is deferred to the first _Process frame so it runs after
        // OverworldBase._Ready() has finished setting GlobalPosition from the SpawnPoint.
    }

    public override void _Process(double delta)
    {
        if (!_startSnapped)
        {
            Position = SnapToGrid(Position);
            _startSnapped = true;
        }

        if (GameManager.Instance.CurrentState is GameState.Dialog or GameState.Battle or GameState.Paused)
            return;

        UpdateNearbyInteractable();

        Vector2I dir = ReadDirection();

        if (dir == Vector2I.Zero)
        {
            _heldDir   = Vector2I.Zero;
            _holdTimer = 0f;
            return;
        }

        if (dir != _heldDir)
        {
            // New direction — move immediately and start the hold timer.
            _heldDir   = dir;
            _holdTimer = 0f;
            if (!_moving) TryMove(dir);
            return;
        }

        // Same direction held — repeat after HoldDelay once each move completes.
        if (_moving) return;
        _holdTimer += (float)delta;
        if (_holdTimer >= HoldDelay)
            TryMove(dir);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact"))
            _nearbyInteractable?.Interact(this);
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void TryMove(Vector2I dir)
    {
        _facingDir = dir;
        UpdateFacing();

        var motion = new Vector2(dir.X * TileSize, dir.Y * TileSize);

        // TestMove checks physics collision — blocked by any StaticBody2D or
        // TileMapLayer tile that has a physics shape assigned.
        if (TestMove(GlobalTransform, motion))
            return;

        _ = DoMove(GlobalPosition + motion);
    }

    private async Task DoMove(Vector2 targetPos)
    {
        _moving = true;
        PlayAnim(WalkAnim());

        var tween = CreateTween()
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(this, "position", targetPos, MoveTime);
        await ToSignal(tween, Tween.SignalName.Finished);

        PlayAnim(IdleAnim());
        _moving = false;
        EmitSignal(SignalName.Moved, (float)TileSize);
    }

    // ── Interaction ───────────────────────────────────────────────────────────

    private void UpdateNearbyInteractable()
    {
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Vector2 SnapToGrid(Vector2 pos)
        => new(Mathf.Floor(pos.X / TileSize) * TileSize + TileSize * 0.5f,
               Mathf.Floor(pos.Y / TileSize) * TileSize + TileSize * 0.5f);

    private static Vector2I ReadDirection()
    {
        if (Input.IsActionPressed("move_up"))    return Vector2I.Up;
        if (Input.IsActionPressed("move_down"))  return Vector2I.Down;
        if (Input.IsActionPressed("move_left"))  return Vector2I.Left;
        if (Input.IsActionPressed("move_right")) return Vector2I.Right;
        return Vector2I.Zero;
    }

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
