using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Base class for all rhythm arena obstacles (notes).
/// Travels horizontally (left → right) through the RhythmArena.
/// RhythmArena manages movement and hit detection — this class just
/// holds state and draws its visual.
/// </summary>
public abstract partial class ObstacleBase : Node2D
{
    [Export] public int   Damage      { get; set; } = 1;
    [Export] public int   Lane        { get; set; } = 0;
    [Export] public float TravelSpeed { get; set; } = 188f;

    /// <summary>True after Resolve() has been called (either hit or missed).</summary>
    public bool IsResolved { get; private set; }

    [Signal] public delegate void ResolvedEventHandler(int grade);

    /// <summary>
    /// Lane colour palette — shared by all obstacle subclasses.
    /// Index maps directly to Lane (0–3).
    /// </summary>
    public static readonly Color[] LaneColors =
    {
        new Color(0.97f, 0.44f, 0.44f),  // 0 — red
        new Color(0.44f, 0.85f, 0.97f),  // 1 — cyan
        new Color(0.44f, 0.97f, 0.55f),  // 2 — green
        new Color(0.97f, 0.88f, 0.44f),  // 3 — yellow
    };

    /// <summary>
    /// Resolve this obstacle with the given grade.
    /// Emits Resolved, marks as resolved, and queues free.
    /// </summary>
    public void Resolve(HitGrade grade)
    {
        if (IsResolved) return;
        IsResolved = true;
        EmitSignal(SignalName.Resolved, (int)grade);
        QueueFree();
    }
}
