using Godot;

namespace SennenRpg.Core.Extensions;

/// <summary>
/// Applies or removes the palette-swap shader on any CanvasItem.
/// Passing an empty sourceColors array clears the shader material.
/// </summary>
public static class PaletteSwapHelper
{
    private const string ShaderPath = "res://assets/shaders/palette_swap.gdshader";
    private const int    MaxColors  = 32;

    public static void ApplyPalette(CanvasItem target, Color[] sourceColors, Color[] targetColors)
    {
        if (sourceColors.Length == 0)
        {
            target.Material = null;
            return;
        }

        int count = Mathf.Min(sourceColors.Length, MaxColors);

        // Shader arrays must be exactly MAX_COLORS long; pad unused slots with zeros.
        var src = new Color[MaxColors];
        var tgt = new Color[MaxColors];
        for (int i = 0; i < count; i++)
        {
            src[i] = sourceColors[i];
            tgt[i] = i < targetColors.Length ? targetColors[i] : sourceColors[i];
        }

        var mat = new ShaderMaterial();
        mat.Shader = GD.Load<Shader>(ShaderPath);
        // Unused slots have alpha=0; the shader uses that as a sentinel to skip them.
        mat.SetShaderParameter("source_colors", src);
        mat.SetShaderParameter("target_colors", tgt);
        target.Material = mat;
    }

    public static void ClearPalette(CanvasItem target) => target.Material = null;
}
