using System.Collections.Generic;
using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// Runtime wrapper around an <see cref="EnemyData"/> resource for the multi-actor
/// battle system. Stores per-instance state that the immutable EnemyData resource
/// can't hold — current HP, status effects, the spawned visual node, the nameplate.
///
/// Plain C# class — not a Godot Resource — because it represents a transient
/// in-battle entity. There can be several EnemyInstances per battle, each backed by
/// the same shared EnemyData resource (e.g. two Wisplets in one fight).
/// </summary>
public class EnemyInstance
{
    /// <summary>The shared, immutable enemy definition resource.</summary>
    public EnemyData Data { get; }

    public int CurrentHp { get; set; }
    public int MaxHp     { get; }

    /// <summary>Per-instance status effects (Poison / Stun / Shield / Silence).</summary>
    public Dictionary<StatusEffect, int> Statuses { get; } = new();

    /// <summary>Spawned battle sprite. Set by BattleScene during SetupEnemySprite.</summary>
    public Node2D? Visual { get; set; }

    /// <summary>Spawned floating nameplate. Set by BattleScene during _Ready.</summary>
    public Node? Nameplate { get; set; }

    public bool IsKO => CurrentHp <= 0;
    public string DisplayName => Data?.DisplayName ?? "???";
    public string EnemyId => Data?.EnemyId ?? "";

    public EnemyInstance(EnemyData data, float difficultyMultiplier = 1f)
    {
        Data      = data;
        MaxHp     = System.Math.Max(1, (int)((data?.Stats?.MaxHp ?? 10) * difficultyMultiplier));
        CurrentHp = MaxHp;
    }
}
