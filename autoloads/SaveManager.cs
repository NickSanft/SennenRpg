using Godot;
using System.Text.Json;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

public partial class SaveManager : Node
{
	public static SaveManager Instance { get; private set; } = null!;

	/// <summary>Active slot (1-based). Set before calling SaveGame / LoadGame.</summary>
	public int CurrentSlot { get; set; } = 1;

	private string SavePath => SaveSlotLogic.GetSavePath(CurrentSlot);

	public override void _Ready()
	{
		Instance    = this;
		ProcessMode = ProcessModeEnum.Always;
	}

	// ── Query ─────────────────────────────────────────────────────────────────

	/// <summary>Returns true when the current slot has a save file on disk.</summary>
	public bool HasSave() => Godot.FileAccess.FileExists(SavePath);

	/// <summary>Returns true when <paramref name="slot"/> has a save file on disk.</summary>
	public bool HasSave(int slot) => Godot.FileAccess.FileExists(SaveSlotLogic.GetSavePath(slot));

	/// <summary>Returns true when any of the three slots has a save file.</summary>
	public bool HasAnySave()
	{
		for (int s = 1; s <= SaveSlotLogic.MaxSlots; s++)
			if (HasSave(s)) return true;
		return false;
	}

	/// <summary>
	/// Reads just the metadata fields (level, name, play-time, timestamp) from
	/// <paramref name="slot"/> without applying anything to GameManager.
	/// Returns null when the slot is empty or the file is unreadable.
	/// </summary>
	public SaveSlotInfo? LoadSlotInfo(int slot)
	{
		string path = SaveSlotLogic.GetSavePath(slot);
		if (!Godot.FileAccess.FileExists(path)) return null;

		using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
		string json = file.GetAsText();
		var data = JsonSerializer.Deserialize<SaveData>(json);
		if (data == null) return null;

		return new SaveSlotInfo(
			data.PlayerLevel,
			data.PlayerName,
			data.PlayTimeSeconds,
			data.Timestamp,
			data.ActiveClassName ?? data.PlayerClassName ?? "Bard"
		);
	}

	// ── Save / load ───────────────────────────────────────────────────────────

