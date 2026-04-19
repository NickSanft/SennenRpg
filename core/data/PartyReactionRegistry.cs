namespace SennenRpg.Core.Data;

/// <summary>
/// Static catalogue of overworld party member reactions, keyed by member ID and map ID.
/// Each character has a distinct voice across different locations.
/// </summary>
public static class PartyReactionRegistry
{
    public static readonly PartyReaction[] All =
    {
        // ── Lily ─────────────────────────────────────────────────────
        new("lily", "mapp_tavern",     "The smell of fresh bread always cheers me up.", 3),
        new("lily", "mapp_tavern",     "Rork keeps this place running like clockwork.", 1),
        new("lily", "mellyr_outpost",  "I wonder if the smiths here could temper my catalysts...", 3),
        new("lily", "mellyr_outpost",  "The heat from the forges feels nice.", 1),
        new("lily", "world_map",       "What a beautiful day for a walk!", 3),
        new("lily", "world_map",       "Do you hear that birdsong? Lovely.", 1),
        new("lily", "dungeon_floor1",  "This place gives me the creeps...", 3),
        new("lily", "dungeon_floor2",  "Stay close, Sen. I have a bad feeling.", 2),
        new("lily", "dungeon_floor3",  "We're deep now. No turning back.", 2),

        // ── Rain ─────────────────────────────────────────────────────
        new("rain", "mapp_tavern",     "I could use a drink.", 3),
        new("rain", "mapp_tavern",     "Wonder if Rork has anything new on tap.", 1),
        new("rain", "mellyr_outpost",  "Good trading post. Keep your coin purse close.", 3),
        new("rain", "mellyr_outpost",  "I've dealt with merchants like these before.", 1),
        new("rain", "world_map",       "Keep your eyes peeled for loot.", 3),
        new("rain", "world_map",       "Nice breeze. Almost makes you forget the monsters.", 1),
        new("rain", "dungeon_floor1",  "Tight corridors. Perfect for an ambush.", 3),
        new("rain", "dungeon_floor2",  "We should check every corner. Treasure hides in the dark.", 2),
        new("rain", "dungeon_floor3",  "Getting warmer down here. That's never a good sign.", 2),

        // ── Bhata ────────────────────────────────────────────────────
        new("bhata", "mapp_tavern",    "A fine establishment. Reminds me of the old garrison.", 3),
        new("bhata", "mapp_tavern",    "Could do with some arm-wrestling around here.", 1),
        new("bhata", "mellyr_outpost", "Solid fortifications. I approve.", 3),
        new("bhata", "mellyr_outpost", "The guards here could use better training.", 1),
        new("bhata", "world_map",      "Open ground. Good visibility, but little cover.", 3),
        new("bhata", "world_map",      "March in formation, everyone. Stay sharp.", 1),
        new("bhata", "dungeon_floor1", "Stay close. I hear something.", 3),
        new("bhata", "dungeon_floor2", "These walls have seen battle. Old scorch marks.", 2),
        new("bhata", "dungeon_floor3", "Whatever lives down here won't go quietly.", 2),

        // ── Kriora ───────────────────────────────────────────────────
        new("kriora", "mapp_tavern",    "Tavern food is passable. I've had worse on campaign.", 3),
        new("kriora", "mapp_tavern",    "The ale here is... adequate.", 1),
        new("kriora", "mellyr_outpost", "The forges here are impressive.", 3),
        new("kriora", "mellyr_outpost", "I could refine my technique watching these smiths.", 2),
        new("kriora", "world_map",      "A straightforward path. I prefer that.", 3),
        new("kriora", "world_map",      "Keep moving. Daylight is precious.", 1),
        new("kriora", "dungeon_floor1", "Hmph. Dark and damp. Typical.", 3),
        new("kriora", "dungeon_floor2", "The stonework here is ancient. Dwarven, perhaps.", 2),
        new("kriora", "dungeon_floor3", "We are close to something. I can feel it.", 2),
    };
}
