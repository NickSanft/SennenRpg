using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

/// <summary>
/// Persistent full-screen post-processing overlay.
/// Applies colorblind correction and high-contrast shader effects to every pixel
/// rendered beneath this CanvasLayer — world tiles, sprites, HUDs, menus.
///
/// Layer 99: above all game content, below SceneTransition (100).
/// Registered as an autoload so it persists across scene changes.
/// Hidden when neither effect is active — zero GPU cost at rest.
/// </summary>
public partial class AccessibilityOverlay : CanvasLayer
{
	public static AccessibilityOverlay Instance { get; private set; } = null!;

	private const string ShaderPath = "res://assets/shaders/accessibility.gdshader";

	private ColorRect?      _rect;
	private ShaderMaterial? _material;

	public override void _Ready()
	{
		Instance    = this;
		Layer       = 99;
		ProcessMode = ProcessModeEnum.Always;

		if (!ResourceLoader.Exists(ShaderPath))
		{
			GD.PushWarning("[AccessibilityOverlay] Shader not found at: " + ShaderPath);
			return;
		}

		var shader = GD.Load<Shader>(ShaderPath);
		_material  = new ShaderMaterial { Shader = shader };

		_rect = new ColorRect
		{
			// Cover the full screen; Godot sets the actual size via anchor
			AnchorRight  = 1f,
			AnchorBottom = 1f,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
			Material     = _material,
			Visible      = false, // hidden until an effect is active
		};
		AddChild(_rect);
	}

	/// <summary>
	/// Updates shader parameters and shows/hides the overlay.
	/// Called automatically by <see cref="SettingsManager.Apply"/>.
	/// </summary>
	public void Apply(ColorblindMode colorblind, bool highContrast)
	{
		if (_rect == null || _material == null) return;

		bool active    = colorblind != ColorblindMode.Normal || highContrast;
		_rect.Visible  = active;
		if (!active) return;

		_material.SetShaderParameter("colorblind_mode", (int)colorblind);
		_material.SetShaderParameter("high_contrast",   highContrast);
	}
}
