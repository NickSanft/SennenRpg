using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// The MAPP tavern — a lively indoor space with seven regulars.
/// Entered from TestRoom; exits back via a door near the south wall.
/// </summary>
public partial class MAPP : OverworldBase
{
	private const string NpcScene    = "res://scenes/overworld/objects/npc.tscn";
	private const string MapExitScene = "res://scenes/overworld/objects/MapExit.tscn";

	public override void _Ready()
	{
		MapId  = "mapp_tavern";
		BgmPath = "";

		// Player returns to TestRoom through the south door
		SpawnPoints["from_mapp_exit"] = new Vector2(0, 120);
		DefaultSpawnPosition = new Vector2(0, 80);

		base._Ready();

		DrawTavernBackground();
		SpawnNpcs();
		SpawnExit();
	}

	// ── Background ────────────────────────────────────────────────────────────

	private void DrawTavernBackground()
	{
		// Floor — warm wooden tone
		var floor = new ColorRect
		{
			Color        = new Color(0.42f, 0.28f, 0.16f),
			Position     = new Vector2(-160, -120),
			Size         = new Vector2(320, 280),
			ZIndex       = -10,
		};
		AddChild(floor);

		// Wall panelling — darker strip along the top
		var wallPanel = new ColorRect
		{
			Color    = new Color(0.25f, 0.14f, 0.08f),
			Position = new Vector2(-160, -120),
			Size     = new Vector2(320, 60),
			ZIndex   = -9,
		};
		AddChild(wallPanel);

		// Bar counter — horizontal slab across the north end
		var bar = new ColorRect
		{
			Color    = new Color(0.32f, 0.20f, 0.10f),
			Position = new Vector2(-120, -60),
			Size     = new Vector2(240, 16),
			ZIndex   = -8,
		};
		AddChild(bar);

		// Bar surface highlight
		var barTop = new ColorRect
		{
			Color    = new Color(0.50f, 0.35f, 0.18f),
			Position = new Vector2(-120, -64),
			Size     = new Vector2(240, 6),
			ZIndex   = -7,
		};
		AddChild(barTop);

		// Fireplace — glowing orange square on the west wall
		var firebox = new ColorRect
		{
			Color    = new Color(0.15f, 0.10f, 0.08f),
			Position = new Vector2(-150, -40),
			Size     = new Vector2(28, 32),
			ZIndex   = -8,
		};
		AddChild(firebox);

		var flame = new ColorRect
		{
			Color    = new Color(1f, 0.55f, 0.1f, 0.9f),
			Position = new Vector2(-146, -36),
			Size     = new Vector2(20, 22),
			ZIndex   = -7,
		};
		AddChild(flame);

		PulseFlame(flame);

		// Two round tables
		AddTable(new Vector2(-60, 40));
		AddTable(new Vector2( 60, 40));
		AddTable(new Vector2(  0, -10));

		// South wall border
		var southWall = new ColorRect
		{
			Color    = new Color(0.20f, 0.12f, 0.06f),
			Position = new Vector2(-160, 155),
			Size     = new Vector2(320, 12),
			ZIndex   = -8,
		};
		AddChild(southWall);

		// Doorway cutout hint (lighter stripe)
		var door = new ColorRect
		{
			Color    = new Color(0.30f, 0.20f, 0.10f),
			Position = new Vector2(-14, 155),
			Size     = new Vector2(28, 12),
			ZIndex   = -7,
		};
		AddChild(door);
	}

	private void AddTable(Vector2 pos)
	{
		var table = new ColorRect
		{
			Color    = new Color(0.35f, 0.22f, 0.12f),
			Position = pos - new Vector2(14, 10),
			Size     = new Vector2(28, 20),
			ZIndex   = -6,
		};
		AddChild(table);
	}

	private void PulseFlame(ColorRect flame)
	{
		var tween = CreateTween().SetLoops();
		tween.TweenProperty(flame, "modulate:a", 0.6f, 0.4f).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(flame, "modulate:a", 1.0f, 0.4f).SetTrans(Tween.TransitionType.Sine);
	}

	// ── NPCs ──────────────────────────────────────────────────────────────────

