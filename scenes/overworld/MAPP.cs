using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// The MAPP tavern — a lively indoor space with seven regulars.
/// NPCs are placed directly in MAPP.tscn (visible in the editor).
/// This script handles the background visuals, map config, and the exit trigger.
/// </summary>
public partial class MAPP : OverworldBase
{
	private const string MapExitScene = "res://scenes/overworld/objects/MapExit.tscn";

	public override void _Ready()
	{
		MapId   = "mapp_tavern";
		BgmPath = "res://assets/music/Divora - New Beginnings - DND 4 - 02 Carillion Forest.wav";

		// Player returns to TestRoom through the south door
		SpawnPoints["from_mapp_exit"] = new Vector2(0, 120);
		DefaultSpawnPosition = new Vector2(0, 80);

		base._Ready();

		DrawTavernBackground();
		ConfigureAltDialogs();
		SpawnExit();
	}

	// ── Background ────────────────────────────────────────────────────────────

	private void DrawTavernBackground()
	{
		// Floor — warm wooden tone
		var floor = new ColorRect
		{
			Color    = new Color(0.42f, 0.28f, 0.16f),
			Position = new Vector2(-160, -120),
			Size     = new Vector2(320, 280),
			ZIndex   = -10,
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

	// ── Alt dialog ────────────────────────────────────────────────────────────

	/// <summary>
	/// NpcDialogOption is a C# [GlobalClass] and cannot be serialised as a sub_resource
	/// in .tscn files, so we assign AltDialogOptions here after the scene is ready.
	/// </summary>
	private void ConfigureAltDialogs()
	{
		SetAlt("Kriora", "talked_to_kriora_mapp", "res://dialog/timelines/npc_kriora_again.dtl");
		SetAlt("Shizu",  "talked_to_shizu_mapp",  "res://dialog/timelines/npc_shizu_again.dtl");
		SetAlt("Lily",   "talked_to_lily_mapp",   "res://dialog/timelines/npc_lily_again.dtl");
		SetAlt("Gus",    "talked_to_gus_mapp",    "res://dialog/timelines/npc_gus_again.dtl");
		SetAlt("Brix",   "talked_to_brix_mapp",   "res://dialog/timelines/npc_brix_again.dtl");
		SetAlt("Bhata",  "talked_to_bhata_mapp",  "res://dialog/timelines/npc_bhata_again.dtl");
		SetAlt("Rain",   "talked_to_rain_mapp",   "res://dialog/timelines/npc_rain_again.dtl");
	}

	private void SetAlt(string nodeName, string flag, string timeline)
	{
		var npc = YSort.GetNodeOrNull<Npc>(nodeName);
		if (npc == null) { GD.PushWarning($"[MAPP] NPC node '{nodeName}' not found for alt dialog setup."); return; }
		npc.AltDialogOptions = [new NpcDialogOption { RequiredFlag = flag, TimelinePath = timeline }];
	}

	// ── Exit ──────────────────────────────────────────────────────────────────

	private void SpawnExit()
	{
		var exit = GD.Load<PackedScene>(MapExitScene).Instantiate<MapExit>();
		exit.TargetMapPath = "res://scenes/overworld/TestRoom.tscn";
		exit.TargetSpawnId = "from_mapp";
		exit.AutoTrigger   = true;
		AddChild(exit);
		exit.GlobalPosition = new Vector2(0, 168);
	}
}
