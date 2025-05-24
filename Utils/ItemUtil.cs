using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using VAMP;
using VampireCommandFramework;

namespace VComforts;

public static class ItemUtil
{
    
    private static ManualLogSource _log => Plugin.LogInstance;

    private static NativeArray<Entity> GetItems()
    {
        var itemQuery = Core.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
        {
            All = new[]
            {
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<ItemPickup>()
                
            },
            None = new[] { ComponentType.ReadOnly<DestroyTag>() }
        });
        return itemQuery.ToEntityArray(Allocator.Temp);
    }

    internal static List<Entity> ClosestItems(Entity entity, float radius)
    {
        try
        {
            var items = GetItems();
            var results = new List<Entity>();
            var origin = Core.Server.EntityManager.GetComponentData<LocalToWorld>(entity).Position;

            foreach (var mob in items)
            {
                var position = Core.Server.EntityManager.GetComponentData<LocalToWorld>(mob).Position;
                var distance = UnityEngine.Vector3.Distance(origin, position);
                if (distance < radius)
                {
                    results.Add(mob);
                }
            }

            return results;
        }
        catch (Exception)
        {
            return [];
        }
    }
    
    
    public static bool TryGiveItem(EntityManager entityManager, NativeParallelHashMap<PrefabGUID, ItemData>? itemDataMap,
        Entity recipient, PrefabGUID itemType, int amount, out int remainingitems, bool dropRemainder = false)
    {
            
        itemDataMap ??=  Core.Server.GetExistingSystemManaged<GameDataSystem>().ItemHashLookupMap;
        var itemSettings = AddItemSettings.Create(entityManager, itemDataMap.Value, false, default, default, false, false, dropRemainder);
        AddItemResponse response = InventoryUtilitiesServer.TryAddItem(itemSettings, recipient, itemType, amount);
        remainingitems = response.RemainingAmount;
        return response.Success;
    }
    
    public static bool AddItemToInventory(Entity recipient, PrefabGUID itemType, int amount, Entity recipientUser = default)
    {
        try
        {
            var gameData = Core.Server.GetExistingSystemManaged<GameDataSystem>();
            var itemSettings = AddItemSettings.Create(Core.EntityManager, gameData.ItemHashLookupMap, dropRemainder: true);
#if DEBUG
            string recipientName;
            recipientName = Core.EntityManager.HasComponent<User>(recipientUser) ? recipientUser.GetUser().CharacterName.Value : recipient.GetPrefabGuidName();
            _log.LogInfo($"Trying to add item: {itemType.GetNamePrefab().Split("_")[2]} to {recipientName} with amount {amount}");
#endif
            AddItemResponse response = InventoryUtilitiesServer.TryAddItem(itemSettings, recipient, itemType, amount);
#if DEBUG
            if (response.Result == AddItemResult.Failed_InventoryFull)
            {

                _log.LogInfo($"Inventory full! Dropped item: {itemType.GetNamePrefab().Split("_")[2]} to {recipientName} with amount {amount}");

            }
            else
            {

                _log.LogInfo("Successfully added item to inventory");

            }
#endif
            return true;
        }
        catch (Exception e)
        {
            _log.LogError(e.Message);
            return false;
        }
    }

    public struct InventoryItemInfo
    {
        public Entity ItemEntity;
        public int Amount;
    }

    public static List<InventoryItemInfo> GetInventoryItemInfos(Entity mixer, PrefabGUID valueMixedBloodPotionPrefab)
    {
        var result = new List<InventoryItemInfo>();
        int totalSlots = InventoryUtilities.GetInventorySize(Core.EntityManager, mixer);
        for (int i = 0; i < totalSlots; i++)
        {
            if (InventoryUtilities.TryGetItemAtSlot(Core.EntityManager, mixer, i, out var item))
            {
                if (item.ItemType.Equals(valueMixedBloodPotionPrefab))
                {
                    result.Add(new InventoryItemInfo
                    {
                        ItemEntity = item.ItemEntity._Entity,
                        Amount = item.Amount
                    });
                }
            }
        }
        return result;
    }
    
    public static bool TryConsumeRequiredItems(ChatCommandContext ctx, out string message)
    {
        try
        {
            var requiredItemPrefabGuid = new PrefabGUID(Settings.RESPAWN_POINT_COST_ITEM_PREFAB.Value);
            var characterEntity = ctx.Event.SenderCharacterEntity;
            int totalSlots = InventoryUtilities.GetInventorySize(Core.EntityManager, characterEntity);
            var totalItemsToRemove = Settings.RESPAWN_POINT_COST_AMOUNT.Value;

            for (int i = 0; i < totalSlots; i++)
            {
                if (InventoryUtilities.TryGetItemAtSlot(Core.EntityManager, characterEntity, i, out var item))
                {
                    if (item.ItemType == requiredItemPrefabGuid)
                    {
                        if (item.Amount >= totalItemsToRemove)
                        {
                            InventoryUtilitiesServer.TryRemoveItemAtIndex(Core.EntityManager, characterEntity, item.ItemType, totalItemsToRemove, i, false);
                            totalItemsToRemove = 0;
                            break;
                        }
                        else
                        {
                            InventoryUtilitiesServer.TryRemoveItemAtIndex(Core.EntityManager, characterEntity, item.ItemType, item.Amount, i, true);
                            totalItemsToRemove -= item.Amount;
                        }
                    }
                }
            }

            if (totalItemsToRemove > 0)
            {
                message = $"You do not have enough of {requiredItemPrefabGuid.GetNamePrefab()} to spawn a respawn point.";
                AddItemToInventory(characterEntity, requiredItemPrefabGuid, Settings.RESPAWN_POINT_COST_AMOUNT.Value - totalItemsToRemove);
                return false;
            }

            message = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            message = $"An error occurred while checking inventory: {ex.Message}";
            return false;
        }
    }
}