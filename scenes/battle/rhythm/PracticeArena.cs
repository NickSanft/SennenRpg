using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Standalone practice arena opened from the Bestiary.
/// Runs the enemy's attack pattern with no HP/damage consequences.
/// Shows a results screen with grade when the phase ends.
/// </summary>
public partial class PracticeArena : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static readonly float[] SpeedOptions = { 0.75f, 1.0f, 1.25f };
    private static readonly string[] SpeedLabels = { "0.75x", "1.0x", "1.25x" };
    private static readonly float[] DensityOptions = { 0.6f, 1.0f, 1.4f };
    private static readonly string[] DensityLabels = { "Light", "Normal", "Heavy" };

    private EnemyData? _enemy;
    private RhythmArena _arena = null!;
    private Label _perfectLabel = null!;
    private Label _goodLabel = null!;
    private Label _missLabel = null!;
    private Label _comboLabel = null!;
    private Label _accuracyLabel = null!;
    private VBoxContainer _statsVbox = null!;
    private Control _resultsOverlay = null!;
    private bool _phaseRunning;
    private bool _showingResults;

    private float _speedMult = 1.0f;
    private float _densityMult = 1.0f;
    private int _speedIndex = 1;   // default Normal
    private int _densityIndex = 1; // default Normal
    private readonly Button[] _speedButtons = new Button[3];
    private readonly Button[] _densityButtons = new Button[3];

    private bool _ghostMode;
    private bool _ranAsGhost;
    private Button _ghostButton = null!;

    public override void _Ready()
    {
        Layer = 52;
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// Open the practice arena for the given enemy.
    /// </summary>
    public void Open(EnemyData enemy)
    {
        _enemy = enemy;
        _phaseRunning = false;
        _showingResults = false;
        BuildUi();
        UiTheme.ApplyPixelFontToAll(this);
        UiTheme.ApplyToAllButtons(this);
        Visible = true;
    }

    private void BuildUi()
    {
        // Clean up previous UI
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();

        // Dim overlay
        var overlay = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.85f),
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        AddChild(overlay);

        // Main layout container
        var centerer = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(centerer);

        var mainVbox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(500f, 0f),
        };
        mainVbox.AddThemeConstantOverride("separation", 8);
        centerer.AddChild(mainVbox);

        // Title: Enemy name + PRACTICE MODE
        var titleRow = new VBoxContainer();
        titleRow.AddThemeConstantOverride("separation", 2);
        mainVbox.AddChild(titleRow);

        var enemyName = new Label
        {
            Text = _enemy?.DisplayName ?? "???",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = UiTheme.Gold,
        };
        enemyName.AddThemeFontSizeOverride("font_size", 16);
        titleRow.AddChild(enemyName);

        var modeLabel = new Label
        {
            Text = "— PRACTICE MODE —",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = UiTheme.SubtleGrey,
        };
        modeLabel.AddThemeFontSizeOverride("font_size", 11);
        titleRow.AddChild(modeLabel);

        // Speed / Density options row
        var optionsRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        optionsRow.AddThemeConstantOverride("separation", 20);
        mainVbox.AddChild(optionsRow);

        // Speed group
        var speedGroup = new HBoxContainer();
        speedGroup.AddThemeConstantOverride("separation", 4);
        optionsRow.AddChild(speedGroup);

        var speedLbl = new Label
        {
            Text = "SPEED:",
            Modulate = UiTheme.SubtleGrey,
        };
        speedLbl.AddThemeFontSizeOverride("font_size", 10);
        speedGroup.AddChild(speedLbl);

        for (int i = 0; i < SpeedOptions.Length; i++)
        {
            int idx = i; // capture for lambda
            var btn = new Button
            {
                Text = SpeedLabels[i],
                CustomMinimumSize = new Vector2(60f, 24f),
                FocusMode = Control.FocusModeEnum.None,
            };
            btn.AddThemeFontSizeOverride("font_size", 10);
            btn.Pressed += () => SelectSpeed(idx);
            speedGroup.AddChild(btn);
            _speedButtons[i] = btn;
        }

        // Density group
        var densityGroup = new HBoxContainer();
        densityGroup.AddThemeConstantOverride("separation", 4);
        optionsRow.AddChild(densityGroup);

        var densityLbl = new Label
        {
            Text = "NOTES:",
            Modulate = UiTheme.SubtleGrey,
        };
        densityLbl.AddThemeFontSizeOverride("font_size", 10);
        densityGroup.AddChild(densityLbl);

        for (int i = 0; i < DensityOptions.Length; i++)
        {
            int idx = i;
            var btn = new Button
            {
                Text = DensityLabels[i],
                CustomMinimumSize = new Vector2(60f, 24f),
                FocusMode = Control.FocusModeEnum.None,
            };
            btn.AddThemeFontSizeOverride("font_size", 10);
            btn.Pressed += () => SelectDensity(idx);
            densityGroup.AddChild(btn);
            _densityButtons[i] = btn;
        }

        UpdateOptionButtonHighlights();

        // Ghost mode toggle row
        var ghostRow = new VBoxContainer();
        ghostRow.AddThemeConstantOverride("separation", 2);
        mainVbox.AddChild(ghostRow);

        _ghostButton = new Button
        {
            Text = "GHOST: OFF",
            Name = "GhostButton",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(140f, 26f),
            FocusMode = Control.FocusModeEnum.None,
        };
        _ghostButton.AddThemeFontSizeOverride("font_size", 10);
        _ghostButton.Pressed += OnGhostTogglePressed;
        ghostRow.AddChild(_ghostButton);

        var ghostHint = new Label
        {
            Text = "Watch pattern only — no scoring saved",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = UiTheme.SubtleGrey,
        };
        ghostHint.AddThemeFontSizeOverride("font_size", 9);
        ghostRow.AddChild(ghostHint);

        UpdateGhostButtonVisual();

        // START button — lets the player adjust options before beginning
        var startBtn = new Button
        {
            Text = "START",
            Name = "StartButton",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(140f, 36f),
        };
        startBtn.AddThemeFontSizeOverride("font_size", 14);
        UiTheme.ApplyButtonTheme(startBtn);
        startBtn.Pressed += OnStartPressed;
        mainVbox.AddChild(startBtn);

        // Arena container (centred)
        var arenaHolder = new CenterContainer
        {
            CustomMinimumSize = new Vector2(0f, 180f),
        };
        mainVbox.AddChild(arenaHolder);

        // The arena is a Node2D — add as a direct child positioned at the centre.
        _arena = new RhythmArena();
        _arena.Position = new Vector2(0f, 0f);
        arenaHolder.AddChild(_arena);

        // Connect signals — ignore damage (practice mode)
        _arena.PhaseEnded += OnPhaseEnded;
        _arena.NoteHit += OnNoteHit;
        // PlayerHurt is intentionally NOT connected — no damage in practice

        // Stats HUD
        var statsPanel = new PanelContainer();
        UiTheme.ApplyPanelTheme(statsPanel);
        mainVbox.AddChild(statsPanel);

        var statsMargin = new MarginContainer();
        statsMargin.AddThemeConstantOverride("margin_left", 12);
        statsMargin.AddThemeConstantOverride("margin_right", 12);
        statsMargin.AddThemeConstantOverride("margin_top", 6);
        statsMargin.AddThemeConstantOverride("margin_bottom", 6);
        statsPanel.AddChild(statsMargin);

        _statsVbox = new VBoxContainer();
        _statsVbox.AddThemeConstantOverride("separation", 2);
        statsMargin.AddChild(_statsVbox);

        var countersRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        countersRow.AddThemeConstantOverride("separation", 16);
        _statsVbox.AddChild(countersRow);

        _perfectLabel = MakeStatLabel("PERFECT: 0", new Color(1f, 0.85f, 0.1f));
        countersRow.AddChild(_perfectLabel);
        _goodLabel = MakeStatLabel("GOOD: 0", Colors.White);
        countersRow.AddChild(_goodLabel);
        _missLabel = MakeStatLabel("MISS: 0", Colors.Red);
        countersRow.AddChild(_missLabel);

        var row2 = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row2.AddThemeConstantOverride("separation", 16);
        _statsVbox.AddChild(row2);

        _comboLabel = MakeStatLabel("COMBO: 0", UiTheme.MpBlue);
        row2.AddChild(_comboLabel);
        _accuracyLabel = MakeStatLabel("ACCURACY: 100%", UiTheme.HaveGreen);
        row2.AddChild(_accuracyLabel);

        // Hint
        var hint = new Label
        {
            Text = "[Esc] Quit Practice",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = UiTheme.SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 10);
        mainVbox.AddChild(hint);
    }

    private static Label MakeStatLabel(string text, Color color)
    {
        var lbl = new Label
        {
            Text = text,
            Modulate = color,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        lbl.AddThemeFontSizeOverride("font_size", 11);
        return lbl;
    }

    private void OnStartPressed()
    {
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);

        // Hide the START button
        var startBtn = FindChild("StartButton", recursive: true) as Button;
        if (startBtn != null) startBtn.Visible = false;

        StartPracticePhase();
    }

    private void StartPracticePhase()
    {
        if (!Visible || _enemy == null) return;

        // Disable option buttons once the phase begins
        SetOptionButtonsDisabled(true);

        // Start the rhythm clock with the selected speed multiplier
        float bpm = _enemy.BattleBpm > 0f ? _enemy.BattleBpm : 120f;
        RhythmClock.Instance.StartFreeRunning(bpm * _speedMult);

        // Capture ghost mode at start so later toggle changes can't affect results
        _ranAsGhost = _ghostMode;

        _phaseRunning = true;
        _arena.ObstacleDensityMult = _densityMult;
        _arena.GhostMode = _ranAsGhost;
        _arena.Visible = true;
        _arena.StartPhase(_enemy.AttackPatternScene, totalMeasures: 4, leadInBeats: 2);
    }

    private void OnGhostTogglePressed()
    {
        _ghostMode = !_ghostMode;
        AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
        UpdateGhostButtonVisual();
    }

    private void UpdateGhostButtonVisual()
    {
        if (_ghostButton == null) return;
        if (_ghostMode)
        {
            _ghostButton.Text = "GHOST: ON";
            _ghostButton.Modulate = UiTheme.Gold;
        }
        else
        {
            _ghostButton.Text = "GHOST: OFF";
            _ghostButton.Modulate = UiTheme.SubtleGrey;
        }
    }

    private void OnNoteHit(int grade)
    {
        UpdateStatsDisplay();
    }

    private void OnPhaseEnded()
    {
        _phaseRunning = false;
        UpdateStatsDisplay();
        ShowResults();
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        if (_phaseRunning)
            UpdateStatsDisplay();
    }

    private void UpdateStatsDisplay()
    {
        int p = _arena.TotalPerfects;
        int g = _arena.TotalGoods;
        int m = _arena.TotalMisses;

        _perfectLabel.Text = $"PERFECT: {p}";
        _goodLabel.Text = $"GOOD: {g}";
        _missLabel.Text = $"MISS: {m}";
        _comboLabel.Text = $"BEST COMBO: {_arena.MaxStreak}";

        float acc = BestiaryPracticeLogic.Accuracy(p, g, m);
        _accuracyLabel.Text = $"ACCURACY: {acc:F0}%";
    }

    private void ShowResults()
    {
        _showingResults = true;

        int p = _arena.TotalPerfects;
        int g = _arena.TotalGoods;
        int m = _arena.TotalMisses;
        var rank = BestiaryPracticeLogic.GradeRun(p, g, m);
        float perfectRate = BestiaryPracticeLogic.PerfectRate(p, g, m);
        float accuracy = BestiaryPracticeLogic.Accuracy(p, g, m);

        // Check if this is a new personal best before persisting
        bool isNewBest = true;
        string? prevBest = null;
        if (!_ranAsGhost)
        {
            if (GameManager.Instance.PracticeBestRanks.TryGetValue(_enemy!.EnemyId, out prevBest))
                isNewBest = string.CompareOrdinal(rank.ToString(), prevBest) < 0;
            // Persist best rank (only for non-ghost runs)
            GameManager.Instance.RecordPracticeRank(_enemy.EnemyId, rank.ToString());
        }
        else
        {
            isNewBest = false;
        }

        // Results overlay
        _resultsOverlay = new Control { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(_resultsOverlay);

        var resultCenter = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        _resultsOverlay.AddChild(resultCenter);

        var resultPanel = new PanelContainer { CustomMinimumSize = new Vector2(350f, 0f) };
        UiTheme.ApplyPanelTheme(resultPanel);
        resultCenter.AddChild(resultPanel);

        var resultMargin = new MarginContainer();
        resultMargin.AddThemeConstantOverride("margin_left", 16);
        resultMargin.AddThemeConstantOverride("margin_right", 16);
        resultMargin.AddThemeConstantOverride("margin_top", 12);
        resultMargin.AddThemeConstantOverride("margin_bottom", 12);
        resultPanel.AddChild(resultMargin);

        var resultVbox = new VBoxContainer();
        resultVbox.AddThemeConstantOverride("separation", 6);
        resultMargin.AddChild(resultVbox);

        var resultTitle = new Label
        {
            Text = "PRACTICE COMPLETE",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = UiTheme.Gold,
        };
        resultTitle.AddThemeFontSizeOverride("font_size", 16);
        resultVbox.AddChild(resultTitle);

        resultVbox.AddChild(new HSeparator());

        // Grade (big, centred) — ghost mode shows unscored placeholder instead
        if (_ranAsGhost)
        {
            var ghostLbl = new Label
            {
                Text = "GHOST MODE",
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate = UiTheme.Gold,
            };
            ghostLbl.AddThemeFontSizeOverride("font_size", 22);
            resultVbox.AddChild(ghostLbl);

            var unscoredLbl = new Label
            {
                Text = "Not scored",
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate = UiTheme.SubtleGrey,
            };
            unscoredLbl.AddThemeFontSizeOverride("font_size", 12);
            resultVbox.AddChild(unscoredLbl);
        }
        else
        {
            Color gradeColor = rank switch
            {
                BestiaryPracticeLogic.PracticeRank.S => new Color(1f, 0.95f, 0.2f),
                BestiaryPracticeLogic.PracticeRank.A => new Color(0.3f, 1f, 0.4f),
                BestiaryPracticeLogic.PracticeRank.B => new Color(0.4f, 0.7f, 1f),
                BestiaryPracticeLogic.PracticeRank.C => Colors.White,
                _ => new Color(0.7f, 0.7f, 0.7f),
            };
            var gradeLbl = new Label
            {
                Text = $"RANK: {rank}",
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate = gradeColor,
            };
            gradeLbl.AddThemeFontSizeOverride("font_size", 22);
            resultVbox.AddChild(gradeLbl);

            // "NEW BEST!" indicator when the player improved their rank
            if (isNewBest && prevBest != null)
            {
                var newBestLbl = new Label
                {
                    Text = "NEW BEST!",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Modulate = UiTheme.Gold,
                };
                newBestLbl.AddThemeFontSizeOverride("font_size", 14);
                resultVbox.AddChild(newBestLbl);
            }
        }

        // Stats
        AddResultLine(resultVbox, $"Perfect: {p}    Good: {g}    Miss: {m}");
        AddResultLine(resultVbox, $"Perfect Rate: {perfectRate:F1}%");
        AddResultLine(resultVbox, $"Accuracy: {accuracy:F1}%");
        AddResultLine(resultVbox, $"Best Combo: {_arena.MaxStreak}");
        AddResultLine(resultVbox,
            $"Speed: {SpeedLabels[_speedIndex]}  Notes: {DensityLabels[_densityIndex]}",
            UiTheme.SubtleGrey);

        if (m == 0 && g == 0 && p > 0)
            AddResultLine(resultVbox, "* FLAWLESS RUN! *", UiTheme.Gold);

        resultVbox.AddChild(new HSeparator());

        var closeHint = new Label
        {
            Text = "Press [Esc] or [Enter] to close",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = UiTheme.SubtleGrey,
        };
        closeHint.AddThemeFontSizeOverride("font_size", 10);
        resultVbox.AddChild(closeHint);

        UiTheme.ApplyPixelFontToAll(_resultsOverlay);

        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
    }

    private static void AddResultLine(VBoxContainer parent, string text, Color? color = null)
    {
        var lbl = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = color ?? Colors.White,
        };
        lbl.AddThemeFontSizeOverride("font_size", 12);
        parent.AddChild(lbl);
    }

    private void SelectSpeed(int index)
    {
        _speedIndex = index;
        _speedMult = SpeedOptions[index];
        UpdateOptionButtonHighlights();
        AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
    }

    private void SelectDensity(int index)
    {
        _densityIndex = index;
        _densityMult = DensityOptions[index];
        UpdateOptionButtonHighlights();
        AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
    }

    private void UpdateOptionButtonHighlights()
    {
        for (int i = 0; i < _speedButtons.Length; i++)
        {
            if (_speedButtons[i] == null) continue;
            _speedButtons[i].Modulate = i == _speedIndex ? UiTheme.Gold : UiTheme.SubtleGrey;
        }
        for (int i = 0; i < _densityButtons.Length; i++)
        {
            if (_densityButtons[i] == null) continue;
            _densityButtons[i].Modulate = i == _densityIndex ? UiTheme.Gold : UiTheme.SubtleGrey;
        }
    }

    private void SetOptionButtonsDisabled(bool disabled)
    {
        foreach (var btn in _speedButtons)
            if (btn != null) btn.Disabled = disabled;
        foreach (var btn in _densityButtons)
            if (btn != null) btn.Disabled = disabled;
        if (_ghostButton != null) _ghostButton.Disabled = disabled;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;

        if (_showingResults && (e.IsActionPressed("ui_cancel") || e.IsActionPressed("ui_accept") || e.IsActionPressed("interact")))
        {
            GetViewport().SetInputAsHandled();
            ClosePractice();
            return;
        }

        if (e.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            ClosePractice();
        }
    }

    private void ClosePractice()
    {
        AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
        Visible = false;
        _phaseRunning = false;
        _showingResults = false;
        EmitSignal(SignalName.Closed);
    }
}
