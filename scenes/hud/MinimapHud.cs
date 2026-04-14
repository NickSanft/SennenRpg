using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Minimap overlay rendered in the top-right corner.
/// Add to the scene via OverworldBase, then call Initialise(worldBounds).
/// Dots: white = player, yellow = map exits, green = save points, cyan = NPCs.
/// Active quest count shown as a gold "!N" badge above the minimap.
/// </summary>
public partial class MinimapHud : CanvasLayer
{
	private const float Width  = 80f;
	private const float Height = 60f;
	private const float Margin = 8f;
	private const float BadgeSize = 18f;
	private const float QuestRefreshInterval = 2f;

	private MinimapCanvas _canvas = null!;
	private Panel? _questBadge;
	private Label? _questBadgeLabel;
	private float _questRefreshTimer;
	private int _lastActiveQuestCount = -1;

	public override void _Ready()
	{
		Layer = 4;
		_canvas = new MinimapCanvas();
		_canvas.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_canvas);

		BuildQuestBadge();

		// Position after entering the tree so GetViewport() is available
		Callable.From(PositionCanvas).CallDeferred();

		var qm = QuestManager.Instance;
		if (qm != null)
		{
			qm.QuestActivated        += _ => RefreshQuestBadge();
			qm.QuestCompleted        += _ => RefreshQuestBadge();
			qm.QuestReadyToComplete  += _ => RefreshQuestBadge();
		}
		RefreshQuestBadge();
	}

	public override void _Process(double delta)
	{
		_questRefreshTimer += (float)delta;
		if (_questRefreshTimer >= QuestRefreshInterval)
		{
			_questRefreshTimer = 0f;
			RefreshQuestBadge();
		}
	}

	/// <summary>Sets the world-space rectangle used to map positions onto the minimap.</summary>
	public void Initialise(Rect2 worldBounds) => _canvas.WorldBounds = worldBounds;

	private void BuildQuestBadge()
	{
		_questBadge = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		var style = new StyleBoxFlat
		{
			BgColor         = new Color(0f, 0f, 0f, 0.75f),
			BorderColor     = UiTheme.Gold,
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
			CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
		};
		_questBadge.AddThemeStyleboxOverride("panel", style);

		_questBadgeLabel = new Label
		{
			Text = "!",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Center,
			MouseFilter         = Control.MouseFilterEnum.Ignore,
		};
		_questBadgeLabel.AddThemeColorOverride("font_color", UiTheme.Gold);
		_questBadgeLabel.AddThemeFontSizeOverride("font_size", 10);
		_questBadgeLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_questBadge.AddChild(_questBadgeLabel);

		AddChild(_questBadge);
	}

	private void RefreshQuestBadge()
	{
		if (_questBadge == null || _questBadgeLabel == null) return;
		var qm = QuestManager.Instance;
		int count = qm?.GetActiveQuestIds().Count ?? 0;
		if (count == _lastActiveQuestCount) return;
		_lastActiveQuestCount = count;

		if (count <= 0)
		{
			_questBadge.Visible = false;
			return;
		}
		_questBadge.Visible = true;
		_questBadgeLabel.Text = count > 1 ? $"!{count}" : "!";
	}

	private void PositionCanvas()
	{
		var viewSize = GetViewport().GetVisibleRect().Size;
		_canvas.SetPosition(new Vector2(viewSize.X - Width - Margin, Margin));
		_canvas.SetSize(new Vector2(Width, Height));

		if (_questBadge != null)
		{
			// Sit just below the minimap, right-aligned.
			_questBadge.SetSize(new Vector2(BadgeSize, BadgeSize));
			_questBadge.SetPosition(new Vector2(
				viewSize.X - BadgeSize - Margin,
				Margin + Height + 2f));
		}
	}
}
