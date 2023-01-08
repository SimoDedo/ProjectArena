using Events;

namespace Logging
{
    public class BaseStartNewExperimentGameEvent : GameEventBase
    {
    }

    public sealed class StartNewExperimentGameEvent : ClassSingleton<BaseStartNewExperimentGameEvent>
    {
    }
}