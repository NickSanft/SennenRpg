using Godot;
using System.Collections.Generic;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Hud;

namespace SennenRpg.Autoloads;

/// <summary>
/// Central dispatcher for first-time tutorial popups. Trigger sites call
/// <see cref="Trigger"/> with a tutorial ID — the manager decides (via
/// <see cref="TutorialLogic.ShouldShow"/>) whether to surface a popup,
/// and marks the ID as seen either way.
/// <para>
/// Registered as an autoload so every scene (overworld, battle, menus)
/// can reach it with a single static call — no per-scene wiring needed.
/// </para>
/// </summary>
public partial class TutorialManager : Node
{
    public static TutorialManager Instance { get; private set; } = null!;

    /// <summary>True while a popup is on-screen. Callers can gate additional
    /// triggers or defer expensive behavior while a tutorial is showing.</summary>
    public bool IsShowing { get; private set; }

    // Queue of IDs that arrived while a popup was already up — drained
    // one-by-one as each popup closes so we never stack popups.
    private readonly Queue<string> _pending = new();

    // When true, pause the scene tree while the popup is visible. Overworld
    // tutorials leave the game running; battle / rhythm tutorials pause so
    // obstacles don't rush past while the player reads.
    private bool _pauseDuringShow;

    public override void _Ready()
    {
        Instance    = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// Attempt to show the tutorial identified by <paramref name="tutorialId"/>.
    /// <para>
    /// No-op when: <see cref="SettingsData.SkipTutorials"/> is true, the ID is
    /// already seen, or the ID is not registered. In the first two cases the
    /// ID is still marked as seen so re-enabling tutorials later won't re-fire
    /// mechanics the player has already encountered.
    /// </para>
    /// </summary>
    /// <param name="tutorialId">Stable ID from <see cref="TutorialIds"/>.</param>
    /// <param name="pauseGame">If true, freeze the scene tree while the popup
    /// is visible. Use for battle / rhythm tutorials; leave false for overworld
    /// popups where the world can keep ambient-animating in the background.</param>
    public void Trigger(string tutorialId, bool pauseGame = false)
    {
        var gm = GameManager.Instance;
        var sm = SettingsManager.Instance;
        if (gm == null) return;

        bool skip = sm?.Current.SkipTutorials ?? false;
        bool shouldShow = TutorialLogic.ShouldShow(tutorialId, skip, gm.SeenTutorialIds);

        // Mark as seen AFTER the decision, not before — otherwise ShouldShow
        // finds it in the seen set and we'd never surface a popup. We still
        // mark it when skip is on so toggling skip back off later won't rewind
        // onboarding to a mechanic the player has already passed.
        if (shouldShow || skip)
            gm.MarkTutorialSeen(tutorialId);

        if (!shouldShow) return;

        // Push onto the pending queue; the show loop will drain it. The queue
        // is drained by ShowNext which will mark-seen again via show flow —
        // that's harmless because MarkTutorialSeen is idempotent (HashSet.Add).
        _pending.Enqueue(tutorialId);
        if (pauseGame) _pauseDuringShow = true;

        if (!IsShowing)
            ShowNext();
    }

    private void ShowNext()
    {
        if (_pending.Count == 0)
        {
            IsShowing = false;
            if (_pauseDuringShow)
            {
                GetTree().Paused = false;
                _pauseDuringShow = false;
            }
            return;
        }

        string id = _pending.Dequeue();
        var tutorial = TutorialLogic.Find(id);
        if (tutorial == null)
        {
            // Safety: drop unknown IDs and keep draining the queue.
            ShowNext();
            return;
        }

        IsShowing = true;
        if (_pauseDuringShow)
            GetTree().Paused = true;

        var popup = new TutorialPopup(tutorial.Value);
        popup.Closed += OnPopupClosed;
        GetTree().Root.AddChild(popup);
    }

    private void OnPopupClosed()
    {
        // Drain next popup or release the pause.
        ShowNext();
    }
}
