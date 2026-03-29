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
    [Export] public EncounterData? BossEncounterData { get; set; }

    /// <summary>
    /// World-map tile the player returns to after warping to the surface.
    /// Must match the ReturnTile set on the WorldMapEntrance node in WorldMap.tscn.
    /// </summary>
    [Export] public Vector2I SurfaceReturnTile { get; set; } = new Vector2I(8, 12);

    private Area2D _bossEntrance  = null!;
    private Area2D _warpToSurface = null!;

    public override void _Ready()
    {
        base._Ready();
        if (Engine.IsEditorHint()) return;

        _bossEntrance  = GetNode<Area2D>("BossEntrance");
        _warpToSurface = GetNode<Area2D>("WarpToSurface");

        bool bossDefeated = GameManager.Instance.GetFlag(Flags.DungeonBossDefeated);

        // Boss entrance is only interactive before the boss is beaten
        _bossEntrance.Monitoring = !bossDefeated;

        // Warp is only available after the boss is beaten
        _warpToSurface.Monitoring = bossDefeated;

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
        _ = SceneTransition.Instance.ToBattleAsync(BossEncounterData);
    }

    private void OnWarpToSurfaceEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        GameManager.Instance.WorldMapReturnTile = SurfaceReturnTile;
        _ = SceneTransition.Instance.GoToAsync("res://scenes/overworld/WorldMap.tscn");
    }
}
