using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Full-screen reward-selection overlay shown after a quest turn-in.
/// Built entirely in code — no .tscn required.
/// Layer 75 (above SaveConfirmDialog at 60, below SceneTransition at 100).
/// </summary>
public partial class QuestRewardScreen : CanvasLayer
{
	private static readonly Color BgColour    = new(0.07f, 0.07f, 0.12f, 0.97f);
	private static readonly Color GoldColour  = new(1.00f, 0.85f, 0.10f);
	private static readonly Color AccentColor = new(0.35f, 0.70f, 1.00f);

	private Label?      _questTitleLabel;
	private Label?      _expLabel;
	private VBoxContainer? _buttonBox;

	private TaskCompletionSource<int>? _tcs;

	public override void _Ready()
	{
		Layer   = 75;
		Visible = false;
		BuildUi();
	}

	private void BuildUi()
	{
		// Full-screen dim background
		var centerer = new CenterContainer
		{
			AnchorRight  = 1f,
			AnchorBottom = 1f,
		};
		AddChild(centerer);

		var panel = new PanelContainer { CustomMinimumSize = new Vector2(480f, 0f) };
		var style = new StyleBoxFlat
		{
			BgColor              = BgColour,
			BorderColor          = GoldColour,
			BorderWidthLeft      = 2,
			BorderWidthRight     = 2,
			BorderWidthTop       = 2,
			BorderWidthBottom    = 2,
			CornerRadiusTopLeft      = 6,
			CornerRadiusTopRight     = 6,
			CornerRadiusBottomLeft   = 6,
			CornerRadiusBottomRight  = 6,
		};
		panel.AddThemeStyleboxOverride("panel", style);
		centerer.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left",   24);
		margin.AddThemeConstantOverride("margin_right",  24);
		margin.AddThemeConstantOverride("margin_top",    20);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		panel.AddChild(margin);

		var outer = new VBoxContainer();
		outer.AddThemeConstantOverride("separation", 14);
		margin.AddChild(outer);

		// "QUEST COMPLETE ★" header
		var header = new Label
		{
			Text                = "QUEST COMPLETE  ★",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		header.AddThemeFontSizeOverride("font_size", 16);
		header.AddThemeColorOverride("font_color", GoldColour);
		outer.AddChild(header);

		var divider = new HSeparator();
		outer.AddChild(divider);

		// Quest title
		_questTitleLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_questTitleLabel.AddThemeFontSizeOverride("font_size", 14);
		_questTitleLabel.AddThemeColorOverride("font_color", Colors.White);
		outer.AddChild(_questTitleLabel);

		// EXP display
		_expLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_expLabel.AddThemeFontSizeOverride("font_size", 12);
		_expLabel.AddThemeColorOverride("font_color", AccentColor);
		outer.AddChild(_expLabel);

		outer.AddChild(new HSeparator());

		var chooseLabel = new Label
		{
			Text                = "Choose your reward:",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		chooseLabel.AddThemeFontSizeOverride("font_size", 12);
		chooseLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
		outer.AddChild(chooseLabel);

		// Reward buttons — populated in ShowRewards()
		_buttonBox = new VBoxContainer();
		_buttonBox.AddThemeConstantOverride("separation", 8);
		outer.AddChild(_buttonBox);
	}

	/// <summary>
	/// Display the reward screen and await the player's choice.
	/// Returns the index of the chosen reward (0-based).
	/// </summary>
	public async Task<int> ShowRewards(string questTitle, int baseExp, IReadOnlyList<QuestRewardOption> rewards)
	{
		if (_questTitleLabel != null)
			_questTitleLabel.Text = questTitle;

		if (_expLabel != null)
			_expLabel.Text = $"Base EXP: {baseExp}";

		// Rebuild buttons
		if (_buttonBox != null)
		{
			foreach (Node child in _buttonBox.GetChildren())
				child.QueueFree();

			for (int i = 0; i < rewards.Count; i++)
			{
				int capturedIndex = i;
				var btn = new Button
				{
					Text = rewards[i].Label,
					CustomMinimumSize = new Vector2(0f, 36f),
				};
				btn.AddThemeFontSizeOverride("font_size", 12);
				btn.Pressed += () => Choose(capturedIndex);
				_buttonBox.AddChild(btn);

				// Focus first button
				if (i == 0)
					btn.CallDeferred(Control.MethodName.GrabFocus);
			}
		}

		_tcs    = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		Visible = true;
		return await _tcs.Task;
	}

	private void Choose(int index)
	{
		Visible = false;
		_tcs?.TrySetResult(index);
	}
}
