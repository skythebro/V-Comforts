using Stunlock.Core;
using VAMP;
using HarmonyLib;
using ProjectM;
using Unity.Entities;
using Unity.Collections;
using VComforts.Patches.Connection;
using VComforts.Systems;

namespace VComforts.Patches.Inventory;

[HarmonyPatch(typeof(InventoryUtilitiesServer), "TrySortInventory")]
public static class TrySortInventoryPatch
{
    static bool Prefix(ref NativeParallelHashMap<PrefabGUID, ItemData> itemDataMap, Entity target)
    {
        if (target == Entity.Null) return true;
        if (!target.IsPlayer()) return true;

        Plugin.LogInstance.LogWarning("Owner of inventory is a player, applying custom itemDataMap");
        var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
        itemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(target, originalMap);
        BonusSystem.HandleInventoryBuffs(target);
        return true;
    }
}

[HarmonyPatch(typeof(InventoryUtilitiesServer), "Internal_TryMoveItem")]
public static class InternalTryMoveItemPatch
{
    static bool Prefix(
        ref NativeParallelHashMap<PrefabGUID, ItemData> itemDataMap,
        Entity toInventoryEntity,
        EntityManager entityManager)
    {
        if (toInventoryEntity == Entity.Null ||
            !entityManager.HasComponent<InventoryConnection>(toInventoryEntity)) return true;

        var inventoryConnection = toInventoryEntity.Read<InventoryConnection>();
        var owner = inventoryConnection.InventoryOwner;

        if (owner == Entity.Null || !owner.IsPlayer()) return true;

        // Always use the original map as the base
        var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
        itemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(owner, originalMap);
        BonusSystem.HandleInventoryBuffs(owner);
        return true;
    }
}

[HarmonyPatch(typeof(InventoryUtilitiesServer), "SplitItemStacks")]
public static class SplitItemStacksPatch
{
    static bool Prefix(
        ref NativeParallelHashMap<PrefabGUID, ItemData> itemDataMap,
        Entity inventoryOwner)
    {
        if (inventoryOwner == Entity.Null || !inventoryOwner.IsPlayer()) return true;

        var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
        itemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(inventoryOwner, originalMap);
        BonusSystem.HandleInventoryBuffs(inventoryOwner);
        return true;
    }
}

[HarmonyPatch(typeof(InventoryUtilitiesServer), "SplitItemStacksV2")]
public static class SplitItemStacksV2Patch
{
    static bool Prefix(
        ref NativeParallelHashMap<PrefabGUID, ItemData> itemDataMap,
        Entity inventoryOwner)
    {
        if (inventoryOwner == Entity.Null || !inventoryOwner.IsPlayer()) return true;
        var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
        itemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(inventoryOwner, originalMap);
        BonusSystem.HandleInventoryBuffs(inventoryOwner);
        return true;
    }
}

[HarmonyPatch(typeof(InventoryUtilitiesServer), "TryAddItem", typeof(AddItemSettings), typeof(Entity),
    typeof(InventoryBuffer))]
public static class TryAddItemPatch
{
    static bool Prefix(ref AddItemSettings addItemSettings, Entity target)
    {
        // Try to get the inventory owner entity (commonly PreviousItemEntity or via settings)
        Entity owner = target;
#if DEBUG
        Plugin.LogInstance.LogWarning("TryAddItemPatch Prefix");
        addItemSettings.PreviousItemEntity.LogComponentTypes();
#endif
        if (owner == Entity.Null || !owner.IsPlayer())
        {
            // If the target is not a player, we skip the patch
            return true;
        }

#if DEBUG
        Plugin.LogInstance.LogWarning("Is owner player? " + owner.IsPlayer());
#endif
        var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
        addItemSettings.ItemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(owner, originalMap);
        BonusSystem.HandleInventoryBuffs(owner);
        return true;
    }
}

[HarmonyPatch(typeof(InventoryUtilitiesServer), "TryAddItem", typeof(AddItemSettings), typeof(Entity),
    typeof(PrefabGUID), typeof(int))]
public static class TryAddItemWithAmountPatch
{
    static bool Prefix(ref AddItemSettings addItemSettings, Entity target)
    {
        if (target == Entity.Null || !target.IsPlayer())
        {
            return true;
        }
#if DEBUG
        Plugin.LogInstance.LogWarning("TryAddItemWithAmountPatch Prefix");
        target.LogComponentTypes();
#endif
        var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
        addItemSettings.ItemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(target, originalMap);
        BonusSystem.HandleInventoryBuffs(target);
        return true;
    }
}

[HarmonyPatch(typeof(InventoryUtilitiesServer), "Internal_TryAddItemInInventory")]
public static class InternalTryAddItemInInventoryPatch
{
    static bool Prefix(
        ref AddItemSettings addItemSettings,
        Entity inventoryEntity)
    {
        if (!inventoryEntity.Has<InventoryConnection>())
        {
            return true;
        }
        var player = inventoryEntity.Read<InventoryConnection>().InventoryOwner;

        if (!player.IsPlayer())
        {
            return true;
        }
#if DEBUG
        Plugin.LogInstance.LogWarning("TryAddItemWithAmountPatch Prefix");
        inventoryEntity.LogComponentTypes();
#endif
        var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
        addItemSettings.ItemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(player, originalMap);
        BonusSystem.HandleInventoryBuffs(player);
        return true;
    }
}