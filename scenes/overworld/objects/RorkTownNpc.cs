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
		_menu?.QueueFree();
		_menu = null;
		PlayFacingIdle(DefaultFacing);
		GameManager.Instance.SetState(GameState.Overworld);
	}
}
