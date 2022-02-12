using ScriptableObjectArchitecture;

namespace Logging
{
    /// <summary>
    /// Represents the game metrics as calculated by an entity.
    /// </summary>
    public struct GameMetrics
    {
        public int entityId;

        public float timeToEngage;

        public int numberOfFights;
        public float timeInFights;

        public int numberOfRetreats;
        public float timeToSurrender;

        public float timeBetweenSights;
    }

    public class BaseEntityGameMetricsGameEvent : GameEventBase<GameMetrics>
    {
    }

    public class EntityGameMetricsGameEvent : ScriptableObjectSingleton<BaseEntityGameMetricsGameEvent>
    {
    }
}