using System;
using System.Collections.Generic;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using VAMP;
using VampireCommandFramework;
using VrisingQoL.Database;
using VrisingQoL.Patches;

namespace VrisingQoL.VCFCompat
{
    public static partial class Commands
    {
        private static ManualLogSource LOG => Plugin.LogInstance;

        static Commands()
        {
            Enabled = IL2CPPChainloader.Instance.Plugins.TryGetValue("gg.deca.VampireCommandFramework", out var info);
            if (Enabled) LOG.LogInfo($"VCF Version: {info.Metadata.Version}");
        }

        public static bool Enabled { get; private set; }

        public static void Register() => CommandRegistry.RegisterAll();

        public static void Unregister() => CommandRegistry.UnregisterAssembly();

        [CommandGroup("respawnpoint", "rsp")]
        internal class RespawnCommands
        {
            [Command("addGlobal", "ag",
                description: "add your current postion and rotation to the global respawnPoint list",
                adminOnly: true)]
            public static void AddGlobalRespawnLocation(ChatCommandContext ctx)
            {
                if (Settings.ENABLE_PREDEFINED_RESPAWN_POINTS.Value)
                {
                    var character = ctx.Event.SenderCharacterEntity;
                    if (Core.EntityManager.TryGetComponentData<LocalToWorld>(character, out var localToWorld) &&
                        Core.EntityManager.TryGetComponentData<Rotation>(character, out var rotation))
                    {
                        List<RespawnPointData> data = InitializePlayer_Patch.ReadRespawnPointsFromFile();
                        data.Add(new RespawnPointData()
                        {
                            Position = new SerializableFloat3(localToWorld.Position),
                            Rotation = new SerializableQuaternion(rotation.Value)
                        });
                        InitializePlayer_Patch.WriteRespawnPointsToFile(data);
                        ctx.Reply("Added your current position and rotation to the global respawnPoint list");
                    }
                    else
                    {
                        ctx.Error("Could not get your position or rotation");
                    }
                }
                else
                {
                    ctx.Error("Predefined respawn points are disabled in the config");
                }
            }


            [Command("spawn", "sp", description: "spawn a respawnPoint at current position and facing direction",
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

                    if (!ctx.IsAdmin && Settings.ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING.Value)
                    {
                        if (Settings.RESPAWN_POINT_COST_AMOUNT.Value > 0)
                        {
                            if (!ItemUtil.TryConsumeRequiredItems(ctx,
                                    out var message)) // Thx Trodi nice way of getting messages!
                            {
                                ctx.Error(message);
                                return;
                            }
                        }
                    }

                    var player = ctx.Event.SenderCharacterEntity;
                    if (Settings.ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING.Value || ctx.IsAdmin)
                    {
                        if (Core.EntityManager.TryGetComponentData<LocalToWorld>(player, out var localToWorld))
                        {
                            if (Core.EntityManager.TryGetComponentData<Rotation>(player, out var rotation))
                            {
                                bool succeeded = Extensions.RespawnPointSpawnerSystem.SpawnRespawnPoint(
                                    localToWorld.Position,
                                    rotation.Value, player, ctx.Event.SenderUserEntity);
                                if (succeeded)
                                {
                                    ctx.Reply($"Spawned respawnpoint at {ctx.Event.User.CharacterName}");
                                    RespawnPointDatabase.AddRespawnPoint(ctx.Event.User.PlatformId,
                                        localToWorld.Position);
                                }
                                else
                                {
                                    ctx.Error("RespawnPoint could not be spawned");
                                }
                            }
                        }
                    }
                    else
                    {
                        ctx.Error(
                            "You are not an admin, spawning of respawnpoints is only allowed by admins unless this setting is turned off in the config");
                    }
                }
                catch (Exception ex)
                {
                    ctx.Error($"An error occured while trying to spawn a respawnpoint: {ex.Message}");
                    Plugin.LogInstance.LogError(ex);
                }
            }


