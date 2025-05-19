using BepInEx.Configuration;

namespace VrisingQoL
{
	public static class Settings
	{
		public static ConfigEntry<bool> ENABLE_BLOODMIXER_EXTRA_BOTTLE { get; private set; }
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
			ENABLE_BLOODMIXER_EXTRA_BOTTLE = config.Bind<bool>("BloodMixer", "enableBloodmixerExtraBottle", true, "If enabled gives you a glass bottle back when mixing 2 blood potions.");
			
			ENABLE_RESPAWN_POINTS = config.Bind<bool>("Respawn", "enableRespawnPoint", true, "If enabled admins can spawn a custom respawn point by using the command '.rps sp' and user can then use set it as a respawn point using '.rsp set' this will only work when near the respawnpoint.");
			ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING = config.Bind<bool>("Respawn", "enableNonAdminRespawnPointSpawning", false, "If enabled non admins can also spawn a respawnpoint limited to X amount defined below.");
			ENABLE_PREDEFINED_RESPAWN_POINTS = config.Bind<bool>("Respawn", "enablePredefinedRespawnPoints", true, "If enabled a json file in '\\BepInEx\\config\\Respawns\\respawns.json' can be edited with a list of coordinates where the respawnpoints will be placed, each player will then get ownership of each instance of a respawnpoint at those locations (if a player logs in another instance of the respawnpoint will be created at the same location and vice versa when logging out).");
			ENABLE_RESPAWN_POINT_BREAKAGE = config.Bind<bool>("Respawn", "enableRespawnPointBreakage", false, "If enabled respawn points will break after 1 use, best used with 'ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING' as predefined portals will not just respawn. (changes the AB_Interact prefab to the waygate one that creates and explosion which breaks the respawnpoint, PS: do not place respawnpoints near other respawnpoints as they will break each other, I will fix this if I can find a way.)");
			RESPAWN_POINT_LIMIT = config.Bind<int>("Respawn", "respawnPointLimit", 2, "Sets the limit of respawn points that can be spawned by non admins. (if 0 it will not limit anything!)");
			RESPAWN_POINT_COST_AMOUNT = config.Bind<int>("Respawn", "respawnPointCostAmount", 1, "If above 0, respawn points will cost X amount of prefabitem defined below. (if 0 it will not cost anything)");
			RESPAWN_POINT_COST_ITEM_PREFAB = config.Bind<int>("Respawn", "respawnPointCostItemPrefab", 271594022, "Sets the item required in the users inventory to spawn a respawnpoint. (this is the prefab item id)");
			RESPAWN_POINT_PREFAB = config.Bind<int>("Respawn", "respawnPointPrefab", -55079755, "Sets the prefab id of the respawn point prefab. (this is TM_Castle_ObjectDecor_GardenLampPost01_Orange) WARNING make sure the prefab has BlueprintData component otherwise players wont be able to respawn due to a client side error when trying to get the BlueprintData of the respawnpoint.");
			RESPAWN_TRAVEL_DELAY_PREFAB = config.Bind<int>("Respawn", "respawnAnimationPrefab", 1290990039, "Sets the prefab id of the respawn animation prefab. (this is StoneCoffinSpawn_Travel_Delay) there are 2 others that are usable you can find them in the prefab list on the Vrising modding wiki, make sure the name contains Travel_Delay (AB_Interact_WaypointSpawn_Travel_Delay is not usable as it is already set when 'ENABLE_RESPAWN_POINT_BREAKAGE' is true).");
			
		}
	}
}
