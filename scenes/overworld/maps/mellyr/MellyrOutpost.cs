using System.Collections.Generic;
using System.Linq;
using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Mellyr Outpost — a small town west of the MAPP Tavern on the world map.
/// Rain and Lily can be hired as residents via Rork's residency shop.
/// When residents are active, they generate passive rewards for the player
/// every 10 steps on qualifying maps; collected here by talking to them.
/// </summary>
public partial class MellyrOutpost : OverworldBase
{
	private const string BgmTrack     = "res://assets/music/Mellyr Outpost.wav";
	private const string RainScene     = "res://scenes/overworld/objects/npcs/NpcRain.tscn";
	private const string LilyScene     = "res://scenes/overworld/objects/npcs/NpcLily.tscn";
	private const string BhataScene     = "res://scenes/overworld/objects/npcs/NpcBhata.tscn";
	private const string KrioraScene    = "res://scenes/overworld/objects/npcs/NpcKriora.tscn";
	private const string RainTimeline   = "res://dialog/timelines/npc_rain_town.dtl";
	private const string LilyTimeline   = "res://dialog/timelines/npc_lily_town.dtl";
	private const string BhataTimeline  = "res://dialog/timelines/npc_bhata_town.dtl";
	private const string KrioraTimeline = "res://dialog/timelines/npc_kriora_town.dtl";
	private const string RainCharPath   = "res://dialog/characters/Rain.dch";
	private const string LilyCharPath   = "res://dialog/characters/Lily.dch";
	private const string BhataCharPath  = "res://dialog/characters/Bhata.dch";
	private const string KrioraCharPath = "res://dialog/characters/Kriora.dch";
	private const string CollectRain    = "collect:rain";
	private const string CollectLily    = "collect:lily";
	private const string CollectBhata   = "collect:bhata";
	private const string CollectKriora  = "collect:kriora";

	public override void _Ready()
	{
		MapId   = "mellyr_outpost";
		BgmPath = BgmTrack;

		SpawnPoints["from_world_map"] = new Vector2(160, 185);
		DefaultSpawnPosition          = new Vector2(160, 185);

		base._Ready();

		// Paint ground tiles for the walkable outpost area
		EnsureGroundTiles();
		FillUnpaintedTilesAsWalls();

		// Spawn residents dynamically — avoids Godot inherited-scene parent_id_path issues
		if (GameManager.Instance.GetFlag(Flags.NpcRainPurchased))
			SpawnResident(RainScene, "rain_town", RainTimeline, RainCharPath, new Vector2(128, 112));

		if (GameManager.Instance.GetFlag(Flags.NpcLilyPurchased))
			SpawnResident(LilyScene, "lily_town", LilyTimeline, LilyCharPath, new Vector2(192, 112));

		if (GameManager.Instance.GetFlag(Flags.NpcBhataPurchased))
			SpawnResident(BhataScene, "bhata_town", BhataTimeline, BhataCharPath, new Vector2(160, 96));

		if (GameManager.Instance.GetFlag(Flags.NpcKrioraPurchased))
			SpawnResident(KrioraScene, "kriora_town", KrioraTimeline, KrioraCharPath, new Vector2(128, 96));

		DialogicBridge.Instance.DialogicSignalReceived += OnDialogicSignal;

		// Rork intro cutscene on first visit
		if (!GameManager.Instance.GetFlag("rork_mellyr_intro"))
			Callable.From(PlayRorkIntroCutscene).CallDeferred();
	}

	private async void PlayRorkIntroCutscene()
	{
		var player = new CutscenePlayer();
		AddChild(player);

		var playerNode = GetTree().GetFirstNodeInGroup("player") as Node2D;
		var playerPos = playerNode?.GlobalPosition ?? DefaultSpawnPosition;

		// Rork starts at (160, 128), player at ~(160, 185)
		// Walk Rork down toward the player, then dialog, then walk back
		float meetY = playerPos.Y - 20f; // Stop just above the player

		await player.Play(new List<CutsceneStep>
		{
			CutsceneStep.ShowLetterbox(0.4f),
			CutsceneStep.Pause(0.3f),
			CutsceneStep.WalkNpc("rork_town", 160f, meetY, 50f),
			CutsceneStep.Pause(0.2f),
			CutsceneStep.NameCard("Rork \u2014 Barkeep", 1.5f),
			CutsceneStep.HideNameCard(0.5f),
			CutsceneStep.Dialog("res://dialog/timelines/rork_mellyr_intro.dtl"),
			CutsceneStep.WaitDialog(),
			CutsceneStep.WalkNpc("rork_town", 160f, 128f, 50f),
			CutsceneStep.HideLetterbox(0.4f),
			CutsceneStep.Flag("rork_mellyr_intro"),
		});

		player.QueueFree();
	}

	public override void _ExitTree()
	{
		if (DialogicBridge.Instance != null)
			DialogicBridge.Instance.DialogicSignalReceived -= OnDialogicSignal;
	}

	// ── Resident spawning ──────────────────────────────────────────────────────