            [Command("set", "st",
                description: "set a nearby owned respawnPoint as active, or if admin of the specified userName",
                adminOnly: false)]
            public static void SetRespawnPointCommand(ChatCommandContext ctx, String userName = "")
            {
                try
                {
                    if (!Settings.ENABLE_RESPAWN_POINTS.Value)
                    {
                        ctx.Reply("Respawn points are disabled in the config");
                        return;
                    }

                    var entityManager = Core.EntityManager;
                    var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
                    Entity userEntity = Entity.Null;
                    if (userName == "")
                    {
                        userEntity = ctx.Event.SenderUserEntity;
                        userName = ctx.Event.User.CharacterName.Value;
                    }
                    else
                    {
                        if (!ctx.IsAdmin)
                        {
                            ctx.Error($"You are not an admin, {userName}, you can only set your own respawnPoints!");
                        }

                        foreach (var entity in query.ToEntityArray(Allocator.Temp))
                        {
                            var user = entityManager.GetComponentData<User>(entity);
                            if (user.CharacterName.Value.Equals(userName, StringComparison.OrdinalIgnoreCase))
                            {
                                userEntity = entity;
                                break;
                            }
                        }
                    }

                    if (userEntity == Entity.Null)
                    {
                        ctx.Reply("No user found with name: " + userName);
                        return;
                    }

                    var player = userEntity.GetUser().LocalCharacter._Entity;
                    if (Core.EntityManager.TryGetComponentData<LocalToWorld>(player, out var localToWorld))
                    {
                        bool hasSet = Extensions.RespawnPointSpawnerSystem.SetRespawnPoint(localToWorld.Position, userEntity, ctx.IsAdmin);
                        if (hasSet)
                        {
                            ctx.Reply("Set respawnpoint near " + userEntity.GetUser().CharacterName);
                        }
                        else
                        {
                            ctx.Error($"No respawnpoint found near {userEntity.GetUser().CharacterName}'s location");
                        }
                    }
                    else
                    {
                        ctx.Error($"Could not get your position");
                    }
                }
                catch (Exception ex)
                {
                    ctx.Error($"An error occured while trying set a respawnpoint: {ex.Message}");
                    Plugin.LogInstance.LogError(ex);
                }
            }

            [Command("remove", "rem",
                description: "remove nearest spawnpoint owned by you, or if admin of the specified userName",
                adminOnly: false)]
            public static void DeleteRespawnPointCommand(ChatCommandContext ctx, String userName = "")
            {
                try
                {
                    if (!Settings.ENABLE_RESPAWN_POINTS.Value)
                    {
                        ctx.Reply("Respawn points are disabled in the config");
                        return;
                    }

                    var entityManager = Core.EntityManager;
                    var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
                    Entity userEntity = Entity.Null;
                    if (userName == "")
                    {
                        userEntity = ctx.Event.SenderUserEntity;
                        userName = ctx.Event.User.CharacterName.Value;
                    }
                    else
                    {
                        if (!ctx.IsAdmin)
                        {
                            ctx.Error($"You are not an admin, {userName}, you can only remove your own respawnpoints!");
                        }

                        foreach (var entity in query.ToEntityArray(Allocator.Temp))
                        {
                            var user = entityManager.GetComponentData<User>(entity);
                            if (user.CharacterName.Value.Equals(userName, StringComparison.OrdinalIgnoreCase))
                            {
                                userEntity = entity;
                                break;
                            }
                        }
                    }

                    if (userEntity == Entity.Null)
                    {
                        ctx.Reply("No user found with name: " + userName);
                        return;
                    }

                    if (Settings.ENABLE_NONADMIN_RESPAWNPOINT_SPAWNING.Value || ctx.IsAdmin)
                    {
                        var player = ctx.Event.SenderCharacterEntity;
                        if (Core.EntityManager.TryGetComponentData<LocalToWorld>(player, out var localToWorld))
                        {
                            bool succeeded =
                                Extensions.RespawnPointSpawnerSystem.RemoveRespawnPoint(localToWorld.Position,
                                    userEntity, ctx.IsAdmin);
                            if (succeeded)
                            {
                                ctx.Reply($"Deleted respawnpoint near {userEntity.GetUser().CharacterName}'s location");
                                RespawnPointDatabase.RemoveRespawnPoint(userEntity.GetUser().PlatformId,
                                    localToWorld.Position);
                            }
                            else
                            {
                                ctx.Error(
                                    $"No respawnpoint found near {userEntity.GetUser().CharacterName}'s location");
                            }
                        }
                    }
                    else
                    {
                        ctx.Error(
                            "You are not an admin, removing of respawnpoints is only allowed by admins unless this setting is turned off in the config");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.LogInstance.LogError(ex);
                }
            }
        }
    }
}