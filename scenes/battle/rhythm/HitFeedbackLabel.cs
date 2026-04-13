using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Short-lived label spawned over the hit zone when a note is evaluated.
/// Plays a grade-specific animation then frees itself.
/// Instantiate with <c>new HitFeedbackLabel()</c>, add as a child of the arena,
/// then call <see cref="Play"/> — the node frees itself when the animation ends.
/// </summary>
public partial class HitFeedbackLabel : Control
{
    /// <summary>
    /// Begin the pop-and-fade animation with grade-specific flair.
    /// </summary>
    /// <param name="text">Text to display ("PERFECT!", "GOOD", "MISS", etc.).</param>
    /// <param name="color">Label colour.</param>
    /// <param name="localPos">Position relative to the parent (arena) origin.</param>
    public void Play(string text, Color color, Vector2 localPos)
    {
        Position = localPos;

        bool isPerfect = text.Contains("PERFECT") || text.Contains("FLAWLESS");
        bool isMiss    = text.Contains("MISS");
        int  fontSize  = isPerfect ? 14 : 13;

        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Position = new Vector2(-24f, 0f);
        AddChild(label);

        if (isPerfect)
            PlayPerfect(label, localPos);
        else if (isMiss)
            PlayMiss(label);
        else
            PlayGood(label, localPos);
    }

    private void PlayPerfect(Label label, Vector2 localPos)
    {
        var tween = CreateTween().SetParallel(true);

        // Scale pop from 1.5x → 1.0x with Back easing
        tween.TweenProperty(label, "scale", Vector2.One, 0.15f)
             .From(new Vector2(1.5f, 1.5f))
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        // Slight random shake (+-2px X offset)
        float shakeDir = GD.Randf() > 0.5f ? 2f : -2f;
        tween.TweenProperty(this, "position:x", localPos.X + shakeDir, 0.04f);
        tween.TweenProperty(this, "position:x", localPos.X - shakeDir, 0.04f).SetDelay(0.04f);
        tween.TweenProperty(this, "position:x", localPos.X, 0.04f).SetDelay(0.08f);

        // Rise 24px upward
        tween.TweenProperty(this, "position:y", localPos.Y - 24f, 0.40f)
             .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        // Hold 0.15s then fade over 0.25s
        tween.TweenProperty(this, "modulate:a", 0f, 0.25f).SetDelay(0.15f);

        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    private void PlayGood(Label label, Vector2 localPos)
    {
        var tween = CreateTween().SetParallel(true);

        // Scale from 1.3x → 1.0x
        tween.TweenProperty(label, "scale", Vector2.One, 0.12f)
             .From(new Vector2(1.3f, 1.3f))
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        // Rise 18px
        tween.TweenProperty(this, "position:y", localPos.Y - 18f, 0.35f)
             .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        // Fade over 0.3s
        tween.TweenProperty(this, "modulate:a", 0f, 0.3f).SetDelay(0.05f);

        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    private void PlayMiss(Label label)
    {
        // No scale or upward drift — horizontal shake then fade
        var tween = CreateTween();

        // Oscillate +-4px X, 3 times (6 tweens), 0.08s total
        const float shakePx = 4f;
        const float shakeStep = 0.08f / 6f;
        float baseX = Position.X;
        tween.TweenProperty(this, "position:x", baseX + shakePx, shakeStep);
        tween.TweenProperty(this, "position:x", baseX - shakePx, shakeStep);
        tween.TweenProperty(this, "position:x", baseX + shakePx, shakeStep);
        tween.TweenProperty(this, "position:x", baseX - shakePx, shakeStep);
        tween.TweenProperty(this, "position:x", baseX + shakePx, shakeStep);
        tween.TweenProperty(this, "position:x", baseX,           shakeStep);

        // Fade over 0.2s
        tween.TweenProperty(this, "modulate:a", 0f, 0.2f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
