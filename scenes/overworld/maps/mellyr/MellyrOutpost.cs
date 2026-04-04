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
	private const string RainScene    = "res://scenes/overworld/objects/npcs/NpcRain.tscn";
	private const string LilyScene    = "res://scenes/overworld/objects/npcs/NpcLily.tscn";
	private const string RainTimeline = "res://dialog/timelines/npc_rain_town.dtl";
	private const string LilyTimeline = "res://dialog/timelines/npc_lily_town.dtl";
	private const string RainCharPath = "res://dialog/characters/Rain.dch";
	private const string LilyCharPath = "res://dialog/characters/Lily.dch";
	private const string CollectRain  = "collect:rain";
	private const string CollectLily  = "collect:lily";

	public override void _Ready()
	{
		MapId   = "mellyr_outpost";
		BgmPath = BgmTrack;

		SpawnPoints["from_world_map"] = new Vector2(160, 185);
		DefaultSpawnPosition          = new Vector2(160, 185);

		base._Ready();

		// Spawn residents dynamically — avoids Godot inherited-scene parent_id_path issues
		if (GameManager.Instance.GetFlag(Flags.NpcRainPurchased))
			SpawnResident(RainScene, "rain_town", RainTimeline, RainCharPath, new Vector2(128, 112));

		if (GameManager.Instance.GetFlag(Flags.NpcLilyPurchased))
			SpawnResident(LilyScene, "lily_town", LilyTimeline, LilyCharPath, new Vector2(192, 112));

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

	// ── Dialogic signal dispatch ───────────────────────────────────────────────

	private void OnDialogicSignal(Variant arg)
	{
		switch (arg.AsString())
		{
			case CollectRain: OnCollectRain(); break;
			case CollectLily: OnCollectLily(); break;
		}
	}

	private void OnCollectRain()
	{
		int gold = GameManager.Instance.CollectRainRewards();
		ShowCollectNotice(gold > 0
			? $"Rain gave you {gold}G from her water-collecting barrels!"
			: "Rain hasn't collected any gold yet — come back after some exploring!");
	}

	private void OnCollectLily()
	{
		var items = GameManager.Instance.CollectLilyRewards();
		if (items.Count == 0)
		{
			ShowCollectNotice("Lily hasn't finished anything yet — come back after some exploring!");
			return;
		}
		var names = string.Join(", ", items.Select(i => i.DisplayName));
		ShowCollectNotice($"Lily forged {names} at the outpost!");
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private void ShowCollectNotice(string message)
	{
		GD.Print($"[MellyrOutpost] {message}");

		var lbl = new Label();
		lbl.Text                = message;
		lbl.HorizontalAlignment = HorizontalAlignment.Center;
		lbl.AutowrapMode        = TextServer.AutowrapMode.Word;
		lbl.AddThemeFontSizeOverride("font_size", 16);

		var canvas = new CanvasLayer { Layer = 60 };
		canvas.AddChild(lbl);
		AddChild(canvas);

		lbl.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
		lbl.OffsetTop    = -60;
		lbl.OffsetBottom = -20;
		lbl.OffsetLeft   = -200;
		lbl.OffsetRight  = 200;

		var tween = CreateTween();
		tween.TweenInterval(2.5f);
		tween.TweenCallback(Callable.From(() => canvas.QueueFree()));
	}
}
