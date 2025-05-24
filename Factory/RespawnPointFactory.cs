using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.CastleBuilding.Placement;
using ProjectM.Network;
using ProjectM.Shared;
using ProjectM.Tiles;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using VAMP;
using VComforts.Initialization;

namespace VComforts.Factory
{
    public class RespawnPointFactory
    {
        public bool CreateRespawnPoint(float3 position, quaternion rotation, Entity character, Entity user)
        {
            var entityManager = Core.EntityManager;

            // Check if a spawn point already exists for the user near the given position
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnedBy>(), ComponentType.ReadOnly<Residency>());
            var existingSpawnPoints = query.ToEntityArray(Allocator.Temp);

            const float positionLeeway = 3.0f; // Define a small leeway for position comparison

            foreach (var entity in existingSpawnPoints)
            {
                var spawnedBy = entityManager.GetComponentData<SpawnedBy>(entity);
                var localToWorld = entityManager.GetComponentData<LocalToWorld>(entity);
                var localToWorldPosition = localToWorld.Position;

                if (spawnedBy.Value == user && math.distance(localToWorldPosition, position) <= positionLeeway)
                {
                    Plugin.LogInstance.LogWarning("A nearby spawn point already exists for this user. Skipping creation.");
                    return false;
                }
            }

            var prefab = GetPrefab();
            if (prefab == Entity.Null)
            {
                Plugin.LogInstance.LogError("Prefab not found or prefab doesn't have BlueprintData.");
                return false;
            }

            var coffinEntity = entityManager.Instantiate(RespawnPointInitializer.prefabCoffin);
            var spawnpointEntity = entityManager.Instantiate(prefab);

            SetupSpawnPoint(spawnpointEntity, position, rotation, user);
            CopyCoffinData(coffinEntity, spawnpointEntity);
            SetOwnerForEntity(character, spawnpointEntity);

            entityManager.DestroyEntity(coffinEntity);
            return true;
        }

        private Entity GetPrefab()
        {
            var spawnPointPrefabGuid = new PrefabGUID(Settings.RESPAWN_POINT_PREFAB.Value);
            if (Core.SystemService.PrefabCollectionSystem.PrefabLookupMap.TryGetValueWithoutLogging(spawnPointPrefabGuid, out var prefab) ||
                Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(spawnPointPrefabGuid, out prefab))
            {
                if (prefab.Has<BlueprintData>())
                {
                    return prefab;
                }

                return Entity.Null;
            }
            return Entity.Null;
        }

        private void SetupSpawnPoint(Entity spawnpointEntity, float3 position, quaternion rotation, Entity user)
        {
            var entityManager = Core.EntityManager;
            spawnpointEntity.Add<PhysicsCustomTags>();
            spawnpointEntity.Write(new Translation { Value = position });
            spawnpointEntity.Write(new Rotation { Value = rotation });

            if (spawnpointEntity.Has<TilePosition>())
            {
                var tilePos = spawnpointEntity.Read<TilePosition>();
                tilePos.TileRotation = CalculateTileRotation(rotation);
                spawnpointEntity.Write(tilePos);

                if (spawnpointEntity.Has<StaticTransformCompatible>())
                {
                    var stc = spawnpointEntity.Read<StaticTransformCompatible>();
                    stc.NonStaticTransform_Rotation = tilePos.TileRotation;
                    spawnpointEntity.Write(stc);
                }
            }
            
            // Has ownership of the spawnpoint, but not if setting change?
            if (!entityManager.HasComponent<SpawnedBy>(spawnpointEntity))
            {
                entityManager.AddComponent<SpawnedBy>(spawnpointEntity);
            }
            spawnpointEntity.Write(new SpawnedBy { Value = user });
            spawnpointEntity.Add<Networked>();
            AddCastleAreaRequirements(spawnpointEntity);
        }

