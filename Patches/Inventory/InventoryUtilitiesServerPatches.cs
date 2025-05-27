using System;
using System.Collections.Generic;
using Stunlock.Core;
using VAMP;
using HarmonyLib;
using ProjectM;
using Unity.Entities;
using Unity.Collections;
using VAMP.Data;
using VComforts.Patches.Connection;
using VComforts.Systems;

namespace VComforts.Patches.Inventory;

[HarmonyPatch(typeof(InventoryUtilitiesServer), "TrySortInventory")]
public static class TrySortInventoryPatch
{
    static bool Prefix(ref NativeParallelHashMap<PrefabGUID, ItemData> itemDataMap, Entity target)
    {
        if (target == Entity.Null) return true;
        if (target.IsPlayer())
        {
#if DEBUG
            Plugin.LogInstance.LogWarning(
                "TrySortInventoryPatch: Owner of inventory is a player, applying custom itemDataMap");
#endif
            var originalMap = Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap;
            itemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(target, originalMap);
            BonusSystem.HandleInventoryBuffs(target);
            return true;
        }

        return true;
    }

    static void Postfix(Entity target)
{
    if (target == Entity.Null) return;
    if (!IsBloodPotionStorage(target)) return;

    var attachedBuffer = target.ReadBuffer<AttachedBuffer>();
    var invBuffer = attachedBuffer[0].Entity.ReadBuffer<InventoryBuffer>();

    var potionSlots = new List<int>();
    var potions = new List<(InventoryBuffer item, string primaryType, float quality, bool hasSecondary, string secondaryType, float secondaryQuality, int originalIndex)>();

    for (int i = 0; i < invBuffer.Length; i++)
    {
        var itemType = invBuffer[i].ItemType;
        if (itemType == Prefabs.Item_Consumable_PrisonPotion_Bloodwine ||
            itemType == Prefabs.Item_Consumable_PrisonPotion ||
            itemType == Prefabs.Item_Consumable_PrisonPotion_Mixed)
        {
            var itemEntity = invBuffer[i].ItemEntity.GetEntityOnServer();
            if (!itemEntity.Has<StoredBlood>()) continue;
            var storedBlood = itemEntity.Read<StoredBlood>();
            var primaryType = storedBlood.PrimaryBloodType.GetNamePrefab().Split("_")[1];
            var quality = storedBlood.BloodQuality;
            var hasSecondary = !storedBlood.SecondaryBlood.IsEmpty;
            potionSlots.Add(i);
            var secondaryType = storedBlood.SecondaryBlood.IsEmpty ? "" : storedBlood.SecondaryBlood.Type.GetNamePrefab().Split("_")[1];
            var secondaryQuality = storedBlood.SecondaryBlood.IsEmpty ? 0f : storedBlood.SecondaryBlood.Quality;
            potions.Add((invBuffer[i], primaryType, quality, hasSecondary, secondaryType, secondaryQuality, i));
        }
    }

    var sortMode = Settings.CUSTOM_BLOODPOTION_SORTING_PRIMARYTHENQUALITY.Value
        ? BloodPotionSortMode.PrimaryTypeThenQuality
        : BloodPotionSortMode.PrimaryOnlyFirstThenSecondary;

    switch (sortMode)
    {
        case BloodPotionSortMode.PrimaryTypeThenQuality:
            potions.Sort((a, b) =>
            {
                int cmp = string.Compare(a.primaryType, b.primaryType, StringComparison.Ordinal);
                if (cmp != 0) return cmp;
                cmp = string.Compare(a.secondaryType, b.secondaryType, StringComparison.Ordinal);
                if (cmp != 0) return cmp;
                cmp = b.quality.CompareTo(a.quality);
                if (cmp != 0) return cmp;
                cmp = b.secondaryQuality.CompareTo(a.secondaryQuality);
                if (cmp != 0) return cmp;
                return a.originalIndex.CompareTo(b.originalIndex);
            });
            break;
        case BloodPotionSortMode.PrimaryOnlyFirstThenSecondary:
            potions.Sort((a, b) =>
            {
                if (a.hasSecondary != b.hasSecondary)
                    return a.hasSecondary ? 1 : -1;
                int cmp = string.Compare(a.primaryType, b.primaryType, StringComparison.Ordinal);
                if (cmp != 0) return cmp;
                cmp = string.Compare(a.secondaryType, b.secondaryType, StringComparison.Ordinal);
                if (cmp != 0) return cmp;
                cmp = b.quality.CompareTo(a.quality);
                if (cmp != 0) return cmp;
                cmp = b.secondaryQuality.CompareTo(a.secondaryQuality);
                if (cmp != 0) return cmp;
                return a.originalIndex.CompareTo(b.originalIndex);
            });
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }

    for (int i = 0; i < potionSlots.Count; i++)
    {
        invBuffer[potionSlots[i]] = potions[i].item;
    }
}
    
    private static bool IsBloodPotionStorage(Entity entity)
    {
        return entity.GetPrefabGuid() == Prefabs.TM_Castle_Container_Specialized_Consumable_T01 || entity.GetPrefabGuid() == Prefabs.TM_Castle_Container_Specialized_Consumable_T02 || entity.GetPrefabGuid() == Prefabs.TM_Castle_Container_Bookshelf_6x2_Blood01 || entity.GetPrefabGuid() == Prefabs.TM_Castle_Container_Bookshelf_3x2_Blood01;
    }
    
    private enum BloodPotionSortMode
    {
        PrimaryTypeThenQuality,
        PrimaryOnlyFirstThenSecondary
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