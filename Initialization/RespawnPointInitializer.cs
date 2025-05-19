using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using VAMP;

namespace VrisingQoL.Initialization
{
    public static class RespawnPointInitializer
    {
        public static Entity prefabCoffin;

        public static void Initialize()
        {
            if (!Settings.ENABLE_RESPAWN_POINTS.Value)
            {
                return;
            }
            var coffingPrefabGuid = new PrefabGUID(1523835471);

            if (!Core.SystemService.PrefabCollectionSystem.PrefabLookupMap.TryGetValueWithoutLogging(coffingPrefabGuid,
                    out prefabCoffin) &&
                !Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(coffingPrefabGuid,
                    out prefabCoffin))
            {
                Plugin.LogInstance.LogInfo("Coffin prefab not found: " + coffingPrefabGuid.GetNamePrefab());
                return;
            }

            var coffinEntity = Core.EntityManager.Instantiate(prefabCoffin);

            var entityManager = Core.EntityManager;
            var respawnPointPrefabGuid = new PrefabGUID(Settings.RESPAWN_POINT_PREFAB.Value);
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnedBy>(),
                ComponentType.ReadOnly<Residency>());

            var interactAbilityBuffer = coffinEntity.ReadBuffer<InteractAbilityBuffer>();
            var gameplayEventListenersBuffer = coffinEntity.ReadBuffer<GameplayEventListeners>();
            foreach (var entity in query.ToEntityArray(Allocator.Temp))
            {
                if (entity.GetPrefabGuid() != respawnPointPrefabGuid) continue;
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

            coffinEntity.Destroy();
        }
    }
}