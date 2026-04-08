using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Alchemist class Fight minigame — "Potion Brew".
/// A cursor bounces back and forth across a meter. The player presses interact/ui_accept
/// to stop it. Stopping inside the central sweet spot brews a random *good* effect
/// (heal/poison enemy/shield self). A narrow miss fizzles harmlessly. A wide miss backfires
/// and damages the brewer.
///
/// The sweet spot widens with the player's Luck stat — see <see cref="AlchemistBrewLogic.SweetHalfWidth"/>.
///
/// Emits <see cref="Confirmed"/> with (float accuracy) where 1.0 = dead-center, 0.0 = edge.
/// Caller pipes that into <see cref="AlchemistBrewLogic.Resolve"/> with the player's Luck.
/// </summary>
public partial class AlchemistBrewMinigame : Control
{
    [Signal] public delegate void ConfirmedEventHandler(float accuracy);

    [Export] public float CursorSpeed { get; set; } = 240f;

    private const float CursorWidth = 8f;

    private float _cursorX;
    private float _cursorDir = 1f;
    private bool  _active;

    public void Activate()
    {
        _active    = true;
        _cursorX   = 0f;
        _cursorDir = 1f;
        QueueRedraw();
        GD.Print("[AlchemistBrewMinigame] Activated.");
    }

    public override void _Process(double delta)
    {
        if (!_active) return;

        float barWidth = GetBarWidth();
        float maxX     = barWidth - CursorWidth;
        _cursorX += _cursorDir * CursorSpeed * (float)delta;

        if (_cursorX >= maxX) { _cursorX = maxX; _cursorDir = -1f; }
        if (_cursorX <= 0f)   { _cursorX = 0f;   _cursorDir =  1f; }

        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_active) return;
        if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
        {
            _active = false;
            float barWidth = GetBarWidth();
            float center   = (barWidth - CursorWidth) * 0.5f;
            float accuracy = 1f - Mathf.Abs(_cursorX - center) / center;
            accuracy = Mathf.Clamp(accuracy, 0f, 1f);
            GD.Print($"[AlchemistBrewMinigame] Confirmed. accuracy={accuracy:F2}");
            EmitSignal(SignalName.Confirmed, accuracy);
            GetViewport().SetInputAsHandled();
        }
    }

    private float GetBarWidth() => Mathf.Max(Size.X, 320f);

    public override void _Draw()
    {
        float w = GetBarWidth();
        float h = Mathf.Max(Size.Y, 32f);
        var fullRect = new Rect2(0, 0, w, h);

        // Dark background — alchemist purple
        DrawRect(fullRect, new Color(0.10f, 0.06f, 0.18f, 1f));

        // Sweet spot widens with player Luck
        int luck = GameManager.Instance?.EffectiveStats?.Luck ?? 0;
        float sweetHalf = AlchemistBrewLogic.SweetHalfWidth(luck);
        float sweetW    = w * (sweetHalf * 2f);
        float sweetX    = (w - sweetW) * 0.5f;
        DrawRect(new Rect2(sweetX, 2f, sweetW, h - 4f),
            new Color(0.55f, 0.85f, 0.45f, 0.55f));

        // Neutral band (between Backfire and Sweet) — subtle dim
        // Drawn behind the sweet spot for context. Width is implicit; we just draw a faint border.

        // Cursor
        DrawRect(new Rect2(_cursorX, 0f, CursorWidth, h), Colors.White);

        // Outer border
        DrawRect(fullRect, new Color(0.7f, 0.6f, 0.9f), filled: false, width: 1.5f);
    }
}
