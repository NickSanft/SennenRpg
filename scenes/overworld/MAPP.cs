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

		PulseFlame(GetNode<ColorRect>("Flame"));
		ConfigureAltDialogs();
		SpawnExit();
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
