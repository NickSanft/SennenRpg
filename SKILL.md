# SKILL.md — SennenRpg

Operational recipes for common development tasks. Each skill is a step-by-step checklist.
Reference these when creating new game content to ensure consistency.

---

## Skill: create-enemy
**Creates a new enemy with battle sprite, stats, attack pattern, and Perform dialog.**

### Steps
1. Create `EnemyData` resource:
   - File: `res://resources/enemies/{id}.tres` (type: `EnemyData`)
   - Set: `EnemyId` (unique string, snake_case), `DisplayName`, `Stats` (nested `CharacterStats` resource)
   - Set: `BattleSprite` (Texture2D), `BardicActOptions` (string array e.g. `["Check", "Serenade"]`)
   - Set: `FlavorText`, `GoldDrop`, `ExpDrop`
2. Create enemy scene:
   - New Inherited Scene from `res://scenes/battle/enemies/EnemyBase.tscn`
   - Save to `res://scenes/battle/enemies/specific/{Name}Enemy.tscn`
   - Attach `{Name}Enemy.cs` extending `EnemyBase` if custom behavior is needed
   - Set `Data` export on root node to the `.tres` resource
3. Create attack pattern (rhythm arena):
   - New scene: `res://scenes/battle/rhythm/patterns/{Name}Pattern.tscn`
   - Root `Node2D` with `{Name}Pattern.cs` extending `RhythmPatternBase`
   - Implement the pattern using the lane obstacle system
4. Link pattern to enemy:
   - Set `EnemyData.AttackPatternScene` to the rhythm pattern `.tscn` PackedScene
5. Create Perform dialog timelines (one per Bard skill option):
   - `res://dialog/timelines/act_{id}_check.dtl` — always create a Check timeline
   - `res://dialog/timelines/act_{id}_{option}.dtl` for each other skill option
6. Register encounter:
   - Create `res://resources/encounters/{id}_encounter.tres` (type: `EncounterData`)
   - Set `Enemies` array to include the new `EnemyData`, set `BackgroundId`

---

## Skill: create-map
**Creates a new overworld map scene.**

### Steps
1. In Godot editor: **Scene > New Inherited Scene** → select `res://scenes/overworld/OverworldBase.tscn`
2. Save to `res://scenes/overworld/maps/{MapName}.tscn`
3. Create script `res://scenes/overworld/maps/{MapName}.cs` extending `OverworldBase`
4. In the scene, select root node → set `MapId` export to a unique string (e.g., `"ruins_entrance"`)
5. Set `BgmPath` export to the BGM file for this map (e.g., `"res://assets/audio/bgm/ruins.ogg"`)
6. Paint tiles in the inherited `TileMapLayer (Ground)` and `TileMapLayer (Walls)` layers
7. Place objects in the **YSort** node (for correct draw order):
   - Instance `Npc.tscn` for NPCs, set `TimelinePath` export
   - Instance `EncounterTrigger.tscn`, set `EncounterData` export
   - Instance `SavePoint.tscn`, set `SavePointId` and `TimelinePath` exports
8. Add exit areas: `Area2D` nodes at map edges with `{MapName}Exit.cs` that calls `SceneTransition.GoToAsync(targetScene)`

---

## Skill: create-dialog
**Creates a new Dialogic 2 dialog timeline.**

### Steps
1. Open **Dialogic editor** from the Godot editor top bar (appears after plugin is enabled)
2. **Create character** (if new person speaking):
   - Dialogic editor → Characters tab → click **+**
   - Set name, display name, color
   - Add portrait textures (sprites from `res://assets/sprites/`)
   - Save to `res://dialog/characters/{Name}.dch`
3. **Create timeline**:
   - Dialogic editor → Timelines tab → click **+**
   - Save to `res://dialog/timelines/{descriptive_name}.dtl`
   - Use snake_case. Prefix convention:
	 - `npc_{name}_{context}.dtl` — NPC conversations
	 - `act_{enemy_id}_{option}.dtl` — Battle Act results
	 - `save_{location}.dtl` — Save point flavor text
	 - `cutscene_{name}.dtl` — Story cutscenes
4. **Build dialog** using Dialogic events:
   - **Character + Text** events for dialog lines
   - **Choice** event for player choices
   - **Set Variable** event to communicate results back to C# (e.g., `choice_result = "a"`)
   - **Condition** event to branch on variables set from C#
5. **Call from C#**:
   ```csharp
   // Set variables the timeline can read
   DialogicBridge.Instance.SetVariable("player_name", GameManager.Instance.PlayerName);
   // Connect end callback
   DialogicBridge.Instance.ConnectTimelineEnded(new Callable(this, MethodName.OnDialogDone));
   // Start
   DialogicBridge.Instance.StartTimeline("res://dialog/timelines/name.dtl");
   ```
