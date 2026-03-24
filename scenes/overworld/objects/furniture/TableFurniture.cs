using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// A tavern table built in code. Visual and collision created on _Ready.
/// The candle flame Polygon2D is added to the "candle_flame" group so
/// MAPP.cs can apply the flicker tween after the scene tree is ready.
/// </summary>
[Tool]
public partial class TableFurniture : Node2D
{
	private static readonly Color TableSurface   = new Color(0.36f, 0.22f, 0.11f);
	private static readonly Color TableHighlight = new Color(0.46f, 0.30f, 0.15f);
	private static readonly Color TableEdge      = new Color(0.20f, 0.12f, 0.06f);
	private static readonly Color CandleWax      = new Color(0.92f, 0.88f, 0.72f);
	private static readonly Color CandleFlameCol = new Color(1.00f, 0.68f, 0.12f);

	public override void _Ready()
	{
		if (GetChildCount() > 0) return;

		var body  = new StaticBody2D();
		var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(28f, 17f) } };
		body.AddChild(shape);
		body.Position = new Vector2(0f, 2.5f);
		AddChild(body);

		MakePoly(new Vector2(2f, 3f), new Vector2[]
		{
			new Vector2(-14f, -6f), new Vector2(14f, -6f),
			new Vector2(12f,  11f), new Vector2(-12f, 11f),
		}, new Color(0f, 0f, 0f, 0.35f), 0);

		MakePoly(Vector2.Zero, new Vector2[]
		{
			new Vector2(-14f,  6f), new Vector2(14f,  6f),
			new Vector2( 11f, 11f), new Vector2(-11f, 11f),
		}, TableEdge, 0);

		MakePoly(Vector2.Zero, new Vector2[]
		{
			new Vector2(-14f, -6f), new Vector2(14f, -6f),
			new Vector2( 14f,  6f), new Vector2(-14f,  6f),
		}, TableSurface, 0);

		MakePoly(Vector2.Zero, new Vector2[]
		{
			new Vector2(-13f, -5f), new Vector2(13f, -5f),
			new Vector2( 13f, -2f), new Vector2(-13f, -2f),
		}, TableHighlight, 0);

		MakePoly(new Vector2(0f, 1f), new Vector2[]
		{
			new Vector2(-1.5f, -4f), new Vector2(1.5f, -4f),
			new Vector2( 1.5f,  2f), new Vector2(-1.5f,  2f),
		}, CandleWax, 0);

		var flame = MakePoly(new Vector2(0f, 1f), new Vector2[]
		{
			new Vector2( 0f, -7f),
			new Vector2( 2f, -5f),
			new Vector2( 0f, -4f),
			new Vector2(-2f, -5f),
		}, CandleFlameCol, 0);
		flame.AddToGroup("candle_flame");
	}

	private Polygon2D MakePoly(Vector2 offset, Vector2[] polygon, Color color, int zIndex)
	{
		var poly = new Polygon2D { Color = color, ZIndex = zIndex, Polygon = polygon };
		poly.Position = offset;
		AddChild(poly);
		return poly;
	}
}
