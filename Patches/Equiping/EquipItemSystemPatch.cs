using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using VAMP;
using VComforts.Systems;

namespace VComforts.Patches.Equiping;

[HarmonyPatch(typeof(EquipItemSystem), "OnUpdate")]
public static class EquipItemSystemPatch
{
    public static void Postfix(EquipItemSystem __instance)
    {
        if (__instance._EventQuery.IsEmpty)
        {
            return;
        }

        if (!Settings.ENABLE_INVENTORY_BONUS.Value && !Settings.ENABLE_LEVEL_BONUS.Value) return;
        // Not checking much here as there is no good data being passed
        foreach (var entity in __instance._EventQuery.ToEntityArray(Allocator.Temp))
        {
            var fromCharacter = entity.Read<FromCharacter>();
            if (Settings.ENABLE_LEVEL_BONUS.Value) BonusSystem.HandleLevelBuffs(fromCharacter.Character);
            if (Settings.ENABLE_INVENTORY_BONUS.Value) BonusSystem.HandleInventoryBuffs(fromCharacter.Character);
        }
    }
}