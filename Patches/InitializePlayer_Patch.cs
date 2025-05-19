using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Stunlock.Network;
using VAMP;
using Extensions = VrisingQoL.Extensions;

namespace SpiderKiller.Patches;

[HarmonyPatch]
public static class InitializePlayer_Patch
{
    public static List<int> playerEntityIndices = new();
    
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    [HarmonyPostfix]
    public static void OnUserConnected_Patch(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        try
        {
            var userIndex = Core.Server.GetExistingSystemManaged<ServerBootstrapSystem>()
                ._NetEndPointToApprovedUserIndex[netConnectionId];
            var serverClient = Core.Server.GetExistingSystemManaged<ServerBootstrapSystem>()
                ._ApprovedUsersLookup[userIndex];
            var userEntity = serverClient.UserEntity;

            var user = Extensions.GetUser(userEntity);
            var player = user.LocalCharacter.GetEntityOnServer();
            
            // Add the player to the list of player entities
            playerEntityIndices.Add(player.Index);
            
            // add respawnpoints for the player if global respawnpoints are enabled
        }
        catch (System.Exception ex)
        {
            Plugin.LogInstance.LogError(ex);
        }
    }
}