using System;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;
using VAMP;
using VAMP.Data;
using VComforts.Patches.Connection;

namespace VComforts.Systems;

public static class BonusSystem
{
    private const float BaseResourceYield = 1f;
    private const float BaseMoveSpeed = 0f;
    private const float BaseShapeshiftMoveSpeed = 0f;

    private static int GetPlayerLevel(this Equipment equipment)
    {
        int weaponLevel = (int)Math.Round(equipment.WeaponLevel);
        int armorLevel = (int)Math.Round(equipment.ArmorLevel);
        int spellLevel = (int)Math.Round(equipment.SpellLevel);

        return weaponLevel + armorLevel + spellLevel;
    }

    public static void HandleLevelBuffs(Entity entity)
    {
        var equipment = Core.EntityManager.GetComponentData<Equipment>(entity);
        var attributes = Core.EntityManager.GetComponentData<VampireSpecificAttributes>(entity);

        // Calculate own bonuses
        int playerLevel = equipment.GetPlayerLevel();
        if (Settings.LEVEL_BONUS_MULTIPLIER.Count != 3)
        {
            Plugin.LogInstance.LogError("LEVEL_BONUS_MULTIPLIER must have 3 values!");
            ;
            return;
        }

        float resourceYieldBonus = playerLevel * Settings.LEVEL_BONUS_MULTIPLIER[0]; // make multiplier customizable?
        float moveSpeedBonus = playerLevel * Settings.LEVEL_BONUS_MULTIPLIER[1];
        float shapeshiftMoveSpeedBonus = playerLevel * Settings.LEVEL_BONUS_MULTIPLIER[2];

        // Add bonus to the current value
        attributes.ResourceYieldModifier._Value = BaseResourceYield + resourceYieldBonus;
        attributes.BonusMovementSpeed._Value = BaseMoveSpeed + moveSpeedBonus;
        attributes.BonusShapeshiftMovementSpeed._Value = BaseShapeshiftMoveSpeed + shapeshiftMoveSpeedBonus;

        Core.EntityManager.SetComponentData(entity, attributes);
    }

    public static void HandleInventoryBuffs(Entity entity, float multiplierOverride = 1.0f)
    {
        var inventoryInstanceBuffer = Core.EntityManager.GetBuffer<InventoryInstanceElement>(entity);

        if (Settings.BAG_INVENTORY_BONUS_MULTIPLIER.Count != 6)
        {
            Plugin.LogInstance.LogError("BAG_INVENTORY_BONUS_MULTIPLIER must have 6 values!");
            return;
        }

        foreach (var inventoryInstance in inventoryInstanceBuffer)
        {
            var externalInventoryEntity = inventoryInstance.ExternalInventoryEntity;
            if (externalInventoryEntity.GetSyncedEntityOrNull() == Entity.Null) continue;
            var inventoryBuffer =
                Core.EntityManager.GetBuffer<InventoryBuffer>(externalInventoryEntity.GetSyncedEntityOrNull());

            for (int i = 0; i < inventoryBuffer.Length; i++)
            {
                var slot = inventoryBuffer[i];

                int baseStackSize = 0;
                if (slot.ItemEntity.GetSyncedEntityOrNull() == Entity.Null)
                {
                    var key = new PrefabGUID() { _Value = slot.ItemType._Value };
                    if (Core.SystemService.PrefabCollectionSystem._PrefabLookupMap.TryGetValue(key, out var prefab))
                    {
                        if (prefab.TryGetComponent<ItemData>(out var itemData))
                        {
                            baseStackSize = itemData.MaxAmount;
                        }
                    }
                }

                if (!Mathf.Approximately(multiplierOverride, 1.0f))
                {
                    slot.MaxAmountOverride = (int)(baseStackSize * multiplierOverride);
                }
                else
                {
                    slot.MaxAmountOverride = (int)(baseStackSize * GetBagMultiplier(entity));
                }
                inventoryBuffer[i] = slot;
            }
        }
    }

    public static int GetMaxAmountOverride(Entity playerCharacter, InventoryBuffer item)
    {
        var baseStackSize = 0;
        if (item.ItemEntity.GetSyncedEntityOrNull() == Entity.Null)
        {
            var key = new PrefabGUID() { _Value = item.ItemType._Value };
            if (Core.SystemService.PrefabCollectionSystem._PrefabLookupMap.TryGetValue(key, out var prefab))
            {
                if (prefab.TryGetComponent<ItemData>(out var itemData))
                {
                    baseStackSize = itemData.MaxAmount;
                }
            }
        }

        return item.MaxAmountOverride = (int)(baseStackSize * GetBagMultiplier(playerCharacter));
    }

    public static float GetBagMultiplier(Entity playerCharacter)
    {
        if (Settings.BAG_INVENTORY_BONUS_MULTIPLIER.Count != 6)
        {
            Plugin.LogInstance.LogError("BAG_INVENTORY_BONUS_MULTIPLIER must have 6 values! Setting multiplier to 1");
            return 1.0f;
        }

        var equipment = Core.EntityManager.GetComponentData<Equipment>(playerCharacter);
        var bagSlot = equipment.BagSlot;

        float bagMultiplier = 1.0f;
        if (!bagSlot.IsBroken)
        {
            if (bagSlot.SlotEntity.GetSyncedEntityOrNull() != Entity.Null)
            {
                if (bagSlot.SlotId.Equals(Prefabs.Item_NewBag_T01))
                    bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[0];
                else if (bagSlot.SlotId.Equals(Prefabs.Item_NewBag_T02))
                    bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[1];
                else if (bagSlot.SlotId.Equals(Prefabs.Item_NewBag_T03))
                    bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[2];
                else if (bagSlot.SlotId.Equals(Prefabs.Item_NewBag_T04))
                    bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[3];
                else if (bagSlot.SlotId.Equals(Prefabs.Item_NewBag_T05))
                    bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[4];
                else if (bagSlot.SlotId.Equals(Prefabs.Item_NewBag_T06))
                    bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[5];
            }
        }
        
        return bagMultiplier;
    }

    public static float GetBagMultiplierByPrefab(PrefabGUID prefab)
    {
        if (Settings.BAG_INVENTORY_BONUS_MULTIPLIER.Count != 6)
        {
            Plugin.LogInstance.LogError("BAG_INVENTORY_BONUS_MULTIPLIER must have 6 values! Setting multiplier to 1");
            return 1.0f;
        }

        float bagMultiplier = 1.0f;

        if (prefab.Equals(Prefabs.Item_NewBag_T01))
            bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[0];
        else if (prefab.Equals(Prefabs.Item_NewBag_T02))
            bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[1];
        else if (prefab.Equals(Prefabs.Item_NewBag_T03))
            bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[2];
        else if (prefab.Equals(Prefabs.Item_NewBag_T04))
            bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[3];
        else if (prefab.Equals(Prefabs.Item_NewBag_T05))
            bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[4];
        else if (prefab.Equals(Prefabs.Item_NewBag_T06))
            bagMultiplier = Settings.BAG_INVENTORY_BONUS_MULTIPLIER[5];


        return bagMultiplier;
    }
}