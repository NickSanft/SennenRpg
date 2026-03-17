using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Overworld enemy that idles until the player enters its detection radius,
/// then chases them. Triggers a battle when it closes to catch distance.
///
/// Place in YSort so it depth-sorts with the player and other NPCs.
/// Assign EncounterResource to control which battle starts.
/// Set PersistenceFlag to a unique string for one-off encounters that should
/// not respawn after the battle has been completed.
/// </summary>
public partial class EnemyPawn : CharacterBody2D
{
	[Export] public EncounterData? EncounterResource { get; set; }
	/// <summary>Radius at which the pawn notices and starts chasing the player.</summary>
	[Export] public float DetectionRadius { get; set; } = 80f;
	/// <summary>Movement speed while chasing (pixels per second).</summary>
	[Export] public float ChaseSpeed { get; set; } = 65f;
	/// <summary>
	/// Optional. When set, the pawn checks this flag on _Ready and removes itself
	/// if the encounter has already been resolved, and sets it before transitioning.
	/// </summary>
	[Export] public string PersistenceFlag { get; set; } = "";

	private CharacterBody2D? _player;
	private bool _chasing;
	private bool _triggered;
	private Label? _exclLabel;

	/// <summary>Pixel distance at which the pawn considers the player caught.</summary>
	private const float CatchDistance = 14f;

	public override void _Ready()
	{
		if (!string.IsNullOrEmpty(PersistenceFlag) && GameManager.Instance.GetFlag(PersistenceFlag))
		{
			QueueFree();
			return;
		}

		// Resize the detection circle at runtime so DetectionRadius export takes effect.
		var detectionShape = GetNodeOrNull<CollisionShape2D>("DetectionArea/CollisionShape2D");
		if (detectionShape?.Shape is CircleShape2D circle)
			circle.Radius = DetectionRadius;

		var detectionArea = GetNode<Area2D>("DetectionArea");
		detectionArea.BodyEntered += OnDetectionEntered;
		detectionArea.BodyExited  += OnDetectionExited;

		_exclLabel = GetNodeOrNull<Label>("ExclLabel");
		if (_exclLabel != null) _exclLabel.Visible = false;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_triggered || !_chasing || _player == null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		if (toPlayer.Length() <= CatchDistance)
		{
			TriggerEncounter();
			return;
		}

		Velocity = toPlayer.Normalized() * ChaseSpeed;
		MoveAndSlide();
	}

	// ── Detection callbacks ───────────────────────────────────────────

	private void OnDetectionEntered(Node2D body)
	{
		if (_triggered || _chasing) return;
		if (!body.IsInGroup("player") || body is not CharacterBody2D cb) return;

		_player  = cb;
		_chasing = true;
		if (_exclLabel != null) _exclLabel.Visible = true;
		GD.Print("[EnemyPawn] Player spotted — chasing.");
	}

	private void OnDetectionExited(Node2D body)
	{
		if (!body.IsInGroup("player")) return;

		_chasing = false;
		_player  = null;
		Velocity = Vector2.Zero;
		if (_exclLabel != null) _exclLabel.Visible = false;
		GD.Print("[EnemyPawn] Player escaped detection range.");
	}

	// ── Encounter trigger ─────────────────────────────────────────────

	private async void TriggerEncounter()
	{
		if (_triggered) return;
		_triggered = true;

		if (EncounterResource == null)
		{
			GD.PushWarning("[EnemyPawn] No EncounterResource assigned — nothing to battle.");
			return;
		}

		GD.Print("[EnemyPawn] Caught player — starting battle.");

		if (!string.IsNullOrEmpty(PersistenceFlag))
			GameManager.Instance.SetFlag(PersistenceFlag, true);

		try
		{
			await SceneTransition.Instance.ToBattleAsync(EncounterResource);
		}
		catch (System.Exception e)
		{
			GD.PushError($"[EnemyPawn] Transition failed: {e.Message}");
			_triggered = false;
		}
	}
}
