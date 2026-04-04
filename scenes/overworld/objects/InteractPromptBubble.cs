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
	private readonly string _action;
	private readonly string _label;
	private Label?  _labelNode;
	private CanvasLayer _canvas  = null!;
	private Node2D      _screenNode = null!;
	private Node2D      _bobNode    = null!;
	private Tween?      _tween;

	public InteractPromptBubble() : this("interact", "Talk") { }
	/// <summary>Create with an action name and label, e.g. ("interact", "Talk").</summary>
	public InteractPromptBubble(string action, string label) { _action = action; _label = label; }
	/// <summary>Legacy: parse "[Z] Talk" format into action + label.</summary>
	public InteractPromptBubble(string text)
	{
		// Parse "[Z] Talk" → action "interact", label "Talk"
		if (text.Contains(']'))
		{
			_label = text[(text.IndexOf(']') + 2)..].Trim();
			_action = "interact";
		}
		else
		{
			_label = text;
			_action = "interact";
		}
	}

	public override void _Ready()
	{
		_canvas     = new CanvasLayer { Layer = 0 };
		_screenNode = new Node2D { Visible = false };
		_bobNode    = new Node2D();

		var font = Core.Data.UiTheme.LoadPixelFont();
		_labelNode = new Label
		{
			Text                = Core.Extensions.InputMapExtensions.HintFor(_action, _label),
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize   = new Vector2(60, 0),
			Position            = new Vector2(-30, 0),
			LabelSettings       = new LabelSettings
			{
				Font         = font,
				FontSize     = 8,
				FontColor    = Core.Data.UiTheme.Gold,
				OutlineSize  = 2,
				OutlineColor = new Color(0f, 0f, 0f, 1f),
			},
		};

		var panel = new PanelContainer();
		var panelStyle = new StyleBoxFlat
		{
			BgColor = Core.Data.UiTheme.PanelBg with { A = 0.85f },
			ContentMarginLeft = 6, ContentMarginRight = 6,
			ContentMarginTop = 2, ContentMarginBottom = 2,
			CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
			CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
			BorderWidthLeft = 1, BorderWidthRight = 1,
			BorderWidthTop = 1, BorderWidthBottom = 1,
			BorderColor = Core.Data.UiTheme.PanelBorder,
		};
		panel.AddThemeStyleboxOverride("panel", panelStyle);
		panel.Position = new Vector2(-40, -2);
		// Reset label position since it's now inside the panel
		_labelNode.Position = Vector2.Zero;
		_labelNode.CustomMinimumSize = new Vector2(0, 0);
		panel.AddChild(_labelNode);
		_bobNode.AddChild(panel);
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
		// Update text based on current input device (keyboard vs gamepad)
		if (_labelNode != null)
			_labelNode.Text = Core.Extensions.InputMapExtensions.HintFor(_action, _label);
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
