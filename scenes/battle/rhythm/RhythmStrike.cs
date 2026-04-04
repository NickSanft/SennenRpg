using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Single-lane note highway for the Fight/Strike action.
/// A note travels from left to right; the player presses Z (interact/ui_accept)
/// when the note reaches the hit-zone line on the right side.
/// Graded by pixel distance from the hit zone, consistent with RhythmArena.
/// </summary>
public partial class RhythmStrike : Control
{
	[Signal] public delegate void StrikeResolvedEventHandler(int grade);

	// ── Layout ────────────────────────────────────────────────────────
	private const float LaneW      = 240f;
	private const float LaneH      = 36f;
	private const float HitZoneX   = 200f;   // x relative to lane origin
	private const float SpawnX     = 0f;
	private const float NoteRadius = 10f;
	private const float GoodPx     = 22f;    // matches RhythmArena window

	// Speed: note covers (HitZoneX - SpawnX) in exactly ActivationBeats beats
	private const int ActivationBeats = 3;

	// ── State ─────────────────────────────────────────────────────────
	private bool  _active;
	private bool  _hasResult;
	private float _noteX;
	private float _speed;

	public override void _Ready()
	{
		Visible = false;
		RhythmClock.Instance.Beat += OnBeat;
	}

	public override void _ExitTree()
	{
		RhythmClock.Instance.Beat -= OnBeat;
	}

	public void Activate()
	{
		float beatInterval = RhythmClock.Instance.BeatInterval;
		_speed      = (HitZoneX - SpawnX) / (ActivationBeats * beatInterval);
		_noteX      = SpawnX;
		_active     = true;
		_hasResult  = false;
		Visible     = true;
		QueueRedraw();
		GD.Print("[RhythmStrike] Activated.");
	}

	private void OnBeat(int _beatIndex)
	{
		if (!_active) return;

		// If the note has already passed the lane, auto-miss
		if (_noteX > LaneW && !_hasResult)
			Resolve(HitGrade.Miss);
	}

	public override void _Process(double delta)
	{
		if (!_active) return;
		_noteX += _speed * (float)delta;

		// Auto-miss once the note is well past the hit zone
		if (_noteX > HitZoneX + GoodPx + 10f && !_hasResult)
			Resolve(HitGrade.Miss);

		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (!_active || _hasResult) return;
		if (!e.IsActionPressed("interact") && !e.IsActionPressed("ui_accept")) return;

		float dist = Mathf.Abs(_noteX - HitZoneX);
		float deviationSec = dist / _speed;
		Resolve(RhythmConstants.GradeDeviation(deviationSec));
		GetViewport().SetInputAsHandled();
	}

	private void Resolve(HitGrade grade)
	{
		_hasResult = true;
		_active    = false;
		Visible    = false;
		GD.Print($"[RhythmStrike] Resolved. noteX={_noteX:F1}, grade={grade}");
		EmitSignal(SignalName.StrikeResolved, (int)grade);
	}

	// ── Drawing ───────────────────────────────────────────────────────

	public override void _Draw()
	{
		if (!_active) return;

		var vp     = GetViewportRect().Size;
		var origin = new Vector2((vp.X - LaneW) * 0.5f, vp.Y * 0.5f - LaneH * 0.5f);

		// Lane background
		DrawRect(new Rect2(origin, new Vector2(LaneW, LaneH)),
				 new Color(0.06f, 0.06f, 0.10f, 1f));
		DrawRect(new Rect2(origin, new Vector2(LaneW, LaneH)),
				 Colors.White, filled: false, width: 2f);

		// Hit-zone line
		float hitScreenX = origin.X + HitZoneX;
		DrawLine(new Vector2(hitScreenX, origin.Y),
				 new Vector2(hitScreenX, origin.Y + LaneH),
				 Colors.White with { A = 0.8f }, 2f);

		// Beat pulse glow on the hit zone
		float pulse = Mathf.Max(0f, 1f - RhythmClock.Instance.BeatPhase * 4f);
		if (pulse > 0f)
			DrawLine(new Vector2(hitScreenX, origin.Y),
					 new Vector2(hitScreenX, origin.Y + LaneH),
					 Colors.White with { A = pulse * 0.6f }, 6f);

		// Travelling note
		float noteScreenX = origin.X + _noteX;
		float noteCentreY = origin.Y + LaneH * 0.5f;
		DrawCircle(new Vector2(noteScreenX, noteCentreY), NoteRadius, Colors.Yellow);
		DrawArc(new Vector2(noteScreenX, noteCentreY), NoteRadius, 0, Mathf.Tau, 24,
				Colors.White, 2f);

		// Prompt
		string strikeHint = $"Press {Core.Extensions.InputMapExtensions.GetInputHint("interact", "Z")} to strike!";
		DrawString(ThemeDB.FallbackFont,
				   new Vector2(origin.X, origin.Y - 10f),
				   strikeHint,
				   HorizontalAlignment.Left, -1, 10, Colors.White);
	}
}
