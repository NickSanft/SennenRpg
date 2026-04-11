using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using SennenRpg.Autoloads;

namespace SennenRpg.Core.Data;

using EquipDict = System.Collections.Generic.Dictionary<EquipmentSlot, string>;

/// <summary>
/// Holds player inventory state: consumable items, spells, owned equipment, and equipped items
/// (both static .tres and Lily-generated dynamic equipment).
/// Plain C# class — owned by GameManager as an internal domain.
/// </summary>
public class InventoryData
{
	public Array<string> InventoryItemPaths  { get; private set; } = new();
	public Array<string> KnownSpellPaths     { get; private set; } = new();
	public Array<string> OwnedEquipmentPaths { get; private set; } = new();
	public EquipDict     EquippedItemPaths   { get; private set; } = new();

	public List<DynamicEquipmentSave>        DynamicEquipmentInventory { get; } = new();
	public System.Collections.Generic.Dictionary<EquipmentSlot, string> EquippedDynamicItemIds { get; } = new();

	// ── Items ─────────────────────────────────────────────────────────────────

	public void AddItem(string resourcePath)    => InventoryItemPaths.Add(resourcePath);
	public bool RemoveItem(string resourcePath)
	{
		int idx = InventoryItemPaths.IndexOf(resourcePath);
		if (idx < 0) return false;
		InventoryItemPaths.RemoveAt(idx);
		return true;
	}

	// ── Spells ────────────────────────────────────────────────────────────────

	public void AddSpell(string resourcePath)
	{
		if (!KnownSpellPaths.Contains(resourcePath))
			KnownSpellPaths.Add(resourcePath);
	}

	public bool RemoveSpell(string resourcePath)
	{
		int idx = KnownSpellPaths.IndexOf(resourcePath);
		if (idx < 0) return false;
		KnownSpellPaths.RemoveAt(idx);
		return true;
	}

	// ── Static equipment ──────────────────────────────────────────────────────

	public void AddEquipment(string resourcePath) => OwnedEquipmentPaths.Add(resourcePath);

	public EquipmentData? GetEquipped(EquipmentSlot slot)
	{
		if (!EquippedItemPaths.TryGetValue(slot, out string? path) || string.IsNullOrEmpty(path))
			return null;
		return ResourceLoader.Exists(path) ? GD.Load<EquipmentData>(path) : null;
	}

	/// <summary>Equip an item from OwnedEquipmentPaths into the given slot. Returns true if equipped.</summary>
	public bool Equip(EquipmentSlot slot, string path)
	{
		if (EquippedItemPaths.TryGetValue(slot, out string? current) && !string.IsNullOrEmpty(current))
			OwnedEquipmentPaths.Add(current);

		EquippedItemPaths[slot] = path;
		OwnedEquipmentPaths.Remove(path);
		return true;
	}

	/// <summary>Unequip the item in a slot, returning it to OwnedEquipmentPaths.</summary>
	public bool Unequip(EquipmentSlot slot)
	{
		if (!EquippedItemPaths.TryGetValue(slot, out string? path) || string.IsNullOrEmpty(path))
			return false;
		OwnedEquipmentPaths.Add(path);
		EquippedItemPaths.Remove(slot);
		return true;
	}

	// ── Dynamic (Lily-forged) equipment ───────────────────────────────────────

	public void EquipDynamic(EquipmentSlot slot, string itemId)
		=> EquippedDynamicItemIds[slot] = itemId;

	public void UnequipDynamic(EquipmentSlot slot)
		=> EquippedDynamicItemIds.Remove(slot);

	// ── Mellyr reward collection ──────────────────────────────────────────────

	public List<DynamicEquipmentSave> CollectLilyRewards(List<string> pendingRecipes)
	{
		var items = pendingRecipes.Select(LilyForgeLogic.Resolve).ToList();
		foreach (var item in items)
			DynamicEquipmentInventory.Add(item);
		pendingRecipes.Clear();
		return items;
	}

	public List<DynamicEquipmentSave> CollectKrioraRewards(List<string> pendingRecipes)
	{
		var items = pendingRecipes.Select(KrioraForgeLogic.Resolve).ToList();
		foreach (var item in items)
			DynamicEquipmentInventory.Add(item);
		return items;
	}

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	public void Reset()
	{
		InventoryItemPaths.Clear();
		InventoryItemPaths.Add("res://resources/items/item_001.tres");
		KnownSpellPaths.Clear();
		KnownSpellPaths.Add("res://resources/spells/shadow_bolt.tres");
		KnownSpellPaths.Add("res://resources/spells/teleport_home.tres");

		OwnedEquipmentPaths.Clear();
		OwnedEquipmentPaths.Add("res://resources/equipment/iron_sword.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/leather_cap.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/leather_body.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/leather_legs.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/leather_boots.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/wooden_shield.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/work_gloves.tres");
		OwnedEquipmentPaths.Add("res://resources/equipment/lucky_charm.tres");
		EquippedItemPaths.Clear();

		DynamicEquipmentInventory.Clear();
		EquippedDynamicItemIds.Clear();
	}

	public void ApplyFromSave(SaveData data)
	{
		InventoryItemPaths.Clear();
		foreach (var path in data.InventoryItemPaths)
			InventoryItemPaths.Add(path);

		KnownSpellPaths.Clear();
		foreach (var path in data.KnownSpellPaths)
			KnownSpellPaths.Add(path);

		OwnedEquipmentPaths.Clear();
		foreach (var path in data.OwnedEquipmentPaths)
			OwnedEquipmentPaths.Add(path);

		EquippedItemPaths.Clear();
		foreach (var kv in data.EquippedItemPaths)
		{
			if (System.Enum.TryParse<EquipmentSlot>(kv.Key, out var slot))
				EquippedItemPaths[slot] = kv.Value;
		}

		DynamicEquipmentInventory.Clear();
		DynamicEquipmentInventory.AddRange(data.DynamicEquipmentInventory);
		EquippedDynamicItemIds.Clear();
		foreach (var kv in data.EquippedDynamicItemIds)
		{
			if (System.Enum.TryParse<EquipmentSlot>(kv.Key, out var slot))
				EquippedDynamicItemIds[slot] = kv.Value;
		}
	}
}
