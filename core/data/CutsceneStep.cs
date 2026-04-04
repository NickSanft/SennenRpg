using System;

namespace SennenRpg.Core.Data;

/// <summary>Action types available in the cutscene framework.</summary>
public enum CutsceneActionType
{
    ShowLetterbox,
    HideLetterbox,
    PanCamera,
    WalkNpc,
    FaceNpc,
    StartDialog,
    WaitForDialog,
    ShowNameCard,
    HideNameCard,
    Wait,
    PlaySfx,
    SetFlag,
    FadeOverlay,
}

/// <summary>
/// One step in a cutscene sequence. Interpreted by CutscenePlayer.
/// Only the fields relevant to the action type are used.
/// </summary>
public class CutsceneStep
{
    public CutsceneActionType Action { get; init; }
    public string StringArg  { get; init; } = "";
    public string StringArg2 { get; init; } = "";
    public float  FloatArg   { get; init; }
    public float  FloatArg2  { get; init; }
    public float  X          { get; init; }
    public float  Y          { get; init; }

    // ── Factory methods for readable cutscene building ────────────

    public static CutsceneStep ShowLetterbox(float duration = 0.4f)
        => new() { Action = CutsceneActionType.ShowLetterbox, FloatArg = duration };

    public static CutsceneStep HideLetterbox(float duration = 0.4f)
        => new() { Action = CutsceneActionType.HideLetterbox, FloatArg = duration };

    public static CutsceneStep PanCamera(float x, float y, float duration = 1.0f)
        => new() { Action = CutsceneActionType.PanCamera, X = x, Y = y, FloatArg = duration };

    public static CutsceneStep WalkNpc(string npcId, float x, float y, float speed = 60f)
        => new() { Action = CutsceneActionType.WalkNpc, StringArg = npcId, X = x, Y = y, FloatArg = speed };

    public static CutsceneStep FaceNpc(string npcId, string direction)
        => new() { Action = CutsceneActionType.FaceNpc, StringArg = npcId, StringArg2 = direction };

    public static CutsceneStep Dialog(string timelinePath)
        => new() { Action = CutsceneActionType.StartDialog, StringArg = timelinePath };

    public static CutsceneStep WaitDialog()
        => new() { Action = CutsceneActionType.WaitForDialog };

    public static CutsceneStep NameCard(string title, float holdDuration = 1.5f)
        => new() { Action = CutsceneActionType.ShowNameCard, StringArg = title, FloatArg = holdDuration };

    public static CutsceneStep HideNameCard(float fadeDuration = 0.5f)
        => new() { Action = CutsceneActionType.HideNameCard, FloatArg = fadeDuration };

    public static CutsceneStep Pause(float seconds)
        => new() { Action = CutsceneActionType.Wait, FloatArg = seconds };

    public static CutsceneStep Sfx(string path)
        => new() { Action = CutsceneActionType.PlaySfx, StringArg = path };

    public static CutsceneStep Flag(string flagName)
        => new() { Action = CutsceneActionType.SetFlag, StringArg = flagName };
}
