using Godot;

namespace SennenRpg.Autoloads;

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

	private Node _dialogic = null!;
	private float _safetyTimer;

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
	}

	/// <summary>
	/// Safety net: if GameState is Dialog but Dialogic is not running (e.g. a timeline
	/// finished without emitting timeline_ended), reset state after 5 seconds.
	/// </summary>
	public override void _Process(double delta)
	{
		if (GameManager.Instance == null) return;
		if (GameManager.Instance.CurrentState != GameState.Dialog || IsRunning())
		{
			_safetyTimer = 0f;
			return;
		}

		_safetyTimer += (float)delta;
		if (_safetyTimer >= 5.0f)
		{
			GD.PushWarning("[DialogicBridge] Safety net: GameState stuck in Dialog but Dialogic is idle — resetting to Overworld.");
			_safetyTimer = 0f;
			GameManager.Instance.SetState(GameState.Overworld);
		}
	}

	/// <summary>
	/// Syncs all GameManager flags and common variables to Dialogic, then starts the timeline.
	/// Use this instead of StartTimeline when the timeline may branch on game state.
	/// Variables that aren't defined in the Dialogic editor are silently skipped.
	/// </summary>
	public void StartTimelineWithFlags(string timelinePath)
	{
		if (_dialogic == null) return;

		var varSubsystem = _dialogic.Call("get_subsystem", "VAR").AsGodotObject();
		if (varSubsystem != null)
		{
			// Sync all GameManager flags — quietly skip any not defined in Dialogic
			foreach (var kvp in GameManager.Instance.Flags)
			{
				if (varSubsystem.Call("has", kvp.Key).AsBool())
					varSubsystem.Call("set_variable", kvp.Key, Variant.From(kvp.Value));
			}

			// Sync common numeric/string variables if they exist in Dialogic
			void TrySync(string name, Variant value)
			{
				if (varSubsystem.Call("has", name).AsBool())
					varSubsystem.Call("set_variable", name, value);
			}

			TrySync("playerName", Variant.From(GameManager.Instance.PlayerName));
			TrySync("gold",       Variant.From(GameManager.Instance.Gold));
			TrySync("love",       Variant.From(GameManager.Instance.Love));
		}

		StartTimeline(timelinePath);
	}

	/// <summary>Start a dialog timeline by its resource path.</summary>
	public void StartTimeline(string timelinePath)
	{
		if (_dialogic == null) return;
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
	/// Set a Dialogic variable. The variable must be defined in the Dialogic editor first.
	/// Call this before StartTimeline to pass data into a timeline.
	/// </summary>
	public void SetVariable(string name, Variant value)
	{
		if (_dialogic == null) return;
		var varSubsystem = _dialogic.Call("get_subsystem", "VAR").AsGodotObject();
		if (varSubsystem == null) return;

		// Only set if the variable is already defined in Dialogic
		bool exists = varSubsystem.Call("has", name).AsBool();
		if (exists)
			varSubsystem.Call("set_variable", name, value);
		else
			GD.PushWarning($"[DialogicBridge] Variable '{name}' is not defined in Dialogic. Define it in the Dialogic editor before setting it.");
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
		// Connect with ONE_SHOT so the callback fires once and disconnects automatically
		_dialogic.Connect("timeline_ended", callback, (uint)ConnectFlags.OneShot);
	}

	/// <summary>
	/// Called by Dialogic's signal_event. Argument format:
	///   "flag:my_flag_name"  → sets GameManager flag "my_flag_name" to true
	///   anything else         → just emits DialogicSignalReceived for game code to handle
	/// </summary>
	private void OnDialogicSignal(Variant argument)
	{
		string sig = argument.AsString();
		if (sig.StartsWith("flag:"))
		{
			string flagName = sig.Substring(5);
			GameManager.Instance.SetFlag(flagName, true);
			GD.Print($"[DialogicBridge] Flag set via timeline signal: '{flagName}'");
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
		GD.Print("[DialogicBridge] Timeline ended.");
	}
}
