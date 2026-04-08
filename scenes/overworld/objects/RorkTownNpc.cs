using System.Linq;
using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Menus;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Rork's NPC script for Mellyr Outpost. On first interaction plays a Dialogic timeline,
/// then opens the ResidencyShopMenu. On subsequent interactions routes to the "_again" timeline
/// (via AutoRevisit logic), then opens the menu.
/// Assign ResidencyStock in the inspector with NpcResidencyEntry sub-resources.
/// </summary>
[Tool]
public partial class RorkTownNpc : Npc
{
	/// <summary>
	/// Purchasable resident entries shown in the residency shop.
	/// Typed as Resource[] so Godot's tool-mode deserialiser doesn't cast-fail on sub-resources;
	/// elements are cast to NpcResidencyEntry at runtime.
	/// </summary>
	[Export] public Resource[] ResidencyStock { get; set; } = [];

	private ResidencyShopMenu? _menu;
	// Queue of post-menu join cutscenes drained one at a time after the menu closes.
	private System.Collections.Generic.Queue<string> _pendingJoinTimelines = new();

	protected override string PromptText => "[Z] Residents";

	public override void Interact(Node player)
	{
		if (DialogicBridge.Instance.IsRunning()) return;

		if (player is Node2D p2d)
			FaceToward(p2d.GlobalPosition);

		GameManager.Instance.SetState(GameState.Dialog);

		string timeline = Npc.SelectTimeline(TimelinePath, AltRequiredFlags, AltTimelinePaths,
			GameManager.Instance.GetFlag);

		// AutoRevisit: route to _again variant after first talk
		if (timeline == TimelinePath && !string.IsNullOrEmpty(NpcId))
		{
			bool talked = GameManager.Instance.GetFlag(Flags.TalkedTo(NpcId));
			string revisit = NpcLogic.GetRevisitPath(TimelinePath, talked);
			if (revisit != TimelinePath && ResourceLoader.Exists(revisit))
				timeline = revisit;
		}

		if (!string.IsNullOrEmpty(timeline))
		{
			DialogicBridge.Instance.ConnectTimelineEnded(Callable.From(OnTalkEnded));
			DialogicBridge.Instance.StartTimelineWithFlags(timeline);
		}
		else
		{
			OpenMenu();
		}
	}

	private void OnTalkEnded()
	{
		if (!string.IsNullOrEmpty(NpcId))
			GameManager.Instance.SetFlag(Flags.TalkedTo(NpcId), true);
		OpenMenu();
	}

	private void OpenMenu()
	{
		var scene = GD.Load<PackedScene>("res://scenes/menus/ResidencyShopMenu.tscn");
		_menu = scene.Instantiate<ResidencyShopMenu>();
		GetTree().Root.AddChild(_menu);
		_menu.Closed += OnMenuClosed;

		var entries = ResidencyStock.OfType<NpcResidencyEntry>().ToArray();
		_menu.Open(entries);

		GD.Print($"[RorkTownNpc] Residency menu opened ({entries.Length} entries).");
	}

	private void OnMenuClosed()
	{
		// Snapshot the menu's pending timeline queue (the menu queue-frees right after).
		_pendingJoinTimelines.Clear();
		if (_menu != null)
		{
			foreach (var path in _menu.PendingJoinTimelines)
				_pendingJoinTimelines.Enqueue(path);
		}
		_menu?.QueueFree();
		_menu = null;
		PlayFacingIdle(DefaultFacing);

		PlayNextJoinTimeline();
	}

	/// <summary>
	/// Drain one timeline from <see cref="_pendingJoinTimelines"/> and play it. When the
	/// timeline ends, recurse to play the next entry. Once the queue is empty we return
	/// the game to the Overworld state. Lets hiring multiple party members in one menu
	/// session play every join cutscene back-to-back.
	/// </summary>
	private void PlayNextJoinTimeline()
	{
		while (_pendingJoinTimelines.Count > 0)
		{
			string path = _pendingJoinTimelines.Dequeue();
			if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path)) continue;

			GameManager.Instance.SetState(GameState.Dialog);
			DialogicBridge.Instance.ConnectTimelineEnded(Callable.From(OnJoinTimelineEnded));
			DialogicBridge.Instance.StartTimelineWithFlags(path);
			return;
		}

		// Queue exhausted — hand control back to the player.
		GameManager.Instance.SetState(GameState.Overworld);
	}

	private void OnJoinTimelineEnded()
	{
		PlayNextJoinTimeline();
	}
}
