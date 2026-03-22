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
	private const string BrixHorseSignal      = "brix_horse_spawn";
	private const string LilyAltSignal        = "lily_alt_ended";
	private const string BhataFerretSignal    = "bhata_ferret_spawn";
	private const string KrioraCrystalsSignal = "kriora_crystals_spawn";
	private const string GusTransformSignal   = "gus_frog_transform";
	private const string ShizuAuraSignal      = "shizu_music_aura";
	private const string LilyCutscenePath     = "res://dialog/timelines/cutscene_lily_effect.dtl";
	private const string ShizuBgmPath         = "res://assets/music/Divora - Origins Of The Gyre - DND 6 - 01 Origins Of The Gyre - Full.wav";
	private const string FrogTexturePath      = "res://assets/sprites/npcs/GusGiantFrog.png";

	public override void _Ready()
	{
		MapId   = "mapp_tavern";
		BgmPath = "res://assets/music/Divora - New Beginnings - DND 4 - 02 Carillion Forest.wav";

		// If Shizu's aura already fired, switch BGM before base._Ready() plays the track
		if (GameManager.Instance.GetFlag(Flags.ShizuMusicAuraActive))
			BgmPath = ShizuBgmPath;

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

		// Restore Kriora crystals on re-entry
		if (GameManager.Instance.GetFlag(Flags.KrioraCrystalsAppeared))
			SpawnKrioraCrystals(KrioraWorldPosition(), withPoof: false);

		// Restore Gus frog on re-entry
		if (GameManager.Instance.GetFlag(Flags.GusTransformedToFrog))
			TransformGusToFrog(withPoof: false);

		// Restore Shizu music notes on re-entry
		if (GameManager.Instance.GetFlag(Flags.ShizuMusicAuraActive))
			SpawnMusicNoteAura(ShizuWorldPosition());

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
			case BrixHorseSignal:      OnBrixHorseSignal();      break;
			case LilyAltSignal:        OnLilyAltSignal();        break;
			case BhataFerretSignal:    OnBhataFerretSignal();     break;
			case KrioraCrystalsSignal: OnKrioraCrystalsSignal();  break;
			case GusTransformSignal:   OnGusTransformSignal();    break;
			case ShizuAuraSignal:      OnShizuAuraSignal();       break;
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

	// ── Kriora crystal event ───────────────────────────────────────────────────

	private void OnKrioraCrystalsSignal()
	{
		if (GameManager.Instance.GetFlag(Flags.KrioraCrystalsAppeared)) return;
		GameManager.Instance.SetFlag(Flags.KrioraCrystalsAppeared, true);
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnKrioraAltEnded));
	}

	private void OnKrioraAltEnded()
	{
		var pos = KrioraWorldPosition();
		SpawnPoof(pos);
		GetTree().CreateTimer(0.3f).Connect("timeout",
			Callable.From(() => SpawnKrioraCrystals(pos, withPoof: true)));
	}

	private Vector2 KrioraWorldPosition()
	{
		var kriora = YSort.GetNodeOrNull<Npc>("Kriora");
		return kriora?.GlobalPosition ?? new Vector2(-80f, -40f);
	}

	private void SpawnKrioraCrystals(Vector2 worldPos, bool withPoof)
	{
		// Crystal offsets around the NPC's feet
		var offsets = new (Vector2 offset, float height, float angle)[]
		{
			(new Vector2(-10f,  6f),  12f, -15f),
			(new Vector2(  8f,  8f),  10f,  20f),
			(new Vector2(-18f,  4f),   8f, -30f),
			(new Vector2( 16f,  5f),   9f,  10f),
			(new Vector2(  2f,  9f),  14f,   5f),
			(new Vector2(-24f,  7f),   7f, -45f),
			(new Vector2( 22f,  3f),   8f,  35f),
		};

		var crystalColor = new Color(0.35f, 0.80f, 1.00f); // light blue

		foreach (var (offset, height, angleDeg) in offsets)
		{
			float a = Mathf.DegToRad(angleDeg);
			// Elongated diamond: tip up, base down
			var crystal = new Polygon2D
			{
				Color    = crystalColor,
				ZIndex   = 5,
				Rotation = a,
				Polygon  = new Vector2[]
				{
					new Vector2( 0f,        -height),
					new Vector2( height * 0.28f, 0f),
					new Vector2( 0f,         height * 0.35f),
					new Vector2(-height * 0.28f, 0f),
				},
			};
			AddChild(crystal);
			crystal.GlobalPosition = worldPos + offset;

			if (withPoof)
			{
				crystal.Scale = Vector2.Zero;
				var popTween = CreateTween();
				popTween.TweenProperty(crystal, "scale", Vector2.One, 0.3f)
					.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
			}

			// Slow shimmer: alpha pulse
			float delay = GD.Randf() * 0.8f;
			var shimmer = CreateTween().SetLoops();
			shimmer.TweenProperty(crystal, "modulate:a", 0.5f, 1.1f + delay * 0.3f)
				.SetTrans(Tween.TransitionType.Sine).SetDelay(delay);
			shimmer.TweenProperty(crystal, "modulate:a", 1.0f, 1.1f + delay * 0.3f)
				.SetTrans(Tween.TransitionType.Sine);

			// Faint inner highlight (smaller white diamond overlaid)
			var highlight = new Polygon2D
			{
				Color   = new Color(0.85f, 0.97f, 1.0f, 0.55f),
				ZIndex  = 6,
				Polygon = new Vector2[]
				{
					new Vector2( 0f,          -height * 0.55f),
					new Vector2( height * 0.12f, 0f),
					new Vector2( 0f,           height * 0.15f),
					new Vector2(-height * 0.12f, 0f),
				},
			};
			AddChild(highlight);
			highlight.GlobalPosition = crystal.GlobalPosition;
			highlight.Rotation       = a;
		}
	}

	// ── Gus frog event ─────────────────────────────────────────────────────────

	private void OnGusTransformSignal()
	{
		if (GameManager.Instance.GetFlag(Flags.GusTransformedToFrog)) return;
		GameManager.Instance.SetFlag(Flags.GusTransformedToFrog, true);
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnGusAltEnded));
	}

	private void OnGusAltEnded()
	{
		var gus = YSort.GetNodeOrNull<Npc>("Gus");
		if (gus == null) return;
		SpawnPoof(gus.GlobalPosition);
		GetTree().CreateTimer(0.2f).Connect("timeout",
			Callable.From(() => TransformGusToFrog(withPoof: true)));
	}

	private void TransformGusToFrog(bool withPoof)
	{
		var gus    = YSort.GetNodeOrNull<Npc>("Gus");
		var sprite = gus?.GetNodeOrNull<AnimatedSprite2D>("Sprite");
		if (sprite == null) return;

		var tex = GD.Load<Texture2D>(FrogTexturePath);
		var frames = new SpriteFrames();

		foreach (var animName in new[] { "idle_down", "idle_side", "idle_up" })
		{
			frames.AddAnimation(animName);
			frames.SetAnimationLoop(animName, true);
			frames.SetAnimationSpeed(animName, 3.0f);

			for (int f = 0; f < 2; f++)
			{
				var atlas = new AtlasTexture
				{
					Atlas  = tex,
					Region = new Rect2(f * 32, 0, 32, 32),
				};
				frames.AddFrame(animName, atlas);
			}
		}

		sprite.SpriteFrames = frames;
		sprite.Play("idle_side");

		if (withPoof)
		{
			gus!.Scale = Vector2.Zero;
			var tween = CreateTween();
			tween.TweenProperty(gus, "scale", Vector2.One, 0.35f)
				 .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}
	}

	// ── Shizu music aura event ─────────────────────────────────────────────────

	private void OnShizuAuraSignal()
	{
		if (GameManager.Instance.GetFlag(Flags.ShizuMusicAuraActive)) return;
		GameManager.Instance.SetFlag(Flags.ShizuMusicAuraActive, true);
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnShizuAltEnded));
	}

	private void OnShizuAltEnded()
	{
		AudioManager.Instance.PlayBgm(ShizuBgmPath);
		SpawnMusicNoteAura(ShizuWorldPosition());
	}

	private Vector2 ShizuWorldPosition()
	{
		var shizu = YSort.GetNodeOrNull<Npc>("Shizu");
		return shizu?.GlobalPosition ?? new Vector2(60f, -20f);
	}

	private void SpawnMusicNoteAura(Vector2 worldPos)
	{
		// Four notes, staggered, drifting upward and looping
		var noteColors = new Color[]
		{
			new Color(0.85f, 0.70f, 1.00f), // lavender
			new Color(1.00f, 0.85f, 0.95f), // rose white
			new Color(0.70f, 0.90f, 1.00f), // sky blue
			new Color(1.00f, 1.00f, 0.80f), // pale yellow
		};

		var baseOffsets = new Vector2[]
		{
			new Vector2(-8f,  -4f),
			new Vector2( 6f,  -8f),
			new Vector2(-14f,-12f),
			new Vector2( 12f, -2f),
		};

		for (int i = 0; i < 4; i++)
		{
			var note   = BuildMusicNote(noteColors[i]);
			AddChild(note);
			Vector2 basePos = worldPos + baseOffsets[i] + new Vector2(0f, -8f);
			note.GlobalPosition = basePos;

			float period = 1.6f + i * 0.25f;
			float delay  = i * 0.4f;
			float rise   = 28f + i * 4f;

			// Rise and fade loop: move up, fade out, snap back, repeat
			var tween = CreateTween().SetLoops();
			tween.TweenProperty(note, "global_position:y", basePos.Y - rise, period)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out)
				.SetDelay(delay);
			tween.Parallel()
				.TweenProperty(note, "modulate:a", 0f, period * 0.35f)
				.SetDelay(delay + period * 0.65f)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			tween.TweenCallback(Callable.From(() =>
			{
				note.GlobalPosition = basePos;
				note.Modulate       = note.Modulate with { A = 1f };
			}));
		}
	}

	/// <summary>Builds a simple pixel-art music note from two Polygon2D shapes.</summary>
	private static Node2D BuildMusicNote(Color color)
	{
		var root = new Node2D { ZIndex = 15 };

		// Note head — small tilted oval approximated as a diamond
		var head = new Polygon2D
		{
			Color   = color,
			Polygon = new Vector2[]
			{
				new Vector2( 0f, -2f),
				new Vector2( 3f,  0f),
				new Vector2( 0f,  2f),
				new Vector2(-3f,  0f),
			},
		};
		root.AddChild(head);

		// Stem — thin rectangle going up from right side of head
		var stem = new Polygon2D
		{
			Color   = color,
			Polygon = new Vector2[]
			{
				new Vector2( 3f,  0f),
				new Vector2( 4f,  0f),
				new Vector2( 4f, -9f),
				new Vector2( 3f, -9f),
			},
		};
		root.AddChild(stem);

		// Flag — small diagonal stroke at top of stem
		var flag = new Polygon2D
		{
			Color   = color,
			Polygon = new Vector2[]
			{
				new Vector2( 4f,  -9f),
				new Vector2( 8f,  -6f),
				new Vector2( 7f,  -5f),
				new Vector2( 3f,  -8f),
			},
		};
		root.AddChild(flag);

		return root;
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
