using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; } = null!;

	private AudioStreamPlayer _bgmPlayer  = null!;
	private AudioStreamPlayer _bgmPlayerB = null!;
	private bool _usingPlayerA = true;

	private AudioStreamPlayer _ambPlayer  = null!;
	private const float       AmbVolumeDb = -12f;

	private const int SfxPoolSize = 8;
	private AudioStreamPlayer[] _sfxPool = null!;
	private int _sfxPoolIndex = 0;

	// Volume state (linear 0–1)
	private float _masterLinear = 1f;
	private float _bgmLinear    = 0.8f;
	private float _sfxLinear    = 1f;

	private float BgmTargetDb => SettingsLogic.LinearToDb(_masterLinear * _bgmLinear);
	private float SfxTargetDb => SettingsLogic.LinearToDb(_masterLinear * _sfxLinear);

	public override void _Ready()
	{
		Instance    = this;
		ProcessMode = ProcessModeEnum.Always;

		_bgmPlayer  = new AudioStreamPlayer();
		_bgmPlayerB = new AudioStreamPlayer();
		AddChild(_bgmPlayer);
		AddChild(_bgmPlayerB);

		_ambPlayer = new AudioStreamPlayer { VolumeDb = -80f };
		AddChild(_ambPlayer);

		_sfxPool = new AudioStreamPlayer[SfxPoolSize];
		for (int i = 0; i < SfxPoolSize; i++)
		{
			_sfxPool[i] = new AudioStreamPlayer();
			AddChild(_sfxPool[i]);
		}
	}

	/// <summary>
	/// Play a new BGM track, crossfading from the current one.
	/// Pass bpm > 0 to synchronise RhythmClock to this track.
	/// </summary>
	public void PlayBgm(string path, float fadeTime = 1.0f, float bpm = 0f, float beatOffsetSec = 0f)
	{
		if (!ResourceLoader.Exists(path)) return;

		var incoming = _usingPlayerA ? _bgmPlayerB : _bgmPlayer;
		var outgoing = _usingPlayerA ? _bgmPlayer  : _bgmPlayerB;
		_usingPlayerA = !_usingPlayerA;

		incoming.Stream    = GD.Load<AudioStream>(path);
		incoming.VolumeDb  = -80f;
		incoming.Play();

		// Attach rhythm clock to the new player before the fade completes
		// so beat tracking is accurate from the first frame.
		float effectiveBpm = bpm > 0f ? bpm : RhythmConstants.DefaultBpm;
		RhythmClock.Instance.AttachPlayer(incoming, effectiveBpm, beatOffsetSec);

		var tween = CreateTween();
		tween.TweenProperty(outgoing, "volume_db", -80f, fadeTime);
		tween.Parallel().TweenProperty(incoming, "volume_db", BgmTargetDb, fadeTime);
		tween.TweenCallback(Callable.From(outgoing.Stop));
	}

	/// <summary>
	/// Applies volume settings from SettingsManager. All values are linear 0–1.
	/// Changes take effect immediately on all currently-playing players.
	/// </summary>
	public void SetVolumes(float master, float bgm, float sfx, float _dialogTyping)
	{
		_masterLinear = Mathf.Clamp(master, 0f, 1f);
		_bgmLinear    = Mathf.Clamp(bgm,    0f, 1f);
		_sfxLinear    = Mathf.Clamp(sfx,    0f, 1f);

		// Apply to BGM players that are currently active
		if (_bgmPlayer.Playing)  _bgmPlayer.VolumeDb  = BgmTargetDb;
		if (_bgmPlayerB.Playing) _bgmPlayerB.VolumeDb = BgmTargetDb;

		// Apply to SFX pool
		float sfxDb = SfxTargetDb;
		foreach (var player in _sfxPool)
			player.VolumeDb = sfxDb;
	}

	public void StopBgm(float fadeTime = 1.0f)
	{
		RhythmClock.Instance.Stop();
		var active = _usingPlayerA ? _bgmPlayer : _bgmPlayerB;
		var tween  = CreateTween();
		tween.TweenProperty(active, "volume_db", -80f, fadeTime);
		tween.TweenCallback(Callable.From(active.Stop));
	}

	/// <summary>
	/// Start a looping ambient track, fading in over <paramref name="fadeTime"/> seconds.
	/// Silently does nothing if the file does not exist, so maps work without an ambience asset.
	/// </summary>
	public void PlayAmbience(string path, float fadeTime = 2.0f)
	{
		if (!ResourceLoader.Exists(path)) return;
		_ambPlayer.Stream   = GD.Load<AudioStream>(path);
		_ambPlayer.VolumeDb = -80f;
		_ambPlayer.Play();
		var tween = CreateTween();
		tween.TweenProperty(_ambPlayer, "volume_db", AmbVolumeDb, fadeTime)
			.SetTrans(Tween.TransitionType.Sine);
	}

	/// <summary>
	/// Smoothly moves the ambience player to <paramref name="targetDb"/> over
	/// <paramref name="duration"/> seconds. Does nothing if ambience is not playing.
	/// Use this for permanent in-session volume shifts (e.g. after a story event).
	/// </summary>
	public void FadeAmbienceTo(float targetDb, float duration)
	{
		if (!_ambPlayer.Playing) return;
		var tween = CreateTween();
		tween.TweenProperty(_ambPlayer, "volume_db", targetDb, duration)
			.SetTrans(Tween.TransitionType.Sine);
	}

	/// <summary>Fade out and stop the ambient track.</summary>
	public void StopAmbience(float fadeTime = 1.5f)
	{
		if (!_ambPlayer.Playing) return;
		var tween = CreateTween();
		tween.TweenProperty(_ambPlayer, "volume_db", -80f, fadeTime)
			.SetTrans(Tween.TransitionType.Sine);
		tween.TweenCallback(Callable.From(_ambPlayer.Stop));
	}

	/// <summary>Play a one-shot sound effect from the pool.</summary>
	public void PlaySfx(string path)
	{
		if (!ResourceLoader.Exists(path)) return;
		var player = _sfxPool[_sfxPoolIndex % SfxPoolSize];
		_sfxPoolIndex++;
		player.Stream = GD.Load<AudioStream>(path);
		player.Play();
	}
}
