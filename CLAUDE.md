# CLAUDE.md — SennenRpg

## Project Overview
Undertale-style 2D RPG in Godot 4.6 with C# (.NET 10). GL Compatibility renderer on D3D12 (Windows).
Project name: **SennenRpg**. Assembly: `SennenRpg`.

## Tech Stack
| Layer | Technology |
|---|---|
| Engine | Godot 4.6 |
| Language | C# (.NET 10) — all gameplay code |
| Dialog | Dialogic 2 (GDScript plugin) via `DialogicBridge.cs` autoload |
| Camera | PhantomCamera 2D plugin |
| Serialization | `System.Text.Json` (no Newtonsoft) |
| Physics | Jolt Physics (3D only; 2D uses default) |

## Critical Rules
- **Never write GDScript for gameplay logic.** GDScript exists only in plugin files under `addons/`.
- All C# nodes must be `public partial class Name : BaseClass`.
- Signals use delegate pattern: `[Signal] public delegate void MySignalEventHandler()`.
- Null safety is enabled — always use `?` and null-check. Godot nodes can be null before `_Ready()`.
- No static mutable state outside autoloads.
- Never use `GetNode<T>()` in constructors — only in `_Ready()` or later.
- `CharacterStats` is a `Resource` — call `.Duplicate()` before modifying if you need a local copy.
- `SceneTransition.GoToAsync` is async — always `await` it.

## File Naming Conventions
| Type | Convention | Example |
|---|---|---|
| Scenes | PascalCase.tscn | `BattleScene.tscn` |
| C# scripts | PascalCase.cs (matches scene) | `BattleScene.cs` |
| Resources | snake_case.tres | `example_enemy.tres` |
| Dialog timelines | snake_case.dtl | `npc_toriel_intro.dtl` |
| Dialogic characters | PascalCase.dch | `Toriel.dch` |
| Folders | snake_case/ | `scenes/battle/` |

