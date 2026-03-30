using Godot;
using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Core.Data;

/// <summary>
/// Loads one or more sprite textures and returns all unique opaque pixel colors,
/// sorted by frequency (most common first). Colors are normalized to 8-bit precision
/// so minor float rounding differences in the same logical color collapse to one entry.
/// </summary>
public static class SpriteColorExtractor
{
    public static Color[] ExtractUniqueColors(params string[] texturePaths)
    {
        var counts = new Dictionary<Color, int>();

        foreach (string path in texturePaths)
        {
            if (!ResourceLoader.Exists(path)) continue;

            var tex = GD.Load<Texture2D>(path);
            var img = tex.GetImage();
            img.Convert(Image.Format.Rgba8);

            for (int y = 0; y < img.GetHeight(); y++)
            {
                for (int x = 0; x < img.GetWidth(); x++)
                {
                    var c = img.GetPixel(x, y);
                    if (c.A < 0.5f) continue;

                    // Round to 8-bit precision so near-identical colours collapse
                    c = new Color(
                        Mathf.Round(c.R * 255f) / 255f,
                        Mathf.Round(c.G * 255f) / 255f,
                        Mathf.Round(c.B * 255f) / 255f,
                        1f
                    );
                    counts[c] = counts.GetValueOrDefault(c, 0) + 1;
                }
            }
        }

        return counts.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();
    }
}
