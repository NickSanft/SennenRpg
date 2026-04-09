using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Lily's "Wither and Bloom" skill minigame.
/// Hold <c>interact</c> / <c>ui_accept</c> to fill the bloom meter; release (or
/// time out at <see cref="MaxHoldDuration"/>) to confirm. Emits
/// <see cref="Confirmed"/> with the final fill ratio (0..1).
///
/// The bloom is drawn as a flower whose petals expand with the fill ratio.
/// Released past <c>1.0</c> over-blooms (capped at 1.0).
/// </summary>
public partial class LilyWitherAndBloomMinigame : Control
{
	[Signal] public delegate void ConfirmedEventHandler(float fillRatio);

	private const float MaxHoldDuration = 1.5f;

	private bool  _active;
	private bool  _holding;
	private float _hold;

	public void Activate()
	{
		_active  = true;
		_holding = false;
		_hold    = 0f;
		Visible  = true;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (!_active) return;

		if (_holding)
		{
			_hold += (float)delta;
			if (_hold >= MaxHoldDuration)
			{
				Confirm();
				return;
			}
			QueueRedraw();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_active) return;

		if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
		{
			_holding = true;
		}
		else if (@event.IsActionReleased("interact") || @event.IsActionReleased("ui_accept"))
		{
			if (_holding) Confirm();
		}
	}

	private void Confirm()
	{
		_active  = false;
		_holding = false;
		float ratio = Mathf.Clamp(_hold / MaxHoldDuration, 0f, 1f);
		Visible  = false;
		EmitSignal(SignalName.Confirmed, ratio);
	}

	public override void _Draw()
	{
		float w  = Mathf.Max(Size.X, 200f);
		float h  = Mathf.Max(Size.Y, 120f);
		float cx = w * 0.5f;
		float cy = h * 0.5f;

		float ratio = Mathf.Clamp(_hold / MaxHoldDuration, 0f, 1f);

		// Stem
		DrawLine(new Vector2(cx, cy + 30f), new Vector2(cx, cy + 60f), new Color(0.3f, 0.7f, 0.3f), 3f);

		// Petals — expand with ratio
		float petalRadius = Mathf.Lerp(4f, 18f, ratio);
		var petalColor    = new Color(0.9f, 0.5f + 0.4f * ratio, 0.7f);
		for (int i = 0; i < 6; i++)
		{
			float angle = i * Mathf.Tau / 6f;
			var pos = new Vector2(
				cx + Mathf.Cos(angle) * petalRadius,
				cy + Mathf.Sin(angle) * petalRadius);
			DrawCircle(pos, petalRadius * 0.7f, petalColor);
		}

		// Center
		DrawCircle(new Vector2(cx, cy), 4f, new Color(1f, 0.85f, 0.2f));

		// Fill bar at the bottom
		float barW = w * 0.6f;
		float barX = (w - barW) * 0.5f;
		float barY = h - 12f;
		DrawRect(new Rect2(barX, barY, barW, 6f),       new Color(0.2f, 0.2f, 0.2f), filled: true);
		DrawRect(new Rect2(barX, barY, barW * ratio, 6f), new Color(0.4f, 0.9f, 0.4f), filled: true);
		DrawRect(new Rect2(barX, barY, barW, 6f),       new Color(1f, 1f, 1f),       filled: false);
	}
}
