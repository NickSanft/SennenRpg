using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Base class for all rhythm arena obstacles (notes).
/// Travels horizontally (left -> right) through the RhythmArena.
/// RhythmArena manages movement and hit detection -- this class holds state
/// and draws its visual, plus plays a grade-specific resolve animation.
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
    /// Lane colour palette -- shared by all obstacle subclasses.
    /// Index maps directly to Lane (0-3).
    /// </summary>
    public static readonly Color[] LaneColors =
    {
        new Color(0.97f, 0.44f, 0.44f),  // 0 -- red
        new Color(0.44f, 0.85f, 0.97f),  // 1 -- cyan
        new Color(0.44f, 0.97f, 0.55f),  // 2 -- green
        new Color(0.97f, 0.88f, 0.44f),  // 3 -- yellow
    };

    /// <summary>
    /// Resolve this obstacle with the given grade.
    /// Plays a grade-specific animation then queues free.
    /// </summary>
    public void Resolve(HitGrade grade)
    {
        if (IsResolved) return;
        IsResolved = true;
        EmitSignal(SignalName.Resolved, (int)grade);

        if (grade == HitGrade.Perfect)
        {
            // Flash gold, scale pop 1.0x -> 1.6x -> 0x with brief rotation. Total 0.18s.
            Modulate = new Color(1f, 0.85f, 0.1f);
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(this, "scale", new Vector2(1.6f, 1.6f), 0.08f)
                 .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(this, "rotation_degrees", GD.Randf() > 0.5f ? 15f : -15f, 0.08f);
            tween.Chain().TweenProperty(this, "scale", Vector2.Zero, 0.10f);
            tween.Chain().TweenCallback(Callable.From(QueueFree));
            return;
        }

        if (grade == HitGrade.Good)
        {
            // Flash white, scale 1.0x -> 1.3x -> 0x. Total 0.14s.
            Modulate = Colors.White;
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", new Vector2(1.3f, 1.3f), 0.06f);
            tween.TweenProperty(this, "scale", Vector2.Zero, 0.08f);
            tween.TweenCallback(Callable.From(QueueFree));
            return;
        }

        // Miss: flash red, scale up 1.2x then shrink to 0.5x with fade. Total 0.15s.
        Modulate = Colors.Red;
        var missTween = CreateTween().SetParallel(true);
        missTween.TweenProperty(this, "scale", new Vector2(1.2f, 1.2f), 0.05f);
        missTween.Chain().TweenProperty(this, "scale", new Vector2(0.5f, 0.5f), 0.10f);
        missTween.TweenProperty(this, "modulate:a", 0f, 0.15f);
        missTween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
