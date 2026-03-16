using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

public partial class Npc : CharacterBody2D, IInteractable
{
	[Export] public string TimelinePath { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "???";

	public override void _Ready()
	{
		AddToGroup("interactable");
		var sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");
		if (sprite?.SpriteFrames == null)
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

			// Name label above NPC
			var label = new Label();
			label.Text = DisplayName;
			label.Position = new Vector2(-20, -34);
			label.AddThemeColorOverride("font_color", Colors.White);
			label.AddThemeFontSizeOverride("font_size", 8);

			AddChild(body);
			AddChild(head);
			AddChild(label);
		}
	}

	public void Interact(Node player)
	{
		GD.Print($"[Npc] Interact called on {DisplayName}. TimelinePath: '{TimelinePath}'");
		if (string.IsNullOrEmpty(TimelinePath)) { GD.Print("[Npc] No timeline path set — aborting."); return; }
		if (DialogicBridge.Instance.IsRunning()) { GD.Print("[Npc] Dialog already running — aborting."); return; }

		GameManager.Instance.SetState(GameState.Dialog);

		// Note: to pass variables into a timeline, define them first in the
		// Dialogic editor (Variables tab), then call DialogicBridge.SetVariable().

		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnTimelineEnded));

		DialogicBridge.Instance.StartTimeline(TimelinePath);
	}

	public string GetInteractPrompt() => $"Talk to {DisplayName}";

	private void OnTimelineEnded()
	{
		GameManager.Instance.SetState(GameState.Overworld);
	}
}
