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
- **Map exits** — transition to another map scene with a configurable spawn point ID. Supports walk-off (auto-trigger) and door (interact) modes.
- **Cutscene triggers** — play a Dialogic timeline automatically when a map loads (fires once per `OnceFlag`).
- **Random encounters** — each map has a configurable `RandomEncounterTable`. Every 32 pixels of player movement, the game rolls each encounter's `EncounterChancePerStep` (0–100%). Multiple encounters can coexist on one map with independent probabilities.
- **Pause menu** — ESC overlay with Resume, Save, and Main Menu options.
- **HUD** — bottom-left HP bar and player name, always visible during overworld.

### MAPP (The Mapp Tavern)
A fully hand-crafted map scene (`scenes/overworld/MAPP.tscn`) demonstrating all overworld objects. Built entirely in code — no external tile sheets required. Features:
- Procedural TileMapLayer floor (wood planks + stone border)
- Ceiling beams, rugs, a fireplace mantelpiece, bottle rack, windows, and a staircase
- Named NPCs (Shizu, the Barkeep, and others) with dialog timelines
- Vendor NPC (Barkeep) selling items
- Furniture objects: tables, chairs, bar stools
- Interactive sign, dartboard, bar drinks, and journal prop
- All scene objects render in the Godot editor via `[Tool]` attributes

### Battle System

Battles are a turn-based state machine: **Intro → PlayerTurn ↔ EnemyTurn/RhythmPhase → Victory or Defeat**.

#### Player Turn — four actions:

| Action | Behaviour |
|---|---|
| **Fight** | Opens the `RhythmStrike` timing minigame. Press confirm on the beat for a Perfect hit; near-perfect for Good. Grade determines damage multiplier. |
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

Saves to `user://save.json` using `System.Text.Json`. The saved data includes:

```
PlayerHp, PlayerMaxHp, Gold, Exp,
LastMapPath, LastSavePointId, LastSpawnId,
Flags (dictionary), InventoryItemPaths (list)
```

---

## Project Structure

```
SennenRpg/
├── autoloads/              # Global singletons (registered in project.godot)
│   ├── GameManager.cs      # Master game state
│   ├── SaveManager.cs      # Save/load JSON
│   ├── AudioManager.cs     # BGM crossfade + SFX pool
│   ├── SceneTransition.cs  # Async scene changes with fade/flash
│   ├── DialogicBridge.cs   # Dialogic 2 C# wrapper
│   ├── BattleRegistry.cs   # Passes encounter data across scene transitions
│   ├── DialogRegistry.cs   # Optional short-name → full-path timeline map
│   └── RhythmClock.cs      # Master beat clock for rhythm minigames
│
├── core/
│   ├── data/               # Pure-logic + Resource subclasses
│   │   ├── CharacterStats.cs, EnemyData.cs, EncounterData.cs, ItemData.cs
│   │   ├── ShopItemEntry.cs        # Line item for vendor shop stock
│   │   ├── RhythmConstants.cs      # Beat timing, grade windows, multipliers
│   │   ├── PerformanceScore.cs     # Hit/miss/perfect accumulator
│   │   ├── Flags.cs                # Flag name constants + helpers
│   │   ├── ItemLogic.cs            # CanUse / Apply logic (pure)
│   │   ├── ShopLogic.cs            # CanAfford / Buy logic (pure)
│   │   ├── NpcLogic.cs             # SelectTimeline (pure, testable)
│   │   ├── DialogicSignalParser.cs # Parses "flag:x", "give_item:y" strings
│   │   └── JournalData.cs          # Journal entry list helpers
│   ├── interfaces/
│   │   ├── IInteractable.cs
│   │   └── IDamageable.cs
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
│   │   ├── MAPP.tscn / .cs            # The Mapp Tavern (fully procedural)
│   │   ├── maps/                      # Other map scenes
│   │   └── objects/
│   │       ├── Npc.cs                 # NPC base (patrol, dialog, emote)
│   │       ├── VendorNpc.cs           # Extends Npc — opens ShopMenu
│   │       ├── SavePoint.cs
│   │       ├── EncounterTrigger.cs
│   │       ├── MapExit.cs
│   │       ├── SpawnPoint.cs
│   │       ├── CutsceneTrigger.cs
│   │       ├── EnemyPawn.cs
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

Central state store. Everything that must survive a scene transition lives here.

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
HealAmount                           int
```

---

## Adding Content

### New Enemy

1. Create `resources/enemies/{id}.tres` — set all `EnemyData` fields.
2. Create a rhythm pattern scene in `scenes/battle/rhythm/patterns/` extending `RhythmPatternBase` and assign it to `AttackPatternScene`.
3. Create `resources/encounters/{id}.tres` — add your enemy to `Enemies`.
4. Optionally create Perform dialog timelines at `dialog/timelines/act_{enemyId}_{optionName}.dtl`.
5. Add your encounter to a map's `RandomEncounterTable`, or place an `EnemyPawn` / `EncounterTrigger`.

### New Map

1. In Godot editor: **Scene → New Inherited Scene** from `OverworldBase.tscn`. Save to `scenes/overworld/maps/MyMap.tscn`.
2. Create `MyMap.cs` extending `OverworldBase`. Set `MapId` in the inspector.
3. Add TileMapLayers named `Ground`, `Walls`, `Objects`. Add a `YSort` node for entities.
4. Place `SpawnPoint.tscn` instances in the scene. Set `SpawnId = "default"` for the default spawn.
5. Place NPCs, SavePoints, EncounterTriggers, MapExits, and EnemyPawns under `YSort`.
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
- **Persistent state** — everything else lives in `GameManager` (flags, stats, gold).

### Resource mutability
`CharacterStats` is a `Resource` — it is shared by default. Always call `.Duplicate()` before modifying if you need a local copy. `GameManager` holds the live player stats copy.

### CanvasLayer draw order

| Layer | Node |
|---|---|
| 0 | InteractPromptBubble / NPC name labels |
| 2 | GameHud (overworld HP) |
| 10 | BattleHUD |
| 50 | PauseMenu |
| 55 | SignReaderPopup |
| 56 | JournalEntryPopup |
| 60 | SaveConfirmDialog |
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
- **`.tres` C# resource types** — use `type="Resource"` with a `script=` reference in the `.tscn`, not `type="ClassName"`. Godot's C++ parser cannot instantiate C# types by name. Export the property as `Resource[]` and cast with `OfType<T>()` at runtime.
- **`[Tool]` not inherited** — Godot 4 C# does not propagate `[Tool]` to subclasses. Add it explicitly on every class that needs editor `_Ready()` execution.
- **`GetChildCount()` guard with pre-baked children** — if a `.tscn` pre-bakes a `CollisionShape2D`, `GetChildCount()` is already 1 when `_Ready()` first runs. Use `> 1` instead of `> 0` for the duplicate-build guard.
- **`GrabFocus()` before `AddChild()`** — calling `GrabFocus()` before the node is in the scene tree throws an error. Call it after `AddChild()`, wrapped in `CallDeferred` for safety.
- **DialogicBridge safety net** — only fires when `_dialogicOwnsDialog == true` (set in `StartTimeline()`, cleared in `OnTimelineEndedInternal()`). Non-Dialogic UI that uses `GameState.Dialog` for player-blocking does not trigger it.
