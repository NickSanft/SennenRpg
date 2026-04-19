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
		// With canvas_items stretch mode, world-space nodes scale with the viewport.
		// No CanvasLayer needed — just add as children of this Node2D.
		_screenNode = new Node2D { Visible = false };
		_bobNode    = new Node2D();
		_canvas     = null!; // unused but kept for field compatibility

		var font = Core.Data.UiTheme.LoadPixelFont();
		_labelNode = new Label
		{
			Text                = Core.Extensions.InputMapExtensions.HintFor(_action, _label),
			HorizontalAlignment = HorizontalAlignment.Center,
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
			ContentMarginLeft = 4, ContentMarginRight = 4,
			ContentMarginTop = 1, ContentMarginBottom = 1,
			CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
			BorderWidthLeft = 1, BorderWidthRight = 1,
			BorderWidthTop = 1, BorderWidthBottom = 1,
			BorderColor = Core.Data.UiTheme.PanelBorder,
		};
		panel.AddThemeStyleboxOverride("panel", panelStyle);
		panel.Position = new Vector2(-40, -2);
		_labelNode.Position = Vector2.Zero;
		panel.AddChild(_labelNode);
		_bobNode.AddChild(panel);
		_screenNode.AddChild(_bobNode);
		AddChild(_screenNode);
	}

	public override void _ExitTree()
	{
		_tween?.Kill();
		_tween = null;
	}

	public void ShowBubble()
	{
		// First time the player sees a [Z] interact prompt anywhere — explain it.
		Autoloads.TutorialManager.Instance?.Trigger(Core.Data.TutorialIds.InteractPrompt);

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
