using System;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using VAMP;
using VComforts.Patches.Connection;
using VComforts.Systems;
using VComforts.Utils;

namespace VComforts.Patches.Equiping;

[HarmonyPatch(typeof(UnEquipItemSystem), "OnUpdate")]
public static class UnEquipItemSystemPatch
{
    public static void Postfix(UnEquipItemSystem __instance)
    {
        if (!Settings.ENABLE_INVENTORY_BONUS.Value && !Settings.ENABLE_LEVEL_BONUS.Value)
            return;
        if (__instance._Query.IsEmpty)
            return;

        foreach (var entity in __instance._Query.ToEntityArray(Allocator.Temp))
        {
            var fromCharacter = entity.Read<FromCharacter>();
            var unequipItemEvent = entity.Read<UnequipItemEvent>();
            Plugin.LogInstance.LogWarning("unequipItemEvent item type: " + unequipItemEvent.EquipmentType);
            var networkIdSystem = Core.SystemService.NetworkIdSystem;
            var inventoryEntity = unequipItemEvent.ToInventory
                .GetNetworkedEntity(ref networkIdSystem._NetworkIdLookupMap).GetEntityOnServer();
            
            if (inventoryEntity == Entity.Null)
            {
#if DEBUG
                Plugin.LogInstance.LogWarning("Inventory is null, getting inventory from character");
#endif
                inventoryEntity = fromCharacter.Character;
            }

            var hasBuffer = inventoryEntity.TryGetBuffer<InventoryBuffer>(out var inventoryBuffer);
            if (!hasBuffer)
            {
                InventoryUtilitiesServer.TryGetInventoryBuffer(__instance.EntityManager, inventoryEntity, out inventoryBuffer);
            }
            
            switch (unequipItemEvent.EquipmentType)
            {
                case EquipmentType.Bag:
                {
                    if (!Settings.ENABLE_INVENTORY_BONUS.Value) break;
                    var customItemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(fromCharacter.Character,
                        Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap);

                    for (int i = 0; i < inventoryBuffer.Length; i++)
                    {
                        var invSlot = inventoryBuffer[i];
                        if (invSlot.ItemType == PrefabGUID.Empty)
                            continue;

                        if (!customItemDataMap.TryGetValue(invSlot.ItemType, out var itemData))
                            continue;

                        int maxAmount = itemData.MaxAmount;
                    
                        if (invSlot.Amount <= maxAmount) continue;
                    
                        int excess = invSlot.Amount - maxAmount;
                        for (int j = 0; j < inventoryBuffer.Length && excess > 0; j++)
                        {
                            if (i == j) continue;
                            var targetSlot = inventoryBuffer[j];
                            if (targetSlot.ItemType != PrefabGUID.Empty) continue;
                            
                            // Move up to maxAmount or remaining excess
                            int moveAmount = Math.Min(excess, maxAmount);
                            var moveResponse = InventoryUtilitiesServer.TryMoveItem(
                                __instance.EntityManager,
                                customItemDataMap,
                                inventoryEntity,
                                i,
                                inventoryEntity,
                                j,
                                true,
                                default,
                                moveAmount
                            );
                            if (moveResponse.Result == MoveItemResult.Success_Complete || moveResponse.Result == MoveItemResult.Success_Partial)
                            {
                                excess -= moveAmount;
                            }
                        }

                        if (excess <= 0) continue;
                    
                        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
                        var invItemType = invSlot.ItemType;

                        InventoryUtilitiesServer.TryDropItem(
                            __instance.EntityManager,
                            commandBuffer,
                            customItemDataMap,
                            inventoryEntity,
                            invItemType,
                            excess
                        );
                        commandBuffer.Playback(__instance.EntityManager);
                        commandBuffer.Dispose();
                    }

                    break;
                }
                case EquipmentType.Gloves or EquipmentType.Chest or EquipmentType.Legs or EquipmentType.Footgear or EquipmentType.Weapon or EquipmentType.MagicSource:
                    if (Settings.ENABLE_LEVEL_BONUS.Value)
                    {
#if DEBUG
                        Plugin.LogInstance.LogWarning(unequipItemEvent.EquipmentType + " were unequipped, handling level buffs.");
#endif
                        DelayedUtil.RunLevelBuffsDelayed(fromCharacter.Character);
                    }
                    break;
            }
        }
    }
}