using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A tavern chair. Set NorthFacing = true for chairs placed above a table.
/// Rebuilds visuals in the editor when NorthFacing is toggled in the Inspector.
/// </summary>
[Tool]
public partial class ChairFurniture : Node2D
{
	private bool _northFacing = false;

	[Export] public bool NorthFacing
	{
		get => _northFacing;
		set { _northFacing = value; if (IsInsideTree()) Rebuild(); }
	}

	private static readonly Color ChairSeat = new Color(0.30f, 0.18f, 0.09f);
	private static readonly Color ChairBack = new Color(0.20f, 0.12f, 0.05f);

	public override void _Ready() => Rebuild();

	private void Rebuild()
	{
		foreach (var child in GetChildren())
			child.Free();

		var body  = new StaticBody2D();
		var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(10f, 8f) } };
		body.AddChild(shape);
		AddChild(body);

		AddChild(new Polygon2D
		{
			Color   = ChairSeat,
			ZIndex  = 0,
			Polygon = new Vector2[]
			{
				new Vector2(-5f, -4f), new Vector2(5f, -4f),
				new Vector2( 5f,  4f), new Vector2(-5f,  4f),
			},
		});

		float by = _northFacing ? -4f : 4f;
		float bh = _northFacing ? -3f : 3f;
		AddChild(new Polygon2D
		{
			Color   = ChairBack,
			ZIndex  = 0,
			Polygon = new Vector2[]
			{
				new Vector2(-5f, by),      new Vector2(5f, by),
				new Vector2( 5f, by + bh), new Vector2(-5f, by + bh),
			},
		});

		AddChild(new Polygon2D
		{
			Color   = new Color(ChairSeat.R + 0.08f, ChairSeat.G + 0.05f, ChairSeat.B + 0.02f),
			ZIndex  = 0,
			Polygon = new Vector2[]
			{
				new Vector2(-4f, -3f), new Vector2(1f, -3f),
				new Vector2( 1f, -1f), new Vector2(-4f, -1f),
			},
		});
	}
}
