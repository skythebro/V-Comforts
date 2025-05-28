using System;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;
using VAMP;
using VAMP.Data;
using VComforts.Database;
using VComforts.Patches.Connection;

namespace VComforts.Systems;

public static class BonusSystem
{
    private const float BaseResourceYield = 1f;
    private const float BaseMoveSpeedBonus = 0f;
    private const float BaseShapeshiftMoveSpeedBonus = 0f;

    private static int GetPlayerLevel(this Equipment equipment)
    {
        return (int)Math.Round(equipment.GetFullLevel());
    }

    public static void HandleLevelBuffs(Entity entity, bool overrideResetLevel = false)
    {
        var equipment = entity.Read<Equipment>();
        var attributes = entity.Read<VampireSpecificAttributes>();

        // Calculate own bonuses
        int playerLevel = equipment.GetPlayerLevel();
#if DEBUG
        Plugin.LogInstance.LogWarning("Player Level: " + playerLevel);
#endif
        if (Settings.LEVEL_BONUS_MULTIPLIER.Count != 3)
        {
            Plugin.LogInstance.LogError("LEVEL_BONUS_MULTIPLIER must have 3 values!");
            return;
        }

        float resourceYieldBonus = playerLevel * Settings.LEVEL_BONUS_MULTIPLIER[0]; // make multiplier customizable?
        float moveSpeedBonus = playerLevel * Settings.LEVEL_BONUS_MULTIPLIER[1];
        float shapeshiftMoveSpeedBonus = playerLevel * Settings.LEVEL_BONUS_MULTIPLIER[2];

        var bonusData = BonusTrackerDatabase.Load(entity.GetUser().PlatformId);

        if (bonusData == null)
        {
            // First time: store original values
            bonusData = new PlayerBonusData
            {
                OriginalResourceYield = Mathf.Approximately(attributes.ResourceYieldModifier._Value, BaseResourceYield)
                    ? attributes.ResourceYieldModifier._Value
                    : BaseResourceYield,
                OriginalMoveSpeed = Mathf.Approximately(attributes.BonusMovementSpeed._Value, BaseMoveSpeedBonus)
                    ? attributes.BonusMovementSpeed._Value
                    : BaseMoveSpeedBonus,
                OriginalShapeshiftMoveSpeed = Mathf.Approximately(attributes.BonusShapeshiftMovementSpeed._Value,
                    BaseShapeshiftMoveSpeedBonus)
                    ? attributes.BonusShapeshiftMovementSpeed._Value
                    : BaseShapeshiftMoveSpeedBonus
            };
        }

        // Add bonus to the current value
        if (overrideResetLevel)
        {
            // Reset to original or base if not set
            attributes.ResourceYieldModifier._Value = bonusData.OriginalResourceYield != 0f
                ? bonusData.OriginalResourceYield
                : BaseResourceYield;
            attributes.BonusMovementSpeed._Value =
                bonusData.OriginalMoveSpeed != 0f ? bonusData.OriginalMoveSpeed : BaseMoveSpeedBonus;
            attributes.BonusShapeshiftMovementSpeed._Value = bonusData.OriginalShapeshiftMoveSpeed != 0f
                ? bonusData.OriginalShapeshiftMoveSpeed
                : BaseShapeshiftMoveSpeedBonus;

            BonusTrackerDatabase.Delete(entity.GetUser().PlatformId);
        }
        else
        {
            // Apply or update bonus if needed
            float expected = bonusData.OriginalResourceYield + resourceYieldBonus;
            if (!Mathf.Approximately(attributes.ResourceYieldModifier._Value, expected) ||
                !Mathf.Approximately(bonusData.LastResourceYieldBonus, resourceYieldBonus))
            {
                attributes.ResourceYieldModifier._Value = bonusData.OriginalResourceYield + resourceYieldBonus;
                bonusData.LastResourceYieldBonus = resourceYieldBonus;
            }

            expected = bonusData.OriginalMoveSpeed + moveSpeedBonus;
            if (!Mathf.Approximately(attributes.BonusMovementSpeed._Value, expected) ||
                !Mathf.Approximately(bonusData.LastMoveSpeedBonus, moveSpeedBonus))
            {
                attributes.BonusMovementSpeed._Value = bonusData.OriginalMoveSpeed + moveSpeedBonus;
                bonusData.LastMoveSpeedBonus = moveSpeedBonus;
            }

            expected = bonusData.OriginalShapeshiftMoveSpeed + shapeshiftMoveSpeedBonus;
            if (!Mathf.Approximately(attributes.BonusShapeshiftMovementSpeed._Value, expected) ||
                !Mathf.Approximately(bonusData.LastShapeshiftMoveSpeedBonus, shapeshiftMoveSpeedBonus))
            {
                attributes.BonusShapeshiftMovementSpeed._Value =
                    bonusData.OriginalShapeshiftMoveSpeed + shapeshiftMoveSpeedBonus;
                bonusData.LastShapeshiftMoveSpeedBonus = shapeshiftMoveSpeedBonus;
            }

            BonusTrackerDatabase.Save(entity.GetUser().PlatformId, bonusData);
        }

        Core.EntityManager.SetComponentData(entity, attributes);
    }

    public static void HandleInventoryBuffsOld(Entity entity)
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
                if (slot.MaxAmountOverride != 0)
                {
                    slot.MaxAmountOverride = 0;
                }

                inventoryBuffer[i] = slot;
            }
        }
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
        if (bagSlot.IsBroken) return bagMultiplier;

        if (bagSlot.SlotEntity.GetSyncedEntityOrNull() == Entity.Null) return bagMultiplier;

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