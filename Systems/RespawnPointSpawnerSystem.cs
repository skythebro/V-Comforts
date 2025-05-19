using System.Collections.Generic;
using ProjectM.CastleBuilding;
using ProjectM.CastleBuilding.Placement;
using ProjectM.Network;
using ProjectM.Shared;
using ProjectM.Tiles;
using Unity.Collections;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using VAMP;
using VampireCommandFramework;
using VrisingQoL.Patches;

namespace VrisingQoL.Systems;

using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using Stunlock.Core;

public class RespawnPointSpawnerSystem
{
    public void SpawnRespawnPoint(float3 position, quaternion rotation, Entity character, Entity user, bool global)
    {
        var entityManager = Core.EntityManager;
        var signPostPrefabGuid = new PrefabGUID(Settings.RESPAWN_POINT_PREFAB.Value); // TM_Castle_ObjectDecor_GardenLampPost01_Orange or  1965581787 // TM_Castle_ObjectDecor_Gothic_Brazier04_Orange_DLC
        if (!Core.SystemService.PrefabCollectionSystem.PrefabLookupMap.TryGetValueWithoutLogging(signPostPrefabGuid, out var prefab) &&
            !Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(signPostPrefabGuid, out prefab))
        {
            Plugin.LogInstance.LogInfo("Prefab not found: " + signPostPrefabGuid.GetNamePrefab());
            return;
        }
        
        var coffinEntity = Core.EntityManager.Instantiate(InitializationPatch.prefabCoffin);
        var spawnpointEntity = Core.EntityManager.Instantiate(prefab);
        spawnpointEntity.Add<PhysicsCustomTags>();
        spawnpointEntity.Write(new Translation { Value = position });
        spawnpointEntity.Write(new Rotation { Value = rotation });

        if (spawnpointEntity.Has<TilePosition>())
        {
            var tilePos = spawnpointEntity.Read<TilePosition>();
            // Get rotation around Y axis
            var euler = new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w).eulerAngles;
            tilePos.TileRotation = (TileRotation)(Mathf.Floor((360 - math.degrees(euler.y) - 45) / 90) % 4);
            spawnpointEntity.Write(tilePos);

            if (spawnpointEntity.Has<StaticTransformCompatible>())
            {
                var stc = spawnpointEntity.Read<StaticTransformCompatible>();
                stc.NonStaticTransform_Rotation = tilePos.TileRotation;
                spawnpointEntity.Write(stc);
            }
            // Has ownership of the spawnpoint, but not if setting change?
            if (!entityManager.HasComponent<SpawnedBy>(spawnpointEntity))
            {
                entityManager.AddComponent<SpawnedBy>(spawnpointEntity);
            }
            spawnpointEntity.Write(new SpawnedBy { Value = user });
            
            
            spawnpointEntity.Write(new Rotation { Value = quaternion.RotateY(math.radians(90 * (int)tilePos.TileRotation)) });

            if (!spawnpointEntity.Has<Networked>())
            {
                spawnpointEntity.Add<Networked>();
            }
            
            // Ensure CastleAreaRequirement exists
            if (!entityManager.HasComponent<CastleAreaRequirement>(spawnpointEntity))
                entityManager.AddComponent<CastleAreaRequirement>(spawnpointEntity);
            spawnpointEntity.Write(new CastleAreaRequirement
            {
                RequirementType = CastleAreaRequirementType.IgnoreCastleAreas,
                BlockPlacementOnRoads = false,
                AllowPlaceInObjectsInRepairState = true,
                AllowTilesStickingOutOfTerritory = true
            });
            if (!entityManager.HasComponent<CastleRebuildPhaseState>(spawnpointEntity))
                entityManager.AddComponent<CastleRebuildPhaseState>(spawnpointEntity);
            spawnpointEntity.Write(new CastleRebuildPhaseState
            {
                State = PhaseState.None
            });

            if (!entityManager.HasComponent<StationServants>(spawnpointEntity))
                entityManager.AddComponent<StationServants>(spawnpointEntity);
            spawnpointEntity.Write(new StationServants
            {
                Servants = ServantType.None
            });

            var signpostHealth = spawnpointEntity.Read<Health>();
            signpostHealth.MaxHealth = new ModifiableFloat(100);
            signpostHealth.Value = 100;
            signpostHealth.MaxRecoveryHealth = 100;
            spawnpointEntity.Write(signpostHealth);
            
            var signpostHealthConstants = spawnpointEntity.Read<HealthConstants>();
            signpostHealthConstants.DestroyOnDeath = false;
            spawnpointEntity.Write(signpostHealthConstants);
            
            
            var coffinBuffableFlagState = InitializationPatch.coffinEntity.Read<BuffableFlagState>();
            if (!spawnpointEntity.Has<BuffableFlagState>())
            {
                spawnpointEntity.Add<BuffableFlagState>();
            }
            spawnpointEntity.Write(coffinBuffableFlagState);
            
            var coffinImmortal = coffinEntity.Read<Immortal>();
            entityManager.AddComponentData(spawnpointEntity, coffinImmortal);
            
            
            //StoneCoffinSpawn_Travel_Delay: 1290990039
            //TombCoffinSpawn_Travel_Delay: -165284501
            //WoodenCoffinSpawn_Travel_Delay: -246207628
            var coffinRespawnPoint = coffinEntity.Read<RespawnPoint>();
            coffinRespawnPoint.RespawnPointType = RespawnPointType.Unknown;
            coffinRespawnPoint.SpawnDelayBuff = new PrefabGUID(1521207380); // waypoint buff, destroys spawnlocation even if immortal
            coffinRespawnPoint.SpawnSleepingBuff = PrefabGUID.Empty;
            coffinRespawnPoint.RespawnPointOwner = NetworkedEntity.Empty;
            coffinRespawnPoint.HasRespawnPointOwner = false;
            entityManager.AddComponentData(spawnpointEntity, coffinRespawnPoint);
            
            
            
            var coffinInteractable = coffinEntity.Read<Interactable>();
            entityManager.AddComponentData(spawnpointEntity, coffinInteractable);
            
            var coffinInteractedUpon = coffinEntity.Read<InteractedUpon>();
            entityManager.AddComponentData(spawnpointEntity, coffinInteractedUpon);
            
            
            entityManager.AddBuffer<BuffBuffer>(spawnpointEntity);
            var eventListenerBuffer = entityManager.AddBuffer<GameplayEventListeners>(spawnpointEntity);
            eventListenerBuffer.Add(new GameplayEventListeners
            {
                EventIdIndex = 0,
                EventIndexOfType = 0,
                ConditionBlob = BlobAssetReference<ConditionBlob>.Null,
                GameplayEventType = GameplayEventTypeEnum.ApplyBuff,
                GameplayEventId = new GameplayEventId { GameplayEventType = GameplayEventType.Local, EventId = 373290447 }
            });
            
            var eventIdMappingBuffer = entityManager.AddBuffer<GameplayEventIdMapping>(spawnpointEntity);
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
            var signPostBuffer = entityManager.AddBuffer<InteractAbilityBuffer>(spawnpointEntity);
            foreach (var entry in coffinBuffer)
            {
                if (entry.Ability != new PrefabGUID(1013114237)) // sleep buff
                {
                    signPostBuffer.Add(entry);
                }
            }
            
            if (!entityManager.HasComponent<Buffable>(spawnpointEntity))
                entityManager.AddComponentData(spawnpointEntity, new Buffable { KnockbackResistanceIndex = new ModifiableInt(13), UniqueBuffCategories = BuffCategoryFlag.Travel | BuffCategoryFlag.Shapeshift });
            else
                entityManager.SetComponentData(spawnpointEntity, new Buffable { KnockbackResistanceIndex = new ModifiableInt(13), UniqueBuffCategories = BuffCategoryFlag.Travel | BuffCategoryFlag.Shapeshift });

            if (!entityManager.HasComponent<CastleHeartConnection>(spawnpointEntity))
                entityManager.AddComponentData(spawnpointEntity, new CastleHeartConnection { CastleHeartEntity = NetworkedEntity.Empty });
            else
                entityManager.SetComponentData(spawnpointEntity, new CastleHeartConnection { CastleHeartEntity = NetworkedEntity.Empty });

            if (!entityManager.HasComponent<Residency>(spawnpointEntity))
                entityManager.AddComponentData(spawnpointEntity, new Residency { InsideBuff = new PrefabGUID(381160212), Resident = Entity.Null });
            else
                entityManager.SetComponentData(spawnpointEntity, new Residency { InsideBuff = new PrefabGUID(381160212), Resident = Entity.Null });
            
            
            var mapIconBuffer = entityManager.AddBuffer<AttachMapIconsToEntity>(spawnpointEntity);
            mapIconBuffer.Add(new AttachMapIconsToEntity
            {
                Prefab = new PrefabGUID(985620050) // MapIcon_StoneCoffin
            });
            
            SetOwnerForEntity(character, spawnpointEntity);
            
            // remove unnecessary components
            // entityManager.RemoveComponent<PlaySequenceOnDeath>(signPostEntity);
            // entityManager.RemoveComponent<DisableWhenNoPlayersInRange>(signPostEntity);
            // entityManager.RemoveComponent<MegaStaticCompatibleTag>(signPostEntity);
            // entityManager.RemoveComponent<StaticTileModel>(signPostEntity);
            // entityManager.RemoveComponent<DropTable>(signPostEntity);
            // entityManager.RemoveComponent<StaticPhysicsCollider>(signPostEntity);
            // entityManager.AddComponent<ProjectM.Scripting.ScriptSpawn>(signPostEntity);
        }
        else
        {
            Plugin.LogInstance.LogWarning("TilePosition component not found on spawnpoint entity.");
        }
    }

    public bool RemoveRespawnPoint(float3 position)
    {
        var entityManager = Core.EntityManager;
        var signPostPrefabGuid = new PrefabGUID(Settings.RESPAWN_POINT_PREFAB.Value); // TM_Castle_ObjectDecor_GardenLampPost01_Orange or  1965581787 // TM_Castle_ObjectDecor_Gothic_Brazier04_Orange_DLC
        Plugin.LogInstance.LogInfo("Creating entityquery.");
        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnedBy>(), ComponentType.ReadOnly<Residency>());

        // find the closest sign post
        var closestSignPost = float.MaxValue;
        Entity signPostEntity = Entity.Null;
        Plugin.LogInstance.LogInfo("starting query.");
        foreach (var entity in query.ToEntityArray(Allocator.Temp))
        {
            if (entityManager.TryGetComponentData<LocalToWorld>(entity, out var localToWorld))
            {
                var distance = math.distance(position, localToWorld.Position);
                if (distance < closestSignPost && entity.GetPrefabGuid() == signPostPrefabGuid)
                {
                    closestSignPost = distance;
                    signPostEntity = entity;
                }
            }
        }
        
        if (signPostEntity != Entity.Null)
        {
            Plugin.LogInstance.LogInfo("Going to destroy.");
            DestroyUtility.DestroyWithReason(entityManager, signPostEntity, DestroyReason.Default);
            Plugin.LogInstance.LogInfo("Sign post removed.");
            return true;
        }
        
        Plugin.LogInstance.LogWarning("No sign post found.");
        return false;
        
    }
    
    public bool SetRespawnPoint(float3 position, ChatCommandContext ctx)
    {
        var entityManager = Core.EntityManager;
        var respawnPointPrefabGuid = new PrefabGUID(Settings.RESPAWN_POINT_PREFAB.Value); // TM_Castle_ObjectDecor_GardenLampPost01_Orange or  1965581787 // TM_Castle_ObjectDecor_Gothic_Brazier04_Orange_DLC
        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnedBy>(), ComponentType.ReadOnly<Residency>());

        // find the closest sign post
        var closestRespawnPointDistance = float.MaxValue;
        Entity respawnPointEntity = Entity.Null;
        List<Entity> respawnPoints = new List<Entity>();
        foreach (var entity in query.ToEntityArray(Allocator.Temp))
        {
            respawnPoints.Add(entity);
            if (entityManager.TryGetComponentData<LocalToWorld>(entity, out var localToWorld))
            {
                var distance = math.distance(position, localToWorld.Position);
                if (distance < closestRespawnPointDistance && entity.GetPrefabGuid() == respawnPointPrefabGuid)
                {
                    if (entity.Read<SpawnedBy>().Value.GetUser().PlatformId != ctx.Event.SenderUserEntity.GetUser().PlatformId && !entity.Read<RespawnPoint>().HasRespawnPointOwner)
                    {
                        closestRespawnPointDistance = distance;
                        respawnPointEntity = entity;
                    }
                }
            }
        }
        
        if (respawnPointEntity != Entity.Null)
        {
            if (!entityManager.HasComponent<Networked>(respawnPointEntity))
                respawnPointEntity.Add<Networked>();
            
            var respawnpointComponent = respawnPointEntity.Read<RespawnPoint>();
            respawnpointComponent.RespawnPointOwner = ctx.Event.SenderUserEntity;
            respawnpointComponent.HasRespawnPointOwner = true;
            respawnPointEntity.Write(respawnpointComponent);
            foreach (var entity in respawnPoints)
            {
                var entityRespawn =  entity.Read<RespawnPoint>();
                if (entityRespawn.RespawnPointOwner.GetSyncedEntityOrNull().GetUser().PlatformId == ctx.Event.SenderUserEntity.GetUser().PlatformId)
                {
                    var location = entity.Read<LocalToWorld>();
                    if (entity.Index == respawnPointEntity.Index)
                    {
                        continue;
                    }
                    entityRespawn.RespawnPointOwner = NetworkedEntity.Empty;
                    entityRespawn.HasRespawnPointOwner = false;
                    entity.Write(entityRespawn);
                    var playerLocation = ctx.Event.SenderCharacterEntity.Read<LocalToWorld>();
                    Plugin.LogInstance.LogInfo($"Updated respawn point for {ctx.Event.SenderUserEntity.GetUser().CharacterName} at {playerLocation.Position} on entity {entity.GetPrefabGuidName()} at {location.Position}.");

                }
                else
                {
                    Plugin.LogInstance.LogInfo("Respawn point not updated as no other respawns were founds.");
                }
            }
            Plugin.LogInstance.LogInfo($"SenderUserEntity: {ctx.Event.SenderUserEntity.Index}, Version: {ctx.Event.SenderUserEntity.Version}");
            Plugin.LogInstance.LogInfo($"Has RespawnPointOwnerBuffer: {entityManager.HasComponent<RespawnPointOwnerBuffer>(ctx.Event.SenderUserEntity)}");
            var respawnPointOwnerBuffer = ctx.Event.SenderUserEntity.ReadBuffer<RespawnPointOwnerBuffer>();
            if (respawnPointOwnerBuffer.Length == 0)
            {
                Plugin.LogInstance.LogInfo("Respawn point owner buffer empty adding buffer item.");
                respawnPointOwnerBuffer.Add(new RespawnPointOwnerBuffer { RespawnPoint = respawnPointEntity, RespawnPointNetworkId = respawnPointEntity.GetNetworkId() });
                return true;
            }
            

            for (int i = 0; i < respawnPointOwnerBuffer.Length; i++)
            {
                var pointOwnerBuffer = respawnPointOwnerBuffer[i];
                pointOwnerBuffer.RespawnPoint = respawnPointEntity;
                pointOwnerBuffer.RespawnPointNetworkId = respawnPointEntity.GetNetworkId();
                respawnPointOwnerBuffer[i] = pointOwnerBuffer;
            }

            return true;
            
        }
        
        Plugin.LogInstance.LogWarning("No sign post found.");
        return false;
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


//ActivateDraculaWarpRiftSystem activateDraculaWarpRiftSystem = Core.Server.GetExistingSystemManaged<ActivateDraculaWarpRiftSystem>();
// SpellTarget;
// ActivateDraculaWarpRift;
