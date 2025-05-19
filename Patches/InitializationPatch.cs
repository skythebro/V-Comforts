using System.Reflection;
using HarmonyLib;
using ProjectM;
using VrisingQoL.Database;
using VrisingQoL.Initialization;

namespace VrisingQoL.Patches
{
    [HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), "OnUpdate")]
    public static class InitializationPatch
    {
        [HarmonyPostfix]
        public static void OneShot_AfterLoad_InitializationPatch()
        {
            Plugin.Harmony.Unpatch((MethodBase) typeof (SpawnTeamSystem_OnPersistenceLoad).GetMethod("OnUpdate"), typeof (InitializationPatch).GetMethod(nameof (OneShot_AfterLoad_InitializationPatch)));
            Plugin.Initialize();
            RespawnPointInitializer.Initialize();
            RespawnPointDatabase.LoadFromFile();
        }
        
        [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnDestroy))]
        [HarmonyPrefix]
        static void OnDestroyPrefix(ServerBootstrapSystem __instance)
        {
            // if i need it?
        }
    }
}