using Events;

namespace Logging
{
    public struct MapInfo
    {
        public float height;
        public float width;
        public float ts;
        public bool f;
    }

    public class BaseMapInfoGameEvent : GameEventBase<MapInfo>
    {
    }

    public sealed class MapInfoGameEvent : ClassSingleton<BaseMapInfoGameEvent>
    {
    }
}