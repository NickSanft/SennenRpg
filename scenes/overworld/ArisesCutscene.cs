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

	private Polygon2D        _overlay       = null!;
	private CenterContainer  _textContainer = null!;
	private Label            _label         = null!;
	private Label            _subLabel      = null!;
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

		var vbox = new VBoxContainer
		{
			Alignment   = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_textContainer.AddChild(vbox);

		_label = new Label
		{
			Text                = "SHE HAS ARISEN",
			VisibleRatio        = 0f,
			Modulate            = new Color(0.70f, 0.00f, 0.90f, 1f),
			MouseFilter         = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_label.AddThemeFontSizeOverride("font_size", 28);
		vbox.AddChild(_label);

		_subLabel = new Label
		{
			Text                = "the tavern falls silent.",
			VisibleRatio        = 0f,
			Modulate            = new Color(0.78f, 0.70f, 0.85f, 0.80f),
			MouseFilter         = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_subLabel.AddThemeFontSizeOverride("font_size", 14);
		vbox.AddChild(_subLabel);

		PhaseWhiteFlash();
	}

	// ── Phase 0: brief white flash before dark overlay ──────────────────────────

	private void PhaseWhiteFlash()
	{
		var flash = new ColorRect
		{
			Color        = new Color(1f, 1f, 1f, 0f),
			AnchorRight  = 1f,
			AnchorBottom = 1f,
			ZIndex       = 10,
		};
		AddChild(flash);

		var t = CreateTween();
		t.TweenProperty(flash, "color:a", 0.90f, 0.08f)
			.SetTrans(Tween.TransitionType.Linear);
		// Kick off overlay fade-in at the peak of the white flash
		t.TweenCallback(Callable.From(PhaseOverlayFadeIn));
		t.TweenProperty(flash, "color:a", 0f, 0.35f)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		t.TweenCallback(Callable.From(flash.QueueFree));
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

	// ── Phase 4: creepy flicker + shake + sub-label types in ────────────────────

	private void PhaseFlicker()
	{
		_flickerTween = CreateTween().SetLoops();
		_flickerTween.TweenProperty(_label, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 0.06f);
		_flickerTween.TweenProperty(_label, "modulate", new Color(0.6f, 0.0f, 0.9f, 1.0f), 0.10f);
		_flickerTween.TweenProperty(_label, "modulate", new Color(1.0f, 0.9f, 1.0f, 0.4f), 0.05f);
		_flickerTween.TweenProperty(_label, "modulate", new Color(0.4f, 0.0f, 0.7f, 1.0f), 0.09f);
		_flickerTween.TweenProperty(_label, "modulate", new Color(0.9f, 0.0f, 0.5f, 0.8f), 0.07f);

		// Sub-label types in gradually during the flicker phase
		var subTween = CreateTween();
		subTween.TweenProperty(_subLabel, "visible_ratio", 1f, 1.8f)
			.SetTrans(Tween.TransitionType.Linear).SetDelay(0.4f);

		// Screen shake via CanvasLayer.Offset — decaying oscillation
		float amp  = 3.5f;
		float step = 0.07f;
		var shake  = CreateTween();
		for (int i = 0; i < 8; i++)
		{
			float decay = 1f - (i / 8f);
			float dx    = (i % 2 == 0 ? amp : -amp) * decay;
			float dy    = (i % 3 == 0 ? amp : -amp) * decay * 0.5f;
			shake.TweenProperty(this, "offset", new Vector2(dx, dy), step)
				.SetTrans(Tween.TransitionType.Sine);
		}
		shake.TweenProperty(this, "offset", Vector2.Zero, step)
			.SetTrans(Tween.TransitionType.Sine);

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
