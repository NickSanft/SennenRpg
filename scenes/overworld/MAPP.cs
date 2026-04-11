using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Hud;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// The MAPP tavern — a lively indoor space with seven regulars.
/// NPCs are placed directly in MAPP.tscn (visible in the editor).
/// This script handles the map config, flame animation, the exit trigger,
/// and the magical horse event triggered by Brix's alt dialog.
/// </summary>
[Tool]
public partial class MAPP : OverworldBase
{
	private const string HorseScene         = "res://scenes/overworld/objects/npcs/NpcHorse.tscn";
	private const string HorseTimeline      = "res://dialog/timelines/npc_horse.dtl";
	private const string FalafelScene       = "res://scenes/overworld/objects/npcs/NpcFalafel.tscn";
	private const string FalafelTimeline    = "res://dialog/timelines/npc_falafel.dtl";
	private const string BrixHorseSignal      = "brix_horse_spawn";
	private const string LilyAltSignal        = "lily_alt_ended";
	private const string BhataFalafelSignal   = "bhata_falafel_spawn";
	private const string KrioraCrystalsSignal = "kriora_crystals_spawn";
	private const string GusTransformSignal   = "gus_frog_transform";
	private const string ShizuAuraSignal      = "shizu_music_aura";
	private const string RainAltSignal        = "rain_alt_ended";
	private const string LilyCutscenePath     = "res://dialog/timelines/cutscene_lily_effect.dtl";
	private const string ShizuBgmPath         = "res://assets/music/Origins Of The Gyre.wav";
	private const int    RainStolenGold       = 10;

	private CanvasLayer? _nauseaLayer;
	private const string FrogTexturePath      = "res://assets/sprites/npcs/GusGiantFrog.png";
	private const string TilesetPath          = "res://assets/tilesets/sennen_tiles.png";

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			BuildEditorVisuals();
			return;
		}

		MapId   = "mapp_tavern";
		BgmPath = "res://assets/music/Carillion Forest.wav";

		// If Shizu's aura already fired, switch BGM before base._Ready() plays the track
		if (GameManager.Instance.GetFlag(Flags.ShizuMusicAuraActive))
			BgmPath = ShizuBgmPath;

		// Player returns to the overworld through the south door
		SpawnPoints["from_mapp_exit"] = new Vector2(0, 120);
		// Player returns from the garden through the east-wall back door
		SpawnPoints["from_garden"]    = new Vector2(134f, 70f);
		DefaultSpawnPosition = new Vector2(0, 80);

		base._Ready();

		BuildTileMap();
		SpawnWallColliders();
		SpawnCeilingBeams();
		SpawnRugs();
		SpawnWallDecorations();
		FlickerCandleLights();

		// Animate candle flames placed by scene-instanced TableFurniture nodes
		foreach (Polygon2D flame in GetTree().GetNodesInGroup("candle_flame"))
			AnimateCandleFlame(flame);

		SpawnStaircase();
		ApplyFirelightTints();

		// Restore the horse on re-entry if it has already appeared
		if (GameManager.Instance.GetFlag(Flags.BrixHorseAppeared))
			InstantiateHorse(HorseWorldPosition(), withPoof: false);

		// Restore Falafel on re-entry if it has already appeared
		if (GameManager.Instance.GetFlag(Flags.BhataFalafelAppeared))
			InstantiateFalafel(FalafelWorldPosition(), withPoof: false);

		// Restore Kriora crystals on re-entry (skip if Arises cutscene already played)
		if (GameManager.Instance.GetFlag(Flags.KrioraCrystalsAppeared)
			&& !GameManager.Instance.GetFlag(Flags.AllAltDialogsDone))
			SpawnKrioraCrystals(KrioraWorldPosition(), withPoof: false);

		// Restore Gus frog on re-entry
		if (GameManager.Instance.GetFlag(Flags.GusTransformedToFrog))
			TransformGusToFrog(withPoof: false);

		// Restore Shizu music notes on re-entry
		if (GameManager.Instance.GetFlag(Flags.ShizuMusicAuraActive))
			SpawnMusicNoteAura(ShizuWorldPosition());

		SpawnAllIdleWanders();

		// Listen for the custom signal fired at the end of npc_brix_again.dtl
		DialogicBridge.Instance.DialogicSignalReceived += OnDialogicSignal;

		SpawnEntryFade();

		// Re-run wall fill AFTER all code-built tiles (MappTiles) are placed
		FillUnpaintedTilesAsWalls();
	}

	// ── Editor preview ─────────────────────────────────────────────────────────

	/// <summary>
	/// Builds only the static visual elements so the scene looks correct in the
	/// Godot editor. Skips all autoload access and runtime animations.
	/// </summary>
	private void BuildEditorVisuals()
	{
		// Guard: OverworldBase already contains a "Ground" TileMapLayer, so we cannot
		// use `child is TileMapLayer` — that always matches. Instead we track the
		// MAPP-specific layer by name.
		if (GetNodeOrNull("MappTiles") != null) return;

		BuildTileMap();
		SpawnCeilingBeams();
		SpawnRugs();
		SpawnWallDecorations();
		SpawnStaircase();
	}

	public override void _ExitTree()
	{
		if (DialogicBridge.Instance != null)
			DialogicBridge.Instance.DialogicSignalReceived -= OnDialogicSignal;
	}

	// ── Signal dispatch ────────────────────────────────────────────────────────

	private void OnDialogicSignal(Variant arg)
	{
		switch (arg.AsString())
		{
			case BrixHorseSignal:      OnBrixHorseSignal();      break;
			case LilyAltSignal:        OnLilyAltSignal();        break;
			case BhataFalafelSignal:   OnBhataFalafelSignal();    break;
			case KrioraCrystalsSignal: OnKrioraCrystalsSignal();  break;
			case GusTransformSignal:   OnGusTransformSignal();    break;
			case ShizuAuraSignal:      OnShizuAuraSignal();       break;
			case RainAltSignal:        OnRainAltSignal();         break;
		}
	}

}
