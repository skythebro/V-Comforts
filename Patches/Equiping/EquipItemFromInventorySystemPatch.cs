using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using VAMP;
using VComforts.Patches.Connection;
using VComforts.Systems;
using VComforts.Utils;

namespace VComforts.Patches.Equiping;

[HarmonyPatch(typeof(EquipItemFromInventorySystem), "OnUpdate")]
public static class EquipItemFromInventorySystemPatch
{
    public static void Postfix(EquipItemFromInventorySystem __instance)
    {
        if (!Settings.ENABLE_LEVEL_BONUS.Value) return;
        if (__instance._Query.IsEmpty)
        {
            return;
        }
        
        // Less efficient but cannot check for the item in a postfix
        foreach (var entity in __instance._Query.ToEntityArray(Allocator.Temp))
        {
            var fromCharacter = entity.Read<FromCharacter>();
            DelayedUtil.RunLevelBuffsDelayed(fromCharacter.Character);
        }
    }
}