using Events;

namespace Logging
{
    public struct PositionInfo
    {
        public float x;
        public float z;
        public float dir;
        public int entityID;
    }

    public class BasePositionInfoGameEvent : GameEventBase<PositionInfo>
    {
    }

    public sealed class PositionInfoGameEvent : ClassSingleton<BasePositionInfoGameEvent>
    {
    }
}