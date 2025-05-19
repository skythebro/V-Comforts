using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using ProjectM;
using ProjectM.CastleBuilding.Rebuilding;
using ProjectM.Tiles;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using VAMP;
using VampireCommandFramework;
using VrisingQoL.Systems;

namespace VrisingQoL.VCFCompat
{
    public static partial class Commands
    {
        private static ManualLogSource _log => VAMP.Plugin.LogInstance;

        static Commands()
        {
            Enabled = IL2CPPChainloader.Instance.Plugins.TryGetValue("gg.deca.VampireCommandFramework", out var info);
            if (Enabled) _log.LogInfo($"VCF Version: {info.Metadata.Version}");
        }

        public static bool Enabled { get; private set; }

        public static void Register() => CommandRegistry.RegisterAll();

        public static void Unregister() => CommandRegistry.UnregisterAssembly();

        [CommandGroup("respawnpoint", "rsp")]
        internal class RespawnCommands
        {
            [Command("set", "st", description: "set a nearby respawnpoint as yours", adminOnly: false)]
            public static void SetRespawnPointCommand(ChatCommandContext ctx)
            {
                try
                {
                    if (!Settings.ENABLE_RESPAWN_POINTS.Value)
                    {
                        ctx.Reply("Respawn points are disabled in the config");
                        return;
                    }

                    var player = ctx.Event.SenderCharacterEntity;
                    if (Core.EntityManager.TryGetComponentData<LocalToWorld>(player, out var localToWorld))
                    {
                        if (Core.EntityManager.TryGetComponentData<Rotation>(player, out var rotation))
                        {
                            bool hasSet =
                                Extensions.RespawnPointSpawnerSystem.SetRespawnPoint(localToWorld.Position, ctx);
                            ctx.Reply(hasSet
                                ? $"Set respawnpoint near {ctx.Event.User.CharacterName}"
                                : $"No respawnpoint found near {ctx.Event.User.CharacterName}'s location or you are not the owner of this respawnpoint");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ctx.Error($"An error occured while trying set a respawnpoint: {ex.Message}");
                    Plugin.LogInstance.LogError(ex);
                }
            }

            [Command("spawn", "sp", description: "spawn a respawnpoint at current position and facting direction",
                adminOnly: false)]
            public static void SpawnRespawnPointCommand(ChatCommandContext ctx)
            {
                try
                {
                    if (!Settings.ENABLE_RESPAWN_POINTS.Value)
                    {
                        ctx.Reply("Respawn points are disabled in the config");
                        return;
                    }

                    var user = ctx.Event.User;
                    var player = ctx.Event.SenderCharacterEntity;
                    if (Settings.ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING.Value || user.IsAdmin)
                    {
                        if (Core.EntityManager.TryGetComponentData<LocalToWorld>(player, out var localToWorld))
                        {
                            if (Core.EntityManager.TryGetComponentData<Rotation>(player, out var rotation))
                            {
                                Extensions.RespawnPointSpawnerSystem.SpawnRespawnPoint(localToWorld.Position,
                                    rotation.Value, player, ctx.Event.SenderCharacterEntity, false);
                                ctx.Reply($"Spawned respawnpoint at {ctx.Event.User.CharacterName}");
                            }
                        }
                    }
                    else
                    {
                        ctx.Error(
                            "You are not an admin, spawning of respawnpoints is only allowed by admins unless this setting is turned off in the config");
                    }
                }
                catch (System.Exception ex)
                {
                    ctx.Error($"An error occured while trying to spawn a respawnpoint: {ex.Message}");
                    Plugin.LogInstance.LogError(ex);
                }
            }

            [Command("remove", "rem", description: "remove the nearest respawnpoint", adminOnly: true)]
            public static void DelteRespawnPointCommand(ChatCommandContext ctx)
            {
                try
                {
                    if (!Settings.ENABLE_RESPAWN_POINTS.Value)
                    {
                        ctx.Reply("Respawn points are disabled in the config");
                        return;
                    }

                    if (Settings.ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING.Value || ctx.Event.User.IsAdmin)
                    {
                        var player = ctx.Event.SenderCharacterEntity;
                        if (Core.EntityManager.TryGetComponentData<LocalToWorld>(player, out var localToWorld))
                        {
                            bool succeeded =
                                Extensions.RespawnPointSpawnerSystem.RemoveRespawnPoint(localToWorld.Position);
                            ctx.Reply(succeeded
                                ? $"Deleted respawnpoint near {ctx.Event.User.CharacterName}'s location"
                                : $"No respawnpoint found near {ctx.Event.User.CharacterName}'s location");
                        }
                    }
                    else
                    {
                        ctx.Error(
                            "You are not an admin, removing of respawnpoints is only allowed by admins unless this setting is turned off in the config");
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.LogInstance.LogError(ex);
                }
            }
        }
    }
}