using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Extensions;
using SennenRpg.Scenes.Menus;

namespace SennenRpg.Scenes.Battle;

public enum BattleState { Intro, PlayerTurn, EnemyTurn, RhythmPhase, StrikePhase, SkillPhase, Victory, Defeat }

/// <summary>
/// Root battle scene state machine — rhythm RPG version.
/// Fight is routed by class: Bard→RhythmStrike, Fighter→FightBar, Ranger→RangerAim, Mage→MageRuneInput.
/// Enemy turn uses RhythmArena (4-lane note highway).
/// PERFORM opens the Bard skills sub-menu (skills implemented in Phase 5).
/// All dialog is displayed through Dialogic timelines in dialog/timelines/battle_*.dtl.
/// </summary>
public partial class BattleScene : Node2D
{
	private enum SubMenuMode { Perform, Items, Spells, ItemTarget }

	private BattleState   _state;
	private EncounterData? _encounter;

	// Phase 7a — multi-enemy support. _enemies holds every enemy spawned for the
	// current encounter; _targetIndex points at the one currently being attacked.
	// _enemy and _enemyCurrentHp are kept as compatibility accessors below so the
	// rest of the file (which uses them in dozens of places) didn't need to be
	// rewritten — they delegate through to the current target enemy.
	private readonly System.Collections.Generic.List<EnemyInstance> _enemies = new();
	private int _targetIndex;

	// Phase 7b — multi-actor turn order. Speed-sorted queue rebuilt at the start
	// of every round; AdvanceTurn() walks through it, dispatching to a party-member
	// action menu or to a single enemy rhythm phase as appropriate.
	private System.Collections.Generic.List<TurnQueueEntry> _turnQueue = new();
	private int _turnQueueIdx;
	/// <summary>Index of the party member whose turn is currently active. -1 if it's an enemy turn.</summary>
	private int _currentActorMemberIdx = -1;

	// Phase 7c — visible target cursor floated above whichever enemy is currently selected.
	// Built code-only in SetupEnemySprite when the encounter has more than one enemy.
	private Polygon2D? _targetCursor;
	private EnemyInstance? Target =>
		(_targetIndex >= 0 && _targetIndex < _enemies.Count) ? _enemies[_targetIndex] : null;
	/// <summary>Backwards-compat read-only accessor for the current target's data.</summary>
	private EnemyData? _enemy => Target?.Data;
	/// <summary>Backwards-compat accessor for the current target's HP. Setter writes through.</summary>
	private int _enemyCurrentHp
	{
		get => Target?.CurrentHp ?? 0;
		set { if (Target != null) Target.CurrentHp = value; }
	}
	private bool AnyLivingEnemy()
	{
		foreach (var e in _enemies)
			if (!e.IsKO) return true;
		return false;
	}
	/// <summary>Move <see cref="_targetIndex"/> to the next living enemy. No-op when none remain.</summary>
	private void AdvanceTargetIfDead()
	{
		if (Target != null && !Target.IsKO) return;
		for (int i = 0; i < _enemies.Count; i++)
		{
			if (!_enemies[i].IsKO)
			{
				_targetIndex = i;
				if (_enemyNameplate != null && Target != null)
				{
					_enemyNameplate.Setup(Target.DisplayName);
					_enemyNameplate.UpdateStatuses(Target.Statuses);
				}
				RefreshTargetCursor();
				return;
			}
		}
	}

	/// <summary>
	/// Cycle the targeting cursor to the next/previous living enemy.
	/// Called by the player turn's left/right input. No-op for solo encounters.
	/// </summary>
	private void CycleTarget(int direction)
	{
		if (_enemies.Count <= 1) return;
		int n = _enemies.Count;
		int next = _targetIndex;
		for (int step = 0; step < n; step++)
		{
			next = (next + direction + n) % n;
			if (!_enemies[next].IsKO)
			{
				_targetIndex = next;
				if (_enemyNameplate != null && Target != null)
				{
					_enemyNameplate.Setup(Target.DisplayName);
					_enemyNameplate.UpdateStatuses(Target.Statuses);
				}
				RefreshTargetCursor();
				AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
				return;
			}
		}
	}

