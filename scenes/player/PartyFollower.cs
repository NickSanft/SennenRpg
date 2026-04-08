using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Player;

/// <summary>
/// Overworld follower for a recruited party member. Reads its target position each frame
/// from a shared <see cref="FollowerTrail"/> populated by the leader (Player /
/// DungeonPlayer / WorldMapPlayer) and tweens toward it. Faces the direction of motion
/// and plays the same six-state walk/idle animations the leader uses.
///
/// <para>
/// Built code-only — no .tscn — so it can be instantiated by any overworld map without
/// having to author per-character scenes. The sprite sheet is loaded from
/// <see cref="PartyMember.OverworldSpritePath"/> and is expected to be a 32×16 strip
/// (two 16×16 frames horizontally), matching the existing Sen / Lily / Rain layout.
/// </para>
/// </summary>
public partial class PartyFollower : Node2D
{
    /// <summary>Steps behind the leader this follower stands. 1 = directly behind.</summary>
    public int StepsBack { get; set; } = 1;

    private FollowerTrail? _trail;
    private AnimatedSprite2D? _sprite;
    private Vector2 _facingDir = Vector2.Down;
    private const float MoveLerpSpeed = 12f;
    /// <summary>How many pixels we have to be from the target before we count as "moving".</summary>
    private const float MovingThreshold = 0.5f;

    /// <summary>
    /// Locally tracked current animation name. AnimatedSprite2D.Animation returns a
    /// <c>StringName</c> which doesn't always compare cleanly against a plain C# string —
    /// when the comparison silently treats them as different we end up calling Play()
    /// every frame and the walk animation never advances past frame 0. Tracking the
    /// name ourselves sidesteps that pitfall.
    /// </summary>
    private string _currentAnim = "";

    /// <summary>
    /// Spawn position to apply once the node enters the tree. Set in Configure().
    /// We can't apply it directly because the follower hasn't been added to a parent
    /// yet, so we'd be setting Position relative to nothing — and once added to YSort
    /// (which has its own transform), the local Position would put us at the wrong
    /// world location. We use _Ready() to assign GlobalPosition after the parent is in place.
    /// </summary>
    private Vector2 _pendingSpawnPosition;

    /// <summary>
    /// Configure the follower with its sprite sheet, trail reference, and chain index.
    /// Call this immediately after instantiation, before adding to the scene tree.
    /// </summary>
    public void Configure(string spriteSheetPath, FollowerTrail trail, int stepsBack, Vector2 spawnPosition)
    {
        _trail               = trail;
        StepsBack            = stepsBack > 0 ? stepsBack : 1;
        _pendingSpawnPosition = spawnPosition;

        _sprite = new AnimatedSprite2D
        {
            Name         = "Sprite",
            SpriteFrames = BuildFrames(spriteSheetPath),
            // Autoplay ensures the animation starts running as soon as the node enters
            // the tree, regardless of whether Play() was called pre-tree.
            Autoplay     = "idle_down",
        };
        AddChild(_sprite);
    }

    public override void _Ready()
    {
        base._Ready();
        // Group membership lets gameplay systems (e.g. the teleport dissolve spell)
        // find every follower currently on screen without walking the whole tree.
        AddToGroup("party_follower");
        // Apply the pending spawn position now that the parent transform is in place.
        // Setting GlobalPosition (rather than Position) means we land on the requested
        // world tile regardless of how the parent YSort / WorldMap is offset.
        GlobalPosition = _pendingSpawnPosition;

        // Belt-and-suspenders: by the time _Ready fires both this Node2D and the
        // AnimatedSprite2D child are inside the scene tree, so Play() will definitely
        // start the animation player. Set _currentAnim to match so PlayAnim's guard
        // doesn't immediately re-trigger.
        if (_sprite != null)
        {
            _sprite.Play("idle_down");
            _currentAnim = "idle_down";
        }
    }

    public override void _Process(double delta)
    {
        if (_trail == null) return;

        // The trail stores leader positions in WORLD space (the leader pushes its
        // GlobalPosition each step), so the follower must read & write in world space too.
        // Using local Position would land us in the wrong tile whenever the parent
        // (e.g. OverworldBase's YSort node) has any transform offset.
        Vector2 here     = GlobalPosition;
        Vector2 target   = _trail.GetTrailPosition(StepsBack, here);
        Vector2 toTarget = target - here;
        float   dist     = toTarget.Length();

        // Determine "moving" by distance-to-target rather than per-frame positional delta.
        // The lerp converges asymptotically, so a delta-based check would flip to idle
        // long before the follower visually finishes its glide — that left the walk anim
        // running for only a frame or two and never advancing past frame 0.
        bool isMoving = dist > MovingThreshold;

        if (isMoving)
        {
            UpdateFacing(toTarget);
            PlayAnim(WalkAnim());
        }
        else
        {
            PlayAnim(IdleAnim());
        }

        // Smooth lerp toward the target so the follower glides between tile centres.
        float t = 1f - Mathf.Exp(-MoveLerpSpeed * (float)delta);
        GlobalPosition = here.Lerp(target, t);
    }

    public void TeleportTo(Vector2 position)
    {
        GlobalPosition = position;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void UpdateFacing(Vector2 motion)
    {
        // Pick the dominant axis so we don't flicker between left/right and up/down
        // when the leader walks diagonally on the WorldMap.
        if (Mathf.Abs(motion.X) > Mathf.Abs(motion.Y))
        {
            _facingDir = motion.X < 0f ? Vector2.Left : Vector2.Right;
            if (_sprite != null) _sprite.FlipH = _facingDir == Vector2.Left;
        }
        else if (Mathf.Abs(motion.Y) > 0f)
        {
            _facingDir = motion.Y < 0f ? Vector2.Up : Vector2.Down;
        }
    }

    private void PlayAnim(string anim)
    {
        // Compare against our locally-tracked name (a plain C# string) instead of
        // _sprite.Animation, which is a Godot StringName whose equality with a string
        // can silently fail. Without this fix Play() was being invoked every frame and
        // the walk animation reset to frame 0 each call, never reaching frame 1.
        if (_currentAnim == anim) return;
        if (_sprite?.SpriteFrames?.HasAnimation(anim) != true) return;
        _sprite.Play(anim);
        _currentAnim = anim;
    }

    private string WalkAnim() => _facingDir.Y switch
    {
        < 0 => "walk_up",
        > 0 => "walk_down",
        _   => "walk_side",
    };

    private string IdleAnim() => _facingDir.Y switch
    {
        < 0 => "idle_up",
        > 0 => "idle_down",
        _   => "idle_side",
    };

    private static SpriteFrames BuildFrames(string spriteSheetPath)
        => OverworldSpriteFactory.Build(spriteSheetPath);
}
