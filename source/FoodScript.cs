using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FoodScriptCS
{
    public class FoodScript : Script
    {
        private List<int> lootedNPCs = new List<int>();
        private DateTime? pendingWantedTime = null;
        private string hashToDisplay = "";
        private DateTime hashDisplayEndTime;

        private List<GTA.Object> nearbyLootableObjects = new List<GTA.Object>();
        private Vector3 lootScanCenter;
        private bool isLootScanActive = false;
        private const float LOOT_SCAN_RADIUS = 10.0f;
        private const float LOOT_ABANDON_RADIUS = 10.0f;

        private Dictionary<string, int> inventory = new Dictionary<string, int>();
        private bool inventoryOpen = false;
        private InventoryState currentState = InventoryState.Main;

        private readonly List<string> page1_items = new List<string> { "soft_drink", "canned_soft_drink", "small_cola", "sprite_small", "coffee", "chocolate", "nuts", "packed_apple", "milk" };
        private readonly List<string> page2_items = new List<string> { "cookie", "burgershot_drink", "clucking_drink", "packed_cookie", "big_cola", "donut", "big_milk", "big_sprite", "canned_juice" };
        private readonly List<string> page3_items = new List<string> { "ice_cream", "orange_juice", "salad", "cheese", "ciabatta", "fried_potato", "chicken_nugget", "noodle", "cereal" };
        private readonly List<string> page4_items = new List<string> { "baguette", "chicken_soup", "hotdog", "pizza", "canned_meat", "burger" };

        private int valuablesPageIndex = 0;
        private int sellValuablesPageIndex = 0;
        private int sellScrapsPageIndex = 0;

        private readonly List<string> valuables_page1_items = new List<string> { "gold_tooth", "pen", "silver_earring", "silver_ring", "gold_earring", "gold_ring", "platinum_earring", "platinum_band", "silver_pocket_watch", "necklace" };
        private readonly List<string> valuables_page2_items = new List<string> { "gold_pocket_watch", "platinum_chain_necklace", "pearl_necklace", "small_jewelry_bag", "gold_nugget", "large_jewelry_bag", "gold_bar", "old_mobile_phone", "modern_mobile_phone", "mp3_player" };
        private readonly List<string> valuables_page3_items = new List<string> { "walkie_talkie", "camera", "metal_scrap", "scrap" };

        private string currentMessage = "";
        private DateTime messageStartTime = DateTime.Now;
        private int messageDuration = 0;
        private string currentMenuText = "";

        private const string DATA_FILE1 = "scripts\\FoodScriptDataGta4.ini";
        private const string DATA_FILE2 = "scripts\\FoodScriptDataTLAD.ini";
        private const string DATA_FILE3 = "scripts\\FoodScriptDataTbogt.ini";
        private const string DATA_FILE = "scripts\\FoodScriptData.ini";

        private Dictionary<string, ItemInfo> itemDatabase = new Dictionary<string, ItemInfo>();

        private List<GTA.Object> spawnedItems = new List<GTA.Object>();
        private Random random = new Random();
        private int savedWeapon = 0;
        private int healthCore = 100;
        private int lastGameHour = -1;
        private int lastGameMinute = -1;
        private int gameMinutesElapsed = 0;
        private int lastHealthRegenGameHour = -1;
        private int lastHealthRegenGameMinute = -1;
        private int healthRegenGameMinutesElapsed = 0;
        private bool wasPlayerDead = false;
        private bool isUsingItem = false;
        private string lastHealthCoreStatus = "";
        private DateTime lastStatusChangeTime = DateTime.Now;
        private bool shouldShowStatusChange = false;

        private int deadeyeCore = 100;
        private bool isDeadeyeActive = false;
        private DateTime lastDeadeyeDrainTime = DateTime.Now;

        private bool isShopOpen = false;
        private Dictionary<string, int> shoppingCart = new Dictionary<string, int>();
        private ShopState currentShopState = ShopState.Main;
        private string selectedShopItem = "";

        private int totalCost = 0;
        private int shippingFee = 0;
        private int totalItemsInCart = 0;

        private string currentBackpack = "none";
        private int inventoryCapacity = 10;
        private float wantedChance = 0.0f;
        private int firstSellTime = -1;
        private Dictionary<GTA.Object, DateTime> objectsToDelete = new Dictionary<GTA.Object, DateTime>();

        private Dictionary<string, CombineInfo> combineDatabase = new Dictionary<string, CombineInfo>();
        private bool isGoldCoreActive = false;
        private int goldCoreStartTime = -1;
        private bool isShopInRemoveMode = false;
        private System.Media.SoundPlayer externalSound;
        private bool isDeadeyeSoundPlaying = false;
        private bool textureLoaded = false;
        private int txdHandle = 0;
        private Dictionary<string, int> textureHandles = new Dictionary<string, int>();
        private bool texturesLoaded = false;
        private DateTime lastTextureLoadAttemptTime = DateTime.Now;
        private bool areCoresVisible = true;
        private DateTime lastNpcScanTime = DateTime.Now;
        private bool shouldShowLootPrompt = false;
        private int consumablesPageIndex = 0;

        private readonly List<string> combine_page1_items = new List<string> { "energy_pack", "snack_duo", "light_bite", "kiosk_breakfast", "dairy_boost", "morning_energy", "hearty_meal", "comfort_soup" };
        private readonly List<string> combine_page2_items = new List<string> { "breakfast_combo", "survival_pack", "sandwich_special", "mega_snack", "protein_plate", "feast_box", "noodle_feast" };
        private readonly List<string> combine_backpack_items = new List<string>
        {
            "craft_small_backpack",
            "craft_normal_backpack",
            "craft_big_backpack",
            "craft_magic_backpack"
        };
        // Returns color code for item category in UI
        private string GetColorCodeForCategory(string category)
        {
            switch (category)
            {
                case "consumables": return "~y~";
                case "deadeye": return "~r~";
                case "armors": return "~b~";
                case "medkit": return "~g~";
                case "valuables": return "~p~";
                case "backpack_material": return "~m~";
                default: return "~w~";
            }
        }

        private enum ShopState
        {
            Main,
            Consumables,
            Consumables_Page1,
            Consumables_Page2,
            Consumables_Page3,
            Consumables_Page4,
            Deadeye,
            Armors,
            Medkit,
            Backpack,
            SellValuables,
            SellScraps
        }

        private class CombineInfo
        {
            public string Name { get; set; }
            public string ResultItemKey { get; set; }
            public Dictionary<string, int> Ingredients { get; set; }
            public int HealthCoreRestore { get; set; }
            public bool GrantsGoldCore { get; set; }
            public int AnimationDuration { get; set; }
        }

        private class LootNotification
        {
            public string ItemKey { get; set; }
            public string ItemName { get; set; }
            public int Quantity { get; set; }
            public DateTime StartTime { get; set; }
            public string TextureName { get; set; }
        }

        private readonly List<string> delayedAnimationSets = new List<string>
        {
            "amb@drink_can",
            "amb@bottle_create",
            "amb@eat_chocolate",
            "amb@eat_fruit"
        };
        private List<LootNotification> activeLootNotifications = new List<LootNotification>();
        public FoodScript()
        {
            InitializeItemDatabase();
            InitializeCombineDatabase();
            InitializeInventory();
            LoadInventoryData();

            Function.Call("REQUEST_ANIMS", "amb@ffood_server");
            Function.Call("REQUEST_ANIMS", "amb@icecream_idles");
            Function.Call("REQUEST_ANIMS", "pickup_object");
            Function.Call("REQUEST_ANIMS", "amb@drink_can");
            Function.Call("REQUEST_ANIMS", "amb@hotdog_idle");
            Function.Call("REQUEST_ANIMS", "amb@eat_chocolate");
            Function.Call("REQUEST_ANIMS", "amb@preen_bsness");
            Function.Call("REQUEST_ANIMS", "amb@bottle_create");
            Function.Call("REQUEST_ANIMS", "amb@kiosk");
            Function.Call("REQUEST_ANIMS", "amb@coffee_idle_m");
            Function.Call("REQUEST_ANIMS", "amb@nuts_idle");
            Function.Call("REQUEST_ANIMS", "amb@eat_fruit");
            Function.Call("REQUEST_ANIMS", "amb@smoking_spliff");
            Function.Call("REQUEST_ANIMS", "clothing");
            Function.Call("REQUEST_ANIMS", "playidles_std");
            Function.Call("REQUEST_ANIMS", "ped");
            Function.Call("REQUEST_ANIMS", "amb@atm");
            Function.Call("REQUEST_ANIMS", "amb@postman_idles");

            this.KeyDown += new GTA.KeyEventHandler(this.FoodScript_KeyDown);
            this.PerFrameDrawing += new GTA.GraphicsEventHandler(this.FoodScript_PerFrameDrawing);
            try
            {
                externalSound = new System.Media.SoundPlayer();
                externalSound.SoundLocation = ".\\scripts\\deadeye.wav";
                externalSound.Load();
            }
            catch
            {
                externalSound = null;
                isDeadeyeSoundPlaying = false;
            }
        }

        private enum MessageType
        {
            Success,
            Error,
            Warning,
            Info
        }

        private enum InventoryState
        {
            Main,
            Consumables,
            Consumables_Page1,
            Consumables_Page2,
            Consumables_Page3,
            Consumables_Page4,
            Deadeye,
            Armors,
            Medkit,
            Valuables,
            Valuables_Page1,
            Valuables_Page2,
            Valuables_Page3,
            Backpack,
            Combine,
            Combine_Consumables1,
            Combine_Consumables2,
            Combine_Armor,
            Combine_Medkit,
            Combine_Backpack
        }

        private class ItemInfo
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public int HealthRestore { get; set; }
            public int ArmorRestore { get; set; }
            public int HealthCoreRestore { get; set; }
            public int DeadeyeRestore { get; set; } // Deadeye stat
            public int Price { get; set; } // Item price
            public string Animation { get; set; }
            public string AnimationSet { get; set; }
            public List<string> ModelNames { get; set; }
            public List<uint> ModelHashes { get; set; }
            public int BoneId { get; set; }
            public Vector3 AttachOffset { get; set; }
            public Vector3 AttachRotation { get; set; }
            public int AnimationDuration { get; set; }
            public int MinDropRange { get; set; }
            public int MaxDropRange { get; set; }
            public string TextureName { get; set; }
        }

        // Initializes the item database with all consumables, armor, valuables, and backpack items
        private void InitializeItemDatabase()
        {
            var animationSetDurations = new Dictionary<string, int>
    {
        { "amb@drink_can", 2900 },
        { "amb@kiosk", 3300 },
        { "amb@bottle_create", 2500 },
        { "amb@smoking_spliff", 4000 },
        { "amb@coffee_idle_m", 4000 },
        { "amb@nuts_idle", 2000 },
        { "amb@eat_chocolate", 2000 },
        { "amb@icecream_idles", 3000 },
        { "amb@eat_fruit", 2000 },
        { "amb@ffood_server", 3000 },
        { "amb@hotdog_idle", 3000 },
        { "clothing", 5000 },
        { "playidles_std", 1000 }
    };

            itemDatabase = new Dictionary<string, ItemInfo>
        {
            { "beer_can", new ItemInfo { TextureName = "beer_can", Name = "Beer Can", Category = "deadeye", HealthCoreRestore = -5, DeadeyeRestore = 10, Price = 5, Animation = "can_walk", AnimationSet = "amb@drink_can", ModelHashes = new List<uint> { 3871840141, 3626367558 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.05f, -0.15f), AnimationDuration = animationSetDurations["amb@drink_can"], MinDropRange = 1, MaxDropRange = 20 } },
            { "bottled_beer", new ItemInfo { TextureName = "bottled_beer", Name = "Bottled Beer", Category = "deadeye", HealthCoreRestore = -8, DeadeyeRestore = 15, Price = 8, Animation = "player_drink", AnimationSet = "amb@kiosk", ModelHashes = new List<uint> { 2216063672,2923275916,1820901452,2064344718,944388665,768825464,3731010684,1223689405 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.05f, -0.15f), AnimationDuration = animationSetDurations["amb@kiosk"], MinDropRange = 21, MaxDropRange = 40 } },
            { "rum", new ItemInfo { TextureName = "rum", Name = "Rum", Category = "deadeye", HealthCoreRestore = -12, DeadeyeRestore = 25, Price = 15, Animation = "player_drink", AnimationSet = "amb@kiosk", ModelHashes = new List<uint> { 3837142202, 2514360022 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.05f, -0.2f), AnimationDuration = animationSetDurations["amb@kiosk"], MinDropRange = 41, MaxDropRange = 55 } },
            { "whiskey", new ItemInfo { TextureName = "whiskey", Name = "Whiskey", Category = "deadeye", HealthCoreRestore = -15, DeadeyeRestore = 30, Price = 20, Animation = "player_drink", AnimationSet = "amb@kiosk", ModelHashes = new List<uint> { 4235708847 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.05f, -0.2f), AnimationDuration = animationSetDurations["amb@kiosk"], MinDropRange = 56, MaxDropRange = 70 } },
            { "wine", new ItemInfo { TextureName = "wine", Name = "Wine", Category = "deadeye", HealthCoreRestore = -10, DeadeyeRestore = 20, Price = 25, Animation = "stand_create", AnimationSet = "amb@bottle_create", ModelHashes = new List<uint> { 4041618393, 442327005, 3010861644 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.08f, 0.04f, -0.3f), AnimationDuration = animationSetDurations["amb@bottle_create"], MinDropRange = 71, MaxDropRange = 80 } },
            { "vodka", new ItemInfo { TextureName = "vodka", Name = "Vodka", Category = "deadeye", HealthCoreRestore = -18, DeadeyeRestore = 40, Price = 30, Animation = "stand_create", AnimationSet = "amb@bottle_create", ModelHashes = new List<uint> { 2463774399, 1329178867 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.08f, 0.04f, -0.3f), AnimationDuration = animationSetDurations["amb@bottle_create"], MinDropRange = 81, MaxDropRange = 88 } },
            { "luxury_wine", new ItemInfo { TextureName = "luxury_wine", Name = "Luxury Wine", Category = "deadeye", HealthCoreRestore = -20, DeadeyeRestore = 50, Price = 50, Animation = "stand_create", AnimationSet = "amb@bottle_create", ModelHashes = new List<uint> { 132495646 }, BoneId = 0x4D0,AttachOffset = new Vector3(0.0f, 0.0f, -0.0f), AttachRotation = new Vector3(0.0f, 0.0f, 0.0f), AnimationDuration = animationSetDurations["amb@bottle_create"], MinDropRange = 89, MaxDropRange = 92 } },
            { "cigarette", new ItemInfo { TextureName = "cigarette", Name = "Cigarette", Category = "deadeye", HealthCoreRestore = -2, DeadeyeRestore = 8, Price = 6, Animation = "partial_smoke", AnimationSet = "amb@smoking_spliff", ModelHashes = new List<uint> { 4026437007 }, BoneId = 0x4D0,AttachOffset = new Vector3(0.0f, 0.0f, -0.0f), AttachRotation = new Vector3(0.0f, 0.0f, 0.0f), AnimationDuration = animationSetDurations["amb@smoking_spliff"], MinDropRange = 93, MaxDropRange = 97 } },
            { "premium_cigarette", new ItemInfo { TextureName = "premium_cigarette", Name = "Premium Cigarette", Category = "deadeye", HealthCoreRestore = -5, DeadeyeRestore = 18, Price = 12, Animation = "partial_smoke", AnimationSet = "amb@smoking_spliff", ModelNames = new List<string> { "bm_char_fag_f" },ModelHashes = new List<uint> { 2257832414 }, BoneId = 0x4D0,AttachOffset = new Vector3(0.0f, 0.0f, -0.0f), AttachRotation = new Vector3(0.0f, 0.0f, 0.0f), AnimationDuration = animationSetDurations["amb@smoking_spliff"], MinDropRange = 98, MaxDropRange = 100 } },

            { "canned_soft_drink", new ItemInfo { TextureName = "canned_soft_drink", Name = "Soda", Category = "consumables", HealthCoreRestore = 8, Price = 4, Animation = "can_walk", AnimationSet = "amb@drink_can", ModelHashes = new List<uint> { 895164698, 3096684429, 3791977071, 1932696776, 2122363748 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.05f, -0.1f), AnimationDuration = animationSetDurations["amb@drink_can"], MinDropRange = 5, MaxDropRange = 9 } },
            { "small_cola", new ItemInfo { TextureName = "small_cola", Name = "Small Cola", Category = "consumables", HealthCoreRestore = 9, Price = 4, Animation = "player_drink", AnimationSet = "amb@kiosk", ModelHashes = new List<uint> { 2845823736, 963479708 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.02f, -0.2f), AnimationDuration = animationSetDurations["amb@kiosk"], MinDropRange = 10, MaxDropRange = 14 } },
            { "sprite_small", new ItemInfo { TextureName = "sprite_small", Name = "Small Sprite", Category = "consumables", HealthCoreRestore = 11, Price = 6, Animation = "player_drink", AnimationSet = "amb@kiosk", ModelHashes = new List<uint> { 2933968171 }, BoneId = 0x4D0, AnimationDuration = animationSetDurations["amb@kiosk"], MinDropRange = 15, MaxDropRange = 18 } },
            { "coffee", new ItemInfo { TextureName = "coffee", Name = "Coffee", Category = "consumables", HealthCoreRestore = 12, Price = 7, Animation = "drink_a", AnimationSet = "amb@coffee_idle_m",  ModelHashes = new List<uint> { 2038373099 }, BoneId = 0x4D0, AnimationDuration = animationSetDurations["amb@coffee_idle_m"], MinDropRange = 19, MaxDropRange = 22 } },
            { "chocolate", new ItemInfo { TextureName = "chocolate", Name = "Chocolate", Category = "consumables", HealthCoreRestore = 13, Price = 7, Animation = "choc_walk", AnimationSet = "amb@eat_chocolate", ModelHashes = new List<uint> { 3341510013 }, BoneId = 0x4D0, AnimationDuration = animationSetDurations["amb@eat_chocolate"], MinDropRange = 23, MaxDropRange = 26 } },
            { "nuts", new ItemInfo { TextureName = "nuts", Name = "Nuts", Category = "consumables", HealthCoreRestore = 15, Price = 9, Animation = "eat_stand", AnimationSet = "amb@nuts_idle", ModelHashes = new List<uint> { 1432278665 }, BoneId = 0x4D0, AnimationDuration = animationSetDurations["amb@nuts_idle"], MinDropRange = 27, MaxDropRange = 30 } },
            { "packed_apple", new ItemInfo { TextureName = "packed_apple", Name = "Apple Juice", Category = "consumables", HealthCoreRestore = 18, Price = 11, Animation = "player_drink", AnimationSet = "amb@kiosk", ModelHashes = new List<uint> { 1196497804 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.05f, -0.1f), AnimationDuration = animationSetDurations["amb@kiosk"], MinDropRange = 31, MaxDropRange = 34 } },
            { "milk", new ItemInfo { TextureName = "milk", Name = "Milk", Category = "consumables", HealthCoreRestore = 19, Price = 12, Animation = "player_drink", AnimationSet = "amb@kiosk", ModelHashes = new List<uint> { 2724523716 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.07f, 0.0f, 0.05f), AnimationDuration = animationSetDurations["amb@kiosk"], MinDropRange = 35, MaxDropRange = 38 } },
            { "cookie", new ItemInfo { TextureName = "cookie", Name = "Cookie", Category = "consumables", HealthCoreRestore = 20, Price = 13, Animation = "eat_stand", AnimationSet = "amb@eat_fruit", ModelHashes = new List<uint> { 3271697734, 2548551442, 2795728009 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.08f, 0.05f, 0.0f), AttachRotation = new Vector3(0.5f, 1.5f, 0.0f), AnimationDuration = animationSetDurations["amb@nuts_idle"], MinDropRange = 39, MaxDropRange = 42 } },
            { "burgershot_drink", new ItemInfo { TextureName = "burgershot_drink", Name = "Burgershot drink", Category = "consumables", HealthCoreRestore = 21, Price = 13, Animation = "player_drink", AnimationSet = "amb@kiosk", ModelHashes = new List<uint> { 179795589 }, BoneId = 0x4D0, AnimationDuration = animationSetDurations["amb@kiosk"], MinDropRange = 43, MaxDropRange = 46 } },
            { "clucking_drink", new ItemInfo { TextureName = "clucking_drink", Name = "Clucking Bell Drink", Category = "consumables", HealthCoreRestore = 22, Price = 14, Animation = "player_drink", AnimationSet = "amb@kiosk", ModelHashes = new List<uint> { 1369653748 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.05f, 0.0f), AnimationDuration = animationSetDurations["amb@kiosk"], MinDropRange = 47, MaxDropRange = 50 } },
            { "packed_cookie", new ItemInfo { TextureName = "packed_cookie", Name = "Packed Cookie", Category = "consumables", HealthCoreRestore = 23, Price = 15, Animation = "eat_stand", AnimationSet = "amb@nuts_idle",  ModelHashes = new List<uint> { 2933499124 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.12f, 0.05f, 0.0f), AttachRotation = new Vector3(0.6f, 0.5f, 0.0f), AnimationDuration = animationSetDurations["amb@eat_fruit"], MinDropRange = 51, MaxDropRange = 53 } },
            { "big_cola", new ItemInfo { TextureName = "big_cola", Name = "Big Cola", Category = "consumables", HealthCoreRestore = 24, Price = 16, Animation = "stand_create", AnimationSet = "amb@bottle_create", ModelHashes = new List<uint> { 3500144302,  2655916555}, BoneId = 0x4D0, AttachOffset = new Vector3(0.07f, 0.05f, -0.45f), AnimationDuration = animationSetDurations["amb@bottle_create"], MinDropRange = 54, MaxDropRange = 56 } },
            { "donut", new ItemInfo { TextureName = "donut", Name = "Donut", Category = "consumables", HealthCoreRestore = 25, Price = 16, Animation = "eat_burger_plyr", AnimationSet = "amb@ffood_server", ModelHashes = new List<uint> { 1011762108, 3998517585, 4272761346 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.14f, 0.1f, -0.05f), AttachRotation = new Vector3(1.0f, 0.0f, 1.0f), AnimationDuration = animationSetDurations["amb@ffood_server"], MinDropRange = 57, MaxDropRange = 59 } },
            { "big_milk", new ItemInfo { TextureName = "big_milk", Name = "Big Milk", Category = "consumables", HealthCoreRestore = 26, Price = 17, Animation = "stand_create", AnimationSet = "amb@bottle_create", ModelHashes = new List<uint> { 3610861588 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.1f, -0.4f), AnimationDuration = animationSetDurations["amb@bottle_create"], MinDropRange = 60, MaxDropRange = 62 } },
            { "big_sprite", new ItemInfo { TextureName = "big_sprite", Name = "Big Sprite", Category = "consumables", HealthCoreRestore = 27, Price = 18, Animation = "stand_create", AnimationSet = "amb@bottle_create", ModelHashes = new List<uint> { 3040528631,  2038290433 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.05f, -0.2f), AttachRotation = new Vector3(0.0f, 0.0f, 10.0f), AnimationDuration = animationSetDurations["amb@bottle_create"], MinDropRange = 63, MaxDropRange = 65 } },
            { "canned_juice", new ItemInfo { TextureName = "canned_juice", Name = "Canned Juice", Category = "consumables", HealthCoreRestore = 28, Price = 19, Animation = "can_walk", AnimationSet = "amb@drink_can", ModelHashes = new List<uint> { 1277447876, 1442210408 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.06f, -0.1f), AnimationDuration = animationSetDurations["amb@drink_can"], MinDropRange = 66, MaxDropRange = 68 } },

            { "ice_cream", new ItemInfo { TextureName = "ice_cream", Name = "Ice Cream", Category = "consumables", HealthCoreRestore = 29, Price = 19, Animation = "stand_eat", AnimationSet = "amb@icecream_idles", ModelHashes = new List<uint> { 3396409940 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.0f, 0.0f, 0.0f), AttachRotation = new Vector3(0.0f, 0.0f, 0.0f), AnimationDuration = animationSetDurations["amb@eat_fruit"], MinDropRange = 69, MaxDropRange = 71 } },

            { "orange_juice", new ItemInfo { TextureName = "orange_juice", Name = "Orange Juice", Category = "consumables", HealthCoreRestore = 30, Price = 20, Animation = "stand_create", AnimationSet = "amb@bottle_create", ModelHashes = new List<uint> { 233901173 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.0f, -0.3f), AttachRotation = new Vector3(0.0f, 0.0f, 10.0f), AnimationDuration = animationSetDurations["amb@bottle_create"], MinDropRange = 72, MaxDropRange = 74 } },
            { "salad", new ItemInfo { TextureName = "salad", Name = "Salad", Category = "consumables", HealthCoreRestore = 31, Price = 21, Animation = "eat_stand", AnimationSet = "amb@nuts_idle", ModelHashes = new List<uint> { 2996132691 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.15f, 0.0f, 0.0f), AnimationDuration = animationSetDurations["amb@nuts_idle"], MinDropRange = 75, MaxDropRange = 77 } },
            { "cheese", new ItemInfo { TextureName = "cheese", Name = "Cheese", Category = "consumables", HealthCoreRestore = 32, Price = 22, Animation = "eat_stand", AnimationSet = "amb@eat_fruit", ModelHashes = new List<uint> { 4004296482, 3247430893 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.0f, 0.0f), AnimationDuration = animationSetDurations["amb@eat_fruit"], MinDropRange = 78, MaxDropRange = 80 } },
            { "ciabatta", new ItemInfo { TextureName = "ciabatta", Name = "Ciabatta", Category = "consumables", HealthCoreRestore = 33, Price = 22, Animation = "eat_walk", AnimationSet = "amb@hotdog_idle", ModelHashes = new List<uint> { 2125363417 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.0f, 0.0f), AttachRotation = new Vector3(0.0f, 0.0f, 10.0f), AnimationDuration = animationSetDurations["amb@hotdog_idle"], MinDropRange = 81, MaxDropRange = 83 } },
            { "fried_potato", new ItemInfo { TextureName = "fried_potato", Name = "Fried potato", Category = "consumables", HealthCoreRestore = 34, Price = 23, Animation = "eat_stand", AnimationSet = "amb@nuts_idle", ModelHashes = new List<uint> { 3213563275,870516668 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.0f, 0.0f), AnimationDuration = animationSetDurations["amb@nuts_idle"], MinDropRange = 84, MaxDropRange = 86 } },
            { "chicken_nugget", new ItemInfo { TextureName = "chicken_nugget", Name = "Chicken Nugget", Category = "consumables", HealthCoreRestore = 35, Price = 24, Animation = "eat_stand", AnimationSet = "amb@nuts_idle", ModelHashes = new List<uint> {  3653453724 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.0f, 0.0f), AnimationDuration = animationSetDurations["amb@nuts_idle"], MinDropRange = 87, MaxDropRange = 88 } },
            { "noodle", new ItemInfo { TextureName = "noodle", Name = "Noodle", Category = "consumables", HealthCoreRestore = 36, Price = 25, Animation = "eat_stand", AnimationSet = "amb@nuts_idle", ModelHashes = new List<uint> { 3972127584, 3733208805, 2941378689, 2702656524 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.15f, 0.07f, -0.15f), AnimationDuration = animationSetDurations["amb@nuts_idle"], MinDropRange = 89, MaxDropRange = 90 } },
            { "cereal", new ItemInfo { TextureName = "cereal", Name = "Cereal", Category = "consumables", HealthCoreRestore = 37, Price = 25, Animation = "eat_stand", AnimationSet = "amb@nuts_idle", ModelHashes = new List<uint> { 1459310348, 1313595603 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.25f, 0.0f, -0.3f), AnimationDuration = animationSetDurations["amb@nuts_idle"], MinDropRange = 91, MaxDropRange = 92 } },
            { "baguette", new ItemInfo { TextureName = "baguette", Name = "Baguette", Category = "consumables", HealthCoreRestore = 38, Price = 26, Animation = "eat_stand", AnimationSet = "amb@eat_fruit", ModelHashes = new List<uint> { 4060419290 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.0f, 0.0f, 0.0f), AttachRotation = new Vector3(5.0f, 0.0f, 3.5f), AnimationDuration = animationSetDurations["amb@eat_fruit"], MinDropRange = 93, MaxDropRange = 94 } },
            { "chicken_soup", new ItemInfo { TextureName = "chicken_soup", Name = "Chicken Soup", Category = "consumables", HealthCoreRestore = 40, Price = 28, Animation = "drink_a", AnimationSet = "amb@coffee_idle_m", ModelHashes = new List<uint> { 2412475480 }, BoneId = 0x4D0,AttachOffset = new Vector3(0.08f, 0.1f, 0.0f), AttachRotation = new Vector3(0.0f, 0.0f, 10.0f), AnimationDuration = animationSetDurations["amb@coffee_idle_m"], MinDropRange = 95, MaxDropRange = 96 } },
            { "hotdog", new ItemInfo { TextureName = "hotdog", Name = "Hotdog", Category = "consumables", HealthCoreRestore = 42, Price = 29, Animation = "eat_walk", AnimationSet = "amb@hotdog_idle", ModelHashes = new List<uint> { 2219059314 }, BoneId = 0x4D0, AnimationDuration = animationSetDurations["amb@hotdog_idle"], MinDropRange = 97, MaxDropRange = 97 } },
            { "pizza", new ItemInfo { TextureName = "pizza", Name = "Pizza", Category = "consumables", HealthCoreRestore = 43, Price = 30, Animation = "eat_walk", AnimationSet = "amb@hotdog_idle", ModelHashes = new List<uint> { 2514489304 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.0f, 0.0f), AnimationDuration = animationSetDurations["amb@hotdog_idle"], MinDropRange = 98, MaxDropRange = 98 } },
            { "canned_meat", new ItemInfo { TextureName = "canned_meat", Name = "Canned Meat", Category = "consumables", HealthCoreRestore = 45, Price = 31, Animation = "drink_a", AnimationSet = "amb@coffee_idle_m", ModelHashes = new List<uint> { 2484452726 }, BoneId = 0x4D0, AttachOffset = new Vector3(0.1f, 0.08f, -0.1f), AnimationDuration = animationSetDurations["amb@coffee_idle_m"], MinDropRange = 99, MaxDropRange = 99 } },
            { "burger", new ItemInfo { TextureName = "burger", Name = "Burger", Category = "consumables", HealthCoreRestore = 50, Price = 35, Animation = "eat_burger_plyr", AnimationSet = "amb@ffood_server", ModelHashes = new List<uint> { 2086092453 }, BoneId = 0x4D0, AnimationDuration = animationSetDurations["amb@ffood_server"], MinDropRange = 100, MaxDropRange = 100 } },


            { "armor_20", new ItemInfo { TextureName = "armor_20", Name = "Light Armor", Category = "armors", ArmorRestore = 20, Price = 200, Animation = "brushoff_suit_stand", AnimationSet = "clothing", ModelHashes = new List<uint> {}, BoneId = 0x4B3, AnimationDuration = animationSetDurations["clothing"], MinDropRange = 1, MaxDropRange = 50 } },
            { "armor_50", new ItemInfo { TextureName = "armor_50", Name = "Heavy Armor", Category = "armors", ArmorRestore = 50, Price = 300, Animation = "brushoff_suit_stand", AnimationSet = "clothing", ModelHashes = new List<uint> {}, BoneId = 0x4B3, AnimationDuration = animationSetDurations["clothing"], MinDropRange = 51, MaxDropRange = 80 } },
            { "armor_100", new ItemInfo { TextureName = "armor_100", Name = "Full Armor", Category = "armors", ArmorRestore = 100, Price = 500, Animation = "brushoff_suit_stand", AnimationSet = "clothing", ModelHashes = new List<uint> {}, BoneId = 0x4B3, AnimationDuration = animationSetDurations["clothing"], MinDropRange = 81, MaxDropRange = 100 } },

            { "used_medkit", new ItemInfo { TextureName = "used_medkit", Name = "Opened Medkit", Category = "medkit", HealthRestore = 50, Price = 0, Animation = "idle_checkarm_r", AnimationSet = "playidles_std", ModelHashes = new List<uint> {1069950328}, BoneId = 0x4C3,AttachOffset = new Vector3(0.2f, 0.05f, 0.0f),AttachRotation = new Vector3(0.0f, 5.0f, 0.0f), AnimationDuration = animationSetDurations["playidles_std"], MinDropRange = 1, MaxDropRange = 60 } },
            { "medkit", new ItemInfo { TextureName = "medkit", Name = "Medkit", Category = "medkit", HealthRestore = 100, Price = 150, Animation = "idle_checkarm_r", AnimationSet = "playidles_std", ModelHashes = new List<uint> {1069950328}, BoneId = 0x4C3,AttachOffset = new Vector3(0.2f, 0.05f, 0.0f),AttachRotation = new Vector3(0.0f, 5.0f, 0.0f), AnimationDuration = animationSetDurations["playidles_std"], MinDropRange = 61, MaxDropRange = 100 } },

            { "small_backpack", new ItemInfo { TextureName = "small_backpack", Name = "Small Backpack", Category = "backpack", Price = 5000 } },
            { "normal_backpack", new ItemInfo { TextureName = "normal_backpack", Name = "Normal Backpack", Category = "backpack", Price = 10000 } },
            { "big_backpack", new ItemInfo { TextureName = "big_backpack", Name = "Big Backpack", Category = "backpack", Price = 30000 } },
            { "magic_backpack", new ItemInfo { TextureName = "magic_backpack", Name = "Legend of the East Satchel", Category = "backpack", Price = 60000 } },

            { "backpack_material", new ItemInfo { TextureName = "backpack_material", Name = "Backpack material", Category = "backpack_material", MinDropRange = 1, MaxDropRange = 100} },

            { "gold_tooth", new ItemInfo { TextureName = "gold_tooth", Name = "Gold Tooth", Category = "valuables", Price = 25, MinDropRange = 1, MaxDropRange = 15 } },
            { "pen", new ItemInfo { TextureName = "pen", Name = "Pen", Category = "valuables", Price = 40, MinDropRange = 16, MaxDropRange = 30 } },
            { "silver_earring", new ItemInfo { TextureName = "silver_earring", Name = "Silver Earring", Category = "valuables", Price = 50, MinDropRange = 31, MaxDropRange = 40 } },
            { "silver_ring", new ItemInfo { TextureName = "silver_ring", Name = "Silver Ring", Category = "valuables", Price = 60, MinDropRange = 41, MaxDropRange = 50 } },
            { "gold_earring", new ItemInfo { TextureName = "gold_earring", Name = "Gold Earring", Category = "valuables", Price = 75, MinDropRange = 51, MaxDropRange = 58 } },
            { "gold_ring", new ItemInfo { TextureName = "gold_ring", Name = "Gold Ring", Category = "valuables", Price = 85, MinDropRange = 59, MaxDropRange = 66 } },
            { "platinum_earring", new ItemInfo { TextureName = "platinum_earring", Name = "Platinum Earring", Category = "valuables", Price = 90, MinDropRange = 67, MaxDropRange = 72 } },
            { "platinum_band", new ItemInfo { TextureName = "platinum_band", Name = "Platinum Ring", Category = "valuables", Price = 100, MinDropRange = 73, MaxDropRange = 78 } },
            { "silver_pocket_watch", new ItemInfo { TextureName = "silver_pocket_watch", Name = "Silver Pocket Watch", Category = "valuables", Price = 120, MinDropRange = 79, MaxDropRange = 83 } },
            { "necklace", new ItemInfo { TextureName = "necklace", Name = "Necklace", Category = "valuables", Price = 150, MinDropRange = 84, MaxDropRange = 87 } },
            { "gold_pocket_watch", new ItemInfo { TextureName = "gold_pocket_watch", Name = "Gold Pocket Watch", Category = "valuables", Price = 180, MinDropRange = 88, MaxDropRange = 90 } },
            { "platinum_chain_necklace", new ItemInfo { TextureName = "platinum_chain_necklace", Name = "Platinum Chain", Category = "valuables", Price = 200, MinDropRange = 91, MaxDropRange = 93 } },
            { "pearl_necklace", new ItemInfo { TextureName = "pearl_necklace", Name = "Pearl Necklace", Category = "valuables", Price = 250, MinDropRange = 94, MaxDropRange = 95 } },
            { "small_jewelry_bag", new ItemInfo { TextureName = "small_jewelry_bag", Name = "Small Jewelry Box", Category = "valuables", Price = 300, MinDropRange = 96, MaxDropRange = 97, ModelHashes = new List<uint> { 3213125533 }  } },
            { "gold_nugget", new ItemInfo { TextureName = "gold_nugget", Name = "Gold Nugget", Category = "valuables", Price = 500, MinDropRange = 98, MaxDropRange = 98 } },
            { "large_jewelry_bag", new ItemInfo { TextureName = "large_jewelry_bag", Name = "Large Jewelry Box", Category = "valuables", Price = 750, MinDropRange = 99, MaxDropRange = 99 } },
            { "gold_bar", new ItemInfo { TextureName = "gold_bar", Name = "Gold Bar", Category = "valuables", Price = 1000, MinDropRange = 100, MaxDropRange = 100 } },
            { "old_mobile_phone", new ItemInfo { TextureName = "old_mobile_phone", Name = "Old Mobile Phone", Category = "valuables", Price = 100, ModelHashes = new List<uint> { 3539882992 } } },
            { "modern_mobile_phone", new ItemInfo { TextureName = "modern_mobile_phone", Name = "Modern Mobile Phone", Category = "valuables", Price = 250, ModelHashes = new List<uint> { 683474796 } } },
            { "mp3_player", new ItemInfo { TextureName = "mp3_player", Name = "MP3 Player", Category = "valuables", Price = 60, ModelHashes = new List<uint> { 2100904143 } } },
            { "walkie_talkie", new ItemInfo { TextureName = "walkie_talkie", Name = "Walkie Talkie", Category = "valuables", Price = 90, ModelHashes = new List<uint> { 3260216985 } } },
            { "camera", new ItemInfo { TextureName = "camera", Name = "Camera", Category = "valuables", Price = 300, ModelHashes = new List<uint> { 3792147693 } } },
            { "metal_scrap", new ItemInfo { TextureName = "metal_scrap", Name = "Metal Scrap", Category = "valuables", Price = 5, ModelHashes = new List<uint> { 2093070418, 2784987853, 1306235956, 1707339027, 1940981977,3196460674, 1298459874  } } },
            { "scrap", new ItemInfo { TextureName = "scrap", Name = "Scrap", Category = "valuables", Price = 3, ModelHashes = new List<uint> { 2354979668, 2035621784, 592664998, 2388036357, 1544958121, 3356297369 } } },

        };
        }

        // Sets up the crafting recipes database for item combining
        private void InitializeCombineDatabase()
        {
            combineDatabase = new Dictionary<string, CombineInfo>
            {
                { "energy_pack", new CombineInfo { Name = "Energy Pack", Ingredients = new Dictionary<string, int> {{"canned_soft_drink", 1}, {"chocolate", 1} },  HealthCoreRestore = 22, AnimationDuration = 5000 } },
                { "snack_duo", new CombineInfo { Name = "Snack Duo", Ingredients = new Dictionary<string, int> { {"nuts", 1}, {"sprite_small", 1} }, HealthCoreRestore = 27, AnimationDuration = 5400  } },
                { "light_bite", new CombineInfo { Name = "Light Bite", Ingredients = new Dictionary<string, int> { {"packed_cookie", 1}, {"coffee", 1} }, HealthCoreRestore = 31, AnimationDuration = 6000 } },
                { "kiosk_breakfast", new CombineInfo { Name = "Kiosk Breakfast", Ingredients = new Dictionary<string, int> {{"packed_cookie", 1}, {"nuts", 1},{"burgershot_drink", 1} },   HealthCoreRestore = 58, AnimationDuration = 7400 } },
                { "dairy_boost", new CombineInfo { Name = "Dairy Boost", Ingredients = new Dictionary<string, int> { {"cheese", 1}, {"orange_juice", 1} }, HealthCoreRestore = 68, AnimationDuration = 4500 } },
                { "morning_energy", new CombineInfo { Name = "Morning Energy", Ingredients = new Dictionary<string, int> {{"cereal", 1}, {"coffee", 1}, {"packed_apple", 1} },  HealthCoreRestore = 71, AnimationDuration = 9400  } },
                { "hearty_meal", new CombineInfo { Name = "Hearty Meal", Ingredients = new Dictionary<string, int> { {"burger", 1}, {"big_cola", 1} }, HealthCoreRestore = 82,GrantsGoldCore = true, AnimationDuration = 5500 } },
                { "comfort_soup", new CombineInfo { Name = "Comfort Soup", Ingredients = new Dictionary<string, int> {{"baguette", 1},{"chicken_soup", 1} },   HealthCoreRestore = 84,GrantsGoldCore = true, AnimationDuration = 6000 } },
                { "breakfast_combo", new CombineInfo { Name = "Breakfast Combo", Ingredients = new Dictionary<string, int> {{"ciabatta", 1}, {"cookie", 1},{"big_milk", 1 } },   HealthCoreRestore = 86,GrantsGoldCore = true, AnimationDuration = 7500 } },
                { "survival_pack", new CombineInfo { Name = "Survival Pack", Ingredients = new Dictionary<string, int> { {"canned_meat", 1}, {"canned_juice", 1}, {"small_cola", 1} }, HealthCoreRestore = 88,GrantsGoldCore = true, AnimationDuration = 10400  } },
                { "sandwich_special", new CombineInfo { Name = "Sandwich Special", Ingredients = new Dictionary<string, int> { {"hotdog", 1}, {"ciabatta", 1}, {"soft_drink", 1} }, HealthCoreRestore = 89,GrantsGoldCore = true, AnimationDuration = 9000 } },
                { "mega_snack", new CombineInfo { Name = "Mega Snack", Ingredients = new Dictionary<string, int> { {"pizza", 1}, {"donut", 1}, {"milk", 1} }, HealthCoreRestore = 96,GrantsGoldCore = true, AnimationDuration = 9400  } },
                { "protein_plate", new CombineInfo { Name = "Protein Plate", Ingredients = new Dictionary<string, int> { {"chicken_nugget", 1}, {"salad", 1}, {"fried_potato", 1} }, HealthCoreRestore = 100, GrantsGoldCore = true, AnimationDuration = 6000 } },
                { "feast_box", new CombineInfo { Name = "Feast Box", Ingredients = new Dictionary<string, int> { {"burger", 1}, {"pizza", 1}, {"big_sprite", 1}}, HealthCoreRestore = 100, GrantsGoldCore = true, AnimationDuration = 8500 } },
                { "noodle_feast", new CombineInfo { Name = "Noodle Feast", Ingredients = new Dictionary<string, int> { {"noodle", 1}, {"cheese", 1}, {"chicken_soup", 1} }, HealthCoreRestore = 100, GrantsGoldCore = true, AnimationDuration = 8000 } },
                { "combine_full_armor_light", new CombineInfo { Name = "Full Armor", ResultItemKey = "armor_100", Ingredients = new Dictionary<string, int> { {"armor_20", 5} } } },
                { "combine_full_armor_heavy", new CombineInfo { Name = "Full Armor", ResultItemKey = "armor_100", Ingredients = new Dictionary<string, int> { {"armor_50", 2} } } },
                { "combine_medkit", new CombineInfo { Name = "Medkit", ResultItemKey = "medkit", Ingredients = new Dictionary<string, int> { {"used_medkit", 2} } } },
                { "craft_small_backpack", new CombineInfo { Name = "Small Backpack", ResultItemKey = "small_backpack", Ingredients = new Dictionary<string, int> { {"backpack_material", 10} } } },
                { "craft_normal_backpack", new CombineInfo { Name = "Normal Backpack", ResultItemKey = "normal_backpack", Ingredients = new Dictionary<string, int> { {"backpack_material", 15} } } },
                { "craft_big_backpack", new CombineInfo { Name = "Big Backpack", ResultItemKey = "big_backpack", Ingredients = new Dictionary<string, int> { {"backpack_material", 20} } } },
                { "craft_magic_backpack", new CombineInfo { Name = "Legend of the East Satchel", ResultItemKey = "magic_backpack", Ingredients = new Dictionary<string, int> { {"backpack_material", 30} } } },
            };
        }

        // Filters items to only include those available in the player's inventory
        private List<string> GetAvailableItems(List<string> itemsToCheck)
        {
            List<string> availableItems = new List<string>();
            foreach (string itemKey in itemsToCheck)
            {
                if (inventory.ContainsKey(itemKey) && inventory[itemKey] > 0)
                {
                    availableItems.Add(itemKey);
                }
            }
            return availableItems;
        }

        // Removes and cleans up game objects that are scheduled for deletion
        private void ManageDetachedObjects()
        {
            if (objectsToDelete.Count == 0) return;

            List<GTA.Object> deletedObjects = new List<GTA.Object>();

            foreach (var entry in objectsToDelete)
            {
                if (DateTime.Now >= entry.Value)
                {
                    GTA.Object obj = entry.Key;
                    if (obj != null && obj.Exists())
                    {
                        obj.NoLongerNeeded();
                        obj.Delete();
                    }
                    deletedObjects.Add(obj);
                }
            }
            foreach (GTA.Object obj in deletedObjects)
            {
                objectsToDelete.Remove(obj);
            }
        }

        // Loads texture assets from the game's TXD files for UI rendering
        private void LoadTextures()
        {
            if ((DateTime.Now - lastTextureLoadAttemptTime).TotalMilliseconds < 500 && textureLoaded)
            {
                return;
            }

            lastTextureLoadAttemptTime = DateTime.Now;

            try
            {
                txdHandle = Function.Call<int>("LOAD_TXD", "inventory");
                if (txdHandle == 0)
                {
                    texturesLoaded = false;
                    textureLoaded = false;
                    return;
                }

                textureHandles.Clear();

                foreach (var itemInfo in itemDatabase.Values)
                {
                    if (!string.IsNullOrEmpty(itemInfo.TextureName) && !textureHandles.ContainsKey(itemInfo.TextureName))
                    {
                        int textureId = Function.Call<int>("GET_TEXTURE", txdHandle, itemInfo.TextureName);
                        if (textureId != 0)
                        {
                            textureHandles[itemInfo.TextureName] = textureId;
                        }
                    }
                }

                texturesLoaded = textureHandles.Count > 0;
                textureLoaded = true;
            }
            catch
            {
                texturesLoaded = false;
                textureLoaded = false;
            }
        }
        // Updates the player's health core value based on game time and health regeneration
        private void UpdateHealthCore()
        {
            if (isGoldCoreActive)
            {
                int currentTime = Function.Call<int>("GET_HOURS_OF_DAY") * 60 + Function.Call<int>("GET_MINUTES_OF_DAY");
                int timeElapsed = (currentTime - goldCoreStartTime + 1440) % 1440;

                if (timeElapsed >= 300)
                {
                    isGoldCoreActive = false;
                    goldCoreStartTime = -1;
                    DisplayMessage("Golden Health Core effect has worn off.", MessageType.Warning);
                }
            }
            if (!Player.Character.isAliveAndWell && !wasPlayerDead)
            {
                isGoldCoreActive = false;
                goldCoreStartTime = -1;
                healthCore = Math.Max(0, healthCore - 50);
                deadeyeCore = Math.Max(0, deadeyeCore - 50);
                wasPlayerDead = true;
            }
            else if (Player.Character.isAliveAndWell && wasPlayerDead)
            {
                wasPlayerDead = false;
            }

            int currentHour = Function.Call<int>("GET_HOURS_OF_DAY");
            int currentMinute = Function.Call<int>("GET_MINUTES_OF_DAY");

            bool canControl = Game.LocalPlayer.CanControlCharacter;

            if (canControl)
            {
                if (!isGoldCoreActive && lastGameHour != -1)
                {
                    if (currentMinute != lastGameMinute)
                    {
                        gameMinutesElapsed++;
                        if (gameMinutesElapsed >= 60)
                        {
                            healthCore = Math.Max(0, healthCore - 3);
                            gameMinutesElapsed = 0;
                        }
                    }
                }

                if (lastHealthRegenGameHour == -1)
                {
                    lastHealthRegenGameHour = currentHour;
                    lastHealthRegenGameMinute = currentMinute;
                    healthRegenGameMinutesElapsed = 0;
                }
                else
                {
                    if (currentMinute != lastHealthRegenGameMinute)
                    {
                        healthRegenGameMinutesElapsed++;

                        if (healthRegenGameMinutesElapsed >= 5)
                        {
                            if (Player.Character.isAliveAndWell && Player.Character.Health < 100)
                            {
                                int regenAmount = GetHealthRegeneration();
                                if (regenAmount > 0)
                                {
                                    Player.Character.Health = Math.Min(100, Player.Character.Health + regenAmount);
                                }
                            }
                            healthRegenGameMinutesElapsed = 0;
                        }
                    }
                }

                lastHealthRegenGameHour = currentHour;
                lastHealthRegenGameMinute = currentMinute;
            }
            else
            {
                lastHealthRegenGameHour = currentHour;
                lastHealthRegenGameMinute = currentMinute;
                healthRegenGameMinutesElapsed = 0;
            }

            lastGameHour = currentHour;
            lastGameMinute = currentMinute;

            string currentStatus = GetHealthCoreStatus();
            if (currentStatus != lastHealthCoreStatus && !string.IsNullOrEmpty(lastHealthCoreStatus))
            {
                lastStatusChangeTime = DateTime.Now;
                shouldShowStatusChange = true;
            }
            lastHealthCoreStatus = currentStatus;
        }

        // Calculates the amount of health regeneration based on current health core status
        private int GetHealthRegeneration()
        {
            if (healthCore > 80) return 10; // Well fed
            if (healthCore > 60) return 8; // Good
            if (healthCore > 40) return 5; // Fair
            if (healthCore > 20) return 3;  // Hungry
            return 0; // Starving
        }

        // Returns the current health core status based on the health core value
        private string GetHealthCoreStatus()
        {
            if (isGoldCoreActive) return "Golden";
            if (healthCore > 80) return "Well Fed";
            if (healthCore > 60) return "Good";
            if (healthCore > 40) return "Satisfied";
            if (healthCore > 20) return "Hungry";
            return "Starving";
        }

        // Returns the appropriate color for displaying the health core status
        private Color GetHealthCoreColor()
        {
            if (isGoldCoreActive) return Color.Gold;
            if (healthCore > 80) return Color.Green;
            if (healthCore > 60) return Color.Yellow; 
            if (healthCore > 40) return Color.Orange;
            if (healthCore > 20) return Color.OrangeRed; 
            return Color.Red; // Starving
        }

        // Initializes all inventory slots to zero for all items in the database
        private void InitializeInventory()
        {
            foreach (var item in itemDatabase.Keys)
            {
                inventory[item] = 0;
            }
        }

        // Loads saved inventory data and backpack type from the data file
        private void LoadInventoryData()
        {
            try
            {
                string dataFile = "";

                switch (Game.CurrentEpisode)
                {
                    case GameEpisode.GTAIV:
                        dataFile = DATA_FILE1;
                        break;
                    case GameEpisode.TBOGT:
                        dataFile = DATA_FILE3;
                        break;
                    case GameEpisode.TLAD:
                        dataFile = DATA_FILE2;
                        break;
                    default:
                        dataFile = DATA_FILE;
                        break;
                }

                if (File.Exists(dataFile))
                {
                    string[] lines = File.ReadAllLines(dataFile);
                    foreach (string line in lines)
                    {
                        if (line.Contains("="))
                        {
                            string[] parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                if (inventory.ContainsKey(parts[0]))
                                {
                                    int count;
                                    if (int.TryParse(parts[1], out count))
                                    {
                                        inventory[parts[0]] = count;
                                    }
                                }
                                else if (parts[0] == "healthCore")
                                {
                                    int.TryParse(parts[1], out healthCore);
                                    healthCore = Math.Max(0, Math.Min(healthCore, 100));
                                }
                                else if (parts[0] == "currentBackpack")
                                {
                                    currentBackpack = parts[1];
                                }
                                else if (parts[0] == "deadeyeCore")
                                {
                                    int.TryParse(parts[1], out deadeyeCore);
                                    deadeyeCore = Math.Max(0, Math.Min(deadeyeCore, 100));
                                }
                            }
                        }
                    }
                    UpdateInventoryCapacity();
                }
            }
            catch {  }
        }

        // Saves the current inventory contents and game state to file
        private void SaveInventoryData()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (var kvp in inventory)
                {
                    lines.Add(string.Format("{0}={1}", kvp.Key, kvp.Value));
                }

                lines.Add(string.Format("healthCore={0}", healthCore));
                lines.Add(string.Format("currentBackpack={0}", currentBackpack));
                lines.Add(string.Format("deadeyeCore={0}", deadeyeCore));

                switch (Game.CurrentEpisode)
                {
                    case GameEpisode.GTAIV:
                        File.WriteAllLines(DATA_FILE1, lines);
                        break;
                    case GameEpisode.TBOGT:
                        File.WriteAllLines(DATA_FILE3, lines);
                        break;
                    case GameEpisode.TLAD:
                        File.WriteAllLines(DATA_FILE2, lines);
                        break;
                    default:
                        File.WriteAllLines(DATA_FILE, lines);
                        break;
                }
            }
            catch {  }
        }

        // Updates inventory capacity based on the current equipped backpack
        private void UpdateInventoryCapacity()
        {
            switch (currentBackpack)
            {
                case "small_backpack":
                    inventoryCapacity = 20;
                    break;
                case "normal_backpack":
                    inventoryCapacity = 30;
                    break;
                case "big_backpack":
                    inventoryCapacity = 50;
                    break;
                case "magic_backpack":
                    inventoryCapacity = 100;
                    break;
                default:
                    inventoryCapacity = 10;
                    break;
            }
        }
        // Returns the storage capacity for a given backpack type
        private int GetBackpackCapacityForKey(string backpackKey)
        {
            switch (backpackKey)
            {
                case "small_backpack": return 20;
                case "normal_backpack": return 30;
                case "big_backpack": return 50;
                case "magic_backpack": return 100;
                default: return 10; // default (none)
            }
        }
        // Returns a list of craftable backpacks with higher capacity than current backpack
        private List<string> GetDisplayableCombineBackpacks()
        {
            List<string> res = new List<string>();
            foreach (var kv in combineDatabase)
            {
                var key = kv.Key;
                var ci = kv.Value;
                if (ci.ResultItemKey == null) continue;
                if (!itemDatabase.ContainsKey(ci.ResultItemKey)) continue;
                if (itemDatabase[ci.ResultItemKey].Category != "backpack") continue;

                int cap = GetBackpackCapacityForKey(ci.ResultItemKey);
                if (cap > inventoryCapacity) 
                {
                    res.Add(key);
                }
            }
            return res;
        }
        // Manages the deadeye mechanic, including activation and draining
        private void HandleDeadeye()
        {
            if (isDeadeyeActive && !Game.isGameKeyPressed(GameKey.Aim))
            {
                isDeadeyeActive = false;
                Function.Call("SET_TIME_SCALE", 1.0f);
                StopDeadeyeSound();
            }
            if (isDeadeyeActive)
            {
                if (deadeyeCore <= 0)
                {
                    isDeadeyeActive = false;
                    Function.Call("SET_TIME_SCALE", 1.0f);
                    StopDeadeyeSound();
                    return;
                }

                if ((DateTime.Now - lastDeadeyeDrainTime).TotalSeconds >= 3)
                {
                    deadeyeCore = Math.Max(0, deadeyeCore - 10);
                    lastDeadeyeDrainTime = DateTime.Now;
                }
            }
        }

        // Stops the deadeye activation sound effect
        private void StopDeadeyeSound()
        {
            if (externalSound != null && isDeadeyeSoundPlaying)
            {
                try
                {
                    externalSound.Stop();
                }
                catch
                {
                }
                isDeadeyeSoundPlaying = false;
            }
        }

        // Checks if player has a valid weapon for using the deadeye ability
        private bool HasValidWeapon(Ped player)
        {
            try
            {
                if (player.Weapons != null && player.Weapons.Current != null)
                {
                    if (player.Weapons.Current == Weapon.Unarmed || player.Weapons.Current == Weapon.Melee_BaseballBat
                    || player.Weapons.Current == Weapon.Melee_PoolCue || player.Weapons.Current == Weapon.Melee_Knife
                    || player.Weapons.Current == Weapon.Thrown_Grenade || player.Weapons.Current == Weapon.Thrown_Molotov
                    || player.Weapons.Current == Weapon.Misc_Object)
                    {
                        return false;
                    }

                    return true;
                }

                int weapon = Function.Call<int>("GET_CURRENT_CHAR_WEAPON", player);

                int[] excludedWeaponIDs = { 0, 1, 15, 16, 4, 5, 17, 18 };

                foreach (int excludedID in excludedWeaponIDs)
                {
                    if (weapon == excludedID)
                    {
                        return false;
                    }
                }

                return weapon > 1;
            }
            catch
            {
                return false;
            }
        }

        // Toggles the deadeye ability on or off with weapon validation
        private void ToggleDeadeye()
        {
            if (isDeadeyeActive) return;

            if (!HasValidWeapon(Player.Character))
            {
                DisplayMessage("Cannot use Deadeye with this weapon!", MessageType.Error);
                return;
            }

            if (deadeyeCore > 0)
            {
                isDeadeyeActive = true;
                lastDeadeyeDrainTime = DateTime.Now;
                Function.Call("SET_TIME_SCALE", 0.2f);

                if (externalSound != null && !isDeadeyeSoundPlaying)
                {
                    try
                    {
                        externalSound.PlayLooping();
                        isDeadeyeSoundPlaying = true;
                    }
                    catch
                    {
                        isDeadeyeSoundPlaying = false;
                    }
                }
            }
            else
            {
                DisplayMessage("Deadeye is empty!", MessageType.Warning);
            }
        }

        // Adds consumable items to inventory for testing purposes
        private void AddAllConsumables()
        {
            int itemsAdded = 0;
            foreach (var kvp in itemDatabase)
            {
                if (kvp.Value.Category == "consumables" && inventory[kvp.Key] < 10)
                {
                    inventory[kvp.Key] = Math.Min(inventory[kvp.Key] + 10, 10);
                    itemsAdded++;
                }
            }
            SaveInventoryData();
            DisplayMessage(string.Format("Added items to {0} consumables!", itemsAdded), MessageType.Success);
        }


        // Main rendering loop called every frame to update all UI elements and game state
        private void FoodScript_PerFrameDrawing(object sender, GTA.GraphicsEventArgs e)
        {
            UpdateHealthCore();
            HandleDeadeye();
            ManageDetachedObjects();
            UpdateLootScanStatus(e);
            DrawLootNotifications(e);
            if ((DateTime.Now - lastNpcScanTime).TotalSeconds >= 0.5)
            {
                CheckForLootableNpcs();
                lastNpcScanTime = DateTime.Now;
            }
            if (pendingWantedTime.HasValue && DateTime.Now >= pendingWantedTime.Value)
            {
                if (Player.WantedLevel == 0)
                {
                    Player.WantedLevel = 1;
                    DisplayMessage("Witness reported your crime!", MessageType.Warning);
                }
                pendingWantedTime = null; 
            }
            bool isItemMessageActive = !string.IsNullOrEmpty(currentMessage) && (DateTime.Now - messageStartTime).TotalMilliseconds < messageDuration;

            if (!string.IsNullOrEmpty(currentMessage) &&
                (DateTime.Now - messageStartTime).TotalMilliseconds < messageDuration)
            {
                Function.Call("PRINT_STRING_WITH_LITERAL_STRING_NOW", "STRING", currentMessage, messageDuration, 1);
            }

            if (inventoryOpen)
            {
                DrawInventoryMenu(e);

                string status = GetHealthCoreStatus();
                Color healthColor = GetHealthCoreColor();
                Color deadeyeColor = GetDeadeyeCoreColor();
                string backpackInfo = "Backpack: " + (currentBackpack == "none" ? "Default" : itemDatabase[currentBackpack].Name);

                int gameWidth = Game.Resolution.Width;
                int gameHeight = Game.Resolution.Height;

                int yPosition = (int)(gameHeight * 0.05);
                int xPosition = (int)(gameWidth * 0.5 - 100);
                e.Graphics.DrawText(string.Format("{0}: {1}", status, healthCore), xPosition, yPosition, healthColor);

                e.Graphics.DrawText(string.Format("|    Deadeye: {0}", deadeyeCore), xPosition + 100, yPosition, deadeyeColor);

                e.Graphics.DrawText(backpackInfo, 30, 20, Color.OrangeRed);
            }

            if (isShopOpen)
            {
                Ped playerPed = Player.Character;
                if (!Function.Call<bool>("IS_PLAYER_FREE_FOR_AMBIENT_TASK", Player.Index))
                {
                    ToggleShop();
                }
                else
                {
                    DrawShopMenu(e);
                }
            }
            bool inCutscene = !Game.LocalPlayer.CanControlCharacter && !inventoryOpen && !isShopOpen;

            if (!inCutscene && (areCoresVisible || inventoryOpen || isShopOpen))
            {
                DrawCoreBars(e);
            }

            if (isDeadeyeActive)
            {
                Color deadeyeUiColor = GetDeadeyeCoreColor();
                int screenWidth = Game.Resolution.Width;
                e.Graphics.DrawText(string.Format("Deadeye: {0}", deadeyeCore), screenWidth / 2 - 50, 30, deadeyeUiColor);
            }

            if (shouldShowStatusChange && (DateTime.Now - lastStatusChangeTime).TotalSeconds < 3 && !isItemMessageActive)
            {
                string statusChangeMessage = "~r~Status: " + GetHealthCoreStatus();
                Function.Call("PRINT_STRING_WITH_LITERAL_STRING_NOW", "STRING", statusChangeMessage, 3000, 1);
            }
            else if (shouldShowStatusChange)
            {
                shouldShowStatusChange = false;
            }
            if (shouldShowLootPrompt)
            {
                int screenWidth = Game.Resolution.Width;
                int screenHeight = Game.Resolution.Height;
                e.Graphics.DrawText("Press X to Loot/Rob", (int)(screenWidth * 0.5f) - 70, (int)(screenHeight * 0.8f), Color.White);
            }
            CheckWantedChanceReset();
            if (DateTime.Now < hashDisplayEndTime && !string.IsNullOrEmpty(hashToDisplay))
            {
                e.Graphics.DrawText(hashToDisplay, 0.45f, 0.05f, Color.WhiteSmoke);
            }
        }

        // Checks if 24 game hours have passed and resets the wanted level heat
        private void CheckWantedChanceReset()
        {
            if (firstSellTime == -1 || wantedChance == 0) return;

            int currentTime = Function.Call<int>("GET_HOURS_OF_DAY") * 60 + Function.Call<int>("GET_MINUTES_OF_DAY");

            int timeElapsed = (currentTime - firstSellTime + 1440) % 1440;

            if (timeElapsed >= 1440)
            {
                wantedChance = 0.0f;
                firstSellTime = -1;
                DisplayMessage("Your heat has died down.", MessageType.Info);
            }
        }





        // Handles all keyboard input events for inventory, shop, and item usage
        private void FoodScript_KeyDown(object sender, GTA.KeyEventArgs e)
        {
            if (inventoryOpen)
            {
                HandleInventoryInput(e);
                return;
            }

            if (isShopOpen)
            {
                HandleShopInput(e);
                return;
            }


            if (e.Key == Keys.I && !isUsingItem && Game.LocalPlayer.CanControlCharacter)
            {
                if (Player.Character.isInWater)
                {
                    DisplayMessage("Cannot open inventory while swimming.", MessageType.Warning);
                }
                else
                {
                    ToggleInventory();
                }
            }

            if (e.Key == Keys.B && !isUsingItem && Game.LocalPlayer.CanControlCharacter && Function.Call<bool>("IS_PLAYER_FREE_FOR_AMBIENT_TASK", Player.Index))
            {
                ToggleShop();
            }
            if (e.Key == Keys.X)
            {
                if (Function.Call<bool>("IS_PED_HOLDING_AN_OBJECT", Player.Character) && Player.Character.Weapons.CurrentType.ToString() == "Misc_Object")
                {
                    StashHeldObject();
                }
                else
                {
                    if (Function.Call<bool>("IS_PLAYER_FREE_FOR_AMBIENT_TASK", Player.Index))
                    {
                        TryLootOrRob();
                    }
                }
            }
            if (e.Key == Keys.K)
            {
                if (Function.Call<bool>("IS_PED_HOLDING_AN_OBJECT", Player.Character) && Player.Character.Weapons.CurrentType.ToString() == "Misc_Object")
                {
                    GTA.Object heldObject = GTA.Native.Function.Call<GTA.Object>("GET_OBJECT_PED_IS_HOLDING", Player.Character);
                    if (heldObject != null && heldObject.Exists())
                    {
                        uint modelHash = (uint)heldObject.Model.Hash;
                        hashToDisplay = string.Format("Object Hash (uint): {0}", modelHash);
                        hashDisplayEndTime = DateTime.Now.AddSeconds(5);
                    }
                }
                else
                {
                    hashToDisplay = "Not holding any object.";
                    hashDisplayEndTime = DateTime.Now.AddSeconds(3);
                }
            }
            if (e.Key == Keys.H && !inventoryOpen && !isShopOpen && !isUsingItem)
            {
                areCoresVisible = !areCoresVisible;
                return; 
            }
            //if (e.Key == Keys.M) AddAllConsumables();
            if (Game.isGameKeyPressed(GameKey.Aim) && e.Key == Keys.MButton)
            {
                if (!isDeadeyeActive)
                {
                    ToggleDeadeye();
                }
                else
                {
                    isDeadeyeActive = false;
                    Function.Call("SET_TIME_SCALE", 1.0f);
                    StopDeadeyeSound();
                }
            }
            if (e.Key == Keys.L && !isUsingItem && Function.Call<bool>("IS_PLAYER_FREE_FOR_AMBIENT_TASK", Player.Index))
            {
                LootWorldObjects();
            }

        }
        // Returns a random consumable item key from the database
        private string GetRandomConsumableKey()
        {
            List<string> consumableKeys = new List<string>();
            foreach (var item in itemDatabase)
            {
                if (item.Value.Category == "consumables")
                {
                    consumableKeys.Add(item.Key);
                }
            }

            if (consumableKeys.Count > 0)
            {
                return consumableKeys[random.Next(consumableKeys.Count)];
            }

            return null; 
        }

        // Renders the health core and deadeye core status bars on screen
        private void DrawCoreBars(GTA.GraphicsEventArgs e)
        {
            // Increase to move right, decrease to move left
            float healthBarX = 0.25f;
            // increase to move up, decrease to move down. 
            float barBaseY = 0.85f;
            // size of the bars
            float barWidth = 0.01f;
            float barMaxHeight = 0.15f;
            // Deadeye bar is to the right of health bar with a small gap
            float deadeyeBarX = healthBarX + barWidth + 0.06f;

            Color healthColor = GetHealthCoreColor();
            float healthFillRatio = healthCore / 100.0f;
            float healthFillHeight = barMaxHeight * healthFillRatio;

            Function.Call("DRAW_RECT", healthBarX, barBaseY, barWidth, barMaxHeight, 100, 100, 100, 200);

            if (healthCore > 0)
            {
                // calculate Y position so that the fill is anchored to the bottom and grows upwards
                float healthFillY = (barBaseY + (barMaxHeight / 2)) - (healthFillHeight / 2);
                Function.Call("DRAW_RECT", healthBarX, healthFillY, barWidth, healthFillHeight, healthColor.R, healthColor.G, healthColor.B, 255);
            }

            // --- draw Deadeye Core ---
            Color deadeyeColor = GetDeadeyeCoreColor();
            float deadeyeFillRatio = deadeyeCore / 100.0f;
            float deadeyeFillHeight = barMaxHeight * deadeyeFillRatio;

            Function.Call("DRAW_RECT", deadeyeBarX, barBaseY, barWidth, barMaxHeight, 100, 100, 100, 200);

            if (deadeyeCore > 0)
            {
                // calculate Y position so that the fill is anchored to the bottom and grows upwards
                float deadeyeFillY = (barBaseY + (barMaxHeight / 2)) - (deadeyeFillHeight / 2);
                Function.Call("DRAW_RECT", deadeyeBarX, deadeyeFillY, barWidth, deadeyeFillHeight, deadeyeColor.R, deadeyeColor.G, deadeyeColor.B, 255);
            }

            int screenWidth = Game.Resolution.Width;
            int screenHeight = Game.Resolution.Height;

            string healthStatusText = GetHealthCoreStatus();
            float statusTextX = (screenWidth * healthBarX) - (healthStatusText.Length * 3.5f);
            
            e.Graphics.DrawText(healthStatusText, statusTextX, screenHeight * (barBaseY + barMaxHeight / 2) + 5, healthColor);
            e.Graphics.DrawText("Deadeye", screenWidth * deadeyeBarX - 25, screenHeight * (barBaseY + barMaxHeight / 2) + 5, deadeyeColor);
        }
        // Finds the nearest lootable object from the cached nearby objects list
        private GTA.Object GetNearestLootableObjectFromCache(Vector3 playerPos)
        {
            GTA.Object nearest = null;
            float minDist = float.MaxValue;
            List<GTA.Object> toRemove = new List<GTA.Object>();

            foreach (var obj in nearbyLootableObjects)
            {
                if (obj == null || !obj.Exists())
                {
                    toRemove.Add(obj);
                    continue;
                }
                float dist = playerPos.DistanceTo(obj.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = obj;
                }
            }
            foreach (var obj in toRemove) nearbyLootableObjects.Remove(obj);
            return nearest;
        }

        // Gets the item name based on its model hash for displaying loot prompts
        private string GetItemNameFromHash(uint hash)
        {
            switch (hash)
            {
                case 3016013513: return itemDatabase["small_cola"].Name;
                case 781334236: return itemDatabase["big_sprite"].Name;
                case 3384484448: case 1195548017: case 889583860: return "Consumable";
                case 989765982: case 2043092722: case 2388036357: case 3773306759: return itemDatabase["scrap"].Name;
                case 1702619648: return "Cluckin' Bell Bag";
                case 1232511449:
                case 186910673:
                case 862345301:
                case 1167064232:
                case 1338577182:
                case 1643885955:
                case 1926649656: return itemDatabase["backpack_material"].Name;
            }
            foreach (var item in itemDatabase)
            {
                if (item.Value.ModelHashes != null && item.Value.ModelHashes.Contains(hash))
                {
                    return item.Value.Name;
                }
            }
            return "Item";
        }

        // Processes the looting of a target object and adds items to inventory
        private void ProcessLoot(GTA.Object targetObject)
        {
            uint foundHash = unchecked((uint)targetObject.Model.Hash);

            Dictionary<string, int> lootedItems = new Dictionary<string, int>();

            Action<string, int> AddLoot = (key, qty) =>
            {
                if (inventory.ContainsKey(key) && inventory[key] < inventoryCapacity)
                {
                    int space = inventoryCapacity - inventory[key];
                    int actualAdd = Math.Min(qty, space);

                    if (actualAdd > 0)
                    {
                        inventory[key] += actualAdd;
                        if (lootedItems.ContainsKey(key)) lootedItems[key] += actualAdd;
                        else lootedItems.Add(key, actualAdd);
                    }
                    else
                    {
                        DisplayMessage("Inventory full for " + itemDatabase[key].Name, MessageType.Warning);
                    }
                }
            };

            string itemKeyToAdd = null;

            switch (foundHash)
            {
                case 3016013513: itemKeyToAdd = "small_cola"; break;
                case 781334236: itemKeyToAdd = "big_sprite"; break;
                case 3384484448: case 1195548017: case 889583860: itemKeyToAdd = GetRandomConsumableKey(); break;
                case 989765982: case 2043092722: case 2388036357: case 3773306759: itemKeyToAdd = "scrap"; break;
                case 578714976: case 818608005: case 3877111192: itemKeyToAdd = "pizza"; break;
                case 2568528009: case 4131702693: itemKeyToAdd = "burger"; break;
                case 1324098947: itemKeyToAdd = "burgershot_drink"; break;

                case 2038290433: AddLoot("big_sprite", 1); break;

                case 1702619648: // Cluckin' Bell Set
                    AddLoot("clucking_drink", 1);
                    AddLoot("chicken_nugget", 1);
                    AddLoot("fried_potato", 1);
                    break;

                case 439406250: // Nugget + Scrap
                    AddLoot("chicken_nugget", 1);
                    AddLoot("scrap", 1);
                    break;

                case 2017313314: // Burger + Drink
                    AddLoot("burger", 1);
                    AddLoot("burgershot_drink", 1);
                    break;

                case 848866570: AddLoot("big_cola", 3); break;

                case 583751396: AddLoot("premium_cigarette", 3); break;

                case 1397547789: AddLoot("cigarette", 3); break;

                case 2195914125: // Mixed Cigarette
                    AddLoot("cigarette", 2);
                    AddLoot("premium_cigarette", 1);
                    break;

                case 486832344: // Clucking + Random
                    AddLoot("clucking_drink", 1);
                    string[] randomItems = { "fried_potato", "burger", "chicken_nugget" };
                    AddLoot(randomItems[random.Next(randomItems.Length)], 1);
                    break;

                case 4256001010:
                    AddLoot("burger", 1);
                    AddLoot("burgershot_drink", 1);
                    break;

                case 1864871926: // Full Meal
                    AddLoot("burger", 1);
                    AddLoot("fried_potato", 1);
                    AddLoot("burgershot_drink", 1);
                    break;

                case 1232511449:
                case 186910673:
                case 862345301:
                case 1167064232:
                case 1338577182:
                case 1643885955:
                case 1926649656:
                    if (inventoryCapacity < 100) itemKeyToAdd = "backpack_material";
                    else DisplayMessage("~y~You don't need this item", MessageType.Info);
                    break;
            }

            if (itemKeyToAdd == null && lootedItems.Count == 0)
            {
                foreach (var item in itemDatabase)
                {
                    if (item.Value.ModelHashes != null && item.Value.ModelHashes.Contains(foundHash))
                    {
                        itemKeyToAdd = item.Key;
                        break;
                    }
                }
            }

            if (itemKeyToAdd != null)
            {
                AddLoot(itemKeyToAdd, 1);
            }

            if (lootedItems.Count > 0)
            {
                foreach (var kvp in lootedItems)
                {
                    ShowLootNotification(kvp.Key, kvp.Value);
                }
            }
            else if (itemKeyToAdd == null) 
            {
                DisplayMessage("~r~Found nothing useful...", MessageType.Warning);
            }
        }
        // Initiates area looting or continues looting nearby objects within scan radius
        private void LootWorldObjects()
        {
            Ped playerPed = Player.Character;

            if (isLootScanActive)
            {
                if (nearbyLootableObjects.Count == 0)
                {
                    isLootScanActive = false;
                    isUsingItem = false;
                    return;
                }

                isUsingItem = true; 
                GTA.Object targetObject = GetNearestLootableObjectFromCache(playerPed.Position);

                if (targetObject == null)
                {
                    if (nearbyLootableObjects.Count == 0) 
                        isLootScanActive = (nearbyLootableObjects.Count > 0);
                    isUsingItem = false;
                    return;
                }

                float distance = playerPed.Position.DistanceTo(targetObject.Position);

                if (distance <= 1.0f)
                {
                    playerPed.Task.TurnTo(targetObject.Position);
                    Wait(600); 
                }
                else
                {
                    playerPed.Task.GoTo(targetObject.Position);

                    DateTime startTime = DateTime.Now;
                    while (playerPed.Position.DistanceTo(targetObject.Position) > 1.5f)
                    {
                        Wait(100);
                        if ((DateTime.Now - startTime).TotalSeconds > 5)
                        {
                            playerPed.Task.ClearAll(); 
                            DisplayMessage("Cannot reach the target object.", MessageType.Warning);
                            isUsingItem = false; 
                            return; 
                        }
                    }
                    playerPed.Task.ClearAll();
                }


                Function.Call("TASK_PLAY_ANIM", playerPed, "pickup_low", "pickup_object", 8.0f, 0, 0, 0, false);
                Wait(1000);

                ProcessLoot(targetObject);

                nearbyLootableObjects.Remove(targetObject);
                targetObject.NoLongerNeeded();
                targetObject.Delete();

                SaveInventoryData();
                playerPed.Task.ClearAll();
                isUsingItem = false;

                if (nearbyLootableObjects.Count == 0)
                {
                    isLootScanActive = false;
                }
            }
            else
            {
                isUsingItem = true;
                lootScanCenter = playerPed.Position;
                nearbyLootableObjects.Clear();

                Function.Call("TASK_PLAY_ANIM_WITH_ADVANCED_FLAGS", playerPed, "search_letterbox", "amb@postman_idles", 8.0f, false, false, false, false, false, false, false, 1500);

                uint[] specialHashes = {
                    3384484448, 1195548017, 889583860, 989765982, 2043092722, 2388036357, 3773306759,
                    578714976, 818608005, 3877111192, 2568528009, 4131702693, 1324098947, 2038290433,
                    1702619648, 439406250, 2017313314, 848866570, 583751396, 1397547789, 2195914125,
                    486832344, 4256001010, 1864871926, 1232511449, 186910673, 862345301, 1167064232,
                    1338577182, 1643885955, 1926649656, 3016013513, 781334236
                };

                foreach (GTA.Object obj in World.GetAllObjects())
                {
                    if (obj == null || !obj.Exists()) continue;
                    float distance = obj.Position.DistanceTo(lootScanCenter);
                    if (distance > LOOT_SCAN_RADIUS) continue; 
                    if (spawnedItems.Contains(obj) || objectsToDelete.ContainsKey(obj)) continue;
                    if (Function.Call<bool>("IS_OBJECT_ATTACHED", obj)) continue;

                    uint objHash = unchecked((uint)obj.Model.Hash);
                    if (objHash == 1069950328) continue;
                    bool isLootable = false;

                    foreach (uint hash in specialHashes) { if (hash == objHash) { isLootable = true; break; } }
                    if (!isLootable)
                    {
                        foreach (var item in itemDatabase)
                        {
                            if (item.Value.ModelHashes != null && item.Value.ModelHashes.Contains(objHash))
                            { isLootable = true; break; }
                        }
                    }
                    if (isLootable) nearbyLootableObjects.Add(obj);
                }

                Wait(1500); 

                if (nearbyLootableObjects.Count == 0)
                {
                    DisplayMessage("~r~Found nothing useful...", MessageType.Warning);
                    playerPed.Task.ClearAll();
                    isUsingItem = false;
                    isLootScanActive = false;
                    return;
                }

                isLootScanActive = true;
                DisplayMessage(string.Format("~g~Found {0} lootable item(s).", nearbyLootableObjects.Count), MessageType.Info);

                isUsingItem = false; 

            }
        }
        // Updates the status of the active loot scan and displays prompts
        private void UpdateLootScanStatus(GTA.GraphicsEventArgs e)
        {
            if (!isLootScanActive) return;

            Ped playerPed = Player.Character;
            if (playerPed == null || !playerPed.Exists() || isUsingItem || inventoryOpen || isShopOpen)
            {
                if (playerPed == null || !playerPed.Exists())
                {
                    isLootScanActive = false;
                    nearbyLootableObjects.Clear();
                }
                return;
            }

            float distanceToCenter = playerPed.Position.DistanceTo(lootScanCenter);
            if (distanceToCenter > LOOT_ABANDON_RADIUS)
            {
                isLootScanActive = false;
                nearbyLootableObjects.Clear();
                DisplayMessage("~y~Moved too far, stopped looting.", MessageType.Warning);
                return;
            }

            if (nearbyLootableObjects.Count > 0)
            {
                GTA.Object nearestObj = GetNearestLootableObjectFromCache(playerPed.Position);
                if (nearestObj != null && nearestObj.Exists())
                {
                    string itemName = GetItemNameFromHash(unchecked((uint)nearestObj.Model.Hash));

                    int screenWidth = Game.Resolution.Width;
                    int screenHeight = Game.Resolution.Height;
                    e.Graphics.DrawText(string.Format("Press L to Loot {0}", itemName), (int)(screenWidth * 0.5f) - 90, (int)(screenHeight * 0.85f), Color.White);
                }
                else if (nearbyLootableObjects.Count == 0)
                {
                    isLootScanActive = false;
                }
            }
        }
        // Stashes a held object into the inventory with animation
        private void StashHeldObject()
        {
            GTA.Object heldObject = GTA.Native.Function.Call<GTA.Object>("GET_OBJECT_PED_IS_HOLDING", Player.Character);

            if (heldObject != null && heldObject.Exists())
            {
                uint heldObjectHash = (uint)heldObject.Model.Hash;
                string foundItemKey = null;

                switch (heldObjectHash)
                {
                    case 583751396: // Premium Cigarette Pack
                        if (inventory["premium_cigarette"] <= inventoryCapacity - 3)
                        {
                            inventory["premium_cigarette"] += 3;
                            CompleteStashAction("~r~Premium Cigarette~w~ x3", heldObject);
                        }
                        else
                        {
                            DisplayMessage("Premium Cigarette inventory is full!", MessageType.Warning);
                        }
                        return; 

                    case 1397547789: // Regular Cigarette Pack
                        if (inventory["cigarette"] <= inventoryCapacity - 3)
                        {
                            inventory["cigarette"] += 3;
                            CompleteStashAction("~w~Cigarette~w~ x3", heldObject);
                        }
                        else
                        {
                            DisplayMessage("Cigarette inventory is full!", MessageType.Warning);
                        }
                        return;

                    case 2195914125: // Mixed Cigarette Pack
                        bool canAddCigarettes = inventory["cigarette"] <= inventoryCapacity - 2;
                        bool canAddPremium = inventory["premium_cigarette"] <= inventoryCapacity - 1;
                        string stashedMessage = "";

                        if (canAddCigarettes && canAddPremium)
                        {
                            inventory["cigarette"] += 2;
                            inventory["premium_cigarette"] += 1;
                            stashedMessage = "~w~Cigarette~w~ x2 + ~r~Premium Cigarette~w~ x1";
                        }
                        else if (canAddCigarettes && !canAddPremium)
                        {
                            inventory["cigarette"] += 2;
                            stashedMessage = "~w~Cigarette~w~ x2 + ~r~Premium Cigarette (Full)~w~";
                        }
                        else if (!canAddCigarettes && canAddPremium)
                        {
                            inventory["premium_cigarette"] += 1;
                            stashedMessage = "~w~Cigarette (Full)~w~ + ~r~Premium Cigarette~w~ x1";
                        }
                        else
                        {
                            DisplayMessage("Both Cigarette & Premium Cigarette inventories are full!", MessageType.Warning);
                            return; 
                        }
                        CompleteStashAction(stashedMessage, heldObject);
                        return; 

                    case 2568528009: case 4131702693: foundItemKey = "burger"; break;
                    case 989765982: case 2043092722: case 2388036357: case 3773306759: foundItemKey = "scrap"; break;
                    case 578714976: case 818608005: case 3877111192: foundItemKey = "pizza"; break;
                    case 1324098947: foundItemKey = "burgershot_drink"; break;
                    case 1232511449:
                    case 186910673:
                    case 862345301:
                    case 1167064232:
                    case 1338577182:
                    case 1643885955:
                    case 1926649656:
                        if (inventoryCapacity >= 100)
                        {
                            DisplayMessage("You don't need this item", MessageType.Info);
                            return;
                        }
                        foundItemKey = "backpack_material";
                        break;
                }

                if (foundItemKey == null)
                {
                    foreach (var kvp in itemDatabase)
                    {
                        if (kvp.Value.ModelHashes != null && kvp.Value.ModelHashes.Contains(heldObjectHash))
                        {
                            foundItemKey = kvp.Key;
                            break;
                        }
                    }
                }

                if (foundItemKey != null)
                {
                    if (inventory[foundItemKey] < inventoryCapacity)
                    {
                        inventory[foundItemKey]++;
                        string colorCode = GetColorCodeForCategory(itemDatabase[foundItemKey].Category);
                        CompleteStashAction(string.Format("{0}{1}~w~", colorCode, itemDatabase[foundItemKey].Name), heldObject);
                    }
                    else
                    {
                        DisplayMessage(string.Format("Cannot stash {0}, inventory is full!", itemDatabase[foundItemKey].Name), MessageType.Warning);
                    }
                }
                else
                {
                    DisplayMessage("This item cannot be stashed.", MessageType.Error);
                }
            }
        }
        // Completes the stashing action and displays confirmation
        private void CompleteStashAction(string message, GTA.Object objectToStash)
        {
            SaveInventoryData();
            Function.Call("TASK_PLAY_ANIM_SECONDARY_UPPER_BODY", Player.Character, "m_putwalletaway_chest", "amb@atm", 8.0f, 0, 0, 0, false, false, false);
            Wait(1000);

            if (objectToStash != null && objectToStash.Exists())
            {
                objectToStash.NoLongerNeeded();
                objectToStash.Delete();
            }

            DisplayMessage("Stashed " + message + ".", MessageType.Success);
        }
        // Toggles the shop menu on or off
        private void ToggleShop()
        {
            if (!textureLoaded) LoadTextures();
            isShopOpen = !isShopOpen;
            if (isShopOpen)
            {

                currentShopState = ShopState.Main;
                shoppingCart.Clear();
                UpdateShopTotals();
                Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT");
                Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT");
            }
            else
            {
                shoppingCart.Clear();
                Function.Call("PLAY_SOUND_FRONTEND", -1, "BACK_ALT_1", "FRONTEND_MENU", 0);
            }
        }
        // Builds a formatted string describing the effects and benefits of using an item
        private string BuildEffectString(ItemInfo item)
        {
            List<string> effects = new List<string>();

            if (item.Category == "deadeye")
            {
                if (item.DeadeyeRestore != 0) effects.Add(string.Format("{0}{1} Dead Eye", (item.DeadeyeRestore > 0 ? "+" : ""), item.DeadeyeRestore));
                if (item.HealthCoreRestore != 0) effects.Add(string.Format("{0}{1} Health Core", (item.HealthCoreRestore > 0 ? "+" : ""), item.HealthCoreRestore));

                return string.Join("\n", effects.ToArray());
            }

            if (item.HealthCoreRestore != 0) effects.Add(string.Format("{0} Health Core", (item.HealthCoreRestore > 0 ? "+" : "") + item.HealthCoreRestore));
            if (item.DeadeyeRestore != 0) effects.Add(string.Format("{0} Dead Eye", (item.DeadeyeRestore > 0 ? "+" : "") + item.DeadeyeRestore));
            if (item.HealthRestore > 0) effects.Add(string.Format("+{0} HP", item.HealthRestore));
            if (item.ArmorRestore > 0) effects.Add(string.Format("+{0} Armor", item.ArmorRestore));

            if (effects.Count > 0)
            {
                return string.Format("({0})", string.Join(", ", effects.ToArray()));
            }
            return "";
        }

        // Routes shop menu rendering to the appropriate page
        private void DrawShopMenu(GTA.GraphicsEventArgs e)
        {
            switch (currentShopState)
            {
                case ShopState.Main:
                    DrawShopMainMenu(e);
                    break;
                case ShopState.Consumables:
                    DrawShopCategoryPage(e, "Consumables", "1. Page 1 (7-19 Health Core)\n2. Page 2 (20-28 Health Core)\n3. Page 3 (29-37 Health Core)\n4. Page 4 (38-50 Health Core)\n0. Back");
                    break;
                case ShopState.Consumables_Page1:
                    DrawShopItemsPage(e, page1_items, "Consumables - Page 1", Color.Yellow);
                    break;
                case ShopState.Consumables_Page2:
                    DrawShopItemsPage(e, page2_items, "Consumables - Page 2", Color.Yellow);
                    break;
                case ShopState.Consumables_Page3:
                    DrawShopItemsPage(e, page3_items, "Consumables - Page 3", Color.Yellow);
                    break;
                case ShopState.Consumables_Page4:
                    DrawShopItemsPage(e, page4_items, "Consumables - Page 4", Color.Yellow);
                    break;
                case ShopState.Deadeye:
                    DrawShopItemsPage(e, "deadeye", "Deadeye Items", Color.Red);
                    break;
                case ShopState.Armors:
                    DrawShopItemsPage(e, "armors", "Armors", Color.Cyan);
                    break;
                case ShopState.Medkit:
                    DrawShopItemsPage(e, "medkit", "Medkits", Color.LightGreen);
                    break;
                case ShopState.Backpack:
                    DrawShopBackpackPage(e);
                    break;
                case ShopState.SellValuables:
                    DrawShopSellValuablesPage(e);
                    break;
                case ShopState.SellScraps:
                    DrawShopSellScrapsPage(e);
                    break;
            }
        }
        // Renders a shop page displaying a grid of purchasable items
        private void DrawShopItemsPage(GTA.GraphicsEventArgs e, List<string> itemsOnPage, string title, Color titleColor)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;
            int menuWidth = 800, menuHeight = 500;
            int menuX = (gameWidth - menuWidth) / 2, menuY = (gameHeight - menuHeight) / 2;
            int itemSize = 120, itemSpacing = 25, itemsPerRow = 5, itemsPerColumn = 2;
            int gridWidth = itemsPerRow * itemSize + (itemsPerRow - 1) * itemSpacing;
            int gridStartX = menuX + (menuWidth - gridWidth) / 2, gridStartY = menuY + 100;

            e.Graphics.DrawRectangle((float)menuX / gameWidth, (float)menuY / gameHeight, (float)menuWidth / gameWidth, (float)menuHeight / gameHeight, Color.FromArgb(180, 0, 0, 0));
            e.Graphics.DrawText(string.Format("=== {0} ===", title), menuX + 250, menuY + 20, Color.White);
            e.Graphics.DrawText(string.Format("Cart: {0} items | Total: ${1}", totalItemsInCart, totalCost), menuX + 280, menuY + 50, Color.LightGreen);
            string mode = isShopInRemoveMode ? "REMOVE MODE (Backspace to toggle)" : "ADD MODE (Backspace to toggle)";
            e.Graphics.DrawText(mode, menuX + 250, menuY + 70, isShopInRemoveMode ? Color.OrangeRed : Color.LightSeaGreen);

            List<string> displayItems = itemsOnPage;
            if (currentShopState == ShopState.Consumables)
            {
                int totalPages = (int)Math.Ceiling(itemsOnPage.Count / 9.0);
                if (totalPages == 0) totalPages = 1;
                consumablesPageIndex = Math.Max(0, Math.Min(consumablesPageIndex, totalPages - 1));
                displayItems = itemsOnPage.Skip(consumablesPageIndex * 9).Take(9).ToList();
                e.Graphics.DrawText(string.Format("Page {0}/{1}", consumablesPageIndex + 1, totalPages), menuX + menuWidth - 150, menuY + menuHeight - 30, Color.White);
            }

            for (int i = 0; i < Math.Min(displayItems.Count, 9); i++)
            {
                string key = displayItems[i];
                if (!itemDatabase.ContainsKey(key)) continue;
                var item = itemDatabase[key];
                int ownedAmount = inventory.ContainsKey(key) ? inventory[key] : 0;
                int cartAmount = shoppingCart.ContainsKey(key) ? shoppingCart[key] : 0;

                int row = i / itemsPerRow, col = i % itemsPerRow;
                int itemX = gridStartX + col * (itemSize + itemSpacing);
                int itemY = gridStartY + row * (itemSize + itemSpacing);

                Color bgColor = Color.FromArgb(100, 50, 50, 50);
                if (cartAmount > 0) bgColor = Color.FromArgb(100, 0, 100, 0);
                e.Graphics.DrawRectangle((float)itemX / gameWidth, (float)itemY / gameHeight, (float)itemSize / gameWidth, (float)itemSize / gameHeight, bgColor);

                if (texturesLoaded && !string.IsNullOrEmpty(item.TextureName) && textureHandles.ContainsKey(item.TextureName))
                {
                    int imageSize = itemSize - 50;
                    int imageX = itemX + (itemSize - imageSize) / 2, imageY = itemY + 10;
                    Function.Call("DRAW_SPRITE", textureHandles[item.TextureName], (float)(imageX + imageSize / 2) / gameWidth, (float)(imageY + imageSize / 2) / gameHeight, (float)imageSize / gameWidth, (float)imageSize / gameHeight, 0.0f, 255, 255, 255, 255);
                }

                e.Graphics.DrawText(string.Format("Own: {0}", ownedAmount), itemX + 80, itemY + 5, Color.WhiteSmoke);
                if (cartAmount > 0) e.Graphics.DrawText(string.Format("Cart: {0}", cartAmount), itemX + 5, itemY + 20, Color.Yellow);

                float currentTextY = itemY + itemSize - 58;
                string priceString = string.Format("${0}", item.Price);
                int priceX = itemX + itemSize - (priceString.Length * 8) - 5;
                e.Graphics.DrawText(priceString, priceX, (int)currentTextY, Color.LightGreen);

                currentTextY += 15;
                string[] nameLines = WrapText(item.Name, 15);
                foreach (string line in nameLines)
                {
                    e.Graphics.DrawText(line, itemX + 15, (int)currentTextY, titleColor);
                    currentTextY += 13;
                }

                string[] effectLines = BuildEffectString(item).Split('\n');
                foreach (string line in effectLines)
                {
                    e.Graphics.DrawText(line, itemX + 10, (int)currentTextY, Color.LightGray);
                    currentTextY += 13;
                }

                e.Graphics.DrawText((i + 1).ToString(), itemX + 5, itemY + 5, Color.Yellow);
            }

            e.Graphics.DrawText("0. Back", menuX + 20, menuY + menuHeight - 30, Color.White);
            if (currentShopState != ShopState.Consumables)
            {
                e.Graphics.DrawText("<-- Prev | Next -->", menuX + menuWidth - 150, menuY + menuHeight - 30, Color.White);
            }
        }

        // Renders a shop page for items filtered by category
        private void DrawShopItemsPage(GTA.GraphicsEventArgs e, string category, string title, Color textColor)
        {
            List<string> itemsOnPage = GetItemsForCategory(category);
            DrawShopItemsPage(e, itemsOnPage, title, textColor);
        }
        // Gets all purchasable items in a specified category
        private List<string> GetItemsForCategory(string category)
        {
            List<string> items = new List<string>();
            foreach (var kvp in itemDatabase)
            {
                if (kvp.Value.Category == category && kvp.Value.Price > 0)
                {
                    items.Add(kvp.Key);
                }
            }
            return items;
        }
        // Renders the backpack upgrade page in the shop
        private void DrawShopBackpackPage(GTA.GraphicsEventArgs e)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;
            int menuWidth = 800, menuHeight = 500;
            int menuX = (gameWidth - menuWidth) / 2, menuY = (gameHeight - menuHeight) / 2;
            int itemSize = 120, itemSpacing = 25, itemsPerRow = 5;
            int gridWidth = itemsPerRow * itemSize + (itemsPerRow - 1) * itemSpacing;
            int gridStartX = menuX + (menuWidth - gridWidth) / 2, gridStartY = menuY + 80;

            e.Graphics.DrawRectangle((float)menuX / gameWidth, (float)menuY / gameHeight, (float)menuWidth / gameWidth, (float)menuHeight / gameHeight, Color.FromArgb(180, 0, 0, 0));
            e.Graphics.DrawText("=== UPGRADE BACKPACK ===", menuX + 250, menuY + 30, Color.Orange);

            List<string> displayableBackpacks = GetDisplayableBackpacks();
            if (displayableBackpacks.Count == 0)
            {
                e.Graphics.DrawText("You already have the best backpack.", menuX + 280, gridStartY + 100, Color.White);
            }

            for (int i = 0; i < displayableBackpacks.Count; i++)
            {
                string key = displayableBackpacks[i];
                var item = itemDatabase[key];
                int capacity = GetBackpackCapacityForKey(key);

                int row = i / itemsPerRow, col = i % itemsPerRow;
                int itemX = gridStartX + col * (itemSize + itemSpacing);
                int itemY = gridStartY + row * (itemSize + itemSpacing);

                bool inCart = shoppingCart.ContainsKey(key);
                e.Graphics.DrawRectangle((float)itemX / gameWidth, (float)itemY / gameHeight, (float)itemSize / gameWidth, (float)itemSize / gameHeight, inCart ? Color.FromArgb(100, 0, 100, 0) : Color.FromArgb(100, 80, 70, 20));

                if (texturesLoaded && !string.IsNullOrEmpty(item.TextureName) && textureHandles.ContainsKey(item.TextureName))
                {
                    int imageSize = itemSize - 50;
                    int imageX = itemX + (itemSize - imageSize) / 2, imageY = itemY + 5;
                    Function.Call("DRAW_SPRITE", textureHandles[item.TextureName], (float)(imageX + imageSize / 2) / gameWidth, (float)(imageY + imageSize / 2) / gameHeight, (float)imageSize / gameWidth, (float)imageSize / gameHeight, 0.0f, 255, 255, 255, 255);
                }

                string[] nameLines = WrapText(item.Name, 15);
                float currentTextY = itemY + itemSize - 45;

                foreach (string line in nameLines)
                {
                    e.Graphics.DrawText(line, itemX + 10, (int)currentTextY, Color.Orange);
                    currentTextY += 13;
                }

                e.Graphics.DrawText(string.Format("{0} slots", capacity), itemX + 10, (int)currentTextY, Color.WhiteSmoke);
                e.Graphics.DrawText(string.Format("${0}", item.Price), itemX + 10, itemY + itemSize - 12, Color.LightGreen);
                e.Graphics.DrawText((i + 1).ToString(), itemX + 5, itemY + 5, Color.Yellow);
            }

            e.Graphics.DrawText("0. Back", menuX + 20, menuY + menuHeight - 30, Color.White);
        }

        // Renders a shop category selection page with menu options
        private void DrawShopCategoryPage(GTA.GraphicsEventArgs e, string title, string options)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;

            int startX = (int)(gameWidth * 0.05f);
            int startY = (int)(gameHeight * 0.3f);
            int yIncrement = 20;

            var menuLines = new List<string> { string.Format("=== {0} ===", title.ToUpper()) };
            menuLines.AddRange(options.Split('\n'));

            int rectWidth = 450;
            int rectHeight = (yIncrement * menuLines.Count) + 10;
            e.Graphics.DrawRectangle(
                (int)(startX - 10) / gameWidth,
                (int)(startY - 15) / gameHeight,
                (int)rectWidth / gameWidth,
                (int)rectHeight / gameHeight,
                Color.FromArgb(180, 0, 0, 0)
            );

            int currentY = startY;
            foreach (string line in menuLines)
            {
                e.Graphics.DrawText(line, startX, currentY, Color.White);
                currentY += yIncrement;
            }
        }

        /// Retrieves a list of available backpack upgrades that have higher capacity than the current backpack.
        private List<string> GetDisplayableBackpacks()
        {
            List<string> displayable = new List<string>();
            int currentCapacity = inventoryCapacity;

            if (currentCapacity < 20) displayable.Add("small_backpack");
            if (currentCapacity < 30) displayable.Add("normal_backpack");
            if (currentCapacity < 50) displayable.Add("big_backpack");
            if (currentCapacity < 100) displayable.Add("magic_backpack");


            return displayable;
        }

        // Routes shop input handling to the appropriate method
        private void HandleShopInput(GTA.KeyEventArgs e)
        {
            if (e.Key == Keys.B)
            {
                ToggleShop();
                return;
            }
            if (currentShopState >= ShopState.Consumables_Page1 && currentShopState <= ShopState.Consumables_Page4)
            {

                if (e.Key == Keys.Right)
                {
                    currentShopState++;
                    if (currentShopState > ShopState.Consumables_Page4) currentShopState = ShopState.Consumables_Page1; 
                    Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    return;
                }
                if (e.Key == Keys.Left)
                {
                    currentShopState--;
                    if (currentShopState < ShopState.Consumables_Page1) currentShopState = ShopState.Consumables_Page4; 
                    Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    return;
                }
            }

            switch (currentShopState)
            {
                case ShopState.Main:
                    HandleShopMainInput(e);
                    break;
                case ShopState.Consumables:
                    HandleShopConsumablesMenuInput(e);
                    break;
                case ShopState.Consumables_Page1:
                    HandleShopItemsPageInput(e, page1_items, ShopState.Consumables);
                    break;
                case ShopState.Consumables_Page2:
                    HandleShopItemsPageInput(e, page2_items, ShopState.Consumables);
                    break;
                case ShopState.Consumables_Page3:
                    HandleShopItemsPageInput(e, page3_items, ShopState.Consumables);
                    break;
                case ShopState.Consumables_Page4:
                    HandleShopItemsPageInput(e, page4_items, ShopState.Consumables);
                    break;
                case ShopState.Deadeye:
                    HandleShopItemsPageInput(e, "deadeye", ShopState.Main);
                    break;
                case ShopState.Armors:
                    HandleShopItemsPageInput(e, "armors", ShopState.Main);
                    break;
                case ShopState.Medkit:
                    HandleShopItemsPageInput(e, "medkit", ShopState.Main);
                    break;
                case ShopState.Backpack:
                    HandleShopBackpackInput(e);
                    break;
                case ShopState.SellValuables:
                    HandleShopSellValuablesInput(e);
                    break;
                case ShopState.SellScraps: 
                    HandleShopSellScrapsInput(e);
                    break;
            }
        }

        // Handles keyboard input for the shop main menu
        private void HandleShopMainInput(GTA.KeyEventArgs e)
        {
            if (e.Key == Keys.Enter && totalItemsInCart > 0) { ConfirmPurchase(); return; }
            if (e.Key == Keys.D0 || e.Key == Keys.NumPad0 || e.Key == Keys.Escape) { ToggleShop(); return; }

            switch (e.Key)
            {

                case Keys.D1: case Keys.NumPad1: currentShopState = ShopState.Consumables; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D2: case Keys.NumPad2: currentShopState = ShopState.Deadeye; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D3: case Keys.NumPad3: currentShopState = ShopState.Armors; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D4: case Keys.NumPad4: currentShopState = ShopState.Medkit; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D5: case Keys.NumPad5: currentShopState = ShopState.Backpack; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D6: case Keys.NumPad6: currentShopState = ShopState.SellValuables; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D7: case Keys.NumPad7: currentShopState = ShopState.SellScraps; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
            }
        }

        // Handles keyboard input for the shop consumables category menu
        private void HandleShopConsumablesMenuInput(GTA.KeyEventArgs e)
        {
            if (e.Key == Keys.D0 || e.Key == Keys.NumPad0 || e.Key == Keys.Escape) { currentShopState = ShopState.Main; Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_RIGHT"); return; }
            switch (e.Key)
            {
                case Keys.D1: case Keys.NumPad1: currentShopState = ShopState.Consumables_Page1; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D2: case Keys.NumPad2: currentShopState = ShopState.Consumables_Page2; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D3: case Keys.NumPad3: currentShopState = ShopState.Consumables_Page3; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D4: case Keys.NumPad4: currentShopState = ShopState.Consumables_Page4; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
            }
        }

        // Handles keyboard input for shop item pages
        private void HandleShopItemsPageInput(GTA.KeyEventArgs e, List<string> itemsOnPage, ShopState backState)
        {
            if (e.Key == Keys.D0 || e.Key == Keys.NumPad0)
            {
                isShopInRemoveMode = false; 
                currentShopState = backState;
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_LEFT");
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_RIGHT");
                return;
            }

            if (e.Key == Keys.Back)
            {
                isShopInRemoveMode = !isShopInRemoveMode;
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_PROMPT_TICK_LEFT");
                return;
            }

            int itemIndex = GetKeyPressedNumber(e) - 1;

            if (itemIndex >= 0 && itemIndex < itemsOnPage.Count)
            {
                string selectedItem = itemsOnPage[itemIndex];

                int currentAmount = shoppingCart.ContainsKey(selectedItem) ? shoppingCart[selectedItem] : 0;
                int inventoryAmount = inventory.ContainsKey(selectedItem) ? inventory[selectedItem] : 0;

                if (isShopInRemoveMode) 
                {
                    if (currentAmount > 0)
                    {
                        shoppingCart[selectedItem] = currentAmount - 1;
                        if (shoppingCart[selectedItem] == 0)
                        {
                            shoppingCart.Remove(selectedItem);
                        }
                        Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    }
                }
                else 
                {
                    if (currentAmount + inventoryAmount < inventoryCapacity)
                    {
                        shoppingCart[selectedItem] = currentAmount + 1;
                        Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    }
                    else
                    {
                        Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_ERROR_LEFT");
                    }
                }

                UpdateShopTotals();
            }
        }

        // Determines if the current shop state requires quantity selection
        private bool ShouldSelectQuantity()
        {
            return false; 
        }

        // Handles keyboard input for category-based shop item pages
        private void HandleShopItemsPageInput(GTA.KeyEventArgs e, string category, ShopState backState)
        {
            if (e.Key == Keys.D0 || e.Key == Keys.NumPad0)
            {
                isShopInRemoveMode = false; 
                currentShopState = backState;
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_LEFT");
                return;
            }

            if (e.Key == Keys.Back)
            {
                isShopInRemoveMode = !isShopInRemoveMode;
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_PROMPT_TICK_LEFT");
                return;
            }

            List<string> itemsOnPage = new List<string>();
            foreach (var kvp in itemDatabase)
            {
                if (kvp.Value.Category == category && kvp.Value.Price > 0)
                {
                    itemsOnPage.Add(kvp.Key);
                }
            }

            int itemIndex = GetKeyPressedNumber(e) - 1;
            if (itemIndex >= 0 && itemIndex < itemsOnPage.Count)
            {
                string selectedItem = itemsOnPage[itemIndex];
                int currentAmount = shoppingCart.ContainsKey(selectedItem) ? shoppingCart[selectedItem] : 0;
                int inventoryAmount = inventory.ContainsKey(selectedItem) ? inventory[selectedItem] : 0;

                if (isShopInRemoveMode) 
                {
                    if (currentAmount > 0)
                    {
                        shoppingCart[selectedItem] = currentAmount - 1;
                        if (shoppingCart[selectedItem] == 0) shoppingCart.Remove(selectedItem);
                        Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    }
                }
                else 
                {
                    if (currentAmount + inventoryAmount < inventoryCapacity)
                    {
                        shoppingCart[selectedItem] = currentAmount + 1;
                        Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    }
                }
                UpdateShopTotals();
            }
        }

        // Handles keyboard input for the shop backpack upgrade page
        private void HandleShopBackpackInput(GTA.KeyEventArgs e)
        {
            if (e.Key == Keys.D0 || e.Key == Keys.NumPad0 || e.Key == Keys.Escape) { currentShopState = ShopState.Main; Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_RIGHT"); return; }
            Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT");

            List<string> displayableBackpacks = GetDisplayableBackpacks();
            int itemIndex = GetKeyPressedNumber(e) - 1;

            if (itemIndex >= 0 && itemIndex < displayableBackpacks.Count)
            {
                string selectedBackpack = displayableBackpacks[itemIndex];

                shoppingCart.Remove("small_backpack");
                shoppingCart.Remove("normal_backpack");
                shoppingCart.Remove("big_backpack");
                shoppingCart.Remove("magic_backpack");

                shoppingCart[selectedBackpack] = 1;
                UpdateShopTotals();
                currentShopState = ShopState.Main; 
            }
        }

        // Handles keyboard input for the sell valuables page
        private void HandleShopSellValuablesInput(GTA.KeyEventArgs e)
        {
            if (e.Key == Keys.Right)
            {
                sellValuablesPageIndex++; 
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                return;
            }
            if (e.Key == Keys.Left)
            {
                sellValuablesPageIndex--; 
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                return;
            }

            if (e.Key == Keys.Enter)
            {
                ConfirmSellValuables();
                Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT");
            }
            else if (e.Key == Keys.Back)
            {
                ConfirmSellValuablesSafe();
                Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT");
            }
            else if (e.Key == Keys.D0 || e.Key == Keys.NumPad0 || e.Key == Keys.Escape)
            {
                sellValuablesPageIndex = 0; 
                currentShopState = ShopState.Main;
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_LEFT");
            }
        }

        // Safely confirms and processes the sale of valuables
        private void ConfirmSellValuablesSafe()
        {
            int totalSaleValue = 0;
            List<string> itemsToSell = new List<string>();

            foreach (var item in inventory)
            {
                if (item.Value > 0 && itemDatabase.ContainsKey(item.Key) && itemDatabase[item.Key].Category == "valuables" &&
                    item.Key != "metal_scrap" && item.Key != "scrap")
                {
                    itemsToSell.Add(item.Key);
                    totalSaleValue += item.Value * itemDatabase[item.Key].Price;
                }
            }

            if (totalSaleValue == 0)
            {
                DisplayMessage("You have no valuables to sell!", MessageType.Warning);
                return;
            }

            int launderingFee = (int)(totalSaleValue * 0.40f);
            int finalPayout = totalSaleValue - launderingFee;

            foreach (string itemKey in itemsToSell)
            {
                inventory[itemKey] = 0;
            }

            Player.Money += finalPayout;

            DisplayMessage(string.Format("Sold valuables safely for {0}$ (after fee).", finalPayout), MessageType.Success);

            Function.Call("DO_AUTO_SAVE");
            SaveInventoryData();
            currentShopState = ShopState.Main;
        }

        // Gets the numeric value from a keyboard event
        private int GetKeyPressedNumber(GTA.KeyEventArgs e)
        {
            if (e.Key >= Keys.D1 && e.Key <= Keys.D9) return (int)e.Key - (int)Keys.D0;
            if (e.Key >= Keys.NumPad1 && e.Key <= Keys.NumPad9) return (int)e.Key - (int)Keys.NumPad0;
            return -1;
        }



        // Recalculates the shopping cart totals
        private void UpdateShopTotals()
        {
            totalCost = 0;
            totalItemsInCart = 0;
            foreach (var item in shoppingCart)
            {
                totalItemsInCart += item.Value;
                totalCost += itemDatabase[item.Key].Price * item.Value;
            }
            shippingFee = (totalItemsInCart / 2) * 5;
            totalCost += shippingFee;
        }

        // Processes the purchase of items in the shopping cart
        private void ConfirmPurchase()
        {
            int playerMoney = Player.Money;
            if (totalCost <= 0)
            {
                DisplayMessage("Your cart is empty!", MessageType.Warning);
                return;
            }
            if (playerMoney < totalCost)
            {
                DisplayMessage("Not enough money!", MessageType.Error);
                return;
            }

            isShopOpen = false; 

            Player.Money -= totalCost;
            string purchaseMessage = "";
            bool inventoryWasFull = false;

            foreach (var item in shoppingCart)
            {
                if (itemDatabase[item.Key].Category == "backpack")
                {
                    currentBackpack = item.Key;
                    UpdateInventoryCapacity();
                    purchaseMessage += string.Format("Equipped {0}. ", itemDatabase[item.Key].Name);
                }
                else
                {
                    int spaceLeft = inventoryCapacity - (inventory.ContainsKey(item.Key) ? inventory[item.Key] : 0);
                    int amountToAdd = Math.Min(item.Value, spaceLeft);

                    if (amountToAdd < item.Value)
                    {
                        inventoryWasFull = true;
                    }

                    if (amountToAdd > 0)
                    {
                        inventory[item.Key] += amountToAdd;
                    }
                }
            }

            purchaseMessage += string.Format("Purchase complete! You paid {0}$.", totalCost);
            if (inventoryWasFull)
            {
                purchaseMessage += " ~y~(Some items not added, inventory full)";
            }

            DisplayMessage(purchaseMessage, MessageType.Success);
            shoppingCart.Clear();

            Function.Call("DO_AUTO_SAVE");
            SaveInventoryData();
        }


        // Routes inventory menu rendering to the appropriate page
        private void DrawInventoryMenu(GTA.GraphicsEventArgs e)
        {
            switch (currentState)
            {
                case InventoryState.Main:
                    DrawInventoryMainMenu(e);
                    break;
                case InventoryState.Consumables:
                    DrawInventoryCategoryPage(e, "Consumables", "1. Page 1 (7-18 Health Core)\n2. Page 2 (19-25 Health Core)\n3. Page 3 (28-34 Health Core)\n4. Page 4 (35-50 Health Core)\n0. Back", Color.Yellow);
                    break;
                case InventoryState.Consumables_Page1:
                    DrawInventoryItemsPage(e, page1_items, "Consumables - Page 1", Color.Yellow);
                    break;
                case InventoryState.Consumables_Page2:
                    DrawInventoryItemsPage(e, page2_items, "Consumables - Page 2", Color.Yellow);
                    break;
                case InventoryState.Consumables_Page3:
                    DrawInventoryItemsPage(e, page3_items, "Consumables - Page 3", Color.Yellow);
                    break;
                case InventoryState.Consumables_Page4:
                    DrawInventoryItemsPage(e, page4_items, "Consumables - Page 4", Color.Yellow);
                    break;
                case InventoryState.Deadeye:
                    DrawInventoryItemsPage(e, "deadeye", "Deadeye Items", Color.Red);
                    break;
                case InventoryState.Armors:
                    DrawInventoryItemsPage(e, "armors", "Armors", Color.Cyan);
                    break;
                case InventoryState.Medkit:
                    DrawInventoryItemsPage(e, "medkit", "Medkits", Color.LightGreen);
                    break;
                case InventoryState.Valuables_Page1:
                    DrawInventoryItemsPage(e, valuables_page1_items, "Valuables - Page 1", Color.Gold);
                    break;
                case InventoryState.Valuables_Page2:
                    DrawInventoryItemsPage(e, valuables_page2_items, "Valuables - Page 2", Color.Gold);
                    break;
                case InventoryState.Valuables_Page3:
                    DrawInventoryItemsPage(e, valuables_page3_items, "Valuables - Page 3", Color.Gold);
                    break;
                case InventoryState.Backpack:
                    DrawInventoryBackpackPage(e);
                    break;
                case InventoryState.Combine:
                    DrawInventoryCombineMenu(e);
                    break;
                case InventoryState.Combine_Consumables1:
                    DrawInventoryCombinePage(e, combine_page1_items, "Consumables Combo - Page 1");
                    break;
                case InventoryState.Combine_Consumables2:
                    DrawInventoryCombinePage(e, combine_page2_items, "Consumables Combo - Page 2");
                    break;
                case InventoryState.Combine_Armor:
                    DrawInventoryCombineArmorMedkitPage(e, "Armor");
                    break;
                case InventoryState.Combine_Medkit:
                    DrawInventoryCombineArmorMedkitPage(e, "Medkit");
                    break;
                case InventoryState.Combine_Backpack:
                    DrawInventoryCombinePage(e, combine_backpack_items, "Backpack Crafts");
                    break;
            }
        }

        // Renders the main inventory menu with category options
        private void DrawInventoryMainMenu(GTA.GraphicsEventArgs e)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;

            int startX = (int)(gameWidth * 0.05f);
            int startY = (int)(gameHeight * 0.3f);
            int yIncrement = 20;

            var menuLines = new List<string>
            {
                "=== INVENTORY ===",
                "1. Consumables",
                "2. Deadeye Items",
                "3. Armors",
                "4. Medkits",
                "5. Valuables",
                "6. Backpack",
                "7. Combine Items",
                "",
                "0. Close"
            };

            int rectWidth = 350;
            int rectHeight = (yIncrement * menuLines.Count) + 10;
            e.Graphics.DrawRectangle(
                (float)(startX - 10) / gameWidth,
                (float)(startY - 15) / gameHeight,
                (float)rectWidth / gameWidth,
                (float)rectHeight / gameHeight,
                Color.FromArgb(180, 0, 0, 0)
            );

            int currentY = startY;
            foreach (string line in menuLines)
            {
                e.Graphics.DrawText(line, startX, currentY, Color.White);
                currentY += yIncrement;
            }
        }

        // Renders an inventory category page with menu options
        private void DrawInventoryCategoryPage(GTA.GraphicsEventArgs e, string title, string options, Color textColor)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;

            int startX = (int)(gameWidth * 0.05f);
            int startY = (int)(gameHeight * 0.3f);
            int yIncrement = 20;

            var menuLines = new List<string> { string.Format("=== {0} ===", title.ToUpper()) };
            menuLines.AddRange(options.Split('\n'));

            int rectWidth = 450;
            int rectHeight = (yIncrement * menuLines.Count) + 10;
            e.Graphics.DrawRectangle(
                (int)(startX - 10) / gameWidth,
                (int)(startY - 15) / gameHeight,
                (int)rectWidth / gameWidth,
                (int)rectHeight / gameHeight,
                Color.FromArgb(180, 0, 0, 0)
            );

            int currentY = startY;
            foreach (string line in menuLines)
            {
                e.Graphics.DrawText(line, startX, currentY, textColor);
                currentY += yIncrement;
            }
        }
        private string[] WrapText(string text, int maxCharsPerLine)
        {
            if (text.Length <= maxCharsPerLine)
            {
                return new string[] { text };
            }

            List<string> lines = new List<string>();
            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (string word in words)
            {
                if ((currentLine + " " + word).Length > maxCharsPerLine)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine += " " + word;
                }
            }
            lines.Add(currentLine);

            return lines.Select(l => l.Trim()).ToArray();
        }
        // Renders an inventory page displaying a grid of items
        private void DrawInventoryItemsPage(GTA.GraphicsEventArgs e, List<string> itemsOnPage, string title, Color titleColor)
        {
            List<string> displayItems = itemsOnPage.Where(key => inventory.ContainsKey(key) && inventory[key] > 0).ToList();

            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;

            int menuWidth = 800, menuHeight = 500;
            int menuX = (gameWidth - menuWidth) / 2, menuY = (gameHeight - menuHeight) / 2;
            int itemSize = 120, itemSpacing = 25, itemsPerRow = 5;
            int gridWidth = itemsPerRow * itemSize + (itemsPerRow - 1) * itemSpacing;
            int gridStartX = menuX + (menuWidth - gridWidth) / 2, gridStartY = menuY + 80;

            e.Graphics.DrawRectangle((float)menuX / gameWidth, (float)menuY / gameHeight, (float)menuWidth / gameWidth, (float)menuHeight / gameHeight, Color.FromArgb(180, 0, 0, 0));
            e.Graphics.DrawText(string.Format("=== {0} ===", title), menuX + 250, menuY + 30, Color.White);

            for (int i = 0; i < displayItems.Count; i++)
            {
                string key = displayItems[i];
                if (!itemDatabase.ContainsKey(key)) continue;
                var item = itemDatabase[key];
                int amount = inventory[key];

                int row = i / itemsPerRow, col = i % itemsPerRow;
                int itemX = gridStartX + col * (itemSize + itemSpacing);
                int itemY = gridStartY + row * (itemSize + itemSpacing);

                e.Graphics.DrawRectangle((float)itemX / gameWidth, (float)itemY / gameHeight, (float)itemSize / gameWidth, (float)itemSize / gameHeight, Color.FromArgb(100, 50, 50, 50));

                if (texturesLoaded && !string.IsNullOrEmpty(item.TextureName) && textureHandles.ContainsKey(item.TextureName))
                {
                    int imageSize = itemSize - 50;
                    int imageX = itemX + (itemSize - imageSize) / 2, imageY = itemY + 10;
                    Function.Call("DRAW_SPRITE", textureHandles[item.TextureName], (float)(imageX + imageSize / 2) / gameWidth, (float)(imageY + imageSize / 2) / gameHeight, (float)imageSize / gameWidth, (float)imageSize / gameHeight, 0.0f, 255, 255, 255, 255);
                }

                e.Graphics.DrawText(string.Format("x{0}", amount), itemX + 90, itemY + 5, Color.WhiteSmoke);

                string[] nameLines = WrapText(item.Name, 15);
                string[] effectLines = BuildEffectString(item).Split('\n');
                float currentTextY = itemY + itemSize - 45;

                foreach (string line in nameLines)
                {
                    e.Graphics.DrawText(line, itemX + 15, (int)currentTextY, titleColor);
                    currentTextY += 13;
                }
                foreach (string line in effectLines)
                {
                    e.Graphics.DrawText(line, itemX + 10, (int)currentTextY, Color.LightGray);
                    currentTextY += 13;
                }

                e.Graphics.DrawText((i + 1).ToString(), itemX + 5, itemY + 5, Color.Yellow);
            }

            e.Graphics.DrawText("<-- Prev | Next -->", menuX + menuWidth - 150, menuY + menuHeight - 30, Color.White);
            e.Graphics.DrawText("0. Back", menuX + 20, menuY + menuHeight - 30, Color.White);
        }

        // Renders an inventory page for items filtered by category
        private void DrawInventoryItemsPage(GTA.GraphicsEventArgs e, string category, string title, Color textColor)
        {
            List<string> itemsOnPage = itemDatabase.Keys.Where(k => itemDatabase[k].Category == category).ToList();
            DrawInventoryItemsPage(e, itemsOnPage, title, textColor);
        }

        // Renders the inventory backpack page
        private void DrawInventoryBackpackPage(GTA.GraphicsEventArgs e)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;

            int menuWidth = 800, menuHeight = 500;
            int menuX = (gameWidth - menuWidth) / 2, menuY = (gameHeight - menuHeight) / 2;

            e.Graphics.DrawRectangle((float)menuX / gameWidth, (float)menuY / gameHeight, (float)menuWidth / gameWidth, (float)menuHeight / gameHeight, Color.FromArgb(180, 0, 0, 0));

            e.Graphics.DrawText("=== BACKPACK ===", menuX + 300, menuY + 30, Color.White);

            string backpackKey = (currentBackpack == "none") ? "default_backpack_placeholder" : currentBackpack;
            ItemInfo info = itemDatabase.ContainsKey(backpackKey) ? itemDatabase[backpackKey] : new ItemInfo { Name = "Default", TextureName = "default_backpack_texture" };

            if (texturesLoaded && !string.IsNullOrEmpty(info.TextureName) && textureHandles.ContainsKey(info.TextureName))
            {
                int imageSize = 150;
                int imageX = menuX + (menuWidth - imageSize) / 2;
                int imageY = menuY + (menuHeight - imageSize) / 2 - 50;
                Function.Call("DRAW_SPRITE", textureHandles[info.TextureName], (float)(imageX + imageSize / 2) / gameWidth, (float)(imageY + imageSize / 2) / gameHeight, (float)imageSize / gameWidth, (float)imageSize / gameHeight, 0.0f, 255, 255, 255, 255);
            }

            string backpackName = (currentBackpack == "none") ? "Default" : info.Name;
            e.Graphics.DrawText(string.Format("Equipped: {0}", backpackName), menuX + 320, menuY + menuHeight / 2 + 50, Color.Orange);
            e.Graphics.DrawText(string.Format("Capacity: {0} items", inventoryCapacity), menuX + 320, menuY + menuHeight / 2 + 70, Color.Orange);

            e.Graphics.DrawText("0. Back", menuX + 20, menuY + menuHeight - 30, Color.White);
        }

        // Routes inventory input handling to the appropriate method
        private void HandleInventoryInput(GTA.KeyEventArgs e)
        {
            if (e.Key == Keys.I)
            {
                CloseInventory();
                return;
            }

            if (e.Key == Keys.D0 || e.Key == Keys.NumPad0)
            {
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_LEFT");
                Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_RIGHT");

                if (currentState == InventoryState.Main) CloseInventory();
                else if (currentState.ToString().Contains("Consumables_Page")) currentState = InventoryState.Consumables;
                else if (currentState.ToString().Contains("Valuables_Page")) currentState = InventoryState.Main; 
                else if (currentState.ToString().Contains("Combine_")) currentState = InventoryState.Combine;
                else currentState = InventoryState.Main; 
                return;
            }

            if (currentState >= InventoryState.Valuables_Page1 && currentState <= InventoryState.Valuables_Page3)
            {
                if (e.Key == Keys.Right)
                {
                    currentState++;
                    if (currentState > InventoryState.Valuables_Page3) currentState = InventoryState.Valuables_Page1;
                    Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    return;
                }
                if (e.Key == Keys.Left)
                {
                    currentState--;
                    if (currentState < InventoryState.Valuables_Page1) currentState = InventoryState.Valuables_Page3;
                    Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    return;
                }
            }

            if (currentState >= InventoryState.Consumables_Page1 && currentState <= InventoryState.Consumables_Page4)
            {
                if (e.Key == Keys.Right)
                {
                    currentState++;
                    if (currentState > InventoryState.Consumables_Page4) currentState = InventoryState.Consumables_Page1;
                    Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    return;
                }
                if (e.Key == Keys.Left)
                {
                    currentState--;
                    if (currentState < InventoryState.Consumables_Page1) currentState = InventoryState.Consumables_Page4;
                    Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_SCROLL_ALT_1_LEFT");
                    return;
                }
            }

            switch (currentState)
            {
                case InventoryState.Main:
                    HandleInventoryMainInput(e);
                    break;
                case InventoryState.Consumables:
                    HandleInventoryConsumablesMenuInput(e);
                    break;
                case InventoryState.Consumables_Page1: HandleInventoryItemSelection(e, page1_items); break;
                case InventoryState.Consumables_Page2: HandleInventoryItemSelection(e, page2_items); break;
                case InventoryState.Consumables_Page3: HandleInventoryItemSelection(e, page3_items); break;
                case InventoryState.Consumables_Page4: HandleInventoryItemSelection(e, page4_items); break;
                case InventoryState.Valuables_Page1: HandleInventoryItemSelection(e, valuables_page1_items); break;
                case InventoryState.Valuables_Page2: HandleInventoryItemSelection(e, valuables_page2_items); break;
                case InventoryState.Valuables_Page3: HandleInventoryItemSelection(e, valuables_page3_items); break;
                case InventoryState.Deadeye:
                case InventoryState.Armors:
                case InventoryState.Medkit:
                    string category = currentState.ToString().ToLower();
                    HandleInventoryItemSelection(e, category);
                    break;
                case InventoryState.Backpack:
                    break;
                case InventoryState.Combine: HandleInventoryCombineInput(e); break;
                case InventoryState.Combine_Consumables1: HandleInventoryCombineSelection(e, combine_page1_items); break;
                case InventoryState.Combine_Consumables2: HandleInventoryCombineSelection(e, combine_page2_items); break;
                case InventoryState.Combine_Armor: HandleInventoryCombineArmorMedkitSelection(e, "Armor"); break;
                case InventoryState.Combine_Medkit: HandleInventoryCombineArmorMedkitSelection(e, "Medkit"); break;
                case InventoryState.Combine_Backpack: HandleInventoryCombineSelection(e, combine_backpack_items); break;
            }
        }

        // Handles keyboard input for the inventory main menu
        private void HandleInventoryMainInput(GTA.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Keys.D1: case Keys.NumPad1: currentState = InventoryState.Consumables; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D2: case Keys.NumPad2: currentState = InventoryState.Deadeye; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D3: case Keys.NumPad3: currentState = InventoryState.Armors; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D4: case Keys.NumPad4: currentState = InventoryState.Medkit; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                
                case Keys.D5: case Keys.NumPad5: currentState = InventoryState.Valuables_Page1; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D6: case Keys.NumPad6: currentState = InventoryState.Backpack; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D7: case Keys.NumPad7: currentState = InventoryState.Combine; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
            }
        }

        // Handles keyboard input for the inventory consumables menu
        private void HandleInventoryConsumablesMenuInput(GTA.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Keys.D1: case Keys.NumPad1: currentState = InventoryState.Consumables_Page1; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D2: case Keys.NumPad2: currentState = InventoryState.Consumables_Page2; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D3: case Keys.NumPad3: currentState = InventoryState.Consumables_Page3; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
                case Keys.D4: case Keys.NumPad4: currentState = InventoryState.Consumables_Page4; Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT"); break;
            }
        }
        // Routes combine/crafting menu rendering to the appropriate category
        private void DrawInventoryCombineMenu(GTA.GraphicsEventArgs e)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;

            int startX = (int)(gameWidth * 0.05f);
            int startY = (int)(gameHeight * 0.3f);
            int yIncrement = 20;

            var menuLines = new List<string>
    {
        "=== COMBINE ITEMS ===",
        "1. Consumables (Page 1)",
        "2. Consumables (Page 2)",
        "3. Armor",
        "4. Medkit",
        "5. Backpack",
        "",
        "0. Back"
    };

            int rectWidth = 350;
            int rectHeight = (yIncrement * menuLines.Count) + 10;
            e.Graphics.DrawRectangle(
                (float)(startX - 10) / gameWidth,
                (float)(startY - 15) / gameHeight,
                (float)rectWidth / gameWidth,
                (float)rectHeight / gameHeight,
                Color.FromArgb(180, 0, 0, 0)
            );

            int currentY = startY;
            foreach (string line in menuLines)
            {
                e.Graphics.DrawText(line, startX, currentY, Color.White);
                currentY += yIncrement;
            }
        }


        // Renders a crafting recipe page
        private void DrawInventoryCombinePage(GTA.GraphicsEventArgs e, List<string> combineKeys, string title)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;

            int startX = (int)(gameWidth * 0.05f);
            int startY = (int)(gameHeight * 0.3f);
            int yIncrement = 20;

            var menuContent = new List<Tuple<string, Color>>();

            List<string> displayKeys = combineKeys;
            if (combineKeys == combine_backpack_items || title.ToLower().Contains("backpack"))
            {
                displayKeys = GetDisplayableCombineBackpacks();
            }

            if (displayKeys.Count == 0)
            {
                if (title.ToLower().Contains("backpack"))
                {
                    menuContent.Add(new Tuple<string, Color>("Already equipped best backpack.", Color.White));
                }
                else
                {
                    menuContent.Add(new Tuple<string, Color>("No items to combine.", Color.White));
                }
            }
            else
            {
                int index = 1;
                foreach (string key in displayKeys)
                {
                    CombineInfo info = combineDatabase[key];
                    int craftable = GetCraftableAmount(key);

                    List<string> ingredientParts = new List<string>();
                    foreach (var ing in info.Ingredients)
                    {
                        int owned = inventory.ContainsKey(ing.Key) ? inventory[ing.Key] : 0;
                        ingredientParts.Add(string.Format("{0}/{1} {2}", owned, ing.Value, itemDatabase[ing.Key].Name));
                    }
                    string ingredientsString = string.Join(", ", ingredientParts.ToArray());

                    string effect = "";
                    if (info.ResultItemKey != null && itemDatabase.ContainsKey(info.ResultItemKey) && itemDatabase[info.ResultItemKey].Category == "backpack")
                    {
                        effect = string.Format(" ({0} capacity)", GetBackpackCapacityForKey(info.ResultItemKey));
                    }
                    else
                    {
                        if (info.HealthCoreRestore != 0) effect = string.Format(" (+{0} Health Core)", info.HealthCoreRestore);
                        if (info.GrantsGoldCore) effect += " (Gold Core)";
                    }

                    Color lineColor = craftable > 0 ? Color.Yellow : Color.White;
                    string lineText = string.Format("{0}. {1} - {2}{3} x{4}", index, info.Name, ingredientsString, effect, craftable);
                    menuContent.Add(new Tuple<string, Color>(lineText, lineColor));
                    index++;
                }
            }

            int rectWidth = 800;
            int rectHeight = (yIncrement * (menuContent.Count + 3)); // +3 for title and back option
            e.Graphics.DrawRectangle(
                (int)(startX - 10) / gameWidth,
                (int)(startY - 15) / gameHeight,
                (int)rectWidth / gameWidth,
                (int)rectHeight / gameHeight,
                Color.FromArgb(180, 0, 0, 0)
            );

            int currentY = startY;
            e.Graphics.DrawText(string.Format("=== {0} ===", title.ToUpper()), startX, currentY, Color.White);
            currentY += yIncrement * 2; 

            foreach (var item in menuContent)
            {
                e.Graphics.DrawText(item.Item1, startX, currentY, item.Item2);
                currentY += yIncrement;
            }

            currentY += yIncrement; 
            e.Graphics.DrawText("0. Back", startX, currentY, Color.White);
        }

        // Renders a crafting page for armor and medkit combinations
        private void DrawInventoryCombineArmorMedkitPage(GTA.GraphicsEventArgs e, string category)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;

            int startX = (int)(gameWidth * 0.05f);
            int startY = (int)(gameHeight * 0.3f);
            int yIncrement = 20;

            string title = (category == "Armor") ? "COMBINE ARMOR" : "COMBINE MEDKIT";
            var menuContent = new List<Tuple<string, Color>>();
            int itemNumber = 1;

            foreach (var entry in combineDatabase)
            {
                if ((category == "Armor" && entry.Value.ResultItemKey == "armor_100") ||
                    (category == "Medkit" && entry.Value.ResultItemKey == "medkit"))
                {
                    CombineInfo info = entry.Value;
                    int craftableAmount = GetCraftableAmount(entry.Key);

                    List<string> ingredientDisplayList = new List<string>();
                    foreach (var ingredient in info.Ingredients)
                    {
                        int ownedAmount = inventory.ContainsKey(ingredient.Key) ? inventory[ingredient.Key] : 0;
                        string ingredientName = itemDatabase.ContainsKey(ingredient.Key) ? itemDatabase[ingredient.Key].Name : ingredient.Key;
                        ingredientDisplayList.Add(string.Format("{0}/{1} {2}", ownedAmount, ingredient.Value, ingredientName));
                    }
                    string ingredientsString = string.Join(", ", ingredientDisplayList.ToArray());

                    Color lineColor = craftableAmount > 0 ? (category == "Armor" ? Color.Cyan : Color.LightGreen) : Color.White;
                    string lineText = string.Format("{0}. {1} - {2} x{3}", itemNumber, info.Name, ingredientsString, craftableAmount);
                    menuContent.Add(new Tuple<string, Color>(lineText, lineColor));
                    itemNumber++;
                }
            }

            int rectWidth = 600;
            int rectHeight = (yIncrement * (menuContent.Count + 3));
            e.Graphics.DrawRectangle(
                (int)(startX - 10) / gameWidth,
                (int)(startY - 15) / gameHeight,
                (int)rectWidth / gameWidth,
                (int)rectHeight / gameHeight,
                Color.FromArgb(180, 0, 0, 0)
            );

            int currentY = startY;
            e.Graphics.DrawText(string.Format("=== {0} ===", title), startX, currentY, Color.White);
            currentY += yIncrement * 2;

            foreach (var item in menuContent)
            {
                e.Graphics.DrawText(item.Item1, startX, currentY, item.Item2);
                currentY += yIncrement;
            }

            currentY += yIncrement;
            e.Graphics.DrawText("0. Back", startX, currentY, Color.White);
        }

        // Routes combine input handling to the appropriate category
        private void HandleInventoryCombineInput(GTA.KeyEventArgs e)
        {
            bool validKeyPress = true;
            switch (e.Key)
            {
                case Keys.D1: case Keys.NumPad1: currentState = InventoryState.Combine_Consumables1; break;
                case Keys.D2: case Keys.NumPad2: currentState = InventoryState.Combine_Consumables2; break;
                case Keys.D3: case Keys.NumPad3: currentState = InventoryState.Combine_Armor; break;
                case Keys.D4: case Keys.NumPad4: currentState = InventoryState.Combine_Medkit; break;
                case Keys.D5: case Keys.NumPad5: currentState = InventoryState.Combine_Backpack; break;
                default:
                    validKeyPress = false;
                    break;
            }
            if (validKeyPress)
            {
                Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT");
                Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT");
            }
        }

        // Handles keyboard input for selecting and crafting recipes
        private void HandleInventoryCombineSelection(GTA.KeyEventArgs e, List<string> combineKeys)
        {
            int itemIndex = GetKeyPressedNumber(e) - 1;

            List<string> displayed = combineKeys;
            if (currentState == InventoryState.Combine_Backpack || combineKeys == combine_backpack_items)
            {
                displayed = GetDisplayableCombineBackpacks();
            }

            if (itemIndex >= 0 && itemIndex < displayed.Count)
            {
                PerformCombination(displayed[itemIndex]);
            }
        }

        // Handles keyboard input for selecting armor and medkit recipes
        private void HandleInventoryCombineArmorMedkitSelection(GTA.KeyEventArgs e, string category)
        {
            List<string> displayedCombos = new List<string>();
            foreach (var entry in combineDatabase)
            {
                if ((category == "Armor" && entry.Value.ResultItemKey == "armor_100") ||
                    (category == "Medkit" && entry.Value.ResultItemKey == "medkit"))
                {
                    displayedCombos.Add(entry.Key);
                }
            }

            int itemIndex = GetKeyPressedNumber(e) - 1;
            if (itemIndex >= 0 && itemIndex < displayedCombos.Count)
            {
                PerformCombination(displayedCombos[itemIndex]);
            }
        }

        // Calculates how many times a recipe can be crafted
        private int GetCraftableAmount(string combineKey)
        {
            if (!combineDatabase.ContainsKey(combineKey)) return 0;

            CombineInfo combo = combineDatabase[combineKey];
            int maxCraftable = int.MaxValue;

            foreach (var ingredient in combo.Ingredients)
            {
                int availableAmount = inventory.ContainsKey(ingredient.Key) ? inventory[ingredient.Key] : 0;
                int craftable = availableAmount / ingredient.Value;
                if (craftable < maxCraftable)
                {
                    maxCraftable = craftable;
                }
            }

            if (combo.ResultItemKey != null)
            {
                int spaceLeft = inventoryCapacity - inventory[combo.ResultItemKey];
                if (spaceLeft < maxCraftable)
                {
                    maxCraftable = spaceLeft;
                }
            }

            return maxCraftable;
        }

        // Executes a crafting recipe and creates the result item
        private void PerformCombination(string combineKey)
        {
            if (GetCraftableAmount(combineKey) <= 0)
            {
                DisplayMessage("Not enough ingredients!", MessageType.Error);
                return;
            }

            CombineInfo combo = combineDatabase[combineKey];

            foreach (var ingredient in combo.Ingredients)
            {
                inventory[ingredient.Key] -= ingredient.Value;
            }

            if (combo.ResultItemKey != null)
            {
                inventory[combo.ResultItemKey]++;
                ItemInfo resultItemInfo = itemDatabase[combo.ResultItemKey];

                if (resultItemInfo.Category == "backpack")
                {
                    currentBackpack = combo.ResultItemKey;
                    UpdateInventoryCapacity();
                    DisplayMessage(string.Format("Crafted and equipped {0}!", resultItemInfo.Name), MessageType.Success);
                }
                else
                {
                    DisplayMessage(string.Format("Crafted {0}!", combo.Name), MessageType.Success);
                }

                SaveInventoryData();
                return;
            }

            try
            {
                isUsingItem = true;
                CloseInventory();
                Ped ped = Player.Character;
                List<string> detachAnimationSets = new List<string> {
            "amb@drink_can", "amb@bottle_create", "amb@kiosk", "amb@coffee_idle_m", "amb@nuts_idle"
        };
                foreach (var ingredient in combo.Ingredients)
                {
                    ItemInfo ingredientInfo = itemDatabase[ingredient.Key];
                    if (!string.IsNullOrEmpty(ingredientInfo.Animation))
                    {
                        if (delayedAnimationSets.Contains(ingredientInfo.AnimationSet))
                        {
                            Function.Call("TASK_PLAY_ANIM_SECONDARY_UPPER_BODY", ped, ingredientInfo.Animation, ingredientInfo.AnimationSet, 8.0f, 0, 0, 0, false, false, false);
                            Wait(800);
                            SpawnItemModel(ingredient.Key, ped);
                        }
                        else
                        {
                            SpawnItemModel(ingredient.Key, ped);
                            Function.Call("TASK_PLAY_ANIM_SECONDARY_UPPER_BODY", ped, ingredientInfo.Animation, ingredientInfo.AnimationSet, 8.0f, 0, 0, 0, false, false, false);
                        }
                        Wait(ingredientInfo.AnimationDuration);
                        if (Function.Call<bool>("IS_CHAR_PLAYING_ANIM", ped, ingredientInfo.AnimationSet, ingredientInfo.Animation))
                        {
                            Wait(200);
                        }
                        if (detachAnimationSets.Contains(ingredientInfo.AnimationSet))
                        {
                            foreach (var spawnedItem in spawnedItems)
                            {
                                if (spawnedItem != null && spawnedItem.Exists())
                                {
                                    if (Function.Call<bool>("IS_CHAR_PLAYING_ANIM", ped, ingredientInfo.AnimationSet, ingredientInfo.Animation))
                                    {
                                        Wait(150);
                                    }
                                    Function.Call("DETACH_OBJECT", spawnedItem, true);
                                    objectsToDelete.Add(spawnedItem, DateTime.Now.AddSeconds(10));
                                }
                            }
                        }
                        else
                        {
                            CleanupSpawnedItems();
                        }
                        spawnedItems.Clear();
                    }
                }
                healthCore = Math.Min(100, healthCore + combo.HealthCoreRestore);
                if (combo.GrantsGoldCore)
                {
                    isGoldCoreActive = true;
                    goldCoreStartTime = Function.Call<int>("GET_HOURS_OF_DAY") * 60 + Function.Call<int>("GET_MINUTES_OF_DAY");
                    DisplayMessage(string.Format("Used {0}! Health Core is now golden!", combo.Name), MessageType.Success);
                }
                else
                {
                    DisplayMessage(string.Format("Used {0}! (+{1} Health Core)", combo.Name, combo.HealthCoreRestore), MessageType.Success);
                }
                SaveInventoryData();
            }
            finally
            {
                isUsingItem = false;
            }
        }

        // Handles keyboard input for selecting and using items from a list
        private void HandleInventoryItemSelection(GTA.KeyEventArgs e, List<string> itemsOnPage)
        {
            List<string> availableItems = GetAvailableItems(itemsOnPage);
            int itemIndex = GetKeyPressedNumber(e) - 1;
            if (itemIndex >= 0 && itemIndex < availableItems.Count)
            {
                UseItem(availableItems[itemIndex]);
            }
        }

        // Handles keyboard input for selecting and using items by category
        private void HandleInventoryItemSelection(GTA.KeyEventArgs e, string category)
        {
            List<string> availableItems = new List<string>();
            foreach (var kvp in itemDatabase)
            {
                if (kvp.Value.Category == category && inventory[kvp.Key] > 0)
                {
                    availableItems.Add(kvp.Key);
                }
            }

            int itemIndex = GetKeyPressedNumber(e) - 1;
            if (itemIndex >= 0 && itemIndex < availableItems.Count)
            {
                UseItem(availableItems[itemIndex]);
            }
        }



        // Uses an item from the inventory and applies its effects
        private void UseItem(string itemKey)
        {
            try
            {
                if (!itemDatabase.ContainsKey(itemKey) || inventory[itemKey] <= 0)
                {
                    if (itemDatabase.ContainsKey(itemKey))
                        DisplayMessage(string.Format("No {0} available!", itemDatabase[itemKey].Name), MessageType.Warning);
                    return;
                }

                Player player = Game.LocalPlayer;
                if (player == null || !player.CanControlCharacter)
                {
                    DisplayMessage("Cannot control character right now!", MessageType.Error);
                    return;
                }

                Ped ped = player.Character;
                if (ped == null || !ped.Exists() || !ped.isAliveAndWell)
                {
                    DisplayMessage("Character is not available!", MessageType.Error);
                    return;
                }

                ItemInfo item = itemDatabase[itemKey];
                if ((item.Category == "medkit" && ped.Health >= 100) || (item.Category == "armors" && ped.Armor >= 100))
                {
                    DisplayMessage(item.Category == "medkit" ? "Health is already full!" : "Armor is already full!", MessageType.Warning);
                    return;
                }

                isUsingItem = true;
                CloseInventory();



                int healthCoreBefore = healthCore;
                string statusBefore = GetHealthCoreStatus();

                try
                {
                    if (delayedAnimationSets.Contains(item.AnimationSet))
                    {
                        Function.Call("TASK_PLAY_ANIM_SECONDARY_UPPER_BODY", ped, item.Animation, item.AnimationSet, 8.0f, 0, 0, 0, false, false, false);
                        Wait(800);
                        SpawnItemModel(itemKey, ped);
                    }
                    else
                    {
                        SpawnItemModel(itemKey, ped);
                        Function.Call("TASK_PLAY_ANIM_SECONDARY_UPPER_BODY", ped, item.Animation, item.AnimationSet, 8.0f, 0, 0, 0, false, false, false);
                    }
                }
                catch { }

                DateTime startTime = DateTime.Now;
                bool wasCancelled = false;
                while ((DateTime.Now - startTime).TotalMilliseconds < item.AnimationDuration)
                {
                    if (Game.isGameKeyPressed(GameKey.Aim) || Game.isGameKeyPressed(GameKey.Attack) || Game.isGameKeyPressed(GameKey.Jump))
                    {
                        wasCancelled = true;
                        break;
                    }
                    Wait(0); 
                }

                List<string> detachAnimationSets = new List<string> { "amb@drink_can", "amb@bottle_create", "amb@kiosk", "amb@coffee_idle_m", "amb@nuts_idle" };
                if (detachAnimationSets.Contains(item.AnimationSet))
                {
                    foreach (var spawnedItem in spawnedItems)
                    {
                        if (spawnedItem != null && spawnedItem.Exists())
                        {
                            Function.Call("DETACH_OBJECT", spawnedItem, true);
                            objectsToDelete.Add(spawnedItem, DateTime.Now.AddSeconds(10));
                        }
                    }
                }
                else
                {
                    CleanupSpawnedItems();
                }
                spawnedItems.Clear();

                if (wasCancelled)
                {
                    ped.Task.ClearAll(); 
                    DisplayMessage("Item effect cancelled.", MessageType.Warning);
                    isUsingItem = false;
                    return; 
                }

                try
                {
                    if (ped.Exists() && ped.isAliveAndWell)
                    {
                        if (item.HealthRestore > 0) ped.Health = Math.Min(ped.Health + item.HealthRestore, 100);
                        if (item.ArmorRestore > 0) ped.Armor = Math.Min(ped.Armor + item.ArmorRestore, 100);
                        if (item.HealthCoreRestore != 0 && !(item.HealthCoreRestore < 0 && isGoldCoreActive))
                        {
                            healthCore = Math.Max(0, Math.Min(healthCore + item.HealthCoreRestore, 100));
                        }
                        if (item.DeadeyeRestore != 0) deadeyeCore = Math.Max(0, Math.Min(deadeyeCore + item.DeadeyeRestore, 100));
                    }
                }
                catch { }

                try
                {
                    List<string> changes = new List<string>();
                    if (item.HealthRestore > 0) changes.Add(string.Format("~g~Health: {0}~w~", Player.Character.Health));
                    if (item.ArmorRestore > 0) changes.Add(string.Format("~b~Armor: {0}~w~", Player.Character.Armor));

                    if (item.HealthCoreRestore != 0 && !(item.HealthCoreRestore < 0 && isGoldCoreActive))
                    {
                        string color = (item.HealthCoreRestore > 0) ? "~g~" : "~r~";
                        changes.Add(string.Format("{0}Health Core: {1}~w~", color, healthCore));
                    }
                    if (item.DeadeyeRestore != 0)
                    {
                        string color = (item.DeadeyeRestore > 0) ? "~g~" : "~r~";
                        changes.Add(string.Format("{0}Deadeye: {1}~w~", color, deadeyeCore));
                    }
                    string statusAfter = GetHealthCoreStatus();
                    if (statusAfter != statusBefore)
                    {
                        string statusColor = healthCore > healthCoreBefore ? "~g~" : "~r~";
                        if (statusAfter == "Golden") statusColor = "~y~";
                        changes.Add(string.Format("Status: {0}{1}~w~", statusColor, statusAfter));
                    }

                    string message = string.Format("Used {0}!", item.Name);
                    if (changes.Count > 0)
                    {
                        message += " " + string.Join(", ", changes.ToArray());
                    }
                    DisplayMessage(message, MessageType.Success);
                }
                catch { }

                isUsingItem = false;
                inventory[itemKey]--; 
                SaveInventoryData();
            }
            catch (System.Exception)
            {
                DisplayMessage("Item usage failed - please try again", MessageType.Error);
                CloseInventory();
                isUsingItem = false;
                try { CleanupSpawnedItems(); } catch { }
            }
        }


        // Spawns a 3D model of an item attached to the player
        private void SpawnItemModel(string itemKey, Ped ped)
        {
            try
            {
                ItemInfo item = itemDatabase[itemKey];
                string modelToRequest = null;
                uint modelHashToRequest = 0;

                bool hasNames = item.ModelNames != null && item.ModelNames.Count > 0;
                bool hasHashes = item.ModelHashes != null && item.ModelHashes.Count > 0;

                if (hasNames && hasHashes)
                {
                    if (random.Next(0, 2) == 0) modelToRequest = item.ModelNames[random.Next(item.ModelNames.Count)];
                    else modelHashToRequest = item.ModelHashes[random.Next(item.ModelHashes.Count)];
                }
                else if (hasNames) modelToRequest = item.ModelNames[random.Next(item.ModelNames.Count)];
                else if (hasHashes) modelHashToRequest = item.ModelHashes[random.Next(item.ModelHashes.Count)];

                if (modelHashToRequest != 0) Function.Call("REQUEST_MODEL", modelHashToRequest);
                else return;

                Wait(100);

                GTA.Object spawnedItem = null;
                if (modelToRequest != null) spawnedItem = World.CreateObject(modelToRequest, ped.Position);
                else if (modelHashToRequest != 0) spawnedItem = World.CreateObject(new Model(modelHashToRequest), ped.Position);

                if (spawnedItem != null && spawnedItem.Exists())
                {
                    Function.Call("ATTACH_OBJECT_TO_PED",
                        spawnedItem,
                        ped,
                        item.BoneId,
                        item.AttachOffset.X,
                        item.AttachOffset.Y,
                        item.AttachOffset.Z,
                        item.AttachRotation.X,
                        item.AttachRotation.Y,
                        item.AttachRotation.Z,
                        false);
                    spawnedItems.Add(spawnedItem);
                }
            }
            catch { /* Ignore model spawn errors */ }
        }

        // Cleans up and deletes all spawned item models
        private void CleanupSpawnedItems()
        {
            foreach (var item in spawnedItems)
            {
                if (item != null && item.Exists())
                {
                    item.NoLongerNeeded();
                    item.Delete();
                }
            }
            spawnedItems.Clear();
        }

        // Toggles the inventory menu open or closed
        private void ToggleInventory()
        {
            if (inventoryOpen) CloseInventory();
            else OpenInventory();
        }

        // Opens the inventory menu
        private void OpenInventory()
        {
            if (!textureLoaded) LoadTextures();
            inventoryOpen = true;
            currentState = InventoryState.Main;
            Function.Call("PAUSE_GAME", true);
            Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_RIGHT");
        }

        // Closes the inventory menu
        private void CloseInventory()
        {
            inventoryOpen = false;
            currentState = InventoryState.Main;
            currentMenuText = ""; 
            Function.Call("UNPAUSE_GAME");
            Function.Call("PLAY_SOUND_FRONTEND", -1, "BACK_ALT_1", "FRONTEND_MENU", 0);
        }


        // Attempts to loot nearby objects or rob NPCs
        private void TryLootOrRob()
        {
            Ped playerPed = Player.Character;
            if (Function.Call<bool>("IS_CHAR_IN_ANY_CAR", playerPed))
            {
                return;
            }

            Ped targetToRob = GetNearestRobbableNPC();
            if (targetToRob != null && targetToRob.Exists())
            {
                lootedNPCs.Add(targetToRob.GetHashCode());
                Function.Call("CLEAR_CHAR_TASKS_IMMEDIATELY", playerPed);
                Function.Call("TASK_PLAY_ANIM", playerPed, "pickup_high", "pickup_object", 8.0f, 0, 0, 0, false);
                Wait(1000);

                string lootMessageRob = LootFromNPC(targetToRob);
                if (!string.IsNullOrEmpty(lootMessageRob))
                {
                    DisplayMessage(lootMessageRob, MessageType.Success);
                }
                else
                {
                    DisplayMessage("Found nothing useful...", MessageType.Warning);
                }

                if (random.Next(0, 100) < 30)
                {
                    if (Player.WantedLevel == 0)
                    {
                        pendingWantedTime = DateTime.Now.AddSeconds(5);
                    }
                }
                if (Function.Call<bool>("IS_CHAR_PLAYING_ANIM", playerPed, "pickup_object", "pickup_high"))
                {
                    Function.Call("CLEAR_CHAR_TASKS_IMMEDIATELY", playerPed);
                }
                return;
            }

            Ped targetToLoot = GetNearestLootableNPC();
            if (targetToLoot != null && targetToLoot.Exists())
            {
                lootedNPCs.Add(targetToLoot.GetHashCode());

                float distance = playerPed.Position.DistanceTo(targetToLoot.Position);

                if (distance <= 1.0f)
                {
                    playerPed.Task.TurnTo(targetToLoot);
                    Wait(600);
                }
                else
                {
                    playerPed.Task.GoTo(targetToLoot);

                    DateTime startTime = DateTime.Now;
                    while (playerPed.Position.DistanceTo(targetToLoot.Position) > 1.5f)
                    {
                        Wait(100);
                        if ((DateTime.Now - startTime).TotalSeconds > 5)
                        {
                            playerPed.Task.ClearAll(); 
                            DisplayMessage("Cannot reach the target.", MessageType.Warning);
                            return; 
                        }
                    }
                    playerPed.Task.ClearAll();
                }

                Function.Call("TASK_PLAY_ANIM_WITH_ADVANCED_FLAGS", playerPed, "search_letterbox", "amb@postman_idles", 8.0f, false, false, false, false, false, false, false, 1000);
                Wait(1000);
                string lootMessage = LootFromNPC(targetToLoot);

                if (!string.IsNullOrEmpty(lootMessage))
                {
                    Function.Call("TASK_PLAY_ANIM_WITH_ADVANCED_FLAGS", playerPed, "sort_letters", "amb@postman_idles", 8.0f, false, false, false, false, false, false, false, 1000);
                    Wait(1000);
                    DisplayMessage(lootMessage, MessageType.Success);
                    if (random.Next(0, 100) < 50)
                    {
                        bool shouldSetMood = random.Next(0, 2) == 0;
                        if (shouldSetMood)
                        {
                            Function.Call("SET_PLAYER_MOOD_PISSED_OFF", Game.LocalPlayer.Index);
                            Function.Call("SAY_AMBIENT_SPEECH", playerPed, "SEARCH_BODY_TAKE_ITEM", 1, 1, 0);
                            Function.Call("SET_PLAYER_MOOD_NORMAL", Game.LocalPlayer.Index);
                        }
                        else Function.Call("SAY_AMBIENT_SPEECH", playerPed, "SEARCH_BODY_TAKE_ITEM", 1, 1, 0);
                    }
                }
                else
                {
                    DisplayMessage("Found nothing useful...", MessageType.Warning);
                    bool shouldSetMood = random.Next(0, 2) == 0;
                    if (shouldSetMood)
                    {
                        Function.Call("SET_PLAYER_MOOD_PISSED_OFF", Game.LocalPlayer.Index);
                        Function.Call("SAY_AMBIENT_SPEECH", playerPed, "GENERIC_CURSE", 1, 1, 0);
                        Function.Call("SET_PLAYER_MOOD_NORMAL", Game.LocalPlayer.Index);
                    }
                    else Function.Call("SAY_AMBIENT_SPEECH", playerPed, "GENERIC_CURSE", 1, 1, 0);
                }

                if (Player.WantedLevel == 0)
                {
                    Ped[] nearbyPeds = World.GetPeds(playerPed.Position, 20.0f);
                    foreach (Ped p in nearbyPeds)
                    {
                        if (p != null && p.Exists() && p.isAliveAndWell && p.PedType == PedType.Cop)
                        {
                            Player.WantedLevel = 1;
                            DisplayMessage("A cop saw you looting the body!", MessageType.Error);
                            break;
                        }
                    }
                }

                CleanupLootedList();
                playerPed.Task.ClearAll();
            }
        }


        private Ped GetNearestRobbableNPC()
        {
            Vector3 playerPos = Player.Character.Position;
            foreach (Ped p in World.GetPeds(playerPos, 10.0f))
            {
                if (p != null && p.Exists() && p.isAliveAndWell && p != Player.Character && !lootedNPCs.Contains(p.GetHashCode()))
                {
                    bool isAimingAtPed = Function.Call<bool>("IS_PLAYER_FREE_AIMING_AT_CHAR", Game.LocalPlayer, p);

                    bool hasScaredAnimation = Function.Call<bool>("IS_CHAR_PLAYING_ANIM", p, "ped", "handsup");

                    if (hasScaredAnimation && isAimingAtPed)
                    {
                        return p;
                    }
                }
            }
            return null;
        }

        // Returns the appropriate color for displaying deadeye core status
        private Color GetDeadeyeCoreColor()
        {
            if (deadeyeCore > 80) return Color.Green;
            if (deadeyeCore > 60) return Color.Yellow; 
            if (deadeyeCore > 40) return Color.Orange;
            if (deadeyeCore > 20) return Color.OrangeRed; 
            return Color.Red; 
        }

        // Determines an item key based on a random roll and category
        private string GetItemFromRoll(int roll, string category)
        {
            foreach (var kvp in itemDatabase)
            {
                if (kvp.Value.Category == category && roll >= kvp.Value.MinDropRange && roll <= kvp.Value.MaxDropRange)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        // Loots items from an NPC character
        private string LootFromNPC(Ped npc)
        {
            PedType pedType = npc.PedType;
            Dictionary<string, int> dropCategoryChances = GetDropCategoryChances(pedType);

            Dictionary<string, int> lootedThisSession = new Dictionary<string, int>();

            foreach (var categoryChance in dropCategoryChances)
            {
                if (random.Next(1, 101) <= categoryChance.Value)
                {
                    string itemKey = GetItemFromRoll(random.Next(1, 101), categoryChance.Key);
                    if (itemKey != null)
                    {
                        if (inventory[itemKey] < inventoryCapacity)
                        {
                            inventory[itemKey]++;

                            if (lootedThisSession.ContainsKey(itemKey)) lootedThisSession[itemKey]++;
                            else lootedThisSession.Add(itemKey, 1);
                        }
                        else
                        {
                        }
                    }
                }
            }

            List<string> bonusItems = new List<string>();
            foreach (var key in lootedThisSession.Keys)
            {
                string bonusKey = GetBonusLoot(key);
                if (bonusKey != null) bonusItems.Add(bonusKey);
            }
            foreach (var bKey in bonusItems)
            {
                if (lootedThisSession.ContainsKey(bKey)) lootedThisSession[bKey]++;
                else lootedThisSession.Add(bKey, 1);
            }

            SaveInventoryData();

            if (lootedThisSession.Count > 0)
            {
                foreach (var kvp in lootedThisSession)
                {
                    ShowLootNotification(kvp.Key, kvp.Value);
                }
                return "Looted items."; 
            }

            return null; 
        }

        // Determines a bonus item that accompanies the primary looted item
        private string GetBonusLoot(string primaryItemKey)
        {
            if (!itemDatabase.ContainsKey(primaryItemKey))
            {
                return null;
            }

            ItemInfo primaryItem = itemDatabase[primaryItemKey];

            if (primaryItemKey == "burgershot_drink")
            {
                if (random.Next(0, 100) < 50)
                {
                    if (inventory["burger"] < inventoryCapacity)
                    {
                        inventory["burger"]++;
                        return "burger"; 
                    }
                }
            }
            else if (primaryItem.Category == "consumables")
            {
                int chance = 0;
                if (primaryItem.HealthCoreRestore <= 10)
                {
                    chance = 70; 
                }
                else if (primaryItem.HealthCoreRestore > 10 && primaryItem.HealthCoreRestore <= 20)
                {
                    chance = 50; 
                }

                if (random.Next(0, 100) < chance)
                {
                    string bonusItemKey = GetItemFromRoll(random.Next(1, 100), "consumables");

                    if (bonusItemKey != null && inventory[bonusItemKey] < inventoryCapacity)
                    {
                        inventory[bonusItemKey]++;
                        return bonusItemKey; 
                    }
                }
            }

            return null;
        }

        private Dictionary<string, int> GetDropCategoryChances(PedType pedType)
        {
            // Default base for each ped type
            Dictionary<string, int> d;
            switch (pedType)
            {
                case PedType.Cop:
                    d = new Dictionary<string, int> { { "consumables", 20 }, { "armors", 40 }, { "medkit", 40 }, { "deadeye", 5 }, { "valuables", 30 }, { "backpack_material", 10 } };
                    break;
                case PedType.CivMale:
                case PedType.CivFemale:
                    d = new Dictionary<string, int> { { "consumables", 60 }, { "armors", 0 }, { "medkit", 20 }, { "deadeye", 15 }, { "valuables", 30 }, { "backpack_material", 10 } };
                    break;
                case PedType.Paramedic:
                    d = new Dictionary<string, int> { { "consumables", 40 }, { "armors", 0 }, { "medkit", 100 }, { "deadeye", 0 }, { "valuables", 30 }, { "backpack_material", 10 } };
                    break;
                default:
                    d = new Dictionary<string, int> { { "consumables", 20 }, { "armors", 0 }, { "medkit", 10 }, { "deadeye", 10 }, { "valuables", 10 }, { "backpack_material", 10 } };
                    break;
            }

            if (inventoryCapacity >= 100)
            {
                if (d.ContainsKey("backpack_material")) d["backpack_material"] = 0;
            }

            return d;
        }

        // Renders the main shop menu
        private void DrawShopMainMenu(GTA.GraphicsEventArgs e)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;

            int startX = (int)(gameWidth * 0.05f);
            int startY = (int)(gameHeight * 0.3f);
            int yIncrement = 20;

            var menuLines = new List<string>
            {
                "=== SHOP ===",
                "1. Consumables",
                "2. Deadeye Items",
                "3. Armors",
                "4. Medkits",
                "5. Backpacks",
                "6. Sell Valuables",
                "7. Sell Scraps",
                "",
                "-----------------------",
                string.Format("Total: {0}$ for {1} items", totalCost, totalItemsInCart),
                string.Format("({0}$ for shipping fee)", shippingFee),
                "-----------------------",
                "Enter. Confirm Order",
                "0. Cancel"
            };

            int rectWidth = 450;
            int rectHeight = (yIncrement * menuLines.Count) + 10;
            e.Graphics.DrawRectangle(
                (int)(startX - 10) / gameWidth,
                (int)(startY - 15) / gameHeight,
                (int)rectWidth / gameWidth,
                (int)rectHeight / gameHeight,
                Color.FromArgb(180, 0, 0, 0)
            );

            int currentY = startY;
            foreach (string line in menuLines)
            {
                e.Graphics.DrawText(line, startX, currentY, Color.White);
                currentY += yIncrement;
            }
        }

        // Renders the shop page for selling valuables
        private void DrawShopSellValuablesPage(GTA.GraphicsEventArgs e)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;
            int menuWidth = 800, menuHeight = 600;
            int menuX = (gameWidth - menuWidth) / 2, menuY = (gameHeight - menuHeight) / 2;
            int itemSize = 120, itemSpacing = 25, itemsPerRow = 5;
            int gridWidth = itemsPerRow * itemSize + (itemsPerRow - 1) * itemSpacing;
            int gridStartX = menuX + (menuWidth - gridWidth) / 2, gridStartY = menuY + 80;

            e.Graphics.DrawRectangle((float)menuX / gameWidth, (float)menuY / gameHeight, (float)menuWidth / gameWidth, (float)menuHeight / gameHeight, Color.FromArgb(180, 0, 0, 0));
            e.Graphics.DrawText("=== SELL VALUABLES ===", menuX + 280, menuY + 20, Color.Gold);

            List<string> sellableItems = inventory.Keys
                .Where(key => inventory[key] > 0 && itemDatabase.ContainsKey(key) && itemDatabase[key].Category == "valuables" && key != "metal_scrap" && key != "scrap")
                .ToList();

            int totalSaleValue = 0;
            foreach (string key in sellableItems)
            {
                totalSaleValue += inventory[key] * itemDatabase[key].Price;
            }

            if (sellableItems.Count == 0)
            {
                e.Graphics.DrawText("You have no valuables to sell.", menuX + 280, gridStartY + 100, Color.White);
            }
            else
            {
                int itemsPerPage = 10;
                int totalPages = (int)Math.Ceiling(sellableItems.Count / (double)itemsPerPage);
                if (totalPages == 0) totalPages = 1;
                sellValuablesPageIndex = Math.Max(0, Math.Min(sellValuablesPageIndex, totalPages - 1));
                List<string> displayItems = sellableItems.Skip(sellValuablesPageIndex * itemsPerPage).Take(itemsPerPage).ToList();

                e.Graphics.DrawText(string.Format("Page {0}/{1}", sellValuablesPageIndex + 1, totalPages), menuX + menuWidth - 150, menuY + menuHeight - 60, Color.White);

                for (int i = 0; i < displayItems.Count; i++)
                {
                    string key = displayItems[i];
                    var item = itemDatabase[key];
                    int amount = inventory[key];
                    int row = i / itemsPerRow, col = i % itemsPerRow;
                    int itemX = gridStartX + col * (itemSize + itemSpacing);
                    int itemY = gridStartY + row * (itemSize + itemSpacing);

                    e.Graphics.DrawRectangle((float)itemX / gameWidth, (float)itemY / gameHeight, (float)itemSize / gameWidth, (float)itemSize / gameHeight, Color.FromArgb(100, 80, 70, 20));

                    if (texturesLoaded && !string.IsNullOrEmpty(item.TextureName) && textureHandles.ContainsKey(item.TextureName))
                    {
                        int imageSize = itemSize - 50;
                        int imageX = itemX + (itemSize - imageSize) / 2, imageY = itemY + 5;
                        Function.Call("DRAW_SPRITE", textureHandles[item.TextureName], (float)(imageX + imageSize / 2) / gameWidth, (float)(imageY + imageSize / 2) / gameHeight, (float)imageSize / gameWidth, (float)imageSize / gameHeight, 0.0f, 255, 255, 255, 255);
                    }

                    e.Graphics.DrawText(string.Format("x{0}", amount), itemX + 90, itemY + 5, Color.White);
                    float currentTextY = itemY + itemSize - 40;
                    string[] nameLines = WrapText(item.Name, 15);
                    foreach (string line in nameLines)
                    {
                        e.Graphics.DrawText(line, itemX + 10, (int)currentTextY, Color.Gold);
                        currentTextY += 13;
                    }
                    e.Graphics.DrawText(string.Format("${0}", item.Price * amount), itemX + 10, (int)currentTextY + 5, Color.LightGreen);
                }
            }

            int infoY = menuY + menuHeight - 150;
            int launderingFee = (int)(totalSaleValue * 0.40f);
            int finalPayout = totalSaleValue - launderingFee;

            e.Graphics.DrawText("----------------------------------------------------------------", menuX + 150, infoY, Color.White);
            e.Graphics.DrawText(string.Format("Current Heat Level: {0}%", wantedChance), menuX + 250, infoY + 20, Color.OrangeRed);
            e.Graphics.DrawText(string.Format("Total Value: {0}$", totalSaleValue), menuX + 250, infoY + 40, Color.White);
            e.Graphics.DrawText(string.Format("Enter. Sell Directly (+Heat): {0}$", totalSaleValue), menuX + 250, infoY + 60, Color.Yellow);
            e.Graphics.DrawText(string.Format("Backspace. Sell Safely (-40% Fee): {0}$", finalPayout), menuX + 250, infoY + 80, Color.LightGreen);
            e.Graphics.DrawText("----------------------------------------------------------------", menuX + 150, infoY + 100, Color.White);

            e.Graphics.DrawText("<-- Prev | Next -->", menuX + menuWidth - 150, menuY + menuHeight - 30, Color.White);
            e.Graphics.DrawText("0. Back", menuX + 20, menuY + menuHeight - 30, Color.White);
        }
        // Renders the shop page for selling scrap materials
        private void DrawShopSellScrapsPage(GTA.GraphicsEventArgs e)
        {
            int gameWidth = Game.Resolution.Width;
            int gameHeight = Game.Resolution.Height;
            int menuWidth = 800, menuHeight = 500;
            int menuX = (gameWidth - menuWidth) / 2, menuY = (gameHeight - menuHeight) / 2;
            int itemSize = 120, itemSpacing = 25, itemsPerRow = 5;
            int gridWidth = itemsPerRow * itemSize + (itemsPerRow - 1) * itemSpacing;
            int gridStartX = menuX + (menuWidth - gridWidth) / 2, gridStartY = menuY + 80;

            e.Graphics.DrawRectangle((float)menuX / gameWidth, (float)menuY / gameHeight, (float)menuWidth / gameWidth, (float)menuHeight / gameHeight, Color.FromArgb(180, 0, 0, 0));
            e.Graphics.DrawText("=== SELL SCRAPS ===", menuX + 280, menuY + 20, Color.Gray);

            List<string> sellableScraps = inventory.Keys
                .Where(key => inventory[key] > 0 && (key == "metal_scrap" || key == "scrap"))
                .ToList();

            int totalSaleValue = 0;
            foreach (string key in sellableScraps)
            {
                totalSaleValue += inventory[key] * itemDatabase[key].Price;
            }

            if (sellableScraps.Count == 0)
            {
                e.Graphics.DrawText("You have no scraps to sell.", menuX + 280, gridStartY + 100, Color.White);
            }
            else
            {
                for (int i = 0; i < sellableScraps.Count; i++)
                {
                    string key = sellableScraps[i];
                    var item = itemDatabase[key];
                    int amount = inventory[key];
                    int row = i / itemsPerRow, col = i % itemsPerRow;
                    int itemX = gridStartX + col * (itemSize + itemSpacing);
                    int itemY = gridStartY + row * (itemSize + itemSpacing);

                    e.Graphics.DrawRectangle((float)itemX / gameWidth, (float)itemY / gameHeight, (float)itemSize / gameWidth, (float)itemSize / gameHeight, Color.FromArgb(100, 50, 50, 50));

                    if (texturesLoaded && !string.IsNullOrEmpty(item.TextureName) && textureHandles.ContainsKey(item.TextureName))
                    {
                        int imageSize = itemSize - 50;
                        int imageX = itemX + (itemSize - imageSize) / 2, imageY = itemY + 5;
                        Function.Call("DRAW_SPRITE", textureHandles[item.TextureName], (float)(imageX + imageSize / 2) / gameWidth, (float)(imageY + imageSize / 2) / gameHeight, (float)imageSize / gameWidth, (float)imageSize / gameHeight, 0.0f, 255, 255, 255, 255);
                    }

                    e.Graphics.DrawText(string.Format("x{0}", amount), itemX + 90, itemY + 5, Color.White);
                    float currentTextY = itemY + itemSize - 40;
                    e.Graphics.DrawText(item.Name, itemX + 10, (int)currentTextY, Color.Gray);
                    e.Graphics.DrawText(string.Format("${0}", item.Price * amount), itemX + 10, (int)currentTextY + 15, Color.LightGreen);
                }
            }

            int infoY = menuY + menuHeight - 100;
            e.Graphics.DrawText("----------------------------------------------------------------", menuX + 150, infoY, Color.White);
            e.Graphics.DrawText(string.Format("Total Payout: {0}$ (No fees, no heat)", totalSaleValue), menuX + 250, infoY + 20, Color.LightGreen);
            e.Graphics.DrawText("Enter. Confirm Sale", menuX + 250, infoY + 40, Color.Yellow);
            e.Graphics.DrawText("----------------------------------------------------------------", menuX + 150, infoY + 60, Color.White);

            e.Graphics.DrawText("0. Back", menuX + 20, menuY + menuHeight - 30, Color.White);
        }

        // Handles keyboard input for the sell scraps shop page
        private void HandleShopSellScrapsInput(GTA.KeyEventArgs e)
        {
            if (e.Key == Keys.Enter) { ConfirmSellScraps(); Function.Call("PLAY_SOUND_FRONTEND", -1, "RESIDENT_FRONTEND_MENU_BLOOP_ENTER_LEFT"); }
            if (e.Key == Keys.D0 || e.Key == Keys.NumPad0 || e.Key == Keys.Escape) { currentShopState = ShopState.Main; Function.Call("PLAY_SOUND_FRONTEND", -1, "GENERAL_FRONTEND_MENU_BACK_ALT_1_LEFT"); }
        }

        // Processes the sale of scrap items
        private void ConfirmSellScraps()
        {
            int totalSaleValue = 0;
            List<string> itemsToSell = new List<string>();

            foreach (var item in inventory)
            {
                if (item.Value > 0 && (item.Key == "metal_scrap" || item.Key == "scrap"))
                {
                    itemsToSell.Add(item.Key);
                    totalSaleValue += item.Value * itemDatabase[item.Key].Price;
                }
            }

            if (totalSaleValue == 0)
            {
                DisplayMessage("You have no scraps to sell!", MessageType.Warning);
                return;
            }

            foreach (string itemKey in itemsToSell)
            {
                inventory[itemKey] = 0;
            }

            Player.Money += totalSaleValue;

            DisplayMessage(string.Format("Sold scraps for {0}$.", totalSaleValue), MessageType.Success);

            Function.Call("DO_AUTO_SAVE");
            SaveInventoryData();
            currentShopState = ShopState.Main;
        }

        // Processes the sale of valuable items
        private void ConfirmSellValuables()
        {
            int totalSaleValue = 0;
            List<string> itemsToSell = new List<string>();

            foreach (var item in inventory)
            {
                if (item.Value > 0 && itemDatabase.ContainsKey(item.Key) && itemDatabase[item.Key].Category == "valuables" &&
                    item.Key != "metal_scrap" && item.Key != "scrap")
                {
                    itemsToSell.Add(item.Key);
                    totalSaleValue += item.Value * itemDatabase[item.Key].Price;
                }
            }

            if (totalSaleValue == 0)
            {
                DisplayMessage("You have no valuables to sell!", MessageType.Warning);
                return;
            }

            int roll = random.Next(1, 101);

            if (roll <= wantedChance)
            {
                Player.WantedLevel = 4;
                wantedChance = 100; 
                DisplayMessage("The deal went south! The cops are on to you!", MessageType.Error);
            }
            else
            {
                int finalPayout = totalSaleValue;

                foreach (string itemKey in itemsToSell)
                {
                    inventory[itemKey] = 0;
                }

                Player.Money += finalPayout;

                wantedChance = Math.Min(100.0f, wantedChance + 20.0f);

                firstSellTime = Function.Call<int>("GET_HOURS_OF_DAY") * 60 + Function.Call<int>("GET_MINUTES_OF_DAY");

                DisplayMessage(string.Format("Sold valuables for {0}$ (full price). Heat increased.", finalPayout), MessageType.Success);

                Function.Call("DO_AUTO_SAVE");
            }

            SaveInventoryData();
            currentShopState = ShopState.Main;
        }
        // Scans for nearby NPCs that can be looted
        private void CheckForLootableNpcs()
        {
            Ped playerPed = Player.Character;

            if (playerPed == null || !playerPed.Exists())
            {
                shouldShowLootPrompt = false;
                return;
            }

            Vector3 playerPos = playerPed.Position;

            if (inventoryOpen || isShopOpen || isUsingItem || !Function.Call<bool>("IS_PLAYER_FREE_FOR_AMBIENT_TASK", Player.Index))
            {
                shouldShowLootPrompt = false;
                return;
            }

            try 
            {
                Ped[] nearbyPeds = World.GetPeds(playerPos, 3.0f);
                foreach (Ped ped in nearbyPeds)
                {
                    if (ped != null && ped.Exists() && !ped.isAliveAndWell && !lootedNPCs.Contains(ped.GetHashCode()) && !Function.Call<bool>("IS_CHAR_IN_ANY_CAR", ped))
                    {
                        shouldShowLootPrompt = true;
                        return;
                    }
                }

                if (GetNearestRobbableNPC() != null)
                {
                    shouldShowLootPrompt = true;
                    return;
                }

                shouldShowLootPrompt = false;
            }
            catch (GTA.NonExistingObjectException)
            {
                shouldShowLootPrompt = false;
            }
        }
        private Ped GetNearestLootableNPC()
        {
            Vector3 playerPos = Player.Character.Position;
            Ped nearestNPC = null;
            float nearestDistance = 3.0f; // Max distance to check

            foreach (Ped ped in World.GetPeds(playerPos, nearestDistance))
            {
                if (ped != null && ped.Exists() && !ped.isAliveAndWell && !lootedNPCs.Contains(ped.GetHashCode()) && !Function.Call<bool>("IS_CHAR_IN_ANY_CAR", ped))
                {
                    float distance = GetDistance(playerPos, ped.Position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestNPC = ped;
                    }
                }
            }
            return nearestNPC;
        }


        // Removes dead NPCs from the looted list
        private void CleanupLootedList()
        {
            if (lootedNPCs.Count > 50)
            {
                lootedNPCs.RemoveRange(0, 25);
            }
        }

        // Calculates the distance between two 3D positions in the game world
        private float GetDistance(Vector3 pos1, Vector3 pos2)
        {
            float dx = pos1.X - pos2.X;
            float dy = pos1.Y - pos2.Y;
            float dz = pos1.Z - pos2.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // Displays a temporary message to the player
        private void DisplayMessage(string text, MessageType type, int duration = 2000)
        {
            string color;
            switch (type)
            {
                case MessageType.Success: color = "~g~"; break;
                case MessageType.Error: color = "~r~"; break;
                case MessageType.Warning: color = "~y~"; break;
                default: color = "~b~"; break;
            }

            currentMessage = color + text;
            messageStartTime = DateTime.Now;
            messageDuration = duration;
        }

        // Shows a floating notification for looted items
        private void ShowLootNotification(string itemKey, int quantity)
        {
            if (!itemDatabase.ContainsKey(itemKey)) return;

            var existing = activeLootNotifications.FirstOrDefault(n => n.ItemKey == itemKey && (DateTime.Now - n.StartTime).TotalSeconds < 2.0);

            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.StartTime = DateTime.Now; 
            }
            else
            {
                activeLootNotifications.Add(new LootNotification
                {
                    ItemKey = itemKey,
                    ItemName = itemDatabase[itemKey].Name,
                    Quantity = quantity,
                    TextureName = itemDatabase[itemKey].TextureName,
                    StartTime = DateTime.Now
                });
            }
        }
        // Returns the display color for an item based on category
        private Color GetItemColor(string category)
        {
            switch (category)
            {
                case "consumables": return Color.Yellow;      
                case "deadeye": return Color.Red;             
                case "armors": return Color.DeepSkyBlue;      
                case "medkit": return Color.LightGreen;        
                case "valuables": return Color.Gold;         
                case "backpack_material": return Color.DarkGray; 
                case "backpack": return Color.Orange;         
                default: return Color.WhiteSmoke;             
            }
        }
        // Renders all active loot notifications on the screen
        private void DrawLootNotifications(GTA.GraphicsEventArgs e)
        {
            if (activeLootNotifications.Count == 0) return;

            if (!textureLoaded)
            {
                LoadTextures();
            }

            int screenWidth = Game.Resolution.Width;
            int screenHeight = Game.Resolution.Height;

            float startX_Ratio = 0.75f;    
            float startY_Ratio = 0.40f;     

            int textX = (int)(screenWidth * (startX_Ratio + 0.035f));
            int startTextYPixel = (int)(screenHeight * startY_Ratio);

            float iconSizeRatio = 0.07f; 
            float iconWidth = iconSizeRatio;
            float iconHeight = iconSizeRatio * ((float)screenWidth / screenHeight);

            for (int i = activeLootNotifications.Count - 1; i >= 0; i--)
            {
                var notif = activeLootNotifications[i];

                if ((DateTime.Now - notif.StartTime).TotalSeconds > 3)
                {
                    activeLootNotifications.RemoveAt(i);
                    continue;
                }

                float currentY_Ratio = startY_Ratio + (i * (iconHeight + 0.015f));
                int currentTextY = startTextYPixel + (int)(i * (screenHeight * (iconHeight + 0.015f)));

                if (texturesLoaded && !string.IsNullOrEmpty(notif.TextureName) && textureHandles.ContainsKey(notif.TextureName))
                {
                    Function.Call("DRAW_SPRITE", textureHandles[notif.TextureName], startX_Ratio, currentY_Ratio, iconWidth, iconHeight, 0.0f, 255, 255, 255, 255);
                }

                Color textColor = Color.WhiteSmoke; 
                if (itemDatabase.ContainsKey(notif.ItemKey))
                {
                    string category = itemDatabase[notif.ItemKey].Category;
                    textColor = GetItemColor(category);
                }

                string text = notif.ItemName;
                if (notif.Quantity > 1)
                {
                    text += string.Format(" x{0}", notif.Quantity);
                }

                e.Graphics.DrawText(text, textX, currentTextY - 10, textColor);
            }
        }
    }
}