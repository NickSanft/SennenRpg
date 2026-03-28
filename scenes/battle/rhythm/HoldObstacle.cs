using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Hold-note obstacle. The player must press and hold a lane key for HoldBeats beats.
/// RhythmArena tracks hold elapsed time and calls StartHold()/Resolve() as appropriate.
/// While held, a white fill bar overlays the tail to show progress.
/// </summary>
public partial class HoldObstacle : ObstacleBase
{
    [Export] public int HoldBeats { get; set; } = 2;

    private const float HeadWidth  = 20f;
    private const float TailHeight = 14f;

    /// <summary>True while the player is actively holding this lane.</summary>
    public bool IsBeingHeld { get; private set; }

    /// <summary>0–1 fill progress, set each frame by RhythmArena while held.</summary>
    public float HoldProgress { get; set; }

    /// <summary>Total seconds the player must hold for a full-hold result.</summary>
    public float FullHoldTime => HoldBeats * RhythmConstants.BeatInterval(RhythmClock.Instance.Bpm);

    /// <summary>Visual tail length in pixels.</summary>
    public float TailLength => FullHoldTime * TravelSpeed;

    /// <summary>Called by RhythmArena when the player presses the lane key.</summary>
    public void StartHold() => IsBeingHeld = true;

    public override void _Draw()
    {
        var   col = Lane >= 0 && Lane < LaneColors.Length ? LaneColors[Lane] : Colors.White;
        float tl  = TailLength;

        // Tail (stretches behind the note head)
        DrawRect(new Rect2(-tl, -TailHeight * 0.5f, tl, TailHeight), col with { A = 0.6f });

        // Progress fill while held
        if (IsBeingHeld && HoldProgress > 0f)
        {
            float fillW = tl * HoldProgress;
            DrawRect(new Rect2(-tl, -TailHeight * 0.5f, fillW, TailHeight),
                     Colors.White with { A = 0.5f });
        }

        // Head
        DrawRect(new Rect2(-HeadWidth * 0.5f, -HeadWidth * 0.5f, HeadWidth, HeadWidth), col);
        DrawRect(new Rect2(-HeadWidth * 0.5f, -HeadWidth * 0.5f, HeadWidth, HeadWidth),
                 Colors.White, filled: false, width: 1.5f);
    }
}
