using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

// Procedural building methods split from MappGarden.cs for size management.
public partial class MappGarden
{
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

	// ── Moonflower trellis ─────────────────────────────────────────────────────

	/// <summary>
	/// Spawns a wooden lattice trellis in the north-west corner with pale-blue
	/// moonflowers at each intersection. Each flower pulses its alpha independently
	/// so the north wall shimmers with soft blue-white light.
	/// </summary>
	private void SpawnMoonflowerTrellis()
	{
		var trellisCenter = new Vector2(-72f, -55f);
		const float tw = 36f;
		const float th = 38f;
		var woodColor  = new Color(0.32f, 0.20f, 0.08f);
		var frameColor = new Color(0.24f, 0.14f, 0.05f);

		// Dark foliage backing (makes the trellis read against the stone wall)
		AddPoly(trellisCenter, new Vector2[]
		{
			new Vector2(-tw * 0.5f - 3f, -th * 0.5f - 3f),
			new Vector2( tw * 0.5f + 3f, -th * 0.5f - 3f),
			new Vector2( tw * 0.5f + 3f,  th * 0.5f + 3f),
			new Vector2(-tw * 0.5f - 3f,  th * 0.5f + 3f),
		}, new Color(0.08f, 0.15f, 0.06f), zIndex: -6);

		// Outer frame (four border strips)
		foreach (var (off, size) in new (Vector2 off, Vector2 size)[]
		{
			(new Vector2(0f,        -th * 0.5f), new Vector2(tw + 6f, 3f)),
			(new Vector2(0f,         th * 0.5f), new Vector2(tw + 6f, 3f)),
			(new Vector2(-tw * 0.5f, 0f),        new Vector2(3f, th + 6f)),
			(new Vector2( tw * 0.5f, 0f),        new Vector2(3f, th + 6f)),
		})
		{
			AddPoly(trellisCenter + off, new Vector2[]
			{
				new Vector2(-size.X * 0.5f, -size.Y * 0.5f),
				new Vector2( size.X * 0.5f, -size.Y * 0.5f),
				new Vector2( size.X * 0.5f,  size.Y * 0.5f),
				new Vector2(-size.X * 0.5f,  size.Y * 0.5f),
			}, frameColor, zIndex: -4);
		}

		// Lattice: 3 columns × 3 rows = 9 intersections
		int   cols    = 3, rows = 3;
		float colStep = tw / (cols + 1);
		float rowStep = th / (rows + 1);

		for (int c = 1; c <= cols; c++)
		{
			float x = -tw * 0.5f + colStep * c;
			AddPoly(trellisCenter + new Vector2(x, 0f), new Vector2[]
			{
				new Vector2(-1f, -th * 0.5f), new Vector2(1f, -th * 0.5f),
				new Vector2(1f,  th * 0.5f),  new Vector2(-1f, th * 0.5f),
			}, woodColor, zIndex: -4);
		}
		for (int r = 1; r <= rows; r++)
		{
			float y = -th * 0.5f + rowStep * r;
			AddPoly(trellisCenter + new Vector2(0f, y), new Vector2[]
			{
				new Vector2(-tw * 0.5f, -1f), new Vector2(tw * 0.5f, -1f),
				new Vector2(tw * 0.5f,   1f), new Vector2(-tw * 0.5f, 1f),
			}, woodColor, zIndex: -3);
		}

		// Moonflowers at each grid intersection + a few extras on the frame
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		var flowerPositions = new System.Collections.Generic.List<Vector2>();
		for (int c = 1; c <= cols; c++)
			for (int r = 1; r <= rows; r++)
				flowerPositions.Add(trellisCenter + new Vector2(
					-tw * 0.5f + colStep * c,
					-th * 0.5f + rowStep * r));

		// Extras scattered on the frame edges
		flowerPositions.Add(trellisCenter + new Vector2(-tw * 0.5f + 1f, -th * 0.25f));
		flowerPositions.Add(trellisCenter + new Vector2( tw * 0.5f - 1f,  th * 0.15f));
		flowerPositions.Add(trellisCenter + new Vector2(-tw * 0.20f,     -th * 0.5f + 1f));
		flowerPositions.Add(trellisCenter + new Vector2( tw * 0.15f,      th * 0.42f));

		foreach (var fPos in flowerPositions)
		{
			float size   = rng.RandfRange(2.5f, 4.0f);
			float initA  = 0.25f; // plan: 0.25 → 0.65 → 0.25
			var flower = new Polygon2D
			{
				Color   = new Color(0.78f, 0.88f, 1.00f, initA),
				ZIndex  = -2,
				Polygon = new Vector2[]
				{
					new Vector2( 0f,    -size),
					new Vector2( size,   0f),
					new Vector2( 0f,     size * 0.65f),
					new Vector2(-size,   0f),
				},
			};
			AddChild(flower);
			flower.GlobalPosition = fPos;

			float delay  = rng.RandfRange(0f, 2.5f);
			float period = rng.RandfRange(2.5f, 4.0f);
			var pulse = CreateTween().SetLoops();
			pulse.TweenProperty(flower, "color:a", 0.65f, period * 0.5f)
				.SetTrans(Tween.TransitionType.Sine).SetDelay(delay);
			pulse.TweenProperty(flower, "color:a", 0.25f, period * 0.5f)
				.SetTrans(Tween.TransitionType.Sine);
		}
	}

