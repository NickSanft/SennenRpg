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
		_dialogic.Connect("signal_event", new Callable(this, MethodName.OnDialogicSignal));
	}

	/// <summary>Start a dialog timeline by its resource path.</summary>
	public void StartTimeline(string timelinePath)
	{
		if (_dialogic == null) return;
		_dialogic.Call("start", timelinePath);
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
}
