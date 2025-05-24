using System.Collections.Generic;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using VAMP;

namespace VComforts.Patches;

[HarmonyPatch(typeof(BloodMixerSystem_Update), "OnUpdate")]
public class BloodMixerSystem_UpdatePatch
{
    private static PrefabGUID glassBottlePrefabGuid = new(-437611596);

    private static Dictionary<Entity, (BloodMixerState prevState, HashSet<Entity> prevPotionEntities)> mixerState = new();
    
    static void Postfix(BloodMixerSystem_Update __instance)
    {
        if (Settings.ENABLE_BLOODMIXER_EXTRA_BOTTLE == null || !Settings.ENABLE_BLOODMIXER_EXTRA_BOTTLE.Value)
        {
            return;
        }
        var entityManager = Core.EntityManager;
        foreach (var mixer in __instance._Query.ToEntityArray(Allocator.Temp))
        {
            var shared = entityManager.GetComponentData<BloodMixer_Shared>(mixer);
            var state = shared.State;

            var potionPrefab = new PrefabGUID(2063723255);
            var potionInfos = ItemUtil.GetInventoryItemInfos(mixer, potionPrefab);
            var potionEntities = new HashSet<Entity>();
            foreach (var info in potionInfos)
                potionEntities.Add(info.ItemEntity);

            if (mixerState.TryGetValue(mixer, out var prev))
            {
                var (prevState, prevEntities) = prev;
                if (prevState == BloodMixerState.Mixing &&
                    state == BloodMixerState.NotReadyToMix)
                {
                    bool newPotion = false;
                    foreach (var entity in potionEntities)
                    {
                        if (!prevEntities.Contains(entity))
                        {
                            newPotion = true;
                            break;
                        }
                    }
                    if (newPotion)
                    {
#if DEBUG
                        Plugin.LogInstance.LogInfo("Adding glass bottle to inventory");
#endif
                        ItemUtil.AddItemToInventory(mixer, glassBottlePrefabGuid, 1);
                    }
                }
            }

            mixerState[mixer] = (state, potionEntities);
        }
    }
}