using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using VAMP;

namespace VrisingQoL.Services;

public class CastleHeartService
{
    struct HeartInfo
    {
        public Entity CastleHeart;
    };
    
    Dictionary<Entity, HeartInfo> fallbackHeart = [];
    
    public void GetFallbackCastleHeart(Entity charEntity, out Entity castleHeartEntity) {
        castleHeartEntity = Entity.Null;

        if (fallbackHeart.TryGetValue(charEntity, out var heartInfo))
        {
            castleHeartEntity = heartInfo.CastleHeart;
            MakeHeartUsableEverywhere(castleHeartEntity);
            return;
        }

        if (charEntity.Has<TeamReference>())
        {
            var team = charEntity.Read<TeamReference>().Value;
            foreach (var allyEntries in Core.EntityManager.GetBuffer<TeamAllies>(team))
            {
                var allyEntity = allyEntries.Value;
                if (allyEntity.Has<CastleTeamData>())
                {
                    castleHeartEntity = allyEntity.Read<CastleTeamData>().CastleHeart;
                    MakeHeartUsableEverywhere(castleHeartEntity);
                    fallbackHeart.TryAdd(charEntity,new HeartInfo {CastleHeart = castleHeartEntity});
                    break;
                }
            }
        }

        static void MakeHeartUsableEverywhere(Entity heartEntity)
        {
            if (heartEntity == Entity.Null) return;

            if (heartEntity.Has<SyncBoundingBox>()) heartEntity.Remove<SyncBoundingBox>();
            if (!heartEntity.Has<SyncToUserBitMask>())
            {
                heartEntity.Add<SyncToUserBitMask>();
                heartEntity.Write(new SyncToUserBitMask()
                {
                    Value = new UserBitMask128()
                    {
                        _Value = new int4(-1, -1, -1, -1)
                    }
                });
            }
        }
    }
}