## Project Structure
```
res://
├── addons/
│   ├── dialogic/                 # Dialogic 2 — dialog management
│   └── phantom_camera/           # PhantomCamera 2D — smooth camera follow
├── autoloads/
│   ├── GameManager.cs            # Facade: delegates to domain data classes below
│   ├── SaveManager.cs            # Save/load JSON to user://save.json
│   ├── AudioManager.cs           # BGM crossfade, SFX pooling, now-playing popup, BGM ducking
│   ├── SceneTransition.cs        # Scene switching with transition animations
│   ├── DialogicBridge.cs         # C# wrapper for Dialogic 2 GDScript API
│   ├── BattleRegistry.cs         # Loads all EnemyData resources; lookup by ID
│   ├── DialogRegistry.cs         # Optional short-name → full-path timeline map
│   ├── RhythmClock.cs            # Master beat clock for rhythm minigames
│   ├── QuestManager.cs           # Quest state machine, condition tracking
│   ├── SettingsManager.cs        # User preferences, difficulty, volume
│   └── AccessibilityOverlay.cs   # Accessibility features
├── core/
│   ├── data/                     # Pure-logic + Resource subclasses + domain data
│   │   ├── CharacterStats.cs, EnemyData.cs, EncounterData.cs, ItemData.cs
│   │   ├── ItemType.cs               # Enum: Consumable, Ingredient, Equipment, KeyItem, Repel
│   │   ├── ShopItemEntry.cs, NpcResidencyEntry.cs, QuestData.cs
│   │   ├── RhythmConstants.cs, PerformanceScore.cs
│   │   ├── Flags.cs              # Flag name constants + helpers
│   │   ├── ItemLogic.cs, ShopLogic.cs, NpcLogic.cs   # pure/testable logic
│   │   ├── CookingLogic.cs, CookingQuality.cs, RecipeIngredient.cs, RecipeData.cs  # cooking system
│   │   ├── TownRewardLogic.cs, LilyForgeLogic.cs     # Mellyr Outpost pure logic
│   │   ├── MultiClassLogic.cs, MultiClassData.cs, ClassProgressionEntry.cs  # multi-class system
│   │   ├── CrossClassBonus.cs, CrossClassBonusRegistry.cs  # cross-class passives
│   │   ├── MusicTrackInfo.cs, MusicMetadata.cs        # music track metadata registry
│   │   ├── TileMapDataParser.cs       # Parse/encode Godot TileMapLayer binary data
│   │   ├── UiSfx.cs                  # UI sound effect path constants
│   │   ├── DialogicSignalParser.cs
│   │   ├── JournalData.cs
│   │   ├── PlayerProgressionData.cs   # Gold, exp, level (owned by GameManager)
│   │   ├── PlayerCombatData.cs        # HP, MP, stats, growth (owned by GameManager)
│   │   ├── InventoryData.cs           # Items, spells, equipment (owned by GameManager)
│   │   ├── WorldStateData.cs          # Map state, spawn points (owned by GameManager)
│   │   └── MellyrRewardData.cs        # Rain gold, Lily recipes (owned by GameManager)
│   ├── interfaces/               # IInteractable
│   └── extensions/               # NodeExtensions.cs, CameraShake.cs
├── scenes/
│   ├── boot/                     # Boot.tscn — first scene loaded
│   ├── menus/                    # MainMenu, PauseMenu, GameOver, InventoryMenu, ShopMenu, EquipmentMenu, ResidencyShopMenu, ClassChangeMenu, CookingMenu, CookingMinigame, SpellsMenu, CreditsMenu, StatsMenu, SettingsMenu
│   ├── overworld/
│   │   ├── OverworldBase.tscn    # Inherited by all maps
│   │   ├── MAPP.tscn / .cs / .Events.cs   # Mapp Tavern (partial class split)
│   │   ├── maps/                 # Individual map scenes
│   │   │   ├── mellyr/           # MellyrOutpost.tscn/.cs — resident hiring town
│   │   │   └── dungeon/          # DungeonFloor1-3 — dungeon floors with wall collision
│   │   └── objects/
│   │       ├── Npc.cs            # NPC base (patrol, dialog, emote, [Tool])
│   │       ├── VendorNpc.cs      # Extends Npc — opens ShopMenu ([Tool])
│       ├── RorkTownNpc.cs   # Extends Npc — opens ResidencyShopMenu ([Tool])
│       ├── QuestGiver.cs    # Child node for NPCs offering quests
│   │       ├── InteractSign.cs   # Readable sign — opens SignReaderPopup ([Tool])
│   │       ├── Chest.cs          # One-time treasure chest ([Tool])
│   │       ├── JournalProp.cs    # Opens journal entry list ([Tool])
│   │       ├── BarkeepNpc.cs     # Extends VendorNpc — shop, rest, class change ([Tool])
│   │       ├── InteractPromptBubble.cs
│   │       ├── SignReaderPopup.cs   # CanvasLayer 55
│   │       ├── JournalEntryPopup.cs # CanvasLayer 56
│   │       ├── NpcInteractMenu.cs, JournalMenuPopup.cs
│   │       └── furniture/        # TableFurniture, ChairFurniture, BarStoolFurniture ([Tool])
│   ├── player/                   # Player.tscn + Player.cs, DungeonPlayer.cs (both emit Moved signal)
│   ├── battle/
│   │   ├── BattleScene.tscn      # Root battle scene
│   │   ├── BattleAttackResolver.cs  # Static: minigame results → damage (extracted helper)
│   │   ├── BattleStatusEffects.cs   # Status effect state & Dialogic signal handling
│   │   ├── ui/                   # BattleHud, ActionMenu, EnemyNameplate, DamageNumber
│   │   ├── dodge/                # DodgeBox, Soul, BulletBase + bullet variants
│   │   ├── patterns/             # Pattern001–006, PatternRandom
│   │   └── rhythm/               # CharmMinigame, BardMinigameBase, skills, lane patterns
│   └── hud/                      # GameHud, MinimapHud, AreaNameLabel, NowPlayingPopup, DialogHistoryOverlay
├── resources/
│   ├── enemies/                  # EnemyData .tres files
│   ├── items/                    # ItemData .tres files (consumables, ingredients, cooked food)
│   ├── encounters/               # EncounterData .tres files
│   ├── recipes/                  # RecipeData .tres files (cooking recipes)
│   ├── characters/               # CharacterStats .tres + per-class growth rates
│   └── tilesets/                 # TileSet .tres + tileset images
├── dialog/
│   ├── timelines/                # Dialogic .dtl timeline files
│   └── characters/               # Dialogic .dch character definitions
├── assets/
│   ├── sprites/                  # player/, enemies/, overworld/, ui/
│   ├── fonts/                    # determination.ttf (Undertale-style)
│   ├── audio/                    # bgm/, sfx/
│   └── shaders/                  # .gdshader files
├── SennenRpg.Tests/              # NUnit — pure logic, no Godot runtime needed
└── tests/gdunit/                 # GdUnit4 — autoload integration tests
```

## Autoloads (registered in project.godot)
Access via `GetNode<T>("/root/AutoloadName")` or via static `Instance` property.

