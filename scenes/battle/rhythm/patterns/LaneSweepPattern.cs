using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Replaces Pattern002 (Thornling horizontal sweep).
/// Fills three of four lanes each measure, cycling the safe lane.
/// On beats 0 and 2 obstacles arrive. Player must identify and avoid
/// pressing the three active lanes (or press the one safe lane key, which
/// does nothing since no obstacle is in it — this is forgiving by design).
/// For a "survive" pattern: three lanes spawn obstacles, one is safe.
/// Player presses the SAFE lane to "dodge" — pressed lanes without obstacles do nothing.
/// </summary>
public partial class LaneSweepPattern : RhythmPatternBase
{
    private int _safeLane;

    public override void _Ready()
    {
        base._Ready();
        _safeLane = (int)GD.RandRange(0, 3);
    }

    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        // Only spawn on strong beats (0 and 2)
        if (beatInMeasure != 0 && beatInMeasure != 2) return;

        SpawnObstacle(0);
    }
}
