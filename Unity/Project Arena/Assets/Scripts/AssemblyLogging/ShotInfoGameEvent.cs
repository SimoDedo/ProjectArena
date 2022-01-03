using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public struct ShotInfo
    {
        public float x;
        public float z;
        public float direction;
        public int ownerId;
        public int gunID;
        public int ammoInCharger;
        public int totalAmmo;
    }

    public class BaseShotInfoGameEvent : GameEventBase<ShotInfo> { }

    public sealed class ShotInfoGameEvent : ScriptableObjectSingleton<BaseShotInfoGameEvent> { }
}