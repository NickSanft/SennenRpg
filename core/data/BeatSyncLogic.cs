namespace SennenRpg.Core.Data;

/// <summary>
/// Sync mode for a sprite registered with the BeatSync system.
///
/// <list type="bullet">
/// <item><term>None</term> — Sprite is unmanaged. The registry leaves its native
/// SpeedScale alone.</item>
/// <item><term>Snap</term> — Frame transitions land exactly on beats. Strong
/// "choreographed" effect; only suitable for cycles of ≤4 frames.</item>
/// <item><term>Scaled</term> — SpeedScale is multiplied so the average frame rate
/// equals <c>currentBpm / baselineBpm</c>. Smooth at any tempo. This is the default
/// for walking sprites and battle enemies.</item>
/// </list>
/// </summary>
public enum BeatSyncMode
{
    None,
    Snap,
    Scaled,
}

/// <summary>
/// Pure logic for converting BPM / beat-index → frame index or speed scale.
/// No Godot deps so it compiles into the test runner. The runtime registry
/// (BeatSyncRegistry autoload) wraps these helpers and applies them to live
/// AnimatedSprite2D nodes.
/// </summary>
public static class BeatSyncLogic
{
    /// <summary>
    /// Compute the visible frame for Snap mode given the current beat index.
    ///
    /// <c>framesPerBeat = 1.0</c> means one frame per beat (default).
    /// <c>framesPerBeat = 0.5</c> means a frame change every 2 beats (slower).
    /// <c>framesPerBeat = 2.0</c> means two frames per beat (faster — strobes
    /// at high BPM).
    ///
    /// Returns 0 if <paramref name="totalFrames"/> is non-positive.
    /// </summary>
    public static int SnappedFrame(int beatIndex, float framesPerBeat, int totalFrames)
    {
        if (totalFrames <= 0) return 0;
        if (framesPerBeat <= 0f) return 0;

        // beatIndex × framesPerBeat → which "logical frame step" we're on.
        // Floor + modulo to get a 0..totalFrames-1 frame index that loops.
        int step = (int)System.Math.Floor(beatIndex * framesPerBeat);
        int wrapped = step % totalFrames;
        if (wrapped < 0) wrapped += totalFrames; // negative beat indices safety
        return wrapped;
    }

    /// <summary>
    /// Compute the SpeedScale multiplier for Scaled mode.
    ///
    /// At <c>currentBpm == baselineBpm</c> the result is 1.0 (sprite plays at
    /// its native rate). At double tempo the result is 2.0; at half it's 0.5.
    ///
    /// <paramref name="framesPerBeat"/> shifts the baseline — a sprite tagged
    /// "1 frame per 2 beats" (framesPerBeat = 0.5) plays half as fast for the
    /// same BPM, so its scale is halved.
    ///
    /// Returns 1.0 (the safe pass-through default) for any invalid input.
    /// </summary>
    public static float ScaleFactor(float currentBpm, float baselineBpm, float framesPerBeat)
    {
        if (currentBpm  <= 0f) return 1f;
        if (baselineBpm <= 0f) return 1f;
        if (framesPerBeat <= 0f) return 1f;

        return (currentBpm / baselineBpm) * framesPerBeat;
    }

    /// <summary>
    /// Combine the registry's beat-derived scale with a user-supplied multiplier
    /// (e.g., the player's "running" speed multiplier). Both are clamped at 0
    /// before multiplication so a negative or NaN never propagates.
    /// </summary>
    public static float CombineScales(float beatScale, float userMultiplier)
    {
        if (beatScale       < 0f || float.IsNaN(beatScale))       beatScale       = 1f;
        if (userMultiplier  < 0f || float.IsNaN(userMultiplier))  userMultiplier  = 1f;
        return beatScale * userMultiplier;
    }
}
