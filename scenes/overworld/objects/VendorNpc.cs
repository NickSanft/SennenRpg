using System.Linq;
using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Menus;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// An NPC that sells items. Inherits all NPC visual setup from Npc.
/// When interacted with, optionally plays a greeting timeline first, then opens the shop.
/// Configure ShopStock in the Godot inspector — each entry is a ShopItemEntry sub-resource.
/// </summary>
[Tool]
public partial class VendorNpc : Npc
{
	/// <summary>
	/// Items this vendor sells. Typed as Resource[] so Godot's deserializer doesn't
	/// try to cast script-attached sub-resources in editor tool mode; elements are
	/// cast to ShopItemEntry at runtime when the shop is opened.
	/// </summary>
	[Export] public Resource[] ShopStock { get; set; } = [];

	private ShopMenu? _shopMenu;

	protected override string PromptText => "[Z] Shop";

	public override string GetInteractPrompt() => $"Shop at {DisplayName}";

	public override void Interact(Node player)
	{
		if (DialogicBridge.Instance.IsRunning()) return;

		if (player is Node2D p2d)
			FaceToward(p2d.GlobalPosition);

		GameManager.Instance.SetState(GameState.Dialog);

		// When a QuestGiver child is present, offer Talk and Shop as separate options
		if (GetNodeOrNull<QuestGiver>("QuestGiver") != null)
		{
			var menu = new NpcInteractMenu();
			GetTree().Root.AddChild(menu);
			menu.Open("", showShop: true);
			menu.TalkSelected += () => base.Interact(player);
			menu.ShopSelected += OpenShop;
			menu.Cancelled    += () => GameManager.Instance.SetState(GameState.Overworld);
			return;
		}

		string timeline = Npc.SelectTimeline(TimelinePath, AltRequiredFlags, AltTimelinePaths,
			GameManager.Instance.GetFlag);

		if (!string.IsNullOrEmpty(timeline))
		{
			// Play greeting timeline (flag-selected), then open shop when it ends
			DialogicBridge.Instance.ConnectTimelineEnded(
				Callable.From(OpenShop));
			DialogicBridge.Instance.StartTimelineWithFlags(timeline);
		}
		else
		{
			OpenShop();
		}
	}

	private void OpenShop()
	{
		var scene = GD.Load<PackedScene>("res://scenes/menus/ShopMenu.tscn");
		_shopMenu = scene.Instantiate<ShopMenu>();
		GetTree().Root.AddChild(_shopMenu);
		_shopMenu.Closed += OnShopClosed;
		var entries = ShopStock.OfType<ShopItemEntry>().ToArray();
		_shopMenu.Open(entries);
		GD.Print($"[VendorNpc] Shop opened for '{DisplayName}'.");
	}

	private void OnShopClosed()
	{
		_shopMenu?.QueueFree();
		_shopMenu = null;
		PlayFacingIdle(DefaultFacing);
		GameManager.Instance.SetState(GameState.Overworld);
		GD.Print($"[VendorNpc] Shop closed for '{DisplayName}'.");
	}
}
