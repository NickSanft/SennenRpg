using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Replaces Pattern003 (Gloomfish radial burst).
/// Spawns obstacles in all four lanes simultaneously on the downbeat (beat 0).
/// This is the "danger" pattern: the player should NOT press any lane key
/// when these arrive — pressing a lane with an obstacle still deals damage.
/// A MultilaneObstacle variant used here to give a visual warning.
///
/// Mechanical note: since pressing a lane with an obstacle resolves it as a hit,
/// the actual "all lanes blocked" effect requires the player to identify the pattern
/// visually and hold off pressing. The visual cue (all lanes fill red) signals this.
/// </summary>
public partial class AllLanesPattern : RhythmPatternBase
{
    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        // Burst on beat 0 of every measure
        if (beatInMeasure != 0) return;

        for (int lane = 0; lane < 4; lane++)
            SpawnObstacle(lane, damage: 2); // higher damage on a missed burst
    }
}
