using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>A round bar stool.</summary>
[Tool]
public partial class BarStoolFurniture : Node2D
{
	public override void _Ready()
	{
		if (GetChildCount() > 0) return;

		var body  = new StaticBody2D();
		var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(8f, 6f) } };
		body.AddChild(shape);
		AddChild(body);

		AddChild(new Polygon2D
		{
			Color   = new Color(0.25f, 0.14f, 0.06f),
			ZIndex  = 0,
			Polygon = new Vector2[]
			{
				new Vector2(-3f, -4f), new Vector2(3f, -4f),
				new Vector2(4f, -2f),  new Vector2(4f,  2f),
				new Vector2(3f,  4f),  new Vector2(-3f, 4f),
				new Vector2(-4f, 2f),  new Vector2(-4f, -2f),
			},
		});

		AddChild(new Polygon2D
		{
			Color   = new Color(0.35f, 0.20f, 0.10f),
			ZIndex  = 0,
			Polygon = new Vector2[]
			{
				new Vector2(-2f, -2f), new Vector2(2f, -2f),
				new Vector2(2f,   0f), new Vector2(-2f, 0f),
			},
		});
	}
}
