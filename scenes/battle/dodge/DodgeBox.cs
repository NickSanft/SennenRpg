using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// The arena box shown during the enemy's turn.
/// - Draws a white-bordered box centered at its origin.
/// - Clamps Soul position to stay inside each frame.
/// - Emits PhaseEnded when the timer expires; cleans up bullets automatically.
/// </summary>
public partial class DodgeBox : Node2D
{
	[Signal] public delegate void PhaseEndedEventHandler();

	[Export] public Vector2 BoxSize { get; set; } = new Vector2(160f, 120f);

	private Soul   _soul            = null!;
	public  Node2D BulletContainer  { get; private set; } = null!;

	private float _phaseTimer = 0f;
	private bool  _running    = false;

	public override void _Ready()
	{
		_soul           = GetNode<Soul>("Soul");
		BulletContainer = GetNode<Node2D>("BulletContainer");
	}

	/// <summary>Show the box and start the dodge phase timer.</summary>
	public void StartPhase(float duration)
	{
		_phaseTimer = duration;
		_running    = true;
		_soul.Position = Vector2.Zero; // start soul at center
		_soul.Visible  = true;
		QueueRedraw();
		GD.Print($"[DodgeBox] Phase started. Duration: {duration}s");
	}

	public override void _Process(double delta)
	{
		if (!_running) return;

		_phaseTimer -= (float)delta;

		// Clamp soul within the box (6 px margin so the sprite doesn't clip the border)
		var half = BoxSize * 0.5f;
		const float soulMargin = 6f;
		_soul.Position = new Vector2(
			Mathf.Clamp(_soul.Position.X, -half.X + soulMargin, half.X - soulMargin),
			Mathf.Clamp(_soul.Position.Y, -half.Y + soulMargin, half.Y - soulMargin)
		);

		// Cull any bullet that has left the arena bounds (8 px grace so it visually
		// just clips the border rather than vanishing inside it).
		const float cullMargin = 8f;
		var cullHalf = half + new Vector2(cullMargin, cullMargin);
		CullBulletsRecursive(BulletContainer, cullHalf);

		if (_phaseTimer <= 0f)
			EndPhase();
	}

	/// <summary>
	/// Recursively walks the bullet container tree and frees any BulletBase node
	/// whose global position has left the arena bounds.
	/// Using GlobalPosition means the check is correct even if intermediate nodes
	/// (e.g. PatternRandom) are at non-zero offsets.
	/// </summary>
	private void CullBulletsRecursive(Node parent, Vector2 cullHalf)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is BulletBase bullet)
			{
				var localPos = BulletContainer.ToLocal(bullet.GlobalPosition);
				if (Mathf.Abs(localPos.X) > cullHalf.X || Mathf.Abs(localPos.Y) > cullHalf.Y)
					bullet.QueueFree();
			}
			else if (child.GetChildCount() > 0)
			{
				CullBulletsRecursive(child, cullHalf);
			}
		}
	}

	private void EndPhase()
	{
		_running = false;
		_soul.Visible = false;

		foreach (Node child in BulletContainer.GetChildren())
			child.QueueFree();

		GD.Print("[DodgeBox] Phase ended.");
		EmitSignal(SignalName.PhaseEnded);
	}

	public override void _Draw()
	{
		var half = BoxSize * 0.5f;
		var rect = new Rect2(-half, BoxSize);

		// Dark interior
		DrawRect(rect, new Color(0.08f, 0.08f, 0.08f, 1f));
		// White border (2 px)
		DrawRect(rect, Colors.White, filled: false, width: 2f);
	}
}
