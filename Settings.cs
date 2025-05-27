using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace VComforts
{
    public static class Settings
    {
        public static ConfigEntry<bool> ENABLE_BLOODMIXER_EXTRA_BOTTLE { get; private set; }
        public static ConfigEntry<bool> ENABLE_AUTOFISH { get; private set; }
        public static ConfigEntry<bool> ENABLE_CARRIAGE_TRACKING { get; private set; }
        public static ConfigEntry<bool> ENABLE_LEVEL_BONUS { get; private set; }
        public static ConfigEntry<string> LEVEL_BONUS_MULTIPLIER_STRING { get; private set; }
        public static List<float> LEVEL_BONUS_MULTIPLIER { get; private set; }

        public static ConfigEntry<bool> ENABLE_INVENTORY_BONUS { get; private set; }
        public static ConfigEntry<string> BAG_INVENTORY_BONUS_MULTIPLIER_STRING { get; private set; }
        public static List<float> BAG_INVENTORY_BONUS_MULTIPLIER { get; private set; }

        public static ConfigEntry<bool> ENABLE_CUSTOM_BLOODPOTION_SORTING { get; private set; }
        public static ConfigEntry<bool> ENABLE_CUSTOM_BLOODPOTION_SORTING_PLAYER { get; private set; }
        public static ConfigEntry<bool> CUSTOM_BLOODPOTION_SORTING_PRIMARYTHENQUALITY { get; private set; }

        public static ConfigEntry<bool> ENABLE_RESPAWN_POINTS { get; private set; }
        public static ConfigEntry<bool> ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING { get; private set; }
        public static ConfigEntry<bool> ENABLE_RESPAWN_POINT_BREAKAGE { get; private set; }
        public static ConfigEntry<bool> ENABLE_PREDEFINED_RESPAWN_POINTS { get; private set; }
        public static ConfigEntry<int> RESPAWN_POINT_COST_AMOUNT { get; private set; }
        public static ConfigEntry<int> RESPAWN_POINT_COST_ITEM_PREFAB { get; private set; }
        public static ConfigEntry<int> RESPAWN_POINT_PREFAB { get; private set; }
        public static ConfigEntry<int> RESPAWN_TRAVEL_DELAY_PREFAB { get; private set; }
        public static ConfigEntry<int> RESPAWN_POINT_LIMIT { get; private set; }


        internal static void Initialize(ConfigFile config)
        {
            ENABLE_BLOODMIXER_EXTRA_BOTTLE = config.Bind<bool>("BloodMixer", "enableBloodmixerExtraBottle", false,
                "If enabled gives you a glass bottle back when mixing 2 blood potions.");
            ENABLE_AUTOFISH = config.Bind<bool>("Fishing", "enableAutoFish", false,
                "If enabled fish will automatically be caught whenever a splash happens");
            ENABLE_CARRIAGE_TRACKING = config.Bind<bool>("Carriage", "enableCarriageTracking", false,
                "If enabled carriages will be tracked and shown on the map.");

            ENABLE_LEVEL_BONUS = config.Bind<bool>("LevelBonus", "enableLevelBonus", false,
                "If enabled you will get a bonus to your stats based on your level, value defined below.");
            LEVEL_BONUS_MULTIPLIER_STRING = config.Bind<string>("LevelBonus", "levelBonusMultiplier",
                "0.005,0.003,0.0035",
                "Sets the level bonus multiplier per level for each stat in the following order: resourceYieldBonus, moveSpeedBonus, shapeshiftMoveSpeedBonus");
            LEVEL_BONUS_MULTIPLIER = LEVEL_BONUS_MULTIPLIER_STRING.Value
                .Split(',')
                .Select(s => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture))
                .ToList();
            ENABLE_INVENTORY_BONUS = config.Bind<bool>("InventoryBonus", "enableInventoryBonus", false,
                "If enabled you will get a bonus to your inventory stack size based on your equipped bag, value defined below.");
            BAG_INVENTORY_BONUS_MULTIPLIER_STRING = config.Bind<string>("InventoryBonus", "bagInventoryBonusMultiplier",
                "1.05,1.10,1.15,1.20,1.25,1.30",
                "Sets the inventory bonus multiplier per level for each bag in the following order: resourceYieldBonus, moveSpeedBonus, shapeshiftMoveSpeedBonus");
            BAG_INVENTORY_BONUS_MULTIPLIER = BAG_INVENTORY_BONUS_MULTIPLIER_STRING.Value
                .Split(',')
                .Select(s => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture))
                .ToList();
            ENABLE_CUSTOM_BLOODPOTION_SORTING = config.Bind<bool>("BloodPotionSorting",
                "enableCustomBloodPotionSorting", false,
                "If enabled blood potions will be sorted by primary blood type and then by quality.");
            ENABLE_CUSTOM_BLOODPOTION_SORTING_PLAYER = config.Bind<bool>(
                "BloodPotionSorting",
                "enableCustomBloodPotionSortingPlayer",
                false,
                "If enabled, blood potions will also use the custom sorting in the player inventory.");
            CUSTOM_BLOODPOTION_SORTING_PRIMARYTHENQUALITY = config.Bind<bool>(
                "BloodPotionSorting",
                "customBloodPotionSortingPrimaryThenQuality",
                true,
                "If enabled, blood potions are sorted first by primary blood type (A-Z), then by quality (highest to lowest). If disabled, potions with only a primary blood type are listed first (sorted A-Z, highest quality first), followed by mixed potions (those with a secondary type), also sorted by primary type and quality."
            );

            ENABLE_RESPAWN_POINTS = config.Bind<bool>("Respawn", "enableRespawnPoint", false,
                "If enabled admins can spawn a custom respawn point by using the command '.rps sp' and user can then use set it as a respawn point using '.rsp set' this will only work when near the respawnpoint.");
            ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING = config.Bind<bool>("Respawn", "enableNonAdminRespawnPointSpawning",
                false, "If enabled non admins can also spawn a respawnpoint limited to X amount defined below.");
            ENABLE_PREDEFINED_RESPAWN_POINTS = config.Bind<bool>("Respawn", "enablePredefinedRespawnPoints", false,
                "If enabled a json file in '\\BepInEx\\config\\Respawns\\respawns.json' can be edited with a list of coordinates where the respawnpoints will be placed, each player will then get ownership of each instance of a respawnpoint at those locations (if a player logs in another instance of the respawnpoint will be created at the same location and vice versa when logging out).");
            ENABLE_RESPAWN_POINT_BREAKAGE = config.Bind<bool>("Respawn", "enableRespawnPointBreakage", false,
                "If enabled respawn points will break after 1 use, best used with 'ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING' as predefined portals will not just respawn. (changes the AB_Interact prefab to the waygate one that creates and explosion which breaks the respawnpoint, PS: do not place respawnpoints near other respawnpoints as they will break each other, I will fix this if I can find a way.)");
            RESPAWN_POINT_LIMIT = config.Bind<int>("Respawn", "respawnPointLimit", 1,
                "Sets the limit of respawn points that can be spawned by non admins. (if 0 it will not limit anything!)");
            RESPAWN_POINT_COST_AMOUNT = config.Bind<int>("Respawn", "respawnPointCostAmount", 1,
                "If above 0, respawn points will cost X amount of prefabitem defined below. (if 0 it will not cost anything)");
            RESPAWN_POINT_COST_ITEM_PREFAB = config.Bind<int>("Respawn", "respawnPointCostItemPrefab", 271594022,
                "Sets the item required in the users inventory to spawn a respawnpoint. (this is the prefab item id)");
            RESPAWN_POINT_PREFAB = config.Bind<int>("Respawn", "respawnPointPrefab", -55079755,
                "Sets the prefab id of the respawn point prefab. (this is TM_Castle_ObjectDecor_GardenLampPost01_Orange) WARNING make sure the prefab has BlueprintData component otherwise players wont be able to respawn due to a client side error when trying to get the BlueprintData of the respawnpoint.");
            RESPAWN_TRAVEL_DELAY_PREFAB = config.Bind<int>("Respawn", "respawnAnimationPrefab", 1290990039,
                "Sets the prefab id of the respawn animation prefab. (this is StoneCoffinSpawn_Travel_Delay) there are 2 others that are usable you can find them in the prefab list on the Vrising modding wiki, make sure the name contains Travel_Delay (AB_Interact_WaypointSpawn_Travel_Delay is not usable as it is already set when 'ENABLE_RESPAWN_POINT_BREAKAGE' is true).");
        }
    }
}