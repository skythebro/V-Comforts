using HarmonyLib;
using ProjectM;
using Unity.Collections;

namespace VrisingQoL.Patches;

[HarmonyPatch(typeof(InstantiateMapIconsSystem_Spawn), "OnUpdate")]
public static class InstantiateMapIconsSystem_SpawnPatch
{
    static void Postfix(InstantiateMapIconsSystem_Spawn __instance)
    {
        var query = __instance.__query_1050583619_0;
        if (query.IsEmpty) return;

        foreach (var entity in query.ToEntityArray(Allocator.Temp))
        {
            string prefabName = entity.GetPrefabGuidName();
            Plugin.LogInstance.LogWarning($"[MapIcon] Entity: {entity.Index}, Prefab: {prefabName}");
        }
    }
}