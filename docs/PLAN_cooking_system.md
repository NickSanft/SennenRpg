# Cooking System Plan — SennenRpg

## Context

Add a cooking system where players combine ingredients into food items via a rhythm minigame. Cooking quality (Burnt/Normal/Perfect) modifies the food's effectiveness. Also refactor the inventory menu with item categorization and improved layout.

---

## Phase 1 — Item Type Categorization

### New file: `core/data/ItemType.cs`
```csharp
public enum ItemType { Consumable, Ingredient, Equipment, KeyItem, Repel }
```

### Modify: `core/data/ItemData.cs`
Add `[Export] public ItemType Type { get; set; } = ItemType.Consumable;`

Defaults to Consumable so all existing items work without migration.

---

## Phase 2 — Recipe Data & Cooking Logic

### New file: `core/data/RecipeIngredient.cs`
Readonly record struct: `(string ItemPath, int Count)`

### New file: `core/data/CookingQuality.cs`
Enum: `Burnt, Normal, Perfect`

### New file: `core/data/CookingLogic.cs`
Pure static class (NUnit-testable), following the `ItemLogic`/`ShopLogic` pattern:

| Method | Purpose |
|---|---|
| `HasIngredients(inventoryPaths, ingredients)` | Check if player has all required ingredients |
| `ConsumeIngredients(inventoryPaths, ingredients)` | Return remaining paths after removing ingredients (null if insufficient) |
| `DetermineQuality(perfects, goods, totalNotes)` | Minigame performance → quality tier |
| `QualityHealBonus(baseHeal, quality)` | Apply multiplier: Burnt=0.5x, Normal=1.0x, Perfect=1.5x |
| `QualityItemPath(basePath, quality)` | Derive quality-variant resource path |
| `QualityLabel(quality)` | Display string: "Burnt!", "Normal", "Perfect!" |

**Quality thresholds:**
- Perfect: >= 80% notes hit AND >= 50% of hits are Perfect-grade
- Normal: >= 50% notes hit
- Burnt: < 50% notes hit

### New file: `core/data/RecipeData.cs`
Godot Resource with exports: RecipeId, DisplayName, Description, Icon, IngredientPaths[], IngredientCounts[] (parallel arrays), OutputItemPath, BaseHealAmount, Difficulty (note count for minigame)

### Tests: `SennenRpg.Tests/Logic/CookingLogicTests.cs`
~16 test cases covering HasIngredients, ConsumeIngredients, DetermineQuality, QualityHealBonus, QualityItemPath.

---

## Phase 3 — Item & Recipe Resources

### Ingredient items (`resources/items/`)
| File | Name | Price at Rork's |
|---|---|---|
| `ingredient_mystery_meat.tres` | Mystery Meat | 15G |
| `ingredient_bread.tres` | Bread | 10G |
| `ingredient_ecto_essence.tres` | Ecto Essence | 25G |
| `ingredient_sugar.tres` | Sugar | 5G |

All set `Type = Ingredient`, `HealAmount = 0`.

### Cooked food items (`resources/items/`)
Quality variants as separate .tres files (no inventory system changes needed):

| Recipe | Burnt (0.5x) | Normal (1.0x) | Perfect (1.5x) |
|---|---|---|---|
| Mystery Meat Sandwich | `cooked_mystery_meat_sandwich_burnt.tres` (15 HP) | `cooked_mystery_meat_sandwich.tres` (30 HP) | `cooked_mystery_meat_sandwich_perfect.tres` (45 HP) |
| Ecto Cooler | `cooked_ecto_cooler_burnt.tres` (10 HP) | `cooked_ecto_cooler.tres` (20 HP) | `cooked_ecto_cooler_perfect.tres` (30 HP) |

All set `Type = Consumable`.

### Recipe resources (`resources/recipes/`)
| File | Ingredients | Difficulty |
|---|---|---|
| `recipe_mystery_meat_sandwich.tres` | Mystery Meat x1, Bread x1 | 6 notes |
| `recipe_ecto_cooler.tres` | Ecto Essence x1, Sugar x1 | 8 notes |

---

## Phase 4 — Cooking Minigame

### New file: `scenes/menus/CookingMinigame.cs` + `.tscn`

Follows the `CharmMinigame` pattern — single-lane rhythm game with custom `_Draw()`:

- Configurable note count from `RecipeData.Difficulty`
- Uses `RhythmClock.Instance.StartFreeRunning(120)` (cooking BPM, moderate tempo)
- Notes scroll left→right, player taps to hit
- Tracks Perfect/Good/Miss separately (not just success/total)
- Emits `CookingCompleted(int perfects, int goods, int misses, int totalNotes)`
- Visual theme: cooking-flavored notes (stir, flip, season labels)
- Hit windows: Perfect (tight, ~60% of note radius), Good (standard 22px), Miss (beyond)

