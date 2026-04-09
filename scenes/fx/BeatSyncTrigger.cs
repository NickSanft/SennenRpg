using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Fx;

/// <summary>
/// Per-sprite beat-sync component. Add this Node as a child of any
/// <see cref="AnimatedSprite2D"/> to make its frames track the live BGM tempo
/// via <see cref="RhythmClock"/>. The actual work is centralised in
/// <see cref="BeatSyncRegistry"/> — this Node just registers itself.
///
/// Two operating modes:
/// <list type="bullet">
/// <item><b>Snap</b> — frame transitions land exactly on beats. Strong
/// "choreographed" effect; only suitable for short cycles (≤4 frames).</item>
/// <item><b>Scaled</b> — SpeedScale is multiplied so the average frame rate
/// matches the BGM tempo. Smooth at any tempo. The default for walks.</item>
/// </list>
///
/// For sprites that aren't <see cref="AnimatedSprite2D"/> (e.g. the custom
/// <c>AnimatedPortrait</c> Control), use the static
/// <see cref="Attach(Node, BeatSyncMode, float, float, System.Action{int}, System.Action{float})"/>
/// helper, which lets you supply a custom frame setter and/or scale setter
/// instead of poking an AnimatedSprite2D.
/// </summary>
public partial class BeatSyncTrigger : Node
{
    [Export] public BeatSyncMode Mode          { get; set; } = BeatSyncMode.Scaled;
    [Export] public float        FramesPerBeat { get; set; } = 1.0f;
    [Export] public float        BaselineBpm   { get; set; } = 120f;

    /// <summary>
    /// When non-empty, the trigger only applies sync while the parent
    /// <see cref="AnimatedSprite2D"/>'s current Animation matches this name.
    /// Lets walk-cycles sync while idle frames stay still.
    /// </summary>
    [Export] public string Animation { get; set; } = "";

    private AnimatedSprite2D? _sprite;
    private float             _nativeSpeedScale = 1f;
    private float             _userMultiplier   = 1f;

    /// <summary>Optional custom frame setter (Snap mode). Set by Attach() for non-AnimatedSprite2D widgets.</summary>
    private System.Action<int>? _customFrameSetter;
    /// <summary>Optional custom scale setter (Scaled mode). Set by Attach() for non-AnimatedSprite2D widgets.</summary>
    private System.Action<float>? _customScaleSetter;
    /// <summary>Total frame count for the synced cycle. Defaults from sprite frames; set explicitly via Attach().</summary>
    private int _totalFrames = 1;

    public override void _Ready()
    {
        // Auto-discover an AnimatedSprite2D parent if no custom callbacks were set.
        if (_customFrameSetter == null && _customScaleSetter == null)
        {
            _sprite = GetParentOrNull<AnimatedSprite2D>();
            if (_sprite != null)
            {
                _nativeSpeedScale = _sprite.SpeedScale;
                _totalFrames      = ResolveFrameCount(_sprite);
            }
        }

        BeatSyncRegistry.Instance?.Register(this);
    }

    public override void _ExitTree()
    {
        BeatSyncRegistry.Instance?.Unregister(this);
    }

    /// <summary>
    /// Add a multiplier on top of the registry's beat-derived scale (Scaled mode only).
    /// Used by <c>Player</c> to layer the running multiplier on top of the BPM scale.
    /// </summary>
    public void SetUserMultiplier(float multiplier)
    {
        _userMultiplier = multiplier < 0f ? 1f : multiplier;
        // Force the registry to re-apply the scale so the new multiplier takes effect immediately.
        BeatSyncRegistry.Instance?.RefreshTrigger(this);
    }

    // ── Called by the registry ────────────────────────────────────────────────

    /// <summary>Called by BeatSyncRegistry on every Beat signal (Snap mode only).</summary>
    internal void HandleBeat(int beatIndex)
    {
        if (Mode != BeatSyncMode.Snap) return;
        if (!IsAnimationActive()) return;

        int frame = BeatSyncLogic.SnappedFrame(beatIndex, FramesPerBeat, _totalFrames);
        if (_customFrameSetter != null)
        {
            _customFrameSetter(frame);
        }
        else if (_sprite != null)
        {
            // Pause Godot's internal clock so it doesn't fight us.
            if (_sprite.IsPlaying()) _sprite.Pause();
            _sprite.Frame = frame;
        }
    }

    /// <summary>Called by BeatSyncRegistry whenever the live BPM changes (Scaled mode).</summary>
    internal void ApplyScale(float currentBpm)
    {
        if (Mode != BeatSyncMode.Scaled) return;

        float beatScale = BeatSyncLogic.ScaleFactor(currentBpm, BaselineBpm, FramesPerBeat);
        float combined  = BeatSyncLogic.CombineScales(beatScale, _userMultiplier);

        if (_customScaleSetter != null)
        {
            _customScaleSetter(combined);
        }
        else if (_sprite != null)
        {
            _sprite.SpeedScale = _nativeSpeedScale * combined;
        }
    }

    /// <summary>
    /// Called by the registry when BGM is unknown / free-running (bpm == 0).
    /// Restores the sprite to its native behaviour: native SpeedScale, resume playing.
    /// </summary>
    internal void RestoreNative()
    {
        if (_customScaleSetter != null)
        {
            _customScaleSetter(1f);
            return;
        }
        if (_sprite == null) return;
        _sprite.SpeedScale = _nativeSpeedScale;
        if (Mode == BeatSyncMode.Snap && !_sprite.IsPlaying())
            _sprite.Play();
    }

    private bool IsAnimationActive()
    {
        if (string.IsNullOrEmpty(Animation)) return true;
        return _sprite != null && _sprite.Animation == Animation;
    }

    private static int ResolveFrameCount(AnimatedSprite2D sprite)
    {
        var frames = sprite.SpriteFrames;
        if (frames == null) return 1;
        string animName = sprite.Animation.IsEmpty ? "default" : sprite.Animation.ToString();
        return frames.HasAnimation(animName) ? frames.GetFrameCount(animName) : 1;
    }

    // ── Static helper for code-built sprites ─────────────────────────────────

    /// <summary>
    /// Create a BeatSyncTrigger and attach it to <paramref name="parent"/> in code.
    /// Use this for sprites that aren't AnimatedSprite2D (e.g. AnimatedPortrait,
    /// the BestiaryMenu enemy preview).
    ///
    /// <paramref name="customFrameSetter"/> is invoked with the next frame index
    /// (Snap mode). <paramref name="customScaleSetter"/> is invoked with the new
    /// scale factor (Scaled mode). Either may be null if that mode isn't used.
    /// </summary>
    public static BeatSyncTrigger Attach(
        Node parent,
        BeatSyncMode mode,
        int totalFrames,
        float framesPerBeat = 1.0f,
        float baselineBpm   = 120f,
        System.Action<int>? customFrameSetter   = null,
        System.Action<float>? customScaleSetter = null)
    {
        var trigger = new BeatSyncTrigger
        {
            Mode               = mode,
            FramesPerBeat      = framesPerBeat,
            BaselineBpm        = baselineBpm,
            _totalFrames       = totalFrames,
            _customFrameSetter = customFrameSetter,
            _customScaleSetter = customScaleSetter,
        };
        parent.AddChild(trigger);
        return trigger;
    }
}
