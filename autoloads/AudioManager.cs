using Godot;

namespace SennenRpg.Autoloads;

public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; } = null!;

	private AudioStreamPlayer _bgmPlayer = null!;
	private AudioStreamPlayer _bgmPlayerB = null!;    // Second player for crossfade
	private bool _usingPlayerA = true;

	// SFX pool — reuse players to avoid instantiation overhead per sound
	private const int SfxPoolSize = 8;
	private AudioStreamPlayer[] _sfxPool = null!;
	private int _sfxPoolIndex = 0;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		_bgmPlayer = new AudioStreamPlayer();
		_bgmPlayerB = new AudioStreamPlayer();
		AddChild(_bgmPlayer);
		AddChild(_bgmPlayerB);

		_sfxPool = new AudioStreamPlayer[SfxPoolSize];
		for (int i = 0; i < SfxPoolSize; i++)
		{
			_sfxPool[i] = new AudioStreamPlayer();
			AddChild(_sfxPool[i]);
		}
	}

	/// <summary>Play a new BGM track, crossfading from the current one.</summary>
	public void PlayBgm(string path, float fadeTime = 1.0f)
	{
		if (!ResourceLoader.Exists(path)) return;

		var incoming = _usingPlayerA ? _bgmPlayerB : _bgmPlayer;
		var outgoing = _usingPlayerA ? _bgmPlayer : _bgmPlayerB;
		_usingPlayerA = !_usingPlayerA;

		incoming.Stream = GD.Load<AudioStream>(path);
		incoming.VolumeDb = -80f;
		incoming.Play();

		var tween = CreateTween();
		tween.TweenProperty(outgoing, "volume_db", -80f, fadeTime);
		tween.Parallel().TweenProperty(incoming, "volume_db", 0f, fadeTime);
		tween.TweenCallback(Callable.From(outgoing.Stop));
	}

	public void StopBgm(float fadeTime = 1.0f)
	{
		var active = _usingPlayerA ? _bgmPlayer : _bgmPlayerB;
		var tween = CreateTween();
		tween.TweenProperty(active, "volume_db", -80f, fadeTime);
		tween.TweenCallback(Callable.From(active.Stop));
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
