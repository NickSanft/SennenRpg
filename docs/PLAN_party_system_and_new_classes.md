# PLAN — Party System, Rogue & Alchemist Classes, Multi-Actor Battles

This is a planning document. No code has been written yet. Resolved design decisions are listed first; the phased plan is at the bottom.

---

## Resolved design decisions

1. **Sen's class scope.** Sen can hot-swap into **all six** classes: Bard, Fighter, Ranger, Mage, Rogue, Alchemist. Lily and Rain remain locked to Alchemist and Rogue respectively.
2. **Battle turn order.** **Speed-sorted queue** (FFVI style). At the start of each round, all living actors (party + enemies) are sorted by `Speed` descending and act in that order. The queue is rebuilt every round so newly KO'd or revived actors are picked up immediately.
3. **Action selection cadence.** **Per-actor menu.** When an actor's turn comes up, the action menu pops up for that actor only and resolves before the next actor in the queue acts. No "command everyone, then play out the round" phase.
4. **KO / revive / game over.** Game over fires only when **all** active party members are KO'd. KO'd members stay at 0 HP until revived (item, spell, or post-battle restoration). KO'd members **still receive XP** at the end of a battle they survived to.
5. **Inventory & equipment ownership.** One shared bag (existing `InventoryData.InventoryItemPaths`). Equipment slots are per-party-member (each member has their own 8-slot dict). Effective stats are computed per-member.
6. **Cross-class bonuses scope.** **Per-character.** Each member's earned bonuses are computed from their own class levels. Lily and Rain are locked to a single class so they will rarely earn cross-class bonuses on their own — that's expected and consistent with their identity as specialists.
7. **Followers on which maps.** Followers appear **only on 16×16 sprite maps**, which currently means the **WorldMap** and the **Dungeon floors** (DungeonFloor1/2/3). They do NOT appear in towns or interiors that use the larger 32×32 player sprite (MAPP, Mellyr Outpost, etc.).
8. **Party size cap.** Up to **6 members**, all of whom can be active in battle. No separate reserve list at v1.
9. **Lily/Rain starting stats.** New `lily_stats.tres` and `rain_stats.tres` CharacterStats resources, both joining at **level 1** regardless of Sen's level.
10. **Recruitment moment.** Short Dialogic timeline fires when the player hires Lily or Rain (e.g., `lily_joins.dtl`, `rain_joins.dtl`).
11. **Mixed encounters.** Each enemy in a multi-enemy encounter has fully **independent stats and loot**. No group modifier.

---

## Adopted suggestions

All of the following are part of the plan. The "Ready to depart" cutscene was considered and dropped.

### Class flavor

- **Rogue minigame — "Pickpocket Combo":** Three rapid one-beat windows in succession. Hit all three Perfect → guaranteed crit + steal an item from the enemy's loot table (single-use per battle). Misses cancel the combo. Plays into Rogue's high speed.
- **Alchemist minigame — "Potion Brew":** A stop-the-meter bar that bounces back and forth. Stop it on the central sweet spot to brew a random *good* effect (heal/buff/poison enemy/turn-skip), narrow miss = neutral, wide miss = backfire (hurt yourself). Luck stat widens the sweet spot. Embraces Alchemist's RNG identity.
- Rogue cross-class unlocks: **+1 to forage roll quality** at Rogue Lv 5; **Backstab** spell (deals double if user has higher Speed than target) at Lv 10.
- Alchemist cross-class unlocks: **Gold drops +20%** at Alchemist Lv 5; cooking minigame Perfect-window widens at Lv 10 (ties into the existing cooking system).

### Party

- **Formation card.** Show a 6-slot formation in the Party Menu: front (takes melee aggro), back (safer, lower attack). Members can be reordered between front and back rows.
- **Party-wide crit chime.** When any party member crits, the next ally's window gets +5 frames. Tiny touch but feels great.
- **Out-of-battle banter.** Reuse the existing `forage_found.dtl` style — when Lily picks up a foraged item, occasionally trigger a one-line Dialogic snippet.

### Battle visuals