	public override void _Input(InputEvent e)
	{
		if (_state != BattleState.PlayerTurn) return;

		// Item target selection mode: ←/→ cycle party members, Confirm applies, Cancel goes back
		if (_subMenuMode == SubMenuMode.ItemTarget)
		{
			if (e.IsActionPressed("ui_left"))
			{
				CycleItemTarget(-1);
				GetViewport().SetInputAsHandled();
			}
			else if (e.IsActionPressed("ui_right"))
			{
				CycleItemTarget(+1);
				GetViewport().SetInputAsHandled();
			}
			else if (e.IsActionPressed("interact") || e.IsActionPressed("ui_accept"))
			{
				_ = CommitItemUse();
				GetViewport().SetInputAsHandled();
			}
			else if (e.IsActionPressed("ui_cancel"))
			{
				CancelItemTarget();
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		// Phase 7c: arrow keys cycle the target reticle when the active actor is
		// looking at the action menu. We use _Input rather than _UnhandledInput because
		// the action menu's focused button consumes ui_left / ui_right via Godot's GUI
		// focus navigation before _UnhandledInput would fire — _Input runs first.
		if (_enemies.Count <= 1) return;
		if (e.IsActionPressed("ui_left"))
		{
			CycleTarget(-1);
			GetViewport().SetInputAsHandled();
		}
		else if (e.IsActionPressed("ui_right"))
		{
			CycleTarget(+1);
			GetViewport().SetInputAsHandled();
		}
	}

	// ── Phase 7b — per-actor stat / HP / MP routing ─────────────────────
	// Sen flows through GameManager.PlayerStats / EffectiveStats and the existing
	// HurtPlayer / HealPlayer / UseMp APIs (so all the existing code keeps working).
	// Lily / Rain are read & mutated directly on their PartyMember, since they don't
	// have a presence in PlayerCombatData / InventoryData.

	private PartyMember? CurrentActor()
	{
		var party = GameManager.Instance.Party;
		if (_currentActorMemberIdx < 0 || _currentActorMemberIdx >= party.Members.Count) return null;
		return party.Members[_currentActorMemberIdx];
	}

	private bool CurrentActorIsSen() => CurrentActor()?.MemberId == "sen";

	/// <summary>Combined equipment + milestone bonuses for a non-Sen party member.</summary>
	private EquipmentBonuses SumActorAllBonuses(PartyMember m)
	{
		var equip = SumActorEquipBonuses(m);
		var milestones = CharacterMilestoneLogic.SumAllMilestoneBonuses(
			m.MemberId, m.Level, GameManager.Instance.Party.AllMembers);
		return EquipmentLogic.SumBonuses(new[] { equip, milestones });
	}

	/// <summary>Effective Attack stat for whichever party member is currently acting.</summary>
	private int ActorAttack()
	{
		var m = CurrentActor();
		if (m == null) return 0;
		if (CurrentActorIsSen()) return GameManager.Instance.EffectiveStats.Attack;
		return m.Attack + SumActorAllBonuses(m).Attack;
	}

	private int ActorMagic()
	{
		var m = CurrentActor();
		if (m == null) return 0;
		if (CurrentActorIsSen()) return GameManager.Instance.EffectiveStats.Magic;
		return m.Magic + SumActorAllBonuses(m).Magic;
	}

	private int ActorLuck()
	{
		var m = CurrentActor();
		if (m == null) return 0;
		if (CurrentActorIsSen()) return GameManager.Instance.EffectiveStats.Luck;
		return m.Luck + SumActorAllBonuses(m).Luck;
	}

	private int ActorMaxHp()
	{
		var m = CurrentActor();
		if (m == null) return 1;
		if (CurrentActorIsSen()) return GameManager.Instance.PlayerStats.MaxHp;
		return m.MaxHp;
	}

	private int ActorCurrentHp()
	{
		var m = CurrentActor();
		if (m == null) return 0;
		if (CurrentActorIsSen()) return GameManager.Instance.PlayerStats.CurrentHp;
		return m.CurrentHp;
	}

	private PlayerClass ActorClass()
	{
		var m = CurrentActor();
		if (m == null) return PlayerClass.Bard;
		if (CurrentActorIsSen()) return GameManager.Instance.PlayerStats.Class;
		return m.PlayerClassEnum;
	}

	private string ActorDisplayName()
	{
		var m = CurrentActor();
		return m?.DisplayName ?? GameManager.Instance.PlayerName;
	}

	private void ActorHurt(int amount)
	{
		var m = CurrentActor();
		if (m == null) return;
		if (CurrentActorIsSen())
		{
			GameManager.Instance.HurtPlayer(amount);
		}
		else
		{
			m.CurrentHp = System.Math.Max(0, m.CurrentHp - amount);
		}
	}

	private void ActorHeal(int amount)
	{
		var m = CurrentActor();
		if (m == null) return;
		if (CurrentActorIsSen())
		{
			GameManager.Instance.HealPlayer(amount);
		}
		else
		{
			m.CurrentHp = System.Math.Min(m.MaxHp, m.CurrentHp + amount);
		}
	}

	private bool ActorUseMp(int cost)
	{
		var m = CurrentActor();
		if (m == null) return false;
		if (CurrentActorIsSen()) return GameManager.Instance.UseMp(cost);
		if (m.CurrentMp < cost) return false;
		m.CurrentMp -= cost;
		return true;
	}

	/// <summary>
	/// Apply a status effect to the active actor. Sen routes through the shared
	/// _statuses.PlayerStatuses (so the existing rhythm-arena / status pipeline keeps
	/// working). Lily / Rain write to their per-member dict.
	/// </summary>
	private void ActorApplyStatus(StatusEffect effect, int turns)
	{
		var m = CurrentActor();
		if (m == null) return;
		if (CurrentActorIsSen())
			StatusLogic.Apply(_statuses.PlayerStatuses, effect, turns);
		else
			StatusLogic.Apply(m.Statuses, effect, turns);
	}

	/// <summary>Read the active status dict for a party member by index.</summary>
	private System.Collections.Generic.Dictionary<StatusEffect, int>? GetStatusesFor(int memberIdx)
	{
		var party = GameManager.Instance.Party;
		if (memberIdx < 0 || memberIdx >= party.Members.Count) return null;
		var m = party.Members[memberIdx];
		return m.MemberId == "sen" ? _statuses.PlayerStatuses : m.Statuses;
	}

	/// <summary>Push the current status dict for a member into the BattleHUD card.</summary>
	private void RefreshActorStatusBadges(int memberIdx)
	{
		var dict = GetStatusesFor(memberIdx);
		if (dict == null || _battleHud == null) return;
		var party = GameManager.Instance.Party;
		if (memberIdx < 0 || memberIdx >= party.Members.Count) return;
		_battleHud.UpdateStatusesFor(party.Members[memberIdx].MemberId, dict);
	}

	/// <summary>
	/// Roll growth rates against a non-Sen party member's accumulated XP and apply
	/// any level-ups directly onto their PartyMember.Level/Stats. Returns one
	/// LevelUpResult per gained level so the existing LevelUpScreen can animate them.
	/// </summary>
	private System.Collections.Generic.List<LevelUpResult> RollLevelUpsForMember(PartyMember member)
	{
		var results = new System.Collections.Generic.List<LevelUpResult>();
		if (member == null || member.MemberId == "sen") return results;

		int gained = LevelData.CheckLevelUp(member.Exp, member.Level);
		if (gained == 0) return results;

		// Load this member's class growth rates from disk. Falls back to a flat 50%
		// per stat if the resource is missing so the level-up still happens.
		string growthPath = $"res://resources/characters/growth_rates_{member.Class.ToLower()}.tres";
		GrowthRates? growth = null;
		if (ResourceLoader.Exists(growthPath))
			growth = GD.Load<GrowthRates>(growthPath);

		for (int i = 0; i < gained; i++)
		{
			int oldHp  = member.MaxHp,      oldAtk = member.Attack,  oldDef = member.Defense,
				oldSpd = member.Speed,       oldMag = member.Magic,   oldRes = member.Resistance,
				oldLck = member.Luck;

			int hpRate  = growth?.MaxHp      ?? 50;
			int atkRate = growth?.Attack     ?? 50;
			int defRate = growth?.Defense    ?? 50;
			int spdRate = growth?.Speed      ?? 50;
			int magRate = growth?.Magic      ?? 50;
			int resRate = growth?.Resistance ?? 50;
			int lckRate = growth?.Luck       ?? 50;

			if (GD.RandRange(0, 99) < hpRate)  { member.MaxHp++; member.CurrentHp++; }
			if (GD.RandRange(0, 99) < atkRate)  member.Attack++;
			if (GD.RandRange(0, 99) < defRate)  member.Defense++;
			if (GD.RandRange(0, 99) < spdRate)  member.Speed++;
			if (GD.RandRange(0, 99) < magRate)  member.Magic++;
			if (GD.RandRange(0, 99) < resRate)  member.Resistance++;
			if (GD.RandRange(0, 99) < lckRate)  member.Luck++;

			member.Level++;

			results.Add(new LevelUpResult
			{
				NewLevel      = member.Level,
				MemberId      = member.MemberId,
				MemberName    = member.DisplayName,
				ClassName     = member.Class,
				OldMaxHp      = oldHp,  NewMaxHp      = member.MaxHp,
				OldAttack     = oldAtk, NewAttack     = member.Attack,
				OldDefense    = oldDef, NewDefense    = member.Defense,
				OldSpeed      = oldSpd, NewSpeed      = member.Speed,
				OldMagic      = oldMag, NewMagic      = member.Magic,
				OldResistance = oldRes, NewResistance = member.Resistance,
				OldLuck       = oldLck, NewLuck       = member.Luck,
			});
		}

		GD.Print($"[BattleScene] {member.DisplayName} gained {gained} level(s) — now Lv {member.Level}.");
		return results;
	}

	/// <summary>True when every party member has 0 HP — the multi-actor game-over check.</summary>
	private bool PartyAllKO()
	{
		var party = GameManager.Instance.Party;
		// Sen's HP lives in PlayerStats; everyone else's lives on PartyMember.
		foreach (var m in party.Members)
		{
			if (m.MemberId == "sen")
			{
				if (GameManager.Instance.PlayerStats.CurrentHp > 0) return false;
			}
			else if (!m.IsKO)
			{
				return false;
			}
		}
		return party.Members.Count > 0;
	}

	/// <summary>Sum static-equipment bonuses for a non-Sen party member from their EquippedItemPaths dict.</summary>
	private static EquipmentBonuses SumActorEquipBonuses(PartyMember member)
	{
		var list = new System.Collections.Generic.List<EquipmentBonuses>();
		foreach (var kv in member.EquippedItemPaths)
		{
			if (string.IsNullOrEmpty(kv.Value) || !ResourceLoader.Exists(kv.Value)) continue;
			var data = GD.Load<EquipmentData>(kv.Value);
			if (data == null) continue;
			list.Add(data.Bonuses);
		}
		return EquipmentLogic.SumBonuses(list);
	}

	/// <summary>
	/// Called after an attack lands. If the just-damaged target died, plays a quick
	/// shrink+fade on its visual and advances the cursor to the next living enemy.
	/// </summary>
	private void HandleEnemyDeathIfApplicable(EnemyInstance? justHit)
	{
		if (justHit == null || !justHit.IsKO) return;
		if (justHit.Visual is Node2D node)
		{
			var t = CreateTween().SetParallel();
			t.TweenProperty(node, "scale", Vector2.Zero, 0.4f)
				.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Back);
			t.TweenProperty(node, "modulate:a", 0f, 0.4f);
		}
		AdvanceTargetIfDead();
	}
	private SubMenuMode _subMenuMode;
	private readonly System.Collections.Generic.List<int> _itemIndexMap = new();
	private bool        _playerGoesFirst;

	// Item target selection state
	private string?   _pendingItemPath;
	private ItemData? _pendingItemData;
	private int       _itemTargetIdx;
	private float           _difficultyMultiplier = 1f;
	private AdaptationResult _adaptation = RhythmMemoryLogic.ComputeAdaptation(null);
	private bool            _adaptedDialogShown;

	// Rhythm streak reward state
	private bool _rhythmShieldActive;
	private bool _rhythmCounterDamage;

	// ── Enemy reaction visuals during rhythm phase ────────────────────
	private Tween? _enemyReactionTween;
	private bool   _enemyWorriedActive;   // trembling at combo >= 5
	private bool   _enemyTauntTriggered;  // taunt bounce at 3+ misses
	private bool   _enemySRankTintActive; // red tint when all-perfect >= 5
	private Vector2 _enemyReactionBasePos; // original Position before shake offset
	private Color   _enemyReactionBaseModulate = Colors.White; // original Modulate

	// ── Node references ───────────────────────────────────────────────
	private Node2D          _enemyArea      = null!;
	private ActionMenu      _actionMenu     = null!;
	private SubMenu         _subMenu        = null!;
	private BattleHUD       _battleHud      = null!;
	private RhythmArena     _rhythmArena    = null!;
	private RhythmStrike    _rhythmStrike   = null!;
	private EnemyNameplate  _enemyNameplate = null!;
	/// <summary>Backwards-compat accessor for the current target's spawned visual node.</summary>
	private Node2D? _enemyVisual => Target?.Visual;
	private ShaderMaterial? _hitFlashMat;

	private LevelUpScreen      _levelUpScreen      = null!;
	private CharmMinigame      _charmMinigame      = null!;
	private BardMinigameBase[] _bardSkills         = null!;
	private ShadowBoltMinigame _shadowBoltMinigame = null!;
	private FightBar              _fightBar              = null!;
	private RangerAim             _rangerAim             = null!;
	private MageRuneInput         _mageRuneInput         = null!;
	private RogueStrikeMinigame   _rogueStrike           = null!;
	private AlchemistBrewMinigame _alchemistBrew         = null!;
	private LilyWitherAndBloomMinigame _witherAndBloom   = null!;
	private int                _currentSkillIndex;
	private SpellData?         _currentSpell;
	private List<SpellData>    _knownSpells        = new();
	private PerformanceScore   _performance        = new();

	// Extracted helpers
	private readonly BattleStatusEffects _statuses = new();

	private static readonly string[] DefaultBardSkillNames =
		{ "Bardic Inspiration", "Lullaby", "War Cry", "Serenade", "Dissonance" };

	private PackedScene? _damageNumberScene;
	private Label _battleComboLabel = null!;

	public override void _Ready()
	{
		_enemyArea      = GetNode<Node2D>("EnemyArea");
		_actionMenu     = GetNode<ActionMenu>("ActionMenu");
		_subMenu        = GetNode<SubMenu>("SubMenu");
		_battleHud      = GetNode<BattleHUD>("BattleHUD");
		_rhythmArena    = GetNode<RhythmArena>("RhythmArena");
		_rhythmStrike   = GetNode<RhythmStrike>("RhythmStrike");
		_enemyNameplate = GetNode<EnemyNameplate>("EnemyNameplate");

		// Hide the old native dialog box — all dialog now goes through Dialogic
		GetNodeOrNull<Control>("DialogBox")?.Hide();

		const string dmgPath = "res://scenes/battle/ui/DamageNumber.tscn";
		if (ResourceLoader.Exists(dmgPath))
			_damageNumberScene = GD.Load<PackedScene>(dmgPath);

		// Wire action menu
		_actionMenu.FightSelected   += OnFightSelected;
		_actionMenu.PerformSelected += OnPerformSelected;
		_actionMenu.ItemSelected    += OnItemSelected;
		_actionMenu.FleeSelected    += OnFleeSelected;

		// Wire Dialogic status signals (e.g. "status:poison:3" applies Poison for 3 turns)
		DialogicBridge.Instance.DialogicSignalReceived += OnDialogicSignalReceived;

		_subMenu.OptionSelected += OnSubMenuOptionSelected;
		_subMenu.Cancelled      += OnSubMenuCancelled;

		// Wire rhythm nodes
		_rhythmStrike.StrikeResolved += OnStrikeResolved;
		_rhythmArena.PhaseEnded      += OnRhythmPhaseEnded;
		_rhythmArena.PlayerHurt      += OnPlayerHurt;
		_rhythmArena.StreakReward     += OnStreakReward;

		// Wire class-specific Fight minigames
		_fightBar      = GetNode<FightBar>("FightBar");
		_rangerAim     = GetNode<RangerAim>("RangerAim");
		_mageRuneInput = GetNode<MageRuneInput>("MageRuneInput");
		_rogueStrike   = GetNode<RogueStrikeMinigame>("RogueStrikeMinigame");
		_alchemistBrew = GetNode<AlchemistBrewMinigame>("AlchemistBrewMinigame");
		_fightBar.Confirmed       += OnFighterConfirmed;
		_rangerAim.Confirmed      += OnRangerConfirmed;
		_mageRuneInput.Completed  += OnMageCompleted;
		_rogueStrike.Confirmed    += OnRogueConfirmed;
		_alchemistBrew.Confirmed  += OnAlchemistConfirmed;

		// Instantiate Charm and Bard skills programmatically
		_charmMinigame = InstantiateFullRect<CharmMinigame>("res://scenes/battle/rhythm/CharmMinigame.tscn");
		_bardSkills = new BardMinigameBase[]
		{
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/BardicInspirationMinigame.tscn"),
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/LullabyMinigame.tscn"),
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/WarCryMinigame.tscn"),
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/SerenadeMinigame.tscn"),
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/DissonanceMinigame.tscn"),
		};

		_levelUpScreen = GD.Load<PackedScene>("res://scenes/menus/LevelUpScreen.tscn")
							.Instantiate<LevelUpScreen>();
		AddChild(_levelUpScreen);

		_shadowBoltMinigame = InstantiateFullRect<ShadowBoltMinigame>(
			"res://scenes/battle/rhythm/skills/ShadowBoltMinigame.tscn");
		_shadowBoltMinigame.SkillCompleted += OnSpellMinigameCompleted;

		// Lily's Wither and Bloom — hold-to-bloom skill, built code-only (no .tscn)
		_witherAndBloom = new LilyWitherAndBloomMinigame { Visible = false };
		_witherAndBloom.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_witherAndBloom);
		_witherAndBloom.Confirmed += OnWitherAndBloomConfirmed;

		_charmMinigame.CharmCompleted += OnCharmCompleted;
		foreach (var skill in _bardSkills)
			skill.SkillCompleted += OnSkillCompleted;

		// Load known spells
		foreach (string path in GameManager.Instance.KnownSpellPaths)
		{
			if (ResourceLoader.Exists(path))
				_knownSpells.Add(GD.Load<SpellData>(path));
		}

		// Performance tracking
		_rhythmArena.NoteHit    += grade => _performance.Record((HitGrade)grade);
		_rhythmArena.PlayerHurt += _ => _performance.Record(HitGrade.Miss);

		// Enemy reaction visuals during rhythm phase
		_rhythmArena.NoteHit += OnNoteHitEnemyReaction;
		_rhythmArena.PlayerHurt += _ => OnNoteHitEnemyReaction((int)HitGrade.Miss);
		_rhythmStrike.StrikeResolved += grade => _performance.Record((HitGrade)grade);

		// Combo counter label — positioned below the arena, right-aligned
		_battleComboLabel = new Label
		{
			Text                = "",
			HorizontalAlignment = HorizontalAlignment.Right,
			Visible             = false,
		};
		_battleComboLabel.AddThemeFontSizeOverride("font_size", 14);
		var comboFont = UiTheme.LoadPixelFont();
		if (comboFont != null)
			_battleComboLabel.AddThemeFontOverride("font", comboFont);
		_battleComboLabel.AddThemeColorOverride("font_color", UiTheme.Gold);
		// Arena is at (576, 200), half-size 112x72, so bottom-right is (688, 272)
		_battleComboLabel.Position = new Vector2(588f, 274f);
		_battleComboLabel.Size     = new Vector2(100f, 24f);
		AddChild(_battleComboLabel);

		_rhythmArena.NoteHit += OnNoteHitUpdateComboLabel;
		_rhythmArena.PlayerHurt += _ => OnNoteHitUpdateComboLabel(0);

		// Load encounter
		var encounter = BattleRegistry.Instance.GetPendingEncounter();
		_encounter = encounter;
		_enemies.Clear();
		_targetIndex = 0;

		_difficultyMultiplier = SettingsLogic.EnemyDifficultyMultiplier(
			SettingsManager.Instance?.Current.BattleDifficulty ?? BattleDifficulty.Normal);

		if (encounter != null && encounter.Enemies.Count > 0)
		{
			foreach (var data in encounter.Enemies)
			{
				if (data == null) continue;
				_enemies.Add(new EnemyInstance(data, _difficultyMultiplier));
			}
		}
		else
		{
			GD.PushWarning("[BattleScene] No pending encounter — using placeholder enemy.");
		}

		// Rhythm Memory: look up how this enemy has adapted to the player
		string enemyId = _enemy?.EnemyId ?? "";
		GameManager.Instance.RhythmMemory.TryGetValue(enemyId, out var rhythmHistory);
		_adaptation = RhythmMemoryLogic.ComputeAdaptation(rhythmHistory);
		GD.Print($"[BattleScene] Rhythm Memory lookup: enemy={enemyId}, " +
			$"history={rhythmHistory?.TotalEncounters ?? 0} encounters " +
			$"(P:{rhythmHistory?.TotalPerfects ?? 0} G:{rhythmHistory?.TotalGoods ?? 0} M:{rhythmHistory?.TotalMisses ?? 0}), " +
			$"tier={_adaptation.Tier}, density=×{_adaptation.ObstacleDensityMult:F2}, measures=+{_adaptation.ExtraMeasures}");

		_playerGoesFirst = BattleFormulas.PlayerGoesFirst(
			GameManager.Instance.EffectiveStats.Speed,
			_enemy?.Stats?.Speed ?? 0);

		GD.Print($"[BattleScene] Turn order: {(_playerGoesFirst ? "Player" : "Enemy")} goes first " +
				 $"(player SPD={GameManager.Instance.EffectiveStats.Speed}, enemy SPD={_enemy?.Stats?.Speed ?? 0})");

		// Start battle BGM with BPM
		StartBattleBgm(encounter);

		SetupBattleBackground();
		SetupEnemySprite();
		_enemyNameplate.Setup(_enemy?.DisplayName ?? "???");
		_ = RunIntro();
	}

	// ── BGM ───────────────────────────────────────────────────────────

	private const string DefaultBattleBgmPath =
		"res://assets/music/Corruption Can Be Fun.wav";

