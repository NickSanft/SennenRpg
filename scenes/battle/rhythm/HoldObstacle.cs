using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Hold-note obstacle. The player must press and hold a lane key for HoldBeats beats
/// before releasing. Used for the Bardic Inspiration skill minigame and some
/// advanced enemy patterns.
/// </summary>
public partial class HoldObstacle : ObstacleBase
{
    [Export] public int HoldBeats { get; set; } = 2;

    private const float HeadWidth  = 20f;
    private const float TailHeight = 14f;

    /// <summary>Length of the hold tail in pixels, computed from HoldBeats and TravelSpeed.</summary>
    public float TailLength => HoldBeats * RhythmConstants.BeatInterval(RhythmClock.Instance.Bpm) * TravelSpeed;

    public override void _Draw()
    {
        var col  = Lane >= 0 && Lane < LaneColors.Length ? LaneColors[Lane] : Colors.White;
        float tl = TailLength;

        // Tail (stretches behind the note head)
        DrawRect(new Rect2(-tl, -TailHeight * 0.5f, tl, TailHeight), col with { A = 0.6f });

        // Head
        DrawRect(new Rect2(-HeadWidth * 0.5f, -HeadWidth * 0.5f, HeadWidth, HeadWidth), col);
        DrawRect(new Rect2(-HeadWidth * 0.5f, -HeadWidth * 0.5f, HeadWidth, HeadWidth),
                 Colors.White, filled: false, width: 1.5f);
    }
}
