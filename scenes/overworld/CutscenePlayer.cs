using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Executes a sequence of CutsceneStep actions: letterbox bars, camera pans,
/// NPC walking, dialog, name cards, etc. Add as a child of any map scene.
/// </summary>
public partial class CutscenePlayer : Node
{
    [Signal] public delegate void CutsceneFinishedEventHandler();

    private const float BarHeight = 40f;

    private ColorRect? _topBar;
    private ColorRect? _bottomBar;
    private Label?     _nameCard;
    private CanvasLayer? _uiLayer;

    /// <summary>Play a cutscene defined as a list of steps.</summary>
    public async Task Play(List<CutsceneStep> steps)
    {
        GameManager.Instance.SetState(GameState.Dialog);

        foreach (var step in steps)
            await ExecuteStep(step);

        GameManager.Instance.SetState(GameState.Overworld);
        EmitSignal(SignalName.CutsceneFinished);
    }

    private async Task ExecuteStep(CutsceneStep step)
    {
        switch (step.Action)
        {
            case CutsceneActionType.ShowLetterbox:
                await ShowLetterbox(step.FloatArg);
                break;
            case CutsceneActionType.HideLetterbox:
                await HideLetterbox(step.FloatArg);
                break;
            case CutsceneActionType.PanCamera:
                await PanCamera(new Vector2(step.X, step.Y), step.FloatArg);
                break;
            case CutsceneActionType.WalkNpc:
                await WalkNpc(step.StringArg, new Vector2(step.X, step.Y), step.FloatArg);
                break;
            case CutsceneActionType.FaceNpc:
                FaceNpc(step.StringArg, step.StringArg2);
                break;
            case CutsceneActionType.StartDialog:
                StartDialog(step.StringArg);
                break;
            case CutsceneActionType.WaitForDialog:
                await WaitForDialog();
                break;
            case CutsceneActionType.ShowNameCard:
                await ShowNameCard(step.StringArg, step.FloatArg);
                break;
            case CutsceneActionType.HideNameCard:
                await HideNameCardAction(step.FloatArg);
                break;
            case CutsceneActionType.Wait:
                await ToSignal(GetTree().CreateTimer(step.FloatArg), SceneTreeTimer.SignalName.Timeout);
                break;
            case CutsceneActionType.PlaySfx:
                AudioManager.Instance?.PlaySfx(step.StringArg);
                break;
            case CutsceneActionType.SetFlag:
                GameManager.Instance.SetFlag(step.StringArg, true);
                break;
        }
    }

    // ── Letterbox bars ────────────────────────────────────────────────

    private void EnsureUI()
    {
        if (_uiLayer != null) return;
        _uiLayer = new CanvasLayer { Layer = 55 };
        AddChild(_uiLayer);

        _topBar = new ColorRect
        {
            Color = Colors.Black,
            AnchorRight = 1f,
            OffsetBottom = 0f,
        };
        _uiLayer.AddChild(_topBar);

        _bottomBar = new ColorRect
        {
            Color = Colors.Black,
            AnchorRight = 1f,
            AnchorTop = 1f,
            AnchorBottom = 1f,
            OffsetTop = 0f,
        };
        _uiLayer.AddChild(_bottomBar);
    }

    private async Task ShowLetterbox(float duration)
    {
        EnsureUI();
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(_topBar!, "offset_bottom", BarHeight, duration)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_bottomBar!, "offset_top", -BarHeight, duration)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task HideLetterbox(float duration)
    {
        if (_topBar == null) return;
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(_topBar!, "offset_bottom", 0f, duration)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_bottomBar!, "offset_top", 0f, duration)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    // ── Camera ────────────────────────────────────────────────────────

    private async Task PanCamera(Vector2 target, float duration)
    {
        var camera = GetViewport().GetCamera2D();
        if (camera == null) return;

        var tween = CreateTween();
        tween.TweenProperty(camera, "global_position", target, duration)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    // ── NPC movement ──────────────────────────────────────────────────

    private async Task WalkNpc(string npcId, Vector2 target, float speed)
    {
        var npc = FindNpcById(npcId);
        if (npc == null)
        {
            GD.PushWarning($"[CutscenePlayer] NPC not found: {npcId}");
            return;
        }

        float distance = npc.GlobalPosition.DistanceTo(target);
        float duration = distance / Mathf.Max(speed, 1f);

        var tween = CreateTween();
        tween.TweenProperty(npc, "global_position", target, duration)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void FaceNpc(string npcId, string direction)
    {
        // NPC facing is handled naturally by dialog — this is a no-op placeholder
        // for future expansion when FaceToward is made public on Npc.
        GD.Print($"[CutscenePlayer] FaceNpc {npcId} → {direction}");
    }

    private Node2D? FindNpcById(string npcId)
    {
        foreach (var node in GetTree().GetNodesInGroup("interactable"))
        {
            if (node is Npc npc && npc.NpcId == npcId)
                return npc;
        }
        return null;
    }

    // ── Dialog ────────────────────────────────────────────────────────

    private void StartDialog(string timelinePath)
    {
        if (string.IsNullOrEmpty(timelinePath)) return;
        DialogicBridge.Instance.StartTimelineWithFlags(timelinePath);
    }

    private async Task WaitForDialog()
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        DialogicBridge.Instance.ConnectTimelineEnded(Callable.From(() => tcs.TrySetResult(true)));
        _ = Task.Delay(15_000).ContinueWith(_ => tcs.TrySetResult(true)); // Safety timeout
        await tcs.Task;
    }

    // ── Name cards ────────────────────────────────────────────────────

    private async Task ShowNameCard(string text, float holdDuration)
    {
        EnsureUI();

        _nameCard?.QueueFree();
        _nameCard = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            AnchorLeft = 0.2f, AnchorRight = 0.8f,
            AnchorTop = 0.4f, AnchorBottom = 0.5f,
            Modulate = Colors.Transparent,
        };
        _nameCard.AddThemeFontSizeOverride("font_size", 16);
        _nameCard.AddThemeColorOverride("font_color", UiTheme.Gold);
        _nameCard.AddThemeConstantOverride("outline_size", 2);
        _nameCard.AddThemeColorOverride("font_outline_color", Colors.Black);

        var font = UiTheme.LoadPixelFont();
        if (font != null)
            _nameCard.AddThemeFontOverride("font", font);

        _uiLayer!.AddChild(_nameCard);

        // Fade in
        var fadeIn = CreateTween();
        fadeIn.TweenProperty(_nameCard, "modulate", Colors.White, 0.4f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(fadeIn, Tween.SignalName.Finished);

        // Hold
        await ToSignal(GetTree().CreateTimer(holdDuration), SceneTreeTimer.SignalName.Timeout);
    }

    private async Task HideNameCardAction(float fadeDuration)
    {
        if (_nameCard == null) return;
        var tween = CreateTween();
        tween.TweenProperty(_nameCard, "modulate", Colors.Transparent, fadeDuration);
        await ToSignal(tween, Tween.SignalName.Finished);
        _nameCard.QueueFree();
        _nameCard = null;
    }
}
