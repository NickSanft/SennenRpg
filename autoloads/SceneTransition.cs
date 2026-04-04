using Godot;
using System.Threading.Tasks;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

public enum TransitionType { Fade, BattleFlash, PixelMosaic }

public partial class SceneTransition : CanvasLayer
{
	public static SceneTransition Instance { get; private set; } = null!;

	private ColorRect _overlay = null!;
	private ColorRect? _mosaicRect;
	private ShaderMaterial? _mosaicMat;

	private const string MosaicShaderPath = "res://assets/shaders/pixel_mosaic.gdshader";
	private const float  MosaicDuration   = 0.4f;
	private const float  MosaicMaxPixel   = 32.0f;

	public override void _Ready()
	{
		Instance    = this;
		ProcessMode = ProcessModeEnum.Always;
		Layer       = 100;

		_overlay = new ColorRect
		{
			Color    = Colors.Black,
			Modulate = new Color(1, 1, 1, 0),
			AnchorsPreset = (int)Control.LayoutPreset.FullRect
		};
		AddChild(_overlay);

		// Prepare mosaic overlay (hidden until needed)
		if (ResourceLoader.Exists(MosaicShaderPath))
		{
			_mosaicMat = new ShaderMaterial { Shader = GD.Load<Shader>(MosaicShaderPath) };
			_mosaicMat.SetShaderParameter("pixel_size", 1.0f);
			_mosaicRect = new ColorRect
			{
				Material      = _mosaicMat,
				Visible       = false,
				AnchorsPreset = (int)Control.LayoutPreset.FullRect,
				MouseFilter   = Control.MouseFilterEnum.Ignore,
			};
			AddChild(_mosaicRect);
		}
	}

	/// <summary>
	/// Transitions to <paramref name="scenePath"/> with an optional fade.
	/// When <paramref name="autoSave"/> is true, the current save slot is written
	/// to disk before the fade begins — call this on dungeon exits and map changes.
	/// </summary>
	public async Task GoToAsync(string scenePath,
								TransitionType type     = TransitionType.Fade,
								bool           autoSave = false)
	{
		if (autoSave)
			SaveManager.Instance.SaveGame();

		await PlayOut(type);
		GetTree().ChangeSceneToFile(scenePath);
		await PlayIn(type);
	}

	/// <summary>
	/// Start a battle encounter.
	/// </summary>
	public async Task ToBattleAsync(EncounterData encounter)
	{
		BattleRegistry.Instance.SetPendingEncounter(encounter);
		await GoToAsync("res://scenes/battle/BattleScene.tscn", TransitionType.BattleFlash);
	}

	private async Task PlayOut(TransitionType type)
	{
		if (type == TransitionType.PixelMosaic && _mosaicRect != null && _mosaicMat != null)
		{
			_mosaicRect.Visible = true;
			_mosaicMat.SetShaderParameter("pixel_size", 1.0f);
			var tween = CreateTween();
			tween.TweenMethod(
				Callable.From<float>(v => _mosaicMat.SetShaderParameter("pixel_size", v)),
				1.0f, MosaicMaxPixel, MosaicDuration)
				.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
			await ToSignal(tween, Tween.SignalName.Finished);
		}
		else
		{
			_overlay.Color = type == TransitionType.BattleFlash ? Colors.White : Colors.Black;
			var tween = CreateTween();
			tween.TweenProperty(_overlay, "modulate:a", 1.0f,
				type == TransitionType.BattleFlash ? 0.08f : 0.2f);
			await ToSignal(tween, Tween.SignalName.Finished);
		}
	}

	private async Task PlayIn(TransitionType type)
	{
		if (type == TransitionType.PixelMosaic && _mosaicRect != null && _mosaicMat != null)
		{
			var tween = CreateTween();
			tween.TweenMethod(
				Callable.From<float>(v => _mosaicMat.SetShaderParameter("pixel_size", v)),
				MosaicMaxPixel, 1.0f, MosaicDuration)
				.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
			await ToSignal(tween, Tween.SignalName.Finished);
			_mosaicRect.Visible = false;
		}
		else
		{
			var tween = CreateTween();
			tween.TweenProperty(_overlay, "modulate:a", 0.0f, 0.2f);
			await ToSignal(tween, Tween.SignalName.Finished);
		}
	}
}