- Render party members as small portraits in the bottom HUD, with HP/MP bars under each. The current actor's portrait gets a yellow border + the existing slow-mo crit hook lights up when they crit.
- When it's a non-Sen turn, briefly flash a banner: **"Lily's Turn"** / **"Rain's Turn"**, similar to how Dialogic name cards work. (Sen's turn shows a banner too for consistency.)
- Enemies stand horizontally; the cursor for target selection is the existing arrow but moves between enemies.

### Multi-enemy encounters

- For the Wisplet+Centiphantom mixed encounter: layer them on the Y-axis so the larger Centiphantom is centered, Wisplet sits to its side. Reuse `EncounterData.Enemies` array (already supports multiples).

### Dropped

- ~~**"Ready to depart" check.**~~ Considered and dropped.

---

## Phased implementation plan

### Phase 0 — Add the new classes as data

*Smallest possible step. Nothing new in UI. Just adds Rogue/Alchemist to the existing hot-swap system so Sen can experiment with them.*

**New files:**

- `resources/characters/growth_rates_rogue.tres` — high Speed/Luck weights
- `resources/characters/growth_rates_alchemist.tres` — high Magic/Luck weights
- `resources/characters/class_rogue.tres` and `class_alchemist.tres` — base CharacterStats templates (mirroring existing `class_*.tres` if any, or `player_stats.tres`)

**Modified:**

- `core/data/PlayerClass.cs:1-9` — add `Rogue, Alchemist` to enum
- `core/data/CrossClassBonusRegistry.cs` — add 2–4 new bonuses anchored on Rogue/Alchemist source class
- `core/data/PlayerCombatData.cs:40` — `LoadGrowthRatesForClass` already pattern-matches on the enum name; verify it picks up the new files
- `scenes/menus/ClassChangeMenu.cs` — confirm the menu enumerates `Enum.GetValues<PlayerClass>()` (it should already loop over the enum)
- `core/data/MultiClassData.cs:Reset` — no change unless `Bard` is hardcoded as default; check
- `SennenRpg.Tests/Logic/MultiClassDataTest.cs` — add cases that exercise the new enum values

**Tests:** existing `MultiClassLogicTest.cs` should keep passing; add a minimal "switch to Rogue → growth rates load" test.

---

### Phase 1 — Rogue & Alchemist rhythm minigames

*Now Rogue/Alchemist actually feel different in battle.*

