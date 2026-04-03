using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Menus;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Extends VendorNpc to add a "Rest (10G)" option that restores HP and MP.
/// Used by Rork in the MAPP Tavern.
/// </summary>
[Tool]
public partial class BarkeepNpc : VendorNpc
{
	private const int RestCost = 10;

	protected override string PromptText => "[Z] Barkeep";

	public override void Interact(Node player)
	{
		if (Engine.IsEditorHint()) return;
		if (DialogicBridge.Instance.IsRunning()) return;

		if (player is Node2D p2d)
			FaceToward(p2d.GlobalPosition);

		GameManager.Instance.SetState(GameState.Dialog);

		_patrolActive  = false;
		Velocity       = Vector2.Zero;
		_pendingPlayer = player;

		var menu = new NpcInteractMenu();
		GetTree().Root.AddChild(menu);
		menu.Open(_characterDescription, showShop: true, showRest: true, showChangeClass: true);
		menu.TalkSelected        += OnMenuTalkSelected;
		menu.ShopSelected        += OpenShopFromMenu;
		menu.RestSelected        += OnRestSelected;
		menu.ChangeClassSelected += OnChangeClassSelected;
		menu.Cancelled           += OnMenuCancelled;
	}

	private void OpenShopFromMenu()
	{
		// Delegate to VendorNpc's OpenShop via reflection-safe approach:
		// just call Interact flow without the menu
		string timeline = Npc.SelectTimeline(TimelinePath, AltRequiredFlags, AltTimelinePaths,
			GameManager.Instance.GetFlag);

		if (!string.IsNullOrEmpty(timeline))
		{
			DialogicBridge.Instance.ConnectTimelineEnded(
				Callable.From(() =>
				{
					if (!string.IsNullOrEmpty(NpcId))
						GameManager.Instance.SetFlag(Flags.TalkedTo(NpcId), true);
					OpenShopDirect();
				}));
			DialogicBridge.Instance.StartTimelineWithFlags(timeline);
		}
		else
		{
			OpenShopDirect();
		}
	}

	private void OpenShopDirect()
	{
		var scene = GD.Load<PackedScene>("res://scenes/menus/ShopMenu.tscn");
		var shopMenu = scene.Instantiate<Menus.ShopMenu>();
		GetTree().Root.AddChild(shopMenu);
		shopMenu.Closed += () =>
		{
			shopMenu.QueueFree();
			PlayFacingIdle(DefaultFacing);
			GameManager.Instance.SetState(GameState.Overworld);
		};
		var entries = System.Linq.Enumerable.OfType<ShopItemEntry>(ShopStock).ToArray();
		shopMenu.Open(entries);
	}

	private void OnChangeClassSelected()
	{
		var scene = GD.Load<PackedScene>("res://scenes/menus/ClassChangeMenu.tscn");
		var classMenu = scene.Instantiate<ClassChangeMenu>();
		GetTree().Root.AddChild(classMenu);
		classMenu.Closed += () =>
		{
			classMenu.QueueFree();
			PlayFacingIdle(DefaultFacing);
			GameManager.Instance.SetState(GameState.Overworld);
		};
		classMenu.Open();
	}

	private async void OnRestSelected()
	{
		var gm = GameManager.Instance;
		if (gm.Gold < RestCost)
		{
			await RunRestTimeline("res://dialog/timelines/npc_barkeep_rest_broke.dtl");
			GameManager.Instance.SetState(GameState.Overworld);
			PlayFacingIdle(DefaultFacing);
			return;
		}

		gm.RemoveGold(RestCost);
		gm.HealPlayer(gm.PlayerStats.MaxHp);
		gm.RestoreMp(gm.PlayerStats.MaxMp);

		await RunRestTimeline("res://dialog/timelines/npc_barkeep_rest.dtl");
		GameManager.Instance.SetState(GameState.Overworld);
		PlayFacingIdle(DefaultFacing);
	}

	private async System.Threading.Tasks.Task RunRestTimeline(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			GD.PushWarning($"[BarkeepNpc] Timeline not found: {path}");
			return;
		}
		var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>(
			System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
		DialogicBridge.Instance.ConnectTimelineEnded(Callable.From(() => tcs.TrySetResult(true)));
		DialogicBridge.Instance.StartTimeline(path);
		await tcs.Task;
	}
}
