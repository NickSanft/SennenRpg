# SennenRpg

An Undertale-inspired 2D RPG built in **Godot 4.6** with **C# (.NET 10)**. The game features a turn-based battle system with a real-time dodge phase, a branching dialog system via Dialogic 2, a persistent overworld with NPCs and random encounters, and a mercy/spare route system that tracks how you treat enemies.

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
| Battle dodge | WASD / Arrow keys (same as overworld) |

---

## What the Game Currently Does

### Main Menu
- **New Game** — resets all state, seeds the player with a Bandage item, and loads the test map.
- **Continue** — loads from `user://save.json` and returns to the last visited map. The button is disabled if no save file exists.

### Overworld
- **Player movement** — 8-directional movement at 80 px/s. Blocked during dialog, battle, and pause states.
- **Camera** — PhantomCamera2D follows the player with smooth interpolation. Bounds are automatically calculated from the `Ground` TileMapLayer's used rect.
- **NPCs** — interactable characters with configurable Dialogic timelines. Alternate timelines trigger based on game flags (e.g., after first conversation). The NPC faces the player during dialog, then resets to its default direction after.
- **Save points** — interactable stars that open a native YES/NO confirmation dialog, then write to disk.
- **Encounter triggers** — invisible Area2D triggers that start a battle when walked into. Supports `OneShot` (fires once then removes itself) and `PersistenceFlag` (remembers across save/load cycles).
- **Enemy pawns** — visible enemy sprites that idle until the player walks within detection range, then chase the player and start a battle on contact.
- **Map exits** — transition to another map scene with a configurable spawn point ID. Supports walk-off (auto-trigger) and door (interact) modes.
- **Cutscene triggers** — play a Dialogic timeline automatically when a map loads (fires once per `OnceFlag`).
- **Random encounters** — each map has a configurable `RandomEncounterTable`. Every 32 pixels of player movement, the game rolls each encounter's `EncounterChancePerStep` (0–100%). Multiple encounters can coexist on one map with independent probabilities.
- **Pause menu** — ESC overlay with Resume, Save, and Main Menu options.
- **HUD** — bottom-left HP bar and player name, always visible during overworld.

### Battle System

Battles are a turn-based state machine: **Intro → PlayerTurn ↔ EnemyTurn/DodgePhase → Victory or Defeat**.

#### Player Turn — four actions:

| Action | Behaviour |
|---|---|
| **Fight** | Opens the timing minigame (FightBar). A cursor bounces back and forth; pressing confirm when it is near centre deals more damage. Accuracy above 0.8 is a critical hit. |
| **Act** | Opens a sub-menu of the enemy's Act options. Each option adds mercy percentage. If a Dialogic timeline exists for that act (`act_{enemyId}_{option}.dtl`) it plays before the enemy's turn. |
| **Item** | Opens the inventory. Using an item heals the player and passes the turn. |
| **Mercy** | Opens Spare / Flee. Spare succeeds when mercy % ≥ 100 and the enemy has `CanBeSpared = true`. Flee has a 50% chance of success. |

#### Damage formula

```
damage = max(1, round(playerATK × accuracyMultiplier) − enemyDEF)
accuracyMultiplier = 1.0 + accuracy × 0.5   // range: 1.0× – 1.5×
```

#### Enemy Turn

