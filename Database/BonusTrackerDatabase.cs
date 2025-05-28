using System;
using System.IO;
using System.Text.Json;

namespace VComforts.Database;

public class BonusTrackerDatabase
{
    private static string GetFilePath(ulong platformID) =>
        Path.Combine(BepInEx.Paths.ConfigPath, "PlayerBonuses", $"bonus_{platformID}.json");

    public static PlayerBonusData Load(ulong platformID)
    {
        var path = GetFilePath(platformID);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        try
        {
            return JsonSerializer.Deserialize<PlayerBonusData>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void Save(ulong platformID, PlayerBonusData data)
    {
        var dir = Path.GetDirectoryName(GetFilePath(platformID));
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(data);
        File.WriteAllText(GetFilePath(platformID), json);
    }

    public static void Delete(ulong platformID)
    {
        var path = GetFilePath(platformID);
        if (File.Exists(path)) File.Delete(path);
    }
}

public class PlayerBonusData
{
    public float OriginalResourceYield { get; set; }
    public float OriginalMoveSpeed { get; set; }
    public float OriginalShapeshiftMoveSpeed { get; set; }
    public float LastResourceYieldBonus { get; set; }
    public float LastMoveSpeedBonus { get; set; }
    public float LastShapeshiftMoveSpeedBonus { get; set; }
}