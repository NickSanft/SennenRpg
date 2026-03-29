using System;
using System.Threading.Tasks;
using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

[Tool]
public partial class Npc : CharacterBody2D, IInteractable
{
	/// <summary>
	/// Unique identifier for this NPC. When set, the flag "talked_to_{NpcId}" is
	/// automatically raised after the first conversation ends — no Signal Event required.
	/// </summary>
	[Export] public string NpcId { get; set; } = "";
	[Export] public string TimelinePath { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "???";
	[Export] public FacingDirection DefaultFacing { get; set; } = FacingDirection.Down;

	/// <summary>
	/// Alternate dialog branches shown when a flag condition is met.
	/// Parallel arrays — index N of AltRequiredFlags pairs with index N of AltTimelinePaths.
	/// Checked in order; the first entry whose flag is set wins.
	/// If none match, TimelinePath is used.
	/// </summary>
	[Export] public string[] AltRequiredFlags { get; set; } = [];
	[Export] public string[] AltTimelinePaths { get; set; } = [];

	/// <summary>
	/// Path to the Dialogic character .dch resource. When set, the examine option
	/// is available and shows the character's description.
	/// </summary>
	[Export] public string CharacterPath { get; set; } = "";

	/// <summary>Body colour used when no sprite sheet is assigned.</summary>
	[Export] public Color PlaceholderColor { get; set; } = new Color(1f, 0.75f, 0.3f);

	/// <summary>Label text shown in the interaction prompt. Override in subclasses.</summary>
	protected virtual string PromptText => "[Z] Talk";

	/// <summary>
	/// Seconds the player must wait before re-triggering this NPC after a conversation ends.
	/// Prevents accidental immediate re-triggers when the player is still holding the interact key.
	/// </summary>
	[Export] public float TalkCooldownSec { get; set; } = 0.5f;

	/// <summary>
	/// World-space waypoints the NPC walks between when idle.
	/// The NPC starts at its spawn position, visits each point in order, then returns home.
	/// Leave empty to keep the NPC stationary.
	/// </summary>
	[Export] public Vector2[] PatrolPoints { get; set; } = [];
	[Export] public float     PatrolSpeed  { get; set; } = 30f;
	[Export] public float     PatrolPause  { get; set; } = 0.5f;

	private AnimatedSprite2D?      _sprite;
	private InteractPromptBubble? _prompt;
	private float                  _talkCooldown;
	private Vector2                _patrolOrigin;
	private Vector2[]              _patrolRoute  = [];
	private int                    _patrolIndex  = 0;
	private float                  _patrolWait   = 0f;  // countdown between waypoints
	protected bool                 _patrolActive = false;
	private bool                   _emoteShown;
	protected string               _characterDescription = "";
	protected Node?                _pendingPlayer;
	private CanvasLayer?           _nameCanvas;
	private Node2D?                _nameLabelNode;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");

		if (!Engine.IsEditorHint())
			AddToGroup("interactable");

		// Load character description from .dch if a path is provided
		if (!string.IsNullOrEmpty(CharacterPath) && ResourceLoader.Exists(CharacterPath))
		{
			var charRes = GD.Load<Resource>(CharacterPath);
			if (charRes != null)
				_characterDescription = charRes.Get("description").AsString();
		}

		if (_sprite?.SpriteFrames == null)
		{
			// Placeholder body
			var body = new Polygon2D();
			body.Polygon = [
				new Vector2(-6, -14), new Vector2(6, -14),
				new Vector2(6, 4),    new Vector2(-6, 4)
			];
			body.Color = PlaceholderColor;

			// Placeholder head
			var head = new Polygon2D();
			head.Polygon = [
				new Vector2(-5, -22), new Vector2(5, -22),
				new Vector2(5, -14),  new Vector2(-5, -14)
			];
			head.Color = new Color(1f, 0.9f, 0.7f); // Skin tone

			AddChild(body);
			AddChild(head);
		}

		// Name label always above NPC
		var nameLabel = new Label
		{
			Text                = DisplayName,
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize   = new Vector2(60, 0),
			Position            = new Vector2(-30, -28),
			LabelSettings       = new LabelSettings
			{
				FontSize     = 15,
				FontColor    = Colors.White,
				OutlineSize  = 2,
				OutlineColor = new Color(0f, 0f, 0f, 0.9f),
			},
		};

		if (Engine.IsEditorHint())
		{
			// In the editor there is no camera zoom, so world-space is fine
			AddChild(nameLabel);
		}
		else
		{
			// At runtime the camera zooms 3x — render in a CanvasLayer so the
			// label is at screen resolution, matching the dialog text quality
			_nameLabelNode          = new Node2D();
			nameLabel.Position      = new Vector2(-30, 0);
			_nameLabelNode.AddChild(nameLabel);
			_nameCanvas             = new CanvasLayer { Layer = 0 };
			_nameCanvas.AddChild(_nameLabelNode);
			GetTree().Root.AddChild(_nameCanvas);
		}

		_prompt = new InteractPromptBubble(PromptText);
		_prompt.Position = new Vector2(0, -40);
		AddChild(_prompt);

		// Apply the default facing direction
		PlayFacingIdle(DefaultFacing);

		if (!Engine.IsEditorHint() && PatrolPoints.Length >= 1)
			Callable.From(StartPatrol).CallDeferred();
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;
		if (_talkCooldown > 0f)
			_talkCooldown -= (float)delta;

		if (_nameLabelNode != null)
		{
			// Keep the name label locked above the NPC in screen space
			var worldPos = GlobalPosition + new Vector2(0, -28);
			var raw      = GetViewportTransform() * worldPos;
			_nameLabelNode.Position = new Vector2(Mathf.Round(raw.X), Mathf.Round(raw.Y));
		}
	}

