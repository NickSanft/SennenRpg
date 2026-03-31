using Godot;
using System.Linq;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;
using SennenRpg.Scenes.Hud;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// One-time treasure chest. Opens once, persists the opened state via a save flag.
///
/// Loot priority:
///   1. <see cref="LootTable"/> (weighted/guaranteed entries) if non-empty.
///   2. <see cref="ItemPath"/> legacy single-item field as a fallback.
///
/// Gold is awarded from [<see cref="MinGold"/>, <see cref="MaxGold"/>] in addition to the item.
/// FlagId is auto-derived when left empty.
/// </summary>
[Tool]
public partial class Chest : Area2D, IInteractable
{
	/// <summary>
	/// Weighted loot table. Export as <c>Resource[]</c> per Godot 4 C# rules;
	/// entries are cast with <c>OfType&lt;LootEntry&gt;()</c> at runtime.
	/// When non-empty, replaces the legacy <see cref="ItemPath"/> field.
	/// </summary>
	[Export] public Resource[] LootTable { get; set; } = [];

	/// <summary>Gold bonus awarded alongside any item. Roll is uniform in [MinGold, MaxGold].</summary>
	[Export] public int MinGold { get; set; } = 0;
	[Export] public int MaxGold { get; set; } = 0;

	/// <summary>Legacy single-item path. Used when <see cref="LootTable"/> is empty.</summary>
	[Export] public string ItemPath { get; set; } = "";

	/// <summary>
	/// Unique save flag. If empty, auto-derived from <see cref="ItemPath"/>
	/// (e.g. "item_001.tres" → "chest_item_001") or from <see cref="Name"/>
	/// when using a LootTable.
	/// </summary>
	[Export] public string FlagId { get; set; } = "";

	private InteractPromptBubble? _prompt;
	private Polygon2D?             _lid;
	private bool                   _opened;

	private bool HasLootTable => LootTable.OfType<LootEntry>().Any();

	private string Flag
	{
		get
		{
			if (!string.IsNullOrEmpty(FlagId)) return FlagId;
			if (!string.IsNullOrEmpty(ItemPath))
				return "chest_" + System.IO.Path.GetFileNameWithoutExtension(ItemPath).ToLowerInvariant();
			// Fall back to node name so each placed chest has a unique flag
			return "chest_" + Name.ToString().ToLowerInvariant();
		}
	}

	public override void _Ready()
	{
		if (GetChildCount() > 1) return; // CollisionShape2D is pre-baked in the .tscn

		if (!Engine.IsEditorHint())
			AddToGroup("interactable");

		bool hasContent = HasLootTable || !string.IsNullOrEmpty(ItemPath) || MaxGold > 0;
		_opened = !Engine.IsEditorHint() && hasContent && GameManager.Instance.GetFlag(Flag);

		BuildVisual();

		if (!_opened && hasContent)
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
		bool hasContent = HasLootTable || !string.IsNullOrEmpty(ItemPath) || MaxGold > 0;
		if (_opened || !hasContent) return;

		_opened = true;
		GameManager.Instance.SetFlag(Flag, true);

		HidePrompt();
		_prompt?.QueueFree();
		_prompt = null;
		AnimateOpen();

		// ── Roll loot ────────────────────────────────────────────────────────
		string? awardedItemPath = RollItemPath();
		string toastMessage;

		if (!string.IsNullOrEmpty(awardedItemPath))
		{
			GameManager.Instance.AddItem(awardedItemPath);
			var item = ResourceLoader.Exists(awardedItemPath)
				? ResourceLoader.Load<ItemData>(awardedItemPath) : null;
			toastMessage = item != null ? $"Found {item.DisplayName}!" : "Found an item!";
		}
		else
		{
			toastMessage = "The chest was empty!";
		}

		// ── Gold reward ───────────────────────────────────────────────────────
		if (MaxGold > 0)
		{
			int gold = (int)GD.RandRange(MinGold, MaxGold);
			if (gold > 0)
			{
				GameManager.Instance.AddGold(gold);
				toastMessage = string.IsNullOrEmpty(awardedItemPath)
					? $"Found {gold} gold!"
					: toastMessage + $"  (+{gold}g)";
			}
		}

		SpawnToast(toastMessage);
	}

	// ── Loot selection ────────────────────────────────────────────────────────

	private string? RollItemPath()
	{
		var entries = LootTable.OfType<LootEntry>().ToArray();
		if (entries.Length > 0)
		{
			string[]  paths  = entries.Select(e => e.ItemPath).ToArray();
			int[]     wts    = entries.Select(e => e.Weight).ToArray();
			bool[]    guars  = entries.Select(e => e.Guaranteed).ToArray();
			return LootLogic.RollLoot(paths, wts, guars, () => GD.Randf());
		}
		// Legacy fallback
		return string.IsNullOrEmpty(ItemPath) ? null : ItemPath;
	}

	// ── Visuals ────────────────────────────────────────────────────────────────

	private void BuildVisual()
	{
		var brown   = _opened ? new Color(0.35f, 0.20f, 0.08f) : new Color(0.55f, 0.33f, 0.12f);
		var goldTrim = new Color(0.80f, 0.65f, 0.15f);

		var body = new Polygon2D { Color = brown };
		body.Polygon = [
			new Vector2(-8, -4), new Vector2(8, -4),
			new Vector2(8,  6),  new Vector2(-8, 6)
		];
		AddChild(body);

		_lid = new Polygon2D { Color = brown };
		_lid.Polygon = [
			new Vector2(-8, -9), new Vector2(8, -9),
			new Vector2(8, -4),  new Vector2(-8, -4)
		];
		if (_opened) _lid.Position = new Vector2(0, -8);
		AddChild(_lid);

		var trim = new Polygon2D { Color = goldTrim };
		trim.Polygon = [
			new Vector2(-8, -5), new Vector2(8, -5),
			new Vector2(8, -3),  new Vector2(-8, -3)
		];
		AddChild(trim);

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
