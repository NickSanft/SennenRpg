using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// One-shot full-screen cutscene: cascading purple waves sweep the screen,
/// then "SHE HAS ARISEN" appears letter-by-letter and flickers.
/// Add to the scene tree; auto-frees when finished and emits Finished.
/// </summary>
public partial class ArisesCutscene : CanvasLayer
{
	[Signal] public delegate void FinishedEventHandler();

	private Polygon2D        _overlay        = null!;
	private CenterContainer  _textContainer  = null!;
	private Label            _label          = null!;
	private Tween?           _flickerTween;

	public override void _Ready()
	{
		Layer = 90;
		var vpSize = GetViewport().GetVisibleRect().Size;

		// ── Full-screen dark overlay ─────────────────────────────────────────────
		_overlay = new Polygon2D
		{
			Color   = new Color(0.07f, 0.00f, 0.12f),
			ZIndex  = 0,
			Polygon = new Vector2[]
			{
				Vector2.Zero,
				new Vector2(vpSize.X, 0f),
				new Vector2(vpSize.X, vpSize.Y),
				new Vector2(0f,       vpSize.Y),
			},
		};
		_overlay.Modulate = new Color(1f, 1f, 1f, 0f); // start transparent
		AddChild(_overlay);

		// ── Centred text ─────────────────────────────────────────────────────────
		_textContainer = new CenterContainer
		{
			AnchorRight  = 1f,
			AnchorBottom = 1f,
			OffsetRight  = 0f,
			OffsetBottom = 0f,
			ZIndex       = 2,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		AddChild(_textContainer);

		_label = new Label
		{
			Text         = "SHE HAS ARISEN",
			VisibleRatio = 0f,
			Modulate     = new Color(0.70f, 0.00f, 0.90f, 1f),
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		_label.AddThemeFontSizeOverride("font_size", 20);
		_textContainer.AddChild(_label);

		PhaseOverlayFadeIn();
	}

	// ── Phase 1: fade in dark overlay ───────────────────────────────────────────

	private void PhaseOverlayFadeIn()
	{
		var t = CreateTween();
		t.TweenProperty(_overlay, "modulate:a", 0.93f, 1.4f)
			.SetTrans(Tween.TransitionType.Sine);
		t.TweenCallback(Callable.From(PhaseWaves));
	}

	// ── Phase 2: purple waves cascade down the screen ───────────────────────────

	private void PhaseWaves()
	{
		var vpSize = GetViewport().GetVisibleRect().Size;

		var waveData = new (Color color, float height)[]
		{
			(new Color(0.55f, 0.00f, 0.80f, 0.85f), 45f),
			(new Color(0.30f, 0.00f, 0.50f, 0.90f), 28f),
			(new Color(0.70f, 0.05f, 0.90f, 0.80f), 52f),
			(new Color(0.20f, 0.00f, 0.38f, 0.88f), 22f),
			(new Color(0.60f, 0.10f, 0.85f, 0.76f), 36f),
		};

		float stagger  = 0.22f;
		float duration = 1.15f;

		for (int i = 0; i < waveData.Length; i++)
		{
			var (color, h) = waveData[i];
			var wave = new Polygon2D
			{
				Color   = color,
				ZIndex  = 1,
				Polygon = new Vector2[]
				{
					new Vector2(0f,       0f),
					new Vector2(vpSize.X, 0f),
					new Vector2(vpSize.X, h),
					new Vector2(0f,       h),
				},
				Position = new Vector2(0f, -h),
			};
			AddChild(wave);

			float delay  = stagger * i;
			float endY   = vpSize.Y + h;
			var   wt     = CreateTween();
			wt.TweenProperty(wave, "position:y", endY, duration)
				.SetTrans(Tween.TransitionType.Sine).SetDelay(delay);
			wt.TweenCallback(Callable.From(wave.QueueFree));
		}

		float totalTime = duration + stagger * (waveData.Length - 1) + 0.35f;
		GetTree().CreateTimer(totalTime)
			.Connect("timeout", Callable.From(PhaseTextReveal));
	}

	// ── Phase 3: letter-by-letter reveal ────────────────────────────────────────

	private void PhaseTextReveal()
	{
		var t = CreateTween();
		t.TweenProperty(_label, "visible_ratio", 1f, 2.2f)
			.SetTrans(Tween.TransitionType.Linear);
		t.TweenCallback(Callable.From(PhaseFlicker));
	}

	// ── Phase 4: creepy flicker for ~3 seconds ──────────────────────────────────

	private void PhaseFlicker()
	{
		_flickerTween = CreateTween().SetLoops();
		_flickerTween.TweenProperty(_label, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 0.06f);
		_flickerTween.TweenProperty(_label, "modulate", new Color(0.6f, 0.0f, 0.9f, 1.0f), 0.10f);
		_flickerTween.TweenProperty(_label, "modulate", new Color(1.0f, 0.9f, 1.0f, 0.4f), 0.05f);
		_flickerTween.TweenProperty(_label, "modulate", new Color(0.4f, 0.0f, 0.7f, 1.0f), 0.09f);
		_flickerTween.TweenProperty(_label, "modulate", new Color(0.9f, 0.0f, 0.5f, 0.8f), 0.07f);

		GetTree().CreateTimer(3.2f)
			.Connect("timeout", Callable.From(PhaseFadeOut));
	}

	// ── Phase 5: fade out and finish ────────────────────────────────────────────

	private void PhaseFadeOut()
	{
		_flickerTween?.Kill();
		_label.Modulate = new Color(0.70f, 0.00f, 0.90f, 1f);

		var t = CreateTween();
		t.TweenProperty(_overlay,       "modulate:a", 0f, 1.5f).SetTrans(Tween.TransitionType.Sine);
		t.Parallel()
		 .TweenProperty(_textContainer, "modulate:a", 0f, 1.5f).SetTrans(Tween.TransitionType.Sine);
		t.TweenCallback(Callable.From(() =>
		{
			EmitSignal(SignalName.Finished);
			QueueFree();
		}));
	}
}
