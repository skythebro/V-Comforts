using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VAMP;
using VampireCommandFramework;

namespace VrisingQoL.Managers
{
    public class RespawnPointManager
    {
        public bool RemoveRespawnPoint(float3 position, Entity user, bool isAdmin, bool keepAssignedRespawnPoint)
        {
            var entityManager = Core.EntityManager;
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnedBy>(),
                ComponentType.ReadOnly<Residency>());

            var closestSpawnPoint = float.MaxValue;
            Entity spawnPointEntity = Entity.Null;

            foreach (var entity in query.ToEntityArray(Allocator.Temp))
            {
                if (entityManager.TryGetComponentData<LocalToWorld>(entity, out var localToWorld))
                {
                    var distance = math.distance(position, localToWorld.Position);

                    if (distance < closestSpawnPoint &&
                        entity.GetPrefabGuid() == new PrefabGUID(Settings.RESPAWN_POINT_PREFAB.Value))
                    {
                        if (!isAdmin)
                        {
                            var spawnedBy = entity.Read<SpawnedBy>();
                            if (spawnedBy.Value.GetUser().PlatformId != user.GetUser().PlatformId)
                                continue;

                            if (keepAssignedRespawnPoint)
                            {
                                var respawnPoint = entity.Read<RespawnPoint>();
                                if (respawnPoint.RespawnPointOwner._Entity.GetUser().PlatformId ==
                                    user.GetUser().PlatformId)
                                    continue;
                            }
                        }

                        closestSpawnPoint = distance;
                        spawnPointEntity = entity;
                    }
                }
            }

            if (spawnPointEntity != Entity.Null)
            {
                DestroyUtility.DestroyWithReason(entityManager, spawnPointEntity, DestroyReason.Default);
                return true;
            }

            return false;
        }

        public bool SetRespawnPoint(float3 position, Entity user, bool isAdmin = false)
        {
            var entityManager = Core.EntityManager;
            var respawnPointPrefabGuid = new PrefabGUID(Settings.RESPAWN_POINT_PREFAB.Value);

            var query = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<SpawnedBy>(), ComponentType.ReadOnly<Residency>() },
                    Options = EntityQueryOptions.IncludeDisabled
                });

            var respawnPoints = query.ToEntityArray(Allocator.Temp);

            // Find the closest respawn point
            var closestRespawnPointDistance = float.MaxValue;
            Entity closestRespawnPointEntity = Entity.Null;

            foreach (var entity in respawnPoints)
            {
                if (entityManager.TryGetComponentData<LocalToWorld>(entity, out var localToWorld))
                {
                    var distance = math.distance(position, localToWorld.Position);
                    if (distance < closestRespawnPointDistance && entity.GetPrefabGuid() == respawnPointPrefabGuid)
                    {
                        // Check if the spawn point is owned by the user
                        if (entity.Read<SpawnedBy>().Value.GetUser().PlatformId == user.GetUser().PlatformId)
                        {
                            closestRespawnPointDistance = distance;
                            closestRespawnPointEntity = entity;
                        }
                    }
                }
            }

            Plugin.LogInstance.LogWarning("Closest respawn point distance: " + closestRespawnPointDistance);
            if (closestRespawnPointEntity != Entity.Null)
            {
                Plugin.LogInstance.LogWarning("Closest respawn point entity found: " +
                                              closestRespawnPointEntity.GetPrefabGuidName());
                // Assign the closest respawn point to the user
                if (!entityManager.HasComponent<Networked>(closestRespawnPointEntity))
                    closestRespawnPointEntity.Add<Networked>();

                var respawnPointComponent = closestRespawnPointEntity.Read<RespawnPoint>();
                respawnPointComponent.RespawnPointOwner = user;
                respawnPointComponent.HasRespawnPointOwner = true;
                closestRespawnPointEntity.Write(respawnPointComponent);
                Plugin.LogInstance.LogWarning("Respawn point owner set to user: " +
                                              respawnPointComponent.RespawnPointOwner.GetSyncedEntityOrNull()
                                                  .GetUser().PlatformId);

                // Clear ownership from other spawn points owned by the user
                foreach (var entity in respawnPoints)
                {
                    if (entity.Index == closestRespawnPointEntity.Index) continue;

                    var entityRespawn = entity.Read<RespawnPoint>();
                    if (entityRespawn.RespawnPointOwner.GetSyncedEntityOrNull().GetUser().PlatformId ==
                        user.GetUser().PlatformId)
                    {
                        entityRespawn.RespawnPointOwner = NetworkedEntity.Empty;
                        entityRespawn.HasRespawnPointOwner = false;
                        entity.Write(entityRespawn);
                    }
                }

                // Update the RespawnPointOwnerBuffer on the user entity
                var respawnPointOwnerBuffer = user.ReadBuffer<RespawnPointOwnerBuffer>();
                if (respawnPointOwnerBuffer.Length == 0)
                {
                    Plugin.LogInstance.LogInfo("Respawn point owner buffer empty, adding buffer item.");
                    respawnPointOwnerBuffer.Add(new RespawnPointOwnerBuffer
                    {
                        RespawnPoint = closestRespawnPointEntity,
                        RespawnPointNetworkId = closestRespawnPointEntity.GetNetworkId()
                    });
                }
                else
                {
                    for (int i = 0; i < respawnPointOwnerBuffer.Length; i++)
                    {
                        var pointOwnerBuffer = respawnPointOwnerBuffer[i];
                        pointOwnerBuffer.RespawnPoint = closestRespawnPointEntity;
                        pointOwnerBuffer.RespawnPointNetworkId = closestRespawnPointEntity.GetNetworkId();
                        respawnPointOwnerBuffer[i] = pointOwnerBuffer;
                    }
                }

                return true;
            }

            Plugin.LogInstance.LogWarning("No respawn points found.");
            return false;
        }
    }
}