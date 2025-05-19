using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using System.Text.Json;
using ProjectM;
using Stunlock.Network;
using Unity.Mathematics;
using VAMP;
using VrisingQoL.Database;

namespace VrisingQoL.Patches;

[HarmonyPatch]
public static class InitializePlayer_Patch
{
    public static List<int> playerEntityIndices = new();
    private static readonly string GlobalRespawnPointsFilePath = Path.Combine(BepInEx.Paths.ConfigPath, "Respawns", "globalRespawns.json");

    
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
        catch (System.Exception ex)
        {
            Plugin.LogInstance.LogError(ex);
        }
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