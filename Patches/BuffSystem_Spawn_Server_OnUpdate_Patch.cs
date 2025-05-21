using System;
using System.Collections.Generic;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VAMP;
using VAMP.Data;
using VrisingQoL.Utils;

namespace VrisingQoL.Patches;

using HarmonyLib;
using ProjectM;

[HarmonyPatch(typeof(BuffSystem_Spawn_Server), "OnUpdate")]
public static class BuffSystem_Spawn_Server_OnUpdate_Patch
{
    private static DateTime _noUpdateBefore = DateTime.MinValue;
    private static readonly PrefabGUID MarkerIcon = Prefabs.MapIcon_POI_Discover_Merchant;


    static void Postfix(BuffSystem_Spawn_Server __instance)
    {
        if (Settings.ENABLE_CARRIAGE_TRACKING.Value)
        {
            if (_noUpdateBefore <= DateTime.Now)
            {
                _noUpdateBefore = DateTime.Now.AddSeconds(5);


                List<Entity> carriages = [];
                var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
                    .AddAll(ComponentType.ReadOnly<Minion>())
                    .AddAll(ComponentType.ReadOnly<Script_CarriageData>())
                    .AddAll(ComponentType.ReadOnly<Torture>())
                    .AddAll(ComponentType.ReadOnly<MinionMaster>())
                    .AddAll(ComponentType.ReadOnly<TeamReference>())
                    .AddAll(ComponentType.ReadOnly<TilePosition>())
                    .WithOptions(EntityQueryOptions.IncludeDisabled);
                var querycarriages = Core.EntityManager.CreateEntityQuery(ref queryBuilder);
                foreach (var carriage in querycarriages.ToEntityArray(Allocator.Temp))
                {
                    if (carriage.GetPrefabGuidName().ToLower().StartsWith("char_carriage"))
                    {
                        carriages.Add(carriage);
                    }
                }

                patrolChanger();
                Plugin.LogInstance.LogWarning("Found " + carriages.Count + " carriage:");
                foreach (var carriage in carriages)
                {
                    

                    if (carriage.Has<Hideable>())
                        carriage.Remove<Hideable>();
                    if (carriage.Has<Stealthable>())
                        carriage.Remove<Stealthable>();
                    if (carriage.Has<HideOutsideVision>())
                        carriage.Remove<HideOutsideVision>();
                    if (carriage.Has<DisableWhenNoPlayersInRange>())
                        carriage.Remove<DisableWhenNoPlayersInRange>();
                    if (!carriage.Has<AlwaysNetworked>())
                        carriage.Add<AlwaysNetworked>();
                    if (!carriage.Has<PreventDisableWhenNoPlayersInRange>())
                        carriage.Add<PreventDisableWhenNoPlayersInRange>();

                    var carriageHorse = carriage.Read<EntityOwner>().Owner;
                    if (carriageHorse.Has<Hideable>())
                        carriageHorse.Remove<Hideable>();
                    if (carriageHorse.Has<Stealthable>())
                        carriageHorse.Remove<Stealthable>();
                    if (carriageHorse.Has<HideOutsideVision>())
                        carriageHorse.Remove<HideOutsideVision>();
                    if (carriageHorse.Has<DisableWhenNoPlayersInRange>())
                        carriageHorse.Remove<DisableWhenNoPlayersInRange>();
                    if (!carriageHorse.Has<AlwaysNetworked>())
                        carriageHorse.Add<AlwaysNetworked>();
                    if (!carriageHorse.Has<PreventDisableWhenNoPlayersInRange>())
                        carriageHorse.Add<PreventDisableWhenNoPlayersInRange>();

                    // Query for map icons targeting this carriage
                    var mapIconQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
                        .AddAll(ComponentType.ReadOnly<MapIconTargetEntity>());
                    var mapIconQuery = Core.EntityManager.CreateEntityQuery(ref mapIconQueryBuilder);
                    var mapIcons = mapIconQuery.ToEntityArray(Allocator.Temp);

                    bool foundIcon = false;
                    foreach (var icon in mapIcons)
                    {
                        var target = icon.Read<MapIconTargetEntity>().TargetEntity.GetEntityOnServer();
                        if (target == carriage)
                        {
                            foundIcon = true;
                            Plugin.LogInstance.LogWarning(
                                $"Carriage at {carriage.Read<LocalToWorld>().Position} already has map icon: {icon.Index}");
                            Plugin.LogInstance.LogWarning($"MapIcon Translation: {icon.Read<Translation>().Value}");
                            Plugin.LogInstance.LogWarning(
                                $"MapIcon LocalToWorld: {icon.Read<LocalToWorld>().Position}");
                            break;
                        }
                    }

                    if (!foundIcon)
                    {
                        var position = carriage.Read<LocalToWorld>().Position;
                        if (math.abs(position.x) > 3000f ||
                            math.abs(position.y) > 3000f ||
                            math.abs(position.z) > 3000f)
                        {
                            Plugin.LogInstance.LogWarning($"carriage at {position} is outside the map bounds.");
                            continue;
                        }

                        Plugin.LogInstance.LogWarning("Carriage inside map at: " +
                                                      carriage.Read<LocalToWorld>().Position);
                        Plugin.LogInstance.LogWarning("Found carriage checking map icon");
                        if (!carriage.TryGetBuffer<AttachMapIconsToEntity>(out var iconBuffer) ||
                            !BufferContainsIcon(iconBuffer, MarkerIcon))
                        {
                            if (!Core.EntityManager.TryGetBuffer<AttachMapIconsToEntity>(carriage, out _))
                                iconBuffer = Core.EntityManager.AddBuffer<AttachMapIconsToEntity>(carriage);

                            iconBuffer.Add(new AttachMapIconsToEntity
                            {
                                Prefab = MarkerIcon
                            });
                            Plugin.LogInstance.LogWarning("Added new map icon to carriage.");
                        }
                        else
                        {
                            Plugin.LogInstance.LogWarning("Carriage already has this map icon.");
                        }

                        var carriagePosition = carriage.Read<LocalToWorld>().Position;
                        var translation = carriage.Read<Translation>().Value;
                        float3 spawnPosition = new float3(carriagePosition.x, translation.y, carriagePosition.z);
                        if (!Core.EntityManager.Exists(carriage))
                        {
                            Plugin.LogInstance.LogWarning("Carriage entity no longer exists.");
                        }
                        else if (!carriage.Has<LocalToWorld>())
                        {
                            Plugin.LogInstance.LogWarning("Carriage entity missing LocalToWorld.");
                        }
                        else
                        {
                            MapIconUtil.SpawnMapIcon(MarkerIcon, spawnPosition, carriage);
                        }
                    }
                }

                MapIconUtil.UpdateIcons();
            }


            
        }

        if (Settings.ENABLE_AUTOFISH.Value && !__instance._Query.IsEmpty)
        {
            foreach (var buffs in __instance._Query.ToEntityArray(Allocator.Temp))
            {
                if (buffs.GetBuffTarget().GetPrefabGuid() == new PrefabGUID(1559481073)) // fish
                {
                    if (buffs.GetPrefabGuid() == new PrefabGUID(1753229314)) // ready to catch
                    {
                        Entity owner = buffs.Read<EntityOwner>().Owner;
                        if (!owner.Has<PlayerCharacter>())
                        {
                            continue;
                        }

                        // buff owner is player so apply AB_Fishing_Draw_Buff to player
                        BuffUtil.AddBuff(owner, new PrefabGUID(552896431), 1);
                    }
                }
            }
        }
    }
    
    
    private static void patrolChanger()
    {
        // Query for possible controller entities
        var controllerQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(ComponentType.ReadOnly<MovePatrolState>())
            .AddAll(ComponentType.ReadOnly<UnitCompositionGroupEntry>()).AddAll(ComponentType.ReadOnly<PatrolBusStopNode>())
            .AddAll(ComponentType.ReadOnly<WaypointTargetBufferEntry>()).WithOptions(EntityQueryOptions.IncludeDisabled);
        var controllerQuery = Core.EntityManager.CreateEntityQuery(ref controllerQueryBuilder);
        foreach (var canditate in controllerQuery.ToEntityArray(Allocator.Temp))
        {
            if (canditate.TryGetBuffer<FollowerBuffer>(out var followerBuffer))
            {
                foreach (var follower in followerBuffer)
                {
                    var horse = follower.Entity;
                    if (horse.GetEntityOnServer().GetPrefabGuidName().ToLower().StartsWith("char_carriagehorse"))
                    {
                        // Found the horse, now get its minions (should include the carriage)
                        if (horse.GetEntityOnServer().TryGetBuffer<MinionBuffer>(out var minionBuffer))
                        {
                            foreach (var minion in minionBuffer)
                            {
                                var foundCarriage = minion.Entity;
                                if (foundCarriage.GetPrefabGuidName().ToLower().StartsWith("char_carriage"))
                                {
                                    if (!canditate.Has<AlwaysNetworked>()) canditate.Add<AlwaysNetworked>();
                                    if (!canditate.Has<PreventDisableWhenNoPlayersInRange>())
                                        canditate.Add<PreventDisableWhenNoPlayersInRange>();  
                                }
                            }
                        }
                    }
                }
            }
        }

        
    }

    private static bool BufferContainsIcon(DynamicBuffer<AttachMapIconsToEntity> buffer, PrefabGUID markerIcon)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].Prefab.Equals(markerIcon))
                return true;
        }

        return false;
    }
}