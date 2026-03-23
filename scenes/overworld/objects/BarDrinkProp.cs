using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// An unattended mug of ale sitting on the bar counter.
/// Costs 3 gold to drink; restores 5 HP.
/// </summary>
[Tool]
public partial class BarDrinkProp : Area2D, IInteractable
{
	private const int DrinkCost  = 3;
	private const int HealAmount = 5;

	private InteractPromptBubble? _prompt;
	private bool _drinking = false;

	public override void _Ready()
	{
		if (GetChildCount() > 0) return;

		if (!Engine.IsEditorHint())
			AddToGroup("interactable");

		var col = new CollisionShape2D();
		col.Shape = new RectangleShape2D { Size = new Vector2(10f, 10f) };
		AddChild(col);

		// Mug body
		var body = new Polygon2D { Color = new Color(0.50f, 0.32f, 0.10f) };
		body.Polygon =
		[
			new Vector2(-4f, -4f), new Vector2(4f, -4f),
			new Vector2(5f,  4f),  new Vector2(-5f, 4f),
		];
		AddChild(body);

		// Handle
		var handle = new Polygon2D { Color = new Color(0.40f, 0.24f, 0.07f) };
		handle.Polygon =
		[
			new Vector2( 4f, -2f), new Vector2(7f, -2f),
			new Vector2(7f,  2f),  new Vector2( 4f,  2f),
		];
		AddChild(handle);

		// Foam top
		var foam = new Polygon2D { Color = new Color(0.93f, 0.91f, 0.86f, 0.9f) };
		foam.Polygon =
		[
			new Vector2(-4f, -4f), new Vector2(4f, -4f),
			new Vector2(3f,  -8f), new Vector2(-3f, -8f),
		];
		AddChild(foam);

		_prompt = new InteractPromptBubble($"[Z] Drink ({DrinkCost}g)");
		_prompt.Position = new Vector2(0f, -22f);
		AddChild(_prompt);
	}

	public void ShowPrompt()  => _prompt?.ShowBubble();
	public void HidePrompt()  => _prompt?.HideBubble();
	public string GetInteractPrompt() => "Drink";

	public void Interact(Node player)
	{
		if (_drinking) return;
		_drinking = true;
		HidePrompt();

		if (GameManager.Instance.Gold < DrinkCost)
		{
			SpawnFloatingText("Not enough gold!", new Color(0.9f, 0.3f, 0.2f));
			_drinking = false;
			ShowPrompt();
			return;
		}

		GameManager.Instance.RemoveGold(DrinkCost);
		GameManager.Instance.HealPlayer(HealAmount);
		SpawnFloatingText($"+{HealAmount} HP", new Color(0.35f, 0.90f, 0.35f));

		// Brief warm glow-pulse on the mug
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate", new Color(1.3f, 1.1f, 0.7f), 0.10f);
		tween.TweenProperty(this, "modulate", Colors.White, 0.30f);
		tween.TweenCallback(Callable.From(() =>
		{
			_drinking = false;
			ShowPrompt();
		}));
	}

	private void SpawnFloatingText(string text, Color color)
	{
		var lbl = new Label { Text = text };
		lbl.AddThemeColorOverride("font_color", color);
		lbl.AddThemeFontSizeOverride("font_size", 10);
		lbl.Position = new Vector2(-20f, -28f);
		AddChild(lbl);

		var tween = CreateTween();
		tween.TweenProperty(lbl, "position:y", lbl.Position.Y - 18f, 0.70f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		tween.Parallel().TweenProperty(lbl, "modulate:a", 0f, 0.70f)
			.SetDelay(0.30f).SetTrans(Tween.TransitionType.Quad);
		tween.TweenCallback(Callable.From(lbl.QueueFree));
	}
}
