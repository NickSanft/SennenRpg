using Godot;
using System.Threading.Tasks;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

public enum TransitionType { Fade, BattleFlash }

public partial class SceneTransition : CanvasLayer
{
	public static SceneTransition Instance { get; private set; } = null!;

	private ColorRect _overlay = null!;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		Layer = 100; // Render on top of everything

		_overlay = new ColorRect
		{
			Color = Colors.Black,
			Modulate = new Color(1, 1, 1, 0), // Transparent initially
			AnchorsPreset = (int)Control.LayoutPreset.FullRect
		};
		AddChild(_overlay);
	}

	/// <summary>Transition to a new scene with an animation.</summary>
	public async Task GoToAsync(string scenePath, TransitionType type = TransitionType.Fade)
	{
		await PlayOut(type);
		GetTree().ChangeSceneToFile(scenePath);
		await PlayIn(type);
	}

	/// <summary>Start a battle encounter. Stores the encounter for BattleScene to read.</summary>
	public async Task ToBattleAsync(EncounterData encounter)
	{
		BattleRegistry.Instance.SetPendingEncounter(encounter);
		await GoToAsync("res://scenes/battle/BattleScene.tscn", TransitionType.BattleFlash);
	}

	private async Task PlayOut(TransitionType type)
	{
		_overlay.Color = type == TransitionType.BattleFlash ? Colors.White : Colors.Black;
		var tween = CreateTween();
		tween.TweenProperty(_overlay, "modulate:a", 1.0f, type == TransitionType.BattleFlash ? 0.08f : 0.2f);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private async Task PlayIn(TransitionType type)
	{
		var tween = CreateTween();
		tween.TweenProperty(_overlay, "modulate:a", 0.0f, 0.2f);
		await ToSignal(tween, Tween.SignalName.Finished);
	}
}
