using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

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
	/// Checked in order — the first option whose RequiredFlag is set wins.
	/// If none match, TimelinePath is used.
	/// Each entry is an <see cref="NpcDialogOption"/> sub-resource set in the Godot inspector.
	/// </summary>
	[Export] public NpcDialogOption[] AltDialogOptions { get; set; } = [];

	/// <summary>Label text shown in the interaction prompt. Override in subclasses.</summary>
	protected virtual string PromptText => "[Z] Talk";

	/// <summary>
	/// Seconds the player must wait before re-triggering this NPC after a conversation ends.
	/// Prevents accidental immediate re-triggers when the player is still holding the interact key.
	/// </summary>
	[Export] public float TalkCooldownSec { get; set; } = 0.5f;

	private AnimatedSprite2D? _sprite;
	private Label? _promptLabel;
	private float _talkCooldown;

	public override void _Ready()
	{
		AddToGroup("interactable");
		_sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");

		if (_sprite?.SpriteFrames == null)
		{
			// Placeholder body
			var body = new Polygon2D();
			body.Polygon = [
				new Vector2(-6, -14), new Vector2(6, -14),
				new Vector2(6, 4),    new Vector2(-6, 4)
			];
			body.Color = new Color(1f, 0.75f, 0.3f); // Orange

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
		var nameLabel = new Label();
		nameLabel.Text = DisplayName;
		nameLabel.Position = new Vector2(-20, -38);
		nameLabel.AddThemeColorOverride("font_color", Colors.White);
		nameLabel.AddThemeFontSizeOverride("font_size", 8);
		AddChild(nameLabel);

		// Interact prompt — hidden by default
		_promptLabel = new Label();
		_promptLabel.Text = PromptText;
		_promptLabel.Position = new Vector2(-20, -50);
		_promptLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		_promptLabel.AddThemeFontSizeOverride("font_size", 8);
		_promptLabel.Visible = false;
		AddChild(_promptLabel);

		// Apply the default facing direction
		PlayFacingIdle(DefaultFacing);
	}

	public override void _Process(double delta)
	{
		if (_talkCooldown > 0f)
			_talkCooldown -= (float)delta;
	}

	public virtual void Interact(Node player)
	{
		if (_talkCooldown > 0f) return;
		GD.Print($"[Npc] Interact called on {DisplayName}. TimelinePath: '{TimelinePath}'");
		if (string.IsNullOrEmpty(TimelinePath)) { GD.Print("[Npc] No timeline path set — aborting."); return; }

		bool running = DialogicBridge.Instance.IsRunning();
		GD.Print($"[Npc] IsRunning = {running}");
		if (running) { GD.Print("[Npc] Dialog already running — aborting."); return; }

		if (player is Node2D p2d)
			FaceToward(p2d.GlobalPosition);

		GameManager.Instance.SetState(GameState.Dialog);

		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnTimelineEnded));

		string timeline = ChooseTimeline();
		GD.Print($"[Npc] Starting timeline: '{timeline}'");
		DialogicBridge.Instance.StartTimelineWithFlags(timeline);
		GD.Print("[Npc] StartTimelineWithFlags called.");
	}

	public virtual string GetInteractPrompt() => $"Talk to {DisplayName}";

	public void ShowPrompt() { if (_promptLabel != null) _promptLabel.Visible = true; }
	public void HidePrompt() { if (_promptLabel != null) _promptLabel.Visible = false; }

	private string ChooseTimeline()
		=> SelectTimeline(TimelinePath, AltDialogOptions, GameManager.Instance.GetFlag);

	/// <summary>
	/// Pure selection logic: returns the path from the first option whose RequiredFlag
	/// is set by <paramref name="flagChecker"/>, or <paramref name="defaultPath"/> if none match.
	/// Exposed as public static so it can be unit-tested without a Godot scene.
	/// </summary>
	public static string SelectTimeline(
		string defaultPath,
		NpcDialogOption[] options,
		Func<string, bool> flagChecker)
	{
		foreach (var opt in options)
		{
			if (!string.IsNullOrEmpty(opt.RequiredFlag) && flagChecker(opt.RequiredFlag))
				return opt.TimelinePath;
		}
		return defaultPath;
	}

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

	private void OnTimelineEnded()
	{
		_talkCooldown = TalkCooldownSec;

		if (!string.IsNullOrEmpty(NpcId))
		{
			string flag = Flags.TalkedTo(NpcId);
			GameManager.Instance.SetFlag(flag, true);
			GD.Print($"[Npc] Flag set: {flag}");
		}

		// Reset to default facing so the NPC doesn't stay turned toward the player
		if (_sprite != null) _sprite.FlipH = false;
		PlayFacingIdle(DefaultFacing);

		GameManager.Instance.SetState(GameState.Overworld);
	}
}

public enum FacingDirection { Down, Up, Side }
