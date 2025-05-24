using System.Reflection;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using VAMP;
using VComforts.Database;
using VComforts.Initialization;

namespace VComforts.Patches.Connection;

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
   /*    
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnDestroy))]
    [HarmonyPrefix]
    static void OnDestroyPrefix(ServerBootstrapSystem __instance)
    {
        // if i need it?
    }*/
}