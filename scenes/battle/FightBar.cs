using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Undertale-style timing minigame shown when the player selects FIGHT.
/// A cursor bounces back and forth; the player presses interact/ui_accept to stop it.
/// Emits Confirmed(float accuracy) where 1.0 = dead-center, 0.0 = edge.
/// The yellow sweet-spot in the center is drawn via _Draw().
/// </summary>
public partial class FightBar : Control
{
	[Signal] public delegate void ConfirmedEventHandler(float accuracy);

	[Export] public float CursorSpeed { get; set; } = 200f;

	private ColorRect _cursor = null!;
	private float _cursorX   = 0f;
	private float _cursorDir = 1f;
	private bool  _active    = false;

	private const float CursorWidth  = 8f;
	private const float SweetSpotPct = 0.35f; // sweet spot is central 35% of bar

	public override void _Ready()
	{
		// Create cursor in code so the tscn doesn't need a specific child node
		_cursor = GetNodeOrNull<ColorRect>("Cursor") ?? CreateCursor();
	}

	private ColorRect CreateCursor()
	{
		var c = new ColorRect { Color = Colors.White };
		AddChild(c);
		return c;
	}

	/// <summary>Start the cursor animation. Call this when Fight is selected.</summary>
	public void Activate()
	{
		_active   = true;
		_cursorX  = 0f;
		_cursorDir = 1f;
		_cursor.Size = new Vector2(CursorWidth, Size.Y > 0f ? Size.Y : 24f);
		QueueRedraw();
		GD.Print("[FightBar] Activated.");
	}

	public override void _Process(double delta)
	{
		if (!_active) return;

		float barWidth = Mathf.Max(Size.X, 300f);
		float maxX = barWidth - CursorWidth;

		_cursorX += _cursorDir * CursorSpeed * (float)delta;

		if (_cursorX >= maxX)  { _cursorX = maxX;  _cursorDir = -1f; }
		if (_cursorX <= 0f)    { _cursorX = 0f;    _cursorDir =  1f; }

		_cursor.Position = new Vector2(_cursorX, 0f);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_active) return;
		if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
		{
			_active = false;
			float barWidth = Mathf.Max(Size.X, 300f);
			// accuracy: 1.0 at center, 0.0 at edges
			float center    = (barWidth - CursorWidth) * 0.5f;
			float accuracy  = 1f - Mathf.Abs(_cursorX - center) / center;
			accuracy = Mathf.Clamp(accuracy, 0f, 1f);
			GD.Print($"[FightBar] Confirmed. CursorX: {_cursorX:F1}, Accuracy: {accuracy:F2}");
			EmitSignal(SignalName.Confirmed, accuracy);
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _Draw()
	{
		float w = Mathf.Max(Size.X, 300f);
		float h = Mathf.Max(Size.Y, 24f);
		var fullRect = new Rect2(0, 0, w, h);

		// Dark background
		DrawRect(fullRect, new Color(0.12f, 0.12f, 0.12f, 1f));

		// Yellow sweet-spot (center SweetSpotPct of bar width)
		float sweetW     = w * SweetSpotPct;
		float sweetStart = (w - sweetW) * 0.5f;
		DrawRect(new Rect2(sweetStart, 2f, sweetW, h - 4f), new Color(0.9f, 0.75f, 0.1f, 0.5f));

		// White border
		DrawRect(fullRect, Colors.White, filled: false, width: 1.5f);
	}
}