6. **Read results back** in callback:
   ```csharp
   private void OnDialogDone()
   {
	   var result = DialogicBridge.Instance.GetVariable("choice_result").AsString();
	   // React to result
	   GameManager.Instance.SetState(GameState.Overworld);
   }
   ```

---

## Skill: add-item
**Adds a new usable item.**

### Steps
1. Create `res://resources/items/{id}.tres` (type: `ItemData`):
   - Set `ItemId` (snake_case), `DisplayName`, `Description`, `Icon` (Texture2D)
   - Set `HealAmount` for healing items, or `EffectType` + `EffectValue` for other effects
2. Add sprite to `res://assets/sprites/ui/items/`
3. To give the player this item at game start, add to `GameManager._Ready()`:
   ```csharp
   Inventory.Add(GD.Load<ItemData>("res://resources/items/{id}.tres"));
   ```
4. Item appears automatically in battle **Item** submenu — no additional wiring needed

---

## Skill: add-flag
**Adds a story progress flag to track game state.**

### Steps
1. Flags are untyped booleans. No registration or enum needed.
2. **Set a flag** (e.g., after a story moment):
   ```csharp
   GameManager.Instance.SetFlag("met_toriel", true);
   ```
3. **Check a flag** (e.g., in NPC logic):
   ```csharp
   if (GameManager.Instance.GetFlag("met_toriel")) { ... }
   ```
4. **Use in Dialogic dialog** — pass as variable before starting timeline:
   ```csharp
   DialogicBridge.Instance.SetVariable("met_toriel", GameManager.Instance.GetFlag("met_toriel"));
   ```
   Then use a **Condition** event in the `.dtl` timeline to branch.
5. Flags are automatically persisted to `user://save.json` via `SaveManager`.

---

## Skill: wire-battle-transition
**Connects a map trigger to launch a random battle encounter.**

### Steps
1. In the map scene, instance `res://scenes/overworld/objects/EncounterTrigger.tscn` into the YSort node
2. Select the trigger node → set `EncounterData` export to the desired `.tres` encounter resource
3. (Optional) Set `EncounterChance` (0.0–1.0) if random encounter; leave default for forced encounter
4. The trigger script automatically calls `SceneTransition.ToBattleAsync(encounterData)` on `body_entered`
5. `BattleRegistry` stores the pending encounter; `BattleScene._Ready()` retrieves it via `BattleRegistry.GetPendingEncounter()`
6. After battle ends, `BattleScene` calls `SceneTransition.GoToAsync(GameManager.Instance.LastMapPath)` to return

---

## Skill: place-save-point
**Places a save point in a map.**

### Steps
1. Instance `res://scenes/overworld/objects/SavePoint.tscn` into the map's YSort node
2. Select the save point node:
   - Set `SavePointId` export to a unique string (e.g., `"ruins_entrance_01"`)
   - Set `TimelinePath` export to the save point flavor dialog (e.g., `"res://dialog/timelines/save_ruins_entrance.dtl"`)
3. Create the flavor dialog timeline (see **create-dialog** skill) — this is the flavor text shown at the save point
4. No other wiring needed — `SavePoint.cs` calls `SaveManager.SaveGame()` and heals player automatically

---

## Skill: add-npc
**Places a talking NPC in a map.**

### Steps
1. Instance `res://scenes/overworld/objects/Npc.tscn` into the map's YSort node
2. Select the NPC node:
   - Set `TimelinePath` export to the NPC's dialog timeline
   - Set `SpriteFrames` export to the NPC's `SpriteFrames` resource
   - Set `DisplayName` export (shown in Dialogic dialog box)
3. Create or select a dialog timeline for this NPC (see **create-dialog** skill)
4. The NPC automatically implements `IInteractable` — player interaction triggers dialog
5. For conditional dialog (different lines after story events), use Dialogic **Condition** events
   and pass game flags via `DialogicBridge.SetVariable()` in `Npc.cs`

---

## Key Paths Quick Reference
| What | Path |
|---|---|
| Enemy resources | `res://resources/enemies/*.tres` |
| Encounter resources | `res://resources/encounters/*.tres` |
| Item resources | `res://resources/items/*.tres` |
| Map scenes | `res://scenes/overworld/maps/*.tscn` |
| Enemy battle scenes | `res://scenes/battle/enemies/specific/*.tscn` |
| Rhythm patterns | `res://scenes/battle/rhythm/patterns/*.tscn` |
| Dialog timelines | `res://dialog/timelines/*.dtl` |
| Dialog characters | `res://dialog/characters/*.dch` |
| BGM audio | `res://assets/audio/bgm/*.ogg` |
| SFX audio | `res://assets/audio/sfx/*.ogg` |
| Sprites | `res://assets/sprites/{player,enemies,overworld,ui}/` |
