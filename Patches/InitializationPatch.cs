using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using VAMP;
using VrisingQoL.Systems;

namespace VrisingQoL.Patches
{
    [HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), "OnUpdate")]
    public static class InitializationPatch
    {
        
        public static Entity coffinEntity;

        public static Entity prefabCoffin;
        
        [HarmonyPostfix]
        public static void OneShot_AfterLoad_InitializationPatch()
        {
            
            Plugin.Harmony.Unpatch((MethodBase) typeof (SpawnTeamSystem_OnPersistenceLoad).GetMethod("OnUpdate"), typeof (InitializationPatch).GetMethod(nameof (OneShot_AfterLoad_InitializationPatch)));
            Plugin.Initialize();
            
            var coffingPrefabGuid = new PrefabGUID(1523835471);
        
            if (!Core.SystemService.PrefabCollectionSystem.PrefabLookupMap.TryGetValueWithoutLogging(coffingPrefabGuid, out prefabCoffin) &&
                !Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(coffingPrefabGuid, out prefabCoffin))
            {
                Plugin.LogInstance.LogInfo("Coffin prefab not found: " + coffingPrefabGuid.GetNamePrefab());
                return;
            }
        
            coffinEntity = Core.EntityManager.Instantiate(prefabCoffin);
            
            var entityManager = Core.EntityManager;
            var signPostPrefabGuid = new PrefabGUID(Settings.RESPAWN_POINT_PREFAB.Value); // TM_Castle_ObjectDecor_GardenLampPost01_Orange
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnedBy>(), ComponentType.ReadOnly<Residency>());

            var interactAbilityBuffer = coffinEntity.ReadBuffer<InteractAbilityBuffer>();
            var gameplayEventListenersBuffer = coffinEntity.ReadBuffer<GameplayEventListeners>();
            foreach (var entity in query.ToEntityArray(Allocator.Temp))
            {
                if (entityManager.TryGetComponentData<LocalToWorld>(entity, out var localToWorld))
                {
                    if (entity.GetPrefabGuid() == signPostPrefabGuid)
                    {
                        foreach (var t in interactAbilityBuffer)
                        {
                            var signBuffer = entity.ReadBuffer<InteractAbilityBuffer>();
                            signBuffer.Add(t);
                        }
                        
                        foreach (var t in gameplayEventListenersBuffer)
                        {
                            var signBuffer = entity.ReadBuffer<GameplayEventListeners>();
                            signBuffer.Add(t);
                        }
                    }
                }
            }
            coffinEntity.Destroy();
        }
        
        [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnDestroy))]
        [HarmonyPrefix]
        static void OnDestroyPrefix(ServerBootstrapSystem __instance)
        {
            // if i need it?
        }
    }
}