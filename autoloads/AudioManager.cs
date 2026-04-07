using Godot;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Hud;

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

	// Active crossfade/stop tween — killed before starting a new one to prevent races
	private Tween? _bgmTween;
	private string _currentBgmPath = "";

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
	public void PlayBgm(string path, float fadeTime = 1.0f, float bpm = 0f,
		float beatOffsetSec = 0f, bool forceRestart = false)
	{
		if (!ResourceLoader.Exists(path)) return;

		// If the same track is already playing, keep it going seamlessly
		// (e.g. transitioning between dungeon floors with the same BGM).
		// Battle scenes pass forceRestart=true to always start fresh.
		var active = _usingPlayerA ? _bgmPlayer : _bgmPlayerB;
		if (!forceRestart && path == _currentBgmPath && active.Playing)
			return;
		_currentBgmPath = path;

		var incoming = _usingPlayerA ? _bgmPlayerB : _bgmPlayer;
		var outgoing = _usingPlayerA ? _bgmPlayer  : _bgmPlayerB;
		_usingPlayerA = !_usingPlayerA;

		// Kill any in-progress crossfade to prevent overlapping tweens from
		// silencing the wrong player when two PlayBgm calls happen close together.
		_bgmTween?.Kill();

		// Stop the outgoing player immediately when force-restarting
		// to prevent stale tween callbacks from killing the new track.
		if (forceRestart)
			outgoing.Stop();

		incoming.Stream    = GD.Load<AudioStream>(path);
		incoming.VolumeDb  = -80f;
		incoming.Play();

		// Attach rhythm clock to the new player before the fade completes
		// so beat tracking is accurate from the first frame.
		float effectiveBpm = bpm > 0f ? bpm : RhythmConstants.DefaultBpm;
		RhythmClock.Instance.AttachPlayer(incoming, effectiveBpm, beatOffsetSec);

		GD.Print($"[AudioManager] PlayBgm: {path} (force={forceRestart}, fade={fadeTime}s)");
		SpawnNowPlayingPopup(path);

		_bgmTween = CreateTween();
		_bgmTween.TweenProperty(outgoing, "volume_db", -80f, fadeTime);
		_bgmTween.Parallel().TweenProperty(incoming, "volume_db", BgmTargetDb, fadeTime);
		_bgmTween.TweenCallback(Callable.From(outgoing.Stop));
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
		_currentBgmPath = "";
		RhythmClock.Instance.Stop();
		_bgmTween?.Kill();

		var active = _usingPlayerA ? _bgmPlayer : _bgmPlayerB;
		_bgmTween = CreateTween();
		_bgmTween.TweenProperty(active, "volume_db", -80f, fadeTime);
		_bgmTween.TweenCallback(Callable.From(active.Stop));
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
		player.Stream   = GD.Load<AudioStream>(path);
		player.PitchScale = 1f;
		player.Play();
	}

	/// <summary>Play a one-shot SFX with random pitch variation for organic feel.</summary>
	/// <param name="path">Resource path to the audio file.</param>
	/// <param name="pitchRange">Max deviation from 1.0 (e.g. 0.1 = ±10%).</param>
	public void PlaySfxVaried(string path, float pitchRange = 0.1f)
	{
		if (!ResourceLoader.Exists(path)) return;
		var player = _sfxPool[_sfxPoolIndex % SfxPoolSize];
		_sfxPoolIndex++;
		player.Stream     = GD.Load<AudioStream>(path);
		player.PitchScale = 1f + (float)GD.RandRange(-pitchRange, pitchRange);
		player.Play();
	}

	// ── BGM rhythm sync helpers ───────────────────────────────────────────────

	/// <summary>
	/// Returns the BPM of the currently playing BGM track via <see cref="MusicMetadata"/>,
	/// or <see cref="RhythmConstants.DefaultBpm"/> if no track is playing or the track has
	/// no registered BPM.
	/// </summary>
	public float GetCurrentBgmBpm()
	{
		if (string.IsNullOrEmpty(_currentBgmPath))
			return RhythmConstants.DefaultBpm;

		var info = MusicMetadata.Lookup(_currentBgmPath);
		return info != null && info.Bpm > 0f ? info.Bpm : RhythmConstants.DefaultBpm;
	}

	/// <summary>
	/// Re-binds <see cref="RhythmClock"/> to the active BGM player using the current
	/// track's registered BPM. Use this from rhythm minigames that need to lock notes
	/// to the live overworld track. Falls back to free-running mode if no BGM is playing.
	/// </summary>
	public void AttachRhythmClockToCurrentBgm()
	{
		float bpm    = GetCurrentBgmBpm();
		var   active = _usingPlayerA ? _bgmPlayer : _bgmPlayerB;

		if (active.Playing)
			RhythmClock.Instance.AttachPlayer(active, bpm);
		else
			RhythmClock.Instance.StartFreeRunning(bpm);
	}

	// ── BGM Ducking ───────────────────────────────────────────────────────────

	private Tween? _duckTween;
	private float  _preDuckDb;
	private bool   _isDucked;

	/// <summary>Lower BGM volume for dialog or focus moments.</summary>
	public void DuckBgm(float duckDb = -6f, float fadeTime = 0.3f)
	{
		if (_isDucked) return;
		_isDucked = true;

		// Use the intended target volume (not current, which may be mid-fade)
		_preDuckDb = BgmTargetDb;

		var active = _usingPlayerA ? _bgmPlayer : _bgmPlayerB;
		if (!active.Playing) return;

		_duckTween?.Kill();
		_duckTween = CreateTween();
		_duckTween.TweenProperty(active, "volume_db", _preDuckDb + duckDb, fadeTime);
	}

	/// <summary>Restore BGM volume after ducking.</summary>
	public void RestoreBgm(float fadeTime = 0.5f)
	{
		if (!_isDucked) return;
		_isDucked = false;

		var active = _usingPlayerA ? _bgmPlayer : _bgmPlayerB;
		if (!active.Playing) return;

		_duckTween?.Kill();
		_duckTween = CreateTween();
		_duckTween.TweenProperty(active, "volume_db", _preDuckDb, fadeTime);
	}

	// ── Now-Playing popup ─────────────────────────────────────────────────────

	private static readonly string NowPlayingScene = "res://scenes/hud/NowPlayingPopup.tscn";

	private void SpawnNowPlayingPopup(string path)
	{
		var info = MusicMetadata.Lookup(path);
		if (info == null) return;
		if (!ResourceLoader.Exists(NowPlayingScene)) return;

		var popup = GD.Load<PackedScene>(NowPlayingScene).Instantiate<NowPlayingPopup>();
		GetTree().Root.AddChild(popup);
		popup.Show(info);
	}
}
