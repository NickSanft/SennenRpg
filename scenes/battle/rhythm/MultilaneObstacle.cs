using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Danger obstacle that spans all 4 lanes. The player survives by NOT pressing
/// any key — pressing any lane while this passes deals damage. Used in AllLanesPattern.
/// </summary>
public partial class MultilaneObstacle : ObstacleBase
{
    private const float Width      = 20f;
    private const float TotalSpan  = 144f; // spans all 4 lanes

    public override void _Draw()
    {
        // Draw a tall barrier across all lanes in a warning red
        var col  = new Color(1f, 0.2f, 0.2f, 0.9f);
        var rect = new Rect2(-Width * 0.5f, -TotalSpan * 0.5f, Width, TotalSpan);
        DrawRect(rect, col);
        DrawRect(rect, Colors.White, filled: false, width: 2f);

        // Diagonal hatching
        for (int i = -3; i <= 3; i++)
            DrawLine(new Vector2(-Width * 0.5f, i * 24f - 12f),
                     new Vector2(Width * 0.5f,  i * 24f + 12f),
                     Colors.White with { A = 0.3f }, 1f);
    }
}