1. A random `BattleDialogLine` is shown (or a full Dialogic timeline plays if `BattleTimelinePath` is set on the `EnemyData`).
2. The **DodgeBox** becomes visible — a 160×120 arena drawn with a white border.
3. The player controls the **Soul** (red heart) inside the arena.
4. The enemy's **AttackPattern** spawns bullets for 3 seconds.
5. Any bullet that exits the arena is automatically culled (8 px grace margin).
6. After 3 seconds (or if the player's HP hits zero) the phase ends.

#### Bullet types

| Class | Behaviour |
|---|---|
| `StraightBullet` | Falls straight down at constant speed. |
| `DirectedBullet` | Travels in any direction set by the spawning pattern. |
| `BouncingBullet` | Reflects off arena walls for up to 12 seconds. |

#### Attack patterns

| Pattern | Used by | Description |
|---|---|---|
| `Pattern001` | Wisplet (via PatternRandom) | Falling rain — bullets spawn at random X positions above the box every 0.4 s. |
| `Pattern002` | Thornling | Horizontal sweep — a row of 3 bullets flies in from alternating sides every 0.7 s. |
| `Pattern003` | Gloomfish | Radial burst — 6 bullets spread outward in a ring every 1.5 s. The ring rotates each burst. |
| `Pattern004` | Dustmote | Chaos — 1–3 bouncing bullets spawn from random edges every 0.5 s at random angles. |
| `PatternRandom` | Wisplet | Meta-pattern. Picks one of its `Patterns[]` array at random each battle. |

#### Victory / Defeat

- **Kill** — `RegisterKill()` increments kill count and recalculates LV and route. Gold and EXP are added.
- **Spare** — `RegisterSpare()`. No gold or EXP.
- **Flee** — 50% chance. Returns to the overworld immediately.
- **Defeat** — "GAME OVER" screen, then returns to the main menu.

#### Route tracking

| Route | Condition |
|---|---|
| Pacifist (LV 1) | 0 kills + all pacifist flags |
| Neutral (LV 2–3) | 1–19 kills |
| Genocide (LV 4) | ≥ 20 kills |

Route and LV recalculate automatically after every kill or spare.

### Dialog System

All dialog runs through **Dialogic 2**, accessed exclusively via `DialogicBridge.cs`. Direct calls to Dialogic from gameplay code are prohibited.

- **Flag signals** — a Dialogic `Signal Event` with text `flag:my_flag` automatically sets `GameManager.SetFlag("my_flag", true)` with no C# handler needed.
- **Pre-timeline sync** — `StartTimelineWithFlags()` pushes all `GameManager.Flags` and common variables (`playerName`, `gold`, `love`) into Dialogic before starting, so timelines can branch on game state.
- **Post-timeline sync** — after every timeline ends, all Dialogic boolean variables are written back to `GameManager.Flags`.
- **Safety net** — if `GameState` is stuck on `Dialog` but Dialogic is not running, the state resets to `Overworld` after 5 seconds.
- **Timeline preloading** — `OverworldBase` calls `ResourceLoader.LoadThreadedRequest()` on all NPC timelines when a map loads, so the first conversation has no stutter.
- **DialogRegistry** — an optional short-name registry (`DialogRegistry.Instance.StartTimeline("intro")`) to avoid scattering full `res://` paths across game code.

### Save System

Saves to `user://save.json` using `System.Text.Json`. The saved data includes:

```
PlayerHp, PlayerMaxHp, Gold, Exp, Love, TotalKills, Route,
LastMapPath, LastSavePointId, Flags (dictionary), InventoryItemPaths (list)
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
│   └── DialogRegistry.cs   # Optional short-name → full-path timeline map
│
├── core/
│   ├── data/               # Resource subclasses ([GlobalClass] for inspector)
│   │   ├── CharacterStats.cs
│   │   ├── EnemyData.cs
│   │   ├── EncounterData.cs
│   │   └── ItemData.cs
│   ├── interfaces/
│   │   ├── IInteractable.cs
│   │   └── IDamageable.cs
│   └── extensions/
│       └── NodeExtensions.cs
│
├── scenes/
│   ├── boot/               # First scene (Bootstrap)
│   ├── menus/              # MainMenu, PauseMenu
│   ├── hud/                # GameHud (overworld HP bar, CanvasLayer 2)
│   ├── player/             # Player.tscn + Player.cs
│   ├── overworld/
│   │   ├── OverworldBase.tscn / .cs   # Base class for all maps
│   │   ├── maps/                      # Individual map scenes (inherit OverworldBase)
│   │   └── objects/                   # Npc, SavePoint, EncounterTrigger,
│   │                                  # MapExit, SpawnPoint, CutsceneTrigger,
│   │                                  # EnemyPawn, SaveConfirmDialog
│   └── battle/
│       ├── BattleScene.tscn / .cs     # Root state machine
│       ├── BattleHUD.cs               # CanvasLayer 10
│       ├── ActionMenu, SubMenu, FightBar
│       ├── ui/                        # DamageNumber, EnemyNameplate
│       ├── dodge/                     # DodgeBox, Soul, BulletBase variants
│       └── patterns/                  # Pattern001–004, PatternRandom
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
└── assets/
    ├── sprites/
    ├── fonts/
    ├── audio/
    └── shaders/
```

---

## Autoloads Reference

All autoloads are accessible via their static `Instance` property.

### `GameManager`

Central state store. Everything that must survive a scene transition lives here.

```csharp
GameManager.Instance.CurrentState        // GameState enum
GameManager.Instance.CurrentRoute        // RouteType enum
GameManager.Instance.Love                // LV (1–4)
GameManager.Instance.PlayerStats         // CharacterStats (live copy)
GameManager.Instance.Gold
GameManager.Instance.Exp
GameManager.Instance.TotalKills
GameManager.Instance.Flags               // Dictionary<string, bool>
GameManager.Instance.InventoryItemPaths  // List<string> of .tres paths
GameManager.Instance.LastMapPath
GameManager.Instance.LastSpawnId

GameManager.Instance.SetState(GameState.Battle);
GameManager.Instance.SetFlag("talked_to_npc_01", true);
GameManager.Instance.GetFlag("talked_to_npc_01");
GameManager.Instance.RegisterKill();
GameManager.Instance.RegisterSpare();
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

---

## Core Data Types

### `EnemyData` (Resource)

Create as `.tres` files in `resources/enemies/`. All fields are inspector-editable.

| Field | Type | Description |
|---|---|---|
| `EnemyId` | string | Unique ID, used to find Act timelines |
| `DisplayName` | string | Shown in battle HUD and dialog |
| `FlavorText` | string | Shown by "Check" act |
| `Stats` | CharacterStats | HP, ATK, DEF, Speed, InvincibilityDuration |
| `BattleSprite` | Texture2D | Enemy graphic (placeholder polygon if null) |
| `ActOptions` | string[] | Names shown in Act sub-menu |
| `ActMercyValues` | int[] | Mercy % added per act (parallel to ActOptions) |
| `ActResultTexts` | string[] | Fallback text if no Dialogic timeline exists |
| `BattleDialogLines` | string[] | Random lines shown before each attack |
| `BattleTimelinePath` | string | Optional Dialogic timeline for the enemy's turn |
| `CanBeSpared` | bool | Whether Spare works when mercy ≥ 100% |
| `GoldDrop` / `ExpDrop` | int | Rewards on kill |
| `AttackPatternScene` | PackedScene | Pattern node to spawn in DodgeBox |

### `EncounterData` (Resource)

```
Enemies            Array<EnemyData>   — currently battles use index 0
BackgroundId       string             — for future background art switching
EncounterChancePerStep  float         — 0–100, checked every 32 px of movement
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
2. Create a pattern scene (see below) and assign it to `AttackPatternScene`.
3. Create `resources/encounters/{id}.tres` — add your enemy to `Enemies`.
4. Optionally create Act dialog timelines at `dialog/timelines/act_{enemyId}_{optionName}.dtl`.
5. Add your encounter to a map's `RandomEncounterTable`, or place an `EnemyPawn` / `EncounterTrigger`.

### New Attack Pattern

Patterns are `Node` (or `Node2D` if containing `Node2D` children) scenes with a `Timer` child.

```csharp
public partial class MyPattern : Node
{
    [Export] public PackedScene? BulletScene { get; set; }

    public override void _Ready()
    {
        GetNode<Timer>("SpawnTimer").Timeout += OnSpawn;
    }

    private void OnSpawn()
    {
        var bullet = BulletScene.Instantiate<DirectedBullet>();
        bullet.Position = new Vector2(0, -50);
        bullet.SetDirection(Vector2.Down);
        bullet.Speed = 100f;
        GetParent().AddChild(bullet); // adds to DodgeBox's BulletContainer
    }
}
```

> **Important:** If your pattern instantiates other nodes that are `Node2D` (e.g. sub-patterns or `PatternRandom`), those intermediate container nodes must also extend `Node2D`, not plain `Node`, so that bullet positions inherit the correct canvas transform.

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
3. Optionally fill `AltTimelinePaths` / `AltTimelineFlags` for alternate dialog after events.
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

### Async scene transitions
`SceneTransition.GoToAsync()` and `ToBattleAsync()` are `async Task` methods. Always `await` them; fire-and-forget is used only for non-blocking background tasks.

### Passing data across scenes
Godot destroys the current scene tree on every scene change. Cross-scene data lives in autoloads:
- **Battle encounter** — `BattleRegistry.SetPendingEncounter()` before transitioning; `GetPendingEncounter()` consumes it once in `BattleScene._Ready()`.
- **Spawn position** — `GameManager.LastSpawnId` is set by `MapExit` and consumed once by `OverworldBase.GetSpawnPosition()`.
- **Persistent state** — everything else lives in `GameManager` (flags, stats, gold, route).

### Resource mutability
`CharacterStats` is a `Resource` — it is shared by default. Always call `.Duplicate()` before modifying if you need a local copy. `GameManager` holds the live player stats copy.

### Bullet culling
`DodgeBox._Process` calls `CullBulletsRecursive()` every frame. It walks the entire subtree of `BulletContainer` using `GlobalPosition` → `BulletContainer.ToLocal()`, so intermediate wrapper nodes (like `PatternRandom`) at non-zero offsets are handled correctly.

### CanvasLayer draw order

| Layer | Node |
|---|---|
| 2 | GameHud (overworld HP) |
| 10 | BattleHUD |
| 50 | PauseMenu |
| 60 | SaveConfirmDialog |
| 100 | SceneTransition (fade overlay — always on top) |

---

## Enemies Reference

| ID | Name | HP | ATK | DEF | Spare Condition | Pattern |
|---|---|---|---|---|---|---|
| `enemy_001` | Wisplet | 20 | 5 | 0 | Greet **or** Hum (50% each) | PatternRandom (all four) |
| `enemy_002` | Thornling | 35 | 8 | 2 | Water (40%) + Apologize (60%) | Pattern002 — horizontal sweep |
| `enemy_003` | Gloomfish | 60 | 15 | 3 | Pet + Sing + Offer Light (any ≥ 100%) | Pattern003 — radial burst |
| `enemy_004` | Dustmote | 12 | 4 | 0 | Hug (instant 100%) | Pattern004 — bouncing chaos |

---

## File Naming Conventions

| Type | Convention | Example |
|---|---|---|
| Scenes | PascalCase.tscn | `BattleScene.tscn` |
| C# scripts | PascalCase.cs (matches scene) | `BattleScene.cs` |
| Resources | snake_case.tres | `enemy_001.tres` |
| Dialog timelines | snake_case.dtl | `npc_toriel_intro.dtl` |
| Dialogic characters | PascalCase.dch | `Toriel.dch` |
| Folders | snake_case/ | `scenes/battle/` |
| Act timelines | `act_{enemyId}_{optionName}.dtl` | `act_enemy_001_greet.dtl` |
| Battle turn timelines | `battle_{enemyId}_turn.dtl` | `battle_enemy_003_turn.dtl` |

---

## Common Pitfalls

- **`GetNode<T>()` in constructors** — only call in `_Ready()` or later.
- **Dialogic `timeline_ended`** — this is a GDScript signal. Always connect via `DialogicBridge.ConnectTimelineEnded()`, never directly.
- **`BulletBase` references** — bullets call `QueueFree()` on hit and the `DodgeBox` culls them on exit. Never store bullet references.
- **`CharacterStats` sharing** — it is a `Resource`; call `.Duplicate()` before mutating if you need a local copy.
- **Pattern intermediate nodes** — any `Node` that sits between `BulletContainer` and actual bullet nodes must extend `Node2D`, not `Node`, or bullets will render at global (0, 0).
- **`SceneTransition.GoToAsync` is async** — always `await` it or the code after it runs before the transition finishes.
- **`.tres` C# resource types** — the `[gd_resource]` header must use `type="Resource" script_class="ClassName"` (not `type="ClassName"`), because Godot's C++ parser cannot instantiate C# types by name.
