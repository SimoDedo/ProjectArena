using ScriptableObjectArchitecture;

namespace Logging
{
    public struct HitInfo
    {
        public float x;
        public float z;
        public int hitEntityID;
        public string hitEntity;
        public int hitterEntityID;
        public string hitterEntity;
        public int damage;
    }

    public class BaseHitInfoGameEvent : GameEventBase<HitInfo> { }

    public sealed class HitInfoGameEvent : ScriptableObjectSingleton<BaseHitInfoGameEvent> { }
}