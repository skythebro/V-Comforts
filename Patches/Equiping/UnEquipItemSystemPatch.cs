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

namespace VComforts.Patches.Equiping;

[HarmonyPatch(typeof(UnEquipItemSystem), "OnUpdate")]
public static class UnEquipItemSystemPatch
{
    public static void Postfix(UnEquipItemSystem __instance)
    {
        if (__instance._Query.IsEmpty)
            return;

        foreach (var entity in __instance._Query.ToEntityArray(Allocator.Temp))
        {
            var fromCharacter = entity.Read<FromCharacter>();
            var unequipItemEvent = entity.Read<UnequipItemEvent>();

            var networkIdSystem = Core.SystemService.NetworkIdSystem;
            var playerEntity = unequipItemEvent.ToInventory
                .GetNetworkedEntity(ref networkIdSystem._NetworkIdLookupMap).GetEntityOnServer();

            if (playerEntity == Entity.Null)
                continue;
#if DEBUG
            Plugin.LogInstance.LogWarning("Is player components?");
            playerEntity.LogComponentTypes();
#endif
            
            var hasBuffer = playerEntity.TryGetBuffer<InventoryBuffer>(out var inventoryBuffer);
            if (!hasBuffer)
            {
                InventoryUtilitiesServer.TryGetInventoryBuffer(__instance.EntityManager, playerEntity, out inventoryBuffer);
            }
            
            switch (unequipItemEvent.EquipmentType)
            {
                case EquipmentType.Bag:
                {
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
                                playerEntity,
                                i,
                                playerEntity,
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
                            playerEntity,
                            invItemType,
                            excess
                        );
                        commandBuffer.Playback(__instance.EntityManager);
                        commandBuffer.Dispose();
                    }

                    break;
                }
                case EquipmentType.Gloves or EquipmentType.Chest or EquipmentType.Legs or EquipmentType.Footgear or EquipmentType.Weapon or EquipmentType.MagicSource:
                    BonusSystem.HandleLevelBuffs(fromCharacter.Character);
                    break;
            }
        }
    }
}