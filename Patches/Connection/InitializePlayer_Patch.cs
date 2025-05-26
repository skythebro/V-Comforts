using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Stunlock.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VAMP;
using VComforts.Database;
using VComforts.Systems;

namespace VComforts.Patches.Connection;

[HarmonyPatch]
public static class InitializePlayer_Patch
{
    public static List<int> playerEntityIndices = new();
    private static readonly string GlobalRespawnPointsFilePath = Path.Combine(BepInEx.Paths.ConfigPath, "Respawns", "globalRespawns.json");

    public static Dictionary<int, NativeParallelHashMap<PrefabGUID, ItemData>> PlayerItemDataMaps = new();

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    [HarmonyPostfix]
    public static void OnUserConnected_Patch(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        try
        {
            if (!Settings.ENABLE_RESPAWN_POINTS.Value)
            {
                return;
            }
            
            EnsureRespawnPointsFile();
            var readRespawnPointsFromFile = ReadRespawnPointsFromFile();
            
            var userIndex = Core.Server.GetExistingSystemManaged<ServerBootstrapSystem>()
                ._NetEndPointToApprovedUserIndex[netConnectionId];
            var serverClient = Core.Server.GetExistingSystemManaged<ServerBootstrapSystem>()
                ._ApprovedUsersLookup[userIndex];
            var userEntity = serverClient.UserEntity;

            var user = userEntity.GetUser();
            var player = user.LocalCharacter.GetEntityOnServer();
            var platformId = user.PlatformId;
            
            // Create a custom item data map for the player
            var customItemDataMap = GetUpdatedItemDataMap(player, Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap);
            
            PlayerItemDataMaps[(int)platformId] = customItemDataMap;
            playerEntityIndices.Add(player.Index);
           
            if (!RespawnPointDatabase.IsInitialized(platformId))
            {
                RespawnPointDatabase.InitializePlayer(platformId); // Initialize with 0
            }
            
            playerEntityIndices.Add(player.Index);
            if (!Settings.ENABLE_PREDEFINED_RESPAWN_POINTS.Value)
            {
                return;
            }
            foreach (var pointData in readRespawnPointsFromFile)
            {
                var succeeded = Extensions.RespawnPointSpawnerSystem.SpawnRespawnPoint(pointData.Position.ToFloat3(), pointData.Rotation.ToQuaternion(), player, userEntity);
                if (succeeded) continue;
                Plugin.LogInstance.LogWarning("Failed to spawn respawn point at " + pointData.Position);;
            }
        }
        catch (Exception ex)
        {
            Plugin.LogInstance.LogError(ex);
        }
    }
    
    public static NativeParallelHashMap<PrefabGUID, ItemData> GetUpdatedItemDataMap(
        Entity player,
        NativeParallelHashMap<PrefabGUID, ItemData> originalItemDataMap)
    {
        return GetUpdatedItemDataMap(player, originalItemDataMap, 1.0f);
    }
    
    public static NativeParallelHashMap<PrefabGUID, ItemData> GetUpdatedItemDataMap(Entity player, NativeParallelHashMap<PrefabGUID, ItemData> originalItemDataMap, float multiplier)
    {
        if (Mathf.Approximately(multiplier, 1.0f))
        {
            multiplier = BonusSystem.GetBagMultiplier(player);

        }
#if DEBUG
        Plugin.LogInstance.LogWarning("Multiplier: " + multiplier);
#endif
        var keys = originalItemDataMap.GetKeyArray(Allocator.Temp);
        var values = originalItemDataMap.GetValueArray(Allocator.Temp);

        var updatedItemDataMap = new NativeParallelHashMap<PrefabGUID, ItemData>(originalItemDataMap.Count(), Allocator.Temp);

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var value = values[i];
            if (value.ItemType == ItemType.Stackable) //should make sure that stackable type contains all stackable items
            {
                value.MaxAmount = (int)Math.Clamp(value.MaxAmount * multiplier, 1, 4095); // clamp to max visibile stack size
            } else if (value.ItemType is ItemType.Jewel)
            {
                // not the issue probably, but just in case
                value.MaxAmount = 1;
            }
            updatedItemDataMap[key] = value;
        }

        keys.Dispose();
        values.Dispose();

        return updatedItemDataMap;
    }
    
    public static void EnsureRespawnPointsFile()
    {
        if (!File.Exists(GlobalRespawnPointsFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GlobalRespawnPointsFilePath) ?? string.Empty);
            var defaultRespawnPoints = new List<RespawnPointData>();
            var json = JsonSerializer.Serialize(defaultRespawnPoints);
            File.WriteAllText(GlobalRespawnPointsFilePath, json);
        }
    }
    
    public static void WriteRespawnPointsToFile(List<RespawnPointData> respawnPoints)
    {
        var json = JsonSerializer.Serialize(respawnPoints);
        File.WriteAllText(GlobalRespawnPointsFilePath, json);
    }

    public static List<RespawnPointData> ReadRespawnPointsFromFile()
    {
        var json = File.ReadAllText(GlobalRespawnPointsFilePath);
        return JsonSerializer.Deserialize<List<RespawnPointData>>(json);
    }
}

public class RespawnPointData
{
    public SerializableFloat3 Position { get; set; }
    public SerializableQuaternion Rotation { get; set; }
}

public record struct SerializableFloat3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public SerializableFloat3(float3 f)
    {
        X = f.x;
        Y = f.y;
        Z = f.z;
    }

    public float3 ToFloat3() => new float3(X, Y, Z);
}

public record struct SerializableQuaternion
{
    public float x { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }

    public SerializableQuaternion(quaternion q)
    {
        x = q.value.x;
        Y = q.value.y;
        Z = q.value.z;
        W = q.value.w;
    }

    public quaternion ToQuaternion() => new quaternion(x, Y, Z, W);
}