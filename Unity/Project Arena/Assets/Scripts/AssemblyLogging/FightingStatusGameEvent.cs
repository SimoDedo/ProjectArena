using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public struct FightingStatus
    {
        public int entityId;
        public bool isActivelyFighting;
    }

    public class BaseFightingStatusGameEvent : GameEventBase<FightingStatus> { }

    // Event raised by combat-aware behaviours to signal when active fighting starts or ends.
    // It might be raised multiple times per frame, so care should be taken to consider only the latest one received
    public sealed class FightingStatusGameEvent : ScriptableObjectSingleton<BaseFightingStatusGameEvent> { }
}