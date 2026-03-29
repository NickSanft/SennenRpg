using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A small animated prompt that bobs gently up and down.
/// Create via <c>new InteractPromptBubble("text")</c>, position the node where the
/// prompt should sit above the object, then call ShowBubble() / HideBubble().
///
/// Content is rendered in a CanvasLayer at screen resolution so it is unaffected
/// by camera zoom and always renders as crisply as the dialog text.
/// </summary>
public partial class InteractPromptBubble : Node2D
{
	private readonly string _text;
	private CanvasLayer _canvas  = null!;
	private Node2D      _screenNode = null!; // positioned at screen coords each frame
	private Node2D      _bobNode    = null!; // child of _screenNode; tweened for bob
	private Tween?      _tween;

	public InteractPromptBubble() : this("") { }
	public InteractPromptBubble(string text) { _text = text; }

	public override void _Ready()
	{
		_canvas     = new CanvasLayer { Layer = 0 };
		_screenNode = new Node2D { Visible = false };
		_bobNode    = new Node2D();

		var label = new Label
		{
			Text                = _text,
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize   = new Vector2(60, 0),
			Position            = new Vector2(-30, 0),
			LabelSettings       = new LabelSettings
			{
				FontSize     = 15,
				FontColor    = Colors.Yellow,
				OutlineSize  = 4,
				OutlineColor = new Color(0f, 0f, 0f, 1f),
			},
		};

		var bg = new ColorRect
		{
			Color    = new Color(0f, 0f, 0f, 0.55f),
			Position = new Vector2(-34, -1),
			Size     = new Vector2(68, 20),
			ZIndex   = -1,
		};

		_bobNode.AddChild(bg);
		_bobNode.AddChild(label);
		_screenNode.AddChild(_bobNode);
		_canvas.AddChild(_screenNode);
		GetTree().Root.AddChild(_canvas);
	}

	public override void _Process(double delta)
	{
		if (!_screenNode.Visible) return;
		var raw = GetViewportTransform() * GlobalPosition;
		_screenNode.Position = new Vector2(Mathf.Round(raw.X), Mathf.Round(raw.Y));
	}

	public override void _ExitTree()
	{
		// QueueFree the canvas (which also frees _screenNode and _bobNode as its children).
		// Null all GodotObject fields immediately so Godot's hot-reload serializer does not
		// encounter freed native objects when it runs SaveGodotObjectData this frame.
		_tween?.Kill();
		_tween      = null;
		_canvas?.QueueFree();
		_canvas     = null!;
		_screenNode = null!;
		_bobNode    = null!;
	}

	public void ShowBubble()
	{
		_screenNode.Visible = true;
		if (_tween != null && _tween.IsRunning()) return;
		_tween?.Kill();
		_bobNode.Position = Vector2.Zero;
		_tween = CreateTween().SetLoops();
		_tween.TweenProperty(_bobNode, "position:y", -3f, 0.4f)
		      .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		_tween.TweenProperty(_bobNode, "position:y", 0f, 0.4f)
		      .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	}

	public void HideBubble()
	{
		_tween?.Kill();
		_tween = null;
		_bobNode.Position   = Vector2.Zero;
		_screenNode.Visible = false;
	}
}
