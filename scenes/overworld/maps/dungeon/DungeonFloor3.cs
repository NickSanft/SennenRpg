using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Dungeon Floor 3 — deepest level. Contains the dungeon boss and the warp back to the surface.
///
/// BossEntrance: fixed-encounter Area2D. Triggers once; afterwards the flag prevents re-entry.
/// WarpToSurface: Area2D that becomes active only after the boss is defeated.
///
/// Assign BossEncounterData in the Inspector to the floor-3 boss EncounterData resource.
/// SurfaceReturnTile must match the ReturnTile on the dungeon WorldMapEntrance in WorldMap.tscn.
/// </summary>
public partial class DungeonFloor3 : OverworldBase
{
    public override bool CountsForTownRewards => true;

    [Export] public EncounterData? BossEncounterData { get; set; }

    /// <summary>
    /// World-map tile the player returns to after warping to the surface.
    /// Must match the ReturnTile set on the WorldMapEntrance node in WorldMap.tscn.
    /// </summary>
    [Export] public Vector2I SurfaceReturnTile { get; set; } = new Vector2I(8, 12);

    private Area2D _bossEntrance  = null!;
    private Area2D _warpToSurface = null!;

    // Boss visual: 4 tiles (32×32 world units) centered on the BossEntrance Area2D.
    // After defeat we drop a black ColorRect over those tiles.
    private ColorRect? _bossBlackout;

    public override void _Ready()
    {
        const string enc = "res://resources/encounters/encounter_003.tres";
        if (ResourceLoader.Exists(enc))
        {
            var encounter = GD.Load<EncounterData>(enc);
            encounter.EncounterChancePerStep = 18f; // 18% per step on floor 3
            RandomEncounterTable.Add(encounter);
        }

        base._Ready();
        if (Engine.IsEditorHint()) return;

        _bossEntrance  = GetNode<Area2D>("BossEntrance");
        _warpToSurface = GetNode<Area2D>("WarpToSurface");

        bool bossDefeated = GameManager.Instance.GetFlag(Flags.DungeonBossDefeated);

        // Boss entrance is only interactive before the boss is beaten
        _bossEntrance.Monitoring = !bossDefeated;

        // Warp is only available after the boss is beaten
        _warpToSurface.Monitoring = bossDefeated;

        if (bossDefeated)
            ShowBossBlackout();

        _bossEntrance.BodyEntered  += OnBossEntranceEntered;
        _warpToSurface.BodyEntered += OnWarpToSurfaceEntered;
    }

    private void OnBossEntranceEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        if (GameManager.Instance.GetFlag(Flags.DungeonBossDefeated)) return;
        if (BossEncounterData == null)
        {
            GD.PushWarning("[DungeonFloor3] BossEncounterData is not assigned — boss fight skipped.");
            return;
        }

        // Remember exactly where the player was so they respawn on this tile after the fight.
        if (body is Node2D player)
            GameManager.Instance.BattleReturnPosition = player.GlobalPosition;

        _ = SceneTransition.Instance.ToBattleAsync(BossEncounterData);
    }

    /// <summary>
    /// Drops a 32×32 black square over the four tiles that visually represent the boss.
    /// The square is anchored to the BossEntrance Area2D so its position tracks the
    /// scene's actual collision shape (centered, 32×32).
    /// </summary>
    private void ShowBossBlackout()
    {
        if (_bossBlackout != null) return;
        _bossBlackout = new ColorRect
        {
            Color    = Colors.Black,
            Size     = new Vector2(32f, 32f),
            Position = _bossEntrance.GlobalPosition - new Vector2(16f, 16f),
            ZIndex   = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_bossBlackout);
    }

    private void OnWarpToSurfaceEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        GameManager.Instance.WorldMapReturnTile = SurfaceReturnTile;
        _ = SceneTransition.Instance.GoToAsync("res://scenes/overworld/WorldMap.tscn", autoSave: true);
    }
}
