using Events;

namespace Logging
{
    public struct SpawnInfo
    {
        public float x;
        public float z;
        public int entityId;
        public string spawnEntity;
    }

    public class BaseSpawnInfoGameEvent : GameEventBase<SpawnInfo>
    {
    }

    public sealed class SpawnInfoGameEvent : ClassSingleton<BaseSpawnInfoGameEvent>
    {
    }
}