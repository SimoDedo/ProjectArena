using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public struct EnterFightInfo
    {
        public float x;
        public float z;
        public int entityId;
    }

    public class BaseEnterFightGameEvent : GameEventBase<EnterFightInfo> { }

    public sealed class FightEnterGameEvent : ScriptableObjectSingleton<BaseEnterFightGameEvent> { }
}