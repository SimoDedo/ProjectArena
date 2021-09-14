using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public struct KillInfo
    {
        public float x;
        public float z;
        public int killedEntityID;
        public string killedEntity;
        public int killerEntityID;
        public string killerEntity;
    }

    public class BaseKillInfoGameEvent : GameEventBase<KillInfo>
    {
    }

    public sealed class KillInfoGameEvent : ScriptableObjectSingleton<BaseKillInfoGameEvent>
    {
    }
}