using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// The backyard garden behind the MAPP tavern.
/// A small enclosed stone-walled yard at perpetual twilight — overgrown but
/// lovingly tended, with fireflies, wildflowers, and a few unusual residents.
///
/// All static visuals are built procedurally. NPCs are placed in MappGarden.tscn.
/// Future interactables (well, bench, hive, herb garden, compost, statue) are
/// added as later implementation phases.
/// </summary>
[Tool]
public partial class MappGarden : OverworldBase
{
	private const string GardenBgmPath      = "res://assets/audio/bgm/garden_theme.ogg";
	private const string GardenAmbiencePath = "res://assets/audio/sfx/garden_ambience.ogg";

	// Garden world bounds (used by multiple helpers)
	private const float WallX    = 112f;  // east/west wall inner face
	private const float WallYN   = -80f;  // north wall inner face
	private const float WallYS   =  80f;  // south wall inner face
	private const float DoorHalf =  16f;  // half-width of south door opening

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			BuildEditorVisuals();
			return;
		}

		MapId   = "mapp_garden";
		BgmPath = ResourceLoader.Exists(GardenBgmPath) ? GardenBgmPath : "";

		// Spawn point when arriving from the MAPP back door
		SpawnPoints["from_mapp_backyard"] = new Vector2(0f, 60f);
		SpawnPoints["default"]            = new Vector2(0f, 60f);
		DefaultSpawnPosition              = new Vector2(0f, 60f);

		base._Ready();

		SpawnGround();
		SpawnPerimeterWalls();
		SpawnGardenWallColliders();
		SpawnGrassPatches();
		SpawnFlagstonePathway();
		SpawnSouthDoor();
		SpawnFireflies();
		SpawnEntryFade();

		if (ResourceLoader.Exists(GardenAmbiencePath))
			AudioManager.Instance.PlayAmbience(GardenAmbiencePath, fadeTime: 1.5f);
	}

	// ── Editor preview ─────────────────────────────────────────────────────────

	private void BuildEditorVisuals()
	{
		if (GetNodeOrNull("GardenGround") != null) return;

		SpawnGround();
		SpawnPerimeterWalls();
		SpawnGrassPatches();
		SpawnFlagstonePathway();
	}

	// ── Ground ─────────────────────────────────────────────────────────────────

	/// <summary>
	/// Lays down the base garden ground as a large dark-green rect, then a
	/// lighter inner patch to suggest the main yard area.
	/// </summary>
	private void SpawnGround()
	{
		const float hw = WallX;
		const float hh = 90f; // slightly larger than WallYS so walls overlap

		// Deep earth tone base layer
		var base_ = new Polygon2D
		{
			Name    = "GardenGround",
			Color   = new Color(0.14f, 0.22f, 0.10f),
			ZIndex  = -10,
			Polygon = new Vector2[]
			{
				new Vector2(-hw, -hh), new Vector2(hw, -hh),
				new Vector2(hw,  hh),  new Vector2(-hw, hh),
			},
		};
		AddChild(base_);
		base_.GlobalPosition = Vector2.Zero;

		// Slightly lighter inner yard (feels like open sky vs. shadowed edges)
		var yard = new Polygon2D
		{
			Color   = new Color(0.16f, 0.26f, 0.12f),
			ZIndex  = -9,
			Polygon = new Vector2[]
			{
				new Vector2(-90f, -70f), new Vector2(90f, -70f),
				new Vector2(90f,  70f),  new Vector2(-90f, 70f),
			},
		};
		AddChild(yard);
		yard.GlobalPosition = Vector2.Zero;
	}

	// ── Perimeter walls ────────────────────────────────────────────────────────

	/// <summary>
	/// Draws procedural stone walls around the garden perimeter. The south wall
	/// has a door-sized gap matching <see cref="SpawnSouthDoor"/>.
	/// </summary>
	private void SpawnPerimeterWalls()
	{
		var stoneColor  = new Color(0.42f, 0.38f, 0.33f);
		var mortarColor = new Color(0.28f, 0.25f, 0.22f);
		var capColor    = new Color(0.50f, 0.45f, 0.40f);
		const float wallW = 16f;

		// North wall — full width
		SpawnWallSegment(new Vector2(0f, WallYN - wallW * 0.5f),
			new Vector2(WallX * 2f + wallW * 2f, wallW), stoneColor, mortarColor, capColor);

		// West wall
		SpawnWallSegment(new Vector2(-WallX - wallW * 0.5f, 0f),
			new Vector2(wallW, (WallYS - WallYN) + wallW), stoneColor, mortarColor, capColor);

		// East wall
		SpawnWallSegment(new Vector2(WallX + wallW * 0.5f, 0f),
			new Vector2(wallW, (WallYS - WallYN) + wallW), stoneColor, mortarColor, capColor);

		// South wall — west portion (x = -WallX .. -DoorHalf)
		float swWidth = WallX - DoorHalf;
		SpawnWallSegment(new Vector2(-(DoorHalf + swWidth * 0.5f), WallYS + wallW * 0.5f),
			new Vector2(swWidth, wallW), stoneColor, mortarColor, capColor);

		// South wall — east portion (x = DoorHalf .. WallX)
		SpawnWallSegment(new Vector2(DoorHalf + swWidth * 0.5f, WallYS + wallW * 0.5f),
			new Vector2(swWidth, wallW), stoneColor, mortarColor, capColor);
	}

	private void SpawnWallSegment(Vector2 centre, Vector2 size, Color stone, Color mortar, Color cap)
	{
		float hw = size.X * 0.5f;
		float hh = size.Y * 0.5f;

		// Main stone fill
		AddPoly(centre, new Vector2[]
		{
			new Vector2(-hw, -hh), new Vector2(hw, -hh),
			new Vector2(hw,  hh),  new Vector2(-hw, hh),
		}, stone, zIndex: -5);

		// Cap highlight (top strip)
		AddPoly(centre + new Vector2(0f, -hh + 2f), new Vector2[]
		{
			new Vector2(-hw, -2f), new Vector2(hw, -2f),
			new Vector2(hw,   2f), new Vector2(-hw, 2f),
		}, cap, zIndex: -4);

		// Mortar lines (two horizontal cracks for a stone-block look)
		for (int i = 1; i <= 2; i++)
		{
			float y = -hh + (size.Y / 3f) * i;
			AddPoly(centre + new Vector2(0f, y), new Vector2[]
			{
				new Vector2(-hw, -0.5f), new Vector2(hw, -0.5f),
				new Vector2(hw,   0.5f), new Vector2(-hw, 0.5f),
			}, mortar, zIndex: -4);
		}
	}

	// ── Grass patches ──────────────────────────────────────────────────────────

	/// <summary>
	/// Scatters irregular grass Polygon2Ds in the garden corners and edges,
	/// giving an overgrown-but-loved feel.
	/// </summary>
	private void SpawnGrassPatches()
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		// Each patch: (centre, maxHalfW, maxHalfH, color)
		var patches = new (Vector2 centre, float hw, float hh, Color color)[]
		{
			(new Vector2(-85f, -60f), 18f, 12f, new Color(0.20f, 0.40f, 0.15f, 0.85f)), // NW
			(new Vector2( 85f, -60f), 18f, 12f, new Color(0.22f, 0.38f, 0.13f, 0.85f)), // NE
			(new Vector2(-85f,  55f), 16f, 10f, new Color(0.18f, 0.35f, 0.12f, 0.85f)), // SW
			(new Vector2( 85f,  55f), 16f, 10f, new Color(0.20f, 0.36f, 0.14f, 0.85f)), // SE
			(new Vector2(-70f,  0f),  12f,  8f, new Color(0.22f, 0.40f, 0.14f, 0.80f)), // mid-west
			(new Vector2( 70f, -20f), 12f,  8f, new Color(0.18f, 0.38f, 0.13f, 0.80f)), // mid-east
		};

		foreach (var (centre, hw, hh, color) in patches)
		{
			// Irregular hexagon for each patch — slight random jitter
			int pts = 7;
			var poly = new Vector2[pts];
			for (int i = 0; i < pts; i++)
			{
				float a  = (Mathf.Tau / pts) * i;
				float rx = hw * rng.RandfRange(0.7f, 1.0f);
				float ry = hh * rng.RandfRange(0.7f, 1.0f);
				poly[i] = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
			}
			AddPoly(centre, poly, color, zIndex: -8);

			// Tiny grass blade wisps (short vertical diamonds)
			int blades = rng.RandiRange(4, 7);
			for (int b = 0; b < blades; b++)
			{
				float bx = rng.RandfRange(-hw * 0.8f, hw * 0.8f);
				float by = rng.RandfRange(-hh * 0.8f, hh * 0.8f);
				float bh = rng.RandfRange(3f, 6f);
				AddPoly(centre + new Vector2(bx, by), new Vector2[]
				{
					new Vector2( 0f, -bh),
					new Vector2( 1f,  0f),
					new Vector2( 0f,  1f),
					new Vector2(-1f,  0f),
				}, new Color(color.R + 0.06f, color.G + 0.08f, color.B, color.A), zIndex: -7);
			}
		}
	}

	// ── Flagstone path ─────────────────────────────────────────────────────────

	/// <summary>
	/// A narrow flagstone pathway running from the south door northward,
	/// widening slightly at a central clearing near the well.
	/// </summary>
	private void SpawnFlagstonePathway()
	{
		var stoneColor  = new Color(0.45f, 0.40f, 0.36f);
		var jointColor  = new Color(0.28f, 0.25f, 0.22f);

		// Central path strip: x=-12 to x=12, y=-60 to y=80
		AddPoly(new Vector2(0f, 10f), new Vector2[]
		{
			new Vector2(-12f, -70f), new Vector2(12f, -70f),
			new Vector2(12f,  70f),  new Vector2(-12f, 70f),
		}, stoneColor, zIndex: -8);

		// Individual stone "slabs" (mortar seams between them)
		float[] seams = { -50f, -30f, -10f, 10f, 30f, 50f, 70f };
		foreach (float sy in seams)
		{
			AddPoly(new Vector2(0f, sy), new Vector2[]
			{
				new Vector2(-12f, -0.5f), new Vector2(12f, -0.5f),
				new Vector2( 12f,  0.5f), new Vector2(-12f, 0.5f),
			}, jointColor, zIndex: -7);
		}

		// Widened clearing near the well (y=-20 to y=20, x=-24 to x=24)
		AddPoly(new Vector2(0f, 0f), new Vector2[]
		{
			new Vector2(-24f, -20f), new Vector2(24f, -20f),
			new Vector2(24f,  20f),  new Vector2(-24f, 20f),
		}, stoneColor, zIndex: -8);
	}

	// ── South door (return to tavern) ──────────────────────────────────────────

	/// <summary>
	/// Spawns the south door frame and a <see cref="MapExit"/> that takes the
	/// player back to the MAPP tavern at the "from_garden" spawn point.
	/// </summary>
	private void SpawnSouthDoor()
	{
		var doorPos    = new Vector2(0f, WallYS);
		var frameColor = new Color(0.28f, 0.16f, 0.07f);
		var panelColor = new Color(0.35f, 0.22f, 0.10f);
		var brassColor = new Color(0.55f, 0.42f, 0.12f);

		// Door opening (darker hole in the wall)
		AddPoly(doorPos, new Vector2[]
		{
			new Vector2(-DoorHalf, -18f), new Vector2(DoorHalf, -18f),
			new Vector2(DoorHalf,   2f),  new Vector2(-DoorHalf, 2f),
		}, new Color(0.07f, 0.04f, 0.03f), zIndex: -4);

		// Door panel
		AddPoly(doorPos + new Vector2(0f, -8f), new Vector2[]
		{
			new Vector2(-13f, -10f), new Vector2(13f, -10f),
			new Vector2(13f,  10f),  new Vector2(-13f, 10f),
		}, panelColor, zIndex: -3);

		// Frame strips
		foreach (var (off, size) in new (Vector2 off, Vector2 size)[]
		{
			(new Vector2(-DoorHalf,  -8f), new Vector2(2f, 22f)),
			(new Vector2( DoorHalf,  -8f), new Vector2(2f, 22f)),
			(new Vector2(       0f, -18f), new Vector2(DoorHalf * 2f + 2f, 2f)),
		})
		{
			AddPoly(doorPos + off, new Vector2[]
			{
				new Vector2(-size.X * 0.5f, -size.Y * 0.5f),
				new Vector2( size.X * 0.5f, -size.Y * 0.5f),
				new Vector2( size.X * 0.5f,  size.Y * 0.5f),
				new Vector2(-size.X * 0.5f,  size.Y * 0.5f),
			}, frameColor, zIndex: -2);
		}

		// Handle
		AddPoly(doorPos + new Vector2(6f, -8f), new Vector2[]
		{
			new Vector2(-1.5f, -1.5f), new Vector2(1.5f, -1.5f),
			new Vector2(1.5f,   1.5f), new Vector2(-1.5f, 1.5f),
		}, brassColor, zIndex: -1);

		// Small sign above door: "THE MAPP" label plaque
		AddPoly(doorPos + new Vector2(0f, -22f), new Vector2[]
		{
			new Vector2(-10f, -3f), new Vector2(10f, -3f),
			new Vector2(10f,   3f), new Vector2(-10f, 3f),
		}, new Color(0.40f, 0.28f, 0.10f), zIndex: -2);

		if (Engine.IsEditorHint()) return;

		var exit = new MapExit();
		exit.TargetMapPath = "res://scenes/overworld/MAPP.tscn";
		exit.TargetSpawnId = "from_garden";
		exit.AutoTrigger   = false;

		var exitShape = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(DoorHalf * 2f, 24f) },
		};
		exit.AddChild(exitShape);
		AddChild(exit);
		exit.GlobalPosition = doorPos + new Vector2(0f, -8f);
	}

	// ── Wall colliders ─────────────────────────────────────────────────────────

	private void SpawnGardenWallColliders()
	{
		const float wallW = 16f;

		// North wall
		SpawnWallBox(new Vector2(0f, WallYN - wallW * 0.5f),
			new Vector2(WallX * 2f + wallW * 2f, wallW));

		// West wall
		SpawnWallBox(new Vector2(-WallX - wallW * 0.5f, 0f),
			new Vector2(wallW, WallYS - WallYN + wallW));

		// East wall
		SpawnWallBox(new Vector2(WallX + wallW * 0.5f, 0f),
			new Vector2(wallW, WallYS - WallYN + wallW));

		// South wall — west portion
		float swWidth = WallX - DoorHalf;
		SpawnWallBox(new Vector2(-(DoorHalf + swWidth * 0.5f), WallYS + wallW * 0.5f),
			new Vector2(swWidth, wallW));

		// South wall — east portion
		SpawnWallBox(new Vector2(DoorHalf + swWidth * 0.5f, WallYS + wallW * 0.5f),
			new Vector2(swWidth, wallW));
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

	// ── Fireflies ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Spawns 6 tiny glowing dots that wander randomly through the garden,
	/// each pulsing its alpha independently to create the firefly blink effect.
	/// </summary>
	private void SpawnFireflies()
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		for (int i = 0; i < 6; i++)
		{
			var startPos = new Vector2(
				rng.RandfRange(-85f, 85f),
				rng.RandfRange(-65f, 45f));

			var firefly = new Polygon2D
			{
				Color   = new Color(0.95f, 0.95f, 0.30f, 0f),
				ZIndex  = 20,
				Polygon = new Vector2[]
				{
					new Vector2( 0f, -2f),
					new Vector2( 2f,  0f),
					new Vector2( 0f,  2f),
					new Vector2(-2f,  0f),
				},
			};
			AddChild(firefly);
			firefly.GlobalPosition = startPos;

			// Independent alpha pulse — gives the intermittent glow effect
			float pulseDelay  = rng.RandfRange(0f, 1.4f);
			float pulsePeriod = rng.RandfRange(0.8f, 1.4f);
			var pulse = CreateTween().SetLoops();
			pulse.TweenProperty(firefly, "color:a", 0.85f, pulsePeriod * 0.4f)
				.SetTrans(Tween.TransitionType.Sine).SetDelay(pulseDelay);
			pulse.TweenProperty(firefly, "color:a", 0f, pulsePeriod * 0.6f)
				.SetTrans(Tween.TransitionType.Sine);

			// Random walk — continuously picks a nearby target, waits, then moves
			var capturedFirefly = firefly;
			System.Action scheduleWalk = null!;
			scheduleWalk = () =>
			{
				if (!IsInsideTree() || !IsInstanceValid(capturedFirefly)) return;
				var cur = capturedFirefly.GlobalPosition;
				float tx = Mathf.Clamp(cur.X + rng.RandfRange(-40f, 40f), -90f, 90f);
				float ty = Mathf.Clamp(cur.Y + rng.RandfRange(-30f, 30f), -70f, 50f);
				var target = new Vector2(tx, ty);

				GetTree().CreateTimer(rng.RandfRange(1.5f, 3.5f))
					.Connect("timeout", Callable.From(() =>
				{
					if (!IsInsideTree() || !IsInstanceValid(capturedFirefly)) return;
					var walk = CreateTween();
					walk.TweenProperty(capturedFirefly, "global_position", target,
						rng.RandfRange(1.5f, 3.0f))
						.SetTrans(Tween.TransitionType.Sine);
					walk.TweenCallback(Callable.From(scheduleWalk));
				}));
			};

			// Stagger startup so they don't all begin moving at once
			GetTree().CreateTimer(rng.RandfRange(0f, 2.5f))
				.Connect("timeout", Callable.From(scheduleWalk));
		}
	}

	// ── Entry fade ─────────────────────────────────────────────────────────────

	/// <summary>
	/// Black overlay that fades out on enter, giving the impression of stepping
	/// through a doorway from the brighter tavern interior.
	/// </summary>
	private void SpawnEntryFade()
	{
		var layer = new CanvasLayer { Layer = 80 };
		AddChild(layer);

		var cover = new ColorRect
		{
			Color        = new Color(0f, 0f, 0f, 0.70f),
			AnchorRight  = 1f,
			AnchorBottom = 1f,
		};
		layer.AddChild(cover);

		var t = CreateTween();
		t.TweenProperty(cover, "color:a", 0f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		t.TweenCallback(Callable.From(layer.QueueFree));
	}

	// ── Polygon helper ─────────────────────────────────────────────────────────

	private Polygon2D AddPoly(Vector2 worldPos, Vector2[] polygon, Color color, int zIndex)
	{
		var poly = new Polygon2D { Color = color, ZIndex = zIndex, Polygon = polygon };
		AddChild(poly);
		poly.GlobalPosition = worldPos;
		return poly;
	}
}
