using System.Collections;
using Unity.Entities;
using UnityEngine;
using VAMP;
using VComforts.Systems;

namespace VComforts.Utils;

public class DelayedUtil
{
    // otherwise the level hasnt updated yet
    public static void RunLevelBuffsDelayed(Entity character)
    {
        Core.StartCoroutine(DelayedHandleLevelBuffs(character));
    }

    private static IEnumerator DelayedHandleLevelBuffs(Entity character)
    {
        yield return new WaitForSeconds(0.1f);
        BonusSystem.HandleLevelBuffs(character);
    }
}