using ScriptableObjectArchitecture;

namespace Logging
{
    public class BaseFinishedExperimentGameEvent : GameEventBase { }

    public sealed class ExperimentEndedGameEvent : ScriptableObjectSingleton<BaseFinishedExperimentGameEvent> { }
}