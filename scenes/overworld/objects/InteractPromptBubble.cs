using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A small animated prompt that bobs gently up and down.
/// Create via <c>new InteractPromptBubble("text")</c>, position the node where the
/// prompt should sit above the object, then call ShowBubble() / HideBubble().
/// </summary>
public partial class InteractPromptBubble : Node2D
{
	private readonly string _text;
	private Node2D _bobNode = null!;
	private Tween?  _tween;

	public InteractPromptBubble() : this("") { }
	public InteractPromptBubble(string text) { _text = text; }

	public override void _Ready()
	{
		_bobNode = new Node2D();

		var label = new Label
		{
			Text               = _text,
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize  = new Vector2(60, 0),
			Position           = new Vector2(-30, 0),
			LabelSettings      = new LabelSettings
			{
				FontSize     = 11,
				FontColor    = Colors.Yellow,
				OutlineSize  = 2,
				OutlineColor = new Color(0f, 0f, 0f, 0.85f),
			},
		};

		_bobNode.AddChild(label);
		AddChild(_bobNode);
		Visible = false;
	}

	public void ShowBubble()
	{
		Visible = true;
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
		_bobNode.Position = Vector2.Zero;
		Visible = false;
	}
}
