using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Transitions the player to another map.
/// AutoTrigger = true  → fires on body_entered (walk-in exits, like Undertale room edges).
/// AutoTrigger = false → waits for the player to press interact (doors).
/// ExitHint            → draws a pulsing directional arrow to hint at off-screen exits.
/// </summary>
public partial class MapExit : Area2D, IInteractable
{
	public enum ExitHintDirection { None = 0, Up = 1, Down = 2, Left = 3, Right = 4 }

	[Export] public string            TargetMapPath { get; set; } = "";
	[Export] public string            TargetSpawnId { get; set; } = "default";
	[Export] public bool              AutoTrigger   { get; set; } = false;
	/// <summary>When true, SaveManager writes the current slot before the transition begins.</summary>
	[Export] public bool              AutoSave      { get; set; } = false;
	[Export] public ExitHintDirection ExitHint      { get; set; } = ExitHintDirection.None;

	private bool                   _triggered = false;
	private InteractPromptBubble? _prompt;

	public override void _Ready()
	{
		AddToGroup("map_exits");

		if (!AutoTrigger)
		{
			AddToGroup("interactable");

			_prompt = new InteractPromptBubble("[Z] Enter");
			_prompt.Position = new Vector2(0, -20);
			AddChild(_prompt);
		}

		if (ExitHint != ExitHintDirection.None)
			SpawnArrow(ExitHint);

		BodyEntered += OnBodyEntered;
	}

	public void ShowPrompt() => _prompt?.ShowBubble();
	public void HidePrompt() => _prompt?.HideBubble();

	private void OnBodyEntered(Node2D body)
	{
		if (!AutoTrigger) return;
		if (body.IsInGroup("player") || body is CharacterBody2D)
			TriggerExit();
	}

	public void Interact(Node player) => TriggerExit();

	public string GetInteractPrompt() => "Enter";

	private void TriggerExit()
	{
		if (_triggered || string.IsNullOrEmpty(TargetMapPath)) return;
		_triggered = true;

		GameManager.Instance.SetLastSpawn(TargetSpawnId);
		_ = SceneTransition.Instance.GoToAsync(TargetMapPath, autoSave: AutoSave);
	}

	private void SpawnArrow(ExitHintDirection dir)
	{
		var arrow = new Polygon2D();
		arrow.Color = new Color(1f, 1f, 0.3f, 0.85f);

		arrow.Polygon = dir switch
		{
			ExitHintDirection.Up    => [new Vector2(0, -12), new Vector2(-6, -4), new Vector2(6, -4)],
			ExitHintDirection.Down  => [new Vector2(0,  12), new Vector2(-6,  4), new Vector2(6,  4)],
			ExitHintDirection.Left  => [new Vector2(-12, 0), new Vector2(-4, -6), new Vector2(-4, 6)],
			ExitHintDirection.Right => [new Vector2( 12, 0), new Vector2(  4, -6), new Vector2( 4, 6)],
			_ => []
		};

		// Offset so the arrow sits just inside the visible edge, not on the trigger centre
		arrow.Position = dir switch
		{
			ExitHintDirection.Up    => new Vector2(0, -20),
			ExitHintDirection.Down  => new Vector2(0,  20),
			ExitHintDirection.Left  => new Vector2(-20, 0),
			ExitHintDirection.Right => new Vector2( 20, 0),
			_ => Vector2.Zero
		};

		AddChild(arrow);

		// Pulse: fade between 0.4 and 1.0, looping
		arrow.Modulate = new Color(1, 1, 1, 0.4f);
		var tween = CreateTween().SetLoops();
		tween.TweenProperty(arrow, "modulate:a", 1.0f, 0.6f);
		tween.TweenProperty(arrow, "modulate:a", 0.4f, 0.6f);
	}
}
