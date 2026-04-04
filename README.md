# SennenRpg

A 2D RPG built in **Godot 4.6** with **C# (.NET 10)**. The game features a turn-based battle system with rhythm minigames, a branching dialog system via Dialogic 2, and a persistent overworld with NPCs, random encounters, and save points.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Godot 4.6 |
| Language | C# (.NET 10) — all gameplay logic |
| Dialog | Dialogic 2 plugin (GDScript), accessed only through `DialogicBridge.cs` |
| Camera | PhantomCamera 2D plugin |
| Serialization | `System.Text.Json` |
| Renderer | GL Compatibility on D3D12 (Windows) |

---

## Getting Started

1. Clone the repository.
2. Open the project in **Godot 4.6** (mono/C# build).
3. Enable plugins in **Project → Project Settings → Plugins**: `Dialogic 2` and `PhantomCamera`.
4. Press **F5** to run. The game starts at `MainMenu.tscn`.

### Controls

| Action | Keys |
|---|---|
| Move | WASD / Arrow keys |
| Interact / Confirm | Z / Enter / Gamepad A |
| Cancel / Back | X / Gamepad B |
| Pause | Escape |

---

## What the Game Currently Does

### Main Menu
- **New Game** — resets all state, seeds the player with a Bandage item, and loads the test map.
- **Continue** — loads from `user://save.json` and returns to the last visited map. The button is disabled if no save file exists.
- **Credits** — shows game and music attribution (Nick Sanft, Divora).

### Overworld
- **Player movement** — 8-directional movement at 80 px/s. Blocked during dialog, battle, and pause states.
- **Camera** — PhantomCamera2D follows the player with smooth interpolation. Bounds are automatically calculated from the `Ground` TileMapLayer's used rect.
- **NPCs** — interactable characters with configurable Dialogic timelines. Alternate timelines trigger based on game flags (e.g., after first conversation). The NPC faces the player during dialog, then resets to its default direction after. An optional examine description shows a Talk/Examine/Cancel menu. NPCs can patrol between waypoints when idle.
- **Vendor NPCs** — a VendorNpc subclass that optionally plays a greeting timeline first, then opens a shop UI with configurable `ShopItemEntry` stock.
- **Signs** — `InteractSign` objects display multi-line text in a bottom panel popup. Press Z or X to dismiss.
- **Treasure chests** — `Chest` objects give the player an item on first open (flag-tracked; stays open on revisit).
- **Journal prop** — `JournalProp` opens a scrollable list of journal entries. Each entry opens in a full-screen popup.
- **Save points** — interactable stars that open a YES/NO confirmation dialog, then write to disk.
- **Encounter triggers** — invisible Area2D triggers that start a battle when walked into. Supports `OneShot` (fires once then removes itself) and `PersistenceFlag` (remembers across save/load cycles).
- **Enemy pawns** — visible enemy sprites that idle until the player walks within detection range, then chase the player and start a battle on contact.
- **Map exits** — transition to another map scene with a configurable spawn point ID. Supports walk-off (auto-trigger) and door (interact) modes. Optional auto-save on transition (enabled on dungeon floor staircases).
- **Cutscene triggers** — play a Dialogic timeline automatically when a map loads (fires once per `OnceFlag`).
- **Random encounters** — each map has a configurable `RandomEncounterTable`. Every 32 pixels of player movement, the game rolls each encounter's `EncounterChancePerStep` (0–100%). Multiple encounters can coexist on one map with independent probabilities.
- **Pause menu** — ESC overlay with Resume, Save, Settings, Items, Cook, Equipment, Spells, Stats, and Main Menu.
- **HUD** — bottom-left HP/MP bars, player name, and gold. MP bar hidden for classes with 0 MaxMp. Bars animate smoothly on change.
- **Now-playing popup** — top-left overlay showing current track title and artist when BGM changes.
- **Area name popup** — bottom-center label with dark background that fades in on map load.
- **Minimap** — top-right corner showing player, NPCs, exits, save points, staircase markers, and quest objective markers.
- **Low HP warning** — HP bar pulses red when below 25%.

### Visual Effects
- **SNES theme** — Chrono Trigger-inspired blue gradient panels with PressStart2P pixel font across all menus.
- **Pixel mosaic transitions** — screen pixelates into large blocks during map transitions, then un-pixelates at the destination.
- **Teleport dissolve** — when casting Teleport Home, the player sprite dissolves bottom-to-top like sand, then reforms at the destination.
- **Dynamic battle backgrounds** — gradient tinted to match the overworld tile color where the encounter started.
- **Battle intro zoom** — enemy sprite bounces in with overshoot scaling on battle start.
- **Critical hit slow-motion** — brief time slowdown on critical hits for dramatic impact.
- **Victory animation** — enemy shrinks and fades on defeat, victory fanfare plays.
- **World map parallax** — subtle cloud layer scrolling behind the tile map for depth.

### Cutscene System
Reusable cinematic cutscene framework with:
- Letterbox bars (slide in/out from top and bottom)
- Camera panning to any position
- NPC walking to target positions
- Name cards with fade-in/hold/fade-out
- Dialogic dialog integration with portraits
- Flag setting to prevent replay

First usage: Rork introduces himself when the player first enters Mellyr Outpost.

### MAPP (The Mapp Tavern)
A fully hand-crafted map scene (`scenes/overworld/MAPP.tscn`) demonstrating all overworld objects. Built entirely in code — no external tile sheets required. Features:
- Procedural TileMapLayer floor (wood planks + stone border)
- Ceiling beams, rugs, a fireplace mantelpiece, bottle rack, windows, and a staircase
- Named NPCs (Shizu, the Barkeep, and others) with dialog timelines
- Vendor NPC (Barkeep) selling items
- Furniture objects: tables, chairs, bar stools
- Interactive sign, dartboard, bar drinks, and journal prop
- All scene objects render in the Godot editor via `[Tool]` attributes

### Mellyr Outpost
A small town west of the MAPP Tavern. Players can hire NPC residents (Rain and Lily) via Rork's residency shop. Hired residents generate passive rewards while the player explores dungeons:
- **Rain** — passively earns gold every 10 steps on qualifying maps (hard cap: 200g).
- **Lily** — forges randomized equipment (deterministic from seed + player level) every 10 steps (hard cap: 5 items).
- Rewards are collected by talking to the residents in the outpost. Lily-forged items appear in both the inventory and equipment menus.
- Residents are spawned dynamically in code (not baked into the .tscn) to avoid Godot inherited-scene issues.

### Quest System
NPCs can offer quests via the `QuestGiver` child node. Quests have typed conditions (e.g., kill count) and reward choices shown on a reward screen. Quest state is tracked by `QuestManager` and persisted via save data.

### Multi-Class System
Players can switch between four classes (Bard, Fighter, Ranger, Mage) at Rork's tavern. Each class has independent progression:
- **Separate levels and stats** — switching classes preserves all progress; return to a class right where you left off.
- **Class-specific growth rates** — Fire Emblem-style probabilistic stat growth (e.g., Fighter favors HP/ATK, Mage favors MAG/RES).
- **Class-specific combat** — Fight action routes to different minigames per class (Bard→RhythmStrike, Fighter→FightBar, Ranger→RangerAim, Mage→MageRuneInput).
- **Cross-class bonuses** — reaching level thresholds in one class grants permanent stat bonuses or spell unlocks for ALL classes (e.g., Fighter Lv5 → +5 ATK for all classes).
- **Visible in StatsMenu** — shows all class levels and earned cross-class bonuses.
- **Level-up screen** — shows class name and announces newly unlocked cross-class bonuses.

### Cooking System
Players combine ingredients into food items via a rhythm minigame:
- **Recipes** — Mystery Meat Sandwich (Meat + Bread) and Ecto Cooler (Ecto Essence + Sugar).
- **Cooking minigame** — single-lane rhythm game with configurable difficulty. Tracks Perfect/Good/Miss separately.
- **Quality tiers** — Burnt (0.5x heal), Normal (1.0x), Perfect (1.5x). Each tier produces a distinct item with different stats and description.
- **Ingredient sources** — purchasable from Rork at the MAPP Tavern or dropped by enemies.
- **Accessible from pause menu** — COOK button between Items and Equipment.
- **Categorized inventory** — items tagged as Consumable, Ingredient, Equipment, KeyItem, or Repel. Inventory menu has filter tabs and item stacking (e.g., "Bread x3").

### Battle System

Battles are a turn-based state machine: **Intro → PlayerTurn ↔ EnemyTurn/RhythmPhase → Victory or Defeat**.

#### Player Turn — four actions:

| Action | Behaviour |
|---|---|
| **Fight** | Routes to class-specific minigame: Bard→RhythmStrike, Fighter→FightBar, Ranger→RangerAim, Mage→MageRuneInput. Grade determines damage multiplier. Screen shake and enemy flash on hit; stronger on crits. |
| **Perform** | Opens the Bard skills sub-menu. Each skill launches a rhythm minigame (see below). Results are shown via Dialogic timeline. |
| **Item** | Opens the inventory. Using an item heals the player and passes the turn. |
| **Flee** | 50% chance to escape. On failure, the enemy's turn proceeds as normal. |

#### Damage formula

```
damage = max(1, round(playerATK × gradeMultiplier) − enemyDEF)
gradeMultiplier: Perfect = 1.5×, Good = 1.0×, Miss = 0.5×
```

#### Enemy Turn

1. A random `BattleDialogLine` is shown (or a full Dialogic timeline plays if `BattleTimelinePath` is set on the `EnemyData`).
2. The **RhythmArena** activates — a 4-lane note highway scrolls obstacles downward.
3. The player presses confirm on each lane's beat to avoid damage. Missing notes deals damage.
4. After the configured number of measures the phase ends and play returns to PlayerTurn.

#### Bard Skills (Perform sub-menu)

| Skill | Description |
|---|---|
| Bardic Inspiration | Single-lane rhythm pattern |
| Lullaby | Slow, sustained note holds |
| War Cry | Fast multi-lane pattern |
| Serenade | Mixed lanes with pauses |
| Dissonance | Irregular rhythm challenge |
| Charm | Separate minigame — time button presses within a scrolling window |

Each skill plays a rhythm minigame and shows a Dialogic result timeline (`battle_skill_result.dtl` or `battle_charm_result.dtl`) with the outcome. `PerformanceScore` accumulates Perfect/Good/Miss counts across the full battle and writes a summary to a Dialogic variable at victory.

#### Victory / Defeat

- **Victory** — enemy HP reaches 0. Gold and EXP are awarded. `battle_victory.dtl` plays, then the game returns to the overworld.
- **Flee** — 50% chance. `battle_fled.dtl` plays, then returns to overworld immediately.
- **Defeat** — player HP reaches 0. `battle_game_over.dtl` plays, then returns to the main menu.

### Dialog System

All dialog runs through **Dialogic 2**, accessed exclusively via `DialogicBridge.cs`. Direct calls to Dialogic from gameplay code are prohibited.

- **Flag signals** — a Dialogic `Signal Event` with text `flag:my_flag` automatically sets `GameManager.SetFlag("my_flag", true)` with no C# handler needed.
- **Pre-timeline sync** — `StartTimelineWithFlags()` pushes all `GameManager.Flags` and common variables (`playerName`, `gold`) into Dialogic before starting, so timelines can branch on game state.
- **Safety net** — if `GameState` is stuck on `Dialog` but Dialogic is not running *and* Dialogic was the one that set the state, the state resets to `Overworld` after 5 seconds. Non-Dialogic UI (signs, journal, shop) that also uses `GameState.Dialog` for player-blocking is excluded.
- **Timeline preloading** — `OverworldBase` calls `ResourceLoader.LoadThreadedRequest()` on all NPC timelines when a map loads, so the first conversation has no stutter.
- **DialogRegistry** — an optional short-name registry (`DialogRegistry.Instance.StartTimeline("intro")`) to avoid scattering full `res://` paths across game code.

### Save System

Saves to `user://save_{slot}.json` (3 slots) using `System.Text.Json`. The saved data includes:

```
PlayerHp, PlayerMaxHp, Gold, Exp, PlayerLevel,
LastMapPath, LastSavePointId, LastSpawnId,
Flags (dictionary), InventoryItemPaths (list),
OwnedEquipmentPaths, EquippedItemPaths,
KillCounts, ActiveQuestIds, CompletedQuestIds,
TownStepCounter, PendingRainGold, PendingLilyRecipes,
DynamicEquipmentInventory, EquippedDynamicItemIds,
ClassProgressionEntries, ActiveClassName,
PlayTimeSeconds, PlayerClassName, PaletteColors
```

Save slot cards show player name, level, class, and play time. Legacy saves (pre-multi-class) are auto-migrated on load.

---

## Project Structure

```
SennenRpg/
├── autoloads/              # Global singletons (registered in project.godot)
│   ├── GameManager.cs      # Facade: delegates to domain data classes in core/data/
│   ├── SaveManager.cs      # Save/load JSON
│   ├── AudioManager.cs     # BGM crossfade + SFX pool
│   ├── SceneTransition.cs  # Async scene changes with fade/flash
│   ├── DialogicBridge.cs   # Dialogic 2 C# wrapper
│   ├── BattleRegistry.cs   # Passes encounter data across scene transitions
│   ├── DialogRegistry.cs   # Optional short-name → full-path timeline map
│   ├── RhythmClock.cs      # Master beat clock for rhythm minigames
│   ├── QuestManager.cs     # Quest state machine, condition tracking
│   └── SettingsManager.cs  # User preferences, difficulty, volume
│
├── core/
│   ├── data/               # Pure-logic + Resource subclasses + domain data
│   │   ├── CharacterStats.cs, EnemyData.cs, EncounterData.cs, ItemData.cs
│   │   ├── ShopItemEntry.cs, NpcResidencyEntry.cs, QuestData.cs
│   │   ├── RhythmConstants.cs      # Beat timing, grade windows, multipliers
│   │   ├── PerformanceScore.cs     # Hit/miss/perfect accumulator
│   │   ├── Flags.cs                # Flag name constants + helpers
│   │   ├── ItemLogic.cs            # CanUse / Apply logic (pure)
│   │   ├── ShopLogic.cs            # CanAfford / Buy logic (pure)
│   │   ├── NpcLogic.cs             # SelectTimeline (pure, testable)
│   │   ├── TownRewardLogic.cs      # Mellyr Outpost passive reward ticking
│   │   ├── LilyForgeLogic.cs       # Deterministic equipment generation
│   │   ├── DialogicSignalParser.cs # Parses "flag:x", "give_item:y" strings
│   │   ├── JournalData.cs          # Journal entry list helpers
│   │   ├── PlayerProgressionData.cs  # Gold, exp, level (GameManager domain)
│   │   ├── PlayerCombatData.cs       # HP, MP, stats, growth (GameManager domain)
│   │   ├── InventoryData.cs          # Items, spells, equipment (GameManager domain)
│   │   ├── WorldStateData.cs         # Map state, spawn points (GameManager domain)
│   │   └── MellyrRewardData.cs       # Rain gold, Lily recipes (GameManager domain)
│   ├── interfaces/
│   │   ├── IInteractable.cs
│   │   └── IInteractable.cs
│   └── extensions/
│       └── NodeExtensions.cs
│
├── scenes/
│   ├── boot/               # First scene (Bootstrap)
│   ├── menus/              # MainMenu, PauseMenu, GameOver, InventoryMenu, ShopMenu
│   ├── hud/                # GameHud, MinimapHud, AreaNameLabel, DialogHistoryOverlay
│   ├── player/             # Player.tscn + Player.cs
│   ├── overworld/
│   │   ├── OverworldBase.tscn / .cs   # Base class for all maps
│   │   ├── MAPP.tscn / .cs / .Events.cs  # Mapp Tavern (partial class split)
│   │   ├── maps/                      # Other maps (MappGarden uses .Builders.cs partial)
│   │   │   └── mellyr/               # MellyrOutpost — resident hiring town
│   │   └── objects/
│   │       ├── Npc.cs                 # NPC base (patrol, dialog, emote)
│   │       ├── VendorNpc.cs           # Extends Npc — opens ShopMenu
│   │       ├── RorkTownNpc.cs        # Extends Npc — opens ResidencyShopMenu
│   │       ├── QuestGiver.cs         # Child node for NPCs offering quests
│   │       ├── SavePoint.cs
│   │       ├── EncounterTrigger.cs
│   │       ├── MapExit.cs
│   │       ├── SpawnPoint.cs
│   │       ├── CutsceneTrigger.cs
│   │       ├── InteractSign.cs        # Readable sign/notice board
│   │       ├── Chest.cs               # One-time treasure chest
│   │       ├── JournalProp.cs         # Opens journal entry list
│   │       ├── DartboardProp.cs       # Decorative dartboard
│   │       ├── BarDrinkProp.cs        # Decorative bar drink
│   │       ├── InteractPromptBubble.cs
│   │       ├── SignReaderPopup.cs
│   │       ├── JournalMenuPopup.cs
│   │       ├── JournalEntryPopup.cs
│   │       ├── NpcInteractMenu.cs
│   │       └── furniture/
│   │           ├── TableFurniture.cs
│   │           ├── ChairFurniture.cs
│   │           └── BarStoolFurniture.cs
│   └── battle/
│       ├── BattleScene.tscn / .cs     # Root state machine
│       ├── BattleAttackResolver.cs    # Static: minigame results → damage
│       ├── BattleStatusEffects.cs     # Status effect state & Dialogic signal handling
│       ├── BattleHUD.cs               # CanvasLayer 10
│       ├── ActionMenu.cs, SubMenu.cs
│       ├── ui/                        # DamageNumber, EnemyNameplate
│       └── rhythm/                    # CharmMinigame, BardMinigameBase + skills + patterns
│
├── resources/
│   ├── characters/         # player_stats.tres
│   ├── enemies/            # enemy_001 – enemy_004 .tres
│   ├── encounters/         # encounter_001 – encounter_004 .tres
│   └── items/              # item_001.tres (Bandage)
│
├── dialog/
│   ├── characters/         # .dch Dialogic character definitions
│   └── timelines/          # .dtl timeline files
│
├── assets/
│   ├── sprites/
│   ├── fonts/
│   ├── audio/
│   └── shaders/
│
├── SennenRpg.Tests/        # NUnit tests — pure C# logic, no Godot runtime
│   └── Logic/              # DialogicSignalParser, Flags, ItemLogic, ShopLogic,
│                           # NpcLogic, JournalData, PerformanceScore, RhythmConstants
│
└── tests/gdunit/           # GdUnit4 tests — Godot runtime required
    # DialogicBridge, GameManager, NpcDialog, RhythmClock
```

---

## Tests

Two complementary test suites:

### NUnit (pure logic — no Godot required)

```bash
dotnet test SennenRpg.Tests/SennenRpg.Tests.csproj
```

Covers all stateless business logic in `core/data/`. Source files are included selectively (not via project reference) to keep the test assembly free of Godot scene dependencies.

### GdUnit4 (Godot runtime required)

Run from the Godot editor: open the GdUnit4 panel → right-click `tests/gdunit/` → **Run Tests**.

Or headless:
```bash
godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd -d res://tests/gdunit/
```

Covers autoload integration: `GameManager`, `DialogicBridge`, `NpcDialog`, `RhythmClock`.

---

## Autoloads Reference

All autoloads are accessible via their static `Instance` property.

### `GameManager`

Thin facade over domain data classes. All public properties and methods are unchanged — callers use `GameManager.Instance.X` as before. Internally delegates to `PlayerProgressionData`, `PlayerCombatData`, `InventoryData`, `WorldStateData`, and `MellyrRewardData` (all in `core/data/`). Everything that must survive a scene transition lives here.

```csharp
GameManager.Instance.CurrentState        // GameState enum
GameManager.Instance.PlayerStats         // CharacterStats (live copy)
GameManager.Instance.Gold
GameManager.Instance.Exp
GameManager.Instance.Flags               // Dictionary<string, bool>
GameManager.Instance.InventoryItemPaths  // List<string> of .tres paths
GameManager.Instance.LastMapPath
GameManager.Instance.LastSpawnId

GameManager.Instance.SetState(GameState.Battle);
GameManager.Instance.SetFlag("talked_to_npc_01", true);
GameManager.Instance.GetFlag("talked_to_npc_01");
GameManager.Instance.HealPlayer(10);
GameManager.Instance.HurtPlayer(5);
GameManager.Instance.AddGold(15);
GameManager.Instance.AddExp(10);
```

### `SceneTransition`

```csharp
await SceneTransition.Instance.GoToAsync("res://scenes/overworld/maps/MyMap.tscn");
await SceneTransition.Instance.ToBattleAsync(encounterData);
```

### `AudioManager`

```csharp
AudioManager.Instance.PlayBgm("res://assets/audio/bgm/town.ogg");
AudioManager.Instance.PlaySfx("res://assets/audio/sfx/step.wav");
AudioManager.Instance.StopBgm();
```

### `DialogicBridge`

```csharp
// Always use this in preference to StartTimeline — syncs flags first
DialogicBridge.Instance.StartTimelineWithFlags("res://dialog/timelines/npc_hello.dtl");

// One-shot callback when timeline ends
DialogicBridge.Instance.ConnectTimelineEnded(new Callable(this, MethodName.OnDone));

// Read a Dialogic variable after a timeline (e.g. a choice result)
var chosen = DialogicBridge.Instance.GetVariable("playerChoice").AsString();
```

### `DialogRegistry`

```csharp
// Register at startup (e.g. in a map's _Ready)
DialogRegistry.Instance.Register("intro_cutscene", "res://dialog/timelines/intro.dtl");

// Use anywhere — accepts either a short key or a full res:// path
DialogRegistry.Instance.StartTimeline("intro_cutscene");
```

### `RhythmClock`

Drives the master beat for all rhythm minigames. BPM and offset are set per-enemy before the minigame starts.

```csharp
RhythmClock.Instance.Bpm = 120f;
RhythmClock.Instance.Start();
RhythmClock.Instance.Stop();
double beat = RhythmClock.Instance.CurrentBeat;
```

---

## Core Data Types

### `EnemyData` (Resource)

Create as `.tres` files in `resources/enemies/`. All fields are inspector-editable.

| Field | Type | Description |
|---|---|---|
| `EnemyId` | string | Unique ID, used to find Perform timelines |
| `DisplayName` | string | Shown in battle UI and dialog |
| `FlavorText` | string | Shown by the "Check" skill option |
| `Stats` | CharacterStats | HP, ATK, DEF, Speed, InvincibilityDuration |
| `BattleSprite` | Texture2D | Enemy graphic (placeholder polygon if null) |
| `BardicActOptions` | string[] | Skill names shown in the Perform sub-menu |
| `ActOptions` | string[] | Legacy Act options (used for Check/flavor text) |
| `ActResultTexts` | string[] | Fallback text if no Dialogic timeline exists |
| `BattleDialogLines` | string[] | Random lines shown before each attack |
| `BattleTimelinePath` | string | Optional Dialogic timeline for the enemy's turn |
| `GoldDrop` / `ExpDrop` | int | Rewards on victory |
| `AttackPatternScene` | PackedScene | Rhythm pattern scene passed to `RhythmArena` |
| `BattleBpm` | float | BPM of this enemy's battle track |
| `BattleBgmPath` | string | BGM file for this enemy's battle |
| `BattleBeatOffsetSec` | float | Seconds from audio start to beat 1 |

### `ShopItemEntry` (Resource)

Configure on a `VendorNpc` via the `ShopStock` inspector array.

```
ItemPath    string   res:// path to an ItemData .tres
Price       int      Gold cost
```

### `EncounterData` (Resource)

```
Enemies                 Array<EnemyData>   — currently battles use index 0
BackgroundId            string             — for future background art switching
EncounterChancePerStep  float              — 0–100, checked every 32 px of movement
BattleBgmPath           string             — overrides enemy BGM if set
BattleBpm               float              — overrides enemy BPM if set
```

### `CharacterStats` (Resource)

```
MaxHp / CurrentHp      int
Attack / Defense       int
Speed                  float   (pixels per second)
InvincibilityDuration  float   (seconds after being hit)
```

### `ItemData` (Resource)

```
ItemId / DisplayName / Description   string
Icon                                 Texture2D
Type                                 ItemType (Consumable, Ingredient, Equipment, KeyItem, Repel)
HealAmount                           int
RepelSteps                           int
```

---

## Adding Content

### New Enemy

1. Create `resources/enemies/{id}.tres` — set all `EnemyData` fields.
2. Create a rhythm pattern scene in `scenes/battle/rhythm/patterns/` extending `RhythmPatternBase` and assign it to `AttackPatternScene`.
3. Create `resources/encounters/{id}.tres` — add your enemy to `Enemies`.
4. Optionally create Perform dialog timelines at `dialog/timelines/act_{enemyId}_{optionName}.dtl`.
5. Add your encounter to a map's `RandomEncounterTable`, or place an `EncounterTrigger`.

### New Map

1. In Godot editor: **Scene → New Inherited Scene** from `OverworldBase.tscn`. Save to `scenes/overworld/maps/MyMap.tscn`.
2. Create `MyMap.cs` extending `OverworldBase`. Set `MapId` in the inspector.
3. Add TileMapLayers named `Ground`, `Walls`, `Objects`. Add a `YSort` node for entities.
4. Place `SpawnPoint.tscn` instances in the scene. Set `SpawnId = "default"` for the default spawn.
5. Place NPCs, SavePoints, EncounterTriggers, and MapExits under `YSort`.
6. Assign `RandomEncounterTable` encounters in the inspector for random battles.

### New NPC

1. Add an `Npc.tscn` instance to `YSort` in your map.
2. Set `NpcId` (unique string), `DisplayName`, and `TimelinePath` (a `.dtl` file).
3. Optionally fill `AltTimelinePaths` / `AltRequiredFlags` for alternate dialog after events.
4. Create the timeline in the Dialogic editor. The `talked_to_{NpcId}` flag is set automatically after the first conversation ends.

### New Item

1. Create `resources/items/{name}.tres` with type `ItemData`.
2. Set `DisplayName`, `HealAmount`, etc.
3. The item will appear in the battle Item menu. Give it to the player via `GameManager.Instance.AddItem("res://resources/items/{name}.tres")`.

### Adding Dialog Branches on Flags

Inside any `.dtl` timeline, use a **Condition** node that reads a Dialogic variable. Before starting the timeline, `StartTimelineWithFlags()` automatically copies all `GameManager.Flags` into Dialogic variables — no manual sync needed.

To set a flag from inside a timeline, add a **Signal Event** node with text `flag:my_flag_name`. The bridge handles the rest.

---

## Architecture Notes

### No GDScript in gameplay code
All logic is C#. GDScript exists only in the `addons/` plugin files. Dialogic (a GDScript plugin) is wrapped entirely by `DialogicBridge.cs`.

### `[Tool]` attribute for procedurally-built scenes
Objects that build their visuals in `_Ready()` (NPCs, furniture, signs, chests, props) use the `[Tool]` attribute so they are visible in the Godot editor scene preview. **`[Tool]` is not inherited in Godot 4 C#** — every class in an inheritance chain needs its own attribute. All autoload access and group registration inside `[Tool]` scripts must be guarded with `if (!Engine.IsEditorHint())`.

### Async scene transitions
`SceneTransition.GoToAsync()` and `ToBattleAsync()` are `async Task` methods. Always `await` them; fire-and-forget is used only for non-blocking background tasks.

### Passing data across scenes
Godot destroys the current scene tree on every scene change. Cross-scene data lives in autoloads:
- **Battle encounter** — `BattleRegistry.SetPendingEncounter()` before transitioning; `GetPendingEncounter()` consumes it once in `BattleScene._Ready()`.
- **Spawn position** — `GameManager.LastSpawnId` is set by `MapExit` and consumed once by `OverworldBase.GetSpawnPosition()`.
- **Persistent state** — everything else lives in `GameManager` (flags, stats, gold). Internally, GameManager delegates to domain data classes (`PlayerProgressionData`, `PlayerCombatData`, `InventoryData`, `WorldStateData`, `MellyrRewardData`) for separation of concerns.

### Resource mutability
`CharacterStats` is a `Resource` — it is shared by default. Always call `.Duplicate()` before modifying if you need a local copy. `GameManager` holds the live player stats copy.

### CanvasLayer draw order

| Layer | Node |
|---|---|
| 0 | InteractPromptBubble / NPC name labels |
| 2 | GameHud (overworld HP, MP, gold) |
| 3 | AreaNameLabel, NowPlayingPopup |
| 4 | MinimapHud |
| 10 | BattleHUD |
| 50 | PauseMenu |
| 51 | InventoryMenu, ShopMenu, EquipmentMenu, CookingMenu |
| 52 | StatsMenu, ClassChangeMenu, NpcInteractMenu |
| 55 | SignReaderPopup |
| 56 | JournalEntryPopup |
| 60 | SaveConfirmDialog |
| 70 | LevelUpScreen |
| 100 | SceneTransition (fade overlay — always on top) |

---

## Enemies Reference

| ID | Name | HP | ATK | DEF | Pattern |
|---|---|---|---|---|---|
| `enemy_001` | Wisplet | 20 | 5 | 0 | PatternRandom |
| `enemy_002` | Thornling | 35 | 8 | 2 | Horizontal sweep |
| `enemy_003` | Gloomfish | 60 | 15 | 3 | Radial burst |
| `enemy_004` | Dustmote | 12 | 4 | 0 | Bouncing chaos |

---

## File Naming Conventions

| Type | Convention | Example |
|---|---|---|
| Scenes | PascalCase.tscn | `BattleScene.tscn` |
| C# scripts | PascalCase.cs (matches scene) | `BattleScene.cs` |
| Resources | snake_case.tres | `enemy_001.tres` |
| Dialog timelines | snake_case.dtl | `npc_intro.dtl` |
| Dialogic characters | PascalCase.dch | `Barkeep.dch` |
| Folders | snake_case/ | `scenes/battle/` |
| Perform timelines | `act_{enemyId}_{optionName}.dtl` | `act_enemy_001_greet.dtl` |
| Battle turn timelines | `battle_{enemyId}_turn.dtl` | `battle_enemy_003_turn.dtl` |

---

## Common Pitfalls

- **`GetNode<T>()` in constructors** — only call in `_Ready()` or later.
- **Dialogic `timeline_ended`** — this is a GDScript signal. Always connect via `DialogicBridge.ConnectTimelineEnded()`, never directly.
- **`CharacterStats` sharing** — it is a `Resource`; call `.Duplicate()` before mutating if you need a local copy.
- **`SceneTransition.GoToAsync` is async** — always `await` it or the code after it runs before the transition finishes.
- **`.tres` C# resource types** — use `type="Resource"` with a `script=` reference in the `.tscn`, not `type="ClassName"`. Godot's C++ parser cannot instantiate C# types by name. Export arrays as `Resource[]` and cast with `OfType<T>()` at runtime. For single typed exports of external `.tres` files, export as `Resource?` and cast with `as T` at runtime.
- **`[Tool]` not inherited** — Godot 4 C# does not propagate `[Tool]` to subclasses. Add it explicitly on every class that needs editor `_Ready()` execution.
- **`GetChildCount()` guard with pre-baked children** — if a `.tscn` pre-bakes a `CollisionShape2D`, `GetChildCount()` is already 1 when `_Ready()` first runs. Use `> 1` instead of `> 0` for the duplicate-build guard.
- **`GrabFocus()` before `AddChild()`** — calling `GrabFocus()` before the node is in the scene tree throws an error. Call it after `AddChild()`, wrapped in `CallDeferred` for safety.
- **DialogicBridge safety net** — only fires when `_dialogicOwnsDialog == true` (set in `StartTimeline()`, cleared in `OnTimelineEndedInternal()`). Non-Dialogic UI that uses `GameState.Dialog` for player-blocking does not trigger it.