	private void SpawnResident(string scenePath, string npcId, string timeline, string charPath, Vector2 pos)
	{
		if (!ResourceLoader.Exists(scenePath)) return;
		var npc = GD.Load<PackedScene>(scenePath).Instantiate<Npc>();
		npc.NpcId        = npcId;
		npc.TimelinePath = timeline;
		npc.CharacterPath = charPath;
		npc.Position     = pos;
		YSort.AddChild(npc);
	}

	/// <summary>
	/// Paint ground tiles to define the walkable outpost area.
	/// Mellyr Outpost has no pre-painted ground tiles in the .tscn,
	/// so we create them here to give FillUnpaintedTilesAsWalls a boundary.
	/// </summary>
	private void EnsureGroundTiles()
	{
		var groundLayer = GetNodeOrNull<TileMapLayer>("Ground/Ground") ?? GroundLayer;
		if (groundLayer == null) return;

		// Ensure tileset is available
		const string stdPath = "res://resources/tilesets/sennen_tiles.tres";
		if (groundLayer.TileSet == null && ResourceLoader.Exists(stdPath))
			groundLayer.TileSet = GD.Load<TileSet>(stdPath);
		if (groundLayer.TileSet == null) return;

		// Define walkable area in tile coords (16px tiles)
		// Spawn at (160,185) = tile (10,11), Rork at (160,128) = tile (10,8), Exit at (160,200) = tile (10,12)
		// Area: roughly tiles (5,5) to (15,13) covering the outpost
		var floorAtlas = new Vector2I(0, 6); // ground tile
		for (int x = 5; x <= 15; x++)
			for (int y = 5; y <= 13; y++)
				if (groundLayer.GetCellSourceId(new Vector2I(x, y)) < 0)
					groundLayer.SetCell(new Vector2I(x, y), 0, floorAtlas);
	}

	// ── Dialogic signal dispatch ───────────────────────────────────────────────

	private void OnDialogicSignal(Variant arg)
	{
		switch (arg.AsString())
		{
			case CollectRain:  OnCollectRain();  break;
			case CollectLily:  OnCollectLily();  break;
			case CollectBhata:  OnCollectBhata();  break;
			case CollectKriora: OnCollectKriora(); break;
		}
	}

	private void OnCollectRain()
	{
		int gold = GameManager.Instance.CollectRainRewards();
		string msg = gold > 0
			? $"Rain gave you {gold}G from his... endeavors."
			: "Rain hasn't collected any gold yet — come back after some exploring!";
		DialogicBridge.Instance.SetVariable("reward_message", msg);
		DialogicBridge.Instance.StartTimeline("res://dialog/timelines/mellyr_reward.dtl");
	}

	private void OnCollectLily()
	{
		var items = GameManager.Instance.CollectLilyRewards();
		string msg = items.Count == 0
			? "Lily hasn't finished anything yet — come back after some exploring!"
			: $"Lily forged {string.Join(", ", items.Select(i => i.DisplayName))} at the outpost!";
		DialogicBridge.Instance.SetVariable("reward_message", msg);
		DialogicBridge.Instance.StartTimeline("res://dialog/timelines/mellyr_reward.dtl");
	}

	private void OnCollectBhata()
	{
		int count = GameManager.Instance.CollectBhataRewards();
		string msg = count == 0
			? "Bhata hasn't brewed anything yet — come back after some exploring!"
			: $"Bhata handed you {count} Bugman's Ale{(count == 1 ? "" : "s")}!";
		DialogicBridge.Instance.SetVariable("reward_message", msg);
		DialogicBridge.Instance.StartTimeline("res://dialog/timelines/mellyr_reward.dtl");
	}

	private void OnCollectKriora()
	{
		var items = GameManager.Instance.CollectKrioraRewards();
		string msg = items.Count == 0
			? "Kriora hasn't found anything yet — come back after some exploring!"
			: $"Kriora found {string.Join(", ", items.Select(i => i.DisplayName))} in the crystal veins!";
		DialogicBridge.Instance.SetVariable("reward_message", msg);
		DialogicBridge.Instance.StartTimeline("res://dialog/timelines/mellyr_reward.dtl");
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private void ShowCollectNotice(string message)
	{
		GD.Print($"[MellyrOutpost] {message}");

		var canvas = new CanvasLayer { Layer = 60 };
		AddChild(canvas);

		var centerer = new CenterContainer
		{
			AnchorRight = 1f, AnchorBottom = 1f,
		};
		canvas.AddChild(centerer);

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(200f, 0f),
		};
		UiTheme.ApplyPanelTheme(panel);
		centerer.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		panel.AddChild(margin);

		var lbl = new Label
		{
			Text                = message,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode        = TextServer.AutowrapMode.WordSmart,
		};
		lbl.AddThemeFontSizeOverride("font_size", 8);
		lbl.AddThemeColorOverride("font_color", UiTheme.Gold);
		margin.AddChild(lbl);

		// Fade in, hold, fade out
		panel.Modulate = Colors.Transparent;
		var tween = CreateTween();
		tween.TweenProperty(panel, "modulate", Colors.White, 0.3f);
		tween.TweenInterval(3.0f);
		tween.TweenProperty(panel, "modulate", Colors.Transparent, 0.5f);
		tween.TweenCallback(Callable.From(() => canvas.QueueFree()));
	}
}