	public override void _ExitTree()
	{
		_nameCanvas?.QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint()) return;
		TickPatrol((float)delta);
	}

	private void TickPatrol(float delta)
	{
		if (!_patrolActive || _patrolRoute.Length == 0) return;

		if (_patrolWait > 0f)
		{
			_patrolWait -= delta;
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 target   = _patrolRoute[_patrolIndex];
		Vector2 toTarget = target - GlobalPosition;
		float   dist     = toTarget.Length();

		if (dist < 2f)
		{
			// Reached waypoint — snap, pause, advance index
			GlobalPosition = target;
			Velocity       = Vector2.Zero;
			_patrolWait    = PatrolPause;
			_patrolIndex   = (_patrolIndex + 1) % _patrolRoute.Length;
			SetPatrolFacing(target, _patrolRoute[_patrolIndex]);
		}
		else
		{
			Velocity = toTarget.Normalized() * PatrolSpeed;
			MoveAndSlide();
			SetPatrolFacing(GlobalPosition, GlobalPosition + Velocity);
		}
	}

	public virtual void Interact(Node player)
	{
		if (_talkCooldown > 0f) return;
		if (DialogicBridge.Instance.IsRunning()) { GD.Print("[Npc] Dialog already running — aborting."); return; }
		if (_pendingPlayer != null) return;

		_patrolActive  = false;
		Velocity       = Vector2.Zero;
		_pendingPlayer = player;

		if (player is Node2D p2d)
			FaceToward(p2d.GlobalPosition);

		// Show TALK/EXAMINE/CANCEL menu when a character description is available
		if (!string.IsNullOrEmpty(_characterDescription))
		{
			GameManager.Instance.SetState(GameState.Dialog);
			var menu = new NpcInteractMenu();
			GetTree().Root.AddChild(menu);
			menu.Open(_characterDescription);
			menu.TalkSelected += OnMenuTalkSelected;
			menu.Cancelled    += OnMenuCancelled;
			return;
		}

		StartTalkSequence();
	}

	protected virtual void OnMenuTalkSelected()
	{
		StartTalkSequence();
	}

	protected virtual void OnMenuCancelled()
	{
		_pendingPlayer = null;
		_patrolActive  = PatrolPoints.Length >= 1;
		GameManager.Instance.SetState(GameState.Overworld);
	}

	private void StartTalkSequence()
	{
		GD.Print($"[Npc] Interact called on {DisplayName}. TimelinePath: '{TimelinePath}'");
		if (string.IsNullOrEmpty(TimelinePath)) { GD.Print("[Npc] No timeline path set — aborting."); return; }

		GameManager.Instance.SetState(GameState.Dialog);

		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnTimelineEnded));

		string timeline = ChooseTimeline();
		GD.Print($"[Npc] Starting timeline: '{timeline}'");
		DialogicBridge.Instance.StartTimelineWithFlags(timeline);
		GD.Print("[Npc] StartTimelineWithFlags called.");
	}

	public virtual string GetInteractPrompt() => $"Talk to {DisplayName}";

	public void ShowPrompt()
	{
		_prompt?.ShowBubble();
		if (!_emoteShown)
		{
			_emoteShown = true;
			SpawnEmote();
		}
	}

	public void HidePrompt() => _prompt?.HideBubble();

	private string ChooseTimeline()
	{
		// Quest giver overrides take priority over normal alt-flag branching
		var questGiver = GetNodeOrNull<QuestGiver>("QuestGiver");
		if (questGiver != null)
		{
			string questOverride = questGiver.GetQuestTimelineOverride();
			if (!string.IsNullOrEmpty(questOverride))
				return questOverride;
		}
		return SelectTimeline(TimelinePath, AltRequiredFlags, AltTimelinePaths, GameManager.Instance.GetFlag);
	}

	/// <summary>Delegates to <see cref="NpcLogic.SelectTimeline"/>.</summary>
	public static string SelectTimeline(
		string defaultPath,
		string[] altRequiredFlags,
		string[] altTimelinePaths,
		Func<string, bool> flagChecker)
		=> NpcLogic.SelectTimeline(defaultPath, altRequiredFlags, altTimelinePaths, flagChecker);

	// ── Emote ─────────────────────────────────────────────────────────────────

	private void SpawnEmote()
	{
		bool alreadyMet = !string.IsNullOrEmpty(NpcId)
					   && GameManager.Instance.GetFlag(Flags.TalkedTo(NpcId));
		string glyph = alreadyMet ? "?" : "!";

		var label = new Label
		{
			Text          = glyph,
			Position      = new Vector2(-5f, -68f),
			LabelSettings = new LabelSettings
			{
				FontSize     = 14,
				FontColor    = Colors.White,
				OutlineSize  = 2,
				OutlineColor = new Color(0f, 0f, 0f, 0.9f),
			},
		};
		AddChild(label);

		var tween = CreateTween();
		tween.TweenProperty(label, "position:y", label.Position.Y - 10f, 0.45f)
			 .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.45f)
			 .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(label.QueueFree));
	}

	// ── Patrol ────────────────────────────────────────────────────────────────

	private void StartPatrol()
	{
		_patrolOrigin = GlobalPosition;

		// Full route: waypoints in order, then back to origin (loops via modulo)
		_patrolRoute    = new Vector2[PatrolPoints.Length + 1];
		for (int i = 0; i < PatrolPoints.Length; i++)
			_patrolRoute[i] = PatrolPoints[i];
		_patrolRoute[PatrolPoints.Length] = _patrolOrigin;

		_patrolIndex  = 0;
		_patrolWait   = 0f;
		_patrolActive = true;

		SetPatrolFacing(_patrolOrigin, _patrolRoute[0]);
	}

	private void SetPatrolFacing(Vector2 from, Vector2 to)
	{
		var dir = to - from;
		bool horizontal = Mathf.Abs(dir.X) > Mathf.Abs(dir.Y);
		if (_sprite != null && horizontal)
			_sprite.FlipH = dir.X < 0;
		PlayFacingIdle(horizontal ? FacingDirection.Side
					 : dir.Y > 0  ? FacingDirection.Down
					 :               FacingDirection.Up);
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	protected void FaceToward(Vector2 targetPosition)
	{
		Vector2 delta = targetPosition - GlobalPosition;
		FacingDirection dir;
		if (Mathf.Abs(delta.X) > Mathf.Abs(delta.Y))
			dir = delta.X >= 0 ? FacingDirection.Side : FacingDirection.Side; // FlipH handled below
		else
			dir = delta.Y >= 0 ? FacingDirection.Down : FacingDirection.Up;

		if (_sprite != null && Mathf.Abs(delta.X) > Mathf.Abs(delta.Y))
			_sprite.FlipH = delta.X < 0;

		PlayFacingIdle(dir);
	}

	protected void PlayFacingIdle(FacingDirection dir)
	{
		if (_sprite?.SpriteFrames == null) return;
		string anim = dir switch
		{
			FacingDirection.Up   => "idle_up",
			FacingDirection.Side => "idle_side",
			_                    => "idle_down",
		};
		if (_sprite.SpriteFrames.HasAnimation(anim))
			_sprite.Play(anim);
	}

	private void OnTimelineEnded() => _ = DoOnTimelineEnded();

	private async Task DoOnTimelineEnded()
	{
		_talkCooldown  = TalkCooldownSec;
		_pendingPlayer = null;
		_patrolActive  = PatrolPoints.Length >= 1;

		if (!string.IsNullOrEmpty(NpcId))
		{
			string flag = Flags.TalkedTo(NpcId);
			GameManager.Instance.SetFlag(flag, true);
			GD.Print($"[Npc] Flag set: {flag}");
		}

		// Let the QuestGiver handle post-dialog state transitions and reward screen
		var questGiver = GetNodeOrNull<QuestGiver>("QuestGiver");
		if (questGiver != null)
			await questGiver.HandlePostDialog(GetTree().Root);

		// Reset to default facing so the NPC doesn't stay turned toward the player
		if (_sprite != null) _sprite.FlipH = false;
		PlayFacingIdle(DefaultFacing);

		GameManager.Instance.SetState(GameState.Overworld);
	}
}

public enum FacingDirection { Down, Up, Side }