	// ── Birdbath ───────────────────────────────────────────────────────────────

	/// <summary>
	/// Spawns a stone birdbath with two perched sparrows. When the player walks
	/// within 60px the birds startle and fly off; they return after 12 seconds.
	/// </summary>
	private void SpawnBirdbath()
	{
		var bathPos    = new Vector2(30f, -15f);
		var stoneColor = new Color(0.52f, 0.48f, 0.43f);
		var capColor   = new Color(0.60f, 0.55f, 0.50f);
		var waterColor = new Color(0.30f, 0.42f, 0.55f, 0.75f);

		// Base foot
		AddPoly(bathPos + new Vector2(0f, 12f), new Vector2[]
		{
			new Vector2(-6f, -1f), new Vector2(6f, -1f),
			new Vector2(7f,  2f),  new Vector2(-7f, 2f),
		}, stoneColor, zIndex: 2);

		// Pedestal column
		AddPoly(bathPos + new Vector2(0f, 4f), new Vector2[]
		{
			new Vector2(-2.5f, -10f), new Vector2(2.5f, -10f),
			new Vector2( 4.0f,  0f),  new Vector2(-4.0f, 0f),
		}, stoneColor, zIndex: 2);

		// Basin (8-sided)
		const int bpts = 8;
		var basinPoly = new Vector2[bpts];
		var waterPoly = new Vector2[bpts];
		for (int i = 0; i < bpts; i++)
		{
			float a = (Mathf.Tau / bpts) * i - Mathf.Pi / bpts;
			basinPoly[i] = new Vector2(Mathf.Cos(a) * 11f, Mathf.Sin(a) * 5.5f);
			waterPoly[i] = new Vector2(Mathf.Cos(a) *  8f, Mathf.Sin(a) * 3.5f);
		}
		AddPoly(bathPos, basinPoly, stoneColor, zIndex: 3);
		AddPoly(bathPos, basinPoly, capColor,   zIndex: 3); // rim highlight via separate draw? reuse stoneColor
		// Rim highlight strip
		AddPoly(bathPos + new Vector2(0f, -4f), new Vector2[]
		{
			new Vector2(-11f, -1f), new Vector2(11f, -1f),
			new Vector2( 11f,  1f), new Vector2(-11f, 1f),
		}, capColor, zIndex: 4);
		// Water surface
		AddPoly(bathPos, waterPoly, waterColor, zIndex: 4);

		// Birds and interaction — play mode only
		if (Engine.IsEditorHint()) return;

		SpawnBirdbathBirds(bathPos);
	}

