using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Rogue class Fight minigame — "Pickpocket Combo".
/// Three rapid timing windows in succession; the player presses interact/ui_accept on each
/// to land a hit. Hits inside the central sweet spot count as Perfects. Three perfects =
/// guaranteed crit + steal an item from the enemy's loot table.
///
/// Each stage gives the player a fixed window of time before timing out as a miss.
///
/// Emits <see cref="Confirmed"/> with (int perfectCount, int hitCount) when all 3 stages resolve.
/// Caller maps that to a <see cref="SennenRpg.Core.Data.RogueStrikeOutcome"/> via RogueStealLogic.
/// </summary>
public partial class RogueStrikeMinigame : Control
{
    [Signal] public delegate void ConfirmedEventHandler(int perfectCount, int hitCount);

    private const int   StageCount    = 3;
    private const float CursorSpeed   = 360f;   // pixels/sec — faster than FightBar to demand precision
    private const float CursorWidth   = 8f;
    private const float SweetSpotPct  = 0.20f;  // central 20% of bar = perfect
    private const float HitFloor      = 0.30f;  // accuracy below this = miss
    private const float StageTimeout  = 1.4f;   // seconds before a stage auto-misses
    private const float StageGap      = 0.15f;  // pause between stages

    private int   _stage;
    private int   _perfectCount;
    private int   _hitCount;
    private bool  _active;
    private bool  _stageWaiting;   // true during the inter-stage pause
    private float _stageElapsed;
    private float _gapRemaining;
    private float _cursorX;
    private float _cursorDir;

    public void Activate()
    {
        _stage         = 0;
        _perfectCount  = 0;
        _hitCount      = 0;
        _active        = true;
        _stageWaiting  = false;
        _gapRemaining  = 0f;
        BeginStage();
        QueueRedraw();
        GD.Print("[RogueStrikeMinigame] Activated.");
    }

    private void BeginStage()
    {
        _cursorX      = 0f;
        _cursorDir    = 1f;
        _stageElapsed = 0f;
        _stageWaiting = false;
    }

    public override void _Process(double delta)
    {
        if (!_active) return;

        float dt = (float)delta;

        if (_stageWaiting)
        {
            _gapRemaining -= dt;
            if (_gapRemaining <= 0f)
            {
                _stage++;
                if (_stage >= StageCount) { Finish(); return; }
                BeginStage();
                QueueRedraw();
            }
            return;
        }

        // Bounce the cursor across the active bar.
        float barWidth = GetBarWidth();
        float maxX     = barWidth - CursorWidth;
        _cursorX += _cursorDir * CursorSpeed * dt;
        if (_cursorX >= maxX) { _cursorX = maxX; _cursorDir = -1f; }
        if (_cursorX <= 0f)   { _cursorX = 0f;   _cursorDir =  1f; }

        _stageElapsed += dt;
        if (_stageElapsed >= StageTimeout)
        {
            // Stage timeout — counted as a miss.
            ResolveStage(landed: false, accuracy: 0f);
        }

        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_active || _stageWaiting) return;
        if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
        {
            float barWidth = GetBarWidth();
            float center   = (barWidth - CursorWidth) * 0.5f;
            float accuracy = 1f - Mathf.Abs(_cursorX - center) / center;
            accuracy = Mathf.Clamp(accuracy, 0f, 1f);
            ResolveStage(landed: true, accuracy: accuracy);
            GetViewport().SetInputAsHandled();
        }
    }

    private void ResolveStage(bool landed, float accuracy)
    {
        if (landed && accuracy >= HitFloor)
        {
            _hitCount++;
            // Sweet spot occupies the central SweetSpotPct of the bar.
            // accuracy >= 1 - SweetSpotPct means the cursor was inside the sweet zone.
            if (accuracy >= 1f - SweetSpotPct)
                _perfectCount++;
        }
        _stageWaiting = true;
        _gapRemaining = StageGap;
        QueueRedraw();
    }

    private void Finish()
    {
        _active = false;
        GD.Print($"[RogueStrikeMinigame] Done. Perfects: {_perfectCount}/3, Hits: {_hitCount}/3");
        EmitSignal(SignalName.Confirmed, _perfectCount, _hitCount);
    }

    private float GetBarWidth() => Mathf.Max(Size.X, 320f);

    public override void _Draw()
    {
        float w = Mathf.Max(Size.X, 320f);
        float h = Mathf.Max(Size.Y, 96f);

        const float barH    = 16f;
        const float barGap  = 6f;
        float startY = (h - (StageCount * barH + (StageCount - 1) * barGap)) * 0.5f;

        for (int i = 0; i < StageCount; i++)
        {
            float y = startY + i * (barH + barGap);
            var barRect = new Rect2(0, y, w, barH);

            bool isResolvedStage = i < _stage || (i == _stage && _stageWaiting);
            bool isActiveStage   = i == _stage && !_stageWaiting && _active;

            // Background
            DrawRect(barRect, isActiveStage
                ? new Color(0.18f, 0.10f, 0.08f, 1f)
                : new Color(0.10f, 0.06f, 0.06f, 1f));

            // Sweet spot
            float sweetW     = w * SweetSpotPct;
            float sweetStart = (w - sweetW) * 0.5f;
            DrawRect(new Rect2(sweetStart, y + 2f, sweetW, barH - 4f),
                new Color(0.95f, 0.45f, 0.20f, 0.55f));

            // Cursor (only on the active stage)
            if (isActiveStage)
            {
                DrawRect(new Rect2(_cursorX, y, CursorWidth, barH), Colors.White);
            }

            // Border
            DrawRect(barRect, isActiveStage ? Colors.White : new Color(0.5f, 0.3f, 0.3f),
                filled: false, width: 1.5f);

            // Resolved indicator
            if (isResolvedStage)
            {
                // Use a simple coloured dot to the left of the bar
                Color dot;
                if (i + 1 <= _perfectCount && _perfectCount > 0)
                    dot = Colors.Yellow;
                else if (i + 1 <= _hitCount && _hitCount > 0)
                    dot = Colors.LimeGreen;
                else
                    dot = Colors.DarkGray;
                DrawCircle(new Vector2(-10f, y + barH * 0.5f), 4f, dot);
            }
        }

        // Border around whole control
        DrawRect(new Rect2(0, 0, w, h), new Color(0.7f, 0.4f, 0.3f), filled: false, width: 1.5f);
    }
}
