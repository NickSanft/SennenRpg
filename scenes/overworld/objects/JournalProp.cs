using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// An interactable journal sitting on a table.
/// Opens a browsable list of Aoife Sylzair's expedition entries.
/// </summary>
public partial class JournalProp : Area2D, IInteractable
{

	private InteractPromptBubble? _prompt;
	private bool _open = false;

	public override void _Ready()
	{
		AddToGroup("interactable");

		// Collision shape
		var shape = new CollisionShape2D();
		shape.Shape = new RectangleShape2D { Size = new Vector2(10f, 12f) };
		AddChild(shape);

		// Visual — a small book lying on the table
		// Cover
		var cover = new Polygon2D { Color = new Color(0.35f, 0.18f, 0.08f) };
		cover.Polygon =
		[
			new Vector2(-5f, -6f), new Vector2(5f, -6f),
			new Vector2(5f,  6f),  new Vector2(-5f, 6f),
		];
		AddChild(cover);

		// Spine
		var spine = new Polygon2D { Color = new Color(0.22f, 0.10f, 0.04f) };
		spine.Polygon =
		[
			new Vector2(-5f, -6f), new Vector2(-3f, -6f),
			new Vector2(-3f,  6f), new Vector2(-5f,  6f),
		];
		AddChild(spine);

		// Pages (right edge — cream strip)
		var pages = new Polygon2D { Color = new Color(0.92f, 0.88f, 0.78f) };
		pages.Polygon =
		[
			new Vector2(4f, -5f), new Vector2(5f, -5f),
			new Vector2(5f,  5f), new Vector2(4f,  5f),
		];
		AddChild(pages);

		// Title initial "A" on cover
		var initial = new Label
		{
			Text     = "A",
			Position = new Vector2(-3f, -6f),
		};
		initial.AddThemeColorOverride("font_color", new Color(0.75f, 0.55f, 0.15f));
		initial.AddThemeFontSizeOverride("font_size", 7);
		AddChild(initial);

		_prompt = new InteractPromptBubble("[Z] Read");
		_prompt.Position = new Vector2(0f, -18f);
		AddChild(_prompt);
	}

	public void ShowPrompt() => _prompt?.ShowBubble();
	public void HidePrompt() => _prompt?.HideBubble();
	public string GetInteractPrompt() => "Read";

	public void Interact(Node player)
	{
		if (_open) return;
		_open = true;
		HidePrompt();
		GameManager.Instance.SetState(GameState.Dialog);

		var menu = new JournalMenuPopup(JournalData.Entries);
		menu.Closed += OnMenuClosed;
		GetTree().CurrentScene.AddChild(menu);
	}

	private void OnMenuClosed()
	{
		_open = false;
		GameManager.Instance.SetState(GameState.Overworld);
		ShowPrompt();
	}
}
