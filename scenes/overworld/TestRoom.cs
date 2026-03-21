using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// The starting room. Registers spawn points so MapExits from other rooms
/// land in the right place. DefaultSpawnPosition is used on fresh game start.
/// </summary>
public partial class TestRoom : OverworldBase
{
	private const string NpcScene          = "res://scenes/overworld/objects/npc.tscn";
	private const string VendorScene       = "res://scenes/overworld/objects/VendorNpc.tscn";
	private const string WalkInScene       = "res://scenes/overworld/objects/WalkInTrigger.tscn";
	private const string ChestScene        = "res://scenes/overworld/objects/Chest.tscn";
	private const string SignScene         = "res://scenes/overworld/objects/InteractSign.tscn";
	private const string ForanTimeline     = "res://dialog/timelines/npc_foran.dtl";
	private const string NorthExitTimeline = "res://dialog/timelines/walkin_north_exit.dtl";

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
		SpawnTriggers();
		SpawnChests();
		SpawnSigns();
	}

	private void SpawnNpcs()
	{
		var npcScene = GD.Load<PackedScene>(NpcScene);

		// ── Foran ──────────────────────────────────────────────────────────────
		// Demonstrates give_item: — gives a Potion on first meeting.
		var foran = npcScene.Instantiate<Npc>();
		foran.NpcId        = "foran_testroom";
		foran.DisplayName  = "Foran";
		foran.TimelinePath = ForanTimeline;
		YSort.AddChild(foran);
		foran.GlobalPosition = new Vector2(32, 0);

		// Foran patrols a short loop near the entrance
		foran.PatrolPoints = [new Vector2(60, 20), new Vector2(20, 40), new Vector2(32, 0)];

		// ── Merchant ───────────────────────────────────────────────────────────
		var vendorScene = GD.Load<PackedScene>(VendorScene);
		var merchant    = vendorScene.Instantiate<VendorNpc>();
		merchant.NpcId         = "merchant_testroom";
		merchant.DisplayName   = "Merchant";
		merchant.DefaultFacing = FacingDirection.Side;
		merchant.ShopStock     =
		[
			new ShopItemEntry { ItemDataPath = "res://resources/items/item_001.tres", Price = 8  },
			new ShopItemEntry { ItemDataPath = "res://resources/items/item_002.tres", Price = 20 },
			new ShopItemEntry { ItemDataPath = "res://resources/items/item_003.tres", Price = 35 },
		];
		YSort.AddChild(merchant);
		merchant.GlobalPosition = new Vector2(80, 0);
	}

	private void SpawnChests()
	{
		var chestScene = GD.Load<PackedScene>(ChestScene);

		// ── Bandage chest ───────────────────────────────────────────────────────
		// A free Bandage tucked near the western wall — rewards exploration.
		var chest = chestScene.Instantiate<Chest>();
		chest.ItemPath = "res://resources/items/item_001.tres";
		chest.FlagId   = "chest_testroom_bandage";
		AddChild(chest);
		chest.GlobalPosition = new Vector2(-60, 30);
	}

	private void SpawnSigns()
	{
		var signScene = GD.Load<PackedScene>(SignScene);

		// ── Entrance notice board ───────────────────────────────────────────────
		var sign = signScene.Instantiate<InteractSign>();
		sign.SignTitle = "Senne Village";
		sign.Lines     =
		[
			"Welcome, traveller.",
			"The merchant to the east trades in supplies.",
			"Danger lies north — prepare before you go.",
		];
		AddChild(sign);
		sign.GlobalPosition = new Vector2(-90, 80);
	}

	private void SpawnTriggers()
	{
		// ── North exit hint ────────────────────────────────────────────────────
		// Demonstrates WalkInTrigger: Foran warns the player the first time they
		// approach the northern map exit (MapExit is at y ≈ -170).
		// The trigger is positioned midway between the save point and the exit.
		var walkInScene = GD.Load<PackedScene>(WalkInScene);
		var northHint   = walkInScene.Instantiate<WalkInTrigger>();
		northHint.TimelinePath = NorthExitTimeline;
		northHint.OnceFlag     = Flags.SeenNorthExitHint;

		// Widen the collision area to span the passable corridor north of the save point.
		var shape = northHint.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (shape?.Shape is RectangleShape2D rect)
			rect.Size = new Vector2(96, 32);

		AddChild(northHint);
		northHint.GlobalPosition = new Vector2(0, -130);
	}
}
