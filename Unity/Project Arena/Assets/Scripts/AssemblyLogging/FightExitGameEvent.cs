using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public struct ExitFightInfo
    {
        public float x;
        public float z;
        public int entityId;
    }

    public class BaseExitFightGameEvent : GameEventBase<ExitFightInfo> { }

    public sealed class FightExitGameEvent : ScriptableObjectSingleton<BaseExitFightGameEvent> { }
}