using Events;

namespace Logging
{
    public class BaseFinishedExperimentGameEvent : GameEventBase
    {
    }

    public sealed class ExperimentEndedGameEvent : ClassSingleton<BaseFinishedExperimentGameEvent>
    {
    }
}