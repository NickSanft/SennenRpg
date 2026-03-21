using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;
using SennenRpg.Scenes.Hud;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// One-time treasure chest. Gives the player an item on first open;
/// stays visually open and blocks re-interaction on subsequent visits.
/// FlagId is auto-derived from ItemPath if left empty.
/// </summary>
public partial class Chest : Area2D, IInteractable
{
	[Export] public string ItemPath { get; set; } = "";
	/// <summary>
	/// Unique save flag. If empty, derived from the item filename
	/// (e.g. "res://…/item_001.tres" → "chest_item_001").
	/// </summary>
	[Export] public string FlagId { get; set; } = "";

	private InteractPromptBubble? _prompt;
	private Polygon2D?             _lid;
	private bool                   _opened;

	private string Flag =>
		!string.IsNullOrEmpty(FlagId) ? FlagId
		: "chest_" + System.IO.Path.GetFileNameWithoutExtension(ItemPath).ToLowerInvariant();

	public override void _Ready()
	{
		AddToGroup("interactable");

		_opened = !string.IsNullOrEmpty(ItemPath) && GameManager.Instance.GetFlag(Flag);

		BuildVisual();

		if (!_opened)
		{
			_prompt = new InteractPromptBubble("[Z] Open");
			_prompt.Position = new Vector2(0, -22);
			AddChild(_prompt);
		}
	}

	public void ShowPrompt() => _prompt?.ShowBubble();
	public void HidePrompt() => _prompt?.HideBubble();
	public string GetInteractPrompt() => "Open";

	public void Interact(Node player)
	{
		if (_opened || string.IsNullOrEmpty(ItemPath)) return;
		_opened = true;
		GameManager.Instance.SetFlag(Flag, true);
		GameManager.Instance.AddItem(ItemPath);

		HidePrompt();
		_prompt?.QueueFree();
		_prompt = null;

		AnimateOpen();

		var item = ResourceLoader.Load<ItemData>(ItemPath);
		SpawnToast(item != null ? $"Found {item.DisplayName}!" : "Found an item!");
	}

	// ── Visuals ────────────────────────────────────────────────────────────────

	private void BuildVisual()
	{
		var brown  = _opened ? new Color(0.35f, 0.20f, 0.08f) : new Color(0.55f, 0.33f, 0.12f);
		var goldTrim = new Color(0.80f, 0.65f, 0.15f);

		// Body
		var body = new Polygon2D { Color = brown };
		body.Polygon = [
			new Vector2(-8, -4), new Vector2(8, -4),
			new Vector2(8,  6),  new Vector2(-8, 6)
		];
		AddChild(body);

		// Lid — stored so it can be animated
		_lid = new Polygon2D { Color = brown };
		_lid.Polygon = [
			new Vector2(-8, -9), new Vector2(8, -9),
			new Vector2(8, -4),  new Vector2(-8, -4)
		];
		if (_opened) _lid.Position = new Vector2(0, -8);
		AddChild(_lid);

		// Gold trim strip
		var trim = new Polygon2D { Color = goldTrim };
		trim.Polygon = [
			new Vector2(-8, -5), new Vector2(8, -5),
			new Vector2(8, -3),  new Vector2(-8, -3)
		];
		AddChild(trim);

		// Latch
		var latch = new Polygon2D { Color = goldTrim };
		latch.Polygon = [
			new Vector2(-2, -6), new Vector2(2, -6),
			new Vector2(2, -2),  new Vector2(-2, -2)
		];
		AddChild(latch);
	}

	private void AnimateOpen()
	{
		if (_lid == null) return;
		var tween = CreateTween();
		tween.TweenProperty(_lid, "position:y", -10f, 0.15f)
		     .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
	}

	private void SpawnToast(string message)
	{
		const string path = "res://scenes/hud/AreaNameLabel.tscn";
		if (!ResourceLoader.Exists(path)) return;
		var toast = GD.Load<PackedScene>(path).Instantiate<AreaNameLabel>();
		GetTree().CurrentScene.AddChild(toast);
		toast.Show(message);
	}
}