	private void StartBattleBgm(EncounterData? encounter)
	{
		string bgmPath = encounter?.BattleBgmPath ?? "";
		if (string.IsNullOrEmpty(bgmPath))
			bgmPath = _enemy?.BattleBgmPath ?? "";
		if (string.IsNullOrEmpty(bgmPath))
			bgmPath = DefaultBattleBgmPath;

		// Prefer the auto-detected BPM/offset from MusicMetadata (which overlays
		// beat_data.json) whenever the analyzer was confident about the track.
		// The hardcoded encounter/enemy BattleBpm values were a workaround from
		// before the analyzer existed and don't always match the actual track
		// (e.g. encounters had Corruption Can Be Fun pinned at 140 while it's
		// actually 179). The .tres values now act as a fallback for tracks the
		// analyzer can't lock onto.
		float bpm        = 0f;
		float beatOffset = 0f;
		var trackInfo = MusicMetadata.Lookup(bgmPath);
		if (trackInfo != null && trackInfo.Bpm > 0f && trackInfo.BeatConfidence >= 0.4f)
		{
			bpm        = trackInfo.Bpm;
			beatOffset = trackInfo.BeatOffsetSec;
		}
		else
		{
			bpm = (encounter?.BattleBpm ?? 0f) > 0f ? encounter!.BattleBpm
				: (_enemy?.BattleBpm ?? 0f) > 0f    ? _enemy!.BattleBpm
				: RhythmConstants.DefaultBpm;
			beatOffset = (_enemy?.BattleBeatOffsetSec ?? 0f);
		}

		if (ResourceLoader.Exists(bgmPath))
		{
			AudioManager.Instance.PlayBgm(bgmPath, fadeTime: 0.1f, bpm: bpm,
			beatOffsetSec: beatOffset, forceRestart: true);
		}
		else
		{
			RhythmClock.Instance.StartFreeRunning(bpm);
			GD.Print($"[BattleScene] No battle BGM found. RhythmClock free-running at {bpm} BPM.");
		}
	}

	// ── Setup ─────────────────────────────────────────────────────────

	private void SetupBattleBackground()
	{
		var baseColor = BattleRegistry.Instance.PendingBackgroundColor;
		// Create gradient: lighter at top, darker at bottom
		var topColor    = baseColor.Lerp(Colors.White, 0.3f) with { A = 0.6f };
		var bottomColor = baseColor.Lerp(Colors.Black, 0.4f) with { A = 0.8f };

		// Use two overlapping ColorRects for a simple two-tone gradient
		var bgTop = new ColorRect
		{
			Color         = topColor,
			AnchorRight   = 1f,
			AnchorBottom  = 0.5f,
			MouseFilter   = Control.MouseFilterEnum.Ignore,
			ZIndex        = -10,
		};
		var bgBottom = new ColorRect
		{
			Color         = bottomColor,
			AnchorTop     = 0.5f,
			AnchorRight   = 1f,
			AnchorBottom  = 1f,
			MouseFilter   = Control.MouseFilterEnum.Ignore,
			ZIndex        = -10,
		};
		AddChild(bgTop);
		AddChild(bgBottom);
		MoveChild(bgTop, 0);
		MoveChild(bgBottom, 1);
	}

	private void SetupEnemySprite()
	{
		// Spawn one battle sprite per enemy in the encounter and lay them out
		// horizontally so the player can see every target. Each visual is stored on
		// its EnemyInstance so the rest of the battle code (crit shake, hit flash,
		// victory shrink) can address the right enemy via the Target accessor.
		if (_enemies.Count == 0) return;

		const float spacing = 192f; // pixels between enemies in the layout

		// Compute the leftmost x so the row is centred on _enemyArea's local origin.
		float totalWidth = (_enemies.Count - 1) * spacing;
		float startX     = -totalWidth * 0.5f;

		const string flashShaderPath = "res://assets/shaders/hit_flash.gdshader";
		Shader? flashShader = ResourceLoader.Exists(flashShaderPath)
			? GD.Load<Shader>(flashShaderPath) : null;

		for (int i = 0; i < _enemies.Count; i++)
		{
			var instance = _enemies[i];
			var data     = instance.Data;
			Node2D? visual = null;

			if (data?.BattleSprite != null && data.SpriteFrameCount > 0)
			{
				var tex  = data.BattleSprite;
				int size = data.SpriteFrameSize;
				var frames = new SpriteFrames();
				frames.AddAnimation("idle");
				frames.SetAnimationLoop("idle", true);
				frames.SetAnimationSpeed("idle", data.SpriteAnimFps);

				for (int f = 0; f < data.SpriteFrameCount; f++)
				{
					var atlas = new AtlasTexture
					{
						Atlas  = tex,
						Region = new Rect2(f * size, 0, size, size),
					};
					frames.AddFrame("idle", atlas);
				}

				var animated = new AnimatedSprite2D { SpriteFrames = frames };
				animated.Play("idle");
				visual = animated;

				// Beat-sync the enemy idle to the battle BGM. Scaled mode keeps
				// long attack cycles smooth (no strobe at high BPMs).
				var enemyBeatSync = new SennenRpg.Scenes.Fx.BeatSyncTrigger
				{
					Mode          = SennenRpg.Core.Data.BeatSyncMode.Scaled,
					BaselineBpm   = 120f,
					FramesPerBeat = 1.0f,
				};
				animated.AddChild(enemyBeatSync);
			}
			else if (data?.BattleSprite != null)
			{
				visual = new Sprite2D { Texture = data.BattleSprite };
			}
			else
			{
				visual = new Polygon2D
				{
					Polygon = [
						new Vector2(-20, -28), new Vector2(20, -28),
						new Vector2(20,  28),  new Vector2(-20, 28)
					],
					Color = new Color(0.55f, 0.3f, 0.85f, 1f),
				};
			}

			// Apply hit flash shader. Each enemy gets its own ShaderMaterial so
			// flashing one doesn't tint the rest.
			if (flashShader != null && visual is CanvasItem ci)
				ci.Material = new ShaderMaterial { Shader = flashShader };

			visual.Scale    = new Vector2(4f, 4f);
			visual.Position = new Vector2(startX + i * spacing, 0f);

			_enemyArea.AddChild(visual);
			instance.Visual = visual;
		}

		// Backwards compat: keep the old _hitFlashMat field pointing at whatever the
		// current target's flash material is, so legacy FlashEnemy() still hits something.
		if (Target?.Visual is CanvasItem targetCi && targetCi.Material is ShaderMaterial mat)
			_hitFlashMat = mat;

		// Phase 7c — spawn a small downward arrow cursor that floats over the active
		// target. Only used when the encounter has more than one enemy; solo fights
		// don't need a target indicator.
		if (_enemies.Count > 1)
		{
			_targetCursor = new Polygon2D
			{
				Polygon = new[]
				{
					new Vector2(-6f, -8f),
					new Vector2( 6f, -8f),
					new Vector2( 0f,  4f),
				},
				Color = new Color(1f, 0.85f, 0.1f, 0.95f),
			};
			_enemyArea.AddChild(_targetCursor);
			RefreshTargetCursor();
		}
	}

	private void RefreshTargetCursor()
	{
		if (_targetCursor == null) return;
		var t = Target;
		if (t?.Visual == null)
		{
			_targetCursor.Visible = false;
			return;
		}
		_targetCursor.Visible = true;
		// Float the cursor 40 px above the enemy sprite — sprites are scaled 4× and
		// drawn around the area origin, so the offset is generous enough to clear them.
		_targetCursor.Position = t.Visual.Position + new Vector2(0f, -64f);
	}

	public override void _ExitTree()
	{
		if (DialogicBridge.Instance != null)
			DialogicBridge.Instance.DialogicSignalReceived -= OnDialogicSignalReceived;
	}

	// ── State machine ─────────────────────────────────────────────────

	private void SetState(BattleState newState)
	{
		_state = newState;
		_subMenu.Visible        = false;
		_rhythmStrike.Visible   = false;
		_enemyNameplate.Visible = newState is BattleState.PlayerTurn or BattleState.EnemyTurn;

		_charmMinigame.Visible      = false;
		_shadowBoltMinigame.Visible = false;
		foreach (var skill in _bardSkills)
			skill.Visible = false;

		// Hide class minigames (visible set individually when activated)
		_fightBar.Visible      = false;
		_rangerAim.Visible     = false;
		_mageRuneInput.Visible = false;

		GD.Print($"[BattleScene] State → {newState}");

		if (newState == BattleState.PlayerTurn)
		{
			int chance = BattleFormulas.FleeChance(
				GameManager.Instance.EffectiveStats.Speed,
				_enemy?.Stats?.Speed ?? 0);
			_actionMenu.SetFleeLabel($"Flee ({chance}%)");
			_actionMenu.SlideIn();
			_actionMenu.FocusFirst();
			_battleHud.SetHints(BattleHints.PlayerTurn);
		}
		else
		{
			_actionMenu.Visible = false;
		}

		if (newState == BattleState.RhythmPhase)
		{
			_battleHud.SetHints(BattleHints.RhythmPhase);
		}
		else
		{
			_rhythmArena.Visible = false;
		}
	}

	private T InstantiateFullRect<T>(string path) where T : Control
	{
		var node = GD.Load<PackedScene>(path).Instantiate<T>();
		node.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		node.Visible = false;
		AddChild(node);
		return node;
	}

	private void SpawnDamageNumber(int damage, bool isCrit)
	{
		if (_damageNumberScene == null) return;
		var num = _damageNumberScene.Instantiate<DamageNumber>();
		// Spawn over the current target's visual when possible so multi-enemy fights
		// place their damage numbers on the right enemy.
		Vector2 anchor = _enemyArea.Position;
		if (Target?.Visual != null)
			anchor = _enemyArea.Position + Target.Visual.Position;
		num.Position = anchor + new Vector2((float)GD.RandRange(-16.0, 16.0), -30f);
		AddChild(num);
		num.Play(damage, isCrit);
	}

	/// <summary>Brief slow-motion on critical hits for dramatic impact.</summary>
	private async void PlayCritSlowMotion()
	{
		Engine.TimeScale = 0.3;
		await ToSignal(GetTree().CreateTimer(0.15f * 0.3f), SceneTreeTimer.SignalName.Timeout);
		Engine.TimeScale = 1.0;
	}

	/// <summary>Briefly flashes the current target enemy sprite white via the hit_flash shader.</summary>
	private void FlashEnemy()
	{
		// Look up the current target's flash material directly so we always flash the
		// enemy that just took the hit, not whichever one happened to be cached at
		// SetupEnemySprite time.
		ShaderMaterial? mat = null;
		if (Target?.Visual is CanvasItem ci && ci.Material is ShaderMaterial sm)
			mat = sm;
		mat ??= _hitFlashMat;
		if (mat == null) return;
		mat.SetShaderParameter("flash_amount", 1.0f);
		var t = CreateTween();
		t.TweenMethod(Callable.From<float>(v => mat.SetShaderParameter("flash_amount", v)),
			1.0f, 0.0f, 0.08f);
	}

	// ── Dialogic helpers ──────────────────────────────────────────────

	/// <summary>Set a Dialogic variable for use in the next battle timeline.</summary>
	private void SetBattleVar(string name, Variant value)
		=> DialogicBridge.Instance.SetVariable(name, value);

	/// <summary>
	/// Start a battle timeline and await its completion.
	/// Falls back silently (with a warning) if the timeline file doesn't exist.
	/// </summary>
	private async Task RunBattleTimeline(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			GD.PushWarning($"[BattleScene] Timeline not found: {path}");
			return;
		}

		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		DialogicBridge.Instance.ConnectTimelineEnded(Callable.From(() => tcs.TrySetResult(true)));
		DialogicBridge.Instance.StartTimeline(path);

		// Safety timeout — if timeline_ended never fires, unblock after 10 s
		_ = Task.Delay(10_000).ContinueWith(_ => tcs.TrySetResult(true));

