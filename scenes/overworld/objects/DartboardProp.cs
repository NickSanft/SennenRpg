using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A dartboard on the wall. Interact to throw a dart and get a random result.
/// </summary>
[Tool]
public partial class DartboardProp : Area2D, IInteractable
{
	private static readonly string[] Results =
	[
		"Bullseye!",
		"Inner ring.",
		"Outer ring.",
		"You hit the wood around it.",
		"Bounces off the wall.",
		"Lands in someone's drink.",
		"Impressive.",
		"Less impressive.",
	];

	private InteractPromptBubble? _prompt;
	private bool _throwing = false;

	public override void _Ready()
	{
		if (GetChildCount() > 0) return;

		if (!Engine.IsEditorHint())
			AddToGroup("interactable");

		var col = new CollisionShape2D();
		col.Shape = new RectangleShape2D { Size = new Vector2(18f, 18f) };
		AddChild(col);

		// Build concentric ring dartboard (layers drawn back to front)
		BuildDartboard();

		_prompt = new InteractPromptBubble("[Z] Throw");
		_prompt.Position = new Vector2(0f, -18f);
		AddChild(_prompt);
	}

	public void ShowPrompt()  => _prompt?.ShowBubble();
	public void HidePrompt()  => _prompt?.HideBubble();
	public string GetInteractPrompt() => "Throw dart";

	public void Interact(Node player)
	{
		if (_throwing) return;
		_throwing = true;
		HidePrompt();

		string result = Results[GD.RandRange(0, Results.Length - 1)];
		SpawnResultText(result);

		GetTree().CreateTimer(1.2f).Connect("timeout", Callable.From(() =>
		{
			_throwing = false;
			ShowPrompt();
		}));
	}

	private void BuildDartboard()
	{
		// Rings drawn largest → smallest so each sits on top of the previous
		var rings = new (float radius, Color color)[]
		{
			(9f,  new Color(0.12f, 0.10f, 0.08f)), // outer black
			(7f,  new Color(0.75f, 0.12f, 0.08f)), // red
			(5f,  new Color(0.12f, 0.10f, 0.08f)), // black
			(3f,  new Color(0.75f, 0.12f, 0.08f)), // red inner
			(1.5f, new Color(0.92f, 0.80f, 0.10f)), // bullseye
		};

		int z = 1;
		foreach (var (radius, color) in rings)
		{
			var poly = new Polygon2D { Color = color, ZIndex = z++ };
			poly.Polygon = MakeCircle(radius);
			AddChild(poly);
		}

		// Mounting board (wooden rectangle behind the rings)
		var board = new Polygon2D
		{
			Color   = new Color(0.40f, 0.24f, 0.10f),
			ZIndex  = 0,
			Polygon = new Vector2[]
			{
				new Vector2(-10f, -10f), new Vector2(10f, -10f),
				new Vector2(10f,   10f), new Vector2(-10f,  10f),
			},
		};
		AddChild(board);
	}

	private static Vector2[] MakeCircle(float radius, int segments = 12)
	{
		var pts = new Vector2[segments];
		for (int i = 0; i < segments; i++)
		{
			float a = Mathf.Tau * i / segments;
			pts[i] = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
		}
		return pts;
	}

	private void SpawnResultText(string text)
	{
		var lbl = new Label { Text = text };
		lbl.AddThemeColorOverride("font_color", Colors.White);
		lbl.AddThemeFontSizeOverride("font_size", 9);
		lbl.Position = new Vector2(-20f, -26f);
		AddChild(lbl);

		var tween = CreateTween();
		tween.TweenProperty(lbl, "position:y", lbl.Position.Y - 14f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		tween.Parallel().TweenProperty(lbl, "modulate:a", 0f, 0.8f)
			.SetDelay(0.4f).SetTrans(Tween.TransitionType.Quad);
		tween.TweenCallback(Callable.From(lbl.QueueFree));
	}
}
