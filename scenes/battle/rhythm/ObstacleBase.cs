using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Base class for all rhythm arena obstacles (notes).
/// Travels horizontally (left → right) through the RhythmArena.
/// RhythmArena manages movement and hit detection — this class holds state
/// and draws its visual, plus plays a scale-pop animation on resolve.
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
    /// Plays a scale-pop tween on hit, fades out on miss, then queues free.
    /// </summary>
    public void Resolve(HitGrade grade)
    {
        if (IsResolved) return;
        IsResolved = true;
        EmitSignal(SignalName.Resolved, (int)grade);

        if (grade == HitGrade.Miss)
        {
            // Quick red fade-out
            Modulate = Colors.Red;
            var fadeTween = CreateTween();
            fadeTween.TweenProperty(this, "modulate:a", 0f, 0.12f);
            fadeTween.TweenCallback(Callable.From(QueueFree));
            return;
        }

        // Scale-pop: flash gold (Perfect) or white (Good), then shrink away
        Modulate = grade == HitGrade.Perfect ? new Color(1f, 0.85f, 0.1f) : Colors.White;
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(1.4f, 1.4f), 0.06f);
        tween.TweenProperty(this, "scale", Vector2.Zero,            0.10f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
