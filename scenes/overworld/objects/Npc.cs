using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

public partial class Npc : CharacterBody2D, IInteractable
{
	[Export] public string TimelinePath { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "???";
	[Export] public FacingDirection DefaultFacing { get; set; } = FacingDirection.Down;

	/// <summary>Alternate timelines shown when a matching flag is set. Checked in order — first match wins.</summary>
	[Export] public string[] AltTimelinePaths { get; set; } = [];
	[Export] public string[] AltTimelineFlags { get; set; } = [];

	private AnimatedSprite2D? _sprite;
	private Label? _promptLabel;

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
		_promptLabel.Text = $"[Z] Talk";
		_promptLabel.Position = new Vector2(-20, -50);
		_promptLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		_promptLabel.AddThemeFontSizeOverride("font_size", 8);
		_promptLabel.Visible = false;
		AddChild(_promptLabel);

		// Apply the default facing direction
		PlayFacingIdle(DefaultFacing);
	}

	public void Interact(Node player)
	{
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
		DialogicBridge.Instance.StartTimeline(timeline);
		GD.Print("[Npc] StartTimeline called.");
	}

	public string GetInteractPrompt() => $"Talk to {DisplayName}";

	public void ShowPrompt() { if (_promptLabel != null) _promptLabel.Visible = true; }
	public void HidePrompt() { if (_promptLabel != null) _promptLabel.Visible = false; }

	private string ChooseTimeline()
	{
		for (int i = 0; i < AltTimelineFlags.Length && i < AltTimelinePaths.Length; i++)
		{
			if (GameManager.Instance.GetFlag(AltTimelineFlags[i]))
				return AltTimelinePaths[i];
		}
		return TimelinePath;
	}

	private void FaceToward(Vector2 targetPosition)
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

	private void PlayFacingIdle(FacingDirection dir)
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
		GameManager.Instance.SetState(GameState.Overworld);
	}
}

public enum FacingDirection { Down, Up, Side }
