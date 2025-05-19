using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.Mathematics;
using VrisingQoL.Patches;

namespace VrisingQoL.Database;

public static class RespawnPointDatabase
{
    private static readonly string RespawnPointsDBFilePath = Path.Combine(BepInEx.Paths.ConfigPath, "Respawns", "RespawnPointData.json");
    private static readonly Dictionary<ulong, UserRespawnData> UserRespawnDataMap = new();

    public static void InitializePlayer(ulong platformId)
    {
        if (!UserRespawnDataMap.ContainsKey(platformId))
        {
            UserRespawnDataMap[platformId] = new UserRespawnData { Count = 0, Locations = [] };
            SaveToFile();
        }
    }
    
    public static bool IsInitialized(ulong platformId)
    {
        return UserRespawnDataMap.ContainsKey(platformId);
    }
    
    public static int GetRespawnPointCount(ulong platformId)
    {
        return UserRespawnDataMap.GetValueOrDefault(platformId)?.Count ?? 0;
    }

    public static List<float3> GetRespawnPointLocations(ulong platformId)
    {
        var serializableList = UserRespawnDataMap.GetValueOrDefault(platformId)?.Locations ?? new List<SerializableFloat3>();
        var float3List = new List<float3>(serializableList.Count);
        foreach (var s in serializableList)
        {
            float3List.Add(s.ToFloat3());
        }
        return float3List;
    }

    public static void AddRespawnPoint(ulong platformId, float3 location)
    {
        if (!UserRespawnDataMap.ContainsKey(platformId))
        {
            InitializePlayer(platformId);
        }

        var userData = UserRespawnDataMap[platformId];
        userData.Locations.Add(new SerializableFloat3(location));
        userData.Count++;
        SaveToFile();
    }

    public static void RemoveRespawnPoint(ulong platformId, float3 location)
    {
        if (UserRespawnDataMap.TryGetValue(platformId, out var userData))
        {
            if (userData.Locations.Remove(new SerializableFloat3(location)))
            {
                userData.Count--;
                SaveToFile();
            }
        }
    }

    public static void LoadFromFile()
    {
        if (!Directory.Exists(Path.GetDirectoryName(RespawnPointsDBFilePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RespawnPointsDBFilePath) ?? string.Empty);
        }

        if (File.Exists(RespawnPointsDBFilePath))
        {
            var json = File.ReadAllText(RespawnPointsDBFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<ulong, UserRespawnData>>(json);
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    UserRespawnDataMap[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    private static void SaveToFile()
    {
        if (!Directory.Exists(Path.GetDirectoryName(RespawnPointsDBFilePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RespawnPointsDBFilePath) ?? string.Empty);
        }

        var json = JsonSerializer.Serialize(UserRespawnDataMap);
        File.WriteAllText(RespawnPointsDBFilePath, json);
    }
}

public class UserRespawnData
{
    public int Count { get; set; }
    public List<SerializableFloat3> Locations { get; set; }
}