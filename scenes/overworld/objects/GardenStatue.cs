using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A stone robed statue at the north end of the garden path.
/// On the first interaction it slowly turns 90° to face east, accompanied
/// by a faint grinding sound and an unusual plaque inscription.
/// Subsequent visits: it stares at the wall.
/// </summary>
public partial class GardenStatue : Area2D, IInteractable
{
	private const string TurnSfxPath = "res://assets/audio/sfx/stone_grind.ogg";

	private InteractPromptBubble _prompt = null!;
	private bool _interacting;

	public override void _Ready()
	{
		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(20f, 28f) },
		});

		BuildVisuals();

		_prompt = new InteractPromptBubble("[Z] Inspect");
		_prompt.Position = new Vector2(0f, -32f);
		AddChild(_prompt);

		// Restore turned orientation if already seen
		if (!Engine.IsEditorHint() && GameManager.Instance.GetFlag(Flags.GardenStatueTurned))
			RotationDegrees = 90f;
	}

	// ── IInteractable ──────────────────────────────────────────────────────────

	public string GetInteractPrompt() => "[Z] Inspect";
	public void   ShowPrompt()        => _prompt.ShowBubble();
	public void   HidePrompt()        => _prompt.HideBubble();

	public void Interact(Node player)
	{
		if (_interacting) return;
		_interacting = true;

		if (GameManager.Instance.GetFlag(Flags.GardenStatueTurned))
		{
			ShowPopup("", new[] { "It stares at the east wall.", "You feel watched." });
			_interacting = false;
			return;
		}

		// First interact — statue turns
		if (ResourceLoader.Exists(TurnSfxPath))
			AudioManager.Instance.PlaySfx(TurnSfxPath);

		var turn = CreateTween();
		turn.TweenProperty(this, "rotation_degrees", 90f, 1.8f)
			.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
		turn.TweenCallback(Callable.From(() =>
		{
			if (!IsInsideTree()) return;
			GameManager.Instance.SetFlag(Flags.GardenStatueTurned, true);
			ShowPopup("",
				new[]
				{
					"...",
					"A plaque on the base reads:",
					"\"I am simply enjoying the garden.\"",
				});
			_interacting = false;
		}));
	}

	// ── Helpers ────────────────────────────────────────────────────────────────

	private void ShowPopup(string title, string[] lines)
	{
		var popup = new SignReaderPopup(title, lines);
		GetTree().CurrentScene.AddChild(popup);
	}

	// ── Visuals ────────────────────────────────────────────────────────────────

	private void BuildVisuals()
	{
		var stoneColor  = new Color(0.52f, 0.48f, 0.43f);
		var capColor    = new Color(0.62f, 0.57f, 0.52f);
		var darkStone   = new Color(0.36f, 0.33f, 0.29f);
		var robeColor   = new Color(0.46f, 0.42f, 0.38f);
		var plaqueColor = new Color(0.42f, 0.36f, 0.28f);

		// Plinth slab cap
		AddLocalPoly(new Vector2(0f, 12f), new Vector2[]
		{
			new Vector2(-10f, -2f), new Vector2(10f, -2f),
			new Vector2( 10f,  2f), new Vector2(-10f, 2f),
		}, capColor, zIndex: 1);
		// Plinth block
		AddLocalPoly(new Vector2(0f, 16f), new Vector2[]
		{
			new Vector2(-8f, -4f), new Vector2(8f, -4f),
			new Vector2(8f,   4f), new Vector2(-8f, 4f),
		}, stoneColor, zIndex: 1);
		// Plinth plaque
		AddLocalPoly(new Vector2(0f, 15f), new Vector2[]
		{
			new Vector2(-5f, -1.5f), new Vector2(5f, -1.5f),
			new Vector2(5f,   1.5f), new Vector2(-5f, 1.5f),
		}, plaqueColor, zIndex: 2);

		// Pedestal column
		AddLocalPoly(new Vector2(0f, 2f), new Vector2[]
		{
			new Vector2(-5f, -10f), new Vector2(5f, -10f),
			new Vector2(6f,   0f),  new Vector2(-6f, 0f),
		}, darkStone, zIndex: 2);

		// Robe body (slightly wider at base)
		AddLocalPoly(new Vector2(0f, -7f), new Vector2[]
		{
			new Vector2(-4f, -6f), new Vector2(4f, -6f),
			new Vector2(5f,   6f), new Vector2(-5f, 6f),
		}, robeColor, zIndex: 3);
		// Robe hem seam
		AddLocalPoly(new Vector2(0f, -2f), new Vector2[]
		{
			new Vector2(-5f, -0.8f), new Vector2(5f, -0.8f),
			new Vector2(5f,   0.8f), new Vector2(-5f, 0.8f),
		}, darkStone, zIndex: 4);

		// Clasped hands (mid-body)
		AddLocalPoly(new Vector2(0f, -9f), new Vector2[]
		{
			new Vector2(-2.5f, -1.5f), new Vector2(2.5f, -1.5f),
			new Vector2( 2.5f,  1.5f), new Vector2(-2.5f, 1.5f),
		}, capColor, zIndex: 4);

		// Head
		AddLocalPoly(new Vector2(0f, -17f), new Vector2[]
		{
			new Vector2( 0f, -4f),
			new Vector2( 3f,  0f),
			new Vector2( 0f,  3f),
			new Vector2(-3f,  0f),
		}, stoneColor, zIndex: 4);
		// Hood shadow across face
		AddLocalPoly(new Vector2(0f, -16f), new Vector2[]
		{
			new Vector2(-3f, -1f), new Vector2(3f, -1f),
			new Vector2(2f,   2f), new Vector2(-2f, 2f),
		}, darkStone, zIndex: 5);
	}

	private Polygon2D AddLocalPoly(Vector2 localPos, Vector2[] polygon, Color color, int zIndex)
	{
		var poly = new Polygon2D { Color = color, ZIndex = zIndex, Polygon = polygon };
		AddChild(poly);
		poly.Position = localPos;
		return poly;
	}
}