	private void SpawnBirdbathBirds(Vector2 bathPos)
	{
		const float rimOff = 9f;
		var birdColor = new Color(0.18f, 0.14f, 0.12f);
		var rimY      = bathPos.Y - 4f;

		Polygon2D? birdW = MakeBird(bathPos + new Vector2(-rimOff, -4f), birdColor);
		Polygon2D? birdE = MakeBird(bathPos + new Vector2( rimOff, -4f), birdColor);
		StartBirdBob(birdW, rimY);
		StartBirdBob(birdE, rimY);

		// Player proximity detector
		var detector = new Area2D();
		detector.AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = 60f },
		});
		AddChild(detector);
		detector.GlobalPosition = bathPos;

		bool flownOff = false;
		detector.BodyEntered += body =>
		{
			if (flownOff || !body.IsInGroup("player")) return;
			flownOff = true;

			FlyBirdOff(birdW, -1f);
			FlyBirdOff(birdE, +1f);
			birdW = null;
			birdE = null;

			// Respawn after 12 s
			GetTree().CreateTimer(12f).Connect("timeout", Callable.From(() =>
			{
				if (!IsInsideTree()) return;
				flownOff = false;
				birdW = MakeBird(bathPos + new Vector2(-rimOff, -4f), birdColor, fadeIn: true);
				birdE = MakeBird(bathPos + new Vector2( rimOff, -4f), birdColor, fadeIn: true);
				StartBirdBob(birdW, rimY);
				StartBirdBob(birdE, rimY);
			}));
		};
	}

	private Polygon2D MakeBird(Vector2 pos, Color color, bool fadeIn = false)
	{
		var bird = new Polygon2D
		{
			Color    = color,
			ZIndex   = 10,
			Modulate = new Color(1f, 1f, 1f, fadeIn ? 0f : 1f),
			Polygon  = new Vector2[]
			{
				new Vector2( 0f, -3f),
				new Vector2( 3f,  0f),
				new Vector2( 0f,  1.5f),
				new Vector2(-3f,  0f),
			},
		};
		AddChild(bird);
		bird.GlobalPosition = pos;

		if (fadeIn)
		{
			var t = CreateTween();
			t.TweenProperty(bird, "modulate:a", 1f, 0.5f)
				.SetTrans(Tween.TransitionType.Sine);
		}
		return bird;
	}

	private void StartBirdBob(Polygon2D? bird, float baseY)
	{
		if (bird == null) return;
		var bob = CreateTween().SetLoops();
		bob.TweenProperty(bird, "global_position:y", baseY - 1.5f, 0.7f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		bob.TweenProperty(bird, "global_position:y", baseY, 0.7f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	}

	private void FlyBirdOff(Polygon2D? bird, float dirX)
	{
		if (bird == null || !IsInstanceValid(bird)) return;
		var fly = CreateTween();
		fly.TweenProperty(bird, "global_position",
			bird.GlobalPosition + new Vector2(dirX * 65f, -75f), 0.30f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		fly.Parallel()
		   .TweenProperty(bird, "modulate:a", 0f, 0.18f)
		   .SetDelay(0.12f).SetTrans(Tween.TransitionType.Linear);
		fly.TweenCallback(Callable.From(bird.QueueFree));
	}

	// ── Hanging lanterns ────────────────────────────────────────────────────────

	/// <summary>
	/// Spawns 2 wall-mounted lanterns with PointLight2D glows that flicker
	/// independently, warming the garden corners.
	/// </summary>
	private void SpawnLanterns()
	{
		// (position, dirX) — dirX says which wall face the hook is on (+1 right side, -1 left)
		var lanternDefs = new (Vector2 pos, float wallFace)[]
		{
			(new Vector2(-98f, -52f), +1f), // west wall, upper half
			(new Vector2( 98f, -36f), -1f), // east wall, mid height
		};

		foreach (var (pos, wallFace) in lanternDefs)
		{
			SpawnLantern(pos, wallFace);
		}
	}

	private void SpawnLantern(Vector2 pos, float wallFace)
	{
		var ironColor  = new Color(0.18f, 0.14f, 0.10f);
		var glassColor = new Color(0.95f, 0.78f, 0.30f, 0.60f);
		var warmColor  = new Color(1.00f, 0.72f, 0.24f);

		// Wall hook (small right-angle iron bracket)
		float hx = wallFace * 5f;
		AddPoly(pos + new Vector2(hx, -10f), new Vector2[]
		{
			new Vector2(-1f,  0f), new Vector2(1f,  0f),
			new Vector2(1f,  6f),  new Vector2(-1f, 6f),
		}, ironColor, zIndex: 2);
		AddPoly(pos + new Vector2(hx * 0.5f, -10f), new Vector2[]
		{
			new Vector2(-2f, -1f), new Vector2(2f, -1f),
			new Vector2(2f,   1f), new Vector2(-2f, 1f),
		}, ironColor, zIndex: 2);

		// Chain (two thin segments)
		for (int i = 0; i < 2; i++)
		{
			AddPoly(pos + new Vector2(0f, -9f + i * 3f), new Vector2[]
			{
				new Vector2(-0.8f, 0f), new Vector2(0.8f,  0f),
				new Vector2(0.8f,  2f), new Vector2(-0.8f, 2f),
			}, ironColor, zIndex: 2);
		}

		// Lantern body — outer frame
		AddPoly(pos, new Vector2[]
		{
			new Vector2(-5f, -7f), new Vector2(5f, -7f),
			new Vector2(5f,   7f), new Vector2(-5f, 7f),
		}, ironColor, zIndex: 3);

		// Glass panels (two visible faces)
		AddPoly(pos, new Vector2[]
		{
			new Vector2(-4f, -6f), new Vector2(4f, -6f),
			new Vector2(4f,   6f), new Vector2(-4f, 6f),
		}, glassColor, zIndex: 4);

		// Inner flame glow dot
		AddPoly(pos + new Vector2(0f, -1f), new Vector2[]
		{
			new Vector2( 0f, -2f),
			new Vector2( 2f,  0f),
			new Vector2( 0f,  2f),
			new Vector2(-2f,  0f),
		}, warmColor, zIndex: 5);

		// PointLight2D (with a radial gradient texture)
		var gradient = new Gradient();
		gradient.SetColor(0, new Color(warmColor.R, warmColor.G, warmColor.B, 1f));
		gradient.SetColor(1, new Color(warmColor.R, warmColor.G, warmColor.B, 0f));
		var lightTex = new GradientTexture2D
		{
			Gradient = gradient,
			Fill     = GradientTexture2D.FillEnum.Radial,
			Width    = 64,
			Height   = 64,
		};
		var light = new PointLight2D
		{
			Color        = warmColor,
			Energy       = 0.55f,
			Texture      = lightTex,
			TextureScale = 2.2f,
			ZIndex       = 6,
		};
		AddChild(light);
		light.GlobalPosition = pos;

		// Gentle flicker tween — offset slightly per lantern so they don't pulse together
		float baseEnergy = 0.55f;
		float period     = 0.50f + GD.Randf() * 0.25f;
		var   flicker    = CreateTween().SetLoops();
		flicker.TweenProperty(light, "energy", baseEnergy + 0.18f, period)
			.SetTrans(Tween.TransitionType.Sine);
		flicker.TweenProperty(light, "energy", baseEnergy - 0.15f, period * 0.7f)
			.SetTrans(Tween.TransitionType.Sine);
		flicker.TweenProperty(light, "energy", baseEnergy + 0.10f, period * 0.5f)
			.SetTrans(Tween.TransitionType.Sine);
		flicker.TweenProperty(light, "energy", baseEnergy - 0.08f, period)
			.SetTrans(Tween.TransitionType.Sine);
	}

	// ── Interactables ─────────────────────────────────────────────────────────

	/// <summary>Wishing well in the central clearing.</summary>
	private void SpawnWell()
	{
		if (Engine.IsEditorHint()) return;
		var well = new GardenWell();
		AddChild(well);
		well.GlobalPosition = new Vector2(0f, -30f);
	}

	/// <summary>Stone bench along the south-west edge.</summary>
	private void SpawnBench()
	{
		if (Engine.IsEditorHint()) return;
		var bench = new GardenBench();
		AddChild(bench);
		bench.GlobalPosition = new Vector2(-50f, 68f);
	}

	/// <summary>Beehive in the north-east corner.</summary>
	private void SpawnHive()
	{
		if (Engine.IsEditorHint()) return;
		var hive = new GardenBeehive();
		AddChild(hive);
		hive.GlobalPosition = new Vector2(97f, -62f);
	}

	/// <summary>Robed stone figure at the north end of the flagstone path. Turns on first interact.</summary>
	private void SpawnStatue()
	{
		if (Engine.IsEditorHint()) return;

		var statue = new GardenStatue();
		AddChild(statue);
		statue.GlobalPosition = new Vector2(0f, -62f);
	}

	/// <summary>
	/// Five small terracotta pots along the east wall, with a readable sign
	/// label above the cluster.
	/// </summary>
	private void SpawnHerbGarden()
	{
		var potColor  = new Color(0.60f, 0.30f, 0.12f);
		var soilColor = new Color(0.28f, 0.18f, 0.09f);
		var plantColor = new Color(0.22f, 0.52f, 0.18f);

		float baseX = 87f;
		float baseY = 5f;

		for (int i = 0; i < 5; i++)
		{
			float px = baseX;
			float py = baseY + (i - 2) * 14f;
			var center = new Vector2(px, py);

			// Pot body
			AddPoly(center, new Vector2[]
			{
				new Vector2(-3f,  0f), new Vector2(3f,  0f),
				new Vector2(4f,   6f), new Vector2(-4f, 6f),
			}, potColor, zIndex: 1);
			// Pot rim
			AddPoly(center + new Vector2(0f, -0.5f), new Vector2[]
			{
				new Vector2(-3.5f, -0.5f), new Vector2(3.5f, -0.5f),
				new Vector2( 3.5f,  0.5f), new Vector2(-3.5f, 0.5f),
			}, new Color(0.70f, 0.40f, 0.18f), zIndex: 2);
			// Soil
			AddPoly(center + new Vector2(0f, 1f), new Vector2[]
			{
				new Vector2(-2.8f, -1f), new Vector2(2.8f, -1f),
				new Vector2( 2.8f,  1f), new Vector2(-2.8f, 1f),
			}, soilColor, zIndex: 2);
			// Plant wisps
			AddPoly(center + new Vector2(-1.5f, -2f), new Vector2[]
			{
				new Vector2(0f, -4f), new Vector2(1f, 0f),
				new Vector2(0f,  1f), new Vector2(-1f, 0f),
			}, plantColor, zIndex: 3);
			AddPoly(center + new Vector2(1.5f, -3f), new Vector2[]
			{
				new Vector2(0f, -5f), new Vector2(1f, 0f),
				new Vector2(0f,  1f), new Vector2(-1f, 0f),
			}, plantColor, zIndex: 3);
		}

		if (Engine.IsEditorHint()) return;

		var sign = new GardenSign(
			"Herb Garden",
			new[]
			{
				"Rosemary. Good for memory, allegedly.",
				"Thyme. It's just thyme.",
				"Something blue. Wendell says not to touch it.",
				"Mint. Aggressively mint.",
				"????? (the label has fallen off)",
			});
		AddChild(sign);
		sign.GlobalPosition = new Vector2(baseX, baseY - 32f);
	}

	/// <summary>
	/// A low earthy compost mound in the south-east corner with a readable sign.
	/// </summary>
	private void SpawnCompostHeap()
	{
		var moundColor = new Color(0.28f, 0.18f, 0.09f);
		var darkColor  = new Color(0.20f, 0.12f, 0.06f);
		var heapCenter = new Vector2(85f, 55f);

		// Main mound (irregular blob)
		AddPoly(heapCenter, new Vector2[]
		{
			new Vector2(-15f,  2f), new Vector2(-8f,  -5f),
			new Vector2(  0f, -7f), new Vector2( 8f,  -5f),
			new Vector2( 14f,  1f), new Vector2( 10f,  7f),
			new Vector2( -10f, 7f),
		}, moundColor, zIndex: 1);

		// Dark shadow on mound
		AddPoly(heapCenter + new Vector2(3f, 2f), new Vector2[]
		{
			new Vector2(-8f, -3f), new Vector2(8f, -3f),
			new Vector2( 8f,  4f), new Vector2(-8f, 4f),
		}, darkColor, zIndex: 2);

		// Tiny decomposing bits on top
		var rng = new RandomNumberGenerator();
		rng.Seed = 42;
		for (int i = 0; i < 5; i++)
		{
			float bx = rng.RandfRange(-10f, 10f);
			float by = rng.RandfRange(-5f, 2f);
			AddPoly(heapCenter + new Vector2(bx, by), new Vector2[]
			{
				new Vector2(-1.5f, -1f), new Vector2(1.5f, -1f),
				new Vector2( 1.5f,  1f), new Vector2(-1.5f, 1f),
			}, new Color(0.35f, 0.55f, 0.20f, 0.70f), zIndex: 3);
		}

		if (Engine.IsEditorHint()) return;

		var sign = new GardenSign(
			"Compost",
			new[]
			{
				"Wendell maintains this with great ceremony.",
				"It smells earthy.",
				"Very earthy.",
				"You decide not to think too hard about what's in there.",
			});
		AddChild(sign);
		sign.GlobalPosition = heapCenter + new Vector2(0f, -20f);
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

// ── Inline helper: garden sign ────────────────────────────────────────────────

/// <summary>
/// A small readable sign for decorative props in the garden (herb pots, compost).
/// Implements <see cref="IInteractable"/> so the player's interact system picks it up.
/// </summary>
public partial class GardenSign : Area2D, IInteractable
{
	private readonly string   _title;
	private readonly string[] _lines;
	private InteractPromptBubble _prompt = null!;

	public GardenSign(string title, string[] lines)
	{
		_title = title;
		_lines = lines;
	}

	public override void _Ready()
	{
		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(24f, 20f) },
		});

		// Small sign visual
		var signColor  = new Color(0.32f, 0.20f, 0.08f);
		var textColor  = new Color(0.70f, 0.60f, 0.40f);

		// Sign board
		var board = new Polygon2D
		{
			Color   = signColor,
			ZIndex  = 5,
			Polygon = new Vector2[]
			{
				new Vector2(-10f, -5f), new Vector2(10f, -5f),
				new Vector2( 10f,  5f), new Vector2(-10f, 5f),
			},
		};
		AddChild(board);

		// Post
		var post = new Polygon2D
		{
			Color   = signColor,
			ZIndex  = 4,
			Polygon = new Vector2[]
			{
				new Vector2(-1f, 4f), new Vector2(1f, 4f),
				new Vector2(1f, 10f), new Vector2(-1f, 10f),
			},
		};
		AddChild(post);

		// Title text on sign (two-pixel stripe to suggest letters)
		var stripe = new Polygon2D
		{
			Color   = textColor,
			ZIndex  = 6,
			Polygon = new Vector2[]
			{
				new Vector2(-7f, -2f), new Vector2(7f, -2f),
				new Vector2( 7f,  2f), new Vector2(-7f, 2f),
			},
		};
		AddChild(stripe);

		_prompt = new InteractPromptBubble($"[Z] Read");
		_prompt.Position = new Vector2(0f, -18f);
		AddChild(_prompt);
	}

	public string GetInteractPrompt() => "[Z] Read";
	public void   ShowPrompt()        => _prompt.ShowBubble();
	public void   HidePrompt()        => _prompt.HideBubble();

	public void Interact(Node player)
	{
		var popup = new SignReaderPopup(_title, _lines);
		GetTree().CurrentScene.AddChild(popup);
	}
}
