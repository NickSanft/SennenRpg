using Godot;

namespace SennenRpg.Scenes.Player;

/// <summary>
/// Builds an overworld <see cref="SpriteFrames"/> resource from a 32×16 strip
/// (two 16×16 frames horizontally) — the layout shared by Sen / Lily / Rain
/// and any future overworld follower.
///
/// Centralised so the leader (WorldMapPlayer / DungeonPlayer) and follower
/// (PartyFollower) all build identical animation sets, and a single fix
/// flows everywhere.
/// </summary>
public static class OverworldSpriteFactory
{
    /// <summary>
    /// Build the standard 6-state idle/walk SpriteFrames from a 32×16 sprite sheet.
    /// Returns a placeholder SpriteFrames containing only an empty <c>idle_down</c> if
    /// the texture is missing, so the caller's AnimatedSprite2D doesn't crash.
    /// </summary>
    public static SpriteFrames Build(string spriteSheetPath)
    {
        var frames = new SpriteFrames();
        if (frames.HasAnimation("default"))
            frames.RemoveAnimation("default");

        if (string.IsNullOrEmpty(spriteSheetPath) || !ResourceLoader.Exists(spriteSheetPath))
        {
            frames.AddAnimation("idle_down");
            return frames;
        }

        var texture = GD.Load<Texture2D>(spriteSheetPath);
        var frame0  = new AtlasTexture { Atlas = texture, Region = new Rect2(0,  0, 16, 16) };
        var frame1  = new AtlasTexture { Atlas = texture, Region = new Rect2(16, 0, 16, 16) };

        // Idle: two-frame slow loop on each direction. Matches Sen's WorldMapPlayer.tscn
        // idle pattern — the character gently bobs while standing still rather than
        // freezing on a single static frame.
        AddAnim(frames, "idle_down", 5f, frame0, frame1);
        AddAnim(frames, "idle_up",   5f, frame0, frame1);
        AddAnim(frames, "idle_side", 5f, frame0, frame1);

        // Walk: two-frame faster loop on each direction.
        AddAnim(frames, "walk_down", 8f, frame0, frame1);
        AddAnim(frames, "walk_up",   8f, frame0, frame1);
        AddAnim(frames, "walk_side", 8f, frame0, frame1);

        return frames;
    }

    private static void AddAnim(SpriteFrames frames, string name, float speed, params Texture2D[] textures)
    {
        frames.AddAnimation(name);
        frames.SetAnimationSpeed(name, speed);
        frames.SetAnimationLoop(name, true);
        foreach (var tex in textures)
            frames.AddFrame(name, tex);
    }
}
