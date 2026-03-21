using Godot;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Minimap overlay rendered in the top-right corner.
/// Add to the scene via OverworldBase, then call Initialise(worldBounds).
/// Dots: white = player, yellow = map exits, green = save points, cyan = NPCs.
/// </summary>
public partial class MinimapHud : CanvasLayer
{
	private const float Width  = 80f;
	private const float Height = 60f;
	private const float Margin = 8f;

	private MinimapCanvas _canvas = null!;

	public override void _Ready()
	{
		Layer = 4;
		_canvas = new MinimapCanvas();
		_canvas.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_canvas);
		// Position after entering the tree so GetViewport() is available
		Callable.From(PositionCanvas).CallDeferred();
	}

	/// <summary>Sets the world-space rectangle used to map positions onto the minimap.</summary>
	public void Initialise(Rect2 worldBounds) => _canvas.WorldBounds = worldBounds;

	private void PositionCanvas()
	{
		var viewSize = GetViewport().GetVisibleRect().Size;
		_canvas.SetPosition(new Vector2(viewSize.X - Width - Margin, Margin));
		_canvas.SetSize(new Vector2(Width, Height));
	}
}
