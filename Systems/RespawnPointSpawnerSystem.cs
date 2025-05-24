using Unity.Entities;
using Unity.Mathematics;
using VComforts.Factory;
using VComforts.Managers;

namespace VComforts.Systems
{
    public class RespawnPointSpawnerSystem
    {
        private readonly RespawnPointFactory _respawnPointFactory = new();
        private readonly RespawnPointManager _respawnPointManager = new();

        public bool SpawnRespawnPoint(float3 position, quaternion rotation, Entity character, Entity user)
        {
            return _respawnPointFactory.CreateRespawnPoint(position, rotation, character, user);
        }

        public bool RemoveRespawnPoint(float3 position, Entity user, bool isAdmin, bool keepAssignedRespawnPoint = false)
        {
            return _respawnPointManager.RemoveRespawnPoint(position, user, isAdmin, keepAssignedRespawnPoint);
        }

        public bool SetRespawnPoint(float3 position, Entity user, bool isAdmin)
        {
            return _respawnPointManager.SetRespawnPoint(position, user, isAdmin);
        }
    }
}