using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Plain C# DTO representing one playable party member.
///
/// Designed to round-trip cleanly through System.Text.Json for SaveData persistence,
/// so it intentionally uses string keys for slot dictionaries instead of
/// <see cref="EquipmentSlot"/> enums (which would force a custom converter for the
/// dictionary key path). Stats are flat ints, identical to the legacy single-character
/// SaveData fields.
///
/// <para>
/// In Phase 2 the only existing party member is "sen" — Sen's stats and equipment
/// are mirrored from the existing <c>PlayerCombatData</c> / <c>InventoryData</c>
/// domains so the legacy save format can migrate forward without losing anything.
/// Phase 3 onward introduces additional members (Lily, Rain) whose stats live entirely
/// on the <see cref="PartyMember"/> instance.
/// </para>
/// </summary>
public class PartyMember
{
    // ── Identity ──────────────────────────────────────────────────────

    /// <summary>Stable lowercase identifier (e.g. "sen", "lily", "rain"). Save key.</summary>
    public string MemberId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Class enum stored as a string so future enum changes don't invalidate saves.
    /// Use <see cref="PartyMember.PlayerClassEnum"/> to read it as the typed enum.
    /// </summary>
    public string Class { get; set; } = nameof(PlayerClass.Bard);

    /// <summary>
    /// True for Sen (the protagonist) — can hot-swap between all six classes via Rork.
    /// False for recruited specialists who are locked to one class.
    /// </summary>
    public bool CanChangeClass { get; set; } = false;

    /// <summary>Battle formation row. <see cref="FormationRow.Front"/> by default.</summary>
    public FormationRow Row { get; set; } = FormationRow.Front;

    // ── Sprites / portraits ───────────────────────────────────────────

    /// <summary>
    /// Sprite sheet path used by the overworld follower system on 16×16 sprite maps.
    /// e.g. "res://assets/sprites/player/Lily_Overworld.png".
    /// </summary>
    public string OverworldSpritePath { get; set; } = "";

    /// <summary>
    /// Optional portrait path for the party menu / battle HUD.
    /// Empty when no portrait has been authored.
    /// </summary>
    public string PortraitPath { get; set; } = "";

    // ── Progression ───────────────────────────────────────────────────

    public int Level { get; set; } = 1;
    public int Exp   { get; set; } = 0;

    // ── Combat stats (mirrors CharacterStats fields) ──────────────────

    public int CurrentHp { get; set; } = 1;
    public int MaxHp     { get; set; } = 1;
    public int CurrentMp { get; set; } = 0;
    public int MaxMp     { get; set; } = 0;

    public int Attack     { get; set; } = 0;
    public int Defense    { get; set; } = 0;
    public int Speed      { get; set; } = 0;
    public int Magic      { get; set; } = 0;
    public int Resistance { get; set; } = 0;
    public int Luck       { get; set; } = 0;

    // ── Equipment (per-member) ────────────────────────────────────────

    /// <summary>
    /// Slot enum name → resource path. Empty values are skipped.
    /// Slot keys match <see cref="EquipmentSlot"/> ToString() output, identical to
    /// <see cref="SaveData.EquippedItemPaths"/>.
    /// </summary>
    public Dictionary<string, string> EquippedItemPaths { get; set; } = new();

    /// <summary>
    /// Slot enum name → dynamic equipment ID for Lily-forged items.
    /// </summary>
    public Dictionary<string, string> EquippedDynamicItemIds { get; set; } = new();

    /// <summary>
    /// In-battle status effects (Poison / Stun / Shield / Silence) and their remaining
    /// turn counts. Persisted across save/load so brews / debuffs survive scene changes
    /// (though typically these clear at battle end). Used by the per-actor status
    /// pipeline added in phase 7-stretch.
    /// </summary>
    public Dictionary<StatusEffect, int> Statuses { get; set; } = new();

    // ── Convenience helpers ───────────────────────────────────────────

    /// <summary>
    /// Parse the persisted <see cref="Class"/> string into a typed <see cref="PlayerClass"/>.
    /// Falls back to Bard if the value is unknown (e.g. a save authored before this enum value existed).
    /// </summary>
    public PlayerClass PlayerClassEnum
    {
        get => System.Enum.TryParse<PlayerClass>(Class, out var cls) ? cls : PlayerClass.Bard;
        set => Class = value.ToString();
    }

    /// <summary>True when the member has 0 HP and is knocked out.</summary>
    public bool IsKO => CurrentHp <= 0;

    /// <summary>Build a default Sen entry from raw stat values. Used by save migration / new game init.</summary>
    public static PartyMember CreateSen(
        string displayName, PlayerClass cls,
        int level, int exp,
        int currentHp, int maxHp, int currentMp, int maxMp,
        int attack, int defense, int speed,
        int magic, int resistance, int luck)
    {
        return new PartyMember
        {
            MemberId            = "sen",
            DisplayName         = displayName,
            Class               = cls.ToString(),
            CanChangeClass      = true,
            Row                 = FormationRow.Front,
            OverworldSpritePath = "res://assets/sprites/player/Sen_Overworld.png",
            PortraitPath        = "",
            Level               = level,
            Exp                 = exp,
            CurrentHp           = currentHp,
            MaxHp               = maxHp,
            CurrentMp           = currentMp,
            MaxMp               = maxMp,
            Attack              = attack,
            Defense             = defense,
            Speed               = speed,
            Magic               = magic,
            Resistance          = resistance,
            Luck                = luck,
        };
    }
}
