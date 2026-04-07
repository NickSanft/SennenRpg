using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Hud;

namespace SennenRpg.Scenes.Overworld;

// Event handlers and helper methods split from MAPP.cs for size management.
public partial class MAPP
{
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
		InstantiateHorse(HorseWorldPosition(), withPoof: true);
		CheckAllAltDialogsDone();
	}

	// ── Lily cutscene ──────────────────────────────────────────────────────────

	private void OnLilyAltSignal()
	{
		GameManager.Instance.SetFlag(Flags.LilyAltDone, true);
		// Wait for Lily's dialog box to close, then play the player's inner monologue
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnLilyAltEnded));
	}

	private void OnLilyAltEnded()
	{
		ShowNauseaTint();
		GameManager.Instance.SetState(GameState.Dialog);
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnLilyCutsceneEnded));
		DialogicBridge.Instance.StartTimeline(LilyCutscenePath);
	}

	private void OnLilyCutsceneEnded()
	{
		if (_nauseaLayer != null)
		{
			var layer = _nauseaLayer;
			_nauseaLayer = null;
			var overlay = layer.GetChild<ColorRect>(0);
			var tween = CreateTween();
			tween.TweenProperty(overlay, "color:a", 0f, 1.2f)
				.SetTrans(Tween.TransitionType.Sine);
			tween.TweenCallback(Callable.From(layer.QueueFree));
		}
		GameManager.Instance.SetState(GameState.Overworld);
		CheckAllAltDialogsDone();
	}

	/// <summary>
	/// Overlays the screen with a sickly green tint — fades in over 0.8 s.
	/// Call <see cref="OnLilyCutsceneEnded"/> to fade it back out.
	/// Uses CanvasLayer 50 so it sits below the dialog UI.
	/// </summary>
	private void ShowNauseaTint()
	{
		_nauseaLayer = new CanvasLayer { Layer = 50 };
		AddChild(_nauseaLayer);

		var overlay = new ColorRect
		{
			Color        = new Color(0.10f, 0.55f, 0.10f, 0f),
			AnchorRight  = 1f,
			AnchorBottom = 1f,
		};
		_nauseaLayer.AddChild(overlay);

		var tween = CreateTween();
		tween.TweenProperty(overlay, "color:a", 0.35f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine);

		ShakeCamera(0.7f, 3.5f);
		GetNodeOrNull<GameHud>("GameHud")?.FlashHpBar(new Color(0.15f, 0.80f, 0.15f));
	}

	// ── Falafel event ──────────────────────────────────────────────────────────

	private void OnBhataFalafelSignal()
	{
		if (GameManager.Instance.GetFlag(Flags.BhataFalafelAppeared)) return;
		GameManager.Instance.SetFlag(Flags.BhataFalafelAppeared, true);

		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnBhataAltEnded));
	}

	private void OnBhataAltEnded()
	{
		InstantiateFalafel(FalafelWorldPosition(), withPoof: false, dropIn: true);
		CheckAllAltDialogsDone();
	}

	private Vector2 FalafelWorldPosition()
	{
		var bhata = YSort.GetNodeOrNull<Npc>("Bhata");
		return (bhata?.GlobalPosition ?? new Vector2(-60f, 20f)) + new Vector2(-28f, -12f);
	}

	private void InstantiateFalafel(Vector2 globalPos, bool withPoof, bool dropIn = false)
	{
		var npcScene = GD.Load<PackedScene>(FalafelScene);
		var falafel  = npcScene.Instantiate<Npc>();

		falafel.NpcId         = "mapp_falafel";
		falafel.DisplayName   = "Falafel";
		falafel.TimelinePath  = FalafelTimeline;
		falafel.CharacterPath = "res://dialog/characters/Falafel.dch";
		falafel.DefaultFacing = FacingDirection.Side;

		YSort.AddChild(falafel);
		falafel.GlobalPosition = globalPos;

		// Purple energy orbs orbiting Falafel
		SpawnEnergyAura(falafel);

		if (dropIn)
		{
			// Drop from above with a physics bounce, then start the idle bob
			falafel.Position = falafel.Position + new Vector2(0f, -80f);
			var drop = CreateTween();
			drop.TweenProperty(falafel, "position:y", falafel.Position.Y + 80f, 0.55f)
				.SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
			drop.TweenCallback(Callable.From(() =>
			{
				SpawnExpandingRing(globalPos, new Color(0.70f, 0.10f, 1.00f, 0.75f));
				StartFalafelBob(falafel);
			}));
		}
		else if (withPoof)
		{
			falafel.Scale = Vector2.Zero;
			var tween = CreateTween();
			tween.TweenProperty(falafel, "scale", Vector2.One, 0.35f)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenCallback(Callable.From(() => StartFalafelBob(falafel)));
		}
		else
		{
			StartFalafelBob(falafel);
		}
	}

	private void StartFalafelBob(Node2D falafel)
	{
		float baseY = falafel.Position.Y;
		var bob = CreateTween().SetLoops();
		bob.TweenProperty(falafel, "position:y", baseY - 6f, 0.9f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		bob.TweenProperty(falafel, "position:y", baseY, 0.9f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
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
		CheckAllAltDialogsDone();
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
				// Scale pulse loop begins after pop-in
				float popDelay = 0.3f + GD.Randf() * 0.2f;
				var pulse = CreateTween().SetLoops();
				pulse.TweenProperty(crystal, "scale", new Vector2(1.12f, 1.12f), 0.55f)
					.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut)
					.SetDelay(popDelay);
				pulse.TweenProperty(crystal, "scale", Vector2.One, 0.55f)
					.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
			}
			else
			{
				// Scale pulse even on restore
				float pDelay = GD.Randf() * 0.8f;
				var pulse = CreateTween().SetLoops();
				pulse.TweenProperty(crystal, "scale", new Vector2(1.12f, 1.12f), 0.55f)
					.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut)
					.SetDelay(pDelay);
				pulse.TweenProperty(crystal, "scale", Vector2.One, 0.55f)
					.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
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
		// White screen flash — transform Gus at the peak so it looks instantaneous
		var flashLayer = new CanvasLayer { Layer = 60 };
		AddChild(flashLayer);
		var flash = new ColorRect
		{
			Color        = new Color(1f, 1f, 1f, 0f),
			AnchorRight  = 1f,
			AnchorBottom = 1f,
		};
		flashLayer.AddChild(flash);

		var t = CreateTween();
		t.TweenProperty(flash, "color:a", 0.90f, 0.06f).SetTrans(Tween.TransitionType.Linear);
		t.TweenCallback(Callable.From(() => TransformGusToFrog(withPoof: false)));
		t.TweenProperty(flash, "color:a", 0f, 0.28f)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		t.TweenCallback(Callable.From(flashLayer.QueueFree));

		CheckAllAltDialogsDone();
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
			frames.SetAnimationSpeed(animName, 6.0f);

			for (int f = 0; f < 8; f++)
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
		var shizuPos = ShizuWorldPosition();
		SpawnExpandingRing(shizuPos, new Color(0.85f, 0.70f, 1.00f, 0.85f));
		SpawnMusicNoteAura(shizuPos);
		CheckAllAltDialogsDone();
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

	// ── Rain alt event ─────────────────────────────────────────────────────────

	private void OnRainAltSignal()
	{
		GameManager.Instance.SetFlag(Flags.RainAltDone, true);
		DialogicBridge.Instance.ConnectTimelineEnded(
			new Callable(this, MethodName.OnRainAltEnded));
	}

	private void OnRainAltEnded()
	{
		GameManager.Instance.RemoveGold(RainStolenGold);
		SpawnGoldStealAnim();
		CheckAllAltDialogsDone();
	}

	/// <summary>
	/// Spawns a short burst of gold coin polygons that fly from the player to
	/// Rain's position, suggesting a pickpocket. Coins fade and free themselves.
	/// </summary>
	private void SpawnGoldStealAnim()
	{
		var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		var rain   = YSort.GetNodeOrNull<Node2D>("Rain");
		if (player == null || rain == null) return;

		var from = player.GlobalPosition + new Vector2(0f, -8f);
		var to   = rain.GlobalPosition   + new Vector2(0f, -14f);

		int   coinCount = 8;
		float spread    = 7f;
		var   goldColor = new Color(1.00f, 0.82f, 0.14f);

		SpawnFloatingLabel($"-{RainStolenGold}G", goldColor, from);

		for (int i = 0; i < coinCount; i++)
		{
			// Small diamond coin shape
			var coin = new Polygon2D
			{
				Color   = goldColor,
				ZIndex  = 15,
				Polygon = new Vector2[]
				{
					new Vector2( 0f, -3f),
					new Vector2( 3f,  0f),
					new Vector2( 0f,  3f),
					new Vector2(-3f,  0f),
				},
			};
			AddChild(coin);

			float angle = (Mathf.Tau / coinCount) * i;
			coin.GlobalPosition = from + new Vector2(
				Mathf.Cos(angle) * spread,
				Mathf.Sin(angle) * spread * 0.5f);

			float delay = i * 0.055f;

			var tween = CreateTween();
			tween.TweenProperty(coin, "global_position", to, 0.45f)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In)
				.SetDelay(delay);
			tween.Parallel()
				.TweenProperty(coin, "modulate:a", 0f, 0.12f)
				.SetDelay(delay + 0.33f)
				.SetTrans(Tween.TransitionType.Linear);
			tween.TweenCallback(Callable.From(coin.QueueFree));
		}
	}

	// ── "SHE HAS ARISEN" trigger ───────────────────────────────────────────────

	private void CheckAllAltDialogsDone()
	{
		if (GameManager.Instance.GetFlag(Flags.AllAltDialogsDone)) return;

		bool allDone =
			GameManager.Instance.GetFlag(Flags.BrixHorseAppeared)     &&
			GameManager.Instance.GetFlag(Flags.LilyAltDone)           &&
			GameManager.Instance.GetFlag(Flags.BhataFalafelAppeared)  &&
			GameManager.Instance.GetFlag(Flags.KrioraCrystalsAppeared) &&
			GameManager.Instance.GetFlag(Flags.GusTransformedToFrog)  &&
			GameManager.Instance.GetFlag(Flags.ShizuMusicAuraActive)  &&
			GameManager.Instance.GetFlag(Flags.RainAltDone);

		if (!allDone) return;

		GameManager.Instance.SetFlag(Flags.AllAltDialogsDone, true);
		StartArisesCutscene();
	}

	private void StartArisesCutscene()
	{
		GameManager.Instance.SetState(GameState.Dialog);
		var cutscene = new ArisesCutscene();
		AddChild(cutscene);
		cutscene.Finished += OnArisesCutsceneFinished;
	}

	private void OnArisesCutsceneFinished()
	{
		GameManager.Instance.SetState(GameState.Overworld);
		// Drop the ambience ~40% to sell "the tavern falls silent" as a real world change.
		// -12 dB is the normal level; -20 dB is roughly 40% quieter in perceived loudness.
		AudioManager.Instance.FadeAmbienceTo(-20f, 3f);
	}

	/// <summary>Returns a world position just to the right of Brix.</summary>
	private Vector2 HorseWorldPosition()
	{
		var brix = YSort.GetNodeOrNull<Npc>("Brix");
		return (brix?.GlobalPosition ?? new Vector2(100f, 20f)) + new Vector2(26f, 0f);
	}

	private void InstantiateHorse(Vector2 finalPos, bool withPoof)
	{
		var npcScene = GD.Load<PackedScene>(HorseScene);
		var horse    = npcScene.Instantiate<Npc>();

		horse.NpcId         = "mapp_horse";
		horse.DisplayName   = "WindBreaker";
		horse.TimelinePath  = HorseTimeline;
		horse.CharacterPath = "res://dialog/characters/Horse.dch";
		horse.DefaultFacing = FacingDirection.Side;

		YSort.AddChild(horse);

		if (withPoof)
		{
			// Trot in from just off the west edge of the room
			horse.GlobalPosition = new Vector2(-190f, finalPos.Y);
			var trot = CreateTween();
			trot.TweenProperty(horse, "global_position:x", finalPos.X, 1.2f)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
			trot.TweenCallback(Callable.From(() =>
			{
				horse.GlobalPosition = finalPos;
				SpawnPoof(finalPos);
				var brix = YSort.GetNodeOrNull<Node2D>("Brix");
				if (brix != null)
					SpawnNpcEmote(brix, "!", new Color(1f, 0.95f, 0.2f));
			}));
		}
		else
		{
			horse.GlobalPosition = finalPos;
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

	// ── Firelight tint ──────────────────────────────────────────────────────────

	/// <summary>
	/// Applies a warm amber modulate to any NPC within range of the hearth
	/// (x &lt; −80), making them look lit by the fire.
	/// </summary>
	private void ApplyFirelightTints()
	{
		foreach (var child in YSort.GetChildren())
		{
			if (child is Node2D node && node.GlobalPosition.X < -80f)
				node.Modulate = new Color(1.05f, 0.95f, 0.88f);
		}
	}

	// ── Ambient life ────────────────────────────────────────────────────────────

	/// <summary>
	/// Kicks off idle-wander behaviour for every named NPC in the tavern.
	/// Each NPC occasionally glances in a new direction, then returns to its default.
	/// </summary>
	private void SpawnAllIdleWanders()
	{
		// Rork turns to face the bottle rack (idle_up) then back
		if (YSort.GetNodeOrNull<Npc>("Rork") is { } rork)
			SpawnIdleWander(rork, new[] { "idle_down", "idle_up" });

		// Everyone else glances side-to-side
		foreach (var name in new[] { "Kriora", "Lily", "Rain", "Bhata", "Shizu", "Brix", "Gus" })
		{
			if (YSort.GetNodeOrNull<Npc>(name) is { } npc)
				SpawnIdleWander(npc, new[] { "idle_down", "idle_side" });
		}
	}

	/// <summary>
	/// Schedules recurring idle animation turns for <paramref name="npc"/>.
	/// Waits a random 8–14 s, plays the next anim in <paramref name="animCycle"/>,
	/// holds it 3–5 s, then returns to the first anim and repeats.
	/// Each interval is re-randomised so the timing never becomes predictable.
	/// </summary>
	private void SpawnIdleWander(Npc npc, string[] animCycle)
	{
		var sprite = npc.GetNodeOrNull<AnimatedSprite2D>("Sprite");
		if (sprite == null) return;

		var rng = new RandomNumberGenerator();
		rng.Randomize();

		int cycleIdx = 0;

		// Self-referential lambda — assign before capture
		System.Action scheduleNext = null!;
		scheduleNext = () =>
		{
			if (!IsInstanceValid(npc)) return;

			GetTree().CreateTimer(rng.RandfRange(8f, 14f))
				.Connect("timeout", Callable.From(() =>
			{
				if (!IsInstanceValid(npc)) return;

				cycleIdx = (cycleIdx + 1) % animCycle.Length;
				sprite.Play(animCycle[cycleIdx]);

				GetTree().CreateTimer(rng.RandfRange(3f, 5f))
					.Connect("timeout", Callable.From(() =>
				{
					if (!IsInstanceValid(npc)) return;

					cycleIdx = 0;
					sprite.Play(animCycle[0]);
					scheduleNext();
				}));
			}));
		};

		// Stagger each NPC's first turn so they don't all move simultaneously
		GetTree().CreateTimer(rng.RandfRange(0f, 12f))
			.Connect("timeout", Callable.From(scheduleNext));
	}

	// ── Visual helpers ──────────────────────────────────────────────────────────

	/// <summary>
	/// Covers the screen with a dark overlay that immediately fades out.
	/// Hides the single-frame pop-in of all code-generated geometry on map load
	/// and reads as stepping through a doorway from a darker corridor.
	/// </summary>
	private void SpawnEntryFade()
	{
		var layer = new CanvasLayer { Layer = 80 };
		AddChild(layer);

		var cover = new ColorRect
		{
			Color        = new Color(0f, 0f, 0f, 0.55f),
			AnchorRight  = 1f,
			AnchorBottom = 1f,
		};
		layer.AddChild(cover);

		var t = CreateTween();
		t.TweenProperty(cover, "color:a", 0f, 0.6f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		t.TweenCallback(Callable.From(layer.QueueFree));
	}

	/// <summary>
	/// Shakes the active camera by tweening its Offset over <paramref name="duration"/> seconds.
	/// Amplitude decays to zero over the duration.
	/// </summary>
	private void ShakeCamera(float amplitude, float duration)
	{
		var cam = GetViewport().GetCamera2D();
		if (cam == null) return;

		int   steps = Mathf.Max(1, (int)(duration / 0.06f));
		float step  = duration / steps;
		var   t     = CreateTween();
		for (int i = 0; i < steps; i++)
		{
			float decay = 1f - (float)i / steps;
			float dx    = amplitude * decay * (i % 2 == 0 ? 1f : -1f);
			float dy    = amplitude * decay * (i % 3 == 0 ? 0.5f : -0.5f);
			t.TweenProperty(cam, "offset", new Vector2(dx, dy), step)
				.SetTrans(Tween.TransitionType.Sine);
		}
		t.TweenProperty(cam, "offset", Vector2.Zero, step)
			.SetTrans(Tween.TransitionType.Sine);
	}

	/// <summary>
	/// Spawns a short-lived text label (e.g. "!") floating above <paramref name="npc"/>
	/// in world space.
	/// </summary>
	private void SpawnNpcEmote(Node2D npc, string text, Color color)
	{
		var label = new Label
		{
			Text        = text,
			Modulate    = color,
			ZIndex      = 30,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		label.AddThemeFontSizeOverride("font_size", 16);
		AddChild(label);
		label.GlobalPosition = npc.GlobalPosition + new Vector2(-4f, -30f);

		var t = CreateTween();
		t.TweenProperty(label, "global_position:y", label.GlobalPosition.Y - 10f, 0.4f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		t.TweenProperty(label, "modulate:a", 0f, 0.3f)
			.SetDelay(0.5f).SetTrans(Tween.TransitionType.Linear);
		t.TweenCallback(Callable.From(label.QueueFree));
	}

	/// <summary>
	/// Spawns an expanding translucent ring at <paramref name="worldPos"/> that
	/// rapidly scales out and fades — good for magical impact moments.
	/// </summary>
	private void SpawnExpandingRing(Vector2 worldPos, Color color)
	{
		int   pts       = 16;
		float maxRadius = 32f;

		var pts2 = new Vector2[pts];
		for (int i = 0; i < pts; i++)
		{
			float a = (Mathf.Tau / pts) * i;
			pts2[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
		}

		var ring = new Polygon2D
		{
			Color    = color,
			ZIndex   = 15,
			Polygon  = pts2,
			Scale    = Vector2.Zero,
		};
		AddChild(ring);
		ring.GlobalPosition = worldPos;

		var t = CreateTween();
		t.TweenProperty(ring, "scale", new Vector2(maxRadius, maxRadius * 0.5f), 0.5f)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		t.Parallel()
		 .TweenProperty(ring, "modulate:a", 0f, 0.5f)
		 .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		t.TweenCallback(Callable.From(ring.QueueFree));
	}

	/// <summary>
	/// Spawns a floating text label (e.g. "-10G") near <paramref name="worldPos"/>
	/// that drifts upward and fades out.
	/// </summary>
	private void SpawnFloatingLabel(string text, Color color, Vector2 worldPos)
	{
		var label = new Label
		{
			Text        = text,
			Modulate    = color,
			ZIndex      = 30,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		label.AddThemeFontSizeOverride("font_size", 14);
		AddChild(label);
		label.GlobalPosition = worldPos + new Vector2(-8f, -10f);

		var t = CreateTween();
		t.TweenProperty(label, "global_position:y", label.GlobalPosition.Y - 24f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		t.Parallel()
		 .TweenProperty(label, "modulate:a", 0f, 0.25f)
		 .SetDelay(0.55f).SetTrans(Tween.TransitionType.Linear);
		t.TweenCallback(Callable.From(label.QueueFree));
	}

	/// <summary>Adds a Polygon2D as a direct child of the MAPP node and returns it.</summary>
	private Polygon2D AddPoly(Vector2 worldPos, Vector2[] polygon, Color color, int zIndex)
	{
		var poly = new Polygon2D { Color = color, ZIndex = zIndex, Polygon = polygon };
		AddChild(poly);
		poly.GlobalPosition = worldPos;
		return poly;
	}

	// ── Staircase ──────────────────────────────────────────────────────────────

	private void SpawnStaircase()
	{
		// Visual staircase steps on the east wall suggesting an upper floor
		var stairPos = new Vector2(120f, -25f);
		var stepColor = new Color(0.42f, 0.28f, 0.12f);
		var shadowColor = new Color(0f, 0f, 0f, 0.25f);

		for (int i = 0; i < 4; i++)
		{
			float y = i * 7f;
			float inset = i * 3f;
			// Shadow under each step
			AddPoly(stairPos + new Vector2(inset + 1f, y + 1f), new Vector2[]
			{
				new Vector2(0f, 0f), new Vector2(22f - inset, 0f),
				new Vector2(22f - inset, 5f), new Vector2(0f, 5f),
			}, shadowColor, zIndex: -6);
			// Step face
			AddPoly(stairPos + new Vector2(inset, y), new Vector2[]
			{
				new Vector2(0f, 0f), new Vector2(22f - inset, 0f),
				new Vector2(22f - inset, 5f), new Vector2(0f, 5f),
			}, new Color(stepColor.R - i * 0.03f, stepColor.G - i * 0.02f, stepColor.B - i * 0.01f), zIndex: -5);
		}

	}

	// ── Tile map ───────────────────────────────────────────────────────────────

	private static readonly Vector2I TileFloorA  = new Vector2I(0, 6); // floor variant A
	private static readonly Vector2I TileFloorB  = new Vector2I(1, 6); // floor variant B
	private static readonly Vector2I TileWall    = new Vector2I(2, 6); // wall (has collision in tileset)
	private static readonly Vector2I TileBar     = new Vector2I(0, 7); // bar counter / accent tile

	private void BuildTileMap()
	{
		var tex = GD.Load<Texture2D>(TilesetPath);
		if (tex == null) return; // tileset texture not yet available

		var source = new TileSetAtlasSource();
		source.Texture            = tex;
		source.TextureRegionSize  = new Vector2I(16, 16);
		source.CreateTile(TileFloorA);
		source.CreateTile(TileFloorB);
		source.CreateTile(TileWall);
		source.CreateTile(TileBar);

		var tileSet = new TileSet();
		tileSet.TileSize = new Vector2I(16, 16);
		tileSet.AddSource(source, 0);

		var layer = new TileMapLayer { Name = "MappTiles", TileSet = tileSet, ZIndex = -10 };
		AddChild(layer);

		PlaceTiles(layer);
	}

	private static void PlaceTiles(TileMapLayer layer)
	{
		// ── Floor (wood planks) — full room interior, y = -3 to +10 ──────────
		for (int x = -10; x <= 9; x++)
		{
			for (int y = -3; y <= 10; y++)
			{
				// Alternate tiles based on column for a plank-strip look
				var tile = (Mathf.Abs(x) % 3 == 0) ? TileFloorB : TileFloorA;
				layer.SetCell(new Vector2I(x, y), 0, tile);
			}
		}

		// ── Bar counter strip ─────────────────────────────────────────────────
		// y = -4 → world y -64 to -48: replaces Bar + BarTop ColorRects
		for (int x = -8; x <= 7; x++)
		{
			layer.SetCell(new Vector2I(x, -4), 0, TileBar);
			layer.SetCell(new Vector2I(x, -5), 0, TileBar);
		}

		// ── North wall (stone brick) ──────────────────────────────────────────
		for (int x = -10; x <= 9; x++)
			for (int y = -8; y <= -6; y++)
				layer.SetCell(new Vector2I(x, y), 0, TileWall);

		// ── South wall strip ──────────────────────────────────────────────────
		for (int x = -10; x <= 9; x++)
			layer.SetCell(new Vector2I(x, 10), 0, TileWall);

		// ── Side wall edges ───────────────────────────────────────────────────
		for (int y = -8; y <= 10; y++)
		{
			layer.SetCell(new Vector2I(-10, y), 0, TileWall);
			layer.SetCell(new Vector2I(9,   y), 0, TileWall);
		}
	}

	// ── Ceiling beams ──────────────────────────────────────────────────────────

	private void SpawnCeilingBeams()
	{
		var beamColor = new Color(0.14f, 0.07f, 0.03f);
		// Three horizontal rafters spanning the full room width, z behind NPCs
		float[] beamYs = [-38f, -14f, 10f];
		foreach (float y in beamYs)
		{
			var beam = new ColorRect
			{
				Color  = beamColor,
				Size   = new Vector2(310f, 4f),
				ZIndex = -5,
			};
			AddChild(beam);
			beam.GlobalPosition = new Vector2(-155f, y);
		}
	}

	// ── Rugs ───────────────────────────────────────────────────────────────────

	private void SpawnRugs()
	{
		// Deep crimson rugs — one under each table cluster, one by the fireplace
		var rugColor    = new Color(0.48f, 0.08f, 0.08f, 0.85f);
		var rugBorder   = new Color(0.60f, 0.20f, 0.10f, 0.80f);
		var rugCenters  = new (Vector2 center, Vector2 size)[]
		{
			(new Vector2(-60f,  40f), new Vector2(46f, 34f)),
			(new Vector2( 60f,  40f), new Vector2(46f, 34f)),
			(new Vector2(  0f, -10f), new Vector2(46f, 34f)),
			(new Vector2(-108f,  8f), new Vector2(32f, 28f)), // hearth area
		};

		foreach (var (center, size) in rugCenters)
		{
			float hw = size.X * 0.5f, hh = size.Y * 0.5f;

			// Border stripe (slightly larger)
			AddPoly(center, new Vector2[]
			{
				new Vector2(-hw - 2f, -hh - 2f), new Vector2(hw + 2f, -hh - 2f),
				new Vector2(hw + 2f,   hh + 2f), new Vector2(-hw - 2f,  hh + 2f),
			}, rugBorder, zIndex: -8);

			// Main rug
			AddPoly(center, new Vector2[]
			{
				new Vector2(-hw, -hh), new Vector2(hw, -hh),
				new Vector2(hw,   hh), new Vector2(-hw,  hh),
			}, rugColor, zIndex: -8);
		}
	}

	// ── Wall decorations ───────────────────────────────────────────────────────

	private void SpawnWallDecorations()
	{
		// Mounted antlers on east side of north wall
		SpawnMountedAntlers(new Vector2(65f, -90f));
	}

	private void SpawnMountedAntlers(Vector2 pos)
	{
		var woodColor   = new Color(0.45f, 0.28f, 0.10f);
		var antlerColor = new Color(0.72f, 0.56f, 0.30f);

		// Mounting plaque
		AddPoly(pos, new Vector2[] {
			new Vector2(-8f, -3f), new Vector2(8f, -3f),
			new Vector2(8f,  3f),  new Vector2(-8f, 3f),
		}, woodColor, zIndex: -4);

		// Left antler branch (three segments pointing up-left)
		AddPoly(pos + new Vector2(-4f, -3f), new Vector2[] {
			new Vector2(-1f, 0f), new Vector2(1f, 0f),
			new Vector2(0f, -7f),
		}, antlerColor, zIndex: -4);
		AddPoly(pos + new Vector2(-6f, -7f), new Vector2[] {
			new Vector2(-1f, 0f), new Vector2(1f, 0f),
			new Vector2(-2f, -5f),
		}, antlerColor, zIndex: -4);
		AddPoly(pos + new Vector2(-3f, -7f), new Vector2[] {
			new Vector2(-1f, 0f), new Vector2(1f, 0f),
			new Vector2( 2f, -4f),
		}, antlerColor, zIndex: -4);

		// Right antler (mirrored)
		AddPoly(pos + new Vector2(4f, -3f), new Vector2[] {
			new Vector2(-1f, 0f), new Vector2(1f, 0f),
			new Vector2(0f, -7f),
		}, antlerColor, zIndex: -4);
		AddPoly(pos + new Vector2(6f, -7f), new Vector2[] {
			new Vector2(-1f, 0f), new Vector2(1f, 0f),
			new Vector2( 2f, -5f),
		}, antlerColor, zIndex: -4);
		AddPoly(pos + new Vector2(3f, -7f), new Vector2[] {
			new Vector2(-1f, 0f), new Vector2(1f, 0f),
			new Vector2(-2f, -4f),
		}, antlerColor, zIndex: -4);
	}

	// ── Candle flame animation ─────────────────────────────────────────────────

	private void AnimateCandleFlame(Polygon2D flame)
	{
		float period = 0.26f + GD.Randf() * 0.14f;
		float delay  = GD.Randf() * 0.4f;
		float baseY  = flame.Position.Y;

		var tScale = CreateTween().SetLoops();
		tScale.TweenProperty(flame, "scale:y", 1.25f, period)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut)
			.SetDelay(delay);
		tScale.TweenProperty(flame, "scale:y", 0.78f, period)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

		var tBob = CreateTween().SetLoops();
		tBob.TweenProperty(flame, "position:y", baseY - 1.5f, period * 1.2f)
			.SetTrans(Tween.TransitionType.Sine).SetDelay(delay + period * 0.5f);
		tBob.TweenProperty(flame, "position:y", baseY + 0.5f, period * 1.2f)
			.SetTrans(Tween.TransitionType.Sine);
	}

	// ── Light flicker ──────────────────────────────────────────────────────────

	private void FlickerCandleLights()
	{
		// Table candle lights — gentle, staggered pulses
		var candleLights = new (string name, float baseEnergy)[]
		{
			("Table1Light", 0.45f),
			("Table2Light", 0.45f),
			("Table3Light", 0.40f),
		};

		float delay = 0f;
		foreach (var (name, baseEnergy) in candleLights)
		{
			var light = GetNodeOrNull<PointLight2D>(name);
			if (light == null) continue;

			float period = 0.55f + GD.Randf() * 0.20f;
			var t = CreateTween().SetLoops();
			t.TweenProperty(light, "energy", baseEnergy + 0.12f, period)
				.SetTrans(Tween.TransitionType.Sine).SetDelay(delay);
			t.TweenProperty(light, "energy", baseEnergy - 0.10f, period)
				.SetTrans(Tween.TransitionType.Sine);

			delay += 0.20f; // stagger so the three lights don't pulse in unison
		}

		// Fire light — more aggressive flicker to match the layered flames
		var fireLight = GetNodeOrNull<PointLight2D>("FireLight");
		if (fireLight != null)
		{
			var t = CreateTween().SetLoops();
			t.TweenProperty(fireLight, "energy", 1.05f, 0.20f)
				.SetTrans(Tween.TransitionType.Sine);
			t.TweenProperty(fireLight, "energy", 0.62f, 0.30f)
				.SetTrans(Tween.TransitionType.Sine);
			t.TweenProperty(fireLight, "energy", 0.90f, 0.16f)
				.SetTrans(Tween.TransitionType.Sine);
			t.TweenProperty(fireLight, "energy", 0.55f, 0.25f)
				.SetTrans(Tween.TransitionType.Sine);
		}
	}

	// ── Wall collision ─────────────────────────────────────────────────────────

	/// <summary>
	/// Adds StaticBody2D collision boxes matching the tile-based wall geometry.
	/// All measurements are in pixels; tile size = 16.
	///
	/// Room tile extents:  x = -10 .. 9  →  pixel x = -160 .. 160
	///                     y = -8  .. 10 →  pixel y = -128 .. 176
	///
	/// The north wall (y=-8..-6) and bar counter (y=-5..-4) are merged into a
	/// single box so the player cannot reach the staff side of the bar.
	/// The south wall has a 32 px doorway gap centred at x=0 for the map exit.
	/// </summary>
	private void SpawnWallColliders()
	{
		// North wall (y=-8..-6) + bar counter (y=-5..-4) — full width
		// pixel: x=-160..160, y=-128..-48  →  size 320×80, centre (0, -88)
		SpawnWallBox(new Vector2(  0f, -88f), new Vector2(320f,  80f));

		// South wall — west portion (x=-160..-16)
		// pixel: x=-160..-16, y=160..176  →  size 144×16, centre (-88, 168)
		SpawnWallBox(new Vector2(-88f, 168f), new Vector2(144f,  16f));

		// South wall — east portion (x=16..160)
		// pixel: x=16..160, y=160..176  →  size 144×16, centre (88, 168)
		SpawnWallBox(new Vector2( 88f, 168f), new Vector2(144f,  16f));

		// West wall — full height (tile col x=-10)
		// pixel: x=-160..-144, y=-128..176  →  size 16×304, centre (-152, 24)
		SpawnWallBox(new Vector2(-152f,  24f), new Vector2( 16f, 304f));

		// East wall — split for garden back-door gap at y=56..88
		// North section: x=144..160, y=-128..56  →  size 16×184, centre (152, -36)
		SpawnWallBox(new Vector2( 152f, -36f), new Vector2( 16f, 184f));
		// South section: x=144..160, y=88..176  →  size 16×88, centre (152, 132)
		SpawnWallBox(new Vector2( 152f, 132f), new Vector2( 16f,  88f));

	}

	private void SpawnWallBox(Vector2 centre, Vector2 size)
	{
		var body  = new StaticBody2D { Position = centre };
		var shape = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = size },
		};
		body.AddChild(shape);
		AddChild(body);
	}

}