| Autoload Name | Script | Purpose |
|---|---|---|
| `GameManager` | `autoloads/GameManager.cs` | Facade: flags, kills, delegates to domain data classes |
| `SaveManager` | `autoloads/SaveManager.cs` | File I/O for save data |
| `AudioManager` | `autoloads/AudioManager.cs` | BGM crossfade, SFX pooling |
| `SceneTransition` | `autoloads/SceneTransition.cs` | Async scene swapping with animations |
| `DialogicBridge` | `autoloads/DialogicBridge.cs` | Dialogic 2 interop only |
| `BattleRegistry` | `autoloads/BattleRegistry.cs` | Enemy/encounter data lookup by ID |
| `DialogRegistry` | `autoloads/DialogRegistry.cs` | Optional short-name → full-path timeline map |
| `RhythmClock` | `autoloads/RhythmClock.cs` | Master beat clock for rhythm minigames |
| `QuestManager` | `autoloads/QuestManager.cs` | Quest state machine, condition tracking |
| `SettingsManager` | `autoloads/SettingsManager.cs` | User preferences, difficulty, volume |

## CanvasLayer Draw Order
| Layer | Node |
|---|---|
| 2 | GameHud (overworld HP, MP, gold) |
| 3 | AreaNameLabel, NowPlayingPopup |
| 4 | MinimapHud |
| 10 | BattleHUD |
| 50 | PauseMenu |
| 51 | InventoryMenu, ShopMenu, EquipmentMenu, CookingMenu, ResidencyShopMenu |
| 52 | StatsMenu, ClassChangeMenu, NpcInteractMenu |
| 55 | SignReaderPopup |
| 56 | JournalEntryPopup |
| 60 | SaveConfirmDialog |
| 70 | LevelUpScreen |
| 100 | SceneTransition (fade overlay — always on top) |

## Battle System Flow
```
PlayerTurn
  → Fight selected   → RhythmStrike minigame → damage calc → EnemyTurn
  → Perform selected → show Bard skills sub-menu → skill/charm minigame → EnemyTurn
  → Item selected    → apply effect → EnemyTurn
  → Flee selected    → 50% escape chance → fled result or EnemyTurn

EnemyTurn:
  Enemy dialog fires (Dialogic timeline or BattleDialogLines) →
  RhythmArena activates (4-lane note highway) →
  PhaseEnded signal → back to PlayerTurn
  (or Defeat if player HP ≤ 0)

Victory → EXP/Gold display → GameManager.AddGold/AddExp → SceneTransition back
```

## Multi-Class System
- Four classes: Bard, Fighter, Ranger, Mage (each with independent level, exp, base stats)
- `GameManager.SwitchClass(PlayerClass)` snapshots current class, loads target class stats + growth rates
- Per-class growth rates in `resources/characters/growth_rates_{class}.tres`
- Cross-class bonuses (`CrossClassBonusRegistry`) grant stat boosts or spell unlocks at level thresholds
- Class change available via Rork's NPC menu in MAPP Tavern
- StatsMenu shows all class levels + earned cross-class bonuses
- Save slot card shows current class name
- Level-up screen shows class name + cross-class bonus unlocks

## Cooking System
- Recipes combine ingredients into food items via a rhythm minigame
- `CookingLogic` (pure static): HasIngredients, DetermineQuality (Burnt/Normal/Perfect), QualityItemPath
- Quality tiers: Burnt (0.5x heal), Normal (1.0x), Perfect (1.5x) — separate .tres per quality variant
- `CookingMinigame` — single-lane rhythm game with `cooking.wav` BGM, configurable note count
- `CookingMenu` — pause menu sub-menu listing recipes with ingredient availability
- `ItemType` enum on `ItemData`: Consumable, Ingredient, Equipment, KeyItem, Repel
- Ingredients sold by Rork and dropped by enemies via `BonusLootItemPath`
- Battle Item menu filters to only show Consumable and Repel items (hides ingredients/key items)

## Spells System
- `SpellData` Resource: SpellId, DisplayName, Description, BasePower, MpCost, MinigameScene
- `OverworldUsable` flag: spells castable from pause menu (e.g., Teleport Home)
- `OverworldTargetScene`: scene path for teleport-type spells
- `SpellsMenu` — pause menu sub-menu listing known spells with MP costs
- `GameManager.AddSpell(path)` / `InventoryData.AddSpell(path)` — spell acquisition API
- Default spell: Shadow Bolt (battle damage); Teleport Home (returns to MAPP Tavern for 5 MP)

