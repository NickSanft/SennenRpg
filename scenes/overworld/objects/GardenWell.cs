using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A stone wishing well in the garden. Dropping a coin produces a mysterious
/// "thank you" on the first visit; subsequent visits are quietly acknowledged.
/// </summary>
public partial class GardenWell : Area2D, IInteractable
{
	private const string CoinDropSfxPath = "res://assets/audio/sfx/coin_drop.ogg";
	private const string ThanksSfxPath   = "res://assets/audio/sfx/thanks.ogg";

	private InteractPromptBubble _prompt = null!;
	private bool _interacting;

	public override void _Ready()
	{
		// Interaction trigger zone
		AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = 20f },
		});

		BuildVisuals();

		_prompt = new InteractPromptBubble("[Z] Toss a coin");
		_prompt.Position = new Vector2(0f, -28f);
		AddChild(_prompt);
	}

	// ── IInteractable ──────────────────────────────────────────────────────────

	public string GetInteractPrompt() => "[Z] Toss a coin";
	public void   ShowPrompt()        => _prompt.ShowBubble();
	public void   HidePrompt()        => _prompt.HideBubble();

	public void Interact(Node player)
	{
		if (_interacting) return;
		_interacting = true;

		bool alreadyWished = GameManager.Instance.GetFlag(Flags.GardenWellThanked);

		if (alreadyWished)
		{
			ShowWellPopup(
				"",
				new[] { "The water is still.", "You think you hear something..." });
			_interacting = false;
			return;
		}

		// First interact — coin drop → pause → grateful murmur
		if (ResourceLoader.Exists(CoinDropSfxPath))
			AudioManager.Instance.PlaySfx(CoinDropSfxPath);

		GetTree().CreateTimer(1.2f).Connect("timeout", Callable.From(() =>
		{
			if (!IsInsideTree()) return;

			if (ResourceLoader.Exists(ThanksSfxPath))
				AudioManager.Instance.PlaySfx(ThanksSfxPath);

			GameManager.Instance.SetFlag(Flags.GardenWellThanked, true);

			ShowWellPopup(
				"",
				new[] { "...", "...thank you." });

			_interacting = false;
		}));
	}

	// ── Helpers ────────────────────────────────────────────────────────────────

	private void ShowWellPopup(string title, string[] lines)
	{
		var popup = new SignReaderPopup(title, lines);
		GetTree().CurrentScene.AddChild(popup);
	}

	// ── Visuals ────────────────────────────────────────────────────────────────

	private void BuildVisuals()
	{
		var stoneColor  = new Color(0.46f, 0.42f, 0.37f);
		var capColor    = new Color(0.56f, 0.51f, 0.46f);
		var mortarColor = new Color(0.28f, 0.25f, 0.22f);
		var woodColor   = new Color(0.28f, 0.18f, 0.08f);
		var ropeColor   = new Color(0.52f, 0.44f, 0.28f);
		var bucketColor = new Color(0.34f, 0.24f, 0.10f);
		var waterColor  = new Color(0.22f, 0.36f, 0.52f, 0.80f);

		// Octagonal stone basin walls
		const int bpts = 8;
		var outerPoly = new Vector2[bpts];
		var innerPoly = new Vector2[bpts];
		for (int i = 0; i < bpts; i++)
		{
			float a = (Mathf.Tau / bpts) * i + Mathf.Pi / 8f;
			outerPoly[i] = new Vector2(Mathf.Cos(a) * 13f, Mathf.Sin(a) * 7f);
			innerPoly[i] = new Vector2(Mathf.Cos(a) *  9f, Mathf.Sin(a) * 5f);
		}
		AddLocalPoly(Vector2.Zero, outerPoly, stoneColor, zIndex: 1);
		AddLocalPoly(Vector2.Zero, innerPoly, mortarColor, zIndex: 2); // dark interior
		AddLocalPoly(Vector2.Zero, innerPoly, waterColor,  zIndex: 3); // water glint

		// Rim cap highlight
		AddLocalPoly(new Vector2(0f, -6f), new Vector2[]
		{
			new Vector2(-13f, -1f), new Vector2(13f, -1f),
			new Vector2( 13f,  1f), new Vector2(-13f, 1f),
		}, capColor, zIndex: 2);

		// Wooden crossbeam (rotated slight overhang)
		AddLocalPoly(new Vector2(0f, -13f), new Vector2[]
		{
			new Vector2(-14f, -2f), new Vector2(14f, -2f),
			new Vector2( 14f,  2f), new Vector2(-14f, 2f),
		}, woodColor, zIndex: 5);

		// Rope (two short segments, slightly angled)
		AddLocalPoly(new Vector2(-2f, -9f), new Vector2[]
		{
			new Vector2(-0.8f, 0f), new Vector2(0.8f,  0f),
			new Vector2( 0.8f, 5f), new Vector2(-0.8f, 5f),
		}, ropeColor, zIndex: 4);

		// Bucket hanging from rope
		AddLocalPoly(new Vector2(-2f, -3f), new Vector2[]
		{
			new Vector2(-3f, 0f), new Vector2(3f, 0f),
			new Vector2(4f,  6f), new Vector2(-4f, 6f),
		}, bucketColor, zIndex: 5);

		// Bucket rim
		AddLocalPoly(new Vector2(-2f, -3f), new Vector2[]
		{
			new Vector2(-3.5f, -0.5f), new Vector2(3.5f, -0.5f),
			new Vector2( 3.5f,  0.5f), new Vector2(-3.5f, 0.5f),
		}, woodColor, zIndex: 6);
	}

	/// <summary>Adds a Polygon2D as a child at a local-space position (not global).</summary>
	private Polygon2D AddLocalPoly(Vector2 localPos, Vector2[] polygon, Color color, int zIndex)
	{
		var poly = new Polygon2D { Color = color, ZIndex = zIndex, Polygon = polygon };
		AddChild(poly);
		poly.Position = localPos;
		return poly;
	}
}
