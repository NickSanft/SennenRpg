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
public partial class VendorNpc : Npc
{
	/// <summary>Items this vendor sells. Each entry links an ItemData path and a gold price.</summary>
	[Export] public ShopItemEntry[] ShopStock { get; set; } = [];

	private ShopMenu? _shopMenu;

	protected override string PromptText => "[Z] Shop";

	public override string GetInteractPrompt() => $"Shop at {DisplayName}";

	public override void Interact(Node player)
	{
		if (DialogicBridge.Instance.IsRunning()) return;

		if (player is Node2D p2d)
			FaceToward(p2d.GlobalPosition);

		// Use Dialog state to block player movement during the shop interaction
		GameManager.Instance.SetState(GameState.Dialog);

		if (!string.IsNullOrEmpty(TimelinePath))
		{
			// Play greeting timeline, then open shop when it ends
			DialogicBridge.Instance.ConnectTimelineEnded(
				Callable.From(OpenShop));
			DialogicBridge.Instance.StartTimelineWithFlags(TimelinePath);
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
		_shopMenu.Open(ShopStock);
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
