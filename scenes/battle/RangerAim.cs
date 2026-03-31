using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Ranger class Fight minigame.
/// A reticle drifts in a Lissajous-like pattern across the enemy silhouette for up to 2 seconds.
/// Press <c>interact</c> / <c>ui_accept</c> to fire; accuracy (0–1) drives the damage multiplier.
/// Accuracy ≥ 0.85 → critical hit (ignores Defence).
///
/// Emits <see cref="Confirmed"/> with (float accuracy, bool isCrit).
/// </summary>
public partial class RangerAim : Control
{
    [Signal] public delegate void ConfirmedEventHandler(float accuracy, bool isCrit);

    private const float Duration    = 2f;
    private const float CritRadius  = 0.15f; // fraction of half-size that counts as bull's-eye
    private const float SpeedX      = 0.7f;  // Lissajous frequency
    private const float SpeedY      = 1.3f;

    private float _elapsed  = 0f;
    private bool  _active   = false;
    private Vector2 _reticle;

    public void Activate()
    {
        _elapsed = 0f;
        _active  = true;
        QueueRedraw();
        GD.Print("[RangerAim] Activated.");
    }

    public override void _Process(double delta)
    {
        if (!_active) return;

        _elapsed += (float)delta;

        float w = Mathf.Max(Size.X, 200f);
        float h = Mathf.Max(Size.Y, 120f);
        float hw = w * 0.5f;
        float hh = h * 0.5f;

        // Lissajous drift
        _reticle = new Vector2(
            hw + hw * 0.8f * Mathf.Sin(SpeedX * _elapsed * Mathf.Tau),
            hh + hh * 0.8f * Mathf.Sin(SpeedY * _elapsed * Mathf.Tau)
        );
        QueueRedraw();

        if (_elapsed >= Duration)
            Confirm(); // time's up → fire at current position
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_active) return;
        if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
        {
            GetViewport().SetInputAsHandled();
            Confirm();
        }
    }

    private void Confirm()
    {
        if (!_active) return;
        _active = false;

        float w  = Mathf.Max(Size.X, 200f);
        float h  = Mathf.Max(Size.Y, 120f);
        float hw = w * 0.5f;
        float hh = h * 0.5f;

        // Distance from center, normalised to 0-1
        float dist = new Vector2(_reticle.X - hw, _reticle.Y - hh).Length()
                     / new Vector2(hw, hh).Length();
        float accuracy = Mathf.Clamp(1f - dist, 0f, 1f);
        bool  isCrit   = dist <= CritRadius;

        GD.Print($"[RangerAim] Confirmed. dist={dist:F2}, accuracy={accuracy:F2}, crit={isCrit}");
        EmitSignal(SignalName.Confirmed, accuracy, isCrit);
    }

    public override void _Draw()
    {
        float w  = Mathf.Max(Size.X, 200f);
        float h  = Mathf.Max(Size.Y, 120f);
        float hw = w * 0.5f;
        float hh = h * 0.5f;

        // Background
        DrawRect(new Rect2(0, 0, w, h), new Color(0.08f, 0.08f, 0.12f, 1f));

        // Enemy silhouette (simple oval)
        DrawArc(new Vector2(hw, hh), Mathf.Min(hw, hh) * 0.6f, 0f, Mathf.Tau, 32, new Color(0.3f, 0.3f, 0.4f), 1.5f);

        // Bull's-eye ring
        float critPx = Mathf.Min(hw, hh) * CritRadius * 2f;
        DrawArc(new Vector2(hw, hh), critPx, 0f, Mathf.Tau, 24, Colors.Yellow, 1f);

        // Reticle
        DrawArc(_reticle, 8f, 0f, Mathf.Tau, 16, Colors.Red, 2f);
        DrawLine(_reticle + new Vector2(-10, 0), _reticle + new Vector2(10, 0), Colors.Red, 1.5f);
        DrawLine(_reticle + new Vector2(0, -10), _reticle + new Vector2(0, 10), Colors.Red, 1.5f);

        // Timer bar at bottom
        if (_elapsed < Duration)
        {
            float pct = 1f - _elapsed / Duration;
            DrawRect(new Rect2(0, h - 4f, w * pct, 4f), Colors.Cyan);
        }

        // Border
        DrawRect(new Rect2(0, 0, w, h), Colors.White, filled: false, width: 1.5f);
    }
}
