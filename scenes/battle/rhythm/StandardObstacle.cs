using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Standard single-press obstacle. Drawn as a rounded rectangle in the lane colour.
/// </summary>
public partial class StandardObstacle : ObstacleBase
{
    private const float Width  = 20f;
    private const float Height = 28f;

    public override void _Draw()
    {
        var col   = Lane >= 0 && Lane < LaneColors.Length ? LaneColors[Lane] : Colors.White;
        var rect  = new Rect2(-Width * 0.5f, -Height * 0.5f, Width, Height);
        DrawRect(rect, col);
        DrawRect(rect, Colors.White, filled: false, width: 1.5f);
    }
}
