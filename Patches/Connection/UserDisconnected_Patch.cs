using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Network;
using VComforts.Database;

namespace VComforts.Patches.Connection;

[HarmonyPatch]
public static class UserDisconnected_Patch
{
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
    [HarmonyPrefix]
    public static void OnUserDisconnected_Patch(ServerBootstrapSystem __instance, NetConnectionId netConnectionId,
        ConnectionStatusChangeReason connectionStatusReason, string extraData)
    {
        if (!Settings.ENABLE_RESPAWN_POINTS.Value)
        {
            return;
        }
        // Check if the NetConnectionId exists in the dictionary
        if (!__instance._NetEndPointToApprovedUserIndex.ContainsKey(netConnectionId))
        {
            // If it doesn't exist, return without doing anything further
            return;
        }
        
        InitializePlayer_Patch.EnsureRespawnPointsFile();
            
        var positions = InitializePlayer_Patch.ReadRespawnPointsFromFile();

        var userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
        var serverClient = __instance._ApprovedUsersLookup[userIndex];
        var userEntity = serverClient.UserEntity;
        var user = __instance.EntityManager.GetComponentData<User>(userEntity);
        var player = user.LocalCharacter.GetEntityOnServer();

        // Find the index of the player entity in the list
        int playerIndex = InitializePlayer_Patch.playerEntityIndices.IndexOf(player.Index);

        // If the player entity is found in the list, remove it
        if (playerIndex != -1)
        {
            InitializePlayer_Patch.playerEntityIndices.RemoveAt(playerIndex);
            if (!Settings.ENABLE_PREDEFINED_RESPAWN_POINTS.Value)
            {
                return;
            }
            foreach (var position in positions)
            {
                var userLocations = RespawnPointDatabase.GetRespawnPointLocations(user.PlatformId);
                bool isClose = false;
                foreach (var loc in userLocations)
                {
                    if (Unity.Mathematics.math.distance(position.Position.ToFloat3(), loc) < 3.0f)
                    {
                        isClose = true;
                        break;
                    }
                }
                if (!isClose)
                {
                    Extensions.RespawnPointSpawnerSystem.RemoveRespawnPoint(position.Position.ToFloat3(), userEntity, true, true);
                }
            }
        }
    }
}