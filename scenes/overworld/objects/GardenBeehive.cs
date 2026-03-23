using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A rope-hung beehive in the north-east corner of the garden.
/// First interact: bees buzz, then player finds honey.
/// Subsequent: the bees give you a look.
/// </summary>
public partial class GardenBeehive : Area2D, IInteractable
{
	private const string HoneyItemPath  = "res://resources/items/item_honey.tres";
	private const string BuzzSfxPath    = "res://assets/audio/sfx/bee_buzz.ogg";

	private InteractPromptBubble _prompt = null!;
	private bool _interacting;

	public override void _Ready()
	{
		AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = 16f },
		});

		BuildVisuals();

		_prompt = new InteractPromptBubble("[Z] Inspect");
		_prompt.Position = new Vector2(0f, -30f);
		AddChild(_prompt);
	}

	// ── IInteractable ──────────────────────────────────────────────────────────

	public string GetInteractPrompt() => "[Z] Inspect";
	public void   ShowPrompt()        => _prompt.ShowBubble();
	public void   HidePrompt()        => _prompt.HideBubble();

	public void Interact(Node player)
	{
		if (_interacting) return;
		_interacting = true;

		bool alreadyTaken = GameManager.Instance.GetFlag(Flags.GardenHiveTaken);

		if (alreadyTaken)
		{
			ShowHivePopup("", new[] { "The bees eye you." });
			_interacting = false;
			return;
		}

		// Bee burst visual
		SpawnBeeBurst();

		if (ResourceLoader.Exists(BuzzSfxPath))
			AudioManager.Instance.PlaySfx(BuzzSfxPath);

		GetTree().CreateTimer(0.7f).Connect("timeout", Callable.From(() =>
		{
			if (!IsInsideTree()) return;

			GameManager.Instance.SetFlag(Flags.GardenHiveTaken, true);

			string[] lines;
			if (ResourceLoader.Exists(HoneyItemPath))
			{
				GameManager.Instance.AddItem(HoneyItemPath);
				lines = new[] { "The bees don't seem bothered.", "You found some honey!" };
			}
			else
			{
				lines = new[] { "The bees don't seem bothered.", "It smells wonderful." };
			}

			ShowHivePopup("", lines);
			_interacting = false;
		}));
	}

	// ── Bee burst ─────────────────────────────────────────────────────────────

	private void SpawnBeeBurst()
	{
		var beeColor = new Color(0.92f, 0.80f, 0.20f);
		var rng = new RandomNumberGenerator();
		rng.Seed = (ulong)Time.GetTicksMsec();

		for (int i = 0; i < 12; i++)
		{
			float angle  = (Mathf.Tau / 12f) * i;
			float radius = rng.RandfRange(18f, 32f);
			var   target = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

			var bee = new Polygon2D
			{
				Color   = beeColor,
				ZIndex  = 15,
				Polygon = new Vector2[]
				{
					new Vector2( 0f, -2f),
					new Vector2( 2f,  0f),
					new Vector2( 0f,  2f),
					new Vector2(-2f,  0f),
				},
			};
			AddChild(bee);
			bee.Position = Vector2.Zero;

			var fly = CreateTween();
			fly.TweenProperty(bee, "position", target, 0.35f)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
			fly.TweenProperty(bee, "position", Vector2.Zero, 0.35f)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
			fly.TweenCallback(Callable.From(bee.QueueFree));
		}
	}

	// ── Helpers ────────────────────────────────────────────────────────────────

	private void ShowHivePopup(string title, string[] lines)
	{
		var popup = new SignReaderPopup(title, lines);
		GetTree().CurrentScene.AddChild(popup);
	}

	// ── Visuals ────────────────────────────────────────────────────────────────

	private void BuildVisuals()
	{
		var amberColor  = new Color(0.80f, 0.55f, 0.12f);
		var darkAmber   = new Color(0.55f, 0.36f, 0.06f);
		var ropeColor   = new Color(0.50f, 0.42f, 0.26f);
		var cellColor   = new Color(0.90f, 0.68f, 0.18f, 0.70f);
		var holeColor   = new Color(0.25f, 0.14f, 0.02f);

		// Hanging rope
		AddLocalPoly(new Vector2(0f, -20f), new Vector2[]
		{
			new Vector2(-0.8f, 0f), new Vector2(0.8f,  0f),
			new Vector2( 0.8f, 8f), new Vector2(-0.8f, 8f),
		}, ropeColor, zIndex: 3);

		// Hive body — slightly tapered hexagonal blob
		const int hpts = 6;
		var hivePoly = new Vector2[hpts];
		for (int i = 0; i < hpts; i++)
		{
			float a  = (Mathf.Tau / hpts) * i - Mathf.Pi / 2f;
			float rx = (i % 2 == 0) ? 9f : 10f;
			float ry = (i % 2 == 0) ? 12f : 10f;
			hivePoly[i] = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
		}
		AddLocalPoly(new Vector2(0f, -5f), hivePoly, amberColor, zIndex: 4);

		// Honeycomb cell strips (three horizontal bands)
		float[] cellY = { -13f, -7f, -1f };
		foreach (float cy in cellY)
		{
			AddLocalPoly(new Vector2(0f, cy), new Vector2[]
			{
				new Vector2(-8f, -1f), new Vector2(8f, -1f),
				new Vector2(8f,   1f), new Vector2(-8f, 1f),
			}, cellColor, zIndex: 5);
		}
		// Vertical cell dividers
		float[] cellX = { -4f, 0f, 4f };
		foreach (float cx in cellX)
		{
			AddLocalPoly(new Vector2(cx, -7f), new Vector2[]
			{
				new Vector2(-0.6f, -6f), new Vector2(0.6f, -6f),
				new Vector2( 0.6f,  6f), new Vector2(-0.6f, 6f),
			}, darkAmber, zIndex: 5);
		}

		// Entry hole
		AddLocalPoly(new Vector2(0f, 2f), new Vector2[]
		{
			new Vector2(-3f, -1.5f), new Vector2(3f, -1.5f),
			new Vector2( 3f,  1.5f), new Vector2(-3f, 1.5f),
		}, holeColor, zIndex: 6);
	}

	private Polygon2D AddLocalPoly(Vector2 localPos, Vector2[] polygon, Color color, int zIndex)
	{
		var poly = new Polygon2D { Color = color, ZIndex = zIndex, Polygon = polygon };
		AddChild(poly);
		poly.Position = localPos;
		return poly;
	}
}
