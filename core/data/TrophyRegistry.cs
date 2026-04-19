namespace SennenRpg.Core.Data;

/// <summary>
/// Static registry of all trophies in the game.
/// </summary>
public static class TrophyRegistry
{
    public static readonly Trophy[] All =
    {
        // ── Combat ──────────────────────────────────────────────────────
        new("first_blood",         "First Blood",         "Defeat your first enemy.",                        "X", false, TrophyCategory.Combat),
        new("centurion",           "Centurion",           "Defeat 100 enemies.",                             "C", false, TrophyCategory.Combat),
        new("flawless_rhythm",     "Flawless Rhythm",     "Achieve an S rank on a rhythm minigame.",         "S", false, TrophyCategory.Combat),
        new("combo_master",        "Combo Master",        "Reach a 30-hit combo streak.",                    "M", false, TrophyCategory.Combat),
        new("party_wipe_survivor", "Party Wipe Survivor", "Win a battle with all members below 10% HP.",    "!", true,  TrophyCategory.Combat),

        // ── Cooking ─────────────────────────────────────────────────────
        new("first_course",        "First Course",        "Cook your first meal.",                           "F", false, TrophyCategory.Cooking),
        new("master_chef",         "Master Chef",         "Cook 10 perfect-quality meals.",                  "P", false, TrophyCategory.Cooking),
        new("recipe_collector",    "Recipe Collector",    "Discover all recipes.",                           "R", false, TrophyCategory.Cooking),

        // ── Exploration ─────────────────────────────────────────────────
        new("cartographer",        "Cartographer",        "Visit every map in the world.",                   "W", false, TrophyCategory.Exploration),
        new("dungeon_diver",       "Dungeon Diver",       "Reach dungeon floor 3.",                          "D", false, TrophyCategory.Exploration),
        new("night_owl",           "Night Owl",           "Fight 10 battles at night.",                      "N", true,  TrophyCategory.Exploration),

        // ── Collection ──────────────────────────────────────────────────
        new("hoarder",             "Hoarder",             "Own 50 or more items at once.",                   "H", false, TrophyCategory.Collection),
        new("bestiary_scholar",    "Bestiary Scholar",    "Discover every enemy in the bestiary.",           "B", false, TrophyCategory.Collection),
        new("junk_dealer",         "Junk Dealer",         "Sell 500G worth of junk items.",                  "J", false, TrophyCategory.Collection),

        // ── Mastery ─────────────────────────────────────────────────────
        new("multi_talented",      "Multi-Talented",      "Reach level 5 in at least 3 different classes.",  "T", false, TrophyCategory.Mastery),
        new("class_master",        "Class Master",        "Reach level 20 in any class.",                    "L", false, TrophyCategory.Mastery),
        new("full_party",          "Full Party",          "Recruit every available party member.",            "A", false, TrophyCategory.Mastery),
    };

    /// <summary>Look up a trophy by ID, or null if not found.</summary>
    public static Trophy? Find(string id)
    {
        foreach (var t in All)
            if (t.Id == id) return t;
        return null;
    }
}
