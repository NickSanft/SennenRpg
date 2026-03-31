using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Short-lived label spawned over the hit zone when a note is evaluated.
/// Scales up briefly then fades out over ~0.4 s.
/// Instantiate with <c>new HitFeedbackLabel()</c>, add as a child of the arena,
/// then call <see cref="Play"/> — the node frees itself when the animation ends.
/// </summary>
public partial class HitFeedbackLabel : Control
{
    /// <summary>
    /// Begin the pop-and-fade animation.
    /// </summary>
    /// <param name="text">Text to display ("PERFECT!", "GOOD", "MISS").</param>
    /// <param name="color">Label colour.</param>
    /// <param name="localPos">Position relative to the parent (arena) origin.</param>
    public void Play(string text, Color color, Vector2 localPos)
    {
        Position = localPos;

        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 13);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Position = new Vector2(-24f, 0f);
        AddChild(label);

        var tween = CreateTween().SetParallel(true);

        // Scale pop: 0.8 → 1.2 → 1.0 over first 0.15 s
        tween.TweenProperty(label, "scale", new Vector2(1.2f, 1.2f), 0.1f)
             .From(new Vector2(0.8f, 0.8f))
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        // Float up
        tween.TweenProperty(this, "position:y", localPos.Y - 18f, 0.4f)
             .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        // Fade out after a short hold
        tween.TweenProperty(this, "modulate:a", 0f, 0.25f).SetDelay(0.15f);

        // Self-free when done
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