## Auto-Save
- `MapExit` has `[Export] bool AutoSave` — writes save before scene transition
- Enabled on all dungeon floor staircase transitions

## SNES Theme (Chrono Trigger Style)
- `UiTheme.cs` — shared constants: `Gold`, `PanelBg`, `PanelBorder`, `SubtleGrey`, etc.
- `UiTheme.ApplyPanelTheme(panel)` — applies blue gradient StyleBoxFlat with rounded borders
- `UiTheme.ApplyButtonTheme(btn)` — applies button normal/hover/focus/pressed styles
- Font: `PressStart2P-Regular.ttf` pixel font in `assets/fonts/`
- All code-built menus use UiTheme instead of hardcoded colors

## Scene Transitions
- `TransitionType.Fade` — classic black fade (menus)
- `TransitionType.BattleFlash` — fast white flash (entering battle)
- `TransitionType.PixelMosaic` — screen pixelates into blocks, un-pixelates at destination (map transitions)
- Pixel mosaic shader: `assets/shaders/pixel_mosaic.gdshader`

## Teleport Dissolve Effect
- `assets/shaders/dissolve_vertical.gdshader` — per-sprite vertical dissolve (bottom-to-top)
- Teleport Home spell: player dissolves out → pixel mosaic transition → reforms at MAPP
- `GameManager.TeleportArriving` flag triggers reform animation in `Player._Ready()`

## Cutscene Framework
- `CutsceneStep.cs` — data class with factory methods: `ShowLetterbox`, `PanCamera`, `WalkNpc`, `Dialog`, `NameCard`, `Flag`, etc.
- `CutscenePlayer.cs` — Node that executes a list of steps sequentially with letterbox bars, camera pans, NPC movement, Dialogic dialog, name cards
- First usage: Rork intro cutscene at Mellyr Outpost (flag: `rork_mellyr_intro`)

## Dynamic Battle Backgrounds
- `BattleRegistry.PendingBackgroundColor` — sampled from ground tile at encounter position
- `BattleScene.SetupBattleBackground()` — creates two-tone gradient (lighter top, darker bottom)
- Fallback: dark purple gradient for scripted encounters

## Battle Visual Effects
- Enemy intro zoom: 0.5x → 1.0x scale with Back easing bounce
- Critical hit slow-motion: `Engine.TimeScale` drops to 0.3x for 0.15s on crits
- Victory: enemy shrinks to 0 + fades, victory fanfare SFX
- Screen shake + hit flash on all damage

## Low HP Warning
- HP below 25% → HP bar pulses between normal color and red (0.8s loop)
- Stops when healed above 25%

## Music Metadata
- `MusicMetadata.Lookup(path)` → `MusicTrackInfo` (Artist, Album, TrackNumber, Title)
- `NowPlayingPopup` — top-left fade-in/out popup spawned by AudioManager on BGM change
- Music files renamed to clean titles (e.g., `Carillion Forest.wav`)
- BGM continuity: same track continues seamlessly across scene transitions (dungeon floors)

## CI/CD
- `.github/workflows/test.yml` — NUnit tests on every push/PR to master
- `.github/workflows/release.yml` — on `v*` tags: tests → Godot export → GitHub Release with zip
- Tag with `git tag v0.1.0 && git push origin v0.1.0` to create a release

## Adding a New Enemy — Quick Reference
1. `res://resources/enemies/{id}.tres` — create `EnemyData` resource, set all fields
2. `res://scenes/battle/enemies/specific/{Name}Enemy.tscn` — inherit from `EnemyBase.tscn`
3. `res://scenes/battle/rhythm/patterns/{Name}Pattern.tscn` — inherit from `RhythmPatternBase`
4. Set `EnemyData.AttackPatternScene` to the pattern `.tscn`
5. Create Perform dialog timelines: `res://dialog/timelines/act_{id}_*.dtl`

## Adding a New Map — Quick Reference
1. New Inherited Scene from `OverworldBase.tscn` → save to `res://scenes/overworld/maps/{MapName}.tscn`
2. Create `{MapName}.cs` extending `OverworldBase`, set `MapId` export
3. Paint tiles, place NPCs/triggers/save points in YSort node

## Adding Dialog — Quick Reference
1. Open Dialogic editor (Godot top bar after plugin enabled)
2. Create character if needed → `res://dialog/characters/{Name}.dch`
3. Create timeline → `res://dialog/timelines/{descriptive_name}.dtl`
4. Set C# variables before starting: `DialogicBridge.Instance.SetVariable("key", value)`
5. Start: `DialogicBridge.Instance.StartTimelineWithFlags("res://dialog/timelines/name.dtl")`
6. React when done: `DialogicBridge.Instance.ConnectTimelineEnded(new Callable(this, MethodName.OnDone))`