        private TileRotation CalculateTileRotation(quaternion rotation)
        {
            var euler = new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w).eulerAngles;
            return (TileRotation)(Mathf.Floor((360 - math.degrees(euler.y) - 45) / 90) % 4);
        }

        private void AddCastleAreaRequirements(Entity spawnpointEntity)
        {
            var entityManager = Core.EntityManager;

            if (!entityManager.HasComponent<CastleAreaRequirement>(spawnpointEntity))
                entityManager.AddComponent<CastleAreaRequirement>(spawnpointEntity);

            spawnpointEntity.Write(new CastleAreaRequirement
            {
                RequirementType = CastleAreaRequirementType.IgnoreCastleAreas,
                BlockPlacementOnRoads = false,
                AllowPlaceInObjectsInRepairState = true,
                AllowTilesStickingOutOfTerritory = true
            });
        }

        private void CopyCoffinData(Entity coffinEntity, Entity spawnPointEntity)
        {
            var entityManager = Core.EntityManager;

            var coffinRespawnPoint = coffinEntity.Read<RespawnPoint>();
            coffinRespawnPoint.RespawnPointType = RespawnPointType.Unknown;
            coffinRespawnPoint.SpawnDelayBuff = new PrefabGUID(Settings.ENABLE_RESPAWN_POINT_BREAKAGE.Value ? 1521207380 : Settings.RESPAWN_TRAVEL_DELAY_PREFAB.Value);
            coffinRespawnPoint.SpawnSleepingBuff = PrefabGUID.Empty;
            coffinRespawnPoint.RespawnPointOwner = NetworkedEntity.Empty;
            coffinRespawnPoint.HasRespawnPointOwner = false;
            entityManager.AddComponentData(spawnPointEntity, coffinRespawnPoint);

            entityManager.AddBuffer<BuffBuffer>(spawnPointEntity);
            var eventListenerBuffer = entityManager.AddBuffer<GameplayEventListeners>(spawnPointEntity);
            eventListenerBuffer.Add(new GameplayEventListeners
            {
                EventIdIndex = 0,
                EventIndexOfType = 0,
                ConditionBlob = BlobAssetReference<ConditionBlob>.Null,
                GameplayEventType = GameplayEventTypeEnum.ApplyBuff,
                GameplayEventId = new GameplayEventId { GameplayEventType = GameplayEventType.Local, EventId = 373290447 }
            });
            
            var eventIdMappingBuffer = entityManager.AddBuffer<GameplayEventIdMapping>(spawnPointEntity);
            eventIdMappingBuffer.Add(new GameplayEventIdMapping
            {
                NextTriggerTime = 872005.4672457098, //?
                TriggerCooldown = 0,
                GameplayEventId = new GameplayEventId { GameplayEventType = GameplayEventType.Local, EventId = 373290447 },
                MaxTriggers = 0,
                CurrentTriggers = 1,
                TriggerMultipleTimes = false
            });

            
            var coffinBuffer = entityManager.GetBuffer<InteractAbilityBuffer>(coffinEntity);
            var respawnPointBuffer = entityManager.AddBuffer<InteractAbilityBuffer>(spawnPointEntity);
            foreach (var entry in coffinBuffer)
            {
                if (entry.Ability != new PrefabGUID(1013114237)) // sleep buff
                {
                    respawnPointBuffer.Add(entry);
                }
            }
            
            var spawnPointHealth = spawnPointEntity.Read<Health>();
            spawnPointHealth.MaxHealth = new ModifiableFloat(100);
            spawnPointHealth.Value = 100;
            spawnPointHealth.MaxRecoveryHealth = 100;
            spawnPointEntity.Write(spawnPointHealth);
            
            var spawnPointHealthConstants = spawnPointEntity.Read<HealthConstants>();
            spawnPointHealthConstants.DestroyOnDeath = false;
            spawnPointEntity.Write(spawnPointHealthConstants);
            
            
            var coffinBuffableFlagState = coffinEntity.Read<BuffableFlagState>();
            if (!spawnPointEntity.Has<BuffableFlagState>())
            {
                spawnPointEntity.Add<BuffableFlagState>();
            }
            spawnPointEntity.Write(coffinBuffableFlagState);
            
            if (!spawnPointEntity.Has<Immortal>())
            {
                var coffinImmortal = coffinEntity.Read<Immortal>();
                entityManager.AddComponentData(spawnPointEntity, coffinImmortal);
            }
            
            if (!spawnPointEntity.Has<Interactable>())
            {
                var coffinInteractable = coffinEntity.Read<Interactable>();
                entityManager.AddComponentData(spawnPointEntity, coffinInteractable);
            }
            
            if (!spawnPointEntity.Has<InteractedUpon>())
            {
                var coffinInteractedUpon = coffinEntity.Read<InteractedUpon>();
                entityManager.AddComponentData(spawnPointEntity, coffinInteractedUpon);
            }
            
            if (!spawnPointEntity.Has<Residency>())
            {
                var coffinResidency = coffinEntity.Read<Residency>();
                coffinResidency.Resident = Entity.Null;
                coffinResidency.InsideBuff = new PrefabGUID(381160212);
                entityManager.AddComponentData(spawnPointEntity, coffinResidency);
            }
            
            if (!entityManager.HasComponent<Buffable>(spawnPointEntity))
                entityManager.AddComponentData(spawnPointEntity, new Buffable { KnockbackResistanceIndex = new ModifiableInt(13), UniqueBuffCategories = BuffCategoryFlag.Travel | BuffCategoryFlag.Shapeshift });
            else
                entityManager.SetComponentData(spawnPointEntity, new Buffable { KnockbackResistanceIndex = new ModifiableInt(13), UniqueBuffCategories = BuffCategoryFlag.Travel | BuffCategoryFlag.Shapeshift });

            
            var mapIconBuffer = entityManager.AddBuffer<AttachMapIconsToEntity>(spawnPointEntity);
            mapIconBuffer.Add(new AttachMapIconsToEntity
            {
                Prefab = new PrefabGUID(985620050) // MapIcon_StoneCoffin
            });
        }

        private void SetOwnerForEntity(Entity charEntity, Entity entity)
        {
            Extensions.CastleHeartService.GetFallbackCastleHeart(charEntity, out var castleHeartEntity);
            if (castleHeartEntity != Entity.Null)
            {
                if (entity.Has<CastleHeartConnection>())
                {
                    entity.Write(new CastleHeartConnection { CastleHeartEntity = castleHeartEntity });
                }

                var teamRef = (Entity)castleHeartEntity.Read<TeamReference>().Value;
                if (entity.Has<Team>())
                {
                    var teamData = teamRef.Read<TeamData>();
                    entity.Write(new Team() { Value = teamData.TeamValue, FactionIndex = -1 });

                    entity.Add<UserOwner>();
                    entity.Write(castleHeartEntity.Read<UserOwner>());
                }

                if (entity.Has<TeamReference>() && !teamRef.Equals(Entity.Null))
                {
                    var tr = new TeamReference();
                    tr.Value._Value = teamRef;
                    entity.Write(tr);
                }
            }
        }
    }
}