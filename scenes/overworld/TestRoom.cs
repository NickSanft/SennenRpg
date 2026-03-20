using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// The starting room. Registers spawn points so MapExits from other rooms
/// land in the right place. DefaultSpawnPosition is used on fresh game start.
/// </summary>
public partial class TestRoom : OverworldBase
{
	private const string NpcScene      = "res://scenes/overworld/objects/npc.tscn";
	private const string VendorScene   = "res://scenes/overworld/objects/VendorNpc.tscn";
	private const string ForanTimeline = "res://dialog/timelines/npc_foran.dtl";

	public override void _Ready()
	{
		MapId = "test_room";

		// Player arrives here after walking back from Room2
		SpawnPoints["from_room2"] = new Vector2(240, 120);

		// Save point spawn — player loads here after saving in this room
		SpawnPoints["test_room"] = new Vector2(200, 100);

		DefaultSpawnPosition = new Vector2(100, 100);

		base._Ready();

		SpawnNpcs();
	}

	private void SpawnNpcs()
	{
		var npcScene = GD.Load<PackedScene>(NpcScene);

		// ── Foran ─────────────────────────────────────────────────────
		var foran = npcScene.Instantiate<Npc>();
		foran.NpcId        = "foran_testroom";
		foran.DisplayName  = "Foran";
		foran.TimelinePath = ForanTimeline;
		YSort.AddChild(foran);
		foran.GlobalPosition = new Vector2(32, 0);

		// ── Merchant ───────────────────────────────────────────────────
		// Positioned to the right of Foran, facing left toward the player.
		var vendorScene = GD.Load<PackedScene>(VendorScene);
		var merchant    = vendorScene.Instantiate<VendorNpc>();
		merchant.NpcId        = "merchant_testroom";
		merchant.DisplayName  = "Merchant";
		merchant.DefaultFacing = FacingDirection.Side;
		merchant.ShopStock    =
		[
			new ShopItemEntry { ItemDataPath = "res://resources/items/item_001.tres", Price = 8  },
			new ShopItemEntry { ItemDataPath = "res://resources/items/item_002.tres", Price = 20 },
			new ShopItemEntry { ItemDataPath = "res://resources/items/item_003.tres", Price = 35 },
		];
		YSort.AddChild(merchant);
		merchant.GlobalPosition = new Vector2(80, 0);
	}
}
