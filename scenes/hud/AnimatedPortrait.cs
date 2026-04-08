using Godot;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Tiny Control widget that loops the two-frame overworld walk animation as a
/// portrait. Used by BattleHUD party cards, the EquipmentMenu portrait slot,
/// and the overworld GameHud cards so every "head" of a party member feels alive
/// instead of being frozen on frame 0.
///
/// Internally a <see cref="TextureRect"/> child whose <see cref="AtlasTexture"/>
/// region is swapped between (0, 0, 16, 16) and (16, 0, 16, 16) every
/// <see cref="FrameInterval"/> seconds. Built code-only — no .tscn — so callers
/// can `new AnimatedPortrait()` and AddChild it directly.
///
/// Pass the sheet via <see cref="SetSpriteSheet"/>; pass an empty / missing path
/// to clear the texture (the widget continues to lay out at its minimum size).
/// </summary>
public partial class AnimatedPortrait : Control
{
    [Export] public Vector2 PortraitSize { get; set; } = new(40f, 40f);
    [Export] public float   FrameInterval { get; set; } = 0.4f;

    private TextureRect _texRect = null!;
    private AtlasTexture? _frame0;
    private AtlasTexture? _frame1;
    private bool _showingFrame1;
    private float _accumulator;

    public override void _Ready()
    {
        CustomMinimumSize = PortraitSize;
        MouseFilter       = MouseFilterEnum.Ignore;

        _texRect = new TextureRect
        {
            ExpandMode          = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode         = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter       = CanvasItem.TextureFilterEnum.Nearest,
            AnchorLeft          = 0f, AnchorRight  = 1f,
            AnchorTop           = 0f, AnchorBottom = 1f,
            MouseFilter         = MouseFilterEnum.Ignore,
        };
        AddChild(_texRect);

        // If SetSpriteSheet was called before _Ready, finalise the assignment now.
        if (_frame0 != null)
            _texRect.Texture = _frame0;

        SetProcess(_frame1 != null);
    }

    /// <summary>
    /// Build the two AtlasTextures from a 32×16 sprite sheet (the standard
    /// overworld layout used by Sen / Lily / Rain). Empty or missing paths
    /// clear the portrait gracefully.
    /// </summary>
    public void SetSpriteSheet(string spriteSheetPath)
    {
        if (string.IsNullOrEmpty(spriteSheetPath) || !ResourceLoader.Exists(spriteSheetPath))
        {
            _frame0 = null;
            _frame1 = null;
            if (_texRect != null) _texRect.Texture = null;
            SetProcess(false);
            return;
        }

        var atlas = GD.Load<Texture2D>(spriteSheetPath);
        _frame0 = new AtlasTexture { Atlas = atlas, Region = new Rect2(0,  0, 16, 16) };
        _frame1 = new AtlasTexture { Atlas = atlas, Region = new Rect2(16, 0, 16, 16) };

        _showingFrame1 = false;
        _accumulator   = 0f;

        if (_texRect != null) _texRect.Texture = _frame0;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_frame0 == null || _frame1 == null || _texRect == null) return;

        _accumulator += (float)delta;
        if (_accumulator < FrameInterval) return;

        _accumulator   = 0f;
        _showingFrame1 = !_showingFrame1;
        _texRect.Texture = _showingFrame1 ? _frame1 : _frame0;
    }
}
