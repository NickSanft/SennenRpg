using Godot;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Custom Control that redraws the minimap every frame.
/// WorldBounds must be set via MinimapHud.Initialise() after the map is ready.
/// </summary>
public partial class MinimapCanvas : Control
{
	public Rect2 WorldBounds { get; set; }

	private static readonly Color BgColour     = new(0f, 0f, 0f, 0.7f);
	private static readonly Color BorderColour = new(1f, 1f, 1f, 0.8f);
	private static readonly Color PlayerColour = Colors.White;
	private static readonly Color ExitColour   = Colors.Yellow;
	private static readonly Color SaveColour   = new(0.3f, 1f, 0.3f);
	private static readonly Color NpcColour    = new(0.3f, 0.8f, 1f);
	private static readonly Color QuestColour  = new(1f, 0.4f, 0.2f);
	private static readonly Color StairsColour = new(1f, 0.7f, 0.3f);

	public override void _Draw()
	{
		if (Size.X < 4f || Size.Y < 4f) return; // not laid out yet

		DrawRect(new Rect2(Vector2.Zero, Size), BgColour);
		DrawRect(new Rect2(Vector2.Zero, Size), BorderColour, filled: false, width: 1f);

		var bounds = GetEffectiveBounds();
		if (!bounds.HasArea()) return;

		foreach (var node in GetTree().GetNodesInGroup("map_exits"))
		{
			if (node is Node2D n)
			{
				// Staircases (MapExit nodes with "Stairs" in name) get a distinct marker
				bool isStairs = n.Name.ToString().Contains("Stairs", System.StringComparison.OrdinalIgnoreCase);
				var col = isStairs ? StairsColour : ExitColour;
				var pos = ToMap(n.GlobalPosition, bounds);
				if (isStairs)
				{
					// Small diamond marker for stairs
					DrawCircle(pos, 3f, col);
					DrawCircle(pos, 1.5f, BgColour);
				}
				else
				{
					DrawRect(new Rect2(pos - Vector2.One * 2f, Vector2.One * 4f), col);
				}
			}
		}

		foreach (var node in GetTree().GetNodesInGroup("interactable"))
		{
			if (node is SennenRpg.Scenes.Overworld.SavePoint sp)
			{
				DrawRect(new Rect2(ToMap(sp.GlobalPosition, bounds) - Vector2.One * 2f, Vector2.One * 4f), SaveColour);
			}
			else if (node is SennenRpg.Scenes.Overworld.Npc npc)
			{
				// NPCs with active quests get a quest marker instead
				bool hasQuest = false;
				foreach (var child in npc.GetChildren())
				{
					if (child is SennenRpg.Scenes.Overworld.QuestGiver qg && qg.HasActiveQuest())
					{
						hasQuest = true;
						break;
					}
				}

				var npcPos = ToMap(npc.GlobalPosition, bounds);
				if (hasQuest)
				{
					DrawCircle(npcPos, 3f, QuestColour);
				}
				else
				{
					DrawCircle(npcPos, 2f, NpcColour);
				}
			}
		}

		foreach (var node in GetTree().GetNodesInGroup("player"))
		{
			if (node is Node2D player)
				DrawCircle(ToMap(player.GlobalPosition, bounds), 3f, PlayerColour);
		}
	}

	public override void _Process(double delta) => QueueRedraw();

	/// <summary>
	/// Returns WorldBounds if valid; otherwise computes a bounding box from all
	/// mapped objects (player, exits, interactables) with padding.
	/// </summary>
	private Rect2 GetEffectiveBounds()
	{
		if (WorldBounds.HasArea()) return WorldBounds;

		float minX = float.MaxValue, minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;
		bool found = false;

		void Expand(Vector2 p)
		{
			if (p.X < minX) minX = p.X;
			if (p.X > maxX) maxX = p.X;
			if (p.Y < minY) minY = p.Y;
			if (p.Y > maxY) maxY = p.Y;
			found = true;
		}

		foreach (var node in GetTree().GetNodesInGroup("player"))
			if (node is Node2D n) Expand(n.GlobalPosition);
		foreach (var node in GetTree().GetNodesInGroup("map_exits"))
			if (node is Node2D n) Expand(n.GlobalPosition);
		foreach (var node in GetTree().GetNodesInGroup("interactable"))
			if (node is Node2D n) Expand(n.GlobalPosition);

		if (!found) return new Rect2(-200, -200, 400, 400);

		const float Padding = 80f;
		return new Rect2(
			minX - Padding,
			minY - Padding,
			(maxX - minX) + Padding * 2f,
			(maxY - minY) + Padding * 2f
		);
	}

	private Vector2 ToMap(Vector2 worldPos, Rect2 bounds)
	{
		float nx = (worldPos.X - bounds.Position.X) / bounds.Size.X;
		float ny = (worldPos.Y - bounds.Position.Y) / bounds.Size.Y;
		float margin = Mathf.Min(2f, Size.X * 0.1f);
		return new Vector2(
			Mathf.Clamp(nx * Size.X, margin, Size.X - margin),
			Mathf.Clamp(ny * Size.Y, margin, Size.Y - margin)
		);
	}
}
