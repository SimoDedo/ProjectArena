using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public struct EnemyAwarenessStatus
    {
        public int observerID;
        public bool isEnemyDetected;
    }

    public class BaseEnemyAwarenessStatusGameEvent : GameEventBase<EnemyAwarenessStatus> { }

    // Event raised by every entity to signal if it is actively aware (e.g. not just suspicious
    // due to damage) or not of the enemy presence.
    // Normally raised only once per frame, unless the entity dies, in which case it it raised twice
    
    public sealed class
        EnemyAwarenessStatusGameEvent : ScriptableObjectSingleton<BaseEnemyAwarenessStatusGameEvent> { }
}