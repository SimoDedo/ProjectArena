using ScriptableObjectArchitecture;

namespace AssemblyLogging
{
    public class BaseFinishedExperimentGameEvent : GameEventBase { }

    public sealed class ExperimentEndedGameEvent : ScriptableObjectSingleton<BaseFinishedExperimentGameEvent> { }
}