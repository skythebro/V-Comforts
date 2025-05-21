using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VAMP;
using VAMP.Data;

namespace VrisingQoL;

public class BuffUtil
{
    public static EntityManager entityManager = Core.EntityManager;
    public static PrefabGUID InCombat = Prefabs.Buff_InCombat;
    public static PrefabGUID InCombat_PvP = Prefabs.Buff_InCombat_PvPVampire;

    
    public static bool IsPlayerInCombat(Entity player)
    {
        return BuffUtility.HasBuff(entityManager, player, InCombat) || BuffUtility.HasBuff(entityManager, player, InCombat_PvP);
    }
    
    public static bool AddBuff(Entity Character, PrefabGUID buffPrefab, int duration = 0, bool immortal = false)
    {
	    return VAMP.Utilities.BuffUtil.BuffEntity(
            Character,
            buffPrefab,
            out Entity _,
            duration,
            immortal);
    }
    
    public static void RemoveBuff(Entity Character, PrefabGUID buffPrefab)
    {
        VAMP.Utilities.BuffUtil.RemoveBuff(Character,buffPrefab);
    }
    
}