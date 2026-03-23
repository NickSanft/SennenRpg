using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A tavern chair. Set NorthFacing = true for chairs placed above a table.
/// </summary>
public partial class ChairFurniture : Node2D
{
	[Export] public bool NorthFacing { get; set; } = false;

	private static readonly Color ChairSeat = new Color(0.30f, 0.18f, 0.09f);
	private static readonly Color ChairBack = new Color(0.20f, 0.12f, 0.05f);

	public override void _Ready()
	{
		var body  = new StaticBody2D();
		var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(10f, 8f) } };
		body.AddChild(shape);
		AddChild(body);

		AddChild(new Polygon2D
		{
			Color   = ChairSeat,
			ZIndex  = -6,
			Polygon = new Vector2[]
			{
				new Vector2(-5f, -4f), new Vector2(5f, -4f),
				new Vector2( 5f,  4f), new Vector2(-5f,  4f),
			},
		});

		float by = NorthFacing ? -4f : 4f;
		float bh = NorthFacing ? -3f : 3f;
		AddChild(new Polygon2D
		{
			Color   = ChairBack,
			ZIndex  = -6,
			Polygon = new Vector2[]
			{
				new Vector2(-5f, by),      new Vector2(5f, by),
				new Vector2( 5f, by + bh), new Vector2(-5f, by + bh),
			},
		});

		AddChild(new Polygon2D
		{
			Color   = new Color(ChairSeat.R + 0.08f, ChairSeat.G + 0.05f, ChairSeat.B + 0.02f),
			ZIndex  = -5,
			Polygon = new Vector2[]
			{
				new Vector2(-4f, -3f), new Vector2(1f, -3f),
				new Vector2( 1f, -1f), new Vector2(-4f, -1f),
			},
		});
	}
}