## Story Flags
- **Flags**: `GameManager.Instance.SetFlag("key", true)` / `GetFlag("key")`
- Flags persist to save data automatically via `SaveManager`
- Use `Flags.*` constants in `core/data/Flags.cs` for well-known flag names

## Testing
- **NUnit** (pure logic, no Godot): `dotnet test SennenRpg.Tests/SennenRpg.Tests.csproj`
  - 500+ tests covering: `DialogicSignalParser`, `Flags`, `ItemLogic`, `ShopLogic`, `NpcLogic`, `JournalData`, `PerformanceScore`, `RhythmConstants`, `TownRewardLogic`, `LilyForgeLogic`, `MultiClassLogic`, `MultiClassData`, `CrossClassBonus`, `CookingLogic`, `MusicMetadata`, `TileMapDataParser`, `EquipmentLogic`, `StatusLogic`, `LootLogic`, `RhythmMemoryLogic`, `SaveSlotLogic`, `SettingsLogic`, `LevelData`, `EncounterLogic`, `DayNightLogic`, `QuestLogic`
  - Uses selective `<Compile Include="...">` — never reference the full game project from tests
  - Add new testable logic to `core/data/` as a standalone static class, then add it to the csproj
- **GdUnit4** (Godot runtime): run from editor or `godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd`
  - Covers: `GameManager`, `DialogicBridge`, `NpcDialog`, `RhythmClock`

## Common Pitfalls
- Dialogic's `timeline_ended` fires on GDScript side — always connect via `DialogicBridge.ConnectTimelineEnded()`.
- `BulletBase` auto-frees when outside DodgeBox bounds — do not cache bullet references.
- `EncounterData` is passed via `BattleRegistry.SetPendingEncounter()` before scene transition.
- `SceneTransition` is a `CanvasLayer` autoload — it renders on top of everything.
- Jolt Physics is enabled for 3D only — do not configure 3D physics bodies for 2D gameplay.
- **`[Tool]` is not inherited in Godot 4 C#** — add it explicitly on every class that needs editor `_Ready()` execution, including subclasses.
- **`Engine.IsEditorHint()` guards required in `[Tool]` scripts** — guard all autoload access, group registration, and physics processing.
- **`GetChildCount()` guard with pre-baked .tscn children** — objects like `InteractSign` and `Chest` have a `CollisionShape2D` child pre-baked in the `.tscn`. Use `if (GetChildCount() > 1) return;` not `> 0`.
- **`Resource[]` for Godot4 C# sub-resource exports** — use `Resource[]` (not `ShopItemEntry[]`) when exporting arrays of custom Resource subclasses. Cast with `OfType<T>()` at runtime. Using a concrete type causes a cast exception in `[Tool]` mode because the C++ serializer produces plain `Resource` objects.
- **`Resource` for single typed exports of external .tres** — use `[Export] public Resource? MyProp` (not `QuestData?`) when loading external `.tres` files into a typed export. Cast with `as QuestData` at runtime. Same root cause as the array case above.
- **`.tres` type attribute** — use `type="Resource"` + `script = ExtResource(...)` in `.tscn` files, never `type="ClassName"`. Godot's C++ ClassDB cannot instantiate C# types by name.
- **`GrabFocus()` before tree** — call after `AddChild()`, wrapped in `CallDeferred`, or it throws.
- **DialogicBridge safety net** — only fires when `_dialogicOwnsDialog == true` (set in `StartTimeline()`, cleared in `OnTimelineEndedInternal()`). Non-Dialogic UI that uses `GameState.Dialog` for player-blocking (signs, journal, shop) does not trigger it.
- **`InteractPromptBubble` parameterless constructor** — required for Godot's hot-reload/serialization. Pattern: `public InteractPromptBubble() : this("") { }`.

## Plugins

### Dialogic 2
- Install from AssetLib or GitHub: `coppolaemilio/dialogic`
- Place in `addons/dialogic/`. Enable in **Project > Project Settings > Plugins**.
- Never call Dialogic directly in C# — always go through `DialogicBridge.cs`.

### PhantomCamera 2D
- Install from AssetLib: search "PhantomCamera"
- Place in `addons/phantom_camera/`. Enable in Project Settings > Plugins.
- Add `PhantomCamera2D` node inside `Player.tscn` to drive the camera.
