using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using VAMP;
using VComforts.Patches.Connection;
using VComforts.Systems;

namespace VComforts.Patches.Equiping;

[HarmonyPatch(typeof(EquipItemFromInventorySystem), "OnUpdate")]
public static class EquipItemFromInventorySystemPatch
{
    public static bool Prefix(EquipItemFromInventorySystem __instance)
    {
        if (__instance._Query.IsEmpty)
        {
            return true;
        }
        if (!Settings.ENABLE_INVENTORY_BONUS.Value) return true;
        
        foreach (var entity in __instance._Query.ToEntityArray(Allocator.Temp))
        {
            var fromCharacter = entity.Read<FromCharacter>();
            var equipItemEvent = entity.Read<EquipItemFromInventoryEvent>();

            var networkIdSystem = Core.SystemService.NetworkIdSystem;
            var fromInventoryEntity = equipItemEvent.FromInventory
                .GetNetworkedEntity(ref networkIdSystem._NetworkIdLookupMap).GetEntityOnServer();

            if (fromInventoryEntity == Entity.Null) continue;
            
            var hasBuffer =
                __instance.EntityManager.TryGetBuffer<InventoryBuffer>(fromInventoryEntity,
                    out var inventoryBuffer);
            if (!hasBuffer)
            {
                InventoryUtilitiesServer.TryGetInventoryBuffer(__instance.EntityManager, fromInventoryEntity,
                    out inventoryBuffer);
            }

            if (equipItemEvent.SlotIndex < 0 || equipItemEvent.SlotIndex >= inventoryBuffer.Length) continue;
                
            var slot = inventoryBuffer[equipItemEvent.SlotIndex];

            if (slot.ItemEntity.GetSyncedEntityOrNull() == Entity.Null) continue;
                
            var itemType = slot.ItemType;

            if (!itemType.GetNamePrefab().StartsWith("Item_NewBag_")) continue;
                        
            var multiplier = BonusSystem.GetBagMultiplierByPrefab(itemType);
            var customItemDataMap = InitializePlayer_Patch.GetUpdatedItemDataMap(
                fromCharacter.Character,
                Core.SystemService.GameDataSystem._GameDatas.ItemHashLookupMap, multiplier);
            InitializePlayer_Patch.PlayerItemDataMaps[(int)fromCharacter.User.GetUser().PlatformId] =
                customItemDataMap;
            BonusSystem.HandleInventoryBuffs(fromCharacter.Character, multiplier);
        }

        return true;
    }

    public static void Postfix(EquipItemFromInventorySystem __instance)
    {
        if (__instance._Query.IsEmpty)
        {
            return;
        }
        if (!Settings.ENABLE_LEVEL_BONUS.Value) return;
        
        foreach (var entity in __instance._Query.ToEntityArray(Allocator.Temp))
        {
            var fromCharacter = entity.Read<FromCharacter>();
            var equipItemEvent = entity.Read<EquipItemFromInventoryEvent>();

            var networkIdSystem = Core.SystemService.NetworkIdSystem;
            var fromInventoryEntity = equipItemEvent.FromInventory
                .GetNetworkedEntity(ref networkIdSystem._NetworkIdLookupMap).GetEntityOnServer();

            if (fromInventoryEntity == Entity.Null) continue;
            
            var hasBuffer =
                __instance.EntityManager.TryGetBuffer<InventoryBuffer>(fromInventoryEntity,
                    out var inventoryBuffer);
            if (!hasBuffer)
            {
                InventoryUtilitiesServer.TryGetInventoryBuffer(__instance.EntityManager, fromInventoryEntity,
                    out inventoryBuffer);
            }

            var slot = inventoryBuffer[equipItemEvent.SlotIndex];

            if (slot.ItemEntity.GetSyncedEntityOrNull() == Entity.Null) continue;
                
            var itemType = slot.ItemType;

            // doesnt check for cosmetic items but eh...
            if (itemType.GetNamePrefab().StartsWith("Item_NewBag_") || itemType.GetNamePrefab().StartsWith("Item_Cloak_"))
            {
            }
            else
            {
                BonusSystem.HandleLevelBuffs(fromCharacter.Character);
            }
        }
    }
}