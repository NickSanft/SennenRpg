using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// The MAPP tavern — a lively indoor space with seven regulars.
/// NPCs are placed directly in MAPP.tscn (visible in the editor).
/// This script handles the map config, flame animation, the exit trigger,
/// and the magical horse event triggered by Brix's alt dialog.
/// </summary>
public partial class MAPP : OverworldBase
{
	private const string MapExitScene       = "res://scenes/overworld/objects/MapExit.tscn";
	private const string HorseScene         = "res://scenes/overworld/objects/npcs/NpcHorse.tscn";
	private const string HorseTimeline      = "res://dialog/timelines/npc_horse.dtl";
	private const string FerretScene        = "res://scenes/overworld/objects/npcs/NpcFerret.tscn";
	private const string FerretTimeline     = "res://dialog/timelines/npc_ferret.dtl";
	private const string BrixHorseSignal    = "brix_horse_spawn";
	private const string LilyAltSignal      = "lily_alt_ended";
	private const string BhataFerretSignal  = "bhata_ferret_spawn";
	private const string LilyCutscenePath   = "res://dialog/timelines/cutscene_lily_effect.dtl";

	public override void _Ready()
	{
		MapId   = "mapp_tavern";
		BgmPath = "res://assets/music/Divora - New Beginnings - DND 4 - 02 Carillion Forest.wav";

		// Player returns to TestRoom through the south door
		SpawnPoints["from_mapp_exit"] = new Vector2(0, 120);
		DefaultSpawnPosition = new Vector2(0, 80);

		base._Ready();

		PulseFlame(GetNode<ColorRect>("Flame"));
		SpawnExit();

		// Restore the horse on re-entry if it has already appeared
		if (GameManager.Instance.GetFlag(Flags.BrixHorseAppeared))
			InstantiateHorse(HorseWorldPosition(), withPoof: false);

		// Restore the ferret on re-entry if it has already appeared
		if (GameManager.Instance.GetFlag(Flags.BhataFerretAppeared))
			InstantiateFerret(FerretWorldPosition(), withPoof: false);

		// Listen for the custom signal fired at the end of npc_brix_again.dtl
		DialogicBridge.Instance.DialogicSignalReceived += OnDialogicSignal;
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
			case BrixHorseSignal:   OnBrixHorseSignal();   break;
			case LilyAltSignal:     OnLilyAltSignal();     break;
			case BhataFerretSignal: OnBhataFerretSignal(); break;
		}
	}

	// ── Horse event ────────────────────────────────────────────────────────────

	private void OnBrixHorseSignal()
	{
		if (GameManager.Instance.GetFlag(Flags.BrixHorseAppeared)) return;
		GameManager.Instance.SetFlag(Flags.BrixHorseAppeared, true);

		// Wait for the dialog box to close before materialising the horse
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnBrixAltEnded));
	}

	private void OnBrixAltEnded()
	{
		var pos = HorseWorldPosition();
		SpawnPoof(pos);
		// Short delay so the poof flash leads the horse pop-in
		GetTree().CreateTimer(0.2f).Connect("timeout",
			Callable.From(() => InstantiateHorse(pos, withPoof: true)));
	}

	// ── Lily cutscene ──────────────────────────────────────────────────────────

	private void OnLilyAltSignal()
	{
		// Wait for Lily's dialog box to close, then play the player's inner monologue
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnLilyAltEnded));
	}

	private void OnLilyAltEnded()
	{
		GameManager.Instance.SetState(GameState.Dialog);
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnLilyCutsceneEnded));
		DialogicBridge.Instance.StartTimeline(LilyCutscenePath);
	}

	private void OnLilyCutsceneEnded()
	{
		GameManager.Instance.SetState(GameState.Overworld);
	}

	// ── Ferret event ───────────────────────────────────────────────────────────

	private void OnBhataFerretSignal()
	{
		if (GameManager.Instance.GetFlag(Flags.BhataFerretAppeared)) return;
		GameManager.Instance.SetFlag(Flags.BhataFerretAppeared, true);

		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnBhataAltEnded));
	}

	private void OnBhataAltEnded()
	{
		var pos = FerretWorldPosition();
		SpawnPurplePoof(pos);
		GetTree().CreateTimer(0.2f).Connect("timeout",
			Callable.From(() => InstantiateFerret(pos, withPoof: true)));
	}

	private Vector2 FerretWorldPosition()
	{
		var bhata = YSort.GetNodeOrNull<Npc>("Bhata");
		return (bhata?.GlobalPosition ?? new Vector2(-60f, 20f)) + new Vector2(-28f, -12f);
	}

	private void InstantiateFerret(Vector2 globalPos, bool withPoof)
	{
		var npcScene = GD.Load<PackedScene>(FerretScene);
		var ferret   = npcScene.Instantiate<Npc>();

		ferret.NpcId         = "mapp_ferret";
		ferret.DisplayName   = "Ferret";
		ferret.TimelinePath  = FerretTimeline;
		ferret.CharacterPath = "res://dialog/characters/Ferret.dch";
		ferret.DefaultFacing = FacingDirection.Side;

		YSort.AddChild(ferret);
		ferret.GlobalPosition = globalPos;

		// Floating bob: oscillate y endlessly
		float baseY = ferret.Position.Y;
		var bobTween = CreateTween().SetLoops();
		bobTween.TweenProperty(ferret, "position:y", baseY - 6f, 0.9f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		bobTween.TweenProperty(ferret, "position:y", baseY, 0.9f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

		// Purple energy orbs orbiting the ferret
		SpawnEnergyAura(ferret);

		if (withPoof)
		{
			ferret.Scale = Vector2.Zero;
			var tween = CreateTween();
			tween.TweenProperty(ferret, "scale", Vector2.One, 0.35f)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
		}
	}

	private void SpawnEnergyAura(Node2D parent)
	{
		var orbColors = new Color[]
		{
			new Color(0.70f, 0.10f, 1.00f), // deep violet
			new Color(0.85f, 0.30f, 1.00f), // bright purple
			new Color(0.55f, 0.05f, 0.85f), // dark magenta
		};

		int orbCount = 6;
		float radius = 14f;

		for (int i = 0; i < orbCount; i++)
		{
			float startAngle = (Mathf.Tau / orbCount) * i;

			// Build a tiny diamond polygon
			var orb = new Polygon2D
			{
				Color    = orbColors[i % orbColors.Length],
				ZIndex   = 10,
				Polygon  = new Vector2[]
				{
					new Vector2( 0f, -3f),
					new Vector2( 2f,  0f),
					new Vector2( 0f,  3f),
					new Vector2(-2f,  0f),
				},
			};
			parent.AddChild(orb);

			// Position the orb at its starting angle
			orb.Position = new Vector2(
				Mathf.Cos(startAngle) * radius,
				Mathf.Sin(startAngle) * radius * 0.45f); // flatten vertically

			// Orbit: 16-step linear approximation of a circle
			int steps = 16;
			var orbTween = CreateTween().SetLoops();
			for (int s = 1; s <= steps; s++)
			{
				float angle = startAngle + (Mathf.Tau / steps) * s;
				var target  = new Vector2(
					Mathf.Cos(angle) * radius,
					Mathf.Sin(angle) * radius * 0.45f);
				orbTween.TweenProperty(orb, "position", target, 2.0f / steps)
					.SetTrans(Tween.TransitionType.Linear);
			}

			// Alpha pulse
			var pulseTween = CreateTween().SetLoops();
			float alphaOffset = (float)i / orbCount;
			pulseTween.TweenProperty(orb, "modulate:a", 0.4f, 0.6f + alphaOffset * 0.3f)
				.SetTrans(Tween.TransitionType.Sine);
			pulseTween.TweenProperty(orb, "modulate:a", 1.0f, 0.6f + alphaOffset * 0.3f)
				.SetTrans(Tween.TransitionType.Sine);
		}
	}

	/// <summary>Returns a world position just to the right of Brix.</summary>
	private Vector2 HorseWorldPosition()
	{
		var brix = YSort.GetNodeOrNull<Npc>("Brix");
		return (brix?.GlobalPosition ?? new Vector2(100f, 20f)) + new Vector2(26f, 0f);
	}

	private void InstantiateHorse(Vector2 globalPos, bool withPoof)
	{
		var npcScene = GD.Load<PackedScene>(HorseScene);
		var horse    = npcScene.Instantiate<Npc>();

		horse.NpcId         = "mapp_horse";
		horse.DisplayName   = "Horse";
		horse.TimelinePath  = HorseTimeline;
		horse.CharacterPath = "res://dialog/characters/Horse.dch";
		horse.DefaultFacing = FacingDirection.Side;

		YSort.AddChild(horse);
		horse.GlobalPosition = globalPos;

		if (withPoof)
		{
			horse.Scale = Vector2.Zero;
			var tween = CreateTween();
			tween.TweenProperty(horse, "scale", Vector2.One, 0.35f)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
		}
	}

	private void SpawnPoof(Vector2 worldPos)
	{
		var colors = new Color[]
		{
			new Color(0.85f, 0.55f, 1.00f), // magic purple
			new Color(0.55f, 0.85f, 1.00f), // sky blue
			new Color(1.00f, 0.90f, 0.45f), // gold
			new Color(1.00f, 1.00f, 1.00f), // white
		};

		var rng = new RandomNumberGenerator();
		rng.Randomize();

		for (int i = 0; i < 16; i++)
		{
			float angle  = rng.RandfRange(0f, Mathf.Tau);
			float radius = rng.RandfRange(6f, 36f);
			float size   = rng.RandfRange(4f, 9f);
			var   offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

			var spark = new ColorRect
			{
				Color  = colors[i % colors.Length],
				Size   = new Vector2(size, size),
				ZIndex = 20,
			};
			AddChild(spark);
			spark.GlobalPosition = worldPos + offset - new Vector2(size * 0.5f, size * 0.5f);

			var tween = CreateTween();
			tween.TweenProperty(spark, "global_position",
				spark.GlobalPosition + offset * 0.5f, 0.45f)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
			tween.Parallel()
				.TweenProperty(spark, "modulate:a", 0f, 0.45f)
				.SetDelay(0.05f)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			tween.TweenCallback(Callable.From(spark.QueueFree));
		}
	}

	private void SpawnPurplePoof(Vector2 worldPos)
	{
		var colors = new Color[]
		{
			new Color(0.70f, 0.10f, 1.00f), // deep violet
			new Color(0.85f, 0.30f, 1.00f), // bright purple
			new Color(0.55f, 0.05f, 0.85f), // dark magenta
			new Color(1.00f, 1.00f, 1.00f), // white flash
		};

		var rng = new RandomNumberGenerator();
		rng.Randomize();

		for (int i = 0; i < 16; i++)
		{
			float angle  = rng.RandfRange(0f, Mathf.Tau);
			float radius = rng.RandfRange(6f, 36f);
			float size   = rng.RandfRange(4f, 9f);
			var   offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

			var spark = new ColorRect
			{
				Color  = colors[i % colors.Length],
				Size   = new Vector2(size, size),
				ZIndex = 20,
			};
			AddChild(spark);
			spark.GlobalPosition = worldPos + offset - new Vector2(size * 0.5f, size * 0.5f);

			var tween = CreateTween();
			tween.TweenProperty(spark, "global_position",
				spark.GlobalPosition + offset * 0.5f, 0.45f)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
			tween.Parallel()
				.TweenProperty(spark, "modulate:a", 0f, 0.45f)
				.SetDelay(0.05f)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			tween.TweenCallback(Callable.From(spark.QueueFree));
		}
	}

	// ── Flame ──────────────────────────────────────────────────────────────────

	private void PulseFlame(ColorRect flame)
	{
		var tween = CreateTween().SetLoops();
		tween.TweenProperty(flame, "modulate:a", 0.6f, 0.4f).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(flame, "modulate:a", 1.0f, 0.4f).SetTrans(Tween.TransitionType.Sine);
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
