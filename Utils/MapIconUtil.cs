using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VAMP;
using VAMP.Data;

namespace VComforts.Utils;

public class MapIconUtil
{
    public static void SpawnMapIcon(PrefabGUID unitPrefab, float3 position, Entity targetEntity)
    {
#if DEBUG
        Plugin.LogInstance.LogWarning("Spawning with prefab: " + unitPrefab +" at position: " + position);
#endif        
        VAMP.Services.SpawnService.SpawnUnitWithCallback(unitPrefab, position, -1, entity =>
        {
            if (!entity.Has<MapIconData>())
                entity.Add<MapIconData>();
            
            if (entity.Has<LifeTime>())
                entity.Remove<LifeTime>();
            
            var attach = new Attach(targetEntity);
            entity.Add<Attach>();
            entity.Write(attach);
            
            var mapIconData = entity.Read<MapIconData>();
            mapIconData.RequiresReveal = false;
            entity.Write(mapIconData);
            
            if (!entity.Has<OnlySyncToUsersTag>())
                entity.Add<OnlySyncToUsersTag>();

            
            if (!entity.Has<MapIconTargetEntity>())
                entity.Add<MapIconTargetEntity>();
            var mapIconTargetEntity = entity.Read<MapIconTargetEntity>();
            mapIconTargetEntity.TargetEntity = NetworkedEntity.ServerEntity(targetEntity);
            mapIconTargetEntity.TargetNetworkId = targetEntity.Read<NetworkId>();
            entity.Write(mapIconTargetEntity);
            if (!entity.Has<NameableInteractable>())
                entity.Add<NameableInteractable>();
            
            NameableInteractable nameableInteractable = entity.Read<NameableInteractable>();
            nameableInteractable.Name = new FixedString64Bytes(nameableInteractable.Name + "_icon");
            entity.Write(nameableInteractable);
            if (!entity.Has<PlayerMapIcon>())
                entity.Add<PlayerMapIcon>();
            var playerMapIcon = entity.Read<PlayerMapIcon>();
            playerMapIcon.UserName = nameableInteractable.Name;
            entity.Write(playerMapIcon);
            entity.Write(targetEntity.Read<LocalToWorld>());
            entity.Write(targetEntity.Read<Translation>());
#if DEBUG
            Plugin.LogInstance.LogWarning("Prefab name of mapIcon: " + entity.GetPrefabGuidName());
            Plugin.LogInstance.LogWarning("MapIcon LocalToWorld: " + entity.Read<LocalToWorld>().Position);
            Plugin.LogInstance.LogWarning("MapIcon Translation: " + entity.Read<Translation>().Value);
#endif
});
    }


    public static void UpdateIcons()
    {
       var mapIconDataQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(ComponentType.ReadOnly<MapIconTargetEntity>())
            .WithOptions(EntityQueryOptions.IncludeDisabled);
        var mapIconDataQuery = Core.EntityManager.CreateEntityQuery(ref mapIconDataQueryBuilder);


        var mapIconDataObjects = mapIconDataQuery.ToEntityArray(Allocator.Temp);

        foreach (var mapIconObject in mapIconDataObjects)
        {
            if (mapIconObject.Read<PrefabGUID>().Equals(Prefabs.MapIcon_POI_Discover_Merchant))
            {
                Attach attachComponent = mapIconObject.Read<Attach>();

                // CarriageHorse who has playerMapIcon attached.
                Entity parentEntity = attachComponent.Parent;
                if (!parentEntity.GetPrefabGuidName().ToLower().StartsWith("char_carriage"))
                {
                    return;
                }
                if (parentEntity.Has<NameableInteractable>())
                {
                    // Get carriagehorse's NameableInteractable for the updated name.
                    var parentNameableInteractable = parentEntity.Read<NameableInteractable>();
                    // if (mapIconObject.Has<MapIconData>())
                    // {
                    //     MapIconData mapIconData = mapIconObject.Read<MapIconData>();
                    //     mapIconData.HeaderLocalizedKey.Key.
                    // }

                    if (mapIconObject.Has<PlayerMapIcon>())
                    {
                        PlayerMapIcon iconObject = mapIconObject.Read<PlayerMapIcon>();
                        iconObject.UserName = parentNameableInteractable.Name;
                        mapIconObject.Write(iconObject);
                    }
                }
            }
        }
    }
}