	private void SpawnNpcs()
	{
		var npcScene = GD.Load<PackedScene>(NpcScene);

		// ── Kriora — bar matron, behind the counter ─────────────────────────
		SpawnNpc(npcScene, new NpcConfig
		{
			Id          = "kriora_mapp",
			Name        = "Kriora",
			Pos         = new Vector2(0, -40),
			Color       = new Color(0.75f, 0.25f, 0.18f),
			Facing      = FacingDirection.Down,
			CharPath    = "res://dialog/characters/Kriora.dch",
			Timeline    = "res://dialog/timelines/npc_kriora.dtl",
			AltFlag     = "talked_to_kriora_mapp",
			AltTimeline = "res://dialog/timelines/npc_kriora_again.dtl",
		});

		// ── Shizu — mysterious traveller, corner seat ───────────────────────
		SpawnNpc(npcScene, new NpcConfig
		{
			Id          = "shizu_mapp",
			Name        = "Shizu",
			Pos         = new Vector2(-120, 60),
			Color       = new Color(0.55f, 0.1f, 0.75f),
			Facing      = FacingDirection.Side,
			CharPath    = "res://dialog/characters/Shizu.dch",
			Timeline    = "res://dialog/timelines/npc_shizu.dtl",
			AltFlag     = "talked_to_shizu_mapp",
			AltTimeline = "res://dialog/timelines/npc_shizu_again.dtl",
		});

		// ── Lily — waitress, moving between tables ──────────────────────────
		SpawnNpc(npcScene, new NpcConfig
		{
			Id           = "lily_mapp",
			Name         = "Lily",
			Pos          = new Vector2(-40, 30),
			Color        = new Color(0.95f, 0.6f, 0.7f),
			Facing       = FacingDirection.Down,
			CharPath     = "res://dialog/characters/Lily.dch",
			Timeline     = "res://dialog/timelines/npc_lily.dtl",
			AltFlag      = "talked_to_lily_mapp",
			AltTimeline  = "res://dialog/timelines/npc_lily_again.dtl",
			PatrolPoints = [new Vector2(-60, 40), new Vector2(60, 40), new Vector2(-40, 30)],
			PatrolSpeed  = 25f,
		});

		// ── Gus — old miner, by the fireplace ──────────────────────────────
		SpawnNpc(npcScene, new NpcConfig
		{
			Id          = "gus_mapp",
			Name        = "Gus",
			Pos         = new Vector2(-110, 20),
			Color       = new Color(0.6f, 0.42f, 0.22f),
			Facing      = FacingDirection.Side,
			CharPath    = "res://dialog/characters/Gus.dch",
			Timeline    = "res://dialog/timelines/npc_gus.dtl",
			AltFlag     = "talked_to_gus_mapp",
			AltTimeline = "res://dialog/timelines/npc_gus_again.dtl",
		});

		// ── Brix — mercenary, alone at east table ───────────────────────────
		SpawnNpc(npcScene, new NpcConfig
		{
			Id          = "brix_mapp",
			Name        = "Brix",
			Pos         = new Vector2(100, 20),
			Color       = new Color(0.4f, 0.42f, 0.46f),
			Facing      = FacingDirection.Side,
			CharPath    = "res://dialog/characters/Brix.dch",
			Timeline    = "res://dialog/timelines/npc_brix.dtl",
			AltFlag     = "talked_to_brix_mapp",
			AltTimeline = "res://dialog/timelines/npc_brix_again.dtl",
		});

		// ── Bhata — scholar, centre table with books ────────────────────────
		SpawnNpc(npcScene, new NpcConfig
		{
			Id          = "bhata_mapp",
			Name        = "Bhata",
			Pos         = new Vector2(30, -10),
			Color       = new Color(0.25f, 0.75f, 0.70f),
			Facing      = FacingDirection.Down,
			CharPath    = "res://dialog/characters/Bhata.dch",
			Timeline    = "res://dialog/timelines/npc_bhata.dtl",
			AltFlag     = "talked_to_bhata_mapp",
			AltTimeline = "res://dialog/timelines/npc_bhata_again.dtl",
		});

		// ── Rain — bard, near the south wall ───────────────────────────────
		SpawnNpc(npcScene, new NpcConfig
		{
			Id           = "rain_mapp",
			Name         = "Rain",
			Pos          = new Vector2(50, 100),
			Color        = new Color(0.35f, 0.55f, 0.95f),
			Facing       = FacingDirection.Up,
			CharPath     = "res://dialog/characters/Rain.dch",
			Timeline     = "res://dialog/timelines/npc_rain.dtl",
			AltFlag      = "talked_to_rain_mapp",
			AltTimeline  = "res://dialog/timelines/npc_rain_again.dtl",
			PatrolPoints = [new Vector2(60, 90), new Vector2(40, 100), new Vector2(50, 100)],
			PatrolSpeed  = 18f,
		});
	}

	private void SpawnNpc(PackedScene scene, NpcConfig cfg)
	{
		var npc = scene.Instantiate<Npc>();
		npc.NpcId            = cfg.Id;
		npc.DisplayName      = cfg.Name;
		npc.PlaceholderColor = cfg.Color;
		npc.DefaultFacing    = cfg.Facing;
		npc.CharacterPath    = cfg.CharPath;
		npc.TimelinePath     = cfg.Timeline;
		npc.PatrolPoints     = cfg.PatrolPoints;
		npc.PatrolSpeed      = cfg.PatrolSpeed;

		if (!string.IsNullOrEmpty(cfg.AltFlag))
		{
			npc.AltDialogOptions =
			[
				new NpcDialogOption
				{
					RequiredFlag = cfg.AltFlag,
					TimelinePath = cfg.AltTimeline,
				}
			];
		}

		YSort.AddChild(npc);
		npc.GlobalPosition = cfg.Pos;
	}

	// ── Exit ──────────────────────────────────────────────────────────────────

	private void SpawnExit()
	{
		var exitScene = GD.Load<PackedScene>(MapExitScene);
		var exit      = exitScene.Instantiate<MapExit>();

		exit.TargetMapPath = "res://scenes/overworld/TestRoom.tscn";
		exit.TargetSpawnId = "from_mapp";
		exit.AutoTrigger   = true;

		AddChild(exit);
		exit.GlobalPosition = new Vector2(0, 168);
	}

	// ── Helper record ─────────────────────────────────────────────────────────

	private record NpcConfig
	{
		public string         Id           { get; init; } = "";
		public string         Name         { get; init; } = "";
		public Vector2        Pos          { get; init; }
		public Color          Color        { get; init; } = new Color(1f, 0.75f, 0.3f);
		public FacingDirection Facing      { get; init; } = FacingDirection.Down;
		public string         CharPath     { get; init; } = "";
		public string         Timeline     { get; init; } = "";
		public string         AltFlag      { get; init; } = "";
		public string         AltTimeline  { get; init; } = "";
		public Vector2[]      PatrolPoints { get; init; } = [];
		public float          PatrolSpeed  { get; init; } = 30f;
	}
}
