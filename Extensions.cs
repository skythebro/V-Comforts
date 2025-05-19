using System;
using System.Collections;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VAMP;
using VrisingQoL.Services;
using VrisingQoL.Systems;

namespace VrisingQoL;
internal static class Extensions
{
    static EntityManager EntityManager => Core.EntityManager;
    
    public static CastleHeartService CastleHeartService { get; } = new();
    
    public static RespawnPointSpawnerSystem RespawnPointSpawnerSystem { get; } = new();

    public delegate void WithRefHandler<T>(ref T item);
    public static void With<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        T item = entity.Read<T>();
        action(ref item);

        EntityManager.SetComponentData(entity, item);
    }
    public static void AddWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        if (!entity.Has<T>())
        {
            entity.Add<T>();
        }

        entity.With(action);
    }
    public static void HasWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        if (entity.Has<T>())
        {
            entity.With(action);
        }
    }
    public static void Write<T>(this Entity entity, T componentData) where T : struct
    {
        EntityManager.SetComponentData(entity, componentData);
    }
    public static T Read<T>(this Entity entity) where T : struct
    {
        return EntityManager.GetComponentData<T>(entity);
    }
    public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct
    {
        return EntityManager.GetBuffer<T>(entity);
    }
    public static DynamicBuffer<T> AddBuffer<T>(this Entity entity) where T : struct
    {
        return EntityManager.AddBuffer<T>(entity);
    }
    public static bool TryGetComponent<T>(this Entity entity, out T componentData) where T : struct
    {
        componentData = default;

        if (entity.Has<T>())
        {
            componentData = entity.Read<T>();
            return true;
        }

        return false;
    }
    public static bool TryRemove<T>(this Entity entity) where T : struct
    {
        if (entity.Has<T>())
        {
            entity.Remove<T>();

            return true;
        }

        return false;
    }
    public static bool Has<T>(this Entity entity) where T : struct
    {
        return EntityManager.HasComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static bool TryGetBuffer<T>(this Entity entity, out DynamicBuffer<T> buffer) where T : struct
    {
        buffer = default;

        if (entity.Has<T>())
        {
            buffer = entity.ReadBuffer<T>();
            return true;
        }

        return false;
    }
    public static bool TryRemoveComponent<T>(this Entity entity) where T : struct
    {
        if (entity.Has<T>())
        {
            entity.Remove<T>();

            return true;
        }

        return false;
    }
#if DEBUG
    public static void LogComponentTypes(this Entity entity)
    {
        NativeArray<ComponentType>.Enumerator enumerator = EntityManager.GetComponentTypes(entity).GetEnumerator();

        Plugin.LogInstance.LogInfo("===");

        while (enumerator.MoveNext())
        {
            ComponentType current = enumerator.Current;
            Plugin.LogInstance.LogInfo($"{current}");
        }

        Plugin.LogInstance.LogInfo("===");

        enumerator.Dispose();
    }
#endif
    public static void Add<T>(this Entity entity)
    {
        EntityManager.AddComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static void Remove<T>(this Entity entity)
    {
        EntityManager.RemoveComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static bool TryGetFollowedPlayer(this Entity entity, out Entity player)
    {
        player = Entity.Null;

        if (entity.Has<Follower>())
        {
            Follower follower = entity.Read<Follower>();
            Entity followed = follower.Followed._Value;

            if (followed.IsPlayer())
            {
                player = followed;

                return true;
            }
        }

        return false;
    }
    public static bool TryGetPlayer(this Entity entity, out Entity player)
    {
        player = Entity.Null;

        if (entity.Has<PlayerCharacter>())
        {
            player = entity;

            return true;
        }

        return false;
    }
    public static bool IsPlayer(this Entity entity)
    {
        if (entity.Has<VampireTag>())
        {
            return true;
        }

        return false;
    }
    public static bool IsDifferentPlayer(this Entity entity, Entity target)
    {
        if (entity.IsPlayer() && target.IsPlayer() && !entity.Equals(target))
        {
            return true;
        }

        return false;
    }
    public static bool IsFollowingPlayer(this Entity entity)
    {
        if (entity.Has<Follower>())
        {
            Follower follower = entity.Read<Follower>();
            if (follower.Followed._Value.IsPlayer())
            {
                return true;
            }
        }

        return false;
    }
    public static Entity GetBuffTarget(this Entity entity)
    {
        return CreateGameplayEventServerUtility.GetBuffTarget(EntityManager, entity);
    }
    public static Entity GetSpellTarget(this Entity entity)
    {
        return CreateGameplayEventServerUtility.GetSpellTarget(EntityManager, entity);
    }
    
    public static unsafe T GetComponentDataAOT<T>(this EntityManager entityManager, Entity entity) where T : unmanaged
    {
        var type = TypeManager.GetTypeIndex(Il2CppType.Of<T>());
        var result = (T*)entityManager.GetComponentDataRawRW(entity, type);
        return *result;
    }
    public static bool TryGetTeamEntity(this Entity entity, out Entity teamEntity)
    {
        teamEntity = Entity.Null;

        if (entity.TryGetComponent(out TeamReference teamReference))
        {
            Entity teamReferenceEntity = teamReference.Value._Value;

            if (teamReferenceEntity.Exists())
            {
                teamEntity = teamReferenceEntity;

                return true;
            }
        }

        return false;
    }
    public static bool Exists(this Entity entity)
    {
        return entity.HasValue() && EntityManager.Exists(entity);
    }
    public static bool HasValue(this Entity entity)
    {
        return entity != Entity.Null;
    }
    public static bool IsDisabled(this Entity entity)
    {
        return entity.Has<Disabled>();
    }
    public static bool IsVBlood(this Entity entity)
    {
        return entity.Has<VBloodUnit>();
    }
    public static ulong GetSteamId(this Entity entity)
    {
        if (entity.TryGetComponent(out PlayerCharacter playerCharacter))
        {
            return playerCharacter.UserEntity.Read<User>().PlatformId;
        }
        else if (entity.TryGetComponent(out User user))
        {
            return user.PlatformId;
        }

        return 0;
    }
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
    public static PrefabGUID GetPrefabGuid(this Entity entity)
    {
        if (entity.TryGetComponent(out PrefabGUID prefabGuid)) return prefabGuid;
        return PrefabGUID.Empty;
    }
    
    public static String GetNamePrefab(this PrefabGUID prefabGuid)
    {
        return Core.ServerGameManager.PrefabLookupMap.GetName(prefabGuid);
    }
    public static String GetPrefabGuidName(this Entity entity)
    {
        if (entity.TryGetComponent(out PrefabGUID prefabGuid))
        {
           return Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>()._PrefabLookupMap.GetName(prefabGuid);
        }

        return "Failed to get name";
    }
    public static bool IsEmpty(this Entity entity)
    {
        return entity.Equals(Entity.Null);
    }
    public static Entity GetUserEntity(this Entity entity)
    {
        if (entity.TryGetComponent(out PlayerCharacter playerCharacter)) return playerCharacter.UserEntity;
        else if (entity.TryGetComponent(out UserOwner userOwner)) return userOwner.Owner.GetEntityOnServer();
        else return Entity.Null;
    }
    public static User GetUser(this Entity entity)
    {
        if (entity.TryGetComponent(out PlayerCharacter playerCharacter) && playerCharacter.UserEntity.TryGetComponent(out User user)) return user;
        else if (entity.TryGetComponent(out user)) return user;

        return User.Empty;
    }
    public static int GetTerritoryIndex(this Entity entity)
    {
        if (entity.TryGetComponent(out CastleTerritory castleTerritory)) return castleTerritory.CastleTerritoryIndex;

        return -1;
    }
    public static void Destroy(this Entity entity)
    {
        if (entity.Exists()) DestroyUtility.Destroy(EntityManager, entity);
    }
    public static void DestroyBuff(this Entity entity)
    {
        if (entity.Exists()) DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.TryRemoveBuff);
    }
    public static NetworkId GetNetworkId(this Entity entity)
    {
        if (entity.TryGetComponent(out NetworkId networkId))
        {
            return networkId;
        }

        return NetworkId.Empty;
    }
    public static Coroutine Start(this IEnumerator routine)
    {
        return Core.StartCoroutine(routine);
    }
    static EntityQuery BuildEntityQuery(
    this EntityManager entityManager,
    ComponentType[] all)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);

        foreach (var componentType in all)
            builder.AddAll(componentType);

        return entityManager.CreateEntityQuery(ref builder);
    }
    static EntityQuery BuildEntityQuery(
    this EntityManager entityManager,
    ComponentType[] all,
    EntityQueryOptions options)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);

        foreach (var componentType in all)
            builder.AddAll(componentType);

        builder.WithOptions(options);

        return entityManager.CreateEntityQuery(ref builder);
    }
    static EntityQuery BuildEntityQuery(
    this EntityManager entityManager,
    ComponentType[] all,
    ComponentType[] none,
    EntityQueryOptions options)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);

        foreach (var componentType in all)
            builder.AddAll(componentType);

        foreach (var componentType in none)
            builder.AddNone(componentType);

        builder.WithOptions(options);

        return entityManager.CreateEntityQuery(ref builder);
    }
    static int[] GenerateDefaultIndices(int length)
    {
        var indices = new int[length];
        for (int i = 0; i < length; i++)
            indices[i] = i;
        return indices;
    }
    public static float3 GetPosition(this Entity entity)
    {
        if (entity.TryGetComponent(out Translation translation))
        {
            return translation.Value;
        }

        return float3.zero;
    }
    public static int2 GetCoordinate(this Entity entity)
    {
        if (entity.TryGetComponent(out TilePosition tilePosition))
        {
            return tilePosition.Tile;
        }

        return int2.zero;
    }
}