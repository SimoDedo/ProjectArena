using Events;

namespace Logging
{
    public struct KillInfo
    {
        public float killedX;
        public float killedZ;
        public int killedEntityID;
        public string killedEntity;
        public int killerEntityID;
        public string killerEntity;
        public float killerX;
        public float killerZ;
    }

    public class BaseKillInfoGameEvent : GameEventBase<KillInfo>
    {
    }

    public sealed class KillInfoGameEvent : ClassSingleton<BaseKillInfoGameEvent>
    {
    }
}