		await tcs.Task;
	}

	// ── Intro ─────────────────────────────────────────────────────────

	private async Task RunIntro()
	{
		SetState(BattleState.Intro);

		// Enemy intro zoom: every spawned visual starts small and bounces to full size
		// in parallel. Run only the last tween's `Finished` signal as the gate so a
		// single Wisplet behaves identically to a wisplet+centiphantom mixed encounter.
		Tween? lastTween = null;
		foreach (var inst in _enemies)
		{
			if (inst.Visual is not Node2D node) continue;
			node.Scale = new Vector2(2f, 2f);
			var zoomTween = CreateTween();
			zoomTween.TweenProperty(node, "scale", new Vector2(4f, 4f), 0.3f)
				.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
			lastTween = zoomTween;
		}
		if (lastTween != null)
			await ToSignal(lastTween, Tween.SignalName.Finished);

		SetBattleVar("enemy_name", _enemy?.DisplayName ?? "???");
		await RunBattleTimeline("res://dialog/timelines/battle_intro.dtl");

		// Multi-actor flow: kick off the first round. The existing _playerGoesFirst
		// boolean is no longer meaningful — turn order is now determined by the
		// speed-sorted queue built inside BeginRound.
		await BeginRound();
	}

	// ── Fight — routed by player class ───────────────────────────────

	private void OnFightSelected() => _ = DoFightSelected();

	private async Task DoFightSelected()
	{
		_actionMenu.SlideOut();
		SetState(BattleState.StrikePhase);
		await RunBattleTimeline("res://dialog/timelines/battle_strike_prompt.dtl");

		var playerClass = ActorClass();
		GD.Print($"[BattleScene] FIGHT selected. Actor={ActorDisplayName()} Class={playerClass}");

		switch (playerClass)
		{
			case PlayerClass.Fighter:
				_battleHud.SetHints(BattleHints.FighterTiming);
				_fightBar.Visible = true;
				_fightBar.Activate();
				break;

			case PlayerClass.Ranger:
				_battleHud.SetHints(BattleHints.RangerAim);
				_rangerAim.Visible = true;
				_rangerAim.Activate();
				break;

			case PlayerClass.Mage:
				_battleHud.SetHints(BattleHints.MageRunes);
				_mageRuneInput.Visible = true;
				_mageRuneInput.Activate();
				break;

			case PlayerClass.Rogue:
				_battleHud.SetHints(BattleHints.RogueCombo);
				_rogueStrike.Visible = true;
				_rogueStrike.Activate();
				break;

			case PlayerClass.Alchemist:
				_battleHud.SetHints(BattleHints.AlchemistBrew);
				_alchemistBrew.Visible = true;
				_alchemistBrew.Activate();
				break;

			default: // Bard + fallback
				_rhythmStrike.Visible = true;
				_rhythmStrike.Activate();
				break;
		}
	}

	// ── Fighter — timing bar result ───────────────────────────────────

	private void OnFighterConfirmed(float accuracy)
	{
		_fightBar.Visible = false;
		_ = DoStrikeResolved((int)BattleAttackResolver.ResolveFighterGrade(accuracy));
	}

	// ── Ranger — aim reticle result ───────────────────────────────────

	private void OnRangerConfirmed(float accuracy, bool isCrit)
	{
		_rangerAim.Visible = false;

		// Phase 4 unique skills hijack the Ranger reticle when set.
		if (_pendingUniqueSkillActor == "bhata")
		{
			_pendingUniqueSkillActor = "";
			_ = DoUniqueRangerSkillResolved(accuracy, SkillResolver.GravityArrowMultiplier,
				"Gravity Arrow!", new Color(0.7f, 0.4f, 0.95f));
			return;
		}
		if (_pendingUniqueSkillActor == "rain")
		{
			_pendingUniqueSkillActor = "";
			_ = DoUniqueRangerSkillResolved(accuracy, SkillResolver.DualClassMultiplier,
				"Dual-Class strike!", new Color(0.6f, 0.95f, 1f));
			return;
		}

		if (isCrit)
		{
			_ = DoRangerCrit();
			return;
		}
		_ = DoStrikeResolved((int)BattleAttackResolver.ResolveRangerGrade(accuracy));
	}

	private async Task DoRangerCrit()
	{
		int damage = BattleAttackResolver.ResolveRangerCrit(ActorAttack());
		var hit = Target;
		_enemyCurrentHp -= damage;
		CameraShake.ShakeNode(this, intensity: 5f, duration: 0.18f);
		FlashEnemy();
		SpawnDamageNumber(damage, isCrit: true);
		SetBattleVar("hit_label",  "Bull's-eye!");
		SetBattleVar("enemy_name", hit?.DisplayName ?? "???");
		SetBattleVar("damage",     damage.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");
		HandleEnemyDeathIfApplicable(hit);
		if (!AnyLivingEnemy())
			await HandleVictory();
		else
			await RunEnemyTurn();
	}

	// ── Mage — rune sequence result ───────────────────────────────────

	private void OnMageCompleted(int correctCount)
	{
		_mageRuneInput.Visible = false;
		if (_pendingUniqueSkillActor == "kriora")
		{
			_pendingUniqueSkillActor = "";
			_ = DoCrystalKnifeResolved(correctCount);
		}
		else
		{
			_ = DoStrikeResolved((int)BattleAttackResolver.ResolveMageGrade(correctCount));
		}
	}

	// ── Rogue — pickpocket combo result ───────────────────────────────

	private void OnRogueConfirmed(int perfectCount, int hitCount)
	{
		_rogueStrike.Visible = false;
		_ = DoRogueResolved(perfectCount, hitCount);
	}

	private async Task DoRogueResolved(int perfectCount, int hitCount)
	{
		var outcome  = RogueStealLogic.Resolve(perfectCount, hitCount);
		var grade    = RogueStealLogic.ToHitGrade(outcome);
		bool forceCrit = RogueStealLogic.GuaranteedCrit(outcome);

		var (baseDamage, rolledCrit, hitLabel) = BattleAttackResolver.ResolveStrike(
			grade,
			ActorAttack(),
			_enemy?.Stats?.Defense ?? 0,
			ActorLuck());

		int  damage = baseDamage;
		bool isCrit = rolledCrit;
		if (forceCrit && !rolledCrit)
		{
			damage *= 2;
			isCrit = true;
		}

		var hit = Target;
		_enemyCurrentHp -= damage;
		CameraShake.ShakeNode(this, intensity: isCrit ? 5f : 2f, duration: isCrit ? 0.18f : 0.1f);
		FlashEnemy();
		if (isCrit) PlayCritSlowMotion();
		SpawnDamageNumber(damage, isCrit);

		// On a full PerfectSteal, roll the (target enemy's) loot table and pocket one item.
		string stolenLabel = "";
		if (RogueStealLogic.ShouldSteal(outcome))
		{
			var lootEntries = (hit?.Data?.LootTable ?? System.Array.Empty<Resource>())
				.OfType<LootEntry>().ToArray();
			if (lootEntries.Length > 0)
			{
				var paths      = lootEntries.Select(e => e.ItemPath).ToArray();
				var weights    = lootEntries.Select(e => e.Weight).ToArray();
				var guaranteed = lootEntries.Select(e => e.Guaranteed).ToArray();
				string? rolled = LootLogic.RollLoot(paths, weights, guaranteed, () => GD.Randf());
				if (!string.IsNullOrEmpty(rolled) && ResourceLoader.Exists(rolled))
				{
					GameManager.Instance.AddItem(rolled);
					var item  = GD.Load<ItemData>(rolled);
					stolenLabel = $"  Stole {item?.DisplayName ?? "an item"}!";
					GD.Print($"[BattleScene] Rogue PerfectSteal — pocketed {rolled}");
				}
			}
		}

		string label = (outcome == RogueStrikeOutcome.PerfectSteal ? "Pickpocket!" : hitLabel) + stolenLabel;
		GD.Print($"[BattleScene] Rogue {outcome} → grade={grade}, damage={damage}. Enemy HP: {hit?.CurrentHp ?? 0}");

		SetBattleVar("hit_label",   label);
		SetBattleVar("enemy_name",  hit?.DisplayName ?? "???");
		SetBattleVar("damage",      damage.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");

		HandleEnemyDeathIfApplicable(hit);
		if (!AnyLivingEnemy())
			await HandleVictory();
		else
			await RunEnemyTurn();
	}

	// ── Alchemist — potion brew result ────────────────────────────────

	private void OnAlchemistConfirmed(float accuracy)
	{
		_alchemistBrew.Visible = false;
		_ = DoAlchemistResolved(accuracy);
	}

	private async Task DoAlchemistResolved(float accuracy)
	{
		int luck  = ActorLuck();
		int magic = ActorMagic();
		var result = AlchemistBrewLogic.Resolve(accuracy, luck, GD.Randf());
		string label;

		// Every brew (except Backfire) lobs the flask at the enemy for ~50% of a normal
		// physical strike on top of its status effect. Sweet brews use a Perfect grade,
		// neutral fizzles use a Good grade so the alchemist always feels like they're
		// chipping away even when the rolled effect is dull.
		int  splashDamage = 0;
		bool splashCrit   = false;
		var splashHit = Target;
		if (result != BrewResult.Backfire)
		{
			var splashGrade = result == BrewResult.Neutral ? HitGrade.Good : HitGrade.Perfect;
			var (rawDamage, isCrit, _) = BattleAttackResolver.ResolveStrike(
				splashGrade,
				ActorAttack(),
				splashHit?.Data?.Stats?.Defense ?? 0,
				luck);
			splashDamage = System.Math.Max(1, rawDamage / 2);
			splashCrit   = isCrit;
			_enemyCurrentHp -= splashDamage;
			SpawnDamageNumber(splashDamage, splashCrit);
			FlashEnemy();
			CameraShake.ShakeNode(this, intensity: splashCrit ? 4f : 2f,
				duration: splashCrit ? 0.15f : 0.08f);
		}

		switch (result)
		{
			case BrewResult.Heal:
			{
				int amount = AlchemistBrewLogic.HealAmount(magic);
				ActorHeal(amount);
				label = $"Healing Draught! -{splashDamage} HP, +{amount} self";
				GD.Print($"[BattleScene] Alchemist HEAL +{amount}, splash {splashDamage}");
				break;
			}
			case BrewResult.PoisonEnemy:
			{
				// Apply Poison to whichever enemy was just splashed (the current target).
				if (splashHit != null)
				{
					StatusLogic.Apply(splashHit.Statuses, StatusEffect.Poison, AlchemistBrewLogic.PoisonTurns);
					_enemyNameplate?.UpdateStatuses(splashHit.Statuses);
				}
				label = $"Toxic Vial! -{splashDamage} HP, poisoned.";
				GD.Print($"[BattleScene] Alchemist POISON {splashHit?.DisplayName ?? "enemy"}, splash {splashDamage}");
				break;
			}
			case BrewResult.ShieldSelf:
			{
				ActorApplyStatus(StatusEffect.Shield, AlchemistBrewLogic.ShieldTurns);
				RefreshActorStatusBadges(_currentActorMemberIdx);
				label = $"Aegis Tonic! -{splashDamage} HP, {ActorDisplayName()} shielded.";
				GD.Print($"[BattleScene] Alchemist SHIELD {ActorDisplayName()}, splash {splashDamage}");
				break;
			}
			case BrewResult.Backfire:
			{
				int amount = AlchemistBrewLogic.BackfireDamage(magic);
				ActorHurt(amount);
				label = $"The brew explodes! -{amount} HP";
				CameraShake.ShakeNode(this, intensity: 3f, duration: 0.12f);
				GD.Print($"[BattleScene] Alchemist BACKFIRE -{amount}");
				break;
			}
			default: // Neutral
			{
				label = $"The brew fizzles. -{splashDamage} HP";
				GD.Print($"[BattleScene] Alchemist neutral fizzle, splash {splashDamage}");
				break;
			}
		}

		SetBattleVar("hit_label",  label);
		SetBattleVar("enemy_name", splashHit?.DisplayName ?? "???");
		SetBattleVar("damage",     splashDamage.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");

		// Backfire might have KO'd the brewer. Game-over only when ALL members are KO'd.
		if (PartyAllKO())
		{
			await HandleDefeat();
			return;
		}
		// Splash damage may have killed the enemy.
		HandleEnemyDeathIfApplicable(splashHit);
		if (!AnyLivingEnemy())
		{
			await HandleVictory();
			return;
		}
		await RunEnemyTurn();
	}

	private void OnStrikeResolved(int gradeInt) => _ = DoStrikeResolved(gradeInt);

	private async Task DoStrikeResolved(int gradeInt)
	{
		var grade = (HitGrade)gradeInt;
		_rhythmStrike.Visible = false;

		var hit = Target;
		var (damage, isCrit, hitLabel) = BattleAttackResolver.ResolveStrike(
			grade,
			ActorAttack(),
			hit?.Data?.Stats?.Defense ?? 0,
			ActorLuck());
		_enemyCurrentHp -= damage;
		CameraShake.ShakeNode(this, intensity: isCrit ? 5f : 2f, duration: isCrit ? 0.18f : 0.1f);
		FlashEnemy();
		if (isCrit) PlayCritSlowMotion();

		GD.Print($"[BattleScene] {hitLabel} grade={grade}, damage={damage}. Enemy HP: {hit?.CurrentHp ?? 0}");
		SpawnDamageNumber(damage, isCrit);

		SetBattleVar("hit_label",   hitLabel);
		SetBattleVar("enemy_name",  hit?.DisplayName ?? "???");
		SetBattleVar("damage",      damage.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");

		HandleEnemyDeathIfApplicable(hit);
		if (!AnyLivingEnemy())
			await HandleVictory();
		else
			await RunEnemyTurn();
	}

	// ── Perform (Bard Skills) ─────────────────────────────────────────

	private void OnPerformSelected()
	{
		GD.Print("[BattleScene] SKILLS selected");
		_subMenuMode = SubMenuMode.Perform;
		_actionMenu.SlideOut();

		string[] options = BuildSkillsMenuForCurrentActor();
		_subMenu.Populate(options);
		_subMenu.Visible = true;
	}

	/// <summary>
	/// Build the Skills sub-menu options for whichever party member is currently acting.
	/// Sen exposes the existing Bard skills; Lily / Rain / Bhata each get one unique skill.
	/// Spells (when known) are still appended for Sen only.
	/// </summary>
	private string[] BuildSkillsMenuForCurrentActor()
	{
		string actorId = CurrentActor()?.MemberId ?? "sen";
		switch (actorId)
		{
			case "lily":
				return new[] { $"Wither and Bloom  ({SkillResolver.WitherAndBloomMpCost} MP)" };
			case "rain":
				return new[] { $"Dual-Class  ({SkillResolver.DualClassMpCost} MP)" };
			case "bhata":
				return new[] { $"Gravity Arrow  ({SkillResolver.GravityArrowMpCost} MP)" };
			case "kriora":
				return new[] { $"Crystal Knife  ({SkillResolver.CrystalKnifeMpCost} MP)" };
			default:
				// Sen — Bard skills + optional spells suffix
				string[] bardOptions = _enemy?.BardicActOptions is { Length: > 0 }
					? _enemy.BardicActOptions
					: DefaultBardSkillNames;
				return _knownSpells.Count > 0
					? bardOptions.Append("Spells ▶").ToArray()
					: bardOptions;
		}
	}

	// ── Item ──────────────────────────────────────────────────────────

	private void OnItemSelected() => _ = DoItemSelected();

	private async Task DoItemSelected()
	{
		var inv = GameManager.Instance.InventoryItemPaths;

		// Build item list — only show Consumable and Repel items in battle
		_itemIndexMap.Clear();
		var labels = new System.Collections.Generic.List<string>();
		for (int i = 0; i < inv.Count; i++)
		{
			string path = inv[i];
			if (!ResourceLoader.Exists(path)) continue;

			ItemData? item = null;
			try { item = GD.Load<ItemData>(path); } catch { /* ignore load failures */ }
			if (item == null) continue;

			// Only show Consumable and Repel items in battle
			if (item.Type != ItemType.Consumable && item.Type != ItemType.Repel) continue;
			if (item.HealAmount <= 0 && item.RestoreMp <= 0 && item.RepelSteps <= 0) continue;

			string label;
			if (item.RepelSteps > 0)
				label = $"{item.DisplayName} (Repel {item.RepelSteps} steps)";
			else if (item.HealAmount > 0 && item.RestoreMp > 0)
				label = $"{item.DisplayName} (+{item.HealAmount} HP / +{item.RestoreMp} MP)";
			else if (item.RestoreMp > 0)
				label = $"{item.DisplayName} (+{item.RestoreMp} MP)";
			else
				label = $"{item.DisplayName} (+{item.HealAmount} HP)";

			labels.Add(label);
			_itemIndexMap.Add(i);
		}

		GD.Print($"[BattleScene] Item menu: {labels.Count} usable items from {inv.Count} total");

		if (labels.Count == 0)
		{
			await RunBattleTimeline("res://dialog/timelines/battle_no_items.dtl");
			await RunEnemyTurn();
			return;
		}

		_subMenuMode = SubMenuMode.Items;
		_actionMenu.SlideOut();
		GD.Print($"[BattleScene] Populating SubMenu with {labels.Count} items");
		_subMenu.Populate(labels.ToArray());
		_subMenu.Visible = true;
		GD.Print($"[BattleScene] SubMenu visible = {_subMenu.Visible}");
	}

	// ── Flee ──────────────────────────────────────────────────────────

	private void OnFleeSelected() => _ = DoFleeSelected();

	private async Task DoFleeSelected()
	{
		GD.Print("[BattleScene] Flee selected");
		_actionMenu.SlideOut();
		bool escaped = BattleFormulas.AttemptFlee(
			GameManager.Instance.EffectiveStats.Speed,
			_enemy?.Stats?.Speed ?? 0,
			GD.Randf());
		if (escaped)
		{
			await HandleFlee();
		}
		else
		{
			await RunBattleTimeline("res://dialog/timelines/battle_flee_blocked.dtl");
			await RunEnemyTurn();
		}
	}

	// ── Sub-menu dispatch ─────────────────────────────────────────────

	private void OnSubMenuOptionSelected(int index) => _ = DoSubMenuOptionSelected(index);

	private async Task DoSubMenuOptionSelected(int index)
	{
		// ── Spells sub-menu ───────────────────────────────────────────────
		if (_subMenuMode == SubMenuMode.Spells)
		{
			_subMenu.Visible = false;
			await HandleSpellOption(index);
			return;
		}

		// ── Perform sub-menu: check if "Spells ▶" was chosen ─────────────
		if (_subMenuMode == SubMenuMode.Perform && _knownSpells.Count > 0)
		{
			string[] bardOptions = _enemy?.BardicActOptions is { Length: > 0 }
				? _enemy.BardicActOptions
				: DefaultBardSkillNames;

			if (index == bardOptions.Length) // "Spells ▶" is appended at this index
			{
				_subMenuMode = SubMenuMode.Spells;
				string[] spellLabels = _knownSpells
					.Select(s => $"{s.DisplayName}  ({s.MpCost} MP)")
					.ToArray();
				_subMenu.Populate(spellLabels); // stays visible, repopulated
				return;
			}
		}

		// ── Items ─────────────────────────────────────────────────────────
		_subMenu.Visible = false;

		if (_subMenuMode == SubMenuMode.Items) { await HandleItemOption(index); return; }

		// ── Perform — non-Sen unique skills ───────────────────────────────
		string actorId = CurrentActor()?.MemberId ?? "sen";
		if (_subMenuMode == SubMenuMode.Perform && actorId != "sen")
		{
			await HandleUniqueSkillSelected(actorId);
			return;
		}

		// ── Perform — bard skill ──────────────────────────────────────────
		GD.Print($"[BattleScene] Skill option {index} selected");
		_currentSkillIndex = index;

		string[] options = _enemy?.BardicActOptions is { Length: > 0 }
			? _enemy.BardicActOptions
			: DefaultBardSkillNames;

		if (index < options.Length && options[index] == "Charm")
		{
			SetState(BattleState.SkillPhase);
			_charmMinigame.Activate();
			return;
		}

		int skillIndex = index < _bardSkills.Length ? index : 0;
		_currentSkillIndex = skillIndex;
		SetState(BattleState.SkillPhase);
		_bardSkills[skillIndex].Activate();
	}

	private void OnSkillCompleted(int gradeInt) => _ = DoSkillCompleted(gradeInt);

	private async Task DoSkillCompleted(int gradeInt)
	{
		var grade = (HitGrade)gradeInt;

		string result = grade switch
		{
			HitGrade.Perfect => "★ Perfect performance! The enemy is moved.",
			HitGrade.Good    => "♪ Good! The enemy responds warmly.",
			_                => "♩ The performance falls flat...",
		};

		GD.Print($"[BattleScene] Skill grade={grade}");

		SetBattleVar("skill_result", result);
		await RunBattleTimeline("res://dialog/timelines/battle_skill_result.dtl");

		await RunEnemyTurn();
	}

	private void OnCharmCompleted(int successCount, int totalNotes) => _ = DoCharmCompleted(successCount, totalNotes);

	private async Task DoCharmCompleted(int successCount, int totalNotes)
	{
		float ratio = totalNotes > 0 ? (float)successCount / totalNotes : 0f;
		var grade = ratio >= 0.75f ? HitGrade.Perfect : ratio >= 0.5f ? HitGrade.Good : HitGrade.Miss;

		string result = grade switch
		{
			HitGrade.Perfect => "★ Charmed! The enemy can't resist.",
			HitGrade.Good    => "♪ The enemy seems swayed.",
			_                => "♩ The charm attempt fizzles.",
		};

		GD.Print($"[BattleScene] Charm {successCount}/{totalNotes}, grade={grade}");

		SetBattleVar("charm_result",  result);
		SetBattleVar("notes_success", successCount.ToString());
		SetBattleVar("notes_total",   totalNotes.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_charm_result.dtl");

		await RunEnemyTurn();
	}

	// ── Unique per-actor Skills (Phase 4) ────────────────────────────────────

	private string _pendingUniqueSkillActor = "";

	/// <summary>
	/// Routes a Skills-menu selection for non-Sen actors. Each recruit has exactly
	/// one unique skill, so the index is irrelevant — actor identity decides which.
	/// </summary>
	private async Task HandleUniqueSkillSelected(string actorId)
	{
		switch (actorId)
		{
			case "lily":
				if (!ActorUseMp(SkillResolver.WitherAndBloomMpCost))
				{
					await RunNoMpTimeline("Wither and Bloom", SkillResolver.WitherAndBloomMpCost);
					return;
				}
				_pendingUniqueSkillActor = "lily";
				SetState(BattleState.SkillPhase);
				_witherAndBloom.Activate();
				break;

			case "rain":
				if (!ActorUseMp(SkillResolver.DualClassMpCost))
				{
					await RunNoMpTimeline("Dual-Class", SkillResolver.DualClassMpCost);
					return;
				}
				_pendingUniqueSkillActor = "rain";
				_battleHud.SetHints(BattleHints.RangerAim);
				SetState(BattleState.SkillPhase);
				_rangerAim.Visible = true;
				_rangerAim.Activate();
				break;

			case "bhata":
				if (!ActorUseMp(SkillResolver.GravityArrowMpCost))
				{
					await RunNoMpTimeline("Gravity Arrow", SkillResolver.GravityArrowMpCost);
					return;
				}
				_pendingUniqueSkillActor = "bhata";
				_battleHud.SetHints(BattleHints.RangerAim);
				SetState(BattleState.SkillPhase);
				_rangerAim.Visible = true;
				_rangerAim.Activate();
				break;

			case "kriora":
				if (!ActorUseMp(SkillResolver.CrystalKnifeMpCost))
				{
					await RunNoMpTimeline("Crystal Knife", SkillResolver.CrystalKnifeMpCost);
					return;
				}
				_pendingUniqueSkillActor = "kriora";
				_battleHud.SetHints(BattleHints.MageRunes);
				SetState(BattleState.SkillPhase);
				_mageRuneInput.Visible = true;
				_mageRuneInput.Activate();
				break;
		}
	}

	private async Task RunNoMpTimeline(string skillName, int cost)
	{
		SetBattleVar("spell_name", skillName);
		SetBattleVar("mp_cost",    cost.ToString());
		await RunBattleTimeline("res://dialog/timelines/spell_no_mp.dtl");
		SetState(BattleState.PlayerTurn);
	}

	private async Task DoUniqueRangerSkillResolved(float accuracy, float multiplier, string skillLabel, Color burstColor)
	{
		var hit = Target;
		int dmg = SkillResolver.ResolveRangerSkillDamage(
			ActorAttack(),
			hit?.Data?.Stats?.Defense ?? 0,
			accuracy,
			multiplier);

		_enemyCurrentHp -= dmg;
		CameraShake.ShakeNode(this, intensity: 5f, duration: 0.18f);
		FlashEnemy();
		SpawnDamageNumber(dmg, isCrit: true);
		SpawnParticleBurst(burstColor, _enemyArea?.GlobalPosition ?? Vector2.Zero);

		SetBattleVar("hit_label",  skillLabel);
		SetBattleVar("enemy_name", hit?.DisplayName ?? "???");
		SetBattleVar("damage",     dmg.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");

		HandleEnemyDeathIfApplicable(hit);
		if (!AnyLivingEnemy())
			await HandleVictory();
		else
			await RunEnemyTurn();
	}

	// ── Crystal Knife — Kriora AoE skill ─────────────────────────────

	private async Task DoCrystalKnifeResolved(int correctCount)
	{
		int magic = ActorMagic();
		int totalDamage = 0;
		bool isPerfect  = correctCount >= 3;
		var crystalBlue = new Color(0.5f, 0.85f, 1.0f);

		CameraShake.ShakeNode(this, intensity: 6f, duration: 0.25f);

		foreach (var enemy in _enemies)
		{
			if (enemy.IsKO) continue;
			int dmg = SkillResolver.ResolveCrystalKnifeDamage(
				magic, enemy.Data?.Stats?.Defense ?? 0, correctCount);
			enemy.CurrentHp -= dmg;
			totalDamage += dmg;

			// Per-enemy VFX
			if (enemy.Visual is CanvasItem ci && ci.Material is ShaderMaterial sm)
			{
				sm.SetShaderParameter("flash_amount", 1.0f);
				var flashTween = CreateTween();
				flashTween.TweenMethod(
					Callable.From<float>(v => sm.SetShaderParameter("flash_amount", v)),
					1.0f, 0.0f, 0.08f);
			}
			Vector2 burstPos = enemy.Visual is Node2D vis
				? _enemyArea.GlobalPosition + vis.Position
				: _enemyArea?.GlobalPosition ?? Vector2.Zero;
			SpawnParticleBurst(crystalBlue, burstPos);

			// Damage number anchored to this enemy
			if (_damageNumberScene != null)
			{
				var num = _damageNumberScene.Instantiate<DamageNumber>();
				num.Position = (enemy.Visual is Node2D vn
					? _enemyArea.Position + vn.Position
					: _enemyArea.Position)
					+ new Vector2((float)GD.RandRange(-16.0, 16.0), -30f);
				AddChild(num);
				num.Play(dmg, isPerfect);
			}

			HandleEnemyDeathIfApplicable(enemy);
		}

		SetBattleVar("hit_label",  "Crystal Knife!");
		SetBattleVar("enemy_name", "all enemies");
		SetBattleVar("damage",     totalDamage.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");

		if (!AnyLivingEnemy())
			await HandleVictory();
		else
			await RunEnemyTurn();
	}

	private void OnWitherAndBloomConfirmed(float fillRatio) => _ = DoWitherAndBloomResolved(fillRatio);

	private async Task DoWitherAndBloomResolved(float fillRatio)
	{
		var hit = Target;
		int dmg = SkillResolver.ResolveWitherDamage(
			ActorMagic(),
			hit?.Data?.Stats?.Defense ?? 0,
			fillRatio);

		_enemyCurrentHp -= dmg;
		CameraShake.ShakeNode(this, intensity: 4f, duration: 0.16f);
		FlashEnemy();
		SpawnDamageNumber(dmg, isCrit: false);
		SpawnParticleBurst(new Color(0.4f, 0.95f, 0.45f), _enemyArea?.GlobalPosition ?? Vector2.Zero);

		// Heal split across living party
		int healPool = SkillResolver.ResolveWitherHealPool(ActorMagic(), fillRatio);
		var party    = GameManager.Instance.Party;
		int living   = 0;
		foreach (var m in party.Members)
		{
			bool alive = m.MemberId == "sen"
				? GameManager.Instance.PlayerStats.CurrentHp > 0
				: m.CurrentHp > 0;
			if (alive) living++;
		}
		int per = SkillResolver.SplitHealEvenly(healPool, living);
		if (per > 0)
		{
			foreach (var m in party.Members)
			{
				if (m.MemberId == "sen")
				{
					if (GameManager.Instance.PlayerStats.CurrentHp > 0)
						GameManager.Instance.HealPlayer(per);
				}
				else if (m.CurrentHp > 0)
				{
					m.CurrentHp = System.Math.Min(m.MaxHp, m.CurrentHp + per);
				}
			}
		}

		SetBattleVar("hit_label",  $"Wither and Bloom! Party +{per} HP");
		SetBattleVar("enemy_name", hit?.DisplayName ?? "???");
		SetBattleVar("damage",     dmg.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");

		HandleEnemyDeathIfApplicable(hit);
		if (!AnyLivingEnemy())
			await HandleVictory();
		else
			await RunEnemyTurn();
	}

	/// <summary>
	/// Spawn a short-lived burst of colored particles at the given screen position.
	/// Used by the Phase 4 unique skills for visual flair.
	/// </summary>
	private void SpawnParticleBurst(Color color, Vector2 worldPosition)
	{
		var burst = new CpuParticles2D
		{
			GlobalPosition  = worldPosition,
			Emitting        = true,
			OneShot         = true,
			Amount          = 24,
			Lifetime        = 0.6f,
			Explosiveness   = 1f,
			Direction       = new Vector2(0, -1),
			Spread          = 180f,
			InitialVelocityMin = 30f,
			InitialVelocityMax = 80f,
			Gravity         = new Vector2(0, 60f),
			ScaleAmountMin  = 1.5f,
			ScaleAmountMax  = 3f,
			Color           = color,
		};
		AddChild(burst);
		GetTree().CreateTimer(1.2f).Timeout += () =>
		{
			if (IsInstanceValid(burst)) burst.QueueFree();
		};
	}

	// ── Spell handling ────────────────────────────────────────────────────────

	private async Task HandleSpellOption(int index)
	{
		if (index >= _knownSpells.Count)
		{
			SetState(BattleState.PlayerTurn);
			return;
		}

		_currentSpell = _knownSpells[index];
		GD.Print($"[BattleScene] Spell selected: {_currentSpell.DisplayName}");

		// Check MP before starting the minigame
		if (!GameManager.Instance.UseMp(_currentSpell.MpCost))
		{
			SetBattleVar("spell_name", _currentSpell.DisplayName);
			SetBattleVar("mp_cost",    _currentSpell.MpCost.ToString());
			await RunBattleTimeline("res://dialog/timelines/spell_no_mp.dtl");
			SetState(BattleState.PlayerTurn);
			return;
		}

		SetState(BattleState.SkillPhase);
		_shadowBoltMinigame.Activate();
	}

	private void OnSpellMinigameCompleted(int gradeInt) => _ = DoSpellCompleted(gradeInt);

	private async Task DoSpellCompleted(int gradeInt)
	{
		var grade = (HitGrade)gradeInt;
		var spell = _currentSpell!;

		if (grade == HitGrade.Miss)
		{
			SetBattleVar("spell_name", spell.DisplayName);
			await RunBattleTimeline("res://dialog/timelines/spell_shadow_bolt_miss.dtl");
		}
		else
		{
			var hit = Target;
			var (damage, isCrit) = BattleAttackResolver.ResolveSpell(
				grade, spell.BasePower,
				GameManager.Instance.EffectiveStats.Magic,
				hit?.Data?.Stats?.Resistance ?? 0);
			_enemyCurrentHp -= damage;
			CameraShake.ShakeNode(this, intensity: isCrit ? 5f : 2f, duration: isCrit ? 0.18f : 0.1f);
			FlashEnemy();
			SpawnDamageNumber(damage, isCrit);

			string result = isCrit
				? $"★ Perfect! Darkness surges! {damage} magic damage!"
				: $"♪ The bolt connects! {damage} magic damage.";

			SetBattleVar("spell_name",   spell.DisplayName);
			SetBattleVar("spell_result", result);
			SetBattleVar("damage",       damage.ToString());
			await RunBattleTimeline("res://dialog/timelines/spell_shadow_bolt_cast.dtl");

			HandleEnemyDeathIfApplicable(hit);
			if (!AnyLivingEnemy()) { await HandleVictory(); return; }
		}

		await RunEnemyTurn();
	}

	private async Task HandleItemOption(int index)
	{
		var inv = GameManager.Instance.InventoryItemPaths;
		int realIndex = index < _itemIndexMap.Count ? _itemIndexMap[index] : -1;
		if (realIndex < 0 || realIndex >= inv.Count)
		{
			await RunBattleTimeline("res://dialog/timelines/battle_item_nothing.dtl");
			await RunEnemyTurn();
			return;
		}

		string path = inv[realIndex];
		var item = ResourceLoader.Exists(path) ? GD.Load<ItemData>(path) : null;
		if (item == null)
		{
			await RunBattleTimeline("res://dialog/timelines/battle_item_gone.dtl");
			await RunEnemyTurn();
			return;
		}

		// Repel item: skip target selection, apply immediately
		if (item.RepelSteps > 0)
		{
			GameManager.Instance.RemoveItem(path);
			int repelSteps = item.RepelSteps;
			// Bhata Lv15 milestone: Repel items last 50% longer
			if (CharacterMilestoneLogic.HasTag(GameManager.Instance.Party.AllMembers, CharacterMilestone.BhataRepelExtend))
				repelSteps = (int)(repelSteps * 1.5f);
			GameManager.Instance.RepelStepsRemaining += repelSteps;
			SetBattleVar("item_name",   item.DisplayName);
			SetBattleVar("heal_amount", repelSteps.ToString());
			await RunBattleTimeline("res://dialog/timelines/battle_item_repel.dtl");
			await RunEnemyTurn();
			return;
		}

		// HP/MP items: enter target selection mode
		_pendingItemPath = path;
		_pendingItemData = item;
		_itemTargetIdx   = 0;
		_subMenuMode     = SubMenuMode.ItemTarget;

		// Highlight first target and show hint
		_battleHud.ClearTargetHighlights();
		_battleHud.SetTargetHighlight(_itemTargetIdx, true);

		var party = GameManager.Instance.Party;
		if (party.Members.Count > 0)
		{
			string targetName = party.Members[_itemTargetIdx].DisplayName;
			_battleHud.SetHints($"Use {item.DisplayName} on: ◀ {targetName} ▶   [Confirm] Use   [Cancel] Back");
		}
	}

	private void CycleItemTarget(int dir)
	{
		var party = GameManager.Instance.Party;
		if (party.Members.Count <= 1) return;

		_battleHud.SetTargetHighlight(_itemTargetIdx, false);
		_itemTargetIdx = (_itemTargetIdx + dir + party.Members.Count) % party.Members.Count;
		_battleHud.SetTargetHighlight(_itemTargetIdx, true);

		string targetName = party.Members[_itemTargetIdx].DisplayName;
		string itemName = _pendingItemData?.DisplayName ?? "Item";
		_battleHud.SetHints($"Use {itemName} on: ◀ {targetName} ▶   [Confirm] Use   [Cancel] Back");
	}

	private void CancelItemTarget()
	{
		_battleHud.ClearTargetHighlights();
		_battleHud.HighlightActor(_currentActorMemberIdx);
		_pendingItemPath = null;
		_pendingItemData = null;
		_subMenuMode = SubMenuMode.Items;
		_subMenu.Visible = true;
		_battleHud.SetHints("");
	}

	private async Task CommitItemUse()
	{
		if (_pendingItemData == null || _pendingItemPath == null) return;

		var party = GameManager.Instance.Party;
		if (_itemTargetIdx < 0 || _itemTargetIdx >= party.Members.Count) return;

		var target = party.Members[_itemTargetIdx];
		var item = _pendingItemData;
		string path = _pendingItemPath;

		// Clear highlight state
		_battleHud.ClearTargetHighlights();
		_battleHud.HighlightActor(_currentActorMemberIdx);
		_subMenuMode = SubMenuMode.Items; // reset mode
		_battleHud.SetHints("");

		GameManager.Instance.RemoveItem(path);

		// Apply to the selected target
		if (target.MemberId == "sen")
		{
			if (item.HealAmount > 0) GameManager.Instance.HealPlayer(item.HealAmount);
			if (item.RestoreMp  > 0) GameManager.Instance.RestoreMp(item.RestoreMp);
		}
		else
		{
			if (item.HealAmount > 0)
				target.CurrentHp = System.Math.Min(target.MaxHp, target.CurrentHp + item.HealAmount);
			if (item.RestoreMp > 0)
				target.CurrentMp = System.Math.Min(target.MaxMp, target.CurrentMp + item.RestoreMp);
		}

		int displayAmount = item.HealAmount > 0 ? item.HealAmount : item.RestoreMp;
		SetBattleVar("item_name",   item.DisplayName);
		SetBattleVar("heal_amount", displayAmount.ToString());

		_pendingItemPath = null;
		_pendingItemData = null;

		await RunBattleTimeline("res://dialog/timelines/battle_item_used.dtl");
		await RunEnemyTurn();
	}

	private void OnSubMenuCancelled()
	{
		_subMenu.Visible = false;
		SetState(BattleState.PlayerTurn);
	}

	// ── Phase 7b — round / turn queue flow ───────────────────────────

	/// <summary>
	/// Start a fresh round: tick statuses (both sides), apply round-start poison
	/// damage, build a speed-sorted queue from the living actors, then dispatch
	/// the first entry. Called from RunIntro and from AdvanceTurn when the queue
	/// is exhausted.
	/// </summary>
	private async Task BeginRound()
	{
		// Bail straight to victory/defeat if a previous round / poison tick / rhythm
		// damage burst already finished the fight. This guards against AdvanceTurn
		// looping forever when one side has no living actors left.
		if (PartyAllKO()) { await HandleDefeat(); return; }
		if (!AnyLivingEnemy()) { await HandleVictory(); return; }

		// Tick all statuses at the top of each round.
		// _statuses.TickAll() handles Sen's PlayerStatuses (legacy single-actor path).
		// Non-Sen members tick their own per-instance Statuses dict.
		_statuses.TickAll();
		foreach (var m in GameManager.Instance.Party.Members)
		{
			if (m.MemberId == "sen") continue;
			StatusLogic.TickAll(m.Statuses);
		}
		UpdateStatusHud();

		// Apply player Poison (Sen for now — Lily/Rain status effects ship in 7c)
		if (_statuses.PlayerHasStatus(StatusEffect.Poison))
		{
			int dmg = _statuses.PlayerPoisonDamage(GameManager.Instance.PlayerStats.MaxHp);
			GameManager.Instance.HurtPlayer(dmg);
			SetBattleVar("damage", dmg.ToString());
			await RunBattleTimeline("res://dialog/timelines/battle_poison_player.dtl");
			if (GameManager.Instance.PlayerStats.CurrentHp <= 0)
			{
				await HandleDefeat();
				return;
			}
		}

		// Per-enemy Poison tick: walk every living enemy with its OWN status dict
		// and apply poison damage individually. Tick the status durations down too.
		// (TickAll() above only handled Sen's player statuses; enemies are per-instance now.)
		foreach (var ei in _enemies)
		{
			if (ei == null || ei.IsKO) continue;
			StatusLogic.TickAll(ei.Statuses);
			if (StatusLogic.HasStatus(ei.Statuses, StatusEffect.Poison))
			{
				int dmg = StatusLogic.PoisonDamage(ei.Data?.Stats?.MaxHp ?? 10);
				ei.CurrentHp = Math.Max(0, ei.CurrentHp - dmg);
				SetBattleVar("damage",     dmg.ToString());
				SetBattleVar("enemy_name", ei.DisplayName);
				await RunBattleTimeline("res://dialog/timelines/battle_poison_enemy.dtl");
				HandleEnemyDeathIfApplicable(ei);
				if (!AnyLivingEnemy())
				{
					await HandleVictory();
					return;
				}
			}
		}
		// Refresh nameplate badges for the current target after the tick.
		_enemyNameplate?.UpdateStatuses(Target?.Statuses ?? new System.Collections.Generic.Dictionary<StatusEffect, int>());

		// Build the speed-sorted queue from the current living actors.
		var party = GameManager.Instance.Party;
		var partySpeeds = new System.Collections.Generic.List<(int, bool)>(party.Members.Count);
		foreach (var m in party.Members)
		{
			int spd = m.MemberId == "sen"
				? GameManager.Instance.EffectiveStats.Speed
				: m.Speed;
			partySpeeds.Add((spd, m.IsKO));
		}
		var enemySpeeds = new System.Collections.Generic.List<(int, bool)>(_enemies.Count);
		foreach (var e in _enemies)
			enemySpeeds.Add((e.Data?.Stats?.Speed ?? 0, e.IsKO));

		_turnQueue    = TurnQueue.BuildOrder(partySpeeds, enemySpeeds);
		_turnQueueIdx = 0;

		await AdvanceTurn();
	}

	/// <summary>
	/// Walk the queue. Skip any entries whose actor died since the queue was built.
	/// Dispatches to BeginActorTurn for party members or RunSingleEnemyTurn for enemies.
	/// When the queue is exhausted, starts a new round.
	/// </summary>
	private async Task AdvanceTurn()
	{
		while (_turnQueueIdx < _turnQueue.Count)
		{
			var entry = _turnQueue[_turnQueueIdx];
			_turnQueueIdx++;

			if (entry.IsParty)
			{
				var party = GameManager.Instance.Party;
				if (entry.Index >= party.Members.Count) continue;
				var member = party.Members[entry.Index];
				if (member.IsKO) continue;
				_currentActorMemberIdx = entry.Index;
				await BeginActorTurn();
				return; // BeginActorTurn yields control to the action menu
			}
			else
			{
				if (entry.Index >= _enemies.Count) continue;
				var enemy = _enemies[entry.Index];
				if (enemy.IsKO) continue;
				_targetIndex = entry.Index;
				if (_enemyNameplate != null) _enemyNameplate.Setup(enemy.DisplayName);
				await RunSingleEnemyTurn();
				return; // RunSingleEnemyTurn yields to the rhythm phase
			}
		}

		// Queue exhausted — top of next round.
		await BeginRound();
	}

	/// <summary>
	/// Start a single party member's turn. Sets the active actor, then shows the
	/// action menu. Replaces the old single-actor BeginPlayerTurn flow.
	/// </summary>
	private async Task BeginActorTurn()
	{
		// Stun: skip this actor's turn entirely. Sen-only for now.
		if (CurrentActorIsSen() && _statuses.PlayerHasStatus(StatusEffect.Stun))
		{
			await RunBattleTimeline("res://dialog/timelines/battle_stun_player.dtl");
			await AdvanceTurn();
			return;
		}

		_battleHud?.HighlightActor(_currentActorMemberIdx);

		// Phase 7c: short "X's Turn" banner before the action menu pops, but only
		// when the party has more than one member — solo Sen battles keep the same
		// snappy feel as before.
		if (GameManager.Instance.Party.Count > 1)
			await ShowPhaseCard($"{ActorDisplayName()}'s Turn", new Color(1f, 0.85f, 0.1f));

		SetState(BattleState.PlayerTurn);
	}

	/// <summary>
	/// Backwards-compat shim. Older code paths called BeginPlayerTurn after recovering
	/// from a sub-menu cancel — those still want "show the player's action menu", which
	/// in the new flow is just BeginActorTurn for the current actor. We never re-run the
	/// status tick here (that already happened at round start in BeginRound).
	/// </summary>
	private async Task BeginPlayerTurn() => await BeginActorTurn();

	// ── Enemy turn ────────────────────────────────────────────────────

	/// <summary>
	/// Run a single enemy's rhythm phase. The active enemy is whichever the
	/// turn queue selected — already stored in <see cref="Target"/> via _targetIndex.
	/// </summary>
	private async Task RunSingleEnemyTurn()
	{
		// Per-enemy Stun: skip THIS specific enemy's rhythm phase this turn (the
		// turn queue already pointed _targetIndex at the right enemy).
		var actor = Target;
		if (actor != null && StatusLogic.HasStatus(actor.Statuses, StatusEffect.Stun))
		{
			SetBattleVar("enemy_name", actor.DisplayName);
			await RunBattleTimeline("res://dialog/timelines/battle_stun_enemy.dtl");
			await AdvanceTurn();
			return;
		}

		SetState(BattleState.EnemyTurn);

		// Rhythm Memory: adapted enemies acknowledge the player on first enemy turn
		if (!_adaptedDialogShown && _adaptation.Tier != AdaptationTier.None)
		{
			_adaptedDialogShown = true;
			string adaptMsg = _adaptation.Tier switch
			{
				AdaptationTier.Rival    => $"{_enemy?.DisplayName ?? "???"} locks eyes with you. It won't hold back!",
				AdaptationTier.Hardened => $"{_enemy?.DisplayName ?? "???"} remembers your rhythm!",
				AdaptationTier.Wary     => $"{_enemy?.DisplayName ?? "???"} seems more cautious...",
				AdaptationTier.Cocky    => $"{_enemy?.DisplayName ?? "???"} yawns lazily at you.",
				_                       => "",
			};
			if (!string.IsNullOrEmpty(adaptMsg))
			{
				var adaptColor = _adaptation.Tier switch
				{
					AdaptationTier.Rival    => new Color(1f, 0.2f, 0.2f),
					AdaptationTier.Hardened => new Color(1f, 0.5f, 0.1f),
					AdaptationTier.Wary     => new Color(1f, 0.9f, 0.3f),
					AdaptationTier.Cocky    => new Color(0.5f, 0.8f, 1f),
					_                       => Colors.White,
				};
				await ShowPhaseCard(adaptMsg, adaptColor);
			}
		}

		string battlePath = _enemy?.BattleTimelinePath ?? "";
		if (!string.IsNullOrEmpty(battlePath) && ResourceLoader.Exists(battlePath))
		{
			GameManager.Instance.SetState(GameState.Dialog);
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			DialogicBridge.Instance.ConnectTimelineEnded(Callable.From(() => tcs.TrySetResult(true)));
			DialogicBridge.Instance.StartTimelineWithFlags(battlePath);
			await tcs.Task;
			GameManager.Instance.SetState(GameState.Battle);
		}
		else
		{
			string line = _enemy?.BattleDialogLines is { Length: > 0 }
				? _enemy.BattleDialogLines[(int)GD.RandRange(0, _enemy.BattleDialogLines.Length - 1)]
				: $"* {_enemy?.DisplayName ?? "???"} prepares to attack...";

			SetBattleVar("enemy_dialog", line);
			await RunBattleTimeline("res://dialog/timelines/battle_enemy_dialog.dtl");
		}

		BeginRhythmPhase();
	}

	private void BeginRhythmPhase() => _ = DoBeginRhythmPhase();

	private async Task DoBeginRhythmPhase()
	{
		SetState(BattleState.RhythmPhase);
		ResetEnemyReactions();

		if (_adaptation.Tier >= AdaptationTier.Hardened)
			await ShowPhaseCard("⚠  DODGE!  ⚠  (INTENSIFIED)", new Color(1f, 0.15f, 0.15f));
		else if (_adaptation.Tier == AdaptationTier.Cocky)
			await ShowPhaseCard("⚠  DODGE!  ⚠  (WEAKENED)", new Color(0.5f, 0.8f, 1f));
		else
			await ShowPhaseCard("⚠  DODGE!  ⚠", new Color(1f, 0.3f, 0.3f));

		PackedScene? patternScene = _enemy?.AttackPatternScene;
		int totalMeasures = Math.Max(1, 2 + _adaptation.ExtraMeasures);
		_rhythmArena.ObstacleDensityMult = _adaptation.ObstacleDensityMult;
		_rhythmArena.SlideIn();
		_rhythmArena.StartPhase(patternScene, totalMeasures: totalMeasures);

		// Dynamic arena skin based on current enemy
		var currentEnemy = Target;
		if (currentEnemy != null)
		{
			_rhythmArena.ArenaBackgroundTint = GetArenaTint(currentEnemy.Data.EnemyId);
			_rhythmArena.ArenaBorderTint = GetArenaBorderTint(currentEnemy.Data.EnemyId);
		}

		// Reset streak reward flags for this phase
		_rhythmShieldActive = false;
		_rhythmCounterDamage = false;

		GD.Print($"[BattleScene] RhythmPhase started. Pattern: {patternScene?.ResourcePath ?? "none"}, measures: {totalMeasures}, density: {_adaptation.ObstacleDensityMult:F2}");
	}

	private async Task ShowPhaseCard(string text, Color color)
	{
		var label = new Label
		{
			Text                = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Center,
			AnchorLeft          = 0f, AnchorTop    = 0f,
			AnchorRight         = 1f, AnchorBottom = 1f,
			Modulate            = color with { A = 0f },
		};
		label.AddThemeFontSizeOverride("font_size", 32);
		AddChild(label);

		var tween = CreateTween();
		tween.TweenProperty(label, "modulate:a", 1f, 0.2f);
		tween.TweenInterval(0.5f);
		tween.TweenProperty(label, "modulate:a", 0f, 0.3f);
		await ToSignal(tween, Tween.SignalName.Finished);
		label.QueueFree();
	}

	private void OnNoteHitUpdateComboLabel(int _grade)
	{
		int combo = _rhythmArena.CurrentCombo;
		if (combo >= 3)
		{
			_battleComboLabel.Text    = $"x{combo}";
			_battleComboLabel.Visible = true;
		}
		else
		{
			_battleComboLabel.Visible = false;
		}
	}

	private void OnRhythmPhaseEnded()
	{
		if (_state != BattleState.RhythmPhase) return;
		_battleComboLabel.Visible = false;
		ResetEnemyReactions();
		GD.Print($"[BattleScene] Rhythm phase ended. Max combo: {_rhythmArena.MaxStreak}");
		_battleHud.ShowPerformanceSummary(_performance);

		// Counterattack reward: deal 10% of current enemy's max HP as bonus damage
		if (_rhythmCounterDamage)
		{
			_rhythmCounterDamage = false;
			if (Target != null && !Target.IsKO)
			{
				int bonus = Math.Max(1, Target.Data.Stats?.MaxHp / 10 ?? 1);
				Target.CurrentHp -= bonus;
				GD.Print($"[BattleScene] Counterattack! Dealt {bonus} bonus damage to {Target.Data.DisplayName}");
				SpawnDamageNumber(bonus, isCrit: false);
				HandleEnemyDeathIfApplicable(Target);
			}
		}

		// Multi-actor flow: advance the queue. If the round is over this kicks BeginRound.
		_ = AdvanceTurn();
	}

	private void OnStreakReward(string rewardId)
	{
		switch (rewardId)
		{
			case "shield":
				_rhythmShieldActive = true;
				GD.Print("[BattleScene] Rhythm Shield activated — next miss at 50% damage");
				break;
			case "counter":
				_rhythmCounterDamage = true;
				GD.Print("[BattleScene] Counter activated — bonus damage at phase end");
				break;
			// "flow" is handled purely in RhythmArena (cosmetic feedback)
		}
	}

	private static Color GetArenaTint(string enemyId) => enemyId switch
	{
		"wisplet" => new Color(0.06f, 0.04f, 0.14f),           // deep purple-blue (ghostly)
		"centiphantom" => new Color(0.04f, 0.10f, 0.10f),       // deep sea teal
		"centiphantom_quing" => new Color(0.08f, 0.07f, 0.06f), // stone grey
		_ => new Color(0.06f, 0.06f, 0.10f),                    // default dark blue
	};

	private static Color GetArenaBorderTint(string enemyId) => enemyId switch
	{
		"wisplet" => new Color(0.6f, 0.5f, 1.0f),              // pale purple
		"centiphantom" => new Color(0.3f, 0.8f, 0.8f),         // cyan-teal
		"centiphantom_quing" => new Color(0.7f, 0.65f, 0.55f), // warm stone
		_ => Colors.White,
	};

	/// <summary>
	/// Backwards-compat shim. Older code paths called RunEnemyTurn() to "do whatever
	/// happens after the player acted". In the multi-actor flow that now means
	/// "advance the turn queue" — we delegate so every existing handler keeps working.
	/// </summary>
	private async Task RunEnemyTurn() => await AdvanceTurn();

	private void OnPlayerHurt(int damage)
	{
		int scaledDamage = Math.Max(1, (int)(damage * _difficultyMultiplier));

		// Rhythm Shield: halve the first hit after a 10-streak
		if (_rhythmShieldActive)
		{
			scaledDamage = Math.Max(1, scaledDamage / 2);
			_rhythmShieldActive = false;
			GD.Print("[BattleScene] Rhythm Shield absorbed half the damage!");
		}

		// Distribute rhythm-phase damage evenly across every LIVING party member.
		// Sen routes through the existing GameManager.HurtPlayer (which writes into
		// PlayerCombatData); Lily/Rain take damage on their PartyMember directly.
		var party = GameManager.Instance.Party;
		var living = new System.Collections.Generic.List<PartyMember>();
		foreach (var m in party.Members)
		{
			bool ko = m.MemberId == "sen"
				? GameManager.Instance.PlayerStats.CurrentHp <= 0
				: m.IsKO;
			if (!ko) living.Add(m);
		}
		if (living.Count == 0)
		{
			_rhythmArena.Visible = false;
			_ = HandleDefeat();
			return;
		}

		// Kriora Lv15 milestone: 15% chance to negate each hit slice
		bool hasShieldWall = CharacterMilestoneLogic.HasTag(
			GameManager.Instance.Party.AllMembers, CharacterMilestone.KrioraShieldWall);

		int share     = scaledDamage / living.Count;
		int remainder = scaledDamage - share * living.Count;
		for (int i = 0; i < living.Count; i++)
		{
			int slice = share + (i < remainder ? 1 : 0);
			if (slice <= 0) continue;

			if (hasShieldWall && GD.Randf() < 0.15f)
			{
				GD.Print($"[BattleScene] Kriora's Shield Wall blocked {slice} damage for {living[i].DisplayName}!");
				continue; // damage negated
			}

			var m = living[i];
			if (m.MemberId == "sen")
				GameManager.Instance.HurtPlayer(slice);
			else
				m.CurrentHp = Math.Max(0, m.CurrentHp - slice);
		}

		CameraShake.ShakeNode(this, intensity: 3f, duration: 0.12f);
		GD.Print($"[BattleScene] Party hurt for {scaledDamage} (split across {living.Count}). " +
			$"Sen HP: {GameManager.Instance.PlayerStats.CurrentHp}");

		if (PartyAllKO())
		{
			_rhythmArena.Visible = false;
			_ = HandleDefeat();
		}
	}

	// ── Victory / Defeat / Flee ───────────────────────────────────────

	private async Task HandleVictory()
	{
		SetState(BattleState.Victory);
		RhythmClock.Instance.Stop();
		AudioManager.Instance.StopBgm(fadeTime: 0.5f);

		// Hide the multi-enemy target reticle and the nameplate so they don't bleed
		// over the victory dialog.
		if (_targetCursor != null) _targetCursor.Visible = false;
		if (_enemyNameplate != null) _enemyNameplate.Visible = false;

		// Victory: every enemy that's still standing shrinks and fades out.
		// (Already-killed enemies were faded out by HandleEnemyDeathIfApplicable.)
		foreach (var inst in _enemies)
		{
			if (inst.Visual is not Node2D node) continue;
			if (inst.IsKO) continue; // already shrunk
			var shrinkTween = CreateTween().SetParallel();
			shrinkTween.TweenProperty(node, "scale", Vector2.Zero, 0.5f)
				.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Back);
			shrinkTween.TweenProperty(node, "modulate:a", 0f, 0.5f);
		}

		// Victory fanfare SFX
		const string fanfarePath = "res://assets/audio/sfx/victory_fanfare.wav";
		AudioManager.Instance?.PlaySfx(fanfarePath);

		// Record kill + rhythm performance for every enemy in the encounter, summing
		// gold/exp/loot across all of them so multi-enemy fights pay out fully.
		int baseGold = 0;
		int baseExp  = 0;
		foreach (var inst in _enemies)
		{
			var data = inst.Data;
			if (data == null) continue;
			if (!string.IsNullOrEmpty(data.EnemyId))
			{
				GameManager.Instance.RecordKill(data.EnemyId);
				GameManager.Instance.RecordRhythmPerformance(data.EnemyId, _performance);
			}
			baseGold += data.GoldDrop;
			baseExp  += data.ExpDrop;
		}

		// Boss encounters: flag the dungeon so the map can react on return.
		if (_encounter?.IsBoss == true)
			GameManager.Instance.SetFlag(Flags.DungeonBossDefeated, true);

		// Apply Rhythm Memory bonus rewards
		int gold = (int)(baseGold * (1f + _adaptation.BonusGoldPercent));
		int exp  = (int)(baseExp  * (1f + _adaptation.BonusExpPercent));

		// Rain Lv15 milestone: +25% gold from battles
		if (CharacterMilestoneLogic.HasTag(GameManager.Instance.Party.AllMembers, CharacterMilestone.RainGoldBonus))
			gold = (int)(gold * 1.25f);

		GameManager.Instance.AddGold(gold);
		// Sen still levels up via the existing GameManager.AddExp pipeline (which feeds
		// PlayerCombatData growth rolls). For Lily / Rain we add the XP directly onto
		// their PartyMember.Exp and roll their own level-ups against per-class growth
		// rates loaded from disk.
		GameManager.Instance.AddExp(exp);
		var nonSen = new System.Collections.Generic.List<PartyMember>();
		foreach (var m in GameManager.Instance.Party.Members)
			if (m.MemberId != "sen") nonSen.Add(m);
		if (nonSen.Count > 0)
			PartyMemberLogic.DistributeXp(nonSen, exp);
		// Apply per-member growth rolls and append the results to the same pending
		// queue Sen uses, so the LevelUpScreen runs them all in sequence.
		foreach (var m in nonSen)
		{
			var rolled = RollLevelUpsForMember(m);
			if (rolled.Count > 0)
				GameManager.Instance.PendingLevelUps.AddRange(rolled);
		}

		if (_adaptation.BonusGoldPercent > 0f || _adaptation.BonusExpPercent > 0f)
			GD.Print($"[BattleScene] Rhythm Memory bonus: gold {baseGold}→{gold} (+{_adaptation.BonusGoldPercent:P0}), exp {baseExp}→{exp} (+{_adaptation.BonusExpPercent:P0})");

		// Bonus loot roll — fired once per battle, against the first enemy with a bonus path.
		if (_adaptation.BonusLootChance > 0f && GD.Randf() < _adaptation.BonusLootChance)
		{
			foreach (var inst in _enemies)
			{
				string lootPath = inst.Data?.BonusLootItemPath ?? "";
				if (!string.IsNullOrEmpty(lootPath) && ResourceLoader.Exists(lootPath))
				{
					GameManager.Instance.AddItem(lootPath);
					GD.Print($"[BattleScene] Rhythm Memory bonus loot: {lootPath}");
					break;
				}
			}
		}

		// Per-enemy LootTable roll — one drop per defeated enemy.
		foreach (var inst in _enemies)
		{
			var lootEntries = (inst.Data?.LootTable ?? []).OfType<LootEntry>().ToArray();
			if (lootEntries.Length == 0) continue;
			var paths      = lootEntries.Select(e => e.ItemPath).ToArray();
			var weights    = lootEntries.Select(e => e.Weight).ToArray();
			var guaranteed = lootEntries.Select(e => e.Guaranteed).ToArray();
			string? rolled = LootLogic.RollLoot(paths, weights, guaranteed, () => GD.Randf());
			if (!string.IsNullOrEmpty(rolled) && ResourceLoader.Exists(rolled))
			{
				GameManager.Instance.AddItem(rolled);
				GD.Print($"[BattleScene] LootTable drop ({inst.DisplayName}): {rolled}");
			}
		}

		// Show level-up screen for each level gained before the victory dialog
		if (GameManager.Instance.PendingLevelUps.Count > 0)
		{
			var pending = new List<LevelUpResult>(GameManager.Instance.PendingLevelUps);
			GameManager.Instance.PendingLevelUps.Clear();
			await _levelUpScreen.ShowAll(pending);
		}

		SetBattleVar("gold_gained",        gold.ToString());
		SetBattleVar("exp_gained",          exp.ToString());
		SetBattleVar("performance_summary", _performance.GetSummaryText());
		await RunBattleTimeline("res://dialog/timelines/battle_victory.dtl");

		await ReturnToOverworld();
	}

	private async Task HandleFlee()
	{
		SetState(BattleState.Victory);
		RhythmClock.Instance.Stop();
		AudioManager.Instance.StopBgm(fadeTime: 0.5f);
		await RunBattleTimeline("res://dialog/timelines/battle_fled.dtl");
		await ReturnToOverworld();
	}

	private async Task HandleDefeat()
	{
		SetState(BattleState.Defeat);
		RhythmClock.Instance.Stop();
		AudioManager.Instance.StopBgm(fadeTime: 1.0f);
		await RunBattleTimeline("res://dialog/timelines/battle_game_over.dtl");
		await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
		await SceneTransition.Instance.GoToAsync("res://scenes/menus/GameOver.tscn");
	}

	private async Task ReturnToOverworld()
	{
		// Force-close any lingering Dialogic dialog (e.g., if safety timeout fired)
		if (DialogicBridge.Instance.IsRunning())
			DialogicBridge.Instance.EndTimeline();

		string map = GameManager.Instance.LastMapPath;
		if (string.IsNullOrEmpty(map))
			map = "res://scenes/overworld/MAPP.tscn";
		await SceneTransition.Instance.GoToAsync(map);
	}

	// ── Status effects ────────────────────────────────────────────────

	private void OnDialogicSignalReceived(Variant argument)
	{
		if (_statuses.TryHandleDialogicSignal(argument.AsString()))
			UpdateStatusHud();
	}

	private void UpdateStatusHud()
	{
		// Sen's status badges flow through the existing PlayerStatuses dict.
		_battleHud.UpdateStatuses(_statuses.PlayerStatuses);
		// Non-Sen members each push their own per-instance dict to their card.
		var party = GameManager.Instance.Party;
		foreach (var m in party.Members)
		{
			if (m.MemberId == "sen") continue;
			_battleHud.UpdateStatusesFor(m.MemberId, m.Statuses);
		}
		// Per-enemy: nameplate shows the current target's status badges.
		_enemyNameplate.UpdateStatuses(Target?.Statuses
			?? new System.Collections.Generic.Dictionary<StatusEffect, int>());
	}

	// ── Enemy reaction visuals during rhythm phase ────────────────────

	/// <summary>
	/// Get the first living enemy's visual node for reaction animations.
	/// Falls back to the current target, then any living enemy.
	/// </summary>
	private Node2D? GetReactionEnemyVisual()
	{
		if (Target?.Visual != null && !Target.IsKO) return Target.Visual;
		foreach (var e in _enemies)
			if (!e.IsKO && e.Visual != null) return e.Visual;
		return null;
	}

	/// <summary>
	/// Reset all enemy reaction visual state. Called at the start and end
	/// of each rhythm phase so tweens don't bleed between turns.
	/// </summary>
	private void ResetEnemyReactions()
	{
		_enemyReactionTween?.Kill();
		_enemyReactionTween = null;
		_enemyWorriedActive   = false;
		_enemyTauntTriggered  = false;
		_enemySRankTintActive = false;

		// Restore every enemy visual to default scale/modulate/position
		foreach (var e in _enemies)
		{
			if (e.Visual == null) continue;
			e.Visual.Scale    = new Vector2(4f, 4f);
			e.Visual.Modulate = Colors.White;
		}
	}

	/// <summary>
	/// Called on every NoteHit during the rhythm phase. Reads the arena's
	/// public counters and applies visual reactions to the first living enemy.
	/// </summary>
	private void OnNoteHitEnemyReaction(int _grade)
	{
		if (_state != BattleState.RhythmPhase) return;
		var visual = GetReactionEnemyVisual();
		if (visual == null) return;

		int combo     = _rhythmArena.CurrentCombo;
		int misses    = _rhythmArena.TotalMisses;
		int perfects  = _rhythmArena.TotalPerfects;
		int totalNotes = _rhythmArena.TotalNotes;
		var grade     = (HitGrade)_grade;

		// --- Flinch on each Perfect hit ---
		if (grade == HitGrade.Perfect)
		{
			// Kill any running flinch tween so they don't stack
			_enemyReactionTween?.Kill();
			visual.Scale = new Vector2(4f, 4f); // ensure clean baseline

			_enemyReactionTween = CreateTween();
			_enemyReactionTween.TweenProperty(visual, "scale",
				new Vector2(3.6f, 4.4f), 0.07f)
				.SetTrans(Tween.TransitionType.Sine);
			_enemyReactionTween.TweenProperty(visual, "scale",
				new Vector2(4f, 4f), 0.08f)
				.SetTrans(Tween.TransitionType.Bounce);
		}

		// --- Worried trembling at combo >= 5 ---
		if (combo >= 5 && !_enemyWorriedActive)
		{
			_enemyWorriedActive = true;
			_enemyReactionBasePos = visual.Position;
			StartWorriedTremble(visual);
		}
		else if (combo < 5 && _enemyWorriedActive)
		{
			_enemyWorriedActive = false;
			visual.Position = _enemyReactionBasePos;
		}

		// --- Taunt bounce at 3+ misses (fires once per phase) ---
		if (misses >= 3 && !_enemyTauntTriggered)
		{
			_enemyTauntTriggered = true;
			PlayTauntBounce(visual);
		}

		// --- S-rank red tint: all notes Perfect so far, count >= 5 ---
		bool allPerfect = perfects == totalNotes && totalNotes >= 5;
		if (allPerfect && !_enemySRankTintActive)
		{
			_enemySRankTintActive = true;
			var tint = CreateTween();
			tint.TweenProperty(visual, "modulate",
				new Color(1f, 0.55f, 0.55f, 1f), 0.3f);
		}
		else if (!allPerfect && _enemySRankTintActive)
		{
			_enemySRankTintActive = false;
			var tint = CreateTween();
			tint.TweenProperty(visual, "modulate", Colors.White, 0.2f);
		}
	}

	/// <summary>
	/// Start a looping positional tremble on the enemy visual. Runs until
	/// the worried flag is cleared or ResetEnemyReactions is called.
	/// </summary>
	private async void StartWorriedTremble(Node2D visual)
	{
		while (_enemyWorriedActive && IsInstanceValid(visual) && _state == BattleState.RhythmPhase)
		{
			float offsetX = (float)(GD.Randf() * 4f - 2f);
			float offsetY = (float)(GD.Randf() * 2f - 1f);
			visual.Position = _enemyReactionBasePos + new Vector2(offsetX, offsetY);
			await ToSignal(GetTree().CreateTimer(0.04f), SceneTreeTimer.SignalName.Timeout);
		}
		// Snap back when done
		if (IsInstanceValid(visual))
			visual.Position = _enemyReactionBasePos;
	}

	/// <summary>
	/// Quick scale bounce (1.0 -> 1.15 -> 1.0 of base) to taunt the player.
	/// </summary>
	private void PlayTauntBounce(Node2D visual)
	{
		var taunt = CreateTween();
		taunt.TweenProperty(visual, "scale",
			new Vector2(4.6f, 4.6f), 0.12f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		taunt.TweenProperty(visual, "scale",
			new Vector2(4f, 4f), 0.15f)
			.SetTrans(Tween.TransitionType.Bounce)
			.SetEase(Tween.EaseType.Out);
	}
}