**Approach:** **Fight** triggers a class-specific minigame (Bard already uses `RhythmStrike`). Rogue and Alchemist each get their own variant of the Fight minigame. Perform stays Bard-exclusive (it's the bardic skill list).

**New files:**

- `scenes/battle/rhythm/RogueStrikeMinigame.tscn` + `.cs` — Pickpocket Combo described above
- `scenes/battle/rhythm/AlchemistBrewMinigame.tscn` + `.cs` — Potion Brew described above
- `core/data/PerformanceScore.cs` — already exists; verify it can model "3-of-3 hit" results without code changes

**Modified:**

- `scenes/battle/BattleScene.cs:144-176` — when launching the Fight phase, dispatch to the right minigame scene by `GameManager.ActiveClass` (or by the active actor's class, once Phase 7 lands)
- `scenes/battle/BattleAttackResolver.cs` — extend to consume the new minigame results (steal, potion effects)

**Tests:** unit-test the steal-roll and potion-effect-roll pure logic in `core/data/RogueStealLogic.cs` and `core/data/AlchemistBrewLogic.cs` (new pure-static helpers), with NUnit cases for sweet-spot widths under different Luck values.

---

### Phase 2 — PartyMember data model

*The keystone refactor. Nothing visible to the player yet, but unlocks everything else.*

**Concept:** Introduce `PartyMember` as a first-class entity. Sen becomes the first member. Lily/Rain will be added in Phase 3.

**New files:**

- `core/data/PartyMember.cs` — class with fields: `string MemberId, string DisplayName, PlayerClass Class, int Level, int Exp, int CurrentHp, int CurrentMp, CharacterStats BaseStats, Dictionary<EquipmentSlot,string> EquippedItemPaths, Dictionary<EquipmentSlot,string> EquippedDynamicItemIds, string OverworldSpritePath, string PortraitPath, bool CanChangeClass, FormationRow Row`. Sen has `CanChangeClass=true`; Lily/Rain `false`. `FormationRow` enum = `Front | Back`.
- `core/data/PartyData.cs` — list of up to **6** members + leader index. Methods: `Add(PartyMember)`, `Remove`, `SetLeader`, `Reorder`, `GetLeader`, `GetActive()`. All members are battle-active in v1 (no reserve list).
- `core/data/PartyMemberLogic.cs` — pure static: per-member level-up rolls, XP distribution across active members.
- `SennenRpg.Tests/Logic/PartyMemberLogicTest.cs` and `PartyDataTest.cs`

**Modified:**

- `autoloads/GameManager.cs` — own a `PartyData _party` field. Existing properties (`PlayerStats`, `EffectiveStats`, `PlayerLevel`, `AddExp`, etc.) become "active leader" facades by default, but Phase 6 will add explicit `GetMember(memberId)` accessors. Add `SelectedMemberId` for menus to track which member the player is inspecting.
- `autoloads/SaveData.cs:78-82` — add `List<PartyMember> Party` (nullable). Migration: if null, convert the existing single-character state into one Sen entry on load.
- `autoloads/SaveManager.cs:64-130` and `GameManager.ApplySaveData` (`autoloads/GameManager.cs:394-443`) — round-trip the party list.
- `autoloads/GameManager.ResetForNewGame` — initialize `_party` with one Sen member.

**Critical decision:** Sen retains the class hot-swap (`MultiClassData` still drives Sen's class progression). Lily/Rain don't use `MultiClassData` at all — their `Level/Exp/Stats` live directly on `PartyMember`. This avoids forcing Lily/Rain through the snapshot/restore dance.

**Phase exit criteria:** save/load round-trips a single-Sen party identical to the current game; no UI changes yet.

---

### Phase 3 — Recruitment via the residency menu

*Lily and Rain become real party members.*

**Modified:**

- `core/data/NpcResidencyEntry.cs` — add `[Export] string PartyMemberId` and `[Export] PlayerClass JoinClass` and `[Export] Resource? StartingStats` (CharacterStats template, typed as `Resource` per CLAUDE.md gotcha).
- `scenes/menus/ResidencyShopMenu.cs:OnHire` — after deducting gold and setting the flag, if `entry.PartyMemberId` is set, build a `PartyMember` from `entry.StartingStats` + `entry.JoinClass` + `entry.PartyMemberId` and call `GameManager.Party.Add(...)`. Also play a short Dialogic timeline like `lily_joins.dtl` (per Q10).
- `scenes/overworld/maps/mellyr/MellyrOutpost.cs` (or wherever Lily/Rain NPCs spawn) — hide the NPC node when the corresponding `PartyMember` is in the party. Show them again if removed.

**New files:**

- `resources/residency/lily_residency.tres` and `rain_residency.tres` — the new `NpcResidencyEntry` instances with `PartyMemberId="lily"`, `JoinClass=Alchemist` and `PartyMemberId="rain"`, `JoinClass=Rogue`.
- `resources/characters/lily_stats.tres`, `rain_stats.tres` — base CharacterStats. Tune to taste.
- `dialog/timelines/lily_joins.dtl`, `rain_joins.dtl` (optional)

**Tests:** `PartyDataTest.cs` — `AddMember_FromResidency_AppearsInActiveList`; `RecruitmentFlowTest.cs` — flag set + party contains member.

---

### Phase 4 — Overworld follower system

*Visual follower chain like DQ3. Restricted to 16×16 sprite maps only — WorldMap and dungeon floors. Towns/interiors that use the larger 32×32 player sprite render the leader alone.*

**New files:**

- `scenes/player/PartyFollower.cs` (Node2D with AnimatedSprite2D) and `PartyFollower.tscn` — used by both WorldMap (smooth) and dungeon floors (grid-locked). The follower picks its movement style from the leader type at spawn.
- `core/extensions/FollowerTrail.cs` — pure ring-buffer of recent leader positions, parameterized by spacing. For grid maps the spacing is one full 16-px tile; for the WorldMap the spacing is the same (16 px) so they read as a connected line.

**Modified:**

- `scenes/player/Player.cs:Moved` and `scenes/player/DungeonPlayer.cs:Moved` signals already exist — the trail subscribes to whichever leader exists in the current scene.
- `scenes/overworld/OverworldBase.cs:_Ready` — after spawning the player, **only if `UseSmallPlayer == true`** (the 16×16 sprite condition), iterate `GameManager.Party.GetActive()` (skipping the leader) and instantiate one `PartyFollower` per member, configured with that member's `OverworldSpritePath`. Towns with the 32×32 sprite skip follower spawn entirely.
- Followers must despawn on scene transition (handled automatically by scene reload) and must NOT exist during battle (battles don't re-enter the overworld scene tree, so this is free).
- The four-direction animation logic mirrors the leader. Sprite assets:
  - Lily → `res://assets/sprites/player/Lily_Overworld.png`
  - Rain → `res://assets/sprites/player/Rain_Overworld.png`
  Stored on the `PartyMember.OverworldSpritePath` field set during recruitment.

**Trail mechanics:** push leader's `GlobalPosition` into a `Vector2[]` ring buffer every time the `Moved` signal fires. Each follower indexes a fixed offset back (follower 0 = 1 tile back, follower 1 = 2 tiles back, etc.). On the grid maps this snaps cleanly between tiles; on the WorldMap (which is also tile-based via WorldMapPlayer) the same spacing applies. With up to 6 members, the ring buffer needs to hold at least 6 distinct previous tile positions.

**Tests:** Pure-logic test in `FollowerTrailTest.cs` — given a sequence of leader positions and a spacing, verify the follower position equals the position N steps behind, and that the buffer correctly handles up to 5 followers.

---

### Phase 5 — Party Menu

*New menu for party management.*

**New files:**

- `scenes/menus/PartyMenu.tscn` + `PartyMenu.cs` (CanvasLayer 51, matches the existing menu layer convention)
- The menu shows:
  - List of party members (name, class, level, HP/MP) — up to 6 rows
  - Two-row formation card: Front and Back (the suggested formation feature). Members can be dragged or shifted between rows.
  - Highlight = currently selected for inspection (sets `GameManager.SelectedMemberId`)
  - Buttons: **View Stats**, **View Equipment**, **Reorder** (swap with neighbor), **Set Leader**, **Toggle Row** (Front/Back)

**Modified:**

- `scenes/menus/PauseMenu.cs` and `.tscn` — add a **PARTY** button between Bestiary and Equipment

---

### Phase 6 — Stats & Equipment menus become party-aware

*Both menus get a "◀ Member ▶" cursor at the top.*

**Modified:**

- `scenes/menus/StatsMenu.cs:260-285` — read from the selected `PartyMember` instead of `gm.PlayerStats`; show that member's class levels (only Sen will have multiple to show); cycle members with left/right.
- `scenes/menus/EquipmentMenu.cs` — same idea; equipment dict reads/writes go to `member.EquippedItemPaths` instead of `InventoryData.EquippedItemPaths`. The shared bag (`InventoryItemPaths`) stays where it is.
- `core/data/PlayerCombatData.cs:ComputeEffectiveStats` — refactor to take a `PartyMember` parameter and compute that member's effective stats. Existing single-character call sites get the leader by default.
- `core/data/EquipmentLogic.cs` — already pure; no changes needed if signature accepts a generic dict.

**Tests:** extend `EquipmentLogicTest` and add `MultiMemberEquipmentTest` — equipping a sword on Lily must not affect Sen's stats.

---

### Phase 7 — Multi-actor battle

*The big one.*

**Modified:**

- `scenes/battle/BattleScene.cs:25-176` — replace `_enemy` and `_enemyCurrentHp` with `List<EnemyInstance>` (a small new class wrapping `EnemyData` + `CurrentHp` + `Statuses`). Replace single-player turn flow with a **speed-sorted turn queue** built from `PartyData.GetActive()` (only living members) + the living enemies, sorted by `CharacterStats.Speed` descending. The queue is rebuilt at the start of each round so newly KO'd or revived actors are picked up immediately.
- New: `core/data/TurnQueue.cs` (pure static logic, NUnit-tested) — `BuildOrder(IEnumerable<PartyMember> party, IEnumerable<EnemyInstance> enemies)` returns the speed-sorted cycle, ties broken deterministically (e.g., by member id then enemy index).
- `scenes/battle/ui/BattleHud.cs` — rebuild as a horizontal row of `PartyMemberCard` (small UI control showing portrait, HP bar, MP bar, status icons). One card per active party member, up to 6 cards laid out across the bottom. Highlight the active actor with a yellow border.
- New: `scenes/battle/ui/PartyMemberCard.tscn` + `.cs`
- `scenes/battle/ui/EnemyNameplate.cs` — already exists; instantiate one per enemy and lay them out horizontally above the enemies.
- `scenes/battle/BattleStatusEffects.cs:1-52` — replace the two single-actor dicts with `Dictionary<int /* actor index */, Dictionary<StatusEffect,int>>`.
- `scenes/battle/BattleAttackResolver.cs` — already takes minigame inputs; extend to take the active member's class and the targeted enemy index.
- Action menu — when it's a party member's turn, show an "X's Turn" banner (Dialogic name-card style) and pop the existing ActionMenu *just for that actor*; resolve their action before the next actor in the queue acts (no "command everyone first" phase). Targeting cursor moves between live enemies for offensive actions. Items/Spells with multi-target support get an "All" cursor option.
- Victory: when no living enemies. Defeat: when no living party members. KO'd members stay at 0 HP for the rest of the battle but **still receive XP** at victory.

**Battle BGM, BPM, intro, level-up screens:** all keep working — the level-up screen needs to fire per member who actually leveled, not just for the leader.

**XP distribution:** `core/data/PartyMemberLogic.cs:DistributeXp` — divide `enemy.ExpDrop` evenly across **all active members, including KO'd ones**. Each member rolls level-ups independently.

**Tests:** `TurnQueueTest.cs` (pure), `XpDistributionTest.cs` (pure), `MultiActorBattleStateTest.cs` (NUnit, pure model).

---

### Phase 8 — Mixed-enemy encounters in the world

*Putting Phase 7 to work.*

**New files:**

- `resources/encounters/world_day_mixed.tres` — `Enemies = [wisplet, centiphantom]`, BattleBpm 130
- `resources/encounters/world_night_mixed.tres` — same enemies, faster BPM

**Modified:**

- `scenes/overworld/WorldMap.tscn` — add the new encounters to `DayEncounters` and `NightEncounters` so the existing weighted picker (`scenes/overworld/WorldMap.cs:413-420`) randomly selects between Wisplet-only, Centiphantom-only, and mixed.
- `scenes/battle/BattleScene.cs:SetupEnemySprite` — generalize to lay out N enemies horizontally, centered, with spacing that scales by enemy count.

---

### Phase 9 — Polish & docs

- Update `CLAUDE.md`: new "Party System" section, PartyMenu added to the CanvasLayer table, new core/data files listed.
- Update `README.md` enemy/encounter list.
- Add party-related entries to the journal/bestiary if it shows recruitment moments.
- New game vs. existing save migration: a save written before Phase 2 must roundtrip into a one-Sen party — covered in Phase 2.

---

## Phase ordering rationale

Phases 0–1 are pure additions and won't break anything else, so they can ship independently and give immediate value. Phase 2 is the keystone refactor — once `PartyMember` exists, every menu and the battle scene will have to migrate, but the migration becomes straightforward. Phases 3–6 each touch one system at a time. Phase 7 is the largest single change because it rewires battle flow; do it last so all the data and UI scaffolding is already in place. Phase 8 is essentially a content commit on top of Phase 7.

If you want to see Lily/Rain in the game *before* doing the multi-actor battle work, we could ship Phases 0–6 first and have battle still be Sen-only — Lily and Rain would follow on the overworld and appear in menus, but battles would still be 1v1. That's a clean intermediate milestone if the scope feels too big to do in one go.

---

## Recommended ship strategy

Recommendation: ship in **two waves** so each is independently testable.

- **Wave 1 — Phases 0–6.** Lily and Rain join the party, follow on the WorldMap and dungeon floors, and appear in Party/Stats/Equipment menus. Battles remain 1-on-1 (Sen-only). Already provides huge new player-facing value.
- **Wave 2 — Phases 7–9.** Multi-actor battle, mixed encounters, and final docs/polish.
