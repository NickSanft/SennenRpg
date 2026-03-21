using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A readable sign or notice board.
/// Press interact to open a text popup; press interact/cancel again to close.
/// </summary>
public partial class InteractSign : Area2D, IInteractable
{
	[Export] public string   SignTitle { get; set; } = "";
	[Export] public string[] Lines     { get; set; } = [];

	private InteractPromptBubble? _prompt;
	private bool                  _reading = false;

	public override void _Ready()
	{
		AddToGroup("interactable");

		// Placeholder sign post visual
		var post = new Polygon2D { Color = new Color(0.5f, 0.32f, 0.1f) };
		post.Polygon = [new Vector2(-2, -4), new Vector2(2, -4), new Vector2(2, 10), new Vector2(-2, 10)];
		AddChild(post);

		var board = new Polygon2D { Color = new Color(0.7f, 0.5f, 0.2f) };
		board.Polygon = [new Vector2(-10, -14), new Vector2(10, -14), new Vector2(10, -4), new Vector2(-10, -4)];
		AddChild(board);

		_prompt = new InteractPromptBubble("[Z] Read");
		_prompt.Position = new Vector2(0, -26);
		AddChild(_prompt);
	}

	public void ShowPrompt() => _prompt?.ShowBubble();
	public void HidePrompt() => _prompt?.HideBubble();
	public string GetInteractPrompt() => "Read";

	public void Interact(Node player)
	{
		if (_reading || Lines.Length == 0) return;
		_reading = true;
		HidePrompt();
		GameManager.Instance.SetState(GameState.Dialog);

		var popup = new SignReaderPopup(SignTitle, Lines);
		popup.Closed += OnClosed;
		GetTree().CurrentScene.AddChild(popup);
	}

	private void OnClosed()
	{
		_reading = false;
		GameManager.Instance.SetState(GameState.Overworld);
		ShowPrompt();
	}
}
