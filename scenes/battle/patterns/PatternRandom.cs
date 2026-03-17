using Godot;
using Godot.Collections;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Meta-pattern: picks one of the entries in Patterns at random and instantiates it
/// as a child. Assign any mix of pattern scenes in the inspector.
/// EnemyData.AttackPatternScene → this scene → one real pattern per battle.
/// </summary>
public partial class PatternRandom : Node2D
{
	[Export] public Array<PackedScene> Patterns { get; set; } = [];

	public override void _Ready()
	{
		if (Patterns.Count == 0)
		{
			GD.PushWarning("[PatternRandom] No patterns assigned.");
			return;
		}

		int idx = (int)GD.RandRange(0, Patterns.Count);
		// Clamp in case RandRange returns the upper bound
		idx = Mathf.Clamp(idx, 0, Patterns.Count - 1);

		var chosen = Patterns[idx];
		GD.Print($"[PatternRandom] Chose pattern {idx}: {chosen.ResourcePath}");
		AddChild(chosen.Instantiate());
	}
}
