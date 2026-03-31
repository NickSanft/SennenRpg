using Godot;
using System.Collections.Generic;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Mage class Fight minigame — directional rune sequence QTE.
/// Three runes are revealed one at a time; player presses arrow keys / WASD to trace each rune.
/// Each rune maps to one cardinal direction.
///
/// Emits <see cref="Completed"/> with (int correctCount) — 0, 1, 2, or 3.
/// Caller maps correctCount to a damage/status grade.
/// </summary>
public partial class MageRuneInput : Control
{
    [Signal] public delegate void CompletedEventHandler(int correctCount);

    private static readonly string[] RuneActions =
        { "ui_up", "ui_down", "ui_left", "ui_right" };

    private static readonly string[] RuneGlyphs =
        { "↑", "↓", "←", "→" };

    private static readonly Color RuneColor      = new(0.6f, 0.4f, 1f);
    private static readonly Color RuneActiveColor = Colors.White;
    private static readonly Color RuneCorrect    = Colors.Cyan;
    private static readonly Color RuneWrong      = Colors.Red;

    private int[]           _sequence       = [];
    private int             _step           = 0;
    private int             _correctCount   = 0;
    private bool            _active         = false;
    private readonly List<bool?> _results   = new();  // null=pending, true=correct, false=wrong

    private Label? _promptLabel;

    public override void _Ready()
    {
        _promptLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        _promptLabel.AddThemeFontSizeOverride("font_size", 24);
        AddChild(_promptLabel);
    }

    public void Activate()
    {
        // Generate a random 3-rune sequence
        _sequence     = new int[3];
        _results.Clear();
        for (int i = 0; i < 3; i++)
        {
            _sequence[i] = (int)GD.RandRange(0, RuneActions.Length - 1);
            _results.Add(null);
        }
        _step         = 0;
        _correctCount = 0;
        _active       = true;
        UpdatePrompt();
        QueueRedraw();
        GD.Print($"[MageRuneInput] Activated. Sequence: {string.Join(",", _sequence)}");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_active || @event is not InputEventKey key || !key.IsPressed()) return;

        for (int i = 0; i < RuneActions.Length; i++)
        {
            if (@event.IsActionPressed(RuneActions[i]))
            {
                bool correct = i == _sequence[_step];
                _results[_step] = correct;
                if (correct) _correctCount++;

                GetViewport().SetInputAsHandled();
                QueueRedraw();
                _step++;

                if (_step >= _sequence.Length)
                {
                    _active = false;
                    GD.Print($"[MageRuneInput] Done. Correct: {_correctCount}/3");
                    EmitSignal(SignalName.Completed, _correctCount);
                }
                else
                {
                    UpdatePrompt();
                }
                return;
            }
        }
    }

    private void UpdatePrompt()
    {
        if (_promptLabel != null)
            _promptLabel.Text = _step < _sequence.Length
                ? $"Press  {RuneGlyphs[_sequence[_step]]}"
                : "";
    }

    public override void _Draw()
    {
        float w = Mathf.Max(Size.X, 240f);
        float h = Mathf.Max(Size.Y, 80f);

        // Background
        DrawRect(new Rect2(0, 0, w, h), new Color(0.06f, 0.04f, 0.12f, 1f));

        // Rune slots
        float slotW  = w / 3f;
        float slotH  = h * 0.55f;
        float slotY  = (h - slotH) * 0.5f;

        for (int i = 0; i < _sequence.Length; i++)
        {
            float slotX   = i * slotW;
            bool? result  = _results[i];
            Color bgColor = i == _step && _active
                ? new Color(0.25f, 0.15f, 0.45f)
                : new Color(0.12f, 0.08f, 0.22f);
            DrawRect(new Rect2(slotX + 4f, slotY, slotW - 8f, slotH), bgColor);

            // Glyph
            string glyph;
            Color  glyphColor;
            if (result == null)
            {
                glyph      = i == _step && _active ? RuneGlyphs[_sequence[i]] : "?";
                glyphColor = i == _step && _active ? RuneActiveColor : RuneColor;
            }
            else
            {
                glyph      = RuneGlyphs[_sequence[i]];
                glyphColor = result.Value ? RuneCorrect : RuneWrong;
            }

            // Draw glyph as text via DrawString
            var font = ThemeDB.FallbackFont;
            DrawString(font, new Vector2(slotX + slotW * 0.5f - 8f, slotY + slotH * 0.6f),
                glyph, HorizontalAlignment.Center, -1, 22, glyphColor);

            // Slot border
            Color borderColor = result switch
            {
                true  => RuneCorrect,
                false => RuneWrong,
                _     => i == _step && _active ? Colors.White : new Color(0.4f, 0.3f, 0.6f),
            };
            DrawRect(new Rect2(slotX + 4f, slotY, slotW - 8f, slotH),
                borderColor, filled: false, width: 1.5f);
        }

        // Border
        DrawRect(new Rect2(0, 0, w, h), Colors.White, filled: false, width: 1.5f);
    }
}
