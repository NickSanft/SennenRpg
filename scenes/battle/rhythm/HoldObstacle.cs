using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Hold-note obstacle. The player must press and hold a lane key for HoldBeats beats.
/// RhythmArena tracks hold elapsed time and calls StartHold()/Resolve() as appropriate.
/// Enhanced visuals: pulsing fill, lane-to-gold colour shift, spark at fill edge.
/// </summary>
public partial class HoldObstacle : ObstacleBase
{
    [Export] public int HoldBeats { get; set; } = 2;

    private const float HeadWidth  = 20f;
    private const float TailHeight = 14f;

    /// <summary>True while the player is actively holding this lane.</summary>
    public bool IsBeingHeld { get; private set; }

    /// <summary>0-1 fill progress, set each frame by RhythmArena while held.</summary>
    public float HoldProgress { get; set; }

    /// <summary>Total seconds the player must hold for a full-hold result.</summary>
    public float FullHoldTime => HoldBeats * RhythmConstants.BeatInterval(RhythmClock.Instance.Bpm);

    /// <summary>Visual tail length in pixels.</summary>
    public float TailLength => FullHoldTime * TravelSpeed;

    /// <summary>Called by RhythmArena when the player presses the lane key.</summary>
    public void StartHold() => IsBeingHeld = true;

    public override void _Process(double delta)
    {
        base._Process(delta);
        // Redraw each frame while held to animate the fill
        if (IsBeingHeld)
            QueueRedraw();
    }

    public override void _Draw()
    {
        var   col = Lane >= 0 && Lane < LaneColors.Length ? LaneColors[Lane] : Colors.White;
        float tl  = TailLength;

        // Tail background
        DrawRect(new Rect2(-tl, -TailHeight * 0.5f, tl, TailHeight), col with { A = 0.35f });

        // Progress fill while held -- colour shifts from lane colour to gold
        if (IsBeingHeld && HoldProgress > 0f)
        {
            var fillColor = col.Lerp(new Color(1f, 0.85f, 0.1f), HoldProgress);
            float fillW = tl * HoldProgress;

            // Pulsing alpha on beat
            float beatPhase = RhythmClock.Instance?.BeatPhase ?? 0f;
            float pulseAlpha = 0.5f + 0.2f * Mathf.Max(0f, 1f - beatPhase * 3f);

            DrawRect(new Rect2(-tl, -TailHeight * 0.5f, fillW, TailHeight),
                     fillColor with { A = pulseAlpha });

            // Spark at fill edge
            float sparkX = -tl + fillW;
            float sparkSize = 4f + 2f * Mathf.Sin((float)Time.GetTicksMsec() / 80f);
            DrawCircle(new Vector2(sparkX, 0f), sparkSize, Colors.White with { A = 0.8f });
        }

        // Head with subtle glow when being held
        if (IsBeingHeld)
            DrawCircle(Vector2.Zero, HeadWidth * 0.7f, col with { A = 0.3f });

        DrawRect(new Rect2(-HeadWidth * 0.5f, -HeadWidth * 0.5f, HeadWidth, HeadWidth), col);
        DrawRect(new Rect2(-HeadWidth * 0.5f, -HeadWidth * 0.5f, HeadWidth, HeadWidth),
                 Colors.White, filled: false, width: 1.5f);
    }
}
