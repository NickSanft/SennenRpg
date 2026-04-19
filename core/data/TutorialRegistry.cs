namespace SennenRpg.Core.Data;

/// <summary>
/// Static catalogue of every tutorial in the game. Trigger sites reference
/// entries by <see cref="TutorialIds"/> constants so typos surface at compile time.
/// </summary>
public static class TutorialRegistry
{
    public static readonly Tutorial[] All =
    {
        // ── Overworld ────────────────────────────────────────────────
        new(TutorialIds.OverworldMovement,
            "Movement",
            "Use the arrow keys or D-pad to move around.\n" +
            "Press [Z] to interact with NPCs, signs, and objects.\n" +
            "Press [X] or [Esc] to open the menu.",
            TutorialCategory.Overworld),

        new(TutorialIds.InteractPrompt,
            "Interacting",
            "A [Z] prompt appears above things you can interact with.\n" +
            "Walk up to the object and press [Z] to examine it.",
            TutorialCategory.Overworld),

        new(TutorialIds.SavePoint,
            "Save Points",
            "Stand on a glowing save point and press [Z] to save.\n" +
            "You can load your progress from the title screen at any time.",
            TutorialCategory.Overworld),

        new(TutorialIds.Foraging,
            "Foraging",
            "You found something! Every few steps in the wild you may\n" +
            "spot a useful item — ingredients for cooking, or junk to\n" +
            "sell for gold back in town.",
            TutorialCategory.Foraging),

        new(TutorialIds.WeatherFirst,
            "Weather",
            "The world's weather changes as you travel. Storms, fog,\n" +
            "and snow each affect battles in subtle ways — obstacles may\n" +
            "fade in, wobble, or arrive a beat later. Stay adaptable.",
            TutorialCategory.Overworld),

        new(TutorialIds.Mode7,
            "Mode 7 View",
            "You've toggled the Mode 7 perspective view! Press [M]\n" +
            "again to return to the top-down view. This is a purely\n" +
            "cosmetic effect — gameplay is unchanged.",
            TutorialCategory.Advanced),

        // ── Battle ───────────────────────────────────────────────────
        new(TutorialIds.EncounterFirst,
            "Battle!",
            "You've been drawn into battle. Each character takes turns\n" +
            "in order of speed. Choose an action when it's your turn.\n" +
            "Defeat every enemy to win — run out of HP and you'll fall.",
            TutorialCategory.Battle),

        new(TutorialIds.BattleFight,
            "Actions",
            "FIGHT — attack the targeted enemy with a timing minigame.\n" +
            "SKILLS — use special techniques or spells (costs MP).\n" +
            "ITEM — use a consumable from your bag.\n" +
            "FLEE — try to escape the battle (not always successful).",
            TutorialCategory.Battle),

        new(TutorialIds.ComboSpell,
            "Combo Attacks",
            "Two party members in a row can team up for a COMBO attack!\n" +
            "Combo attacks consume both characters' turns and deal\n" +
            "heavy combined damage. Costs MP from both members.",
            TutorialCategory.Battle),

        // ── Rhythm ───────────────────────────────────────────────────
        new(TutorialIds.RhythmStrike,
            "Timing Strikes",
            "Press [Z] as the cursor aligns with the sweet spot to land\n" +
            "your attack. Perfect timing deals critical damage — miss\n" +
            "it entirely and the attack glances off.",
            TutorialCategory.Rhythm),

        new(TutorialIds.RhythmDodge,
            "Rhythm Phase",
            "Enemies attack on the beat. Obstacles fly down four lanes\n" +
            "to the music — press the matching direction key as each\n" +
            "one reaches the line to dodge. Miss and you take damage.",
            TutorialCategory.Rhythm),

        // ── Menus ────────────────────────────────────────────────────
        new(TutorialIds.ItemMenu,
            "Items",
            "Use consumables to heal, cure status, or buff your party.\n" +
            "Ingredients and key items are shown here too, but only\n" +
            "consumables can be used in battle.",
            TutorialCategory.Menus),

        new(TutorialIds.Equipment,
            "Equipment",
            "Equip weapons, armor, and accessories to boost stats.\n" +
            "Each party member has their own equipment slots — cycle\n" +
            "between members with [◀] and [▶].",
            TutorialCategory.Menus),

        new(TutorialIds.SpellsMenu,
            "Spells",
            "Cast spells in battle for MP cost. Some spells work outside\n" +
            "of battle too — like Teleport Home, which returns you to\n" +
            "the tavern from anywhere in the world.",
            TutorialCategory.Menus),

        // ── Cooking ──────────────────────────────────────────────────
        new(TutorialIds.Cooking,
            "Cooking",
            "Combine ingredients to craft food items. A rhythm minigame\n" +
            "determines quality — Perfect meals heal more, Burnt meals\n" +
            "heal less. Recipes are tracked in your Cooking Journal.",
            TutorialCategory.Cooking),

        // ── Party ────────────────────────────────────────────────────
        new(TutorialIds.PartyRecruit,
            "New Party Member!",
            "Someone has joined your party! Up to six members can travel\n" +
            "with you. Manage your formation and leader from the PARTY\n" +
            "menu in the pause screen.",
            TutorialCategory.Party),

        new(TutorialIds.PartyMenu,
            "Party",
            "Arrange your party here. Set the leader (who you control\n" +
            "on the map), swap front and back rows, and check each\n" +
            "member's stats. All members gain EXP after battles.",
            TutorialCategory.Party),

        new(TutorialIds.ClassChange,
            "Class Change",
            "Sen can switch between six classes — each has its own level,\n" +
            "stats, and abilities. Level up multiple classes to unlock\n" +
            "cross-class bonuses that stay with you regardless of class.",
            TutorialCategory.Party),

        // ── Advanced ─────────────────────────────────────────────────
        new(TutorialIds.Jukebox,
            "Jukebox",
            "Replay any music track you've heard on your journey.\n" +
            "New tracks unlock automatically the first time they play.",
            TutorialCategory.Advanced),
    };
}

/// <summary>
/// Compile-time constants for every tutorial ID. Reference these from
/// trigger sites instead of string literals to avoid typos.
/// </summary>
public static class TutorialIds
{
    public const string OverworldMovement = "overworld_movement";
    public const string InteractPrompt    = "interact_prompt";
    public const string SavePoint         = "save_point";
    public const string Foraging          = "foraging";
    public const string WeatherFirst      = "weather_first";
    public const string Mode7             = "mode7";

    public const string EncounterFirst = "encounter_first";
    public const string BattleFight    = "battle_fight";
    public const string ComboSpell     = "combo_spell";

    public const string RhythmStrike = "rhythm_strike";
    public const string RhythmDodge  = "rhythm_dodge";

    public const string ItemMenu   = "item_menu";
    public const string Equipment  = "equipment";
    public const string SpellsMenu = "spells_menu";

    public const string Cooking = "cooking";

    public const string PartyRecruit = "party_recruit";
    public const string PartyMenu    = "party_menu";
    public const string ClassChange  = "class_change";

    public const string Jukebox = "jukebox";
}
