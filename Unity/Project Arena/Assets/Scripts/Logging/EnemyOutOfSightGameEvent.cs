using ScriptableObjectArchitecture;

namespace Logging
{
    /// <summary>
    /// Event sent when an entity detects for the first time that an enemy has exited its field of view.
    /// The int represented by this event is the id of the entity.
    /// </summary>
    public class BaseEnemyOutOfSightGameEvent : GameEventBase<int>
    {
    }

    public sealed class EnemyOutOfSightGameEvent : ScriptableObjectSingleton<BaseEnemyOutOfSightGameEvent>
    {
    }
}