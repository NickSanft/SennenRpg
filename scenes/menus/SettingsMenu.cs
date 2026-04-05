using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using System.Collections.Generic;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Settings overlay. Layer 51 — opened from PauseMenu.
/// All UI is built in code (same pattern as StatsMenu).
/// Emit <see cref="Closed"/> when the player closes without applying,
/// or after calling Apply — PauseMenu listens and re-shows itself.
/// </summary>
public partial class SettingsMenu : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	// ── Controls ──────────────────────────────────────────────────────────────

	// Audio
	private HSlider _masterSlider = null!;
	private HSlider _bgmSlider    = null!;
	private HSlider _sfxSlider    = null!;
	private HSlider _dialogSlider = null!;

	// Display
	private OptionButton _textSizeOption    = null!;
	private CheckButton  _highContrastCheck = null!;
	private OptionButton _colorblindOption  = null!;
	private CheckButton  _screenFlashCheck  = null!;
	private OptionButton _windowScaleOption = null!;
	private CheckButton  _vsyncCheck        = null!;
	private CheckButton  _showFpsCheck      = null!;

	// Gameplay
	private OptionButton _difficultyOption = null!;
	private OptionButton _encounterOption  = null!;
	private OptionButton _rhythmOption     = null!;
	private OptionButton _textSpeedOption  = null!;
	private CheckButton  _autoAdvanceCheck = null!;

	// Accessibility
	private CheckButton _speakerNameCheck   = null!;
	private CheckButton _dialogHistoryCheck = null!;

	// Controls tab
	private VBoxContainer _controlsRows = null!;

	// Buttons
	private Button _applyButton = null!;
	private Button _closeButton = null!;

	// ── Key rebind state ──────────────────────────────────────────────────────

	private string?                     _rebindingAction;
	private Button?                     _rebindingButton;
	private Label?                      _rebindingLabel;
	private readonly Dictionary<string, int> _pendingBindings = new();

	// ── Colours ───────────────────────────────────────────────────────────────

	private static Color Gold       => UiTheme.Gold;
	private static Color SubtleGrey => UiTheme.SubtleGrey;

	// ── Setup ─────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		Layer   = 51;
		Visible = false;
		BuildUI();
		UiTheme.ApplyToAllButtons(this);
		UiTheme.ApplyPixelFontToAll(this);
	}

	private void BuildUI()
	{
		// Full-screen dim — blocks clicks on the world behind the menu
		var overlay = new ColorRect
		{
			Color        = new Color(0f, 0f, 0f, 0.75f),
			AnchorRight  = 1f,
			AnchorBottom = 1f,
			MouseFilter  = Control.MouseFilterEnum.Stop,
		};
		AddChild(overlay);

		// CenterContainer reliably centres content without anchor arithmetic
		var centerer = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
		AddChild(centerer);

		// Panel
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(640f, 0f) };
		UiTheme.ApplyPanelTheme(panel);
		centerer.AddChild(panel);

		// Inner margin + outer VBox
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left",   16);
		margin.AddThemeConstantOverride("margin_right",  16);
		margin.AddThemeConstantOverride("margin_top",    12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		panel.AddChild(margin);

		var outer = new VBoxContainer();
		outer.AddThemeConstantOverride("separation", 8);
		margin.AddChild(outer);

		// Title
		var title = new Label
		{
			Text                = "SETTINGS",
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate            = Gold,
		};
		title.AddThemeFontSizeOverride("font_size", 18);
		outer.AddChild(title);

		outer.AddChild(new HSeparator());

		// Tab container — holds the five setting categories
		var tabs = new TabContainer();
		tabs.CustomMinimumSize = new Vector2(0f, 300f);
		tabs.SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		var pixelFont = UiTheme.LoadPixelFont();
		if (pixelFont != null)
		{
			tabs.AddThemeFontOverride("font", pixelFont);
			tabs.AddThemeFontSizeOverride("font_size", 12);
		}
		outer.AddChild(tabs);

		tabs.AddChild(BuildAudioTab());
		tabs.AddChild(BuildDisplayTab());
		tabs.AddChild(BuildGameplayTab());
		tabs.AddChild(BuildAccessibilityTab());
		tabs.AddChild(BuildControlsTab());

		outer.AddChild(new HSeparator());

		// Apply / Close buttons
		var buttonRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		buttonRow.AddThemeConstantOverride("separation", 12);
		outer.AddChild(buttonRow);

		_applyButton = new Button { Text = "APPLY", CustomMinimumSize = new Vector2(100f, 0f) };
		_applyButton.Pressed += OnApplyPressed;
		_applyButton.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
		buttonRow.AddChild(_applyButton);

		_closeButton = new Button { Text = "CLOSE", CustomMinimumSize = new Vector2(100f, 0f) };
		_closeButton.Pressed += OnClosePressed;
		_closeButton.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
		buttonRow.AddChild(_closeButton);
	}

	// ── Tab builders ──────────────────────────────────────────────────────────

	private VBoxContainer BuildAudioTab()
	{
		var tab = MakeTab("Audio");
		(_masterSlider, _) = AddSliderRow(tab, "Master Volume");
		(_bgmSlider,    _) = AddSliderRow(tab, "BGM Volume");
		(_sfxSlider,    _) = AddSliderRow(tab, "SFX Volume");
		(_dialogSlider, _) = AddSliderRow(tab, "Dialog Volume");
		return tab;
	}

	private VBoxContainer BuildDisplayTab()
	{
		var tab = MakeTab("Display");
		_windowScaleOption = AddOptionRow(tab, "Window Size",
			SettingsLogic.WindowScaleLabel(WindowScale.Scale1x),
			SettingsLogic.WindowScaleLabel(WindowScale.Scale2x),
			SettingsLogic.WindowScaleLabel(WindowScale.Scale3x),
			SettingsLogic.WindowScaleLabel(WindowScale.Scale4x),
			SettingsLogic.WindowScaleLabel(WindowScale.Fullscreen));
		_vsyncCheck        = AddCheckRow(tab,  "VSync");
		_textSizeOption    = AddOptionRow(tab, "Text Size",       "Small", "Medium", "Large");
		_highContrastCheck = AddCheckRow(tab,  "High Contrast Mode");
		_colorblindOption  = AddOptionRow(tab, "Colorblind Mode", "Normal", "Protanopia", "Deuteranopia", "Tritanopia");
		_screenFlashCheck  = AddCheckRow(tab,  "Screen Flash Effects");
		_showFpsCheck      = AddCheckRow(tab,  "Show FPS Counter");
		return tab;
	}

	private VBoxContainer BuildGameplayTab()
	{
		var tab = MakeTab("Gameplay");
		_difficultyOption = AddOptionRow(tab, "Battle Difficulty", "Easy",   "Normal",     "Hard");
		_encounterOption  = AddOptionRow(tab, "Encounter Rate",    "Normal", "Low",        "Off");
		_rhythmOption     = AddOptionRow(tab, "Rhythm Window",     "Tight",  "Normal",     "Forgiving", "AutoHit");
		_textSpeedOption  = AddOptionRow(tab, "Battle Text Speed", "Slow",   "Normal",     "Fast",      "Instant");
		_autoAdvanceCheck = AddCheckRow(tab,  "Auto-Advance Dialog");
		return tab;
	}

	private VBoxContainer BuildAccessibilityTab()
	{
		var tab = MakeTab("Accessibility");
		_speakerNameCheck   = AddCheckRow(tab, "Always Show Speaker Name");
		_dialogHistoryCheck = AddCheckRow(tab, "Dialog History");

		var hint = new Label
		{
			Text         = "Difficulty and Rhythm Window settings are also in the Gameplay tab.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate     = SubtleGrey,
		};
		hint.AddThemeFontSizeOverride("font_size", 16);
		tab.AddChild(hint);

		return tab;
	}

	private VBoxContainer BuildControlsTab()
	{
		var tab = MakeTab("Controls");

		var scroll = new ScrollContainer
		{
			SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
			CustomMinimumSize = new Vector2(0f, 200f),
		};
		tab.AddChild(scroll);

		_controlsRows = new VBoxContainer();
		_controlsRows.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_controlsRows.AddThemeConstantOverride("separation", 4);
		scroll.AddChild(_controlsRows);

		BuildControlsRows();
		return tab;
	}

	// ── Row factories ─────────────────────────────────────────────────────────

	private static VBoxContainer MakeTab(string name)
	{
		var tab = new VBoxContainer { Name = name };
		tab.AddThemeConstantOverride("separation", 8);
		return tab;
	}

	private static (HSlider slider, Label label) AddSliderRow(VBoxContainer parent, string labelText)
	{
		var row   = new HBoxContainer();
		var lbl   = new Label { Text = labelText, CustomMinimumSize = new Vector2(160f, 0f) };
		var slider = new HSlider
		{
			MinValue            = 0.0,
			MaxValue            = 1.0,
			Step                = 0.01,
			SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
			CustomMinimumSize   = new Vector2(200f, 0f),
		};
		row.AddChild(lbl);
		row.AddChild(slider);
		parent.AddChild(row);
		return (slider, lbl);
	}

	private static OptionButton AddOptionRow(VBoxContainer parent, string labelText, params string[] items)
	{
		var row = new HBoxContainer();
		var lbl = new Label { Text = labelText, CustomMinimumSize = new Vector2(180f, 0f) };
		var opt = new OptionButton();
		foreach (string item in items) opt.AddItem(item);
		row.AddChild(lbl);
		row.AddChild(opt);
		parent.AddChild(row);
		return opt;
	}

	private static CheckButton AddCheckRow(VBoxContainer parent, string labelText)
	{
		var row   = new HBoxContainer();
		var lbl   = new Label { Text = labelText, CustomMinimumSize = new Vector2(180f, 0f) };
		var check = new CheckButton();
		row.AddChild(lbl);
		row.AddChild(check);
		parent.AddChild(row);
		return check;
	}

	private void BuildControlsRows()
	{
		foreach (StringName action in InputMap.GetActions())
		{
			string actionStr = action.ToString();
			if (actionStr.StartsWith("ui_")) continue;
			// Skip internal Dialogic and GdUnit actions
			if (actionStr.StartsWith("dialogic_")) continue;
			if (actionStr.StartsWith("gdunit_")) continue;

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);

			var actionLabel = new Label
			{
				Text              = FormatActionName(actionStr),
				CustomMinimumSize = new Vector2(100f, 0f),
			};
			actionLabel.AddThemeFontSizeOverride("font_size", 16);

			// Keyboard binding
			var keyLabel = new Label
			{
				Text                = GetKeyboardText(actionStr),
				HorizontalAlignment = HorizontalAlignment.Center,
				CustomMinimumSize   = new Vector2(80f, 0f),
				Name                = "Key_" + actionStr,
			};
			keyLabel.AddThemeFontSizeOverride("font_size", 16);

			var rebindKeyBtn = new Button { Text = "Key" };
			rebindKeyBtn.AddThemeFontSizeOverride("font_size", 18);
			string capturedAction = actionStr;
			rebindKeyBtn.Pressed += () => StartRebind(capturedAction, keyLabel, rebindKeyBtn);
			rebindKeyBtn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);

			// Controller binding
			var padLabel = new Label
			{
				Text                = GetControllerText(actionStr),
				HorizontalAlignment = HorizontalAlignment.Center,
				CustomMinimumSize   = new Vector2(60f, 0f),
				Name                = "Pad_" + actionStr,
			};
			padLabel.AddThemeFontSizeOverride("font_size", 16);

			var rebindPadBtn = new Button { Text = "Pad" };
			rebindPadBtn.AddThemeFontSizeOverride("font_size", 18);
			rebindPadBtn.Pressed += () => StartRebind(capturedAction, padLabel, rebindPadBtn);
			rebindPadBtn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);

			row.AddChild(actionLabel);
			row.AddChild(keyLabel);
			row.AddChild(rebindKeyBtn);
			row.AddChild(padLabel);
			row.AddChild(rebindPadBtn);
			_controlsRows.AddChild(row);
		}
	}

	private static string FormatActionName(string action) => action switch
	{
		"move_up"    => "Move Up",
		"move_down"  => "Move Down",
		"move_left"  => "Move Left",
		"move_right" => "Move Right",
		"interact"   => "Interact",
		"cancel"     => "Cancel",
		"menu"       => "Menu",
		"run"        => "Run",
		"lane_0"     => "Lane 1",
		"lane_1"     => "Lane 2",
		"lane_2"     => "Lane 3",
		"lane_3"     => "Lane 4",
		"dialog_log" => "Dialog Log",
		_            => action,
	};

	private static string GetKeyboardText(string action)
	{
		var events = InputMap.ActionGetEvents(action);
		foreach (var ev in events)
		{
			if (ev is InputEventKey iek)
			{
				Key k = SettingsLogic.EffectiveKey(iek.Keycode, iek.PhysicalKeycode);
				if (k != Key.None) return OS.GetKeycodeString(k);
			}
		}
		return "—";
	}

	private static string GetControllerText(string action)
	{
		var events = InputMap.ActionGetEvents(action);
		foreach (var ev in events)
		{
			if (ev is InputEventJoypadButton jb)
				return GetJoyButtonName(jb.ButtonIndex);
		}
		return "—";
	}

	private static string GetJoyButtonName(JoyButton btn) => btn switch
	{
		JoyButton.A          => "A / Cross",
		JoyButton.B          => "B / Circle",
		JoyButton.X          => "X / Square",
		JoyButton.Y          => "Y / Triangle",
		JoyButton.Back       => "Select",
		JoyButton.Start      => "Start",
		JoyButton.LeftStick  => "L3",
		JoyButton.RightStick => "R3",
		JoyButton.LeftShoulder  => "LB",
		JoyButton.RightShoulder => "RB",
		JoyButton.DpadUp     => "D-Up",
		JoyButton.DpadDown   => "D-Down",
		JoyButton.DpadLeft   => "D-Left",
		JoyButton.DpadRight  => "D-Right",
		_                    => $"Btn {(int)btn}",
	};

	// ── Public API ────────────────────────────────────────────────────────────

	public void Open()
	{
		PopulateFromSettings(SettingsManager.Instance!.Current);
		Visible = true;
		_applyButton.CallDeferred(Button.MethodName.GrabFocus);
	}

	// ── Input ─────────────────────────────────────────────────────────────────

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible) return;

		// Capture the next key or joypad button for rebinding
		if (_rebindingAction != null)
		{
			string? displayName = null;
			int bindingCode = 0;

			if (@event is InputEventKey { Pressed: true } rebindKey)
			{
				bindingCode = (int)rebindKey.Keycode;
				displayName = OS.GetKeycodeString(rebindKey.Keycode);
			}
			else if (@event is InputEventJoypadButton { Pressed: true } joyBtn)
			{
				// Store joypad buttons as negative values to distinguish from keycodes
				bindingCode = -((int)joyBtn.ButtonIndex + 1);
				displayName = $"Pad {(int)joyBtn.ButtonIndex}";
			}

			if (displayName == null) return;

			_pendingBindings[_rebindingAction] = bindingCode;
			if (_rebindingButton != null) _rebindingButton.Text = "Rebind";
			if (_rebindingLabel  != null) _rebindingLabel.Text  = displayName;
			_rebindingAction = null;
			_rebindingButton = null;
			_rebindingLabel  = null;
			GetViewport().SetInputAsHandled();
			return;
		}

		// ESC closes without applying
		if (@event.IsActionPressed("ui_cancel"))
		{
			OnClosePressed();
			GetViewport().SetInputAsHandled();
		}
	}

	// ── Populate / collect ────────────────────────────────────────────────────

	private void PopulateFromSettings(SettingsData s)
	{
		_masterSlider.Value = s.MasterVolume;
		_bgmSlider.Value    = s.BgmVolume;
		_sfxSlider.Value    = s.SfxVolume;
		_dialogSlider.Value = s.DialogTypingVolume;

		_windowScaleOption.Selected      = (int)s.WindowScale;
		_vsyncCheck.ButtonPressed        = s.VSync;
		_showFpsCheck.ButtonPressed      = s.ShowFps;
		_textSizeOption.Selected         = (int)s.TextSize;
		_highContrastCheck.ButtonPressed = s.HighContrastMode;
		_colorblindOption.Selected       = (int)s.ColorblindMode;
		_screenFlashCheck.ButtonPressed  = s.ScreenFlashEffects;

		_difficultyOption.Selected      = (int)s.BattleDifficulty;
		_encounterOption.Selected       = (int)s.EncounterRateMode;
		_rhythmOption.Selected          = (int)s.RhythmTimingWindow;
		_textSpeedOption.Selected       = (int)s.BattleTextSpeed;
		_autoAdvanceCheck.ButtonPressed = s.AutoAdvanceDialog;

		_speakerNameCheck.ButtonPressed   = s.AlwaysShowSpeakerName;
		_dialogHistoryCheck.ButtonPressed = s.DialogHistoryEnabled;

		_pendingBindings.Clear();
		RefreshControlsRows(s);
	}

	private void RefreshControlsRows(SettingsData s)
	{
		foreach (StringName action in InputMap.GetActions())
		{
			string actionStr = action.ToString();
			if (actionStr.StartsWith("ui_")) continue;

			var keyLabel = _controlsRows.FindChild("Key_" + actionStr, true, false) as Label;
			if (keyLabel == null) continue;

			if (s.KeyBindings.TryGetValue(actionStr, out int savedCode))
			{
				keyLabel.Text = savedCode < 0 ? $"Pad {-(savedCode + 1)}" : OS.GetKeycodeString((Key)savedCode);
			}
			else
			{
				keyLabel.Text = GetCurrentKeyText(actionStr);
			}
		}
	}

	private SettingsData BuildSettings()
	{
		// Merge saved bindings with any pending changes
		var bindings = new Dictionary<string, int>(SettingsManager.Instance!.Current.KeyBindings);
		foreach ((string a, int k) in _pendingBindings)
			bindings[a] = k;

		return new SettingsData
		{
			MasterVolume       = (float)_masterSlider.Value,
			BgmVolume          = (float)_bgmSlider.Value,
			SfxVolume          = (float)_sfxSlider.Value,
			DialogTypingVolume = (float)_dialogSlider.Value,

			WindowScale        = (WindowScale)_windowScaleOption.Selected,
			VSync              = _vsyncCheck.ButtonPressed,
			ShowFps            = _showFpsCheck.ButtonPressed,
			TextSize           = (TextSize)_textSizeOption.Selected,
			HighContrastMode   = _highContrastCheck.ButtonPressed,
			ColorblindMode     = (ColorblindMode)_colorblindOption.Selected,
			ScreenFlashEffects = _screenFlashCheck.ButtonPressed,

			BattleDifficulty   = (BattleDifficulty)_difficultyOption.Selected,
			EncounterRateMode  = (EncounterRateMode)_encounterOption.Selected,
			RhythmTimingWindow = (RhythmTimingWindow)_rhythmOption.Selected,
			BattleTextSpeed    = (BattleTextSpeed)_textSpeedOption.Selected,
			AutoAdvanceDialog  = _autoAdvanceCheck.ButtonPressed,

			AlwaysShowSpeakerName = _speakerNameCheck.ButtonPressed,
			DialogHistoryEnabled  = _dialogHistoryCheck.ButtonPressed,

			KeyBindings = bindings,
		};
	}

	// ── Button handlers ───────────────────────────────────────────────────────

	private void OnApplyPressed()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
		SettingsManager.Instance!.Apply(BuildSettings());
		GD.Print("[SettingsMenu] Settings applied.");
	}

	private void OnClosePressed()
	{
		AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
		_rebindingAction = null;
		_rebindingButton = null;
		_rebindingLabel  = null;
		Visible = false;
		EmitSignal(SignalName.Closed);
	}

	private void StartRebind(string action, Label keyLabel, Button rebindBtn)
	{
		// Cancel any in-progress rebind first
		if (_rebindingButton != null) _rebindingButton.Text = "Rebind";
		if (_rebindingLabel  != null) _rebindingLabel.Text  = GetCurrentKeyText(action);

		_rebindingAction = action;
		_rebindingButton = rebindBtn;
		_rebindingLabel  = keyLabel;
		rebindBtn.Text   = "Press a key...";
		keyLabel.Text    = "...";
	}

	private string GetCurrentKeyText(string action)
	{
		if (_pendingBindings.TryGetValue(action, out int pending))
			return pending < 0 ? $"Pad {-(pending + 1)}" : OS.GetKeycodeString((Key)pending);

		var events = InputMap.ActionGetEvents(action);
		foreach (var ev in events)
		{
			if (ev is InputEventKey iek)
			{
				Key k = SettingsLogic.EffectiveKey(iek.Keycode, iek.PhysicalKeycode);
				if (k != Key.None) return OS.GetKeycodeString(k);
			}
			else if (ev is InputEventJoypadButton jb)
			{
				return $"Pad {jb.ButtonIndex}";
			}
		}
		return "—";
	}
}
