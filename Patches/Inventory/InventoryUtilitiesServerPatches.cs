using Stunlock.Core;
using VAMP;
using HarmonyLib;
using ProjectM;
using Unity.Entities;
using Unity.Collections;
using VComforts.Patches.Connection;

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

        return true;
    }
}

[HarmonyPatch(typeof(InventoryUtilitiesServer), "TryAddItem", typeof(AddItemSettings), typeof(Entity), typeof(InventoryBuffer))]
public static class TryAddItemPatch
{
    static bool Prefix(ref AddItemSettings addItemSettings)
    {
        // Try to get the inventory owner entity (commonly PreviousItemEntity or via settings)
        Entity owner = addItemSettings.PreviousItemEntity;
#if DEBUG
        Plugin.LogInstance.LogWarning("TryAddItemPatch Prefix");
        Plugin.LogInstance.LogWarning("Is owner player? " + owner.IsPlayer());
        owner.LogComponentTypes();
#endif
        if (owner == Entity.Null || !owner.IsPlayer())
            return true;

        var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
        addItemSettings.ItemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(owner, originalMap);

        return true;
    }
}

[HarmonyPatch(typeof(InventoryUtilitiesServer), "TryAddItem", typeof(AddItemSettings), typeof(Entity), typeof(PrefabGUID), typeof(int))]
public static class TryAddItemWithAmountPatch
{
    static bool Prefix(ref AddItemSettings addItemSettings)
    {
        Entity owner = addItemSettings.PreviousItemEntity;
#if DEBUG
        Plugin.LogInstance.LogWarning("TryAddItemWithAmountPatch Prefix");
        Plugin.LogInstance.LogWarning("Is owner player? " + owner.IsPlayer());
        owner.LogComponentTypes();
#endif
        if (owner == Entity.Null || !owner.IsPlayer())
            return true;

        var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
        addItemSettings.ItemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(owner, originalMap);

        return true;
    }
}