using Godot;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

/// <summary>Represents a single line from a dialog timeline.</summary>
public record DialogLine(string Speaker, string Text);

/// <summary>
/// C# bridge to Dialogic 2 (a GDScript plugin).
/// All dialog calls in game code must go through this class — never call Dialogic directly.
/// Dialogic registers itself as an autoload named "Dialogic" when the plugin is enabled.
/// We call its GDScript methods using Godot's cross-language reflection API (Node.Call).
/// </summary>
public partial class DialogicBridge : Node
{
	public static DialogicBridge Instance { get; private set; } = null!;

	[Signal] public delegate void DialogicSignalReceivedEventHandler(Variant argument);
	[Signal] public delegate void HistoryUpdatedEventHandler();

	private const int MaxHistoryLines = 50;
	private readonly List<DialogLine> _history = new();

	/// <summary>Read-only view of the most recent dialog lines, oldest first.</summary>
	public IReadOnlyList<DialogLine> DialogHistory => _history;

	private Node _dialogic = null!;
	private float _safetyTimer;
	private bool _dialogicOwnsDialog = false;

	/// <summary>
	/// Battle-specific variables that must always exist so {variable} interpolation works
	/// in battle .dtl files regardless of what the Dialogic editor has saved to project.godot.
	/// </summary>
	private static readonly string[] BattleVars =
	[
		"enemy_name", "damage", "hit_label", "enemy_dialog",
		"charm_result", "skill_result", "performance_summary",
		"exp_gained", "gold_gained",
		"notes_success", "notes_total", "item_name", "heal_amount",
	];

	/// <summary>
	/// Story flags that timeline conditions may branch on.
	/// Pre-initialised to false so {flag_name} in a condition always resolves,
	/// even before the flag has ever been set in GameManager.
	/// </summary>
	private static readonly string[] StoryFlagVars =
	[
		Flags.MetShizu,
		Flags.GotItemFromForan,
		Flags.SeenNorthExitHint,
	];

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		// Dialogic registers as "/root/Dialogic" when the plugin is enabled.
		_dialogic = GetNode("/root/Dialogic");

		if (_dialogic == null)
		{
			GD.PushError("[DialogicBridge] Dialogic autoload not found. Is the plugin enabled in Project Settings?");
			return;
		}

		// Wire Dialogic's signal_event to our C# handler.
		// Any Signal Event in a timeline with argument "flag:xyz" sets flag "xyz" in GameManager.
		if (_dialogic.HasSignal("signal_event"))
			_dialogic.Connect("signal_event", new Callable(this, MethodName.OnDialogicSignal));
		else
			GD.PushWarning("[DialogicBridge] 'signal_event' signal not found on Dialogic node — flag signals will not work.");

		// Sync Dialogic variables back to GameManager after every timeline ends.
		if (_dialogic.HasSignal("timeline_ended"))
			_dialogic.Connect("timeline_ended", new Callable(this, MethodName.OnTimelineEndedInternal));

		// Hook into the TEXT subsystem to capture each spoken line for the history log.
		// Uses CallDeferred so the Text subsystem is fully initialised.
		Callable.From(ConnectTextSubsystem).CallDeferred();

