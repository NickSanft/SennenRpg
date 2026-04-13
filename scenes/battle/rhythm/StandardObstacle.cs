using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Standard single-press obstacle. Drawn as a rectangle in the lane colour
/// with trailing ghost copies for a motion-trail effect.
/// </summary>
public partial class StandardObstacle : ObstacleBase
{
    private const float Width  = 20f;
    private const float Height = 28f;

    public override void _Draw()
    {
        var col = Lane >= 0 && Lane < LaneColors.Length ? LaneColors[Lane] : Colors.White;

        // Ghost trail (3 copies behind, decreasing alpha)
        for (int i = 3; i >= 1; i--)
        {
            float alpha = 0.12f * (4 - i);
            var ghostCol = col with { A = alpha };
            float offsetX = -i * 12f;
            var ghostRect = new Rect2(-Width * 0.5f + offsetX, -Height * 0.5f, Width, Height);
            DrawRect(ghostRect, ghostCol);
        }

        // Main note
        var rect = new Rect2(-Width * 0.5f, -Height * 0.5f, Width, Height);
        DrawRect(rect, col);
        DrawRect(rect, Colors.White, filled: false, width: 1.5f);
    }
}
