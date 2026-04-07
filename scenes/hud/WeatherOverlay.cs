using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Full-screen ambient weather overlay — one per overworld scene, added by WorldMap
/// when <c>AllowsWeather</c> is true. Subscribes to <see cref="WeatherManager.WeatherChanged"/>
/// and crossfades between Sunny / Foggy / Stormy / Snowy / Aurora visuals.
///
/// Visuals are drawn with simple <see cref="ColorRect"/>s and per-frame custom drawing
/// (no shaders) so the overlay works in GL Compatibility with no asset dependencies.
/// </summary>
public partial class WeatherOverlay : CanvasLayer
{
    private const int NumRainStreaks = 60;
    private const int NumSnowflakes  = 45;
    private const int NumAuroraBands = 3;

    private sealed class Particle
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public float   Size;
        public float   Alpha;
    }

    private WeatherType _current = WeatherType.Sunny;
    private Control    _drawHost = null!;      // owns the _Draw override
    private ColorRect  _tintRect = null!;
    private readonly Particle[] _rain  = new Particle[NumRainStreaks];
    private readonly Particle[] _snow  = new Particle[NumSnowflakes];
    private float _auroraPhase;
    private float _lightningFlash; // 0..1, decays each frame
    private RandomNumberGenerator _rng = new();
    private Vector2 _viewportSize = new(1280, 720);

    public override void _Ready()
    {
        Layer       = 5; // above MinimapHud (4), below pause/menus
        ProcessMode = ProcessModeEnum.Always;

        _rng.Randomize();

        _tintRect = new ColorRect
        {
            Color       = Colors.Transparent,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_tintRect);

        _drawHost = new ParticleDrawHost(this)
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
            MouseFilter  = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_drawHost);

        InitParticles();

        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.WeatherChanged += OnWeatherChanged;
            ApplyWeather(WeatherManager.Instance.Current, animate: false);
        }
    }

    public override void _ExitTree()
    {
        if (WeatherManager.Instance != null)
            WeatherManager.Instance.WeatherChanged -= OnWeatherChanged;
    }

    private void OnWeatherChanged(int newWeatherInt)
    {
        ApplyWeather((WeatherType)newWeatherInt, animate: true);
    }

    // ── Visual state ──────────────────────────────────────────────────────

    /// <summary>
    /// Crossfade the tint layer to the target weather's color. Particles show/hide based
    /// on which weather is currently active; the shared _drawHost always runs but only
    /// draws what matches <see cref="_current"/>.
    /// </summary>
    private void ApplyWeather(WeatherType next, bool animate)
    {
        _current = next;

        Color targetTint = next switch
        {
            WeatherType.Foggy  => new Color(0.85f, 0.87f, 0.90f, 0.30f), // hazy white
            WeatherType.Stormy => new Color(0.05f, 0.08f, 0.14f, 0.40f), // dark blue
            WeatherType.Snowy  => new Color(0.70f, 0.80f, 0.95f, 0.20f), // cool blue
            WeatherType.Aurora => new Color(0.35f, 0.10f, 0.45f, 0.25f), // purple
            _                  => Colors.Transparent,
        };

        if (!animate)
        {
            _tintRect.Color = targetTint;
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(_tintRect, "color", targetTint, 1.5f)
             .SetTrans(Tween.TransitionType.Sine);
    }

    // ── Particle init ─────────────────────────────────────────────────────

    private void InitParticles()
    {
        for (int i = 0; i < _rain.Length; i++)
        {
            _rain[i] = new Particle
            {
                Pos   = new Vector2(_rng.RandfRange(0f, _viewportSize.X), _rng.RandfRange(-_viewportSize.Y, _viewportSize.Y)),
                Vel   = new Vector2(-120f, 700f),
                Size  = _rng.RandfRange(6f, 12f),
                Alpha = _rng.RandfRange(0.35f, 0.75f),
            };
        }
        for (int i = 0; i < _snow.Length; i++)
        {
            _snow[i] = new Particle
            {
                Pos   = new Vector2(_rng.RandfRange(0f, _viewportSize.X), _rng.RandfRange(-_viewportSize.Y, _viewportSize.Y)),
                Vel   = new Vector2(_rng.RandfRange(-20f, 20f), _rng.RandfRange(30f, 60f)),
                Size  = _rng.RandfRange(1.5f, 3.5f),
                Alpha = _rng.RandfRange(0.6f, 1.0f),
            };
        }
    }

    // ── Per-frame updates ─────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        _viewportSize = GetViewport().GetVisibleRect().Size;

        float dt = (float)delta;

        switch (_current)
        {
            case WeatherType.Stormy:
                TickRain(dt);
                MaybeStrikeLightning(dt);
                break;
            case WeatherType.Snowy:
                TickSnow(dt);
                break;
            case WeatherType.Aurora:
                _auroraPhase += dt * 0.5f;
                break;
        }

        if (_lightningFlash > 0f)
            _lightningFlash = Mathf.Max(0f, _lightningFlash - dt * 4f);

        _drawHost.QueueRedraw();
    }

    private void TickRain(float dt)
    {
        for (int i = 0; i < _rain.Length; i++)
        {
            var p = _rain[i];
            p.Pos += p.Vel * dt;
            if (p.Pos.Y > _viewportSize.Y + 20f || p.Pos.X < -20f)
            {
                p.Pos = new Vector2(
                    _rng.RandfRange(0f, _viewportSize.X + 40f),
                    -20f);
            }
        }
    }

    private void TickSnow(float dt)
    {
        for (int i = 0; i < _snow.Length; i++)
        {
            var p = _snow[i];
            // Gentle horizontal sway
            p.Vel.X += _rng.RandfRange(-6f, 6f) * dt;
            p.Vel.X  = Mathf.Clamp(p.Vel.X, -30f, 30f);
            p.Pos   += p.Vel * dt;
            if (p.Pos.Y > _viewportSize.Y + 10f)
            {
                p.Pos = new Vector2(_rng.RandfRange(0f, _viewportSize.X), -10f);
            }
            if (p.Pos.X < -10f)           p.Pos.X = _viewportSize.X + 10f;
            if (p.Pos.X > _viewportSize.X + 10f) p.Pos.X = -10f;
        }
    }

    private void MaybeStrikeLightning(float dt)
    {
        // Small chance per frame for a lightning flash. Independent from the
        // "lightning strike loot bolt" gameplay event in WorldMap.
        if (_rng.Randf() < dt * 0.15f) // ~once every 7 seconds on average
        {
            _lightningFlash = 1f;
            AudioManager.Instance?.PlaySfxVaried(UiSfx.Cursor, 0.0f);
        }
    }

    // ── Drawing host ──────────────────────────────────────────────────────
    // Nested Control class so _Draw can access the parent's fields cleanly.

    private sealed partial class ParticleDrawHost : Control
    {
        private readonly WeatherOverlay _owner;
        public ParticleDrawHost(WeatherOverlay owner) { _owner = owner; }

        public override void _Draw()
        {
            switch (_owner._current)
            {
                case WeatherType.Stormy:
                    DrawRain();
                    DrawLightningFlash();
                    break;
                case WeatherType.Snowy:
                    DrawSnow();
                    break;
                case WeatherType.Aurora:
                    DrawAuroraBands();
                    break;
                case WeatherType.Foggy:
                    DrawFogBands();
                    break;
            }
        }

        private void DrawRain()
        {
            var color = new Color(0.8f, 0.85f, 1f);
            foreach (var p in _owner._rain)
            {
                Vector2 end = p.Pos + new Vector2(-p.Size * 0.2f, p.Size);
                DrawLine(p.Pos, end, color with { A = p.Alpha }, 1.2f);
            }
        }

        private void DrawSnow()
        {
            var color = Colors.White;
            foreach (var p in _owner._snow)
                DrawCircle(p.Pos, p.Size, color with { A = p.Alpha });
        }

        private void DrawLightningFlash()
        {
            if (_owner._lightningFlash <= 0f) return;
            var rect = new Rect2(Vector2.Zero, _owner._viewportSize);
            DrawRect(rect, Colors.White with { A = 0.45f * _owner._lightningFlash });
        }

        private void DrawAuroraBands()
        {
            // Soft horizontal ribbons drifting in parallax.
            float phase = _owner._auroraPhase;
            for (int i = 0; i < NumAuroraBands; i++)
            {
                float yFrac = 0.15f + i * 0.12f + 0.03f * Mathf.Sin(phase + i);
                float y = _owner._viewportSize.Y * yFrac;
                var color = new Color(
                    0.55f + 0.15f * Mathf.Sin(phase * 0.8f + i * 2f),
                    0.25f + 0.15f * Mathf.Cos(phase + i),
                    0.75f + 0.15f * Mathf.Sin(phase * 1.2f + i),
                    0.18f);
                var rect = new Rect2(0f, y, _owner._viewportSize.X, 22f);
                DrawRect(rect, color);
            }
        }

        private void DrawFogBands()
        {
            // Horizontal bands of low-opacity white to fake scrolling fog.
            float t = (float)Time.GetTicksMsec() * 0.00005f;
            for (int i = 0; i < 5; i++)
            {
                float yFrac = 0.1f + i * 0.18f + 0.03f * Mathf.Sin(t * 3f + i);
                float y = _owner._viewportSize.Y * yFrac;
                var rect = new Rect2(0f, y, _owner._viewportSize.X, 40f);
                DrawRect(rect, new Color(1f, 1f, 1f, 0.07f));
            }
        }
    }
}
