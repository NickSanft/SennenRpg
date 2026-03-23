using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;
using SennenRpg.Scenes.Hud;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A stone bench in the garden. Sitting restores 5 HP with a gentle screen dim.
/// </summary>
public partial class GardenBench : Area2D, IInteractable
{
	private InteractPromptBubble _prompt = null!;
	private bool _resting;

	public override void _Ready()
	{
		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(36f, 20f) },
		});

		BuildVisuals();

		_prompt = new InteractPromptBubble("[Z] Sit");
		_prompt.Position = new Vector2(0f, -24f);
		AddChild(_prompt);
	}

	// ── IInteractable ──────────────────────────────────────────────────────────

	public string GetInteractPrompt() => "[Z] Sit";
	public void   ShowPrompt()        => _prompt.ShowBubble();
	public void   HidePrompt()        => _prompt.HideBubble();

	public void Interact(Node player)
	{
		if (_resting) return;
		_resting = true;

		// Dim overlay (CanvasLayer 58 so it sits above HUD but below menus)
		var dimLayer = new CanvasLayer { Layer = 58 };
		var dimRect  = new ColorRect
		{
			Color        = new Color(0f, 0f, 0f, 0f),
			AnchorRight  = 1f,
			AnchorBottom = 1f,
		};
		dimLayer.AddChild(dimRect);
		GetTree().CurrentScene.AddChild(dimLayer);

		var dimIn = CreateTween();
		dimIn.TweenProperty(dimRect, "color:a", 0.30f, 0.5f)
			.SetTrans(Tween.TransitionType.Sine);
		dimIn.TweenCallback(Callable.From(() =>
		{
			GameManager.Instance.HealPlayer(5);

			// Flash HP bar green to signal the heal
			GetTree().CurrentScene
				.GetNodeOrNull<GameHud>("GameHud")
				?.FlashHpBar(new Color(0.15f, 0.80f, 0.15f));

			var popup = new SignReaderPopup(
				"",
				new[] { "You sit for a while.", "You feel a little better." });
			popup.Connect(SignReaderPopup.SignalName.Closed, Callable.From(() =>
			{
				var dimOut = CreateTween();
				dimOut.TweenProperty(dimRect, "color:a", 0f, 0.4f)
					.SetTrans(Tween.TransitionType.Sine);
				dimOut.TweenCallback(Callable.From(dimLayer.QueueFree));
				_resting = false;
			}));
			GetTree().CurrentScene.AddChild(popup);
		}));
	}

	// ── Visuals ────────────────────────────────────────────────────────────────

	private void BuildVisuals()
	{
		var stoneColor   = new Color(0.50f, 0.46f, 0.41f);
		var capColor     = new Color(0.60f, 0.56f, 0.50f);
		var shadowColor  = new Color(0.30f, 0.27f, 0.24f);

		// Seat slab
		AddLocalPoly(Vector2.Zero, new Vector2[]
		{
			new Vector2(-18f, -3f), new Vector2(18f, -3f),
			new Vector2( 18f,  3f), new Vector2(-18f, 3f),
		}, stoneColor, zIndex: 2);

		// Seat cap (top highlight)
		AddLocalPoly(new Vector2(0f, -3f), new Vector2[]
		{
			new Vector2(-18f, -1f), new Vector2(18f, -1f),
			new Vector2( 18f,  1f), new Vector2(-18f, 1f),
		}, capColor, zIndex: 3);

		// Backrest
		AddLocalPoly(new Vector2(0f, -10f), new Vector2[]
		{
			new Vector2(-18f, -4f), new Vector2(18f, -4f),
			new Vector2( 18f,  4f), new Vector2(-18f, 4f),
		}, stoneColor, zIndex: 2);
		AddLocalPoly(new Vector2(0f, -14f), new Vector2[]
		{
			new Vector2(-18f, -1f), new Vector2(18f, -1f),
			new Vector2( 18f,  1f), new Vector2(-18f, 1f),
		}, capColor, zIndex: 3);

		// Left leg
		AddLocalPoly(new Vector2(-13f, 5f), new Vector2[]
		{
			new Vector2(-2f, 0f), new Vector2(2f, 0f),
			new Vector2(2f,  6f), new Vector2(-2f, 6f),
		}, shadowColor, zIndex: 1);

		// Right leg
		AddLocalPoly(new Vector2(13f, 5f), new Vector2[]
		{
			new Vector2(-2f, 0f), new Vector2(2f, 0f),
			new Vector2(2f,  6f), new Vector2(-2f, 6f),
		}, shadowColor, zIndex: 1);
	}

	private Polygon2D AddLocalPoly(Vector2 localPos, Vector2[] polygon, Color color, int zIndex)
	{
		var poly = new Polygon2D { Color = color, ZIndex = zIndex, Polygon = polygon };
		AddChild(poly);
		poly.Position = localPos;
		return poly;
	}
}