		// Defer variable initialisation by one frame so Dialogic's subsystems (including VAR)
		// have finished their own _Ready() before we try to call get_subsystem("VAR").
		Callable.From(InitialiseDialogicVariables).CallDeferred();
	}

	private void ConnectTextSubsystem()
	{
		var textNode = _dialogic?.Get("Text").AsGodotObject();
		if (textNode == null) return;
		if (textNode.HasSignal("text_finished"))
			textNode.Connect("text_finished", new Callable(this, MethodName.OnTextFinished));
	}

	private void OnTextFinished(Godot.Collections.Dictionary info)
	{
		string text    = info.ContainsKey("text")      ? info["text"].AsString()      : "";
		string speaker = info.ContainsKey("character") ? info["character"].AsString()  : "";

		if (string.IsNullOrWhiteSpace(text)) return;

		_history.Add(new DialogLine(speaker, text));
		if (_history.Count > MaxHistoryLines)
			_history.RemoveAt(0);

		EmitSignal(SignalName.HistoryUpdated);
	}

	/// <summary>
	/// Seeds all known variables into the Dialogic VAR subsystem so they always exist.
	/// This prevents "unknown variable" errors when the Godot editor regenerates project.godot
	/// and removes manually-added variable declarations.
	/// Called deferred (one frame after _Ready) so the VAR subsystem is available.
	/// </summary>
	private void InitialiseDialogicVariables()
	{
		foreach (var name in BattleVars)
			ForceSetVariable(name, Variant.From(""));

		foreach (var name in StoryFlagVars)
			ForceSetVariable(name, Variant.From(false));

		GD.Print($"[DialogicBridge] Initialised {BattleVars.Length + StoryFlagVars.Length} Dialogic variables.");
	}

	/// <summary>
	/// Safety net: if GameState is Dialog but Dialogic is not running (e.g. a timeline
	/// finished without emitting timeline_ended), reset state after 5 seconds.
	/// </summary>
	public override void _Process(double delta)
	{
		if (GameManager.Instance == null) return;
		if (GameManager.Instance.CurrentState != GameState.Dialog || IsRunning() || !_dialogicOwnsDialog)
		{
			_safetyTimer = 0f;
			return;
		}

		_safetyTimer += (float)delta;
		if (_safetyTimer >= 5.0f)
		{
			GD.PushWarning("[DialogicBridge] Safety net: GameState stuck in Dialog but Dialogic is idle — resetting to Overworld.");
			_safetyTimer = 0f;
			_dialogicOwnsDialog = false;
			GameManager.Instance.SetState(GameState.Overworld);
		}
	}

	/// <summary>
	/// Syncs all GameManager flags and common variables to Dialogic, then starts the timeline.
	/// Use this instead of StartTimeline when the timeline may branch on game state.
	/// Variables are created in the VAR subsystem if they don't already exist.
	/// </summary>
	public void StartTimelineWithFlags(string timelinePath)
	{
		if (_dialogic == null) return;

		// Sync all GameManager flags and common player variables directly into
		// current_state_info['variables'], creating entries that don't yet exist.
		foreach (var kvp in GameManager.Instance.Flags)
			ForceSetVariable(kvp.Key, Variant.From(kvp.Value));

		ForceSetVariable("playerName", Variant.From(GameManager.Instance.PlayerName));
		ForceSetVariable("gold",       Variant.From(GameManager.Instance.Gold));

		StartTimeline(timelinePath);
	}

	/// <summary>Start a dialog timeline by its resource path.</summary>
	public void StartTimeline(string timelinePath)
	{
		if (_dialogic == null) return;
		_dialogicOwnsDialog = true;
		GD.Print($"[DialogicBridge] Calling Dialogic.start('{timelinePath}')");
		_dialogic.Call("start", timelinePath);
		GD.Print("[DialogicBridge] Dialogic.start() returned.");

		// One frame later, confirm the timeline is actually running
		GetTree().CreateTimer(0.1).Connect("timeout",
			Callable.From(() => GD.Print($"[DialogicBridge] IsRunning 0.1s later = {IsRunning()}")));
	}

	/// <summary>Returns true if a dialog timeline is currently running.</summary>
	public bool IsRunning()
	{
		if (_dialogic == null) return false;
		// Dialogic 2 exposes current_timeline as a property — null means no dialog active
		return _dialogic.Get("current_timeline").AsGodotObject() != null;
	}

	/// <summary>
	/// Set a Dialogic variable. Variables are created dynamically at runtime if not
	/// already defined in the Dialogic editor — useful for per-battle data like
	/// enemy_name, damage, gold_gained, etc.
	/// Call this before StartTimeline to pass data into a timeline.
	/// </summary>
	public void SetVariable(string name, Variant value)
	{
		if (_dialogic == null) return;
		var varSubsystem = _dialogic.Call("get_subsystem", "VAR").AsGodotObject();
		varSubsystem?.Call("set_variable", name, value);
	}

	/// <summary>
	/// Writes a variable directly into Dialogic's current_state_info['variables'] dictionary,
	/// creating the entry if it doesn't already exist.
	/// Use this instead of SetVariable when the variable may not be pre-declared in the editor.
	/// </summary>
	private void ForceSetVariable(string name, Variant value)
	{
		if (_dialogic == null) return;
		var stateInfo = _dialogic.Get("current_state_info").AsGodotDictionary();
		if (stateInfo == null) return;
		var variables = stateInfo["variables"].AsGodotDictionary();
		if (variables == null) return;
		variables[name] = value;
	}

	/// <summary>Get a Dialogic variable. Call this after timeline_ended to read choice results.</summary>
	public Variant GetVariable(string name)
	{
		if (_dialogic == null) return default;
		var varSubsystem = _dialogic.Call("get_subsystem", "VAR").AsGodotObject();
		return varSubsystem?.Call("get_variable", name) ?? default;
	}

	/// <summary>Connect a callback to fire when the current timeline ends.</summary>
	public void ConnectTimelineEnded(Callable callback)
	{
		if (_dialogic == null) return;
		// Guard against double-connection (can happen when a previous timeline failed to
		// start and never emitted timeline_ended, leaving a stale ONE_SHOT in place).
		if (_dialogic.IsConnected("timeline_ended", callback))
			_dialogic.Disconnect("timeline_ended", callback);
		_dialogic.Connect("timeline_ended", callback, (uint)ConnectFlags.OneShot);
	}

	/// <summary>
	/// Called by Dialogic's signal_event. Dispatches based on the argument prefix:
	///   "flag:{name}"      → sets GameManager flag "{name}" to true
	///   "give_item:{path}" → adds the item at "{path}" to inventory
	///   anything else      → forwarded via DialogicSignalReceived for game code to handle
	/// See <see cref="SennenRpg.Core.Data.DialogicSignalParser"/> for the full convention.
	/// </summary>
	private void OnDialogicSignal(Variant argument)
	{
		string sig = argument.AsString();
		var (type, arg) = DialogicSignalParser.Parse(sig);

		switch (type)
		{
			case DialogicSignalParser.TypeFlag:
				GameManager.Instance.SetFlag(arg, true);
				GD.Print($"[DialogicBridge] Flag set via timeline signal: '{arg}'");
				break;

			case DialogicSignalParser.TypeGiveItem:
				GameManager.Instance.AddItem(arg);
				GD.Print($"[DialogicBridge] Item given via timeline signal: '{arg}'");
				break;

			case DialogicSignalParser.TypeRemoveGold:
				if (int.TryParse(arg, out int goldAmount))
				{
					GameManager.Instance.RemoveGold(goldAmount);
					GD.Print($"[DialogicBridge] Removed {goldAmount} gold via timeline signal.");
				}
				break;
		}

		EmitSignal(SignalName.DialogicSignalReceived, argument);
	}

	/// <summary>
	/// Runs after every timeline ends.
	/// Flag syncing is handled in real-time by OnDialogicSignal ("flag:xyz" convention),
	/// so no post-timeline variable sweep is needed.
	/// </summary>
	private void OnTimelineEndedInternal()
	{
		_dialogicOwnsDialog = false;
		GD.Print("[DialogicBridge] Timeline ended.");
	}
}