**Key difference from battle minigames:** Starts `RhythmClock` in free-running mode since cooking happens from the pause menu (no BGM beat to sync to). Stops the clock on completion.

---

## Phase 5 — Cooking Menu

### New file: `scenes/menus/CookingMenu.cs` + `.tscn`

CanvasLayer (Layer 51), following `InventoryMenu`/`ShopMenu` pattern:

**Layout:**
- Title: "COOKING" (gold)
- Recipe list: each row shows recipe name, ingredient requirements (green=have, red=need), COOK button
- COOK button disabled when ingredients insufficient
- Feedback area for cooking results

**Flow:**
1. Open → load recipes from static path list
2. Each row shows: `[Icon] Recipe Name | Bread 1/1 Meat 0/1 | [COOK]`
3. Press COOK → consume ingredients → show CookingMinigame
4. Minigame completes → `DetermineQuality()` → `QualityItemPath()` → `AddItem()`
5. Show result: "Cooked Mystery Meat Sandwich! (Perfect!)" with quality label
6. Return to recipe list

---

## Phase 6 — Pause Menu Integration

### Modify: `scenes/menus/PauseMenu.cs` + `.tscn`

Add COOK button between ITEMS and EQUIPMENT. Follow the exact same pattern as the other sub-menus:
- Load `CookingMenu.tscn` as sibling in `_Ready()`
- Connect `Closed` signal
- Hide PauseMenu when cooking menu opens, restore on close

Button order: Resume / Save / Settings / Items / **Cook** / Equipment / Stats / Main Menu

---

## Phase 7 — Inventory Menu Refactor

### Modify: `scenes/menus/InventoryMenu.cs` + `.tscn`

**Category tabs** — button row at top:
- ALL | CONSUMABLE | INGREDIENT | KEY ITEM
- Active tab highlighted, filters displayed items
- `_activeFilter: ItemType?` (null = All)

**Item stacking** — count duplicate paths:
- Show "Bandage x3" instead of three separate rows
- Helper: `CountItems(IEnumerable<string> paths) → Dictionary<string, int>`

**Improved row layout:**
- `[Icon 24x24] [Name + xCount] [Description snippet] [USE/INFO button]`
- USE button for consumables (disabled if full HP for heal items)
- No action button for ingredients (just display)
- Equipment rows in their own tab or section

**Visual improvements:**
- Dark panel background matching StatsMenu aesthetic
- Category tab buttons with active/inactive color states
- ScrollContainer for overflow when many items

---

## Phase 8 — Shop & Loot Integration

### Modify: Rork's NPC scene
Add 4 ShopItemEntry sub-resources for the ingredients to `ShopStock` on the NpcRork node in `MAPP.tscn` (or the NpcRork.tscn inherited scene).

### Modify: Enemy loot tables
Add `BonusLootItemPath` to select enemy .tres files:
- Wisplet → Ecto Essence (thematic: spectral enemy drops spectral ingredient)
- Other enemies → Mystery Meat, Bread, Sugar as appropriate

---

## Implementation Order

1. Phase 1 — ItemType enum + ItemData field (foundation)
2. Phase 2 — CookingLogic + RecipeData + tests (pure logic)
3. Phase 3 — Ingredient/food/recipe .tres files (data)
4. Phase 4 — CookingMinigame (rhythm gameplay)
5. Phase 5 — CookingMenu (UI)
6. Phase 6 — PauseMenu integration (wiring)
7. Phase 7 — InventoryMenu refactor (can be parallel with 4-6)
8. Phase 8 — Shop/loot integration (content)

## New Files

| File | Type |
|---|---|
| `core/data/ItemType.cs` | Enum |
| `core/data/CookingQuality.cs` | Enum |
| `core/data/RecipeIngredient.cs` | Record struct |
| `core/data/CookingLogic.cs` | Pure static logic |
| `core/data/RecipeData.cs` | Godot Resource |
| `scenes/menus/CookingMinigame.cs` + `.tscn` | Rhythm minigame |
| `scenes/menus/CookingMenu.cs` + `.tscn` | Cooking menu UI |
| `SennenRpg.Tests/Logic/CookingLogicTests.cs` | NUnit tests |
| `resources/items/ingredient_*.tres` (x4) | Ingredient items |
| `resources/items/cooked_*.tres` (x6) | Cooked food (2 recipes x 3 qualities) |
| `resources/recipes/recipe_*.tres` (x2) | Recipe definitions |

## Verification

1. `dotnet test` — CookingLogic tests pass
2. Pause menu → Cook → shows recipes with ingredient counts
3. Buy ingredients from Rork → ingredient count updates in Cook menu
4. Cook with sufficient ingredients → minigame plays → food produced with quality label
5. All-perfect minigame → Perfect quality food with 1.5x heal
6. All-miss minigame → Burnt quality food with 0.5x heal
7. Use cooked food from inventory → heals correct amount
8. Inventory menu → category tabs filter correctly
9. Kill enemies → ingredients drop as loot
