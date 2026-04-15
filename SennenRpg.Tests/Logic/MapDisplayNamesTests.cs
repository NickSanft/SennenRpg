using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class MapDisplayNamesTests
{
	// ── ForId ─────────────────────────────────────────────────────────────────

	[TestCase("mapp_tavern",     "Mapp Tavern")]
	[TestCase("mellyr_outpost",  "Mellyr Outpost")]
	[TestCase("world_map",       "World Map")]
	[TestCase("dungeon_floor1",  "Dungeon - Floor 1")]
	[TestCase("dungeon_floor2",  "Dungeon - Floor 2")]
	[TestCase("dungeon_floor3",  "Dungeon - Floor 3")]
	public void ForId_KnownIds_ReturnsExpectedName(string id, string expected)
	{
		Assert.That(MapDisplayNames.ForId(id), Is.EqualTo(expected));
	}

	[Test]
	public void ForId_CaseInsensitive()
	{
		Assert.That(MapDisplayNames.ForId("MAPP_TAVERN"), Is.EqualTo("Mapp Tavern"));
		Assert.That(MapDisplayNames.ForId("Mapp_Tavern"), Is.EqualTo("Mapp Tavern"));
	}

	[Test]
	public void ForId_UnknownId_PrettifiesUnderscores()
	{
		Assert.That(MapDisplayNames.ForId("test_room"),   Is.EqualTo("Test Room"));
		Assert.That(MapDisplayNames.ForId("some_cool_area"), Is.EqualTo("Some Cool Area"));
	}

	[Test]
	public void ForId_UnknownId_PrettifiesDashes()
	{
		Assert.That(MapDisplayNames.ForId("hidden-grove"), Is.EqualTo("Hidden Grove"));
	}

	[Test]
	public void ForId_NullOrEmpty_ReturnsUnknown()
	{
		Assert.That(MapDisplayNames.ForId(null),  Is.EqualTo("Unknown"));
		Assert.That(MapDisplayNames.ForId(""),    Is.EqualTo("Unknown"));
		Assert.That(MapDisplayNames.ForId("   "), Is.EqualTo("Unknown"));
	}

	[Test]
	public void ForId_KnownIdsAlwaysNonEmpty()
	{
		string[] knownIds = {
			"mapp_tavern", "mellyr_outpost", "world_map",
			"dungeon_floor1", "dungeon_floor2", "dungeon_floor3",
			"Dungeon - Floor 1", "Dungeon - Floor 2", "Dungeon - Floor 3",
		};
		foreach (var id in knownIds)
		{
			var name = MapDisplayNames.ForId(id);
			Assert.That(name, Is.Not.Null.And.Not.Empty, $"id={id}");
			Assert.That(name, Is.Not.EqualTo("Unknown"), $"id={id}");
		}
	}

	// ── ForPath ───────────────────────────────────────────────────────────────

	[TestCase("res://scenes/overworld/MAPP.tscn",                          "Mapp Tavern")]
	[TestCase("res://scenes/overworld/WorldMap.tscn",                      "World Map")]
	[TestCase("res://scenes/overworld/maps/mellyr/MellyrOutpost.tscn",     "Mellyr Outpost")]
	[TestCase("res://scenes/overworld/maps/dungeon/DungeonFloor1.tscn",    "Dungeon - Floor 1")]
	[TestCase("res://scenes/overworld/maps/dungeon/DungeonFloor2.tscn",    "Dungeon - Floor 2")]
	[TestCase("res://scenes/overworld/maps/dungeon/DungeonFloor3.tscn",    "Dungeon - Floor 3")]
	public void ForPath_KnownPaths_ReturnsExpectedName(string path, string expected)
	{
		Assert.That(MapDisplayNames.ForPath(path), Is.EqualTo(expected));
	}

	[Test]
	public void ForPath_StemMatchesKnownId()
	{
		// "mapp_tavern.tscn" stem "mapp_tavern" hits the ById table
		Assert.That(MapDisplayNames.ForPath("res://scenes/mapp_tavern.tscn"),
			Is.EqualTo("Mapp Tavern"));
	}

	[Test]
	public void ForPath_UnknownStem_Prettified()
	{
		Assert.That(MapDisplayNames.ForPath("res://scenes/test_room.tscn"),
			Is.EqualTo("Test Room"));
	}

	[Test]
	public void ForPath_NoDirectory_UsesFilename()
	{
		Assert.That(MapDisplayNames.ForPath("MAPP.tscn"),         Is.EqualTo("Mapp Tavern"));
		Assert.That(MapDisplayNames.ForPath("custom_place.tscn"), Is.EqualTo("Custom Place"));
	}

	[Test]
	public void ForPath_NoExtension_UsesWholeStem()
	{
		Assert.That(MapDisplayNames.ForPath("MAPP"),       Is.EqualTo("Mapp Tavern"));
		Assert.That(MapDisplayNames.ForPath("some_area"),  Is.EqualTo("Some Area"));
	}

	[Test]
	public void ForPath_BackslashSeparators_HandledToo()
	{
		Assert.That(MapDisplayNames.ForPath(@"C:\scenes\MAPP.tscn"), Is.EqualTo("Mapp Tavern"));
	}

	[Test]
	public void ForPath_NullOrEmpty_ReturnsUnknown()
	{
		Assert.That(MapDisplayNames.ForPath(null), Is.EqualTo("Unknown"));
		Assert.That(MapDisplayNames.ForPath(""),   Is.EqualTo("Unknown"));
	}
}