	public void SaveGame()
	{
		var gm   = GameManager.Instance;
		// Push Sen's authoritative state into his PartyMember so the serialised
		// Party list reflects the current numbers (Phase 2 mirror).
		gm.SyncSenToParty();
		var data = new SaveData
		{
			PlayerHp         = gm.PlayerStats.CurrentHp,
			PlayerMaxHp      = gm.PlayerStats.MaxHp,
			PlayerAttack     = gm.PlayerStats.Attack,
			PlayerDefense    = gm.PlayerStats.Defense,
			PlayerSpeed      = gm.PlayerStats.Speed,
			PlayerMagic      = gm.PlayerStats.Magic,
			PlayerResistance = gm.PlayerStats.Resistance,
			PlayerLuck       = gm.PlayerStats.Luck,
			PlayerMaxMp      = gm.PlayerStats.MaxMp,
			PlayerMp         = gm.PlayerStats.CurrentMp,
			PlayerLevel      = gm.PlayerLevel,
			Gold             = gm.Gold,
			Exp              = gm.Exp,
			LastMapPath      = gm.LastMapPath,
			LastSavePointId  = gm.LastSavePointId,
			LastSpawnId      = gm.LastSavePointId, // spawn at save point on load
			Flags            = new System.Collections.Generic.Dictionary<string, bool>(gm.Flags),
			InventoryItemPaths  = new System.Collections.Generic.List<string>(gm.InventoryItemPaths),
			KnownSpellPaths     = new System.Collections.Generic.List<string>(gm.KnownSpellPaths),
			OwnedEquipmentPaths = new System.Collections.Generic.List<string>(gm.OwnedEquipmentPaths),
			EquippedItemPaths   = SerialiseEquipped(gm.EquippedItemPaths),
			WorldMapSpawnTileX    = gm.WorldMapSpawnTile.X,
			WorldMapSpawnTileY    = gm.WorldMapSpawnTile.Y,
			IsNight               = gm.IsNight,
			TilesWalkedOnWorldMap = gm.TilesWalkedOnWorldMap,
			KillCounts            = new System.Collections.Generic.Dictionary<string, int>(gm.KillCounts),
			RhythmMemory          = new System.Collections.Generic.Dictionary<string, SennenRpg.Core.Data.EnemyRhythmHistory>(gm.RhythmMemory),
			ActiveQuestIds        = QuestManager.Instance.GetActiveQuestIds(),
			CompletedQuestIds     = QuestManager.Instance.GetCompletedQuestIds(),
			PlayerClassName       = gm.PlayerStats.ClassName,
			PaletteSourceColors   = SerialiseColors(gm.PaletteSourceColors),
			PaletteTargetColors   = SerialiseColors(gm.PaletteTargetColors),
			// Slot metadata
			PlayerName      = gm.PlayerName,
			PlayTimeSeconds = gm.PlayTimeSeconds,
			Timestamp       = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
			// Mellyr Outpost
			TownStepCounter          = gm.TownStepCounter,
			PendingRainGold          = gm.PendingRainGold,
			PendingLilyRecipes       = new System.Collections.Generic.List<string>(gm.PendingLilyRecipes),
			DynamicEquipmentInventory = new System.Collections.Generic.List<SennenRpg.Core.Data.DynamicEquipmentSave>(gm.DynamicEquipmentInventory),
			EquippedDynamicItemIds   = SerialiseDynamicEquipped(gm.EquippedDynamicItemIds),
			// Multi-class progression
			ClassProgressionEntries  = new System.Collections.Generic.List<SennenRpg.Core.Data.ClassProgressionEntry>(gm.ClassEntries.Values),
			ActiveClassName          = gm.ActiveClass.ToString(),
			// Foraging
			ForageStreak             = gm.ForageStreak,
			ForageCodex              = new System.Collections.Generic.Dictionary<string, SennenRpg.Core.Data.ForageCodexEntry>(gm.ForageCodex.Entries),
			// Weather
			Weather                  = WeatherManager.Instance?.Current ?? SennenRpg.Core.Data.WeatherType.Sunny,
			WeatherStepCounter       = WeatherManager.Instance?.StepCounter ?? 0,
			// Bestiary
			Bestiary                 = new System.Collections.Generic.Dictionary<string, SennenRpg.Core.Data.BestiaryEntry>(gm.Bestiary.Entries),
			// Party (Phase 2)
			Party                    = new System.Collections.Generic.List<SennenRpg.Core.Data.PartyMember>(gm.Party.Members),
			PartyLeaderIndex         = gm.Party.LeaderIndex,
		};

		string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
		using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
		file.StoreString(json);

		GD.Print($"[SaveManager] Saved to slot {CurrentSlot} ({SavePath}).");
	}

	public SaveData? LoadGame()
	{
		if (!HasSave()) return null;

		using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
		string json = file.GetAsText();
		return JsonSerializer.Deserialize<SaveData>(json);
	}

	public void ApplyLoadedData(SaveData data)
	{
		GameManager.Instance.ApplySaveData(data);
		QuestManager.Instance.ApplySaveData(data.ActiveQuestIds, data.CompletedQuestIds);
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private static string[] SerialiseColors(Color[] colors)
	{
		var result = new string[colors.Length];
		for (int i = 0; i < colors.Length; i++)
			result[i] = colors[i].ToHtml(false);
		return result;
	}

	private static System.Collections.Generic.Dictionary<string, string> SerialiseEquipped(
		System.Collections.Generic.Dictionary<SennenRpg.Core.Data.EquipmentSlot, string> equipped)
	{
		var result = new System.Collections.Generic.Dictionary<string, string>();
		foreach (var kv in equipped)
			result[kv.Key.ToString()] = kv.Value;
		return result;
	}

	private static System.Collections.Generic.Dictionary<string, string> SerialiseDynamicEquipped(
		System.Collections.Generic.Dictionary<SennenRpg.Core.Data.EquipmentSlot, string> equipped)
	{
		var result = new System.Collections.Generic.Dictionary<string, string>();
		foreach (var kv in equipped)
			result[kv.Key.ToString()] = kv.Value;
		return result;
	}
}
