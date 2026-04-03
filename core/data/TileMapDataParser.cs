using System;
using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Core.Data;

/// <summary>
/// Represents a single tile entry from a Godot TileMapLayer's PackedByteArray tile_map_data.
/// </summary>
public readonly record struct TileEntry(
    short GridX,
    short GridY,
    ushort SourceId,
    short AtlasX,
    short AtlasY,
    ushort AlternativeTile = 0);

/// <summary>
/// Parses and encodes Godot 4 TileMapLayer tile_map_data (PackedByteArray stored as base64 in .tscn).
///
/// Binary format:
///   2-byte header (always 0x0000)
///   Then 12 bytes per tile:
///     int16  GridX
///     int16  GridY
///     uint16 SourceId
///     int16  AtlasX
///     int16  AtlasY
///     uint16 AlternativeTile
/// </summary>
public static class TileMapDataParser
{
    private const int HeaderSize = 2;
    private const int EntrySize = 12;

    /// <summary>
    /// Decode a base64-encoded tile_map_data PackedByteArray into tile entries.
    /// </summary>
    public static List<TileEntry> Decode(string base64)
    {
        byte[] data = Convert.FromBase64String(base64);
        if (data.Length < HeaderSize)
            return new List<TileEntry>();

        int entryCount = (data.Length - HeaderSize) / EntrySize;
        var tiles = new List<TileEntry>(entryCount);

        for (int i = 0; i < entryCount; i++)
        {
            int offset = HeaderSize + i * EntrySize;
            short gx = BitConverter.ToInt16(data, offset);
            short gy = BitConverter.ToInt16(data, offset + 2);
            ushort src = BitConverter.ToUInt16(data, offset + 4);
            short ax = BitConverter.ToInt16(data, offset + 6);
            short ay = BitConverter.ToInt16(data, offset + 8);
            ushort alt = BitConverter.ToUInt16(data, offset + 10);
            tiles.Add(new TileEntry(gx, gy, src, ax, ay, alt));
        }

        return tiles;
    }

    /// <summary>
    /// Encode tile entries back into a base64 string for tile_map_data.
    /// </summary>
    public static string Encode(IReadOnlyList<TileEntry> tiles)
    {
        byte[] data = new byte[HeaderSize + tiles.Count * EntrySize];
        // Header is 0x0000 (default)

        for (int i = 0; i < tiles.Count; i++)
        {
            int offset = HeaderSize + i * EntrySize;
            var t = tiles[i];
            BitConverter.TryWriteBytes(data.AsSpan(offset), t.GridX);
            BitConverter.TryWriteBytes(data.AsSpan(offset + 2), t.GridY);
            BitConverter.TryWriteBytes(data.AsSpan(offset + 4), t.SourceId);
            BitConverter.TryWriteBytes(data.AsSpan(offset + 6), t.AtlasX);
            BitConverter.TryWriteBytes(data.AsSpan(offset + 8), t.AtlasY);
            BitConverter.TryWriteBytes(data.AsSpan(offset + 10), t.AlternativeTile);
        }

        return Convert.ToBase64String(data);
    }

    /// <summary>
    /// Given a set of atlas coordinates that are classified as walls,
    /// split a list of tiles into (groundTiles, wallTiles).
    /// </summary>
    public static (List<TileEntry> Ground, List<TileEntry> Walls) SplitByClassification(
        IReadOnlyList<TileEntry> tiles,
        HashSet<(short AtlasX, short AtlasY)> wallAtlasCoords)
    {
        var ground = new List<TileEntry>();
        var walls = new List<TileEntry>();

        foreach (var tile in tiles)
        {
            if (wallAtlasCoords.Contains((tile.AtlasX, tile.AtlasY)))
                walls.Add(tile);
            else
                ground.Add(tile);
        }

        return (ground, walls);
    }

    /// <summary>
    /// Returns the distinct set of atlas coordinates used in a tile list.
    /// </summary>
    public static HashSet<(short AtlasX, short AtlasY)> GetUniqueAtlasCoords(IReadOnlyList<TileEntry> tiles)
    {
        return tiles.Select(t => (t.AtlasX, t.AtlasY)).ToHashSet();
    }